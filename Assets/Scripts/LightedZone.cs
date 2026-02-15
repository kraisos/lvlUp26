using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightedZone : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireCapsuleCollider = true;
    [SerializeField] private bool ignoreTriggerColliders = true;

    [Header("Related mask sphere (optional)")]
    [SerializeField] private GameObject maskSphere;

    private static int playerCountInLightZones;
    private bool playerInsideThisZone;

    public static bool IsPlayerInAnyLightZone => playerCountInLightZones > 0;


    private void Start()
    {
        if (maskSphere != null)
        {
            transform.localScale = maskSphere.transform.localScale;
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other))
        {
            return;
        }

        if (!playerInsideThisZone)
        {
            playerInsideThisZone = true;
            playerCountInLightZones++;
        }

        KillAllMobs();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other) || !playerInsideThisZone)
        {
            return;
        }

        playerInsideThisZone = false;
        playerCountInLightZones = Mathf.Max(0, playerCountInLightZones - 1);
    }

    private void OnDisable()
    {
        if (!playerInsideThisZone)
        {
            return;
        }

        playerInsideThisZone = false;
        playerCountInLightZones = Mathf.Max(0, playerCountInLightZones - 1);
    }

    private bool IsPlayer(Collider other)
    {
        if (other == null)
        {
            return false;
        }

        if (ignoreTriggerColliders && other.isTrigger)
        {
            return false;
        }

        if (requireCapsuleCollider && other is not CapsuleCollider)
        {
            return false;
        }

        if (other.CompareTag(playerTag))
        {
            return true;
        }

        return other.GetComponentInParent<FirstPersonController>() != null;
    }

    private int KillAllMobs()
    {
        MobAI[] mobs = FindObjectsByType<MobAI>(FindObjectsSortMode.None);

        int killedCount = 0;
        for (int i = 0; i < mobs.Length; i++)
        {
            if (mobs[i] == null)
            {
                continue;
            }

            mobs[i].FadeOutAndDestroy();
            killedCount++;
        }

        return killedCount;
    }
}