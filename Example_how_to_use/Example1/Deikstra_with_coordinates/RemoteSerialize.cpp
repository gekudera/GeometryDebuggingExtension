#include <fstream>
#include <vector>
#include <math.h>
#include <algorithm>
#include <memory>
#include <thread>
#include <omp.h>
#include <iostream>
#include <stdio.h>
#include <windows.h>
#include <string>
#include <direct.h>
#include <filesystem>
#include "Edge.h"
#include "Graph.h"
#include "Path.h"


#define M_PI 3.14159265358979323846
#define MMAPNAME L"MySharedMemory"
#define MMAPSIZE 1000

std::string buffer = "";
std::string file_name = "output_serializestring.txt";


//сериализатор для точки
static void serialize(const Point* p)
{
	buffer += "(" + std::to_string(p->x) + "," + std::to_string(p->y) + "," + std::to_string(p->z) + ")";
}

void serialize(const Point* p, std::string name)
{
	buffer += "points: " + name + '\n';
	serialize(p);
	buffer += '\n';
}

void serializeRef(Point** p, std::string name)
{
	serialize(*p, name);
}

//сериализатор для линии
void serializeline(const Point* p1, const Point* p2, std::string colour)
{
	serialize(p1);
	serialize(p2);
	buffer += colour;
}

void serialize(const Edge* p, std::string name)
{
	buffer += "lines: " + name + '\n';
	serializeline(p->prev_point, p->next_point, " (0, 10, 0) \n");
}

void serialize(const Graph* p, std::string name)
{
	buffer += "points: " + name + ".points" + '\n';
	for (int i = 0; i < p->nodes.size(); i++)
	{
		serialize(p->nodes[i].coord);
		buffer += '\n';
	}

	buffer += "lines: " + name + '\n';
	for (int i = 0; i < p->edges.size(); i++)
	{
		serializeline(p->edges[i].prev_point, p->edges[i].next_point, " (0, 10, 0) \n");
	}
}

void serialize(const Graph** p, std::string name)
{
	serialize(*p, name);
}

void serialize(const Path* p, std::string name)
{
	buffer += "lines: " + name + '\n';
	for (int i = 0; i < p->path.size()-1; i++)
	{
		serializeline(p->path[i].coord, p->path[i+1].coord, " (10, 0, 0) \n");
	}

	buffer += "points: " + name + ".points" + '\n';
	for (int i = 0; i < p->path.size(); i++)
	{
		serialize(p->path[i].coord);
		buffer += "7 (10, 0, 0) \n";
	}
}

void serialize(const Path** p, std::string name)
{
	serialize(*p, name);
}

struct GeometryObject
{
	std::string name;
	std::string type;
	void* address;
};


std::string GetLastWordRef(std::string s)
{
	std::vector<std::string> res;
	int pos = 0;
	while (pos < s.size()) {
		pos = s.find(" ");
		res.push_back(s.substr(0, pos));
		s.erase(0, pos + 1); // 1 is the length of the delimiter, " "
	}

	if ((res.size() >= 3) && (res[res.size() - 2] == "*"))
	{
		return (res[res.size()-3] + " *");
	}
	else return res[res.size() - 1];
}


std::string GetLastWord(std::string s)
{
	size_t pos = s.find_last_of(' ');
	std::string phone(s.substr(pos + 1));
	return phone;
}

std::string SerializeObjects(std::vector<GeometryObject> obj)
{
	buffer = "";

	for (const auto& o : obj)
	{
#define REGISTER(X) { auto otype = GetLastWord(typeid(X).name()); \
				std::cout << "TYPE: " << otype; \
				if(otype == o.type) { \
					std::cout << "this was " << #X << std::endl; \
                    try { \
                         serialize(static_cast<X*>(o.address), o.name); \
                    } catch (...) \
		            { \
				    std::cout << "serialize failed" << "\n" << std::flush; \
		            }\
						continue; \
				} \
				}

#define REGISTER_REF(X) { auto otype = GetLastWordRef(typeid(X).name()); \
				std::cout << "\nREF TYPE: " << otype << "\nFULL TYPE: " << typeid(X).name(); \
				if(otype == o.type) { \
					std::cout << "this was " << #X << std::endl; \
                    try { \
                         serialize(*static_cast<X*>(o.address), o.name); \
                    } catch (...) \
		            { \
				    std::cout << "serialize failed" << "\n" << std::flush; \
		            }\
						continue; \
				} \
				}

			REGISTER(Point)
			REGISTER(Edge)
			REGISTER(Graph)
			REGISTER_REF(Point*)
			REGISTER(Path)
			REGISTER_REF(Path *)
			REGISTER_REF(Graph *)
			
			std::cout << "object " << o.type << " is unknown\n";
#undef CHECK
	}
	return buffer;
}


__declspec(dllexport) void SendSignWithSharedMemory()
{
	HANDLE handle = CreateFileMappingW(INVALID_HANDLE_VALUE, NULL, PAGE_READWRITE, 0, 4, MMAPNAME);
	char* ptr = (char*)MapViewOfFile(handle, FILE_MAP_READ | FILE_MAP_WRITE, 0, 0, MMAPSIZE);

	//получение и запись пути к файлу
	char buff[MAX_PATH];
	_getcwd(buff, MAX_PATH);
	std::string current_working_dir(buff);
	current_working_dir += "\\" + file_name;
	std::cout << current_working_dir;
	for (int i = 4; i < current_working_dir.size() + 4; i++)
		ptr[i] = current_working_dir[i - 4];
	ptr[0] = 1;

	std::cout << "SIGN WAS SENT";
}

__declspec(dllexport) std::string RecieveObjectsArray()
{
	HANDLE handle;
	char* ptr = nullptr;
	int msgSize = 0;

	while (true)
	{
		handle = OpenFileMappingW(FILE_READ_ACCESS, false, MMAPNAME);

		if (handle == NULL)
			std::cout << "CreateFileMapping error: \n" << GetLastError();
		else
		{
			ptr = (char*)MapViewOfFile(handle, FILE_MAP_READ, 0, 0, MMAPSIZE);
			std::cout << "opened mapped file with handle " << handle << ", mapped view to " << (void*)ptr << '\n';

			msgSize = int((unsigned char)(ptr[3]) << 24 |
				(unsigned char)(ptr[2]) << 16 |
				(unsigned char)(ptr[1]) << 8 |
				(unsigned char)(ptr[0]));

			if (msgSize != 0) break;
		}
		std::cout << "error with read from memory \n";
		Sleep(1000);
	}

	std::string data = "";
	std::cout << "there are " << msgSize << " letters\n";
	std::cout << "the message is: ";
	for (int i = 0; i < 2 * msgSize; i++)
	{
		if (((int)ptr[1 + i] >= 32) && ((int)ptr[1 + i] <= 127))
			data += ptr[1 + i];
		std::cout << ptr[1 + i];
	}
	std::cout << '\n';
	return data;
}

__declspec(dllexport) std::vector<GeometryObject> tokenize(std::string const& str, char delim)
{
	std::vector<GeometryObject> objects;
	size_t start;
	size_t end = 0;
	GeometryObject* temp = new GeometryObject();
	int count = 0;

	while ((start = str.find_first_not_of(delim, end)) != std::string::npos)
	{
		end = str.find(delim, start);
		std::string temp_str = str.substr(start, end - start);

		switch (count)
		{
		case(0):
			temp->name = temp_str;
			count++;
			break;

		case(1):
			temp->type = temp_str;
			count++;
			break;

		case(2):
			char* end;
			char* ptr = nullptr;
			long long int number = strtoll(temp_str.c_str(), &end, 0);
			ptr += number;
			temp->address = ptr;
			objects.push_back(*temp);
			count = 0;
			break;
		}
	}

	return objects;
}

void WriteSerializeStringToFile(std::string input)
{
	std::ofstream out(file_name);
	out.clear();
	out << input;
	out.close();
}

__declspec(dllexport) void StartRemoteSerialize()
{
	std::cout << "remote thread is created";

	//получение списка отрисовыемых объектов от shmem
	std::string objects_str = RecieveObjectsArray();

	//разделение строки на конкретные объекты
	std::vector<GeometryObject> obj_array = tokenize(objects_str, '|');

	std::string ser_obj = SerializeObjects(obj_array);

	std::cout << std::endl << "buffer: " << buffer;
	//записать сериализованную строку в файл
	WriteSerializeStringToFile(buffer);

	//посылаем расширению сообщение о завершении
	SendSignWithSharedMemory();
}