using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class FirstDrawnPoint : MonoBehaviour, IFocusable {
    private float scalingFactor = 1.3f;
    private bool isEnlarged;

    private void Start() {
        // make the cursor sphere the only one with rigidbody?
        // if we make it with our hands then we might need to do onTriggerEnter
        // but with gaze, just use onFocusEnter/onFocusExit

        // adding this collider shifts the first sphere by a bit in upwards direction
        SphereCollider collider = gameObject.AddComponent<SphereCollider>();
        collider.radius *= 3;

        isEnlarged = false;
    }

    private void enlargeToShowFeedback() {
        if (!isEnlarged)
            transform.localScale *= scalingFactor;
        isEnlarged = true;
    }

    private void returnToNormalScale() {
        if (isEnlarged)
            transform.localScale /= scalingFactor;
        isEnlarged = false;
    }

    public void OnFocusEnter() {
        if (DrawingManager.Instance.CheckCanPolygonBeEnclosed()) {
            DrawingManager.Instance.CanPolygonBeEnclosedAndCursorOnFirstPoint = true;
            enlargeToShowFeedback();
            DrawingManager.Instance.FixLineEndAtFirstSphere();
            //DrawingManager.Instance.ForceCursorStateChange();
            DrawingManager.Instance.instantiateGuide(transform.position);
        }
    }

    public void OnFocusExit() {
        DrawingManager.Instance.CanPolygonBeEnclosedAndCursorOnFirstPoint = false;
        returnToNormalScale();
        DrawingManager.Instance.destroyGuide();
        //DrawingManager.Instance.ForceCursorStateChange();
    }

    private void OnDestroy() {
        if (DrawingManager.Instance != null)
            DrawingManager.Instance.destroyGuide();
    }

}
