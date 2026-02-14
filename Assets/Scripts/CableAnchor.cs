using UnityEngine;

public class CableAnchor : MonoBehaviour
{
    private const float AnchorHeight = 0.05f;
    private const float AnchorOffset = 0.3f;

    private Transform anchorNorth;
    private Transform anchorSouth;
    private Transform anchorEast;
    private Transform anchorWest;

    public Transform AnchorNorth => anchorNorth;
    public Transform AnchorSouth => anchorSouth;
    public Transform AnchorEast => anchorEast;
    public Transform AnchorWest => anchorWest;

    private void Awake()
    {
        CreateAnchors();
    }

    private void CreateAnchors()
    {
        anchorNorth = CreateAnchorPoint("Anchor_North", new Vector3(0f, AnchorHeight, AnchorOffset));
        anchorSouth = CreateAnchorPoint("Anchor_South", new Vector3(0f, AnchorHeight, -AnchorOffset));
        anchorEast = CreateAnchorPoint("Anchor_East", new Vector3(AnchorOffset, AnchorHeight, 0f));
        anchorWest = CreateAnchorPoint("Anchor_West", new Vector3(-AnchorOffset, AnchorHeight, 0f));
    }

    private Transform CreateAnchorPoint(string anchorName, Vector3 localPosition)
    {
        var existing = transform.Find(anchorName);
        if (existing != null)
        {
            existing.localPosition = localPosition;
            return existing;
        }

        var anchor = new GameObject(anchorName);
        anchor.transform.SetParent(transform, false);
        anchor.transform.localPosition = localPosition;
        return anchor.transform;
    }

    public Transform GetAnchorToward(Vector3 worldTarget)
    {
        var localDir = transform.InverseTransformPoint(worldTarget) - new Vector3(0f, AnchorHeight, 0f);
        localDir.y = 0f;

        if (localDir.sqrMagnitude < 0.001f)
        {
            return anchorNorth;
        }

        var absX = Mathf.Abs(localDir.x);
        var absZ = Mathf.Abs(localDir.z);

        if (absZ >= absX)
        {
            return localDir.z >= 0f ? anchorNorth : anchorSouth;
        }

        return localDir.x >= 0f ? anchorEast : anchorWest;
    }

    public Transform[] GetAllAnchors()
    {
        return new[] { anchorNorth, anchorSouth, anchorEast, anchorWest };
    }
}
