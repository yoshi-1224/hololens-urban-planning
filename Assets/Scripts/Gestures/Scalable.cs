using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scalable : MonoBehaviour {
    [SerializeField]
    private float scalingSensitivity;

    public enum ScalingMode {
        Manipulation,
        Navigation
    }

    [SerializeField]
    private float minimumScale = 0.0005f;
    [SerializeField]
    private float maximumScale = 0.005f;

    public event Action OnRegisteringForScaling = delegate { };
    public event Action OnUnregisterForScaling = delegate { };
    public event Action<bool> OnScalingUpdated = delegate { };
    
    public void RegisterForScaling(ScalingMode mode) {
        OnRegisteringForScaling.Invoke();
        if (mode == ScalingMode.Manipulation)
            GestureManager.Instance.RegisterGameObjectForScalingUsingManipulation(this);
        else if (mode == ScalingMode.Navigation)
            GestureManager.Instance.RegisterGameObjectForScalingUsingNavigation(this);
        // change this to scalable i.e. this rather than gameObject
    }

    public void PerformScalingStarted(Vector3 canBeEither) {
    }

    public void PerformScalingUpdateUsingNavigation(Vector3 normalizedOffset) {
        float yMovement = normalizedOffset.y;
        float scalingFactor = yMovement * scalingSensitivity;
        float currentScale = transform.localScale.x;
        bool exceedingLimit = true;
        if (currentScale + scalingFactor > maximumScale) {
            transform.localScale = new Vector3(maximumScale, maximumScale, maximumScale);
        }
        else if (currentScale + scalingFactor < minimumScale) {
            transform.localScale = new Vector3(minimumScale, minimumScale, minimumScale);
        }
        else {
            exceedingLimit = false;
            transform.localScale += new Vector3(scalingFactor, scalingFactor, scalingFactor);
        }
        OnScalingUpdated.Invoke(exceedingLimit);
    }

    public void UnregisterForScaling() {
        OnUnregisterForScaling.Invoke();
    }

    public void PerformScalingUpdateUsingManipulation(Vector3 cumulativeDelta) {

    }
}
