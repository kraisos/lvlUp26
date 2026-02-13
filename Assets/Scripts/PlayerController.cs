using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float jumpForce = 8.0f;
    public float groundCheckDistance = 0.1f;

    [Header("Input Settings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintMultiplier = 1.5f;

    [Header("Components")]
    public LightSource attachedLight;
    public Camera playerCamera;

    [Header("Physics")]
    public LayerMask groundMask = 1;

    private Rigidbody playerRigidbody;
    private bool isGrounded;
    private Vector3 moveDirection;

    void Start()
    {
        // Get components
        playerRigidbody = GetComponent<Rigidbody>();
        if (playerRigidbody == null)
        {
            playerRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        // Find light source if not assigned
        if (attachedLight == null)
        {
            attachedLight = GetComponentInChildren<LightSource>();
            if (attachedLight == null)
            {
                Debug.LogWarning("PlayerController: No LightSource found. Consider adding one as a child object.");
            }
        }

        // Find camera if not assigned
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                playerCamera = FindObjectOfType<Camera>();
            }
        }

        // Lock cursor for better control
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleInput();
        CheckGrounded();

        // Update light source position if attached
        UpdateLightSourcePosition();
    }

    void FixedUpdate()
    {
        ApplyMovement();
    }

    void HandleInput()
    {
        // Get movement input
        float horizontal = Input.GetAxis("Horizontal"); // A/D or Left/Right arrows
        float vertical = Input.GetAxis("Vertical");     // W/S or Up/Down arrows

        // Calculate movement direction relative to camera
        Vector3 forward = Vector3.zero;
        Vector3 right = Vector3.zero;

        if (playerCamera != null)
        {
            forward = playerCamera.transform.forward;
            right = playerCamera.transform.right;
        }
        else
        {
            // Fallback to world directions
            forward = Vector3.forward;
            right = Vector3.right;
        }

        // Keep movement on the horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate desired movement direction
        moveDirection = (forward * vertical + right * horizontal).normalized;

        // Handle jumping
        if (Input.GetKeyDown(jumpKey) && isGrounded)
        {
            Jump();
        }

        // Handle cursor lock toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCursorLock();
        }
    }

    void ApplyMovement()
    {
        // Calculate current movement speed
        float currentSpeed = moveSpeed;
        if (Input.GetKey(sprintKey))
        {
            currentSpeed *= sprintMultiplier;
        }

        // Apply horizontal movement
        Vector3 velocity = playerRigidbody.linearVelocity;
        Vector3 targetVelocity = moveDirection * currentSpeed;
        targetVelocity.y = velocity.y; // Preserve vertical velocity

        playerRigidbody.linearVelocity = Vector3.Lerp(velocity, targetVelocity, Time.fixedDeltaTime * 10f);
    }

    void Jump()
    {
        if (playerRigidbody != null)
        {
            playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    void CheckGrounded()
    {
        // Simple ground check using raycast
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;

        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out hit, groundCheckDistance + 0.1f, groundMask);

        // Visual debug for ground check
        Debug.DrawRay(rayOrigin, Vector3.down * (groundCheckDistance + 0.1f), isGrounded ? Color.green : Color.red);
    }

    void UpdateLightSourcePosition()
    {
        if (attachedLight != null)
        {
            // The light source will automatically detect position changes
            // We just need to make sure it stays with the player
            if (attachedLight.transform.parent != transform)
            {
                attachedLight.transform.SetParent(transform);
            }

            // Optional: Offset the light slightly above the player
            Vector3 lightOffset = Vector3.up * 0.5f;
            attachedLight.transform.localPosition = lightOffset;
        }
    }

    void ToggleCursorLock()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    // Public methods for external control
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
    }

    public void SetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public Vector3 GetVelocity()
    {
        return playerRigidbody != null ? playerRigidbody.linearVelocity : Vector3.zero;
    }

    public bool IsMoving()
    {
        return moveDirection.magnitude > 0.1f;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    void OnDrawGizmosSelected()
    {
        // Draw movement direction
        if (Application.isPlaying && moveDirection != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, moveDirection * 2f);
        }

        // Draw ground check
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 groundCheckStart = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(groundCheckStart, groundCheckStart + Vector3.down * (groundCheckDistance + 0.1f));
    }
}