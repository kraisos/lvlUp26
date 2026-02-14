using UnityEngine;

public class RandomIdleBehaviour : StateMachineBehaviour
{
    [Tooltip("How many idle states exist (0-indexed: 0 to idleCount-1)")]
    public int idleCount = 5;

    [Tooltip("Min number of loops before switching")]
    public int minLoops = 1;
    [Tooltip("Max number of loops before switching")]
    public int maxLoops = 3;

    private int loopsRemaining;
    private int lastNormalizedTime;

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        loopsRemaining = Random.Range(minLoops, maxLoops + 1);
        lastNormalizedTime = 0;
    }

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // Count completed loops
        int currentLoop = Mathf.FloorToInt(stateInfo.normalizedTime);
        if (currentLoop > lastNormalizedTime)
        {
            loopsRemaining -= (currentLoop - lastNormalizedTime);
            lastNormalizedTime = currentLoop;

            if (loopsRemaining <= 0)
            {
                // Pick a random different idle
                int current = animator.GetInteger("IdleIndex");
                int next;
                do
                {
                    next = Random.Range(0, idleCount);
                } while (next == current && idleCount > 1);

                animator.SetInteger("IdleIndex", next);
            }
        }
    }
}
