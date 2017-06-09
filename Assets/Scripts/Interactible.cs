using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class Interactible : MonoBehaviour, IFocusable, ISpeechHandler {
    [Tooltip("The table object to show on show details")]
    public GameObject tablePrefab;

    [Tooltip("Sound to play upon table instantiate and destroy")]
    public AudioClip tableSound;
    private AudioSource audioSource;

    private GameObject instantiatedTable;
    private bool isTableAlreadyExists;

    /// <summary>
    /// recognised commands. Make sure they are all in lower case
    /// </summary>
    private const string showDetailsCommand = "show details";
    private const string hideDetailsCommand = "hide details";
    
    private CustomObjectCursor cursorScriptCache;

    void Start() {
        cursorScriptCache = GameObject.FindWithTag("cursor").GetComponent<CustomObjectCursor>();
        isTableAlreadyExists = false;
        EnableAudioHapticFeedback();
    }

    void Update() {

    }

    public void OnFocusEnter() {
        cursorScriptCache.showManipulationFeedback();
    }

    public void OnFocusExit() {
        cursorScriptCache.hideManipulationFeedback();
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case showDetailsCommand:
                showDetails();
                break;

            case hideDetailsCommand:
                hideDetails();
                break;

            default:
                // just ignore
                break;
        }
    }


    // voice command handlers

    private void showDetails() {
        Debug.Log("show details called");
        if (isTableAlreadyExists)
            return;

        instantiatedTable = Instantiate(tablePrefab);
        isTableAlreadyExists = true;
        playTableSound();
    }

    private void hideDetails() {
        Debug.Log("hide details called");
        if (!isTableAlreadyExists)
            return;

        Destroy(instantiatedTable);
        instantiatedTable = null;
        isTableAlreadyExists = false;
        playTableSound();
    }

    private void EnableAudioHapticFeedback() {
        if (tableSound == null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = tableSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.dopplerLevel = 0;
    }

    private void playTableSound() {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }
}
