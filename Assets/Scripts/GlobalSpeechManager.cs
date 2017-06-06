using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class GlobalSpeechManager : MonoBehaviour {

    void Start () {
        if (InputManager.Instance == null) {
            return;
        }
        InputManager.Instance.AddGlobalListener(gameObject);
	}
	
	void Update () {
		
	}

    private void OnDestroy() {
        if (InputManager.Instance == null) {
            return;
        }
        InputManager.Instance.RemoveGlobalListener(gameObject);
    }

    public void changeMapLayout(string layoutType) {
        switch (layoutType) {
            case "":

            default:
                break;
        }
    }
}
