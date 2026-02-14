using UnityEngine;

[RequireComponent(typeof(CableAnchor))]
public class Streetlight : MonoBehaviour
{
    public bool IsEnergySource { get; private set; }

    private void OnEnable()
    {
        EnsureNetwork();
        GasPipeNetwork.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (GasPipeNetwork.Instance != null)
        {
            GasPipeNetwork.Instance.Unregister(this);
        }
    }

    public void MarkAsEnergySource()
    {
        IsEnergySource = true;
    }

    private static void EnsureNetwork()
    {
        if (GasPipeNetwork.Instance != null)
        {
            return;
        }

        var go = new GameObject("GasPipeNetwork");
        go.AddComponent<GasPipeNetwork>();
    }
}
