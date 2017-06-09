using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
/// <summary>
/// This handles the voice commands as well as the gesture inputs on a building.
/// </summary>
public class Interactible : MonoBehaviour, IFocusable, ISpeechHandler, IInputClickHandler {
    [Tooltip("The table object to show on show details")]
    public GameObject tablePrefab;

    [Tooltip("Sound to play upon table instantiate and destroy")]
    public AudioClip tableSound;
    private AudioSource audioSource;

    private GameObject tableObject;
    private bool isTableAlreadyExists;

    /// <summary>
    /// recognised commands. Make sure they are all in lower case
    /// </summary>
    private const string COMMAND_SHOW_DETAILS = "show details";
    private const string COMMAND_HIDE_DETAILS = "hide details";
    private const string COMMAND_MOVE = "move";
    private const string COMMAND_ROTATE = "rotate";
    
    private CustomObjectCursor cursorScriptCache;
    private Material[] defaultMaterials;

    void Start() {
        cursorScriptCache = GameObject.FindWithTag("cursor").GetComponent<CustomObjectCursor>();
        defaultMaterials = GetComponent<Renderer>().materials;
        isTableAlreadyExists = false;
        EnableAudioHapticFeedback();
    }

    void Update() {

    }

    public void OnFocusEnter() {
        cursorScriptCache.showManipulationFeedback();
        enableEmission();
    }

    public void OnFocusExit() {
        cursorScriptCache.hideManipulationFeedback();
        disableEmission();
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_SHOW_DETAILS:
                showDetails();
                break;

            case COMMAND_HIDE_DETAILS:
                hideDetails();
                break;

            case COMMAND_MOVE:
                break;
            case COMMAND_ROTATE:
                break;
            default:
                // just ignore
                break;
        }
    }


    // voice command handlers

    private void showDetails() {
        //if (isTableAlreadyExists)
        //    return;
        playTableSound();
        if (isTableAlreadyExists) {
            positionTableObject();
            return;
        }

        tableObject = Instantiate(tablePrefab);
        fillTableData();
        positionTableObject();

        isTableAlreadyExists = true;
        //playTableSound();
    }

    private void hideDetails() {
        if (!isTableAlreadyExists)
            return;

        Destroy(tableObject);
        tableObject = null;
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

    public void OnInputClicked(InputClickedEventData eventData) {
        Debug.Log("Input clicked");
    }

    private void positionTableObject() {
        float ratio = 0.4f;
        tableObject.transform.position = ratio * Camera.main.transform.position + (1 - ratio) * transform.position;
        tableObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
    }

    private void fillTableData() {
        TextMesh nameText = tableObject.transform.Find("Name").gameObject.GetComponent<TextMesh>();
        TextMesh descriptionText = tableObject.transform.Find("Desc_v").gameObject.GetComponent<TextMesh>();
        TextMesh heightText = tableObject.transform.Find("Height").gameObject.GetComponent<TextMesh>();
        TextMesh widthText = tableObject.transform.Find("Width").gameObject.GetComponent<TextMesh>();

        /// fill in the data
        //nameText.text = "";
        //descriptionText.text = "";
        //heightText.text = "";
        //widthText.text = "";
    }

    private void enableEmission() {
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].EnableKeyword("_EMISSION");
        }
    }

    private void disableEmission() {
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].DisableKeyword("_EMISSION");
        }
    }
}
