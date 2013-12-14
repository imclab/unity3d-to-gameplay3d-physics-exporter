using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// A Unity3D extension that partially exports the physics and camera data in a Unity3D
/// scene to the Gameplay3D .physics and .scene data formats.
/// 
/// The goal of this tool is to allow you to rapidly iterate upon Gameplay3D physics scenes within
/// the Unity3D editor. You can re-load the output scene file at runtime in your Gameplay3D application and use
/// getPhysicsController()->drawDebug() to visualise the scene.
/// 
/// This tool does not attempt to replicate the physics behaviour of Unity3D in Gameplay3D and therefore 
/// the outcome of the physics simulations will differ.
/// </summary>
public class Gameplay3DPhysicsExporter : EditorWindow
{
    private string fileName = string.Empty;                 // The name of the .scene and .physics files
    private string sceneDir = string.Empty;                 // The directory to output the .scene file to
    private string physicsDir = string.Empty;               // The directoy to output the .physics file to
    private string physicsDirGameplay3D = string.Empty;     // The Gameplay3D dir of the .physics directory
    private Vector2 viewportGameplay3D = Vector2.zero;      // The size of the Gameplay3D viewport defined in game.config
    private StringBuilder logOutput = new StringBuilder();
    
    [MenuItem("Window/Gameplay3D Physics Exporter")]
    public static void ShowWindow()
    {
        EditorWindow window = EditorWindow.GetWindow(typeof(Gameplay3DPhysicsExporter));
        window.title = "GP3D Phys Ex";
    }
    
    /// <summary>
    /// Exports the camera and physics data in the Unity3D scene to .physics and .scene files
    /// </summary>
    void Export()
    {
        int physicsNodeCount = 0;
        string physicsFileName = fileName + ".physics";
        string physicsFile = (physicsDir + "/" + physicsFileName).Replace("\\", "/");
        string sceneFile = (sceneDir + "/" + fileName + ".scene").Replace("\\", "/");
        StringBuilder physicsOutput = new StringBuilder();
        StringBuilder sceneOutput = new StringBuilder();
        HashSet<int> processedPrefabs = new HashSet<int>();
        Dictionary<string, int> nodeNameOccurenceMap = new Dictionary<string, int>();

        logOutput.AppendLine("Exporting GameObjects in current scene...");

        // Iterate all gameobjects in currently loaded scene
        foreach (object obj in FindSceneObjectsOfType(typeof(GameObject)))
        {
            GameObject gameObj = (GameObject)obj;
            object prefab = PrefabUtility.GetPrefabParent(gameObj);
            Collider collider = gameObj.GetComponent<Collider>();

            // Export either rigid bodies with colliders or colliders by themselves. Colliders without
            // rigid bodies will be treated as static rigid bodies with a mass of 0.
            bool isValidPhysicsObject = (gameObj.rigidbody != null && collider != null) || collider != null;
            
            // Each pre-fab and gameobject not linked to a pre-fab will require a collisionObject
            bool isCollisionObjectExported = isValidPhysicsObject && prefab != null &&
                !processedPrefabs.Add(prefab.GetHashCode());

            if (isValidPhysicsObject)
            {
                // Begin the scene scope and export the camera before exporting
                // any physics nodes
                if (physicsNodeCount == 0)
                {
                    sceneOutput.AppendLine("scene");
                    sceneOutput.AppendLine("{");

                    ExportCamera(sceneOutput);
                }

                // GameObject names aren't unique so count the number of occurences of each
                // name so they can be output as sequential node names e.g. Box_0, Box_1
                if (!nodeNameOccurenceMap.ContainsKey(gameObj.name))
                {
                    nodeNameOccurenceMap.Add(gameObj.name, 0);
                }

                ExportPhysicsNode(sceneOutput,
                    gameObj, gameObj.name + "_" + nodeNameOccurenceMap[gameObj.name]++,
                    physicsFileName);

                ++physicsNodeCount;

                if (!isCollisionObjectExported)
                {
                    ExportCollisionObject(physicsOutput, gameObj);
                }
            }
        }
        
        ExportGravity(sceneOutput);
        
        logOutput.AppendLine(physicsNodeCount + " physics scene node(s) found");
        logOutput.AppendLine(processedPrefabs.Count + " unique collisionObject(s) found");
        
        // Finally, output the .physics and .scene files
        if(WriteToFile(physicsFile, physicsOutput))
        {
            WriteToFile(sceneFile, sceneOutput);
        }
    }
    
    /// <summary>
    /// Exports the main camera in the current scene in a Gameplay3D node called 'Camera'.
    /// The camera property name will be the name of the current main camera stipped of any
    /// white space. This export takes into accouunt whether the camera is ortho or persp.
    /// 
    /// Attributes:
    /// 
    /// translate
    /// scale
    /// camera
    ///     - type              (Supported: ORTHOGRAPHIC | PERSPECTIVE)
    ///     - nearPlane
    ///     - farPlane
    ///     - zoomX             (Only for ORTHOGRAPHIC)
    ///     - zoomY             (Only for ORTHOGRAPHIC)
    ///     - fieldOfView       (Only for PERSPECTIVE)
    /// 
    /// </summary>
    void ExportCamera(StringBuilder sceneOutput)
    {
        sceneOutput.AppendLine("\tnode Camera");
        sceneOutput.AppendLine("\t{");
        
        sceneOutput.AppendLine(string.Format("\t\ttranslate = {0}, {1}, {2}",
            Camera.main.transform.position.x,
            Camera.main.transform.position.y,
            -Camera.main.transform.position.z));
        
        sceneOutput.AppendLine(string.Format("\t\tscale = {0}, {1}, {2}",
            Camera.main.transform.localScale.x,
            Camera.main.transform.localScale.y,
            Camera.main.transform.localScale.z));
        
        sceneOutput.AppendLine();
        sceneOutput.AppendLine("\t\tcamera " + Camera.main.name.Replace(" ", ""));
        sceneOutput.AppendLine("\t\t{");
        sceneOutput.AppendLine("\t\t\ttype = " + (Camera.main.orthographic ? "ORTHOGRAPHIC" : "PERSPECTIVE"));
        sceneOutput.AppendLine("\t\t\tnearPlane = " + Camera.main.nearClipPlane);
        sceneOutput.AppendLine("\t\t\tfarPlane = " + Camera.main.farClipPlane);
        
        if(Camera.main.orthographic)
        {
            float zoomScale = (1.0f / viewportGameplay3D.y) * (Camera.main.orthographicSize * 2.0f);
            sceneOutput.AppendLine("\t\t\tzoomX = " + (viewportGameplay3D.x * zoomScale));
            sceneOutput.AppendLine("\t\t\tzoomY = " + (viewportGameplay3D.y * zoomScale));
        }
        else
        {
            sceneOutput.AppendLine("\t\t\tfieldOfView = " + Camera.main.fov);    
        }
        
        sceneOutput.AppendLine("\t\t}");
        sceneOutput.AppendLine("\t}");
    }
    
    /// <summary>
    /// Exports a GameObject as a Gameplay3D physics node.
    /// 
    /// Attributes:
    /// 
    /// translate
    /// rotate
    /// collisionObject (Uses physicsDirGameplay3D to build an absolute path)
    /// 
    /// </summary>
    void ExportPhysicsNode(StringBuilder sceneOutput, GameObject gameObj, string nodeName, string physicsFileName)
    {
        sceneOutput.AppendLine();
        sceneOutput.AppendLine("\tnode " + nodeName);
        sceneOutput.AppendLine("\t{");
        sceneOutput.AppendLine(string.Format("\t\ttranslate = {0}, {1}, {2}",
            gameObj.transform.position.x,
            gameObj.transform.position.y,
            -gameObj.transform.position.z));
        
        float angle = 0.0f;
        Vector3 axis = Vector3.zero;
        gameObj.transform.rotation.ToAngleAxis(out angle, out axis);
        sceneOutput.AppendLine(string.Format("\t\trotate = {0}, {1}, {2}, {3}",
            axis.x, axis.y, axis.z, angle));
        
        sceneOutput.AppendLine(string.Format("\t\tcollisionObject = {0}/{1}#{2}",
            physicsDirGameplay3D, physicsFileName, gameObj.name));
        sceneOutput.AppendLine("\t}");
    }
    
    /// <summary>
    /// Exports a Unity3D GameObject as a Gameplay3D collisionObject.
    /// 
    /// Attributes:
    /// 
    /// type                    (Supported: RIGID_BODY | GHOST_OBJECT)
    /// shape                   (Supported: BOX | CAPSULE | SPHERE)
    /// mass
    /// linearFactor            (Only for GameObjects with RigidBody)
    /// angularFactor           (Only for GameObjects with RigidBody)
    /// kinematic               (Only for GameObjects with RigidBody)
    /// radius                  (Only for CAPSULE or SPHERE)
    /// extents                 (Only for BOX)
    /// height                  (Only for CAPSULE)
    /// 
    /// friction                (Only for GameObjects with RigidBody and Gameplay3DRigidBodyParams)
    /// restitution             (Only for GameObjects with RigidBody and Gameplay3DRigidBodyParams)
    /// linearDamping           (Only for GameObjects with RigidBody and Gameplay3DRigidBodyParams)
    /// angularDamping          (Only for GameObjects with RigidBody and Gameplay3DRigidBodyParams)
    /// anisotropicFriction     (Only for GameObjects with RigidBody and Gameplay3DRigidBodyParams)
    /// </summary>
    void ExportCollisionObject(StringBuilder physicsOutput, GameObject gameObj)
    {
        Collider collider = gameObj.GetComponent<Collider>();
        
        if(physicsOutput.Length > 0)
        {
            physicsOutput.AppendLine();
        }
        
        physicsOutput.AppendLine("collisionObject " + gameObj.name);
        physicsOutput.AppendLine("{");
        physicsOutput.AppendLine("\ttype = " + (collider.isTrigger ? "GHOST_OBJECT" : "RIGID_BODY"));
        physicsOutput.AppendLine("\tshape = " + collider.GetType().Name.Replace("Collider","").ToUpper());
        
        bool hasRigidBody = gameObj.rigidbody != null;
        physicsOutput.AppendLine("\tmass = " + (hasRigidBody ? gameObj.rigidbody.mass : 0));
        
        if(collider.GetType() == typeof(BoxCollider))
        {
            physicsOutput.AppendLine(string.Format("\textents = {0}, {1}, {2}",
            gameObj.transform.localScale.x,
            gameObj.transform.localScale.y,
            gameObj.transform.localScale.z));
        }
        else if(collider.GetType() == typeof(CapsuleCollider))
        {
            physicsOutput.AppendLine("\tradius = " + (((CapsuleCollider)collider).radius * gameObj.transform.localScale.x));
            physicsOutput.AppendLine("\theight = " + (((CapsuleCollider)collider).height * gameObj.transform.localScale.y));
        }
        else if(collider.GetType() == typeof(SphereCollider))
        {
            physicsOutput.AppendLine("\tradius = " + (((SphereCollider)collider).radius * gameObj.transform.localScale.x)); 
        }
        
        if(hasRigidBody)
        {
            physicsOutput.AppendLine("\tkinematic = " + gameObj.rigidbody.isKinematic);
            physicsOutput.AppendLine(string.Format("\tlinearFactor = {0}, {1}, {2}",
                (gameObj.rigidbody.constraints & RigidbodyConstraints.FreezePositionX) > 0 ? 0 : 1,
                (gameObj.rigidbody.constraints & RigidbodyConstraints.FreezePositionY) > 0 ? 0 : 1,
                (gameObj.rigidbody.constraints & RigidbodyConstraints.FreezePositionZ) > 0 ? 0 : 1));
            physicsOutput.AppendLine(string.Format("\tangularFactor = {0}, {1}, {2}",
                (gameObj.rigidbody.constraints & RigidbodyConstraints.FreezeRotationX) > 0 ? 0 : 1,
                (gameObj.rigidbody.constraints & RigidbodyConstraints.FreezeRotationY) > 0 ? 0 : 1,
                (gameObj.rigidbody.constraints & RigidbodyConstraints.FreezeRotationZ) > 0 ? 0 : 1));

            Gameplay3DRigidBodyParams gpRigidBodyParams = gameObj.GetComponent<Gameplay3DRigidBodyParams>();

            if (gpRigidBodyParams != null)
            {
                physicsOutput.AppendLine("\tfriction = " + gpRigidBodyParams.Friction);
                physicsOutput.AppendLine("\trestitution = " + gpRigidBodyParams.Restitution);
                physicsOutput.AppendLine("\tlinearDamping = " + gpRigidBodyParams.LinearDamping);
                physicsOutput.AppendLine("\tangularDamping = " + gpRigidBodyParams.AngularDamping);
                physicsOutput.AppendLine(string.Format("\tanisotropicFriction = {0}, {1}, {2}",
                    gpRigidBodyParams.AnisotropicFriction.x,
                    gpRigidBodyParams.AnisotropicFriction.y,
                    gpRigidBodyParams.AnisotropicFriction.z));
            }
        }
        
        physicsOutput.AppendLine("}");
    }
    
    /// <summary>
    /// Exports the scenes Physics.gravity value, this defaults to {0, -9.81, 0}
    /// </summary>
    void ExportGravity(StringBuilder sceneOutput)
    {
        sceneOutput.AppendLine();
        sceneOutput.AppendLine("\tphysics");
        sceneOutput.AppendLine("\t{");
        sceneOutput.AppendLine(string.Format("\t\tgravity = {0}, {1}, {2}",
            Physics.gravity.x, Physics.gravity.y, -Physics.gravity.z));
        sceneOutput.AppendLine("\t}");
        sceneOutput.AppendLine("}");
    }
    
    /// <summary>
    /// Writes the input to the file, overrites pre-existing file. Logs any errors
    /// and returns false if IO failed.
    /// </summary>
    private bool WriteToFile(string filePath, StringBuilder fileContents)
    {
        if(fileContents.Length > 0)
        {
            logOutput.AppendLine("Exporting " + filePath);
            
            try
            {
                if(File.Exists(filePath))
                {
                    logOutput.AppendLine("Removing pre-existing file...");
                    File.Delete(filePath);
                }
                
                fileContents.Replace("\t", "    ");
                logOutput.AppendLine("Writing...");
                File.WriteAllText(filePath, fileContents.ToString());
                logOutput.AppendLine("Done");
            }
            catch (Exception e)
            {
                logOutput.AppendLine(e.Message);
                logOutput.AppendLine(e.StackTrace);
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Sanity checks input and outputs any errors to the log. Returns false if invalid
    /// </summary>
    bool ValidateForm()
    {
        logOutput.Remove(0, logOutput.Length);
        
        if(!Directory.Exists(sceneDir))
        {
            logOutput.AppendLine(string.Format(".scene output directory '{0}' doesn't exist", sceneDir));
        }
        
        if(!Directory.Exists(physicsDir))
        {
            logOutput.AppendLine(string.Format(".physics output directory '{0}' doesn't exist", physicsDir));
        }
        
        if(string.IsNullOrEmpty(fileName))
        {
            logOutput.AppendLine("Name cannot be blank");
        }
        
        if(string.IsNullOrEmpty(physicsDirGameplay3D))
        {
            logOutput.AppendLine("Gameplay3d absolute physics dir cannot be blank");
        }
        
        if(Camera.main == null)
        {
            logOutput.AppendLine("Scene must contain a main camera");
        }
        else
        {
            if(Camera.main.orthographic)
            {
                if(viewportGameplay3D.x <= 0 || viewportGameplay3D.y <= 0)
                {
                    logOutput.AppendLine("Screen dimension must be > 0 when using an ortho camera");
                }
            }
        }
        
        return logOutput.Length == 0;
    }

    /// <summary>
    /// Defines a horizontal form with several input fields, an export button and an
    /// output log
    /// </summary>
    void OnGUI()
    {
        fileName = EditorGUILayout.TextField("Name", fileName);
        sceneDir = EditorGUILayout.TextField("Scene output dir", sceneDir);
        physicsDir = EditorGUILayout.TextField("Physics output dir", physicsDir);
        physicsDirGameplay3D = EditorGUILayout.TextField("GP3D physics dir", physicsDirGameplay3D);
        
        // Only show the dimensions field if the main camera is ortho
        if(Camera.main != null && Camera.main.orthographic)
        {
            viewportGameplay3D = EditorGUILayout.Vector2Field("GP3D screen dimensions", viewportGameplay3D);
        }

        Rect bounds = EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        
        if(GUI.Button(bounds, GUIContent.none))
        {
            if(ValidateForm())
            {
                Export();
            }
        }
        
        EditorGUILayout.LabelField ("Export");
        
        EditorGUILayout.EndHorizontal();
        
        // Only show the log when there was output
        if(logOutput.Length > 0)
        {
            EditorGUILayout.LabelField("Log:");
            EditorGUILayout.TextArea(logOutput.ToString());
        }
    }
}