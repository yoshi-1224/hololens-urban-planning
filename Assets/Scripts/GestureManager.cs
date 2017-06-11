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
        currentObjectInMotion = null;
    }

    public void OnManipulationCanceled(ManipulationEventData eventData) {
        Debug.Log("manipulation completed");
        if (IsTranslating || IsRotating)
            Unregister();
    }

    public void OnManipulationCompleted(ManipulationEventData eventData) {
        Debug.Log("manipulation completed");
        if (IsTranslating || IsRotating)
            Unregister();
    }

    public void OnManipulationStarted(ManipulationEventData eventData) {
        Debug.Log("manipulation started");
        currentObjectInMotion.SendMessage("PerformTranslationStarted", eventData.CumulativeDelta);
    }

    public void OnManipulationUpdated(ManipulationEventData eventData) {
        Debug.Log("manipulation updated");
        if (IsRotating) {
            currentObjectInMotion.SendMessage("PerformRotationUpdate", eventData.CumulativeDelta);
        } else if (IsTranslating) {
            currentObjectInMotion.SendMessage("PerformTranslationUpdate", eventData.CumulativeDelta);
        } else {
            // ignore
        }
        
    }

    public bool RegisterGameObjectForRotation(GameObject objectToRegister) {
        if (currentObjectInMotion != null || IsTranslating || IsRotating)
            return false;

        IsRotating = true;
        currentObjectInMotion = objectToRegister;
        // register this as what receives the gestures in prescedence
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowRotationFeedback");
        return true;
    }

    public bool RegisterGameObjectForTranslation(GameObject objectToRegister) {
        Debug.Log("this is called");
        if (currentObjectInMotion != null || IsTranslating || IsRotating)
            return false;
        Debug.Log("has reached here ");
        IsTranslating = true;
        currentObjectInMotion = objectToRegister;
        // register this as what receives the gestures in prescedence
        InputManager.Instance.PushModalInputHandler(gameObject);
        cursor.SendMessage("ShowTranslationFeedback");
        return true;
    }

    public void Unregister() {
        Debug.Log("unregistering object for the motion");
        currentObjectInMotion = null;
        if (IsRotating) {
            cursor.SendMessage("HideRotationFeedback");
        } else if (IsTranslating) {
            cursor.SendMessage("HideTranslationFeedback");
        }
        // clear the stack so that other gameobjects can receive gesture inputs
        // it might actually be necessary to have the manipulation handler on the object itself
        // or just register this as global listener!
        InputManager.Instance.ClearModalInputStack();

        IsRotating = false;
        IsTranslating = false;
    }


    // overriding the inputmanager might be necessary
}
