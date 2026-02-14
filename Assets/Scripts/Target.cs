using System.Collections.Generic;
using UnityEngine;

public class Target : MonoBehaviour
{
    private static readonly List<Target> allTargets = new List<Target>();

    public static IReadOnlyList<Target> AllTargets => allTargets;

    void OnEnable()
    {
        allTargets.Add(this);
    }

    void OnDisable()
    {
        allTargets.Remove(this);
    }
}
