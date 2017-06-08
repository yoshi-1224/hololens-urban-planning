using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class Interactible : MonoBehaviour, IFocusable {
    public GameObject cursor;
    private CustomObjectCursor cursorScriptCache;

    public void OnFocusEnter() {
        cursorScriptCache.showManipulationFeedback();
    }

    public void OnFocusExit() {
        cursorScriptCache.hideMan();
    }

    // Use this for initialization
    void Start() {
        cursorScriptCache = cursor.GetComponent<CustomObjectCursor>();
    }

    // Update is called once per frame
    void Update() {

    }
}
