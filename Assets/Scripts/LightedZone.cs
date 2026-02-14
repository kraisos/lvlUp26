using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LightedZone : MonoBehaviour
{
    [Header("Player Detection")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireCapsuleCollider = true;
    [SerializeField] private bool ignoreTriggerColliders = true;

    [Header("Behavior")]
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool triggerStoryLine = true;

    [SerializeField] private GameObject maskSphere;

    private bool hasTriggered;


    private void Start()
    {
        if (maskSphere != null)
        {
            transform.localScale = maskSphere.transform.localScale;
        }
    }

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce)
        {
            return;
        }

        Debug.Log($"LightedZone: Triggered by {other.name}");
        if (!IsPlayer(other))
        {
            return;
        }

        int killedCount = KillAllMobs();
        hasTriggered = true;

        if (triggerStoryLine && killedCount > 0 && StoryAudioManager.Instance != null)
        {
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.CreatureKilledByLight);
        }

        Debug.Log($"LightedZone triggered by {other.name}. Killed {killedCount} mob(s).");
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

            Destroy(mobs[i].gameObject);
            killedCount++;
        }

        return killedCount;
    }
}