using UnityEngine;
using HoloToolkit.Unity.InputModule;

public class ButtonBase : MonoBehaviour, IFocusable, IInputClickHandler {

    [Tooltip("Children of this gameObject whose renderer is to be changed on events")]
    public GameObject RendererHolder;
    protected MeshRenderer faceRenderer;
    private static string colorString = "_Color";

    [Tooltip("The Colour to which RendererHolder's material should change into upon focus")]
    public Color FocusedColor;
    protected Color originalColor;

    [Tooltip("The sound to play when the button is clicked")]
    public AudioClip ClickSound;
    private AudioSource audioSource;

    public virtual void OnFocusEnter() {
        faceRenderer.material.SetColor(colorString, FocusedColor);
    }

    public virtual void OnFocusExit() {
        faceRenderer.material.SetColor(colorString, originalColor);
    }

    public virtual void OnInputClicked(InputClickedEventData eventData) {
        playButtonClickSound();
    }

    protected virtual void Awake() {
        faceRenderer = RendererHolder.GetComponent<MeshRenderer>();
        originalColor = faceRenderer.material.GetColor("_Color");
        EnableAudioHapticFeedback();
    }

    #region audio-related
    /// <summary>
    /// sets up the audio feedback on this object. The clip attached will then be able to play
    /// by calling playPlacementAudio()
    /// </summary>
    protected void EnableAudioHapticFeedback() {
        if (ClickSound == null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = ClickSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.dopplerLevel = 0;
    }

    protected void playButtonClickSound() {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    #endregion
}
