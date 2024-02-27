#include "geom_view.h"
#include <clocale>
#include <fstream>

#define MyFunctions _declspec(dllexport)

geom_view gv;

extern "C" {

	MyFunctions void InitGeomView(wchar_t* b)
	{
		std::setlocale(LC_NUMERIC, "en_US.UTF-8");
		gv.init((char*)b);
	}

	MyFunctions void ReloadGeomView()
	{
		gv.reload();
	}
}