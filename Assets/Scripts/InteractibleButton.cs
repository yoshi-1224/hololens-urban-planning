using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class InteractibleButton : MonoBehaviour, IFocusable, IInputClickHandler {
    [Tooltip("Children of this gameObject whose renderer is to be changed on events")]
    public GameObject RendererHolder;

    [Tooltip("Prefab to instantiate when this button is clicked")]
    public GameObject prefabToInstantiate;
    private GameObject instantiated;

    [Tooltip("The Colour to which RendererHolder's material should change into upon focus")]
    public Color FocusedColor;
    private Color originalColor;

    private MeshRenderer faceRenderer;

    // counts the number of building objects instantiated
    private uint objectCount = 0;

    [Tooltip("The sound to play when the button is clicked")]
    public AudioClip ClickSound;

    private AudioSource audioSource;


    private static string colorString = "_Color";

    void Start () {
        faceRenderer = RendererHolder.GetComponent<MeshRenderer>();
        originalColor = faceRenderer.material.GetColor("_Color");
        EnableAudioHapticFeedback();
	}
    
    public void InstantiatePrefab() {
        if (objectCount >= 1)
            return;
        instantiated = Instantiate(prefabToInstantiate);
        // set the parent transform of the instantiated prefab to the transform of building collection
        // AFTER it has been placed somewhere
        float distanceUpFromHub = 0.2f;
        instantiated.transform.position = GameObject.Find("EnergyHub").transform.position + new Vector3(0, distanceUpFromHub ,0);
        objectCount++;
    }

    public void OnFocusEnter() {
        faceRenderer.material.SetColor(colorString, FocusedColor);
    }

    public void OnFocusExit() {
        faceRenderer.material.SetColor(colorString, originalColor);
    }

    public void OnInputClicked(InputClickedEventData eventData) {
        playButtonClickSound();
        // if the prefab does not yet exist, then instantiate
        // else point to the prefab?
        InstantiatePrefab();
    }

#region audio-related
    /// <summary>
    /// sets up the audio feedback on this object. The clip attached will then be able to play
    /// by calling playPlacementAudio()
    /// </summary>
    private void EnableAudioHapticFeedback() {
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

    private void playButtonClickSound() {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

#endregion
}
