using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using HoloToolkit.Unity;
using System;

/// <summary>
/// This class listens to the gesture events and sends messages to the focused building
/// in order to perform transforms (i.e. manipulation and rotation)
/// </summary>
///
/// <remark>
/// The reason why this class exists is because if we implement the event handlers in interactible.cs,
/// all the buildings become event handlers which leads to inefficiency.
/// </remark>
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

    /// <summary>
    /// to be set by Interactible.cs class by voice command. There must be only one at one time
    /// </summary>
    public GameObject currentObjectInMotion { get; set; }

    protected override void Awake() {
        base.Awake();
        IsRotating = false;
        IsTranslating = false;
        IsScalingUsingNavigation = false;
        IsScalingUsingManipulation = false;
        currentObjectInMotion = null;
    }

    public void OnManipulationCanceled(ManipulationEventData eventData) {
        if (IsTranslating || IsScalingUsingManipulation)
            UnregisterCleanUp();
    }

    public void OnManipulationCompleted(ManipulationEventData eventData) {
        if (IsTranslating)
            UnregisterCleanUp();
        else if (IsScalingUsingManipulation)
            UnregisterObjectForScaling();
    }

    public void OnManipulationStarted(ManipulationEventData eventData) {
        if (IsTranslating)
            currentObjectInMotion.SendMessage("PerformTranslationStarted", eventData.CumulativeDelta);
        else if (IsScalingUsingManipulation)
            currentObjectScalableComponent.PerformScalingStarted(eventData.CumulativeDelta);
    }

    public void OnManipulationUpdated(ManipulationEventData eventData) {
        if (IsTranslating) {
            currentObjectInMotion.SendMessage("PerformTranslationUpdate", eventData.CumulativeDelta);
        } else if (IsScalingUsingManipulation) {
            currentObjectScalableComponent.PerformScalingUpdateUsingManipulation(eventData.CumulativeDelta);
        } else {
            // ignore
        }
    }

    public bool RegisterGameObjectForRotation(Rotatable rotatableComponent) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation | IsScalingUsingManipulation)
            return false;

        IsRotating = true;
        currentObjectRotatableComponent = rotatableComponent;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowRotationFeedback();
        return true;
    }

    public bool RegisterGameObjectForTranslation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation || IsScalingUsingManipulation)
            return false;
        IsTranslating = true;
        currentObjectInMotion = objectToRegister;

        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowTranslationFeedback();
        return true;
    }

    /// <summary>
    /// right now this is used for the base map rotation only
    /// </summary>
    public bool RegisterGameObjectForScalingUsingNavigation(Scalable scalableComponent) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation || IsScalingUsingManipulation)
            return false;
        IsScalingUsingNavigation = true;
        currentObjectScalableComponent = scalableComponent;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowScalingMapFeedback();
        return true;
    }

    public bool RegisterGameObjectForScalingUsingManipulation(Scalable scalableComponent) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation || IsScalingUsingManipulation)
            return false;
        IsScalingUsingManipulation = true;
        currentObjectScalableComponent = scalableComponent;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursorScript.ShowScalingFeedback();
        return true;
    }

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

    public void UnregisterCleanUp() {
        currentObjectInMotion = null;
        if (IsRotating) {
            cursorScript.HideRotationFeedback();
        }
        else if (IsTranslating) {
            cursorScript.HideTranslationFeedback();
        }
        else if (IsScalingUsingNavigation) {
            cursorScript.HideScalingMapFeedback();
        }
        else if (IsScalingUsingManipulation)
            cursorScript.HideScalingFeedback();
        // clear the stack so that other gameobjects can receive gesture inputs
        // it might actually be necessary to have the manipulation handler on the object itself
        // or just register this as global listener!
        InputManager.Instance.ClearModalInputStack();
        
        IsRotating = false;
        IsTranslating = false;
        IsScalingUsingNavigation = false;
        IsScalingUsingManipulation = false;
    }

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
}
