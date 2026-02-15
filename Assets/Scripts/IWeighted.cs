using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Interface for objects that have a weight value for weighted random selection.
/// </summary>
public interface IWeighted
{
    float Weight { get; }
}
