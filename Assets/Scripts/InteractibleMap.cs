using HoloToolkit.Unity.InputModule;
using UnityEngine;
using HoloToolkit.Unity;
using System.Collections;
using System;

[RequireComponent(typeof(Interpolator))]
public class InteractibleMap: Singleton<InteractibleMap>, IInputClickHandler, IFocusable {

    private float DistanceFromCamera = 2.5f;
    private Transform cameraTransform;
    private Interpolator interpolator;

    [SerializeField]
    private GameObject axisPrefab;
    private GameObject axis;

    /// <summary>
    /// Keeps track of if the user is moving the object or not.
    /// Setting this to true will enable the user to move and place the object in the scene.
    /// Useful when you want to place an object immediately.
    /// </summary>
    [Tooltip("Setting this to true will enable the user to move and place the object in the scene without needing to tap on the object. Useful when you want to place an object immediately.")]
    public bool IsBeingPlaced;

    /// added
    [Tooltip("The user guide to show when gazed at for some time")]
    [SerializeField]
    private GameObject guidePrefab;
    private GameObject guideObject;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    [SerializeField]
    private float gazeDurationTillGuideDisplay;

    public static bool shouldShowGuide {
        get {
            return GuideStatus.ShouldShowGuide;
        }
        set {
            GuideStatus.ShouldShowGuide = value;
        }
    }

    [SerializeField]
    private CustomObjectCursor cursorScript;
    private bool wasMapVisible;

    [SerializeField]
    private FeedbackSound feedbackSoundComponent;
    [SerializeField]
    Scalable scalableComponent;
    [SerializeField]
    Rotatable rotatableComponent;

    private void Start() {
        // Make sure we have all the components in the scene we need.
        interpolator = GetComponent<Interpolator>();
        interpolator.PositionPerSecond = 30f;
        
        if (scalableComponent != null) {
            scalableComponent.OnRegisteringForScaling += scalable_OnRegisteringForScaling;
            scalableComponent.OnScalingUpdated += scalable_OnScalingUpdated;
            scalableComponent.OnUnregisterForScaling += scalable_OnUnregister;
        }

        if (rotatableComponent != null) {
            rotatableComponent.OnRotationUpdated += rotatable_OnRotationUpdated;
            rotatableComponent.OnRegisteringForRotation += rotatable_OnRegisteringForRotation;
            rotatableComponent.OnUnregisterForRotation += rotatable_OnUnregisterForRotation;
        }

        //WorldAnchorManager.Instance.AttachAnchor(gameObject, "anchor");
    }

    private void Update() {
        if (IsBeingPlaced) {
            cameraTransform = Camera.main.transform;
            interpolator.SetTargetPosition(cameraTransform.position + (cameraTransform.forward * DistanceFromCamera));
            interpolator.SetTargetRotation(Quaternion.Euler(0, cameraTransform.localEulerAngles.y, 0));

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

    protected override void OnDestroy() {
        base.OnDestroy();
        if (scalableComponent != null) {
            scalableComponent.OnRegisteringForScaling -= scalable_OnRegisteringForScaling;
            scalableComponent.OnScalingUpdated -= scalable_OnScalingUpdated;
            scalableComponent.OnUnregisterForScaling -= scalable_OnUnregister;
        }
        if (rotatableComponent != null) {
            rotatableComponent.OnRotationUpdated -= rotatable_OnRotationUpdated;
            rotatableComponent.OnRegisteringForRotation -= rotatable_OnRegisteringForRotation;
            rotatableComponent.OnUnregisterForRotation -= rotatable_OnUnregisterForRotation;
        }
    }

    private bool isMapVisible() {
        if (transform.position.y - cameraTransform.position.y > 0)
            return false;
        return true;
    }

    private void showDirectionalIndicator() {
        cursorScript.TellUserToLookLower();
    }

    private void hideDirectionalIndicator() {
        cursorScript.DisableUserMessage();
    }

    public void PlacementStart() {
        //WorldAnchorManager.Instance.RemoveAnchor(gameObject);
        IsBeingPlaced = true;
        feedbackSoundComponent.PlayFeedbackSound();
        DisallowGuideObject();
        InputManager.Instance.PushModalInputHandler(gameObject);
        wasMapVisible = true; // set to true at the start
    }

    private void PlacementStop() {
        //WorldAnchorManager.Instance.AttachAnchor(gameObject, "anchor");
        IsBeingPlaced = false;
        feedbackSoundComponent.PlayFeedbackSound();
        AllowGuideObject();
        InputManager.Instance.PopModalInputHandler();
    }

#region guide-related

    IEnumerator ShowGuideCoroutine() {
        if (guideObject != null) //already exists
            yield break;
        // wait and then show
        yield return new WaitForSeconds(gazeDurationTillGuideDisplay);

        if (shouldShowGuide) { // if any user action has not been taken during the wait
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

    private void scalable_OnScalingUpdated(bool isExceedingLimit) {
        UpdateMapInfo(isExceedingLimit);
    }

    private void scalable_OnRegisteringForScaling() {
        foreach (Interactible script in GetComponentsInChildren<Interactible>()) {
            script.HideDetails();
        }
        DisallowGuideObject();
        UpdateMapInfo(false);
    }

    public void scalable_OnUnregister() {
        AllowGuideObject();
        TableDataHolder.Instance.MapScale = transform.localScale.x;
    }

    /// <summary>
    /// call this whenever the scaling for the map has been changed so that we can 
    /// update the number displayed to the user using mapInfo object
    /// </summary>
    public void UpdateMapInfo(bool isExceedingLimit) {
        // send any of its scaling component (x, y or z)
        object[] arguments = { transform.localScale.x, isExceedingLimit };
        cursorScript.UpdateCurrentScaling(arguments);
    }

#endregion

#region rotation-related
    public void rotatable_OnRegisteringForRotation() {
        Debug.Log("Rotation feedback");
        DisallowGuideObject();
        foreach (Interactible script in GetComponentsInChildren<Interactible>()) {
            script.HideDetails();
        }
        axis = Instantiate(axisPrefab, transform.position, Quaternion.identity);
    }

    public void rotatable_OnRotationUpdated() {
    }

    private void rotatable_OnUnregisterForRotation() {
        if (axis != null) {
            Destroy(axis);
            axis = null;
        }
        AllowGuideObject();
    }
    #endregion

    /// <summary>
    /// places the map upon click. Note that placement itself must be started with
    /// voice command, and when placement starts, this gameObject must be pushed
    /// to the modal stack of inputManager.
    /// </summary>
    /// <param name="eventData"></param>
    public void OnInputClicked(InputClickedEventData eventData) {
        if (!IsBeingPlaced)
            return;
        PlacementStop();
    }

    /// <summary>
    /// this is required because the cursor by itself cannot detect the change in 
    /// the object gazed (i.e. between the map and other rest)
    /// </summary>
    public void OnFocusEnter() {
        DrawingManager.Instance.ForceCursorStateChange();
    }

    public void OnFocusExit() {
        DrawingManager.Instance.ForceCursorStateChange();
    }
}
