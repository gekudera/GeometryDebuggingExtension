#include "Graph.h"
#include <iostream>
#include <ctime>

#define MIN_VAL -100
#define MAX_VAL 100

#define CELL_LEN 10

    //Добавление узла в граф
    void Graph::addNode(Point new_p)
    {
        int num = nodes.size();
        Node* node = new Node(num, new_p);
        nodes.push_back(*node);
        matrix.resize(nodes.size());
        for (int i = 0; i < nodes.size(); i++) 
        {
            matrix[i].resize(nodes.size());
        }
    }

    //Добавление ребра в граф
    void Graph::addEdge(int from, int to, int weight) 
    {
        if (from > nodes.size() || to > nodes.size() || from < 0 || to < 0) 
        {
            std::cout << "Node not found" << std::endl;
            return;
        }

        matrix[from][to] = weight;
        matrix[to][from] = weight;
        Edge e(nodes[from].coord, nodes[to].coord);
        edges.push_back(e);
    }

    void Graph::addEdge(int from, int to)
    {
       double dist =  nodes[from].get_distance(nodes[to]);

       matrix[from][to] = dist;
       matrix[to][from] = dist;
       Edge e(nodes[from].coord, nodes[to].coord);
       edges.push_back(e);
    }

    //Вывод матрицы смежности
    void Graph::printMatrix() {
        std::cout << "    ";
        for (int j = 0; j < nodes.size(); j++) 
        {
            printf("%3d ", nodes[j].top_id);
        }

        std::cout << std::endl;
        for (int i = 0; i < nodes.size(); i++) 
        {
            printf("%3d|", nodes[i].top_id);  //Имя узла

            for (int j = 0; j < nodes.size(); j++) 
            {
                printf("%3d ", matrix[i][j]);
            }
            std::cout << std::endl;
        }
    }

    void Graph::fillGrid(int n, int m)
    {
        // заполняем узлы сетки
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                double random = -5 + rand() % 5;
                Point a(i*10+random, j*10+random, 0);
                this->addNode(a);
            }
        }

        // заполняем ребра графа
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < m; j++)
            {
                if (j != m- 1)
                    this->addEdge(i*m + j, i*m + j + 1);
                if (i != n - 1)
                    this->addEdge(i*m + j, i*m + m + j);
            }
        }
    }

    void Graph::fillRandom(int gr_size)
    {
        srand(time(0));
        for (int i = 0; i < gr_size; i++)
        {
            double x_a = MIN_VAL + rand() % MAX_VAL;
            double y_a = MIN_VAL + rand() % MAX_VAL;
            double z_a = MIN_VAL + rand() % MAX_VAL;
            Point a(x_a, y_a, z_a);

            this->addNode(a);
        }

        for (int i = 0; i < gr_size; i++)
        {
            this->addEdge((int)0 + rand() % gr_size, (int)0 + rand() % gr_size);
        }
        //this->printMatrix();
    }

    //Поиск кратчайшего пути
    Path Graph::algDeikstra(int fromIndex, int toIndex) 
    {
        Graph gr(*this);
        // Массив для хранения кратчайших расстояний от начальной вершины до всех остальных
        std::vector<int> distance(nodes.size(), INT32_MAX);

        // Массив для хранения предыдущих вершин
        std::vector<int> visited(nodes.size(), 0);

        std::vector<int> prev(nodes.size(), -1);

        // Начальная вершина
        distance[fromIndex] = 0;

        // Ищем вершину с минимальным расстоянием, пока не пройдем все вершины
        for (int i = 0; i < nodes.size(); i++) {
            int minIndex = -1;
            int minDistance = INT32_MAX;

            for (int j = 0; j < nodes.size(); j++) {
                // Если вершина еще не пройдена и расстояние до нее меньше минимального
                if (!visited[j] && distance[j] < minDistance) {
                    minIndex = j;
                    minDistance = distance[j];
                }
            }

            // Если вершину не нашли
            if (minIndex == -1) {
                break;
            }

            // Помечаем вершину как пройденную
            visited[minIndex] = 1;

            // Обновляем расстояния до соседей
            for (int j = 0; j < nodes.size(); j++) {
                if (matrix[minIndex][j] != 0) {
                    if (distance[j] > distance[minIndex] + matrix[minIndex][j]) {
                        distance[j] = distance[minIndex] + matrix[minIndex][j];
                        prev[j] = minIndex;
                    }
                }
            }
        }

        Path path_s;
        // Выводим кратчайшие расстояния
        if (distance[toIndex] == INT32_MAX) 
        {
            std::cout << "Path not found" << std::endl;
            return path_s;
        }

        // Восстанавливаем пут
        int index = toIndex;

        while (index != -1) 
        {
            path_s.AddNode(nodes[index]);
            index = prev[index];
        }

        // Выводим путь
        std::cout << "Path: ";
        for (int i = path_s.GetSize() - 1; i >= 0; i--) {
            std::cout << nodes[path_s.path[i].top_id].top_id << " ";
        }
        std::cout << std::endl;
        std::cout << "Distance: " << distance[toIndex] << std::endl;

        return path_s;
    }