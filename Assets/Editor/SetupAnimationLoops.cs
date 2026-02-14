using UnityEngine;
using UnityEditor;

public class SetupAnimationLoops : MonoBehaviour
{
    [MenuItem("Tools/Setup Animation Loops")]
    static void SetupLoops()
    {
        string[] fbxPaths = new string[]
        {
            "Assets/FBX/tank/idle.fbx",
            "Assets/FBX/tank/idle (2).fbx",
            "Assets/FBX/tank/idle (3).fbx",
            "Assets/FBX/tank/idle (4).fbx",
            "Assets/FBX/tank/idle (5).fbx",
            "Assets/FBX/tank/walking.fbx",
            "Assets/FBX/tank/running.fbx"
        };

        foreach (string path in fbxPaths)
        {
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"Could not find ModelImporter at {path}");
                continue;
            }

            // Get the default clip info from the FBX
            ModelImporterClipAnimation[] defaultClips = importer.defaultClipAnimations;
            if (defaultClips.Length == 0)
            {
                Debug.LogWarning($"No animation clips found in {path}");
                continue;
            }

            // Enable looping on each clip
            ModelImporterClipAnimation[] clips = new ModelImporterClipAnimation[defaultClips.Length];
            for (int i = 0; i < defaultClips.Length; i++)
            {
                clips[i] = defaultClips[i];
                clips[i].loopTime = true;
                clips[i].loopPose = true;
                Debug.Log($"Set loop on clip '{clips[i].name}' in {path} (frames {clips[i].firstFrame}-{clips[i].lastFrame})");
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();
        }

        Debug.Log("Animation loop setup complete!");
    }
}
