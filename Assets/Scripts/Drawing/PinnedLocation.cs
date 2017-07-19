using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using Mapbox.Utils;

public class PinnedLocation : MonoBehaviour, IFocusable {
    private Vector2d Coordinates;
    private TextMesh textMesh;
    private DeleteOnVoice deleteOnVoiceComponent;

    private void Start() {
        textMesh = GetComponentInChildren<TextMesh>();
        deleteOnVoiceComponent = gameObject.AddComponent<DeleteOnVoice>();
        deleteOnVoiceComponent.OnBeforeDelete += DeleteOnVoiceComponent_OnBeforeDelete;
        setCoordinates();
    }

    private void DeleteOnVoiceComponent_OnBeforeDelete() {
        deleteOnVoiceComponent.OnBeforeDelete -= DeleteOnVoiceComponent_OnBeforeDelete;
        // delete this object from the list
    }

    public void setCoordinates() {
        this.Coordinates = LocationHelper.worldPositionToGeoCoordinate(transform.position);
    }

    private void displayInfo() {
        transform.LookAt(Camera.main.transform);
        string textToDisplay = PrefabHolder.changeTextSize(gameObject.name, 60);
        textToDisplay = PrefabHolder.renderBold(textToDisplay);
        textToDisplay += "\n" + PrefabHolder.formatLatLong(Coordinates);
        textMesh.text = textToDisplay;
    }

    private void hideInfo() {
        textMesh.text = "";
    }

    public void OnFocusEnter() {
        displayInfo();
    }

    public void OnFocusExit() {
        hideInfo();
    }
}
