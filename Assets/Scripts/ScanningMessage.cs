//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
//
using HoloToolkit.Unity;
using UnityEngine;

// Used to place the scene origin on startup
// Adapted from Holoacadamy's fitbox
public class ScanningMessage : MonoBehaviour {

    private float Distance = 1.0f;
    private Interpolator interpolator;
    // The offset from the Camera to the StartupObject when
    // the app starts up. This is used to place the StartupObject
    // in the correct relative position after the Fitbox is
    // dismissed.
    private Vector3 collectionStartingOffsetFromCamera;

    private void Start() {
        interpolator = GetComponent<Interpolator>();
        interpolator.PositionPerSecond = 2f;
    }

    void LateUpdate() {
        Transform cameraTransform = Camera.main.transform;
        interpolator.SetTargetPosition(cameraTransform.position + (cameraTransform.forward * Distance));
    }
}
