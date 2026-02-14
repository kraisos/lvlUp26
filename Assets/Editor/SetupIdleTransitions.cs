using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class SetupIdleTransitions
{
    [MenuItem("Tools/Setup Idle Transitions")]
    static void Setup()
    {
        string controllerPath = "Assets/FBX/tank/TankAnimatorController.controller";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"Could not load animator controller at {controllerPath}");
            return;
        }

        // Add IdleIndex parameter if it doesn't exist
        bool hasIdleIndex = false;
        foreach (var param in controller.parameters)
        {
            if (param.name == "IdleIndex")
            {
                hasIdleIndex = true;
                break;
            }
        }
        if (!hasIdleIndex)
        {
            controller.AddParameter("IdleIndex", AnimatorControllerParameterType.Int);
            Debug.Log("Added IdleIndex parameter");
        }

        // Get the base layer state machine
        AnimatorStateMachine rootSM = controller.layers[0].stateMachine;

        // Find the 5 idle states
        string[] idleNames = { "idle", "idle (2)", "idle (3)", "idle (4)", "idle (5)" };
        AnimatorState[] idleStates = new AnimatorState[5];

        foreach (var childState in rootSM.states)
        {
            for (int i = 0; i < idleNames.Length; i++)
            {
                if (childState.state.name == idleNames[i])
                {
                    idleStates[i] = childState.state;
                    Debug.Log($"Found state: {idleNames[i]}");
                }
            }
        }

        // Verify all found
        for (int i = 0; i < idleStates.Length; i++)
        {
            if (idleStates[i] == null)
            {
                Debug.LogError($"Could not find state '{idleNames[i]}'");
                return;
            }
        }

        // Remove any existing transitions between idle states to avoid duplicates
        for (int i = 0; i < idleStates.Length; i++)
        {
            var transitions = idleStates[i].transitions;
            var keepList = new System.Collections.Generic.List<AnimatorStateTransition>();
            foreach (var t in transitions)
            {
                bool isIdleTransition = false;
                for (int j = 0; j < idleStates.Length; j++)
                {
                    if (j != i && t.destinationState == idleStates[j])
                    {
                        isIdleTransition = true;
                        break;
                    }
                }
                if (!isIdleTransition)
                {
                    keepList.Add(t);
                }
            }
            idleStates[i].transitions = keepList.ToArray();
        }

        // Add transitions from each idle to every other idle
        for (int from = 0; from < idleStates.Length; from++)
        {
            for (int to = 0; to < idleStates.Length; to++)
            {
                if (from == to) continue;

                AnimatorStateTransition transition = idleStates[from].AddTransition(idleStates[to]);
                transition.hasExitTime = false;
                transition.exitTime = 0.9f;
                transition.duration = 0.5f;
                transition.hasFixedDuration = true;
                transition.canTransitionToSelf = false;

                // Condition: IdleIndex == to
                transition.AddCondition(AnimatorConditionMode.Equals, to, "IdleIndex");
            }
        }
        Debug.Log("Added transitions between all idle states");

        // Attach RandomIdleBehaviour to each idle state
        // First get the script type
        var behaviourType = typeof(RandomIdleBehaviour);

        for (int i = 0; i < idleStates.Length; i++)
        {
            // Check if behaviour already attached
            bool alreadyHas = false;
            foreach (var b in idleStates[i].behaviours)
            {
                if (b is RandomIdleBehaviour)
                {
                    alreadyHas = true;
                    break;
                }
            }

            if (!alreadyHas)
            {
                var behaviour = idleStates[i].AddStateMachineBehaviour<RandomIdleBehaviour>();
                behaviour.idleCount = 5;
                behaviour.minLoops = 1;
                behaviour.maxLoops = 3;
                Debug.Log($"Attached RandomIdleBehaviour to '{idleNames[i]}'");
            }
        }

        // Find the walking state so idle (2-5) can transition to it
        AnimatorState walkingState = null;
        foreach (var childState in rootSM.states)
        {
            if (childState.state.name == "walking")
            {
                walkingState = childState.state;
                break;
            }
        }

        if (walkingState != null)
        {
            // Add Speed > 0.1 -> walking transition to idle (2-5)
            // idle (0) already has this transition, skip it
            for (int i = 1; i < idleStates.Length; i++)
            {
                // Check if transition to walking already exists
                bool hasWalkingTransition = false;
                foreach (var t in idleStates[i].transitions)
                {
                    if (t.destinationState == walkingState)
                    {
                        hasWalkingTransition = true;
                        break;
                    }
                }

                if (!hasWalkingTransition)
                {
                    AnimatorStateTransition toWalking = idleStates[i].AddTransition(walkingState);
                    toWalking.hasExitTime = false;
                    toWalking.duration = 0.15f;
                    toWalking.hasFixedDuration = true;
                    toWalking.canTransitionToSelf = false;
                    toWalking.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                    Debug.Log($"Added '{idleNames[i]}' -> walking (Speed > 0.1)");
                }
            }
        }
        else
        {
            Debug.LogWarning("Could not find 'walking' state!");
        }

        // Save
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("Idle transitions setup complete!");
    }
}
