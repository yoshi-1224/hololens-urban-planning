using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;

public class HideButton : MonoBehaviour, IInputClickHandler {

    /// <summary>
    /// use this event to detect when the button is clicked and act accordingly
    /// </summary>
    public event Action OnButtonClicked = delegate { };

    public void OnInputClicked(InputClickedEventData eventData) {
        OnButtonClicked.Invoke();
    }
}
