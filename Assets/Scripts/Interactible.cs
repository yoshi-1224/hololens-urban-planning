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

    [Tooltip("The user guide to show when gazed at for some time")]
    public GameObject guidePrefab;
    private GameObject guideObject;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    public float gazeDurationTillGuideDisplay;

    private float gazeStartedTime;
    private bool shouldShowGuide;

    [Tooltip("Sound to play upon table instantiate and destroy")]
    public AudioClip tableSound;

    private AudioSource audioSource;

    private GameObject tableObject;
    private bool isTableAlreadyExists;

    public float RotationSensitivity = 10f;
    public float TranslationSensitivity = 5f;

    // used for translation to get the moveVector
    private Vector3 previousManipulationPosition;

    /// <summary>
    /// recognised voice commands. Make sure they are all in lower case
    /// </summary>
    private const string COMMAND_SHOW_DETAILS = "show details";
    private const string COMMAND_HIDE_DETAILS = "hide details";
    private const string COMMAND_MOVE = "move";
    private const string COMMAND_ROTATE = "rotate";
    
    /// <summary>
    /// used for visual feedback when focus has entered/exited this gameobject.
    /// </summary>
    private Material[] defaultMaterials;

    void Start() {
        defaultMaterials = GetComponent<Renderer>().materials;
        isTableAlreadyExists = false;
        shouldShowGuide = true;
        EnableAudioHapticFeedback();
        gazeStartedTime = -1;
    }

    public void OnFocusEnter() {
        Debug.Log("Focus enter");
        gazeStartedTime = Time.unscaledTime;
        EnableEmission();
    }

    public void OnFocusExit() {
        gazeStartedTime = -1;
        DisableEmission();
        Debug.Log("Focus exit");
        hideGuideObject();
    }

    private void Update() {
        if (!shouldShowGuide || guideObject != null)
            // for any gaze session if the user is doing something
            // or the guideObject already exists then
            return;

        if (gazeStartedTime != -1) { // the user is currently gazing at this object
            if (Time.unscaledTime - gazeStartedTime >= gazeDurationTillGuideDisplay) {
                // the user has been gazing at this object for gazeDurationTillGuideDisplay
                Debug.Log("Instantiating one after time ");
                showGuideObject();
            }
        }
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_SHOW_DETAILS:
                ShowDetails();
                break;

            case COMMAND_HIDE_DETAILS:
                HideDetails();
                break;

            case COMMAND_MOVE:
                registerForTranslation();
                break;

            case COMMAND_ROTATE:
                registerForRotation();
                break;

            default:
                // just ignore
                break;
        }
    }

#region guide-related
    private void showGuideObject() {
        if (guideObject == null)
            guideObject = Instantiate(guidePrefab);
        positionGuideObject();
    }

    private void hideGuideObject() {
        if (guideObject != null)
            Destroy(guideObject);
        guideObject = null;
    }

    private void positionGuideObject() {
        float distanceRatio = 0.2f;
        guideObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        guideObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
    }

    private void resetShowStatus() {
        shouldShowGuide = true;
        gazeStartedTime = -1;
    }

#endregion

#region translation-related
    /// <summary>
    /// register this object as the one in focus for rotation
    /// </summary>
    private void registerForTranslation() {
        Debug.Log("registered for translation");
        GestureManager.Instance.RegisterGameObjectForTranslation(gameObject);
        shouldShowGuide = false;
        hideGuideObject();
    }
    
    /// <summary>
    /// This message is sent from GestureManager instance.
    /// </summary>
    public void PerformTranslationStarted(Vector3 cumulativeDelta) {
        Debug.Log("Translation starting");
        previousManipulationPosition = cumulativeDelta;
    }

    /// <summary>
    /// this message is sent from GestureManager instance.
    /// </summary>
    public void PerformTranslationUpdate(Vector3 cumulativeDelta) {
        Vector3 moveVector = Vector3.zero;
        moveVector = cumulativeDelta - previousManipulationPosition;
        previousManipulationPosition = cumulativeDelta;

        // disable the y-move as it doesn't make sense to have buildings flying around,
        // and also it makes it easier just to limit to this script for translation (vs placeable.cs)
        transform.position += new Vector3(moveVector.x * TranslationSensitivity, 0, moveVector.z * TranslationSensitivity);
    }

#endregion

#region rotation-related
    /// <summary>
    /// register this object as the one in focus for rotation
    /// </summary>
    private void registerForRotation() {
        Debug.Log("registered for rotation");
        GestureManager.Instance.RegisterGameObjectForRotation(gameObject);
        shouldShowGuide = false;
        hideGuideObject();
    }

    /// <summary>
    /// This message is sent from GestureManager instance
    /// </summary>
    /// <param name="cumulativeDelta"></param>
    public void PerformRotationUpdate(Vector3 cumulativeDelta) {
        float rotationFactor = cumulativeDelta.x * RotationSensitivity; // may be wrong by doing this.
        transform.Rotate(new Vector3(0,  rotationFactor, 0));
    }

    /// <summary>
    /// shouldShowGuide is set to true so that next time the user gaze enters the help guide
    /// will show. At the END of the user action this should be set to true.
    /// </summary>
    public void UnregisterCallBack() {
        resetShowStatus();
    }

#endregion

#region table-related
    public void ShowDetails() {
        playTableSound();
        if (isTableAlreadyExists) {
            positionTableObject();
            return;
        }

        tableObject = Instantiate(tablePrefab);
        tableObject.transform.parent = gameObject.transform;
        fillTableData();
        positionTableObject();

        // add box collider at run time so that it fits the dynamically-set text sizes
        tableObject.AddComponent<BoxCollider>();
        isTableAlreadyExists = true;

        shouldShowGuide = false;
        hideGuideObject();
    }

    public void HideDetails() {
        if (!isTableAlreadyExists)
            return;

        Destroy(tableObject);
        tableObject = null;
        isTableAlreadyExists = false;
        playTableSound();

        // at the end of user action
        resetShowStatus();
    }

    private void positionTableObject() {
        float distanceRatio = 0.4f;
        tableObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        tableObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
    }

    private void fillTableData() {
        /// use Unity's RichText format to enable diverse fonts, colours etc.
    }

#endregion

#region audio-related
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

#endregion

    public void OnInputClicked(InputClickedEventData eventData) {
        ShowDetails();
    }

#region visual feedbacks
    /// <summary>
    /// enable emission so that when this building is focused the material lights up
    /// to give the user visual feedback
    /// </summary>
    public void EnableEmission() {
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].EnableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// disable emission when gaze is exited from this building
    /// </summary>
    public void DisableEmission() {
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].DisableKeyword("_EMISSION");
        }
    }

#endregion

}
