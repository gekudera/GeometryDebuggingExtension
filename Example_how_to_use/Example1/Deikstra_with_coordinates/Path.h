#pragma once
#include <vector>
#include "Node.h"

class Path
{
public:
	std::vector<Node> path;

	void AddNode(Node n);
	int GetSize();
};

