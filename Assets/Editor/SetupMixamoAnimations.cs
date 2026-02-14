using UnityEngine;
using UnityEditor;
using System.IO;

public class SetupMixamoAnimations
{
    [MenuItem("Tools/Setup Mixamo Tank Animations")]
    static void SetupAnimations()
    {
        string tankFolder = "Assets/FBX/tank";
        string mainModelPath = "Assets/FBX/tank/tank-dead.fbx";

        // First ensure the main model is Humanoid with its own avatar
        ModelImporter mainImporter = AssetImporter.GetAtPath(mainModelPath) as ModelImporter;
        if (mainImporter == null)
        {
            Debug.LogError("Could not find main model at: " + mainModelPath);
            return;
        }

        if (mainImporter.animationType != ModelImporterAnimationType.Human)
        {
            mainImporter.animationType = ModelImporterAnimationType.Human;
            mainImporter.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mainImporter.SaveAndReimport();
            Debug.Log("Set tank-dead.fbx to Humanoid (Create From This Model)");
        }

        // Get the avatar from the main model
        Avatar sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(mainModelPath);
        if (sourceAvatar == null)
        {
            // Try to find it as a sub-asset
            Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(mainModelPath);
            foreach (Object asset in allAssets)
            {
                if (asset is Avatar)
                {
                    sourceAvatar = asset as Avatar;
                    break;
                }
            }
        }

        if (sourceAvatar == null)
        {
            Debug.LogError("Could not find Avatar in tank-dead.fbx. Make sure the model is correctly configured as Humanoid.");
            return;
        }

        Debug.Log("Found source avatar: " + sourceAvatar.name);

        // Configure all other FBX files to use Copy From Other Avatar
        string[] fbxFiles = Directory.GetFiles(tankFolder, "*.fbx");
        int count = 0;

        foreach (string file in fbxFiles)
        {
            string assetPath = file.Replace("\\", "/");
            if (assetPath.EndsWith("tank-dead.fbx"))
                continue;

            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null)
                continue;

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

            if (importer.sourceAvatar != sourceAvatar)
            {
                importer.sourceAvatar = sourceAvatar;
                needsReimport = true;
            }

            if (needsReimport)
            {
                importer.SaveAndReimport();
                count++;
                Debug.Log("Configured: " + Path.GetFileName(assetPath));
            }
        }

        Debug.Log("Done! Configured " + count + " animation files to use tank-dead avatar.");

        // Now create an Animator Controller
        CreateAnimatorController(tankFolder, mainModelPath);
    }

    static void CreateAnimatorController(string tankFolder, string mainModelPath)
    {
        string controllerPath = "Assets/FBX/tank/animations/TankAnimatorController.controller";

        // Ensure the animations folder exists
        if (!AssetDatabase.IsValidFolder("Assets/FBX/tank/animations"))
        {
            AssetDatabase.CreateFolder("Assets/FBX/tank", "animations");
        }

        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

        // Add parameters
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCovering", AnimatorControllerParameterType.Bool);
        controller.AddParameter("TurnDirection", AnimatorControllerParameterType.Float);

        var rootStateMachine = controller.layers[0].stateMachine;

        // Helper to find animation clip from FBX
        System.Func<string, AnimationClip> findClip = (string fbxName) =>
        {
            string path = tankFolder + "/" + fbxName + ".fbx";
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            return null;
        };

        // Create states
        var idleClip = findClip("idle");
        var idleState = rootStateMachine.AddState("Idle", new Vector3(300, 0, 0));
        if (idleClip != null) idleState.motion = idleClip;
        rootStateMachine.defaultState = idleState;

        var walkClip = findClip("walking");
        var walkState = rootStateMachine.AddState("Walking", new Vector3(600, 0, 0));
        if (walkClip != null) walkState.motion = walkClip;

        var runClip = findClip("running");
        var runState = rootStateMachine.AddState("Running", new Vector3(600, -100, 0));
        if (runClip != null) runState.motion = runClip;

        var jumpClip = findClip("jumping up");
        var jumpState = rootStateMachine.AddState("Jumping", new Vector3(300, -200, 0));
        if (jumpClip != null) jumpState.motion = jumpClip;

        var fallingClip = findClip("falling idle");
        var fallingState = rootStateMachine.AddState("Falling", new Vector3(600, -200, 0));
        if (fallingClip != null) fallingState.motion = fallingClip;

        var landClip = findClip("hard landing");
        var landState = rootStateMachine.AddState("Landing", new Vector3(600, -300, 0));
        if (landClip != null) landState.motion = landClip;

        var fallingRollClip = findClip("falling to roll");
        var fallingRollState = rootStateMachine.AddState("FallingToRoll", new Vector3(300, -300, 0));
        if (fallingRollClip != null) fallingRollState.motion = fallingRollClip;

        var runStopClip = findClip("run to stop");
        var runStopState = rootStateMachine.AddState("RunToStop", new Vector3(900, -100, 0));
        if (runStopClip != null) runStopState.motion = runStopClip;

        var deadClip = findClip("tank-dead");
        var deadState = rootStateMachine.AddState("Dead", new Vector3(300, 200, 0));
        if (deadClip != null) deadState.motion = deadClip;

        var standToCoverClip = findClip("stand to cover");
        var standToCoverState = rootStateMachine.AddState("StandToCover", new Vector3(0, -100, 0));
        if (standToCoverClip != null) standToCoverState.motion = standToCoverClip;

        var coverToStandClip = findClip("cover to stand");
        var coverToStandState = rootStateMachine.AddState("CoverToStand", new Vector3(0, -200, 0));
        if (coverToStandClip != null) coverToStandState.motion = coverToStandClip;

        var leftTurnClip = findClip("left turn");
        var leftTurnState = rootStateMachine.AddState("LeftTurn", new Vector3(900, 0, 0));
        if (leftTurnClip != null) leftTurnState.motion = leftTurnClip;

        var rightTurnClip = findClip("right turn");
        var rightTurnState = rootStateMachine.AddState("RightTurn", new Vector3(900, 100, 0));
        if (rightTurnClip != null) rightTurnState.motion = rightTurnClip;

        var sneakLeftClip = findClip("crouched sneaking left");
        var sneakLeftState = rootStateMachine.AddState("SneakLeft", new Vector3(0, -300, 0));
        if (sneakLeftClip != null) sneakLeftState.motion = sneakLeftClip;

        var sneakRightClip = findClip("crouched sneaking right");
        var sneakRightState = rootStateMachine.AddState("SneakRight", new Vector3(0, -400, 0));
        if (sneakRightClip != null) sneakRightState.motion = sneakRightClip;

        // Transitions: Idle <-> Walk <-> Run
        var idleToWalk = idleState.AddTransition(walkState);
        idleToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "Speed");
        idleToWalk.hasExitTime = false;
        idleToWalk.duration = 0.2f;

        var walkToIdle = walkState.AddTransition(idleState);
        walkToIdle.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
        walkToIdle.hasExitTime = false;
        walkToIdle.duration = 0.2f;

        var walkToRun = walkState.AddTransition(runState);
        walkToRun.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.5f, "Speed");
        walkToRun.hasExitTime = false;
        walkToRun.duration = 0.2f;

        var runToWalk = runState.AddTransition(walkState);
        runToWalk.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.5f, "Speed");
        runToWalk.hasExitTime = false;
        runToWalk.duration = 0.2f;

        // Run to stop
        var runToStop = runState.AddTransition(runStopState);
        runToStop.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "Speed");
        runToStop.hasExitTime = false;
        runToStop.duration = 0.1f;

        var stopToIdle = runStopState.AddTransition(idleState);
        stopToIdle.hasExitTime = true;
        stopToIdle.duration = 0.2f;

        // Jump
        var idleToJump = idleState.AddTransition(jumpState);
        idleToJump.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsJumping");
        idleToJump.hasExitTime = false;
        idleToJump.duration = 0.1f;

        var jumpToFall = jumpState.AddTransition(fallingState);
        jumpToFall.hasExitTime = true;
        jumpToFall.duration = 0.2f;

        var fallToLand = fallingState.AddTransition(landState);
        fallToLand.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsJumping");
        fallToLand.hasExitTime = false;
        fallToLand.duration = 0.1f;

        var landToIdle = landState.AddTransition(idleState);
        landToIdle.hasExitTime = true;
        landToIdle.duration = 0.2f;

        // Dead (any state)
        var anyToDead = rootStateMachine.AddAnyStateTransition(deadState);
        anyToDead.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsDead");
        anyToDead.hasExitTime = false;
        anyToDead.duration = 0.1f;

        // Cover
        var idleToCover = idleState.AddTransition(standToCoverState);
        idleToCover.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0, "IsCovering");
        idleToCover.hasExitTime = false;
        idleToCover.duration = 0.2f;

        var coverToStandTrans = standToCoverState.AddTransition(coverToStandState);
        coverToStandTrans.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0, "IsCovering");
        coverToStandTrans.hasExitTime = false;
        coverToStandTrans.duration = 0.2f;

        var coverStandToIdle = coverToStandState.AddTransition(idleState);
        coverStandToIdle.hasExitTime = true;
        coverStandToIdle.duration = 0.2f;

        AssetDatabase.SaveAssets();
        Debug.Log("Animator Controller created at: " + controllerPath);
    }
}
