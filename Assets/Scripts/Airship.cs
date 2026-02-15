using UnityEngine;

public class Airship : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float flyHeight = 18f;
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float rotationSpeed = 2f;

    [Header("Navigation")]
    [SerializeField] private float waypointReachDistance = 3f;
    [SerializeField] private float minWaypointDistance = 30f;
    [SerializeField] private float maxWaypointDistance = 60f;
    [Tooltip("Chance (0-1) that the next waypoint biases toward the player")]
    [SerializeField] [Range(0f, 1f)] private float playerBiasChance = 0.4f;
    [SerializeField] private float playerBiasMaxOffset = 25f;

    [Header("Bobbing Animation")]
    [SerializeField] private Transform modelTransform;
    [SerializeField] private float bobAmplitude = 0.5f;
    [SerializeField] private float bobFrequency = 0.8f;
    [SerializeField] private float swayAmplitude = 2f;
    [SerializeField] private float swayFrequency = 0.5f;

    [Header("Map Bounds")]
    [SerializeField] private float maxDistanceFromOrigin = 70f;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private Vector3 currentWaypoint;
    private Transform playerTransform;
    private float bobTime;

    void Start()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }

        Vector3 pos = transform.position;
        pos.y = flyHeight;
        transform.position = pos;

        if (modelTransform == null && transform.childCount > 0)
        {
            modelTransform = transform.GetChild(0);
        }

        PickNewWaypoint();
    }

    void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }

        NavigateToWaypoint();
        ApplyBobbingAndSway();

        Vector3 flatPos = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatWaypoint = new Vector3(currentWaypoint.x, 0f, currentWaypoint.z);

        if (Vector3.Distance(flatPos, flatWaypoint) < waypointReachDistance)
        {
            PickNewWaypoint();
        }
    }

    void NavigateToWaypoint()
    {
        Vector3 direction = currentWaypoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Vector3 moveDir = direction.normalized;

        Vector3 newPos = transform.position + moveDir * moveSpeed * Time.deltaTime;
        newPos.y = flyHeight;
        transform.position = newPos;

        Quaternion targetRotation = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation,
            rotationSpeed * Time.deltaTime);
    }

    void ApplyBobbingAndSway()
    {
        if (modelTransform == null) return;

        bobTime += Time.deltaTime;

        float yOffset = bobAmplitude * Mathf.Sin(bobTime * bobFrequency * Mathf.PI * 2f);
        float rollAngle = swayAmplitude * Mathf.Sin(bobTime * swayFrequency * Mathf.PI * 2f);

        modelTransform.localPosition = new Vector3(0f, yOffset, 0f);
        modelTransform.localRotation = Quaternion.Euler(0f, 0f, rollAngle);
    }

    void PickNewWaypoint()
    {
        Vector3 newWaypoint;

        bool biasToPlayer = playerTransform != null && Random.value < playerBiasChance;

        if (biasToPlayer)
        {
            Vector2 offset = Random.insideUnitCircle * playerBiasMaxOffset;
            newWaypoint = new Vector3(
                playerTransform.position.x + offset.x,
                flyHeight,
                playerTransform.position.z + offset.y);
        }
        else
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(minWaypointDistance, maxWaypointDistance);

            newWaypoint = transform.position + new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance,
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance);
            newWaypoint.y = flyHeight;
        }

        Vector3 fromOrigin = newWaypoint;
        fromOrigin.y = 0f;
        if (fromOrigin.magnitude > maxDistanceFromOrigin)
        {
            fromOrigin = fromOrigin.normalized * maxDistanceFromOrigin;
            newWaypoint = new Vector3(fromOrigin.x, flyHeight, fromOrigin.z);
        }

        currentWaypoint = newWaypoint;
    }

    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(currentWaypoint, 2f);
        Gizmos.DrawLine(transform.position, currentWaypoint);

        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(new Vector3(0f, flyHeight, 0f), maxDistanceFromOrigin);
    }
}
