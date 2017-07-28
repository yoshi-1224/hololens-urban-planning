using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;

/// <summary>
/// This class listens to the gesture events and sends messages to the register game object
/// in order to perform transforms (i.e. scaling, tranlation and rotation)
/// </summary>

public class GestureManager : Singleton<GestureManager>, IManipulationHandler, INavigationHandler {
    public bool IsTranslating { get; private set; }
    public bool IsRotating { get; private set; }
    public bool IsScalingUsingNavigation { get; private set; }
    public bool IsScalingUsingManipulation { get; private set; }

    [Tooltip("Reference to cursor object script in order to show motion feedbacks")]
    [SerializeField]
    private CustomObjectCursor cursorScript;

    private Scalable currentObjectScalableComponent;
    private Rotatable currentObjectRotatableComponent;
    private Movable currentObjectMovableComponent;

    protected override void Awake() {
        base.Awake();
        IsRotating = false;
        IsTranslating = false;
        IsScalingUsingNavigation = false;
        IsScalingUsingManipulation = false;
    }

    #region registering game object component
    public bool RegisterGameObjectForRotation(Rotatable rotatableComponent) {
        if (isAnotherObjectAlreadyRegistered())
            return false;

        DisallowGuideObject();
        IsRotating = true;
        currentObjectRotatableComponent = rotatableComponent;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowRotationFeedback();
        return true;
    }

    /// <summary>
    /// right now this is used for the base map rotation only
    /// </summary>
    public bool RegisterGameObjectForScalingUsingNavigation(Scalable scalableComponent) {
        if (isAnotherObjectAlreadyRegistered())
            return false;

        DisallowGuideObject();
        IsScalingUsingNavigation = true;
        currentObjectScalableComponent = scalableComponent;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowScalingMapFeedback();
        return true;
    }

    public bool RegisterGameObjectForScalingUsingManipulation(Scalable scalableComponent) {
        if (isAnotherObjectAlreadyRegistered())
            return false;

        DisallowGuideObject();
        IsScalingUsingManipulation = true;
        currentObjectScalableComponent = scalableComponent;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowScalingFeedback();
        return true;
    }

    public bool RegisterGameObjectForTranslation(Movable movableComponent) {
        if (isAnotherObjectAlreadyRegistered())
            return false;

        DisallowGuideObject();
        IsTranslating = true;
        currentObjectMovableComponent = movableComponent;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowTranslationFeedback();
        return true;
    }

    private bool isAnotherObjectAlreadyRegistered() {
        if (IsTranslating || IsRotating || IsScalingUsingNavigation || IsScalingUsingManipulation)
            return true;
        return false;
    }

    #endregion

    #region unregistering game object component
    public void UnregisterObjectForScaling() {
        currentObjectScalableComponent.UnregisterForScaling();
        currentObjectScalableComponent = null;
        UnregisterCleanUp();
    }

    public void UnregisterObjectForRotation() {
        currentObjectRotatableComponent.UnregisterForRotation();
        currentObjectRotatableComponent = null;
        UnregisterCleanUp();
    }

    public void UnregisterObjectForTranslation() {
        currentObjectMovableComponent.UnregisterForTranslation();
        currentObjectMovableComponent = null;
        UnregisterCleanUp();
    }

    public void UnregisterCleanUp() {
        if (IsRotating) {
            IsRotating = false;
            cursorScript.HideRotationFeedback();
        }
        else if (IsTranslating) {
            IsTranslating = false;
            cursorScript.HideTranslationFeedback();
        }
        else if (IsScalingUsingNavigation) {
            IsScalingUsingNavigation = false;
            cursorScript.HideScalingMapFeedback();
        }
        else if (IsScalingUsingManipulation) {
            IsScalingUsingManipulation = false;
            cursorScript.HideScalingFeedback();
        }
    
        // clear the stack so that other gameobjects can receive gesture inputs
        InputManager.Instance.ClearModalInputStack();
        AllowGuideObject();
    }

#endregion

    #region navigation
    public void OnNavigationStarted(NavigationEventData eventData) {
        if (IsScalingUsingNavigation)
            currentObjectScalableComponent.PerformScalingStarted(eventData.NormalizedOffset);
        else if (IsRotating)
            currentObjectRotatableComponent.PerformRotationStarted(eventData.NormalizedOffset);
    }

    public void OnNavigationUpdated(NavigationEventData eventData) {
        if (IsRotating) {
            currentObjectRotatableComponent.PerformRotationUpdate(eventData.NormalizedOffset);
        } else if (IsScalingUsingNavigation) {
            currentObjectScalableComponent.PerformScalingUpdateUsingNavigation(eventData.NormalizedOffset);
        }
    }

    public void OnNavigationCompleted(NavigationEventData eventData) {
        if (IsRotating)
            UnregisterObjectForRotation();
        else if (IsScalingUsingNavigation)
            UnregisterObjectForScaling();
    }

    public void OnNavigationCanceled(NavigationEventData eventData) {
        if (IsRotating)
            UnregisterObjectForRotation();
        else if (IsScalingUsingNavigation)
            UnregisterObjectForScaling();
    }

    #endregion

    #region manipulation
    public void OnManipulationCanceled(ManipulationEventData eventData) {
        OnManipulationCompleted(eventData);
    }

    public void OnManipulationCompleted(ManipulationEventData eventData) {
        if (IsTranslating)
            UnregisterObjectForTranslation();
        else if (IsScalingUsingManipulation)
            UnregisterObjectForScaling();
    }

    public void OnManipulationStarted(ManipulationEventData eventData) {
        if (IsTranslating)
            currentObjectMovableComponent.PerformTranslationStarted(eventData.CumulativeDelta);
        else if (IsScalingUsingManipulation)
            currentObjectScalableComponent.PerformScalingStarted(eventData.CumulativeDelta);
    }

    public void OnManipulationUpdated(ManipulationEventData eventData) {
        if (IsTranslating)
            currentObjectMovableComponent.PerformTranslationUpdate(eventData.CumulativeDelta);
        else if (IsScalingUsingManipulation)
            currentObjectScalableComponent.PerformScalingUpdateUsingManipulation(eventData.CumulativeDelta);

    }

    #endregion

    private void AllowGuideObject() {
        GuideStatus.ShouldShowGuide = true;
    }

    private void DisallowGuideObject() {
        GuideStatus.ShouldShowGuide = false;
        GuideStatus.GuideObjectInstance.SetActive(false);
    }

}
