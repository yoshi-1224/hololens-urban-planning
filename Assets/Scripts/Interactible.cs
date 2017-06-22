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

    public static bool shouldShowGuide {
        get {
            return GuideStatus.ShouldShowGuide;
        } set {
            GuideStatus.ShouldShowGuide = value;
        }
    }

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
    private const string COMMAND_SHOW_DETAILS = "show info";
    private const string COMMAND_HIDE_DETAILS = "hide info";
    private const string COMMAND_POSITION = "position";
    private const string COMMAND_ROTATE = "rotate";
    
    /// <summary>
    /// used for visual feedback when focus has entered/exited this gameobject.
    /// </summary>
    private Material[] defaultMaterials;

    void Start() {
        Renderer tempRenderer = GetComponentInChildren<Renderer>();
        if (tempRenderer != null)
            defaultMaterials = tempRenderer.materials;
        if (defaultMaterials != null) {
            for (int i = 0; i < defaultMaterials.Length; i++) {
                defaultMaterials[i].SetColor("_EmissionColor", new Color(0.1176471f, 0.1176471f, 0.1176471f));
            }
        }
        isTableAlreadyExists = false;
        EnableAudioHapticFeedback();
    }

    public void OnFocusEnter() {
        if (shouldShowGuide)
            StartCoroutine("ShowGuideCoroutine");
        EnableEmission();
    }

    public void OnFocusExit() {
        DisableEmission();
        hideGuideObject();
        StopCoroutine("ShowGuideCoroutine");
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch (eventData.RecognizedText.ToLower()) {
            case COMMAND_SHOW_DETAILS:
                ShowDetails();
                break;

            case COMMAND_HIDE_DETAILS:
                HideDetails();
                break;

            case COMMAND_POSITION:
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

    IEnumerator ShowGuideCoroutine() {
        if (guideObject != null || !shouldShowGuide) //already exists
            yield break;
        // wait and then show
        yield return new WaitForSeconds(gazeDurationTillGuideDisplay);
        if (shouldShowGuide)
            showGuideObject();
    }

    private void showGuideObject() {
        if (guideObject == null) {
            guideObject = Instantiate(guidePrefab);
            fillGuideDetails();
            guideObject.transform.parent = transform;
        }

        positionGuideObject();
    }

    private void hideGuideObject() {
        if (guideObject != null)
            Destroy(guideObject);
        guideObject = null;
    }

    private void fillGuideDetails() {
        TextMesh textMesh = guideObject.GetComponent<TextMesh>();
        textMesh.text =
            "<b>Valid commands:</b>\n" + COMMAND_SHOW_DETAILS + "\n" + 
            COMMAND_HIDE_DETAILS + "\n" + COMMAND_POSITION + "\n" + COMMAND_ROTATE;
        // should put commands in an array or dictionary as # of commands grow
        if (GetComponent<DeleteOnVoice>() != null)
            textMesh.text += "\n" + DeleteOnVoice.COMMAND_DELETE;
        textMesh.fontSize = 55;
        float scale = 0.003f;
        guideObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    private void positionGuideObject() {
        float distanceRatio = 0.2f;
        guideObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        guideObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
    }

    private void AllowGuideObject() {
        shouldShowGuide = true;
    }

    private void DisallowGuideObject() {
        shouldShowGuide = false;
        hideGuideObject();
    }

    #endregion

    #region translation-related
    /// <summary>
    /// register this object as the one in focus for rotation
    /// </summary>
    private void registerForTranslation() {
        HideDetails();
        GestureManager.Instance.RegisterGameObjectForTranslation(gameObject);
        DisallowGuideObject();
    }
    
    /// <summary>
    /// This message is sent from GestureManager instance.
    /// </summary>
    public void PerformTranslationStarted(Vector3 cumulativeDelta) {
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
        HideDetails();
        GestureManager.Instance.RegisterGameObjectForRotation(gameObject);
        DisallowGuideObject();
    }

    /// <summary>
    /// This message is sent from GestureManager instance
    /// </summary>
    /// <param name="cumulativeDelta"></param>
    public void PerformRotationUpdate(Vector3 cumulativeDelta) {
        Vector3 moveVector = Vector3.zero;
        Vector3 cumulativeDeltaInCameraSpace = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
        moveVector = cumulativeDeltaInCameraSpace - previousManipulationPosition;
        previousManipulationPosition = cumulativeDeltaInCameraSpace;

        float rotationFactor = -moveVector.x * RotationSensitivity * 20; // may be wrong by doing this.
        transform.Rotate(new Vector3(0, rotationFactor, 0));
    }

    public void PerformRotationStarted(Vector3 cumulativeDelta) {
        /// This part is VERY IMPORTANT, as cumulativeDelta is the absolute position of the hand
        /// and NOT the movement.
        previousManipulationPosition = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
    }

    /// <summary>
    /// shouldShowGuide is set to true so that next time the user gaze enters the help guide
    /// will show. At the END of the user action this should be set to true.
    /// </summary>
    public void UnregisterCallBack() {
        AllowGuideObject();
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

        //DisallowGuideObject();
        hideGuideObject();
    }

    public void HideDetails() {
        if (!isTableAlreadyExists)
            return;

        Destroy(tableObject);
        tableObject = null;
        isTableAlreadyExists = false;

        //AllowGuideObject();
    }

    private void positionTableObject() {
        float distanceRatio = 0.4f;
        tableObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * transform.position;
        tableObject.transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position, Vector3.up);
        tableObject.SendMessage("OnPositionChange");
    }

    private void fillTableData() {
        /// use Unity's RichText format to enable diverse fonts, colours etc.
        TextMesh textMesh = tableObject.GetComponent<TextMesh>();
        TableDataHolder.TableData data;
        if (TableDataHolder.Instance.dataDict.TryGetValue(gameObject.name, out data)) {
            string name = "<size=60><b>" + data.building_name + "</b></size>";
            string _class = "<b>Class</b> : " + data.building_class;
            string GPR = "<b>Gross Plot Ratio</b> : " + data.GPR;
            if (data.building_name == "Chinese Culture Centre") {
                string type = "(Prefab Type " + data.storeys_above_ground + ")";
                textMesh.text = name + "\n" + type + "\n\n" + _class + "\n" + GPR;
                return;
            }
            string measured_height = "<b>Measured Height</b> : " + data.measured_height + "m";
            string numStoreys = "<b>Number of Storeys</b> : " + data.storeys_above_ground;
            textMesh.text = name + "\n\n" + _class + "\n" + GPR + "\n" + measured_height + "\n" + numStoreys;
        } else {
            textMesh.text = "status unknown";
        }
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
        if (defaultMaterials == null)
            return;
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].EnableKeyword("_EMISSION");
        }
    }

    /// <summary>
    /// disable emission when gaze is exited from this building
    /// </summary>
    public void DisableEmission() {
        if (defaultMaterials == null)
            return;
        for (int i = 0; i < defaultMaterials.Length; i++) {
            defaultMaterials[i].DisableKeyword("_EMISSION");
        }
    }

#endregion


}
