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
    public bool IsTranslating {
        get; private set;
    }

    public bool IsRotating {
        get; private set;
    }

    public bool IsScalingUsingNavigation {
        get; private set;
    }

    public bool IsScalingUsingManipulation {
        get; private set;
    }

    [Tooltip("Reference to cursor object in order to show motion feedbacks")]
    public GameObject cursor;

    /// <summary>
    /// to be set by Interactible.cs class by voice command. There must be only one at one time
    /// </summary>
    public GameObject currentObjectInMotion {
        get; set;
    }

    private void Start() {
        IsRotating = false;
        IsTranslating = false;
        IsScalingUsingNavigation = false;
        IsScalingUsingManipulation = false;
        currentObjectInMotion = null;
    }

    public void OnManipulationCanceled(ManipulationEventData eventData) {
        if (IsTranslating || IsScalingUsingManipulation)
            UnregisterObject();
    }

    public void OnManipulationCompleted(ManipulationEventData eventData) {
        if (IsTranslating || IsScalingUsingManipulation)
            UnregisterObject();
    }

    public void OnManipulationStarted(ManipulationEventData eventData) {
        if (IsTranslating)
            currentObjectInMotion.SendMessage("PerformTranslationStarted", eventData.CumulativeDelta);
        else if (IsScalingUsingManipulation)
            currentObjectInMotion.SendMessage("PerformScalingStarted", eventData.CumulativeDelta);
    }

    public void OnManipulationUpdated(ManipulationEventData eventData) {
        if (IsTranslating) {
            currentObjectInMotion.SendMessage("PerformTranslationUpdate", eventData.CumulativeDelta);
        } else if (IsScalingUsingManipulation) {
            currentObjectInMotion.SendMessage("PerformScalingUpdate", eventData.CumulativeDelta);
        } else {
            // ignore
        }
    }

    public bool RegisterGameObjectForRotation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation | IsScalingUsingManipulation)
            return false;

        IsRotating = true;
        currentObjectInMotion = objectToRegister;

        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowRotationFeedback");
        return true;
    }

    public bool RegisterGameObjectForTranslation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation || IsScalingUsingManipulation)
            return false;
        IsTranslating = true;
        currentObjectInMotion = objectToRegister;

        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowTranslationFeedback");
        return true;
    }

    public bool RegisterGameObjectForScalingUsingNavigation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation || IsScalingUsingManipulation)
            return false;
        IsScalingUsingNavigation = true;
        currentObjectInMotion = objectToRegister;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowScalingFeedback");
        return true;
    }

    public bool RegisterGameObjectForScalingUsingManipulation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScalingUsingNavigation || IsScalingUsingManipulation)
            return false;
        IsScalingUsingManipulation = true;
        currentObjectInMotion = objectToRegister;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowScalingFeedback");
        return true;
    }

    public void UnregisterObject() {
        currentObjectInMotion.SendMessage("UnregisterCallBack");
        currentObjectInMotion = null;
        if (IsRotating) {
            cursor.SendMessage("HideRotationFeedback");
        }
        else if (IsTranslating) {
            cursor.SendMessage("HideTranslationFeedback");
        }
        else if (IsScalingUsingNavigation) {
            cursor.SendMessage("HideScalingFeedback");
        }
        else if (IsScalingUsingManipulation)
            cursor.SendMessage("HideScalingFeedback");
        // clear the stack so that other gameobjects can receive gesture inputs
        // it might actually be necessary to have the manipulation handler on the object itself
        // or just register this as global listener!
        InputManager.Instance.ClearModalInputStack();
        
        IsRotating = false;
        IsTranslating = false;
        IsScalingUsingNavigation = false;
        IsScalingUsingManipulation = false;
    }

    public void UpdateMapScale() {

    }

    public void OnNavigationStarted(NavigationEventData eventData) {
        if (IsScalingUsingNavigation)
            currentObjectInMotion.SendMessage("PerformScalingStarted", eventData.NormalizedOffset);
        // just to get rid of the guide from the map
        else if (IsRotating)
            currentObjectInMotion.SendMessage("PerformRotationStarted", eventData.NormalizedOffset);
    }

    public void OnNavigationUpdated(NavigationEventData eventData) {
        if (IsRotating) {
            currentObjectInMotion.SendMessage("PerformRotationUpdate", eventData.NormalizedOffset);
        } else if (IsScalingUsingNavigation) {
            currentObjectInMotion.SendMessage("PerformScalingUpdate", eventData.NormalizedOffset);
        } else {
            // ignore
        }
    }

    public void OnNavigationCompleted(NavigationEventData eventData) {
        if (IsRotating || IsScalingUsingNavigation)
            UnregisterObject();
    }

    public void OnNavigationCanceled(NavigationEventData eventData) {
        if (IsRotating || IsScalingUsingNavigation)
            UnregisterObject();
    }
}
