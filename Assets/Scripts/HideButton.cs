using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class HideButton : MonoBehaviour, IInputClickHandler {

    public void OnInputClicked(InputClickedEventData eventData) {
        GlobalVoiceCommands.Instance.HideTools();
    }
}
