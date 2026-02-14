using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StoryTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private StoryTriggerType triggerType;
    [SerializeField] private bool triggerOnEnter = true;
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered;

    public void Trigger()
    {
        if (StoryAudioManager.Instance == null) return;
        StoryAudioManager.Instance.TriggerStory(triggerType);
        hasTriggered = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!triggerOnEnter) return;
        if (hasTriggered) return;

        if (other.CompareTag(playerTag) || other.GetComponentInParent<Inventory>() != null)
        {
            Trigger();
        }
    }
}
