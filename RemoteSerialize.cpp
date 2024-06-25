#include <fstream>
#include <vector>
#include <algorithm>
#include <memory>
#include <thread>
#include <iostream>
#include <stdio.h>
#include <windows.h>
#include <string>
#include <direct.h>
#include <filesystem>
#include "mb_cart_point3d.h"
#include "topology.h"
#include <curve3d.h>


#define M_PI 3.14159265358979323846
#define MMAPNAME L"MySharedMemory"
#define MMAPSIZE 1000

std::string buffer = "";
std::string file_name = "output_serializestring.txt";

//разбиение на примитивы для точки
static void serialize(const MbCartPoint3D* p)
{
	buffer += "(" + std::to_string(p->x) + "," + std::to_string(p->y) + "," + std::to_string(p->z) + ")";
}

void serialize(const MbCartPoint3D* p, std::string name)
{
	buffer += "points: " + name + '\n';
	serialize(p);
	buffer += '\n';
}

//сериализатор для линии
void serializeline(const MbCartPoint3D* p1, const MbCartPoint3D* p2)
{
	serialize(p1);
	serialize(p2);
	buffer += " (0, 1, 0)\n";
}

void serialize(const MbCurve3D* p, std::string name)
{
	buffer += "lines: " + name + '\n';
	int count_of_points = 30.0;
	double fixed_step = (p->GetTMax() - p->GetTMin()) / count_of_points;


	for (double i = p->GetTMin(); i < p->GetTMax(); i += fixed_step)
	{
		MbCartPoint3D p1;
		MbCartPoint3D p2;
		p1 = p->PointOn(i);
		double next_i = i + fixed_step;
		p2 = p->PointOn(next_i);
		serializeline(&p1, &p2);
	}

	buffer += '\n';
}

void serialize(const MbFace* p, std::string name)
{
	std::cout << "in serialize MbFace \n";
	buffer += "points: " + name + '\n';
	double min_u = p->GetRect().GetXMin();
	double max_u = p->GetRect().GetXMax();
	double min_v = p->GetRect().GetYMin();
	double max_v = p->GetRect().GetYMax();
	int n = 100; // количество точек

	//посчитать 3 объемных точки. Вычислить расстояние от первой до второй и от первой до третьей. 
	//Это и будет отношение альфа к бета
	double step = sqrt((max_u - min_u) * (max_v - min_v) / n);
	std::cout << "step = " << step << std::endl;
	std::cout << "max u = " << max_u << std::endl;
	std::cout << "min u = " << min_u << std::endl;
	std::cout << "max v = " << max_v << std::endl;
	std::cout << "min v = " << min_v << std::endl;

	//MbCartPoint3D point1, point2, point3;
	//p->PointOn(min_u, min_v, point1);
	//p->PointOn(min_u + step, min_v, point2);
	//p->PointOn(min_u, min_v + step, point3);
	//serialize(&point1);
	//buffer += '\n';
	//serialize(&point2);
	//buffer += '\n';
	//serialize(&point3);
	//buffer += '\n';

	for (double u = min_u; u <= max_u; u = u + step)
	{
		for (double v = min_v; v <= max_v; v = v + step)
		{
			MbCartPoint3D point;
			double precision = 0.01;
			p->PointOn(u, v, point);
			MbCartPoint face_point(u, v);
			if (p->DistanceToBorder(face_point, precision) >= 0)
				serialize(&point);
			
			buffer += '\n';
		}
	}
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
	size_t pos = 0;
	while (pos < s.size()) {
		pos = s.find(" ");
		res.push_back(s.substr(0, pos));
		s.erase(0, pos + 1); // 1 is the length of the delimiter, " "
	}

	if ((res.size() >= 3) && (res[res.size() - 2] == "*"))
	{
		return (res[res.size() - 3] + "*");
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
				std::cout << "TYPE: " << otype << std::endl; \
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

			REGISTER(MbCartPoint3D);
			REGISTER(MbCurve3D);
			REGISTER(MbFace);
			
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
	int i=4;
	for (; i < current_working_dir.size() + 4; i++)
		ptr[i] = current_working_dir[i - 4];
	ptr[i] = char(0);
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

__declspec(dllexport) std::string tokenize_type(std::string const& str, char delim)
{
	size_t end = 0;
	size_t start;
	int count = 0;
	std::string temp_str;
	std::string ret_str;
	std::string second_str;
	std::string third_str;

	while ((start = str.find_first_not_of(delim, end)) != std::string::npos)
	{
		end = str.find(delim, start);

		temp_str = str.substr(start, end - start);

		switch (count)
		{
		case(0):
			ret_str += temp_str;
			count++;
			break;

		case(1):
			second_str = temp_str;
			ret_str += temp_str;
			count++;
			break;

		case(2):
			third_str = temp_str;
			ret_str += temp_str;
			count = 0;
			break;
		}
	}

	if (third_str == "&")
		return second_str;

		return ret_str;
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
			temp->type = tokenize_type(temp_str, ' ');
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

void WriteSerializeStringToFile()
{
	std::ofstream out(file_name);
	out.clear();
	out << buffer;
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
	WriteSerializeStringToFile();
	
	//посылаем расширению сообщение о завершении
	SendSignWithSharedMemory();
}