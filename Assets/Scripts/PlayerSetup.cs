using UnityEngine;

public class PlayerSetup : MonoBehaviour
{
    [Header("Auto-Setup Components")]
    public bool autoSetupOnStart = true;
    public bool createVisualRepresentation = true;

    [Header("Visual Settings")]
    public Color playerColor = Color.blue;
    public Vector3 playerSize = new Vector3(0.8f, 1.8f, 0.8f);

    [Header("Light Settings")]
    public Color defaultLightColor = Color.yellow;
    public float defaultLightRadius = 5.0f;
    public Vector3 lightOffset = new Vector3(0, 0.5f, 0);

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupPlayer();
        }
    }

    [ContextMenu("Setup Player")]
    public void SetupPlayer()
    {
        // Add Rigidbody if missing
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Prevent tumbling
        }

        // Add Collider if missing
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            CapsuleCollider capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = playerSize.y;
            capsule.radius = playerSize.x * 0.5f;
            capsule.center = Vector3.up * playerSize.y * 0.5f;
        }

        // Add PlayerController if missing
        PlayerController playerController = GetComponent<PlayerController>();
        if (playerController == null)
        {
            playerController = gameObject.AddComponent<PlayerController>();
        }

        // Setup light source
        SetupLightSource(playerController);

        // Create visual representation
        if (createVisualRepresentation)
        {
            CreatePlayerVisuals();
        }

        Debug.Log($"Player setup complete: {gameObject.name}");
    }

    void SetupLightSource(PlayerController controller)
    {
        // Look for existing light source
        LightSource existingLight = GetComponentInChildren<LightSource>();

        if (existingLight == null)
        {
            // Create new light source GameObject
            GameObject lightObject = new GameObject("Light Source");
            lightObject.transform.SetParent(transform);
            lightObject.transform.localPosition = lightOffset;

            // Add LightSource component
            LightSource lightSource = lightObject.AddComponent<LightSource>();
            lightSource.lightRadius = defaultLightRadius;

            // Link to player controller
            controller.attachedLight = lightSource;

            // Add a small visual indicator for the light
            GameObject lightIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lightIndicator.name = "Light Indicator";
            lightIndicator.transform.SetParent(lightObject.transform);
            lightIndicator.transform.localPosition = Vector3.zero;
            lightIndicator.transform.localScale = Vector3.one * 0.2f;

            // Make it glow
            Renderer lightRenderer = lightIndicator.GetComponent<Renderer>();
            Material lightMat = new Material(Shader.Find("Standard"));
            lightMat.color = defaultLightColor;
            lightMat.EnableKeyword("_EMISSION");
            lightMat.SetColor("_EmissionColor", defaultLightColor);
            lightRenderer.material = lightMat;

            // Remove collider from light indicator
            Collider lightCol = lightIndicator.GetComponent<Collider>();
            if (lightCol != null)
            {
                DestroyImmediate(lightCol);
            }
        }
        else
        {
            controller.attachedLight = existingLight;
        }
    }

    void CreatePlayerVisuals()
    {
        // Check if visuals already exist
        Transform visualsParent = transform.Find("PlayerVisuals");
        if (visualsParent != null)
        {
            return; // Visuals already created
        }

        // Create visual parent
        GameObject visuals = new GameObject("PlayerVisuals");
        visuals.transform.SetParent(transform);
        visuals.transform.localPosition = Vector3.zero;

        // Create body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(visuals.transform);
        body.transform.localPosition = Vector3.up * playerSize.y * 0.5f;
        body.transform.localScale = new Vector3(playerSize.x, playerSize.y * 0.5f, playerSize.z);

        // Style the body
        Renderer bodyRenderer = body.GetComponent<Renderer>();
        Material bodyMat = new Material(Shader.Find("Standard"));
        bodyMat.color = playerColor;
        bodyRenderer.material = bodyMat;

        // Remove collider from visual (we have one on the main GameObject)
        Collider bodyCol = body.GetComponent<Collider>();
        if (bodyCol != null)
        {
            DestroyImmediate(bodyCol);
        }

        // Create simple "head"
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(visuals.transform);
        head.transform.localPosition = Vector3.up * (playerSize.y + 0.2f);
        head.transform.localScale = Vector3.one * 0.4f;

        // Style the head
        Renderer headRenderer = head.GetComponent<Renderer>();
        Material headMat = new Material(Shader.Find("Standard"));
        headMat.color = playerColor * 0.8f; // Slightly darker
        headRenderer.material = headMat;

        // Remove collider from head
        Collider headCol = head.GetComponent<Collider>();
        if (headCol != null)
        {
            DestroyImmediate(headCol);
        }
    }

    [ContextMenu("Remove Setup")]
    public void RemoveSetup()
    {
        // Remove added components (be careful to only remove what we added)
        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
        {
            DestroyImmediate(pc);
        }

        // Remove light source
        LightSource light = GetComponentInChildren<LightSource>();
        if (light != null)
        {
            DestroyImmediate(light.gameObject);
        }

        // Remove visuals
        Transform visuals = transform.Find("PlayerVisuals");
        if (visuals != null)
        {
            DestroyImmediate(visuals.gameObject);
        }

        Debug.Log($"Player setup removed from: {gameObject.name}");
    }
}