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
public class GestureManager : Singleton<GestureManager>, IManipulationHandler {
    public bool IsTranslating {
        get; private set;
    }

    public bool IsRotating {
        get; private set;
    }

    public bool IsScaling {
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
        IsScaling = false;
        currentObjectInMotion = null;
    }

    public void OnManipulationCanceled(ManipulationEventData eventData) {
        if (IsTranslating || IsRotating || IsScaling)
            Unregister();
    }

    public void OnManipulationCompleted(ManipulationEventData eventData) {
        if (IsTranslating || IsRotating || IsScaling)
            Unregister();
    }

    public void OnManipulationStarted(ManipulationEventData eventData) {
        if (IsTranslating)
            currentObjectInMotion.SendMessage("PerformTranslationStarted", eventData.CumulativeDelta);
        if (IsScaling)
            currentObjectInMotion.SendMessage("PerformScalingStarted");
        // just to get rid of the guide from the map
    }

    public void OnManipulationUpdated(ManipulationEventData eventData) {
        if (IsRotating) {
            currentObjectInMotion.SendMessage("PerformRotationUpdate", eventData.CumulativeDelta);
        } else if (IsTranslating) {
            currentObjectInMotion.SendMessage("PerformTranslationUpdate", eventData.CumulativeDelta);
        } else if (IsScaling){
            currentObjectInMotion.SendMessage("PerformScalingUpdate", eventData.CumulativeDelta);
        } else {
            // ignore
        }
        
    }

    public bool RegisterGameObjectForRotation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScaling)
            return false;

        IsRotating = true;
        currentObjectInMotion = objectToRegister;
        // register this as what receives the gestures in prescedence
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowRotationFeedback");
        return true;
    }

    public bool RegisterGameObjectForTranslation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScaling)
            return false;
        IsTranslating = true;
        currentObjectInMotion = objectToRegister;
        // register this as what receives the gestures in prescedence
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowTranslationFeedback");
        return true;
    }

    public bool RegisterGameObjectForScaling(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating || IsScaling)
            return false;
        IsScaling = true;
        currentObjectInMotion = objectToRegister;
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowScalingFeedback");
        return true;
    }

    public void Unregister() {
        Debug.Log("unregistering object for the motion");
        currentObjectInMotion.SendMessage("UnregisterCallBack");
        currentObjectInMotion = null;
        if (IsRotating) {
            cursor.SendMessage("HideRotationFeedback");
        } else if (IsTranslating) {
            cursor.SendMessage("HideTranslationFeedback");
        } else if (IsScaling) {
            cursor.SendMessage("HideScalingFeedback");
        }
        // clear the stack so that other gameobjects can receive gesture inputs
        // it might actually be necessary to have the manipulation handler on the object itself
        // or just register this as global listener!
        InputManager.Instance.ClearModalInputStack();
        
        IsRotating = false;
        IsTranslating = false;
        IsScaling = false;
    }
}
