using UnityEngine;
using UnityEditor;

public class DebugWorkerClips
{
    [MenuItem("Tools/Debug Worker Clips")]
    static void Debug_Clips()
    {
        string[] testPaths = {
            "Assets/FBX/worker/idle.fbx",
            "Assets/FBX/worker/walking.fbx",
            "Assets/FBX/worker/worker-dead.fbx"
        };

        foreach (string path in testPaths)
        {
            Debug.Log($"--- {path} ---");
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                Debug.Log($"  Asset: '{asset.name}' type={asset.GetType().Name} isClip={asset is AnimationClip}");
            }
        }
    }
}
