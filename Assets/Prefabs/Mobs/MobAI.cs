using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MobAI : MonoBehaviour
{
    private static readonly List<MobAI> ActiveMobs = new List<MobAI>();

    [Header("Detection")]
    public float detectionRange = 15f;
    public float fieldOfViewAngle = 360f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 1.5f;

    [Header("Mob Separation")]
    public float separationRadius = 2f;
    [Tooltip("How strongly this mob pushes away from nearby mobs while moving")]
    public float separationStrength = 2f;

    [Header("NavMesh")]
    [Tooltip("How often (in seconds) the mob recalculates its path to the target")]
    public float pathUpdateInterval = 0.25f;

    [Header("Animation")]
    public string speedAnimParam = "Speed";
    [Tooltip("moveSpeed at which the animator transitions from walk to run (Speed param crosses 0.6)")]
    public float runMoveSpeed = 3f;
    [Tooltip("Multiplier applied to animator Speed parameter. 0.5 = 50% slower animation")]
    public float animationSpeedMultiplier = 0.5f;

    [Header("Sanity")]
    public float sanityDamage = 25f;
    public float sanityProximityRate = 3f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    [Header("Fade Out")]
    public float fadeDuration = .8f;

    private Transform currentTarget;
    private Animator animator;
    private NavMeshAgent agent;
    private bool isWalking;
    private Vector3 desiredMoveDirection;
    private bool shouldMove;
    private bool isFading;
    private float pathUpdateTimer;

    void Awake()
    {
        animator = GetComponent<Animator>();

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
        }

        // Disable immediately so Unity doesn't complain about not being on a NavMesh
        agent.enabled = false;

        // Sync inspector values to NavMeshAgent
        agent.speed = moveSpeed;
        agent.angularSpeed = rotationSpeed * 100f;
        agent.stoppingDistance = stoppingDistance;
        agent.autoBraking = true;
    }

    void OnEnable()
    {
        if (!ActiveMobs.Contains(this))
        {
            ActiveMobs.Add(this);
        }

        // Try to place the agent on the NavMesh then enable it
        PlaceOnNavMesh();
    }

    /// <summary>
    /// Warp the agent to the nearest NavMesh point and enable it.
    /// </summary>
    private void PlaceOnNavMesh()
    {
        if (agent == null) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
        {
            agent.enabled = true;
            agent.Warp(hit.position);
        }
        // else: agent stays disabled, Update will keep retrying
    }

    void OnDisable()
    {
        ActiveMobs.Remove(this);
    }

    void Update()
    {
        // Agent not yet placed on NavMesh — keep trying
        if (!agent.enabled)
        {
            PlaceOnNavMesh();
            return;
        }

        // If the agent fell off the NavMesh (can happen with dynamic rebakes), try to recover
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            return;
        }

        FindClosestTarget();

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (distance <= detectionRange && distance > stoppingDistance)
            {
                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    MoveTowardTarget();
                    pathUpdateTimer = pathUpdateInterval;
                }
                UpdateAnimation(true);
            }
            else
            {
                StopMoving();
                UpdateAnimation(false);
            }
        }
        else
        {
            StopMoving();
            UpdateAnimation(false);
        }
    }

    void FindClosestTarget()
    {
        if (currentTarget != null)
        {
            float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
            if (currentDistance <= detectionRange)
            {
                return;
            }

            currentTarget = null;
        }

        float closestDistance = detectionRange;

        foreach (Target target in Target.AllTargets)
        {
            if (target == null) continue;

            float distance = Vector3.Distance(transform.position, target.transform.position);

            if (distance < closestDistance)
            {
                // Optional: check field of view
                if (fieldOfViewAngle < 360f)
                {
                    Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
                    float angle = Vector3.Angle(transform.forward, directionToTarget);
                    if (angle > fieldOfViewAngle * 0.5f) continue;
                }

                closestDistance = distance;
                currentTarget = target.transform;
            }
        }
    }

    void MoveTowardTarget()
    {
        if (!agent.isOnNavMesh) return;

        // Calculate separation offset to avoid clumping with other mobs
        Vector3 separationOffset = GetSeparationDirection() * separationStrength;

        // Set the destination with a slight offset for separation
        Vector3 destination = currentTarget.position + separationOffset;

        // Make sure the offset destination is still on the NavMesh
        if (separationOffset.sqrMagnitude > 0.01f)
        {
            if (NavMesh.SamplePosition(destination, out NavMeshHit hit, separationRadius, NavMesh.AllAreas))
            {
                destination = hit.position;
            }
            else
            {
                destination = currentTarget.position; // Fallback: go straight to target
            }
        }

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    void StopMoving()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }
    }

    Vector3 GetSeparationDirection()
    {
        if (separationRadius <= 0f || ActiveMobs.Count <= 1)
        {
            return Vector3.zero;
        }

        Vector3 separation = Vector3.zero;
        int neighborCount = 0;
        float radiusSqr = separationRadius * separationRadius;

        for (int i = 0; i < ActiveMobs.Count; i++)
        {
            MobAI otherMob = ActiveMobs[i];
            if (otherMob == null || otherMob == this)
            {
                continue;
            }

            Vector3 away = transform.position - otherMob.transform.position;
            away.y = 0f;

            float distanceSqr = away.sqrMagnitude;
            if (distanceSqr < 0.0001f || distanceSqr > radiusSqr)
            {
                continue;
            }

            float distance = Mathf.Sqrt(distanceSqr);
            float weight = 1f - (distance / separationRadius);
            separation += away.normalized * weight;
            neighborCount++;
        }

        if (neighborCount == 0)
        {
            return Vector3.zero;
        }

        separation /= neighborCount;
        return separation.normalized;
    }

    void UpdateAnimation(bool moving)
    {
        isWalking = moving;

        if (animator != null)
        {
            float animSpeed = 0f;
            if (moving && agent != null)
            {
                // Use actual agent velocity for smoother animation blending
                float currentSpeed = agent.velocity.magnitude;
                animSpeed = Mathf.Clamp01(currentSpeed / runMoveSpeed);
            }
            animator.SetFloat(speedAnimParam, animSpeed);
        }
    }

    public void FadeOutAndDestroy()
    {
        if (isFading)
        {
            return;
        }

        isFading = true;
        StartCoroutine(FadeCoroutine());
    }

    private IEnumerator FadeCoroutine()
    {
        var player = FindFirstObjectByType<FirstPersonController>();
        if (player != null)
        {
            var playerColliders = player.GetComponentsInChildren<Collider>();
            var mobColliders = GetComponentsInChildren<Collider>();
            foreach (var mc in mobColliders)
            {
                foreach (var pc in playerColliders)
                {
                    Physics.IgnoreCollision(mc, pc, true);
                }
            }
        }

        var renderers = GetComponentsInChildren<Renderer>();
        var allMaterials = new List<Material>();

        foreach (var r in renderers)
        {
            allMaterials.AddRange(r.materials);
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - (elapsed / fadeDuration);

            foreach (var mat in allMaterials)
            {
                if (mat.HasProperty("_FadeOut"))
                {
                    mat.SetFloat("_FadeOut", fade);
                }
                else
                {
                    Color c = mat.color;
                    mat.color = new Color(c.r, c.g, c.b, fade);
                }
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        Vector3 pos = transform.position;

        // Detection range
        Gizmos.color = new Color(1f, 0f, 0f, 0.08f);
        Gizmos.DrawSphere(pos, detectionRange);
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(pos, detectionRange);

        // Stopping distance
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireSphere(pos, stoppingDistance);

        // Field of view cone
        if (fieldOfViewAngle < 360f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            float halfFOV = fieldOfViewAngle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -halfFOV, 0) * transform.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfFOV, 0) * transform.forward;
            Gizmos.DrawRay(pos, leftDir * detectionRange);
            Gizmos.DrawRay(pos, rightDir * detectionRange);

            // Draw arc segments
            int segments = 20;
            Vector3 prevPoint = pos + leftDir * detectionRange;
            for (int i = 1; i <= segments; i++)
            {
                float angle = -halfFOV + (fieldOfViewAngle * i / segments);
                Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
                Vector3 point = pos + dir * detectionRange;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }

        // Forward direction
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(pos, transform.forward * 2f);

        // Line to current target
        if (currentTarget != null)
        {
            float dist = Vector3.Distance(pos, currentTarget.position);
            bool inRange = dist <= detectionRange && dist > stoppingDistance;
            Gizmos.color = inRange ? Color.green : Color.yellow;
            Gizmos.DrawLine(pos, currentTarget.position);

            // Small sphere on target
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
            Gizmos.DrawWireSphere(currentTarget.position, 0.3f);

            // Draw NavMesh path
            if (Application.isPlaying && agent != null && agent.hasPath)
            {
                Gizmos.color = Color.cyan;
                Vector3[] corners = agent.path.corners;
                for (int i = 0; i < corners.Length - 1; i++)
                {
                    Gizmos.DrawLine(corners[i], corners[i + 1]);
                }
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        string state = isWalking ? "CHASING" : (currentTarget != null ? "TARGET IN RANGE" : "IDLE");
        string label = $"[{state}]";
        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);
            label += $"\nDist: {dist:F1}m";
        }
        if (animator != null && Application.isPlaying)
        {
            float animSpeed = animator.GetFloat(speedAnimParam);
            label += $"\nSpeed: {animSpeed:F2}";
        }
        if (agent != null && Application.isPlaying)
        {
            label += $"\nOnNavMesh: {agent.isOnNavMesh}";
            label += $"\nHasPath: {agent.hasPath}";
        }

        GUIStyle style = new GUIStyle();
        style.normal.textColor = isWalking ? Color.red : Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, label, style);
    }
#endif
}
