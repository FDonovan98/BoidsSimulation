Boids simulation implemented in Unity.

As stated, code is licensed under GNU license, but I would always love to see anything you use the code in!

Project formed a submission for a University module. **IF YOU ARE A MARKER** the below branches exist to provide a snapshot of the project at different stages to help demonstrate the projects evolution.

*initial-implementation*
*partition-multithread*
*spatial-partition*

If you are looking to use the project, I recommend using *initial-implementation* or *dev* as these are the most stable depending on your intended use. *initial-implementation* will not scale to large flocks well, but will avoid obstacles in the scene, while the current version on *dev* is better optimised for large flocks (1000 boids at ~120fps) they will ignore and pass through any objects in scene.