#include "Node.h"

Node::Node(int top, Point coordinates)
{
	top_id = top;
	coord = new Point();
	coord->copy_point(coordinates);
}

Node::Node(int top, double x, double y, double z)
{
	top_id = top;
	coord = new Point(x, y, z);
}

double Node::get_distance(Node a2)
{
	return (this->coord->get_distanse(a2.coord));
}
