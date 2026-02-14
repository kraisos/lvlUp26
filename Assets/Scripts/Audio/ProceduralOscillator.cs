using UnityEngine;

/// <summary>
/// Low-level procedural audio oscillator that generates waveforms in real-time.
/// Supports sine, saw, square, triangle, and noise waveforms.
/// Used as a building block for the ambient music system.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProceduralOscillator : MonoBehaviour
{
    public enum WaveType { Sine, Saw, Square, Triangle, Noise }

    [Header("Oscillator Settings")]
    public WaveType waveType = WaveType.Sine;
    public float frequency = 80f;
    public float amplitude = 0.15f;
    public float targetAmplitude = 0.15f;
    public float amplitudeLerpSpeed = 1f;

    [Header("Filter")]
    public float lowPassCutoff = 2000f;
    public float lowPassResonance = 1f;

    [Header("LFO Modulation")]
    public bool useLFO = false;
    public float lfoFrequency = 0.2f;
    public float lfoDepth = 5f; // Hz deviation for frequency, or amplitude modulation
    public bool lfoModulatesFrequency = true;
    public bool lfoModulatesAmplitude = false;

    [Header("Detune (for richness)")]
    public float detuneHz = 0f;

    private float _phase;
    private float _phase2; // for detuned oscillator
    private float _lfoPhase;
    private float _noiseValue;
    private float _noiseTimer;
    private float _currentAmplitude;
    private int _sampleRate;

    // Simple one-pole low-pass filter state
    private float _filterState;

    void Awake()
    {
        _sampleRate = AudioSettings.outputSampleRate;
        _currentAmplitude = amplitude;
    }

    void Update()
    {
        // Smoothly lerp amplitude toward target
        _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, Time.deltaTime * amplitudeLerpSpeed);
        amplitude = _currentAmplitude;
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        float increment, lfoValue, freq, sample;

        for (int i = 0; i < data.Length; i += channels)
        {
            // LFO
            lfoValue = 0f;
            if (useLFO)
            {
                lfoValue = Mathf.Sin(_lfoPhase * 2f * Mathf.PI);
                _lfoPhase += lfoFrequency / _sampleRate;
                if (_lfoPhase >= 1f) _lfoPhase -= 1f;
            }

            // Effective frequency
            freq = frequency;
            if (useLFO && lfoModulatesFrequency)
                freq += lfoValue * lfoDepth;

            // Main oscillator
            sample = GenerateSample(_phase, freq);

            // Detuned oscillator for richness
            if (detuneHz != 0f)
            {
                float sample2 = GenerateSample(_phase2, freq + detuneHz);
                sample = (sample + sample2) * 0.5f;

                _phase2 += (freq + detuneHz) / _sampleRate;
                if (_phase2 >= 1f) _phase2 -= 1f;
            }

            // Phase advance
            _phase += freq / _sampleRate;
            if (_phase >= 1f) _phase -= 1f;

            // Amplitude modulation via LFO
            float amp = amplitude;
            if (useLFO && lfoModulatesAmplitude)
                amp *= Mathf.Clamp01(1f + lfoValue * 0.5f);

            sample *= amp;

            // Simple one-pole low-pass filter
            float cutoffNormalized = Mathf.Clamp01(lowPassCutoff / _sampleRate);
            float rc = 1f / (cutoffNormalized * 2f * Mathf.PI);
            float dt = 1f / _sampleRate;
            float alpha = dt / (rc + dt);
            _filterState += alpha * (sample * lowPassResonance - _filterState);
            sample = _filterState;

            // Write to all channels
            for (int c = 0; c < channels; c++)
            {
                data[i + c] += sample;
            }
        }
    }

    private float GenerateSample(float phase, float freq)
    {
        switch (waveType)
        {
            case WaveType.Sine:
                return Mathf.Sin(phase * 2f * Mathf.PI);
            case WaveType.Saw:
                return 2f * phase - 1f;
            case WaveType.Square:
                return phase < 0.5f ? 1f : -1f;
            case WaveType.Triangle:
                return 4f * Mathf.Abs(phase - 0.5f) - 1f;
            case WaveType.Noise:
                _noiseTimer += 1f / _sampleRate;
                if (_noiseTimer >= 1f / Mathf.Max(freq, 1f))
                {
                    _noiseValue = Random.Range(-1f, 1f);
                    _noiseTimer = 0f;
                }
                return _noiseValue;
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Smoothly fade to a new amplitude over time.
    /// </summary>
    public void FadeTo(float newAmplitude, float speed = 1f)
    {
        targetAmplitude = Mathf.Clamp01(newAmplitude);
        amplitudeLerpSpeed = speed;
    }

    /// <summary>
    /// Smoothly shift frequency.
    /// </summary>
    public void SetFrequencySmooth(float newFreq)
    {
        frequency = Mathf.Lerp(frequency, newFreq, Time.deltaTime * 2f);
    }
}
