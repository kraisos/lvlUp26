using UnityEngine;

public enum StoryTriggerType
{
    FirstSpawn,
    IdleAtBase,
    LookAtFurnace,
    FirstExit,
    EnterBuilding,
    PickupNote,
    PlaceLamp,
    WorldRearrange,
    FirstCreatureSeen,
    CreatureKilledByLight,
    FirstChase,
    SurviveChase,
    FirstDeath,
    DeathRespawn,
    PickupEnergy,
    PickupGas,
    PickupBlueprint,
    PlaceLampRecurrent,
    LongDarkness,
    FirstDash,
    MeetLeFondeur,
    MeetLeRampant,
    MeetLaSentinelle,
    ReturnToBase,
    BossEncounter,
    ReachFurnace,
    ActivateFurnace,
    FurnaceLit,
    SkyClears,
    Epilogue,
    Custom
}

[CreateAssetMenu(fileName = "NewStoryEntry", menuName = "Audio/Story Entry")]
public class StoryEntry : ScriptableObject
{
    [Header("Identification")]
    public string entryId;
    public string voiceId;

    [Header("Audio")]
    public AudioClip audioClip;

    [Header("Trigger")]
    public StoryTriggerType triggerType;
    public bool playOnce = true;
    [Range(0, 10)]
    public int priority = 5;

    [Header("Subtitle")]
    [TextArea(3, 8)]
    public string subtitleText;
}
