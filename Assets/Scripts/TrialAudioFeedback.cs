using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class TrialAudioFeedback : MonoBehaviour
{
    [Header("Success Sound")]
    [SerializeField, Range(0f, 1f)]
    private float volume = 0.5f;

    [SerializeField]
    private float duration = 0.25f;

    private AudioSource audioSource;
    private AudioClip successClip;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // 2D ses: platformun yönünü belli etmesin.
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;
        audioSource.loop = false;

        successClip = CreateSuccessClip();
    }

    public void PlaySuccess()
    {
        if (successClip == null)
            return;

        audioSource.PlayOneShot(
            successClip,
            volume);
    }

    private AudioClip CreateSuccessClip()
    {
        const int sampleRate = 44100;

        int sampleCount =
            Mathf.CeilToInt(sampleRate * duration);

        float[] samples =
            new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time =
                (float)i / sampleRate;

            // İlk yarı 660 Hz, ikinci yarı 880 Hz.
            float frequency =
                time < duration * 0.5f
                    ? 660f
                    : 880f;

            // Sesin sonunda yumuşakça sönmesi.
            float envelope =
                1f - (time / duration);

            samples[i] =
                Mathf.Sin(
                    2f * Mathf.PI *
                    frequency * time)
                * envelope;
        }

        AudioClip clip =
            AudioClip.Create(
                "VMWT_Success",
                sampleCount,
                1,
                sampleRate,
                false);

        clip.SetData(samples, 0);

        return clip;
    }
}