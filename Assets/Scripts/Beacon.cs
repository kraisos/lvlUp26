using UnityEngine;

/// <summary>
/// End-goal beacon that the player must reach.
/// Place this on a GameObject with a trigger collider to detect player arrival.
/// </summary>
public class Beacon : MonoBehaviour
{
    [Header("Detection")]
    public float activationRadius = 3f;
    public string playerTag = "Player";

    [Header("Visuals")]
    public Color beaconColor = new Color(0.2f, 0.8f, 1f, 1f);
    public float pulseSpeed = 2f;
    public float lightIntensity = 2f;

    private Light beaconLight;
    private bool reached = false;

    public bool IsReached => reached;

    /// <summary>
    /// Fired when the player reaches the beacon.
    /// </summary>
    public event System.Action OnBeaconReached;

    void Start()
    {
        // Add a sphere trigger collider for player detection
        SphereCollider trigger = gameObject.GetComponent<SphereCollider>();
        if (trigger == null)
        {
            trigger = gameObject.AddComponent<SphereCollider>();
        }
        trigger.isTrigger = true;
        trigger.radius = activationRadius;

        // Add a point light as a visual beacon
        beaconLight = gameObject.GetComponentInChildren<Light>();
        if (beaconLight == null)
        {
            GameObject lightObj = new GameObject("BeaconLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 5f;
            beaconLight = lightObj.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = beaconColor;
            beaconLight.intensity = lightIntensity;
            beaconLight.range = 20f;
        }
    }

    void Update()
    {
        if (reached) return;

        // Pulse the light
        if (beaconLight != null)
        {
            float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
            beaconLight.intensity = Mathf.Lerp(lightIntensity * 0.5f, lightIntensity * 1.5f, pulse);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (reached) return;

        if (other.CompareTag(playerTag) || other.GetComponentInParent<Inventory>() != null)
        {
            reached = true;
            Debug.Log("Beacon reached! Objective complete.");
            OnBeaconReached?.Invoke();
        }
    }

    void OnDrawGizmos()
    {
        // Always show the beacon position in the editor
        Gizmos.color = beaconColor;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 10f);
    }
}
