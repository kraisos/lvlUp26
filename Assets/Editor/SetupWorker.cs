using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupWorker
{
    [MenuItem("Tools/Setup Worker")]
    static void Setup()
    {
        string basePath = "Assets/FBX/worker/";
        string controllerPath = basePath + "WorkerAnimatorController.controller";

        // ---- 1. Create Animator Controller ----
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        Debug.Log($"Created animator controller at {controllerPath}");

        // Add parameters (same as tank)
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsFalling", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsCrouching", AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("TurnDirection", AnimatorControllerParameterType.Float);
        controller.AddParameter("CoverAction", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("IdleIndex", AnimatorControllerParameterType.Int);

        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        // Helper: load clip from FBX
        System.Func<string, AnimationClip> loadClip = (string fbxFile) =>
        {
            string path = basePath + fbxFile;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }
            Debug.LogWarning($"No clip found in {path}");
            return null;
        };

        // ---- 2. Create all states ----
        // Main states with positions
        string[][] stateDefinitions = new string[][]
        {
            new string[] { "idle",                  "idle.fbx",                     "-90",  "600" },
            new string[] { "walking",               "walking.fbx",                  "-200", "780" },
            new string[] { "running",               "running.fbx",                  "-300", "960" },
            new string[] { "idle (2)",              "idle (2).fbx",                 "810",  "370" },
            new string[] { "idle (3)",              "idle (3).fbx",                 "840",  "430" },
            new string[] { "idle (4)",              "idle (4).fbx",                 "880",  "500" },
            new string[] { "idle (5)",              "idle (5).fbx",                 "910",  "560" },
            new string[] { "jumping up",            "jumping up.fbx",               "160",  "660" },
            new string[] { "falling idle",          "falling idle.fbx",             "200",  "900" },
            new string[] { "tank-dead",             "worker-dead.fbx",              "-140", "240" },
            new string[] { "hard landing",          "hard landing.fbx",             "410",  "390" },
            new string[] { "falling to roll",       "falling to roll.fbx",          "250",  "250" },
            new string[] { "left turn",             "left turn.fbx",                "640",  "600" },
            new string[] { "right turn",            "right turn.fbx",               "760",  "730" },
            new string[] { "run to stop",           "run to stop.fbx",              "1090", "960" },
            new string[] { "stand to cover",        "stand to cover.fbx",           "1020", "730" },
            new string[] { "stand to cover (2)",    "stand to cover (2).fbx",       "990",  "670" },
            new string[] { "cover to stand",        "cover to stand.fbx",           "200",  "0"   },
            new string[] { "cover to stand (2)",    "cover to stand (2).fbx",       "235",  "65"  },
            new string[] { "crouched sneaking left","crouched sneaking left.fbx",   "270",  "130" },
            new string[] { "crouched sneaking right","crouched sneaking right.fbx", "305",  "195" },
            new string[] { "left cover sneak",      "left cover sneak.fbx",         "655",  "845" },
            new string[] { "right cover sneak",     "right cover sneak.fbx",        "1020", "890" },
        };

        var stateMap = new System.Collections.Generic.Dictionary<string, AnimatorState>();

        foreach (string[] def in stateDefinitions)
        {
            string stateName = def[0];
            string fbxFile = def[1];
            float posX = float.Parse(def[2]);
            float posY = float.Parse(def[3]);

            AnimationClip clip = loadClip(fbxFile);
            AnimatorState state = rootSM.AddState(stateName, new Vector3(posX, posY, 0));
            state.motion = clip;
            stateMap[stateName] = state;
            Debug.Log($"Created state '{stateName}' with clip from {fbxFile}");
        }

        // Set default state to idle
        rootSM.defaultState = stateMap["idle"];

        // ---- 3. Main transitions ----
        // idle -> walking (Speed > 0.1)
        var t = stateMap["idle"].AddTransition(stateMap["walking"]);
        t.hasExitTime = false; t.duration = 0.15f; t.hasFixedDuration = true;
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        // walking -> idle (Speed < 0.1)
        t = stateMap["walking"].AddTransition(stateMap["idle"]);
        t.hasExitTime = false; t.duration = 0.15f; t.hasFixedDuration = true;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // walking -> running (Speed > 0.6)
        t = stateMap["walking"].AddTransition(stateMap["running"]);
        t.hasExitTime = false; t.duration = 0.15f; t.hasFixedDuration = true;
        t.AddCondition(AnimatorConditionMode.Greater, 0.6f, "Speed");

        // running -> walking (Speed < 0.6)
        t = stateMap["running"].AddTransition(stateMap["walking"]);
        t.hasExitTime = false; t.duration = 0.15f; t.hasFixedDuration = true;
        t.AddCondition(AnimatorConditionMode.Less, 0.6f, "Speed");

        // AnyState -> jumping up (IsJumping)
        t = rootSM.AddAnyStateTransition(stateMap["jumping up"]);
        t.hasExitTime = false; t.duration = 0.1f; t.hasFixedDuration = true;
        t.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");

        // jumping up -> falling idle (exit time)
        t = stateMap["jumping up"].AddTransition(stateMap["falling idle"]);
        t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.15f; t.hasFixedDuration = true;

        // falling idle -> idle (!IsFalling)
        t = stateMap["falling idle"].AddTransition(stateMap["idle"]);
        t.hasExitTime = false; t.duration = 0.15f; t.hasFixedDuration = true;
        t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsFalling");

        // AnyState -> dead (IsDead)
        t = rootSM.AddAnyStateTransition(stateMap["tank-dead"]);
        t.hasExitTime = false; t.duration = 0.1f; t.hasFixedDuration = true;
        t.AddCondition(AnimatorConditionMode.If, 0, "IsDead");

        // ---- 4. Idle transitions (between all 5 idles) ----
        string[] idleNames = { "idle", "idle (2)", "idle (3)", "idle (4)", "idle (5)" };
        AnimatorState[] idleStates = new AnimatorState[5];
        for (int i = 0; i < idleNames.Length; i++)
            idleStates[i] = stateMap[idleNames[i]];

        // Transitions between idles based on IdleIndex
        for (int from = 0; from < idleStates.Length; from++)
        {
            for (int to = 0; to < idleStates.Length; to++)
            {
                if (from == to) continue;
                var idleT = idleStates[from].AddTransition(idleStates[to]);
                idleT.hasExitTime = false;
                idleT.duration = 0.5f;
                idleT.hasFixedDuration = true;
                idleT.canTransitionToSelf = false;
                idleT.AddCondition(AnimatorConditionMode.Equals, to, "IdleIndex");
            }
        }

        // idle (2-5) -> walking (Speed > 0.1)
        for (int i = 1; i < idleStates.Length; i++)
        {
            var toWalk = idleStates[i].AddTransition(stateMap["walking"]);
            toWalk.hasExitTime = false;
            toWalk.duration = 0.15f;
            toWalk.hasFixedDuration = true;
            toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        }

        // ---- 5. Attach RandomIdleBehaviour to each idle ----
        for (int i = 0; i < idleStates.Length; i++)
        {
            var behaviour = idleStates[i].AddStateMachineBehaviour<RandomIdleBehaviour>();
            behaviour.idleCount = 5;
            behaviour.minLoops = 1;
            behaviour.maxLoops = 3;
        }
        Debug.Log("Attached RandomIdleBehaviour to all idle states");

        // ---- 6. Save ----
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Worker animator controller setup complete!");
    }
}
