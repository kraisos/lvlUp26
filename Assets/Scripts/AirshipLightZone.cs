using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AirshipLightZone : MonoBehaviour
{
    [Header("Behavior")]
    [SerializeField] private bool triggerStoryLine = true;

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
        TryKillMob(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryKillMob(other);
    }

    private void TryKillMob(Collider other)
    {
        if (other == null || other.isTrigger) return;

        MobAI mob = other.GetComponentInParent<MobAI>();
        if (mob == null) return;

        Debug.Log($"AirshipLightZone: Killed mob {mob.name}");

        if (triggerStoryLine && StoryAudioManager.Instance != null)
        {
            StoryAudioManager.Instance.TriggerStory(StoryTriggerType.CreatureKilledByLight);
        }

        Destroy(mob.gameObject);
    }
}
