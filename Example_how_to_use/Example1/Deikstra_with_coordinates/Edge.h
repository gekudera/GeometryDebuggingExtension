#pragma once
#include<vector>
#include "Point.h"

class Edge
{
public:
	Point* prev_point;
	Point* next_point;

	Edge(Point* from, Point* to);
};

