#include "Edge.h"

Edge::Edge(Point* from, Point* to)
{
	prev_point = from;
	next_point = to;
}
