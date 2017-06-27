// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using HoloToolkit.Unity.InputModule;

/// <summary>
/// The object cursor can switch between different game objects based on its state.
/// It simply links the game object to set to active with its associated cursor state.
/// </summary>
public class CustomObjectCursor : HoloToolkit.Unity.InputModule.Cursor {
    [Serializable]
    public struct ObjectCursorDatum {
        public string Name;
        public CursorStateEnum CursorState;
        public GameObject CursorObject;
    }

    [SerializeField]
    public ObjectCursorDatum[] CursorStateData;
    /// <summary>
    /// Sprite renderer to change.  If null find one in children
    /// </summary>
    public Transform ParentTransform;

    /// <summary>
    /// for feedbacks for rotation, translation, scaling etc.
    /// </summary>
    public GameObject translationFeedbackObject;
    public GameObject rotationFeedbackObject;
    public GameObject MessageToUser;
    public GameObject DirectionalIndicator;
    public GameObject DrawPointCursor;

    private float currentScaling;
    private TextMesh messageTextMesh;
    private bool isDrawing;

    private Dictionary<CursorStateEnum, ObjectCursorDatum> cursorStatesDict = new Dictionary<CursorStateEnum, ObjectCursorDatum>();
    private GameObject lastActiveCursorObj;

    /// <summary>
    /// On enable look for a sprite renderer on children
    /// </summary>
    protected override void OnEnable() {
        if (ParentTransform == null) {
            ParentTransform = transform;
        }

        for (int i = 0; i < ParentTransform.childCount; i++) {
            ParentTransform.GetChild(i).gameObject.SetActive(false);
        }

        base.OnEnable();
        messageTextMesh = MessageToUser.GetComponent<TextMesh>();

        // initialize dictionary
        for (int i = 0; i < CursorStateData.Length; i++) {
            CursorStateEnum stateName = CursorStateData[i].CursorState;
            cursorStatesDict[stateName] = CursorStateData[i];
        }

        isDrawing = false;
    }

    /// <summary>
    /// Override OnCursorState change to set the correct object state for the cursor
    /// </summary>
    public override void OnCursorStateChange(CursorStateEnum state) {
        base.OnCursorStateChange(state);
        if (state != CursorStateEnum.Contextual) {

            //// First, try to find a cursor for the current state
            var newActive = new ObjectCursorDatum();
            bool newStateCursorFound = cursorStatesDict.TryGetValue(state, out newActive);

            // if not found, just show the last active cursor object
            if (!newStateCursorFound)
                return;

            // hide the last active cursor
            if (lastActiveCursorObj != null)
                lastActiveCursorObj.SetActive(false);

            lastActiveCursorObj = newActive.CursorObject;
            newActive.CursorObject.SetActive(true);

            // cursorstate does NOT change if going from building => map (still observeHover)
            // targeted object is NOT updated until the state changes => use HitObject
            if (isDrawing) {
                GameObject hitObject = GazeManager.Instance.HitObject;
                if (hitObject != null) {
                    if (hitObject.name == "CustomizedMap") {
                        if (newActive.CursorState == CursorStateEnum.ObserveHover)
                            newActive.CursorObject.SetActive(false);
                        ShowPointCursor();
                        return;

                    } else if (hitObject.name.Contains("Point")) {
                        //This is for the first cursor collision
                        HidePointCursor();
                        if (newActive.CursorState == CursorStateEnum.ObserveHover)
                            newActive.CursorObject.SetActive(false);
                        return;
                    }
                }
                HidePointCursor();
            }
        }
    }

    /// The below codes are for navigation and manipulation feedback
    /// To be called by objects that implement IFocusable
    public void ShowRotationFeedback() {
        if (rotationFeedbackObject == null || rotationFeedbackObject.activeSelf)
            return;
        rotationFeedbackObject.SetActive(true);
    }

    public void HideRotationFeedback() {
        if (rotationFeedbackObject == null || !rotationFeedbackObject.activeSelf)
            return;
        rotationFeedbackObject.SetActive(false);
    }

    public void ShowTranslationFeedback() {
        if (translationFeedbackObject == null || translationFeedbackObject.activeSelf)
            return;
        translationFeedbackObject.SetActive(true);
    }

    public void HideTranslationFeedback() {
        if(translationFeedbackObject == null || !translationFeedbackObject.activeSelf)
            return;
        translationFeedbackObject.SetActive(false);
    }

    public void ShowScalingFeedback() {
        if (rotationFeedbackObject == null || rotationFeedbackObject.activeSelf)
            return;
        // make the rotation feedback vertical
        rotationFeedbackObject.transform.Rotate(new Vector3(0, 0, 90));
        rotationFeedbackObject.transform.localScale += new Vector3(1, 1, 1);
        SurfaceCursorDistance += 0.4f;
        rotationFeedbackObject.SetActive(true);
        MessageToUser.SetActive(true);
    }

    public void HideScalingFeedback() {
        if (rotationFeedbackObject == null || !rotationFeedbackObject.activeSelf)
            return;
        //put everything back to before state
        rotationFeedbackObject.transform.Rotate(new Vector3(0, 0, -90));
        rotationFeedbackObject.transform.localScale += new Vector3(-1, -1, -1);
        SurfaceCursorDistance -= 0.4f;
        rotationFeedbackObject.SetActive(false);
        MessageToUser.SetActive(false);
    }

    /// <summary>
    /// shows the current scaling for the user. arguments array MUST BE an array
    /// with first element being float value and second element boolean
    /// </summary>
    /// <param name="arguments"></param>
    public void UpdateCurrentScaling(object[] arguments) {
        currentScaling = (float)arguments[0];
        if (messageTextMesh == null)
            messageTextMesh = GetComponent<TextMesh>();
        messageTextMesh.text = string.Format("Current Scale: {0:0.0000}", currentScaling);
        bool isExceedingLimit = (bool)arguments[1];
        if (isExceedingLimit) {
            messageTextMesh.color = Color.red;
        } else {
            messageTextMesh.color = Color.white;
        }

        // TODO: make it more efficient in the future and check for inactive state
        GameObject mapInfoPanel = GameObject.Find("MapInfo");
        if (mapInfoPanel != null)
            mapInfoPanel.GetComponent<TextMesh>().text = string.Format("<b>Map Scale</b>: {0:0.0000}", currentScaling);
    }

    public void TellUserToLookLower() {
        if (MessageToUser.activeSelf) // this is a guess game. It should be inactive
            return;
        MessageToUser.SetActive(true);
        DirectionalIndicator.SetActive(true);
        messageTextMesh.text = "Look lower";
    }

    public void DisableUserMessage() {
        MessageToUser.SetActive(false);
        DirectionalIndicator.SetActive(false);
    }

    public void EnterDrawingMode() {
        isDrawing = true;
        CursorStateData[1].CursorObject.transform.localPosition -= new Vector3(0.04f, 0, 0);
        CursorStateData[4].CursorObject.transform.localPosition -= new Vector3(0.04f, 0, 0);
        OnCursorStateChange(CursorStateEnum.ObserveHover);
    }

    public void ExitDrawingMode() {
        isDrawing = false;
        OnCursorStateChange(CursorStateEnum.ObserveHover);
        if (DrawPointCursor.activeSelf)
            HidePointCursor();
        CursorStateData[1].CursorObject.transform.localPosition += new Vector3(0.04f, 0, 0);
        CursorStateData[4].CursorObject.transform.localPosition += new Vector3(0.04f, 0, 0);
    }

    public void HidePointCursor() {
        DrawPointCursor.SetActive(false);
    }

    public void ShowPointCursor() {
        DrawPointCursor.SetActive(true);
    }

    public void OnMapFocused() {
        // this not enough for the start when the user is already looking at the map
        // but still want to show the pointcursor
        OnCursorStateChange(CursorStateEnum.ObserveHover);
        // why not add to CursorStateEnum? Which can be called with manually
    }

    public void OnMapFocusExit() {
        OnCursorStateChange(CursorStateEnum.Observe);
    }

}
