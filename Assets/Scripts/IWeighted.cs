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

/// <summary>
/// Extension methods for weighted random selection.
/// </summary>
public static class WeightedExtensions
{
    /// <summary>
    /// Picks a random item from a collection based on weights.
    /// </summary>
    public static T PickWeighted<T>(this IEnumerable<T> items) where T : IWeighted
    {
        var list = items.ToList();
        if (list.Count == 0) return default;
        if (list.Count == 1) return list[0];

        float totalWeight = list.Sum(item => item.Weight);
        float roll = Random.value * totalWeight;
        float cumulative = 0f;

        foreach (var item in list)
        {
            cumulative += item.Weight;
            if (roll <= cumulative)
            {
                return item;
            }
        }

        return list.Last();
    }
}

