# Purpose
This extension is used to visualize geometry models in debug mode in VisualStudio IDE 2022.
This tool is useful for debugging geometry tasks.

# Dependencies
The extension uses a specific vizualizer. Before installation you need to build and install this project:
https://github.com/dafadey/geomView/tree/main 
Only after successful installation of the visualizer can you proceed to the next steps.

# Build
1. GeomView vizualizer is installed successfully.
2. git clone HTTPS
3. Open project in Visual Studio IDE 2022. Go to GeomViewShell properties and add the GeomView paths to the project properties (see screen 1,2,3).
C/C++ -> General -> Additional Include Directories -> ${PATH TO INCLUDE GEOMVIEW}
Linker -> General -> Additional Library Directories -> ${PATH TO LIB GEOMVIEW}
Linker -> Input -> add "libgeom_view.lib;opengl32.lib;gdi32.lib;"

![screen1](https://github.com/gekudera/GeometryDebuggingExtension/assets/67547100/1457357a-60bf-442a-8827-a69f32a1d3de)

![screen2](https://github.com/gekudera/GeometryDebuggingExtension/assets/67547100/a39b23e3-e158-4911-83a7-61bbbc253993)

![screen3](https://github.com/gekudera/GeometryDebuggingExtension/assets/67547100/1f460d99-b440-4596-9ee3-9c2d926b27a7)

4. Build GeomViewShell
if the build failed, see steps 1-3.
5. If the build was successful, go to GeometryDebuggingExtension/x64/Release and copy all files in this folder.
Then insert this files in GeometryDebuggingExtension\GeometryDebuggingWindow\bin\Release.
6. Build GeometryDebuggingWindow in VisualStudio IDE.
If the build was successful, you can install this extension on your computer.

# Install
1. Go to GeometryDebuggingExtension\GeometryDebuggingWindow\bin\Release and find GeometryDebuggingWindow.vsix. Install this file with double click.
NOTE: you need to close VisualSludio IDE before installation.
2. Follow the installation instructions.
3. 
![image](https://github.com/gekudera/GeometryDebuggingExtension/assets/67547100/a3a2df47-adf8-4204-9e28-9765b76545af)

![image](https://github.com/gekudera/GeometryDebuggingExtension/assets/67547100/cb6c4249-296e-4e16-8b77-b562142ab38c)

Extension installed successfully!

# Adding serialize functions to project
