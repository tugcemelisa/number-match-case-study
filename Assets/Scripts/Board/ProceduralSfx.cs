using UnityEngine;

// Tiny procedural sound effects (pitched sine blips/chimes) generated at
// runtime instead of shipping audio assets - no licensing to track, works
// identically in editor and build, and is more than enough for game-feel
// feedback hooks (pickup, reject, place, reveal).
public static class ProceduralSfx
{
    const int SampleRate = 44100;

    public static AudioClip CreateBlip(float startFreq, float endFreq, float duration, float volume = 0.5f)
    {
        int sampleCount = Mathf.CeilToInt(SampleRate * duration);
        var data = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)SampleRate;
            float freq = Mathf.Lerp(startFreq, endFreq, t / duration);
            float envelope = 1f - t / duration;
            data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
        }

        var clip = AudioClip.Create("Blip", sampleCount, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    public static AudioClip CreateChime(float[] frequencies, float noteDuration, float volume = 0.4f)
    {
        int samplesPerNote = Mathf.CeilToInt(SampleRate * noteDuration);
        int totalSamples = samplesPerNote * frequencies.Length;
        var data = new float[totalSamples];

        for (int n = 0; n < frequencies.Length; n++)
        {
            float freq = frequencies[n];
            for (int i = 0; i < samplesPerNote; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Sin(Mathf.PI * i / samplesPerNote);
                data[n * samplesPerNote + i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * volume;
            }
        }

        var clip = AudioClip.Create("Chime", totalSamples, 1, SampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
