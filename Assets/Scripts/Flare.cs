using UnityEngine;

public class Flare : MonoBehaviour
{
    [SerializeField] private GameObject maskColiderSphere;
    [SerializeField] private GameObject lightZone;
    [SerializeField] private float destroyAfterSeconds = 20f;

    private void Start()
    {
        if (maskColiderSphere == null)
        {
            maskColiderSphere = FindChildByName(transform, "MaskColiderSphere");
        }

        Invoke(nameof(DestroyMaskColliderSphere), Mathf.Max(0f, destroyAfterSeconds));
    }

    private void DestroyMaskColliderSphere()
    {
        if (maskColiderSphere == null)
        {
            return;
        }

        Destroy(maskColiderSphere);
        Destroy(lightZone);
    }

    private GameObject FindChildByName(Transform parent, string targetName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == targetName)
            {
                return child.gameObject;
            }

            GameObject nestedResult = FindChildByName(child, targetName);
            if (nestedResult != null)
            {
                return nestedResult;
            }
        }

        return null;
    }
}
