# Repuzzler
AR Puzzle Solver
(in development, version 0.0)

Current stage is making a concept proof supporting only Rubik Cube.

## Folders
### Learners
Python scripts to learn CNN models with Keras and Tensorflow.
Includes customized data generator with augmentation.

### Tools
C#.Net applications to process and prepare training data (images and labels), includes:
* Removing blue screen from photos;
* Labeling objects bounds (single object per picture, with a number of key-points, e.g. edges of a cube). Later versions of this tool use current model to help with labeling more data.

### Ravlyk
Shared libraries in C#.Net with image processing algorithms (copied from https://github.com/MykolaKovalchuk/SAE5)
