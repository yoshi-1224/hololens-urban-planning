// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
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
    /// for feedbacks for rotation, translation and scaling
    /// </summary>
    public GameObject translationFeedbackObject;
    public GameObject rotationFeedbackObject;
    public GameObject MessageToUser;
    public GameObject DirectionalIndicator;

    private float currentScaling;
    private TextMesh messageTextMesh;

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
    }

    /// <summary>
    /// Override OnCursorState change to set the correct animation
    /// state for the cursor
    /// </summary>
    /// <param name="state"></param>
    public override void OnCursorStateChange(CursorStateEnum state) {
        base.OnCursorStateChange(state);
        if (state != CursorStateEnum.Contextual) {

            // First, try to find a cursor for the current state
            var newActive = new ObjectCursorDatum();
            for (int cursorIndex = 0; cursorIndex < CursorStateData.Length; cursorIndex++) {
                ObjectCursorDatum cursor = CursorStateData[cursorIndex];
                if (cursor.CursorState == state) {
                    newActive = cursor;
                    break;
                }
            }

            // If no cursor for current state is found, let the last active cursor be
            // (any cursor is better than an invisible cursor)
            if (newActive.Name == null) {
                return;
            }

            // If we come here, there is a cursor for the new state, 
            // so de-activate a possible earlier active cursor
            for (int cursorIndex = 0; cursorIndex < CursorStateData.Length; cursorIndex++) {
                ObjectCursorDatum cursor = CursorStateData[cursorIndex];
                if (cursor.CursorObject.activeSelf) {
                    cursor.CursorObject.SetActive(false);
                    break;
                }
            }
            newActive.CursorObject.SetActive(true);
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
} 
