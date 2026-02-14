using UnityEngine;
using UnityEditor;

public class FixWorkerRig
{
    [MenuItem("Tools/Fix Worker Rig")]
    static void Fix()
    {
        string basePath = "Assets/FBX/worker/";

        string[] fbxFiles = new string[]
        {
            "idle.fbx", "idle (2).fbx", "idle (3).fbx", "idle (4).fbx", "idle (5).fbx",
            "walking.fbx", "running.fbx",
            "jumping up.fbx", "falling idle.fbx", "hard landing.fbx", "falling to roll.fbx",
            "left turn.fbx", "right turn.fbx", "run to stop.fbx",
            "stand to cover.fbx", "stand to cover (2).fbx",
            "cover to stand.fbx", "cover to stand (2).fbx",
            "crouched sneaking left.fbx", "crouched sneaking right.fbx",
            "left cover sneak.fbx", "right cover sneak.fbx"
        };

        foreach (string fbxFile in fbxFiles)
        {
            string path = basePath + fbxFile;
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not find importer for {path}");
                continue;
            }

            // Force Humanoid with Create From This Model
            importer.animationType = ModelImporterAnimationType.Human;
            importer.sourceAvatar = null;
            importer.SaveAndReimport();
            Debug.Log($"Reimported {fbxFile} as Humanoid (Create From This Model)");
        }

        Debug.Log("Worker rig fix complete!");
    }
}
