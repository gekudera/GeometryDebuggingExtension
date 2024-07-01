#pragma once
#include <vector>
#include "Node.h"
#include "Edge.h"
#include "Path.h"

class Graph
{
public:
    std::vector<Node> nodes;
    std::vector<std::vector<int>> matrix;
    std::vector<Edge> edges;

    void addNode(Point new_p);
    void addEdge(int from, int to, int weight);
    void addEdge(int from, int to);
    void printMatrix();
    void fillRandom(int gr_size);
    void fillGrid(int n, int m);
    Path algDeikstra(int fromIndex, int toIndex);
};

