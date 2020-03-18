# Graph Digitizer

A standalone Winodws desktop tool to extract the numeric coordinates of a 2D graph in image format.


## Basic Program Usage

1. Load a graph with the first button of the above toolbar.
2. Select two points of the horizontal axis (X axis).
3. Select two points of the vertical axis (Y axis).
4. A dialog will appear for specifying the values of each axis that have been selected, and also whether the axes are logarithmic or not.
5. Now the graph is fully calibrated. Start picking curve points with the left mouse button.
6. After all the desired points have been selected, they can be copied by either the specific toolbar button or the contextual menu of the left grid.


### Details

Once the axes of the graph are defined, the program will translate screen coordinates into graph coordinates. The axes can also
be be logarithmic.

The points can be easily copied into the clipboard and pasted in other programs, such as text files or spreadsheets. The program
also supports graphs that are not perfectly square (e.g. from an angled photograph).

**Note: Although the latest release is fully functional, the project is quite old and undergoing several changes that will allow
further growth. The most important of these is the migration from a pure view approach to the MVVM pattern. After this, testing and
an a CI/CD pipeline will also be implemented.**

### Download

The latest version of the tool can be downloaded from the [Releases](https://github.com/fernandreu/graph-digitizer/releases) section
of this GitHub repository.

### Screenshots

![Screenshot](Screenshot.png)
