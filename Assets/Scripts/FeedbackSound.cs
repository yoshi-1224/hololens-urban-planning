using UnityEngine;

public class FeedbackSound : MonoBehaviour {
    [SerializeField]
    private AudioClip feedbackSound;
    private AudioSource audioSource;

    private void Awake () {
        EnableAudioHapticFeedback();
	}

    private void EnableAudioHapticFeedback() {
        if (feedbackSound == null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = feedbackSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.dopplerLevel = 0;
    }

    public void PlayFeedbackSound() {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }
}
