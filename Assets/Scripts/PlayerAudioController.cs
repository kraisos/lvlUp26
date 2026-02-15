using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    [Header("Movement Audio")]
    public AudioClip walkingSound;
    public float walkingSoundVolume = 0.5f;
    public AudioClip runningSound;
    public float runningSoundVolume = 0.6f;

    private AudioSource movementAudioSource;
    private bool isWalking;
    private bool isSprinting;
    private bool isGrounded;

    private void Awake()
    {
        SetupMovementAudio();
    }

    private void SetupMovementAudio()
    {
        movementAudioSource = gameObject.AddComponent<AudioSource>();
        movementAudioSource.playOnAwake = false;
        movementAudioSource.loop = true;
        movementAudioSource.spatialBlend = 0f; // 2D sound
    }

    public void UpdateMovementState(bool walking, bool sprinting, bool grounded)
    {
        isWalking = walking;
        isSprinting = sprinting;
        isGrounded = grounded;
        UpdateMovementSound();
    }

    private void UpdateMovementSound()
    {
        if (movementAudioSource == null)
        {
            return;
        }

        // Determine which sound and volume to use
        AudioClip targetClip = null;
        float targetVolume = 0f;

        if (isWalking && isGrounded)
        {
            if (isSprinting && runningSound != null)
            {
                targetClip = runningSound;
                targetVolume = runningSoundVolume;
            }
            else if (walkingSound != null)
            {
                targetClip = walkingSound;
                targetVolume = walkingSoundVolume;
            }
        }

        // If we should be playing a sound
        if (targetClip != null)
        {
            // If we're already playing the correct sound, just adjust volume
            if (movementAudioSource.isPlaying && movementAudioSource.clip == targetClip)
            {
                movementAudioSource.volume = targetVolume;
            }
            // Otherwise, switch to the new sound
            else
            {
                movementAudioSource.clip = targetClip;
                movementAudioSource.volume = targetVolume;
                movementAudioSource.Play();
            }
        }
        // If we shouldn't be playing any sound, stop
        else if (movementAudioSource.isPlaying)
        {
            movementAudioSource.Stop();
        }
    }

    public void StopAllSounds()
    {
        if (movementAudioSource != null && movementAudioSource.isPlaying)
        {
            movementAudioSource.Stop();
        }
    }
}

