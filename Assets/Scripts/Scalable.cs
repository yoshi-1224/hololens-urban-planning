using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class Scalable : MonoBehaviour, ISpeechHandler {
    public const string COMMAND_SCALE = "scale";
    private float ScalingSensitivity = 50f;
    private Transform parent;
    private float minimumHeight = 0.1f;
    private Vector3 previousManipulationPosition;

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData) {
        switch(eventData.RecognizedText.ToLower()) {
            case COMMAND_SCALE:
                RegisterForScaling();
                break;
            default:
                break;
        }
    }

    #region scaling-related
    public void PerformScalingStarted(Vector3 cumulativeDelta) {
        Debug.Log("Scaling started");
        parent = transform.parent;
        transform.parent = parent.transform.parent;
        previousManipulationPosition = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
    }

    public void PerformScalingUpdate(Vector3 cumulativeDelta) {
        // we should make this one use manipulation gesture rather than navigation
        Vector3 moveVector = Vector3.zero;
        Vector3 cumulativeDeltaInCameraSpace = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
        moveVector = cumulativeDeltaInCameraSpace - previousManipulationPosition;
        previousManipulationPosition = cumulativeDeltaInCameraSpace;
        float currentYScale = transform.localScale.y;
        float scalingFactor = moveVector.y * ScalingSensitivity;
        if (currentYScale + scalingFactor <= minimumHeight) {

        } else {
            transform.localScale += new Vector3(0, scalingFactor, 0);
        }

        // notify the user of height, GFA?, number of storeys

    }

    public void RegisterForScaling() {
        GestureManager.Instance.RegisterGameObjectForScalingUsingManipulation(gameObject);
    }

    public void UnregisterCallBack() {
        transform.parent = parent;
    }

#endregion
}
