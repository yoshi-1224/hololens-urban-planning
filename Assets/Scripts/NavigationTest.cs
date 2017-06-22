using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class NavigationTest : MonoBehaviour, INavigationHandler, IManipulationHandler {
    public void OnManipulationCanceled(ManipulationEventData eventData) {
    }

    public void OnManipulationCompleted(ManipulationEventData eventData) {
    }

    public void OnManipulationStarted(ManipulationEventData eventData) {
        Debug.Log("started");
    }

    public void OnManipulationUpdated(ManipulationEventData eventData) {
        Debug.Log("updated");
    }

    public void OnNavigationCanceled(NavigationEventData eventData) {
    }

    public void OnNavigationCompleted(NavigationEventData eventData) {
    }

    public void OnNavigationStarted(NavigationEventData eventData) {
    }

    public void OnNavigationUpdated(NavigationEventData eventData) {
        float rotationFactor = eventData.NormalizedOffset.x * 10;
        transform.Rotate(new Vector3(0, rotationFactor, 0));
    }

}
