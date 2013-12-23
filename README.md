# Unity3D to Gameplay3D physics exporter

A Unity3D extension that exports the physics and camera data in a Unity3D scene to the Gameplay3D .physics and .scene data formats.

![ScreenShot](https://raw.github.com/louis-mclaughlin/unity3d-to-gameplay3d-physics-exporter/master/Img/unity.png)

This tool allows you to rapidly iterate upon Gameplay3D physics scenes within the Unity3D editor.

This tool does not attempt to replicate the physics behaviour of Unity3D in Gameplay3D and therefore the outcome of the both simulations will differ when run side by side.

## Pre-requisites
- Download [Gameplay3D](http://www.gameplay3d.org/)
- Download [Unity3D](http://unity3d.com/)
- Familiarity with Unity3D
- Familiarity with building and running Gameplay3D on your platform
- Familiarity with the [Gameplay3D physics schema](https://github.com/blackberry/GamePlay/wiki/Physics)

## Instructions
### Unity3D
- Run Unity3D
- Create a new Unity project or open an existng one
- Create two sub-directories in your projects root asset folder called **Editor** and **Standard Assets**
- Copy these two .cs files in this depot into the folders you just created:
 - **Unity/Editor/Gameplay3DPhysicsExporter.cs**
 - **Unity/Standard Assets/Gameplay3DRigidBodyParams.cs**
- Create pre-fabs that will represent Gameplay3D collisionObjects:
 - Create an empty gameobject: Menu->GameObject->Create Empty
 - Add a `MeshFilter`, currently the exporter only supports Box, Capsule and Sphere
 - Add a `MeshRenderer` so you can see it in the editor
 - Add a `Collider` for the mesh shape type you chose. Ticking *'Is Trigger'* will cause this pre-fab to be exported as a ghost object.
 - Add a `RigidBody` (optional). A pre-fab without a Rigid Body will be treated as static object with a mass of 0. The *'Mass', 'Is Kinematic', 'Freeze Position'* and *'Freeze Rotation'* attributes will be exported
 - Add a `Gameplay3DRigidBodyParams` if you have a `RigidBody` (optional). You can use this to define additional Gameplay3D rigid body params: *friction, restitution, linearDamping, angularDamping and anisotropicFriction*
- Create your scene using instances of the pre-fabs you just created
- Ensure your main camera is configured correctly in either Orthographic or Persepctive mode, this will be exported as part of the Gameplay3D scene
- Open the exporter tool: Menu->Window->Gameplay3D Physics Exporter:

 ![ScreenShot](https://raw.github.com/louis-mclaughlin/unity3d-to-gameplay3d-physics-exporter/master/Img/exporter.png)

- You'll be presented with a form containing several properties that you must fill in:
 - **Name** - The name of the .scene and .physics files that will be output
 - **Scene output dir** - The directory the .scene file will be written to
 - **Physics output dir** - The directory the .physics file will be written to
 - **GP3D physics dir** - The directory where the .physics file will reside as an absolute path in Gameplay3D. For example, 'res/physics'
 - **GP3D screen dimensions** - This only appears when the main camera in your scene is set to ortho. Set this to the screen width/height defined in your game.config
 - **Export** - When you hit this button you'll see an output log which will either notify you of errors in the input or success
- You can now load the scene in Gameplay3D

### Gameplay3D
See the **Example** directory for an example of how to easily reload .scenes at runtime. All the scene and physics files in **res/scenes** and **res/physics** were generated in Unity3D using the exporter.

![ScreenShot](https://raw.github.com/louis-mclaughlin/unity3d-to-gameplay3d-physics-exporter/master/Img/example2d.png)

![ScreenShot](https://raw.github.com/louis-mclaughlin/unity3d-to-gameplay3d-physics-exporter/master/Img/example3d.png)