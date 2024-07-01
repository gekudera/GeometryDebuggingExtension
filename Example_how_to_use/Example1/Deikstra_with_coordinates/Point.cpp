#include "Point.h"
#include <cmath>

Point::Point()
{
	x = 0;
	y = 0;
	z = 0;
}

Point::Point(double x1, double y1, double z1)
{
	this->x = x1;
	this->y = y1;
	this->z = z1;
}

void Point::copy_point(Point a1)
{
	this->x = a1.x;
	this->y = a1.y;
	this->z = a1.z;
}

double Point::get_distanse(Point* a2)
{
	double dist = (double)(this->x - a2->x) * (this->x - a2->x);
	dist += (double)(this->y - a2->y) * (this->y - a2->y);
	dist += (double)(this->z - a2->z) * (this->z - a2->z);
	return sqrt(dist);
}
