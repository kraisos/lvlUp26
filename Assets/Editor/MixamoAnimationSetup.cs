using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;
using System.Linq;

public class MixamoAnimationSetup
{
    [MenuItem("Tools/Setup Mixamo Tank Animations")]
    public static void SetupAnimations()
    {
        string tankFolder = "Assets/FBX/tank";
        string mainModelPath = "Assets/FBX/tank/tank-dead.fbx";

        // Step 1: Ensure main model is Humanoid
        var mainImporter = AssetImporter.GetAtPath(mainModelPath) as ModelImporter;
        if (mainImporter == null)
        {
            Debug.LogError("Cannot find main model at: " + mainModelPath);
            return;
        }

        if (mainImporter.animationType != ModelImporterAnimationType.Human)
        {
            mainImporter.animationType = ModelImporterAnimationType.Human;
            mainImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mainImporter.SaveAndReimport();
            Debug.Log("Set tank-dead.fbx to Humanoid");
        }

        // Get the avatar from the main model
        var mainAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(mainModelPath);
        if (mainAvatar == null)
        {
            // Try to find it as sub-asset
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(mainModelPath);
            mainAvatar = allAssets.OfType<Avatar>().FirstOrDefault();
        }

        if (mainAvatar == null)
        {
            Debug.LogError("Cannot find Avatar in tank-dead.fbx. Make sure it's configured as Humanoid first.");
            return;
        }

        Debug.Log("Found avatar: " + mainAvatar.name);

        // Step 2: Configure all other FBX files to use this avatar
        string[] fbxFiles = Directory.GetFiles(tankFolder, "*.fbx", SearchOption.TopDirectoryOnly);
        int configuredCount = 0;

        foreach (string fbxPath in fbxFiles)
        {
            string assetPath = fbxPath.Replace("\\", "/");
            if (assetPath.EndsWith("tank-dead.fbx")) continue;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) continue;

            bool needsReimport = false;

            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                needsReimport = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CopyFromOther)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                needsReimport = true;
            }

            if (importer.sourceAvatar != mainAvatar)
            {
                importer.sourceAvatar = mainAvatar;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
                configuredCount++;
                Debug.Log("Configured: " + Path.GetFileName(assetPath));
            }
        }

        Debug.Log($"Configured {configuredCount} animation FBX files to use tank-dead avatar.");

        // Step 3: Create Animator Controller
        CreateAnimatorController(tankFolder);
    }

    static void CreateAnimatorController(string tankFolder)
    {
        string controllerPath = "Assets/FBX/tank/TankAnimatorController.controller";

        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var rootStateMachine = controller.layers[0].stateMachine;

        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("TurnDirection", AnimatorControllerParameterType.Float);
        controller.AddParameter("CoverAction", AnimatorControllerParameterType.Trigger);

        // Find and add animation clips from FBX files
        string[] fbxFiles = Directory.GetFiles(tankFolder, "*.fbx", SearchOption.TopDirectoryOnly);

        AnimatorState idleState = null;
        AnimatorState walkState = null;
        AnimatorState runState = null;
        AnimatorState jumpState = null;
        AnimatorState fallingState = null;
        AnimatorState deadState = null;

        foreach (string fbxPath in fbxFiles)
        {
            string assetPath = fbxPath.Replace("\\", "/");
            string fileName = Path.GetFileNameWithoutExtension(assetPath);

            // Get the animation clip from the FBX
            var clips = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToArray();

            if (clips.Length == 0) continue;

            var clip = clips[0];
            var state = rootStateMachine.AddState(fileName);
            state.motion = clip;

            // Categorize states
            string lowerName = fileName.ToLower();
            if (lowerName == "idle") idleState = state;
            else if (lowerName == "walking") walkState = state;
            else if (lowerName == "running") runState = state;
            else if (lowerName.Contains("jumping")) jumpState = state;
            else if (lowerName.Contains("falling idle")) fallingState = state;
            else if (lowerName == "tank-dead") deadState = state;
        }

        // Set default state to idle
        if (idleState != null)
        {
            rootStateMachine.defaultState = idleState;

            // Idle -> Walk (Speed > 0.1)
            if (walkState != null)
            {
                var t = idleState.AddTransition(walkState);
                t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                t.hasExitTime = false;
                t.duration = 0.15f;

                // Walk -> Idle (Speed < 0.1)
                var t2 = walkState.AddTransition(idleState);
                t2.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
                t2.hasExitTime = false;
                t2.duration = 0.15f;
            }

            // Walk -> Run (Speed > 0.6)
            if (walkState != null && runState != null)
            {
                var t = walkState.AddTransition(runState);
                t.AddCondition(AnimatorConditionMode.Greater, 0.6f, "Speed");
                t.hasExitTime = false;
                t.duration = 0.15f;

                // Run -> Walk (Speed < 0.6)
                var t2 = runState.AddTransition(walkState);
                t2.AddCondition(AnimatorConditionMode.Less, 0.6f, "Speed");
                t2.hasExitTime = false;
                t2.duration = 0.15f;
            }

            // Any -> Jump
            if (jumpState != null)
            {
                var t = rootStateMachine.AddAnyStateTransition(jumpState);
                t.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
                t.hasExitTime = false;
                t.duration = 0.1f;

                // Jump -> Falling
                if (fallingState != null)
                {
                    var t2 = jumpState.AddTransition(fallingState);
                    t2.hasExitTime = true;
                    t2.exitTime = 0.9f;
                    t2.duration = 0.15f;

                    // Falling -> Idle
                    var t3 = fallingState.AddTransition(idleState);
                    t3.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFalling");
                    t3.hasExitTime = false;
                    t3.duration = 0.15f;
                }
            }

            // Any -> Dead
            if (deadState != null)
            {
                var t = rootStateMachine.AddAnyStateTransition(deadState);
                t.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
                t.hasExitTime = false;
                t.duration = 0.1f;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Animator Controller created at: " + controllerPath);
    }
}
