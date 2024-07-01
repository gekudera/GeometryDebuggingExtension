#pragma once
class Point
{
public:
	double x;
	double y;
	double z;

	Point();
	Point(double x1, double y1, double z1);
	void copy_point(Point a1);
	double get_distanse(Point* a2);
};

