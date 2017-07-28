using HoloToolkit.Unity.InputModule;
using UnityEngine;
using HoloToolkit.Unity;
using System.Collections;
using System;
using System.Text;

[RequireComponent(typeof(Interpolator))]
public class InteractibleMap: Singleton<InteractibleMap>, IInputClickHandler, IFocusable {

    private float DistanceFromCamera = 2.5f;
    private Transform cameraTransform;
    private Interpolator interpolator;

    [SerializeField]
    private GameObject axisPrefab;
    private GameObject axis;

    [SerializeField]
    private GameObject mapTools;

    /// <summary>
    /// Keeps track of if the user is moving the object or not.
    /// Setting this to true will enable the user to move and place the object in the scene.
    /// Useful when you want to place an object immediately.
    /// </summary>
    [Tooltip("Setting this to true will enable the user to move and place the object in the scene without needing to tap on the object. Useful when you want to place an object immediately.")]
    public bool IsBeingPlaced;

    private GameObject guideObject;

    [Tooltip("The duration in seconds for which user should gaze the object at to see the guide")]
    [SerializeField]
    private float gazeDurationTillGuideDisplay;

    [SerializeField]
    private CustomObjectCursor cursorScript;
    private bool wasMapVisible;

    [SerializeField]
    private FeedbackSound feedbackSoundComponent;
    [SerializeField]
    Scalable scalableComponent;
    [SerializeField]
    Rotatable rotatableComponent;
    private bool isThisObjectShowingGuide;
    private object guideObjectInstance;

    public event Action<bool> OnBeforeUserActionOnMap = delegate { };
    public event Action OnAfterUserActionOnMap = delegate { };

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

        isThisObjectShowingGuide = false;
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
        cursorScript.DisableLookLowerMessage();
    }

    public void PlacementStart() {
        IsBeingPlaced = true;
        hideMapTools();
        HideTablesAndObjects();
        feedbackSoundComponent.PlayFeedbackSound();
        GuideStatus.GuideObjectInstance.SetActive(false);
        GuideStatus.ShouldShowGuide = false;
        InputManager.Instance.PushModalInputHandler(gameObject);
        wasMapVisible = true; // set to true at the start
    }

    private void PlacementStop() {
        hideDirectionalIndicator();
        showMapTools();
        OnAfterUserActionOnMap.Invoke();
        IsBeingPlaced = false;
        feedbackSoundComponent.PlayFeedbackSound();
        AllowGuideObject();
        InputManager.Instance.PopModalInputHandler();
        GuideStatus.ShouldShowGuide = true;
    }

#region scaling-related

    private void scalable_OnScalingUpdated(bool isExceedingLimit) {
        UpdateMapScaleInfo(isExceedingLimit);
    }

    private void scalable_OnRegisteringForScaling() {
        HideTablesAndObjects();
        DisallowGuideObject();
        UpdateMapScaleInfo(false);
    }

    public void HideTablesAndObjects() {
        OnBeforeUserActionOnMap.Invoke(true);
    }

    public void HideAllTables() {
        OnBeforeUserActionOnMap.Invoke(false);
    }

    public void scalable_OnUnregister() {
        OnAfterUserActionOnMap.Invoke();
        AllowGuideObject();
    }

    /// <summary>
    /// call this whenever the scaling for the map has been changed so that we can 
    /// update the number displayed to the user using mapInfo object
    /// </summary>
    public void UpdateMapScaleInfo(bool isExceedingLimit) {
        // send any of its scaling component (x, y or z)
        float currentScale = scalableComponent.gameObject.transform.localScale.x;
        string text = string.Format("Current Scale: " + Utils.FormatNumberInDecimalPlace(currentScale, 4));
        Color messageColor = isExceedingLimit ? Color.red : Color.black;
        ScreenMessageManager.Instance.DisplayMessage(text, messageColor);
    }

#endregion

#region rotation-related
    public void rotatable_OnRegisteringForRotation() {
        DisallowGuideObject();
        HideAllTables();
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
        if (isMapVisible())
            PlacementStop();
    }

    /// <summary>
    /// this is required because the cursor by itself cannot detect the change in 
    /// the object gazed (i.e. between the map and other rest)
    /// </summary>
    public void OnFocusEnter() {
        if (GlobalVoiceCommands.Instance.IsInDrawingMode)
            DrawingManager.Instance.ForceCursorStateChange();
        if (GuideStatus.ShouldShowGuide)
            StartCoroutine("ShowGuideCoroutine");
    }

    public void OnFocusExit() {
        if (GlobalVoiceCommands.Instance.IsInDrawingMode)
            DrawingManager.Instance.ForceCursorStateChange();
        hideGuideObject();
        StopCoroutine("ShowGuideCoroutine");
    }

    public void showMapTools() {
        if (!mapTools.activeSelf)
            mapTools.SetActive(true);
    }

    public void hideMapTools() {
        GameObject.Find("Toolbar").SetActive(false);
        if (mapTools.activeSelf)
            mapTools.SetActive(false);
    }


#region guide-related

    /// <summary>
    /// waits for gazeDurationTillGuideDisplay seconds and then display the command guide
    /// </summary>
    IEnumerator ShowGuideCoroutine() {
        if (guideObjectInstance != null || !GuideStatus.ShouldShowGuide) //already exists
            yield break;

        // wait and then display
        yield return new WaitForSeconds(GuideStatus.GazeDurationTillGuideDisplay);
        if (GuideStatus.ShouldShowGuide)
            showGuideObject();
    }

    private void showGuideObject() {
        if (isThisObjectShowingGuide)
            return;

        GuideStatus.GuideObjectInstance.SetActive(true);
        fillGuideDetails();
        GuideStatus.PositionGuideObject(GazeManager.Instance.HitPosition);
        isThisObjectShowingGuide = true;
    }

    private void hideGuideObject() {
        if (isThisObjectShowingGuide && GuideStatus.GuideObjectInstance.activeSelf) {
            GuideStatus.GuideObjectInstance.SetActive(false);
            isThisObjectShowingGuide = false;
        }
    }

    private void fillGuideDetails() {
        StringBuilder str = new StringBuilder();
        str.AppendLine(PinnedLocationManager.COMMAND_PIN_LOCATION);
        str.AppendLine(StreetViewManager.COMMAND_STREET_VIEW);
        str.AppendLine(GlobalVoiceCommands.COMMAND_MOVE_MAP);
        str.Append(Rotatable.COMMAND_ROTATE);
        GuideStatus.FillCommandDetails(str.ToString());
    }

    private void AllowGuideObject() {
        GuideStatus.ShouldShowGuide = true;
    }

    private void DisallowGuideObject() {
        GuideStatus.ShouldShowGuide = false;
        hideGuideObject();
    }
#endregion

}
