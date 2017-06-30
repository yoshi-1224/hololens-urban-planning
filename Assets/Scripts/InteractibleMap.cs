using HoloToolkit.Unity.InputModule;
using UnityEngine;
using HoloToolkit.Unity;
using System;
using System.Collections.Generic;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(Interpolator))]
public class InteractibleMap: Singleton<InteractibleMap>, IInputClickHandler, IFocusable {

    private float Distance = 2.5f;
    private Transform cameraTransform;
    private Interpolator interpolator;
    private float RotationSensitivity = 10f;
    public GameObject axisPrefab;
    private GameObject axis;
    // used for translation to get the moveVector

    public bool IsDrawing {
        get; set;
    }

    /// <summary>
    /// Keeps track of if the user is moving the object or not.
    /// Setting this to true will enable the user to move and place the object in the scene.
    /// Useful when you want to place an object immediately.
    /// </summary>
    [Tooltip("Setting this to true will enable the user to move and place the object in the scene without needing to tap on the object. Useful when you want to place an object immediately.")]
    public bool IsBeingPlaced;

    /// added
    [Tooltip("The user guide to show when gazed at for some time")]
    public GameObject guidePrefab;
    private GameObject guideObject;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    public float gazeDurationTillGuideDisplay;

    public static bool shouldShowGuide {
        get {
            return GuideStatus.ShouldShowGuide;
        }
        set {
            GuideStatus.ShouldShowGuide = value;
        }
    }

    [Tooltip("The child object(s) to hide during placement.")]
    public List<GameObject> ChildrenToHide = new List<GameObject>();

    [Tooltip("The sound to play when the map is placed")]
    public AudioClip PlacementSound;

    [Tooltip("scaling sensitivity when the map is being scaled")]
    public float ScalingSensitivity = 0.0002f;

    private AudioSource audioSource;
    private GameObject cursor;

    private Material[] defaultMaterials;
    private bool wasMapVisible;

    private void Start() {
        // Make sure we have all the components in the scene we need.
        interpolator = GetComponent<Interpolator>();
        interpolator.PositionPerSecond = 30f;
        defaultMaterials = GetComponent<Renderer>().materials;
        EnableAudioHapticFeedback();
        TableDataHolder.Instance.MapScale = transform.localScale.x;
    }

    private void Update() {
        if (IsBeingPlaced) {
            cameraTransform = Camera.main.transform;
            interpolator.SetTargetPosition(cameraTransform.position + (cameraTransform.forward * Distance));
            interpolator.SetTargetRotation(Quaternion.Euler(0, cameraTransform.localEulerAngles.y -180, 0));

            /// whether or not to tell user that they should look lower
            bool isMapVisibleNow = isMapVisible();
            if (isMapVisibleNow != wasMapVisible) { // if state has changed
                if (!isMapVisibleNow)
                    showDirectionalIndicator();
                else
                    hideDirectionalIndicator();
            }
            wasMapVisible = isMapVisibleNow;
        }
    }

    private bool isMapVisible() {
        if (transform.position.y - cameraTransform.position.y > 0)
            return false;
        return true;
    }

    private void showDirectionalIndicator() {
        if (cursor == null)
            cursor = GameObject.Find("CustomCursorWithFeedback");
        cursor.SendMessage("TellUserToLookLower", "Look lower");
    }

    private void hideDirectionalIndicator() {
        if (cursor == null)
            cursor = GameObject.Find("CustomCursorWithFeedback");
        cursor.SendMessage("DisableUserMessage");
    }

    public virtual void OnInputClicked(InputClickedEventData eventData) {
        IsBeingPlaced = !IsBeingPlaced;
        if (IsBeingPlaced) {
            OnPlacementStart();
        } else {
            OnPlacementStop();
        }
    }

    private void OnPlacementStart() {
        playPlacementAudio();
        MakeSiblingsChildren();
        HideChildren();
        DisallowGuideObject();
        wasMapVisible = true; // set to true at the start
    }

    private void OnPlacementStop() {
        playPlacementAudio();
        MakeChildrenSiblings();
        ShowChildren();
        AllowGuideObject();
    }


#region audio-related
    /// <summary>
    /// sets up the audio feedback on this object. The clip attached will then be able to play
    /// by calling playPlacementAudio()
    /// </summary>
    private void EnableAudioHapticFeedback() {
        if (PlacementSound == null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = PlacementSound;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1;
        audioSource.dopplerLevel = 0;
    }

    private void playPlacementAudio() {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    #endregion

#region transform-related
    /// <summary>
    /// This should be called right before the placing starts so that the buildings follow
    /// the transform of map
    /// </summary>
    public void MakeSiblingsChildren() {
        foreach (GameObject child in ChildrenToHide) {
            child.transform.parent = transform;
        }
    }

    /// <summary>
    /// This should be called right after the placing ends so that the buildings become 
    /// independent from the map and can receive their own select event handlers
    /// </summary>
    public void MakeChildrenSiblings() {
        foreach (GameObject sibling in ChildrenToHide) {
            sibling.transform.parent = transform.parent;
        }
    }

    public void HideChildren() {
        for (int i = 0; i < ChildrenToHide.Count; i++) {
            ChildrenToHide[i].SetActive(false);
        }
    }

    public void ShowChildren() {
        for (int i = 0; i < ChildrenToHide.Count; i++) {
            ChildrenToHide[i].SetActive(true);
        }
    }

#endregion

    public void OnFocusEnter() {
        if (shouldShowGuide)
            StartCoroutine("ShowGuideCoroutine");
        if (IsDrawing) {
            if (cursor == null)
                cursor = GameObject.Find("CustomCursorWithFeedback");
            cursor.SendMessage("OnMapFocused");
        }
        EnableEmission();
    }

    public void OnFocusExit() {
        DisableEmission();
        HideGuideObject();
        StopCoroutine("ShowGuideCoroutine");
        if (IsDrawing) {
            if (cursor == null)
                cursor = GameObject.Find("CustomCursorWithFeedback");
            cursor.SendMessage("OnMapFocusExit");
        }
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

#region guide-related

    IEnumerator ShowGuideCoroutine() {
        if (guideObject != null) //already exists
            yield break;
        // wait and then show
        yield return new WaitForSeconds(gazeDurationTillGuideDisplay);

        if (shouldShowGuide) { // if any user action has taken during the wait
            showGuideObject();
        }
    }

    private void showGuideObject() {
        if (guideObject == null)
            guideObject = Instantiate(guidePrefab);
        fillGuideDetails();
        positionGuideObject();
    }

    private void fillGuideDetails() {
        TextMesh textMesh = guideObject.GetComponent<TextMesh>();
        textMesh.text =
            "<b>Valid commands:</b>\nRotate Map\nScale Map\nMove Map\nStreet View";
        textMesh.fontSize = 52;
        float scale = 0.005f;
        guideObject.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void HideGuideObject() {
        if (guideObject != null)
            Destroy(guideObject);
        guideObject = null;
    }

    private void positionGuideObject() {
        float distanceRatio = 0.2f;
        guideObject.transform.position = distanceRatio * Camera.main.transform.position + (1 - distanceRatio) * GazeManager.Instance.HitPosition;
        guideObject.transform.rotation = Quaternion.LookRotation(GazeManager.Instance.HitPosition - Camera.main.transform.position, Vector3.up);
    }

    private void AllowGuideObject() {
        shouldShowGuide = true;
    }
    
    private void DisallowGuideObject() {
        shouldShowGuide = false;
        HideGuideObject();
    }

#endregion

#region scaling-related
    public void PerformScalingStarted(Vector3 cumulativeDelta) {
        if (!IsBeingPlaced)
            MakeSiblingsChildren();
        foreach (Interactible script in GetComponentsInChildren<Interactible>()) {
            script.HideDetails();
        }
    }

    public void PerformScalingUpdate(Vector3 normalizedOffset) {
        float yMovement = normalizedOffset.y;
        float scalingFactor = yMovement * ScalingSensitivity;
        float minimumScale = 0.0005f;
        float maximumScale = 0.005f;
        float currentScale = transform.localScale.x;
        bool notifyUser = true;
        if (currentScale + scalingFactor > maximumScale) {
            transform.localScale = new Vector3(maximumScale, maximumScale, maximumScale);
        } else if (currentScale + scalingFactor < minimumScale) {
            transform.localScale = new Vector3(minimumScale, minimumScale, minimumScale);
        } else {
            transform.localScale += new Vector3(scalingFactor, scalingFactor, scalingFactor);
            notifyUser = false;
        }

        UpdateMapInfo(notifyUser);
    }

    public void RegisterForScaling() {
        DisallowGuideObject();
        GestureManager.Instance.RegisterGameObjectForScalingUsingNavigation(gameObject);
        UpdateMapInfo(false);
    }

    /// <summary>
    /// call this whenever the scaling for the map has been changed so that we can 
    /// update the number displayed to the user using mapInfo object
    /// </summary>
    public void UpdateMapInfo(bool isExceedingLimit) {
        if (cursor == null)
            cursor = GameObject.Find("CustomCursorWithFeedback");
        // send any of its scaling component (x, y or z)
        object[] arguments = { transform.localScale.x, isExceedingLimit };
        cursor.SendMessage("UpdateCurrentScaling", arguments);
    }

#endregion

#region rotation-related
    public void RegisterForRotation() {
        DisallowGuideObject();
        if (!IsBeingPlaced)
            MakeSiblingsChildren();
        GestureManager.Instance.RegisterGameObjectForRotation(gameObject);
        axis = Instantiate(axisPrefab);
        axis.transform.position = transform.position;
    }

    private void PerformRotationStarted(Vector3 cumulativeDelta) {
        foreach (Interactible script in GetComponentsInChildren<Interactible>()) {
            script.HideDetails();
        }
    }

    /// <summary>
    /// This message is sent from GestureManager instance
    /// </summary>
    public void PerformRotationUpdate(Vector3 normalizedOffset) {
        float rotationFactor = -normalizedOffset.x * RotationSensitivity; // may be wrong by doing this.
        transform.Rotate(new Vector3(0, rotationFactor, 0));
    }

    #endregion
    /// <summary>
    /// called when this object is done with receiving manipulation events.
    /// </summary>
    public void UnregisterCallBack() {
        AllowGuideObject();
        if (!IsBeingPlaced)
            MakeChildrenSiblings();
        TableDataHolder.Instance.MapScale = transform.localScale.x;
        if (axis != null) {
            Destroy(axis);
            axis = null;
        }
    }

}
