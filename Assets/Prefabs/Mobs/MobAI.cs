using UnityEngine;

public class MobAI : MonoBehaviour
{
    private static readonly System.Collections.Generic.List<MobAI> ActiveMobs = new System.Collections.Generic.List<MobAI>();

    [Header("Detection")]
    public float detectionRange = 15f;
    public float fieldOfViewAngle = 360f;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 5f;
    public float stoppingDistance = 1.5f;

    [Header("Physics")]
    public bool autoConfigureRigidbody = true;

    [Header("Mob Separation")]
    public float separationRadius = 2f;
    [Tooltip("How strongly this mob pushes away from nearby mobs while moving")]
    public float separationStrength = 2f;

    [Header("Ground")]
    public float groundCheckDistance = 2f;
    public LayerMask groundLayer;

    [Header("Animation")]
    public string speedAnimParam = "Speed";
    [Tooltip("moveSpeed at which the animator transitions from walk to run (Speed param crosses 0.6)")]
    public float runMoveSpeed = 3f;

    [Header("Debug")]
    public bool showDebugGizmos = true;

    private Transform currentTarget;
    private Animator animator;
    private Rigidbody rb;
    private bool isWalking;
    private Vector3 desiredMoveDirection;
    private bool shouldMove;

    void Awake()
    {
        animator = GetComponent<Animator>();

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        if (autoConfigureRigidbody && rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }

    void OnEnable()
    {
        if (!ActiveMobs.Contains(this))
        {
            ActiveMobs.Add(this);
        }
    }

    void OnDisable()
    {
        ActiveMobs.Remove(this);
    }

    void Update()
    {
        shouldMove = false;
        desiredMoveDirection = Vector3.zero;

        FindClosestTarget();

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.position);

            if (distance <= detectionRange && distance > stoppingDistance)
            {
                MoveTowardTarget();
                UpdateAnimation(true);
            }
            else
            {
                UpdateAnimation(false);
            }
        }
        else
        {
            UpdateAnimation(false);
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector3 currentVelocity = rb.linearVelocity;

        if (!shouldMove || desiredMoveDirection.sqrMagnitude < 0.001f)
        {
            rb.linearVelocity = new Vector3(0f, currentVelocity.y, 0f);
            return;
        }

        Vector3 horizontalVelocity = desiredMoveDirection * moveSpeed;
        rb.linearVelocity = new Vector3(horizontalVelocity.x, currentVelocity.y, horizontalVelocity.z);

        Quaternion targetRotation = Quaternion.LookRotation(desiredMoveDirection);
        Quaternion smoothedRotation = Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(smoothedRotation);
    }

    void FindClosestTarget()
    {
        currentTarget = null;
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
        Vector3 direction = currentTarget.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        Vector3 chaseDirection = direction.normalized;
        Vector3 separationDirection = GetSeparationDirection();
        Vector3 moveDirection = chaseDirection + separationDirection * separationStrength;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude < 0.001f)
        {
            moveDirection = chaseDirection;
        }

        moveDirection.Normalize();
        desiredMoveDirection = moveDirection;
        shouldMove = true;
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
            // Map moveSpeed to animator Speed param:
            // 0 -> 0 (idle), moveSpeed -> 0.5 (walk), runMoveSpeed -> 1.0 (run)
            float animSpeed = moving ? Mathf.Clamp01(moveSpeed / runMoveSpeed) : 0f;
            animator.SetFloat(speedAnimParam, animSpeed);
        }
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

        GUIStyle style = new GUIStyle();
        style.normal.textColor = isWalking ? Color.red : Color.white;
        style.fontStyle = FontStyle.Bold;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, label, style);
    }
#endif
}
