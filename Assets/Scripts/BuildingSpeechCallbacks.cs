using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

/// <summary>
/// attach this to the object that has SpeechInputHandler, and assign the methods in this
/// class for callbacks (this class need not implement ISpeechHandler)
/// </summary>
public class ModelSpeechHandler : MonoBehaviour {

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    /// <summary>
    /// shows a table of about the building. Supposed to work only when the building is
    /// being focused at.
    /// </summary>
    private void ShowDetails()
    {
        // show the table details
    }
}
