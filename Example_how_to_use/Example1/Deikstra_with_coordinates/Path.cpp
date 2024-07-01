#include "Path.h"

void Path::AddNode(Node n)
{
	path.push_back(n);
}

int Path::GetSize()
{
	return path.size();
}

