using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scalable : MonoBehaviour {
    public float ScalingSensitivity;
    public float minimumScale = 0.0005f;
    public float maximumScale = 0.005f;

    public enum ScalingMode {
        Manipulation,
        Navigation
    }

    public event Action OnRegisteringForScaling = delegate { };
    public event Action OnUnregisterForScaling = delegate { };
    public event Action<bool> OnScalingUpdated = delegate { };

    private Vector3 previousManipulationPosition;
    private Transform parentTransform;

    public void RegisterForScaling(ScalingMode mode) {
        OnRegisteringForScaling.Invoke();
        if (mode == ScalingMode.Manipulation)
            GestureManager.Instance.RegisterGameObjectForScalingUsingManipulation(this);
        else if (mode == ScalingMode.Navigation)
            GestureManager.Instance.RegisterGameObjectForScalingUsingNavigation(this);
        // change this to scalable i.e. this rather than gameObject
    }

    public void PerformScalingStarted(Vector3 canBeEither) {
        parentTransform = transform.parent;
        transform.parent = null;
        previousManipulationPosition = Camera.main.transform.InverseTransformPoint(canBeEither);
    }

    public void PerformScalingUpdateUsingNavigation(Vector3 normalizedOffset) {
        float yMovement = normalizedOffset.y;
        float scalingFactor = yMovement * ScalingSensitivity;
        float currentScale = transform.localScale.x;
        bool isExceedingLimit = true;
        if (currentScale + scalingFactor > maximumScale) {
            transform.localScale = Vector3.one * maximumScale;
        }
        else if (currentScale + scalingFactor < minimumScale) {
            transform.localScale = Vector3.one * minimumScale;
        } else {
            isExceedingLimit = false;
            transform.localScale += new Vector3(scalingFactor, scalingFactor, scalingFactor);
        }
        OnScalingUpdated.Invoke(isExceedingLimit);
    }

    public void UnregisterForScaling() {
        transform.parent = parentTransform;
        OnUnregisterForScaling.Invoke();
    }

    /// <summary>
    /// note that currently only the height, along the y-axis is scalable. This can
    /// be more generalized using a serialized boolean/enum field in this script
    /// </summary>
    public void PerformScalingUpdateUsingManipulation(Vector3 cumulativeDelta) {
        // we should make this one use manipulation gesture rather than navigation
        Vector3 moveVector = Vector3.zero;
        Vector3 cumulativeDeltaInCameraSpace = Camera.main.transform.InverseTransformPoint(cumulativeDelta);
        moveVector = cumulativeDeltaInCameraSpace - previousManipulationPosition;
        previousManipulationPosition = cumulativeDeltaInCameraSpace;
        float currentYScale = transform.localScale.y;

        float scalingFactor = moveVector.y * ScalingSensitivity;

        bool isExceedingLimit;
        if (currentYScale + scalingFactor <= minimumScale) {
            isExceedingLimit = true;
        } else {
            transform.localScale += new Vector3(0, scalingFactor, 0);
            isExceedingLimit = false;
        }

        OnScalingUpdated.Invoke(isExceedingLimit);
    }
}
