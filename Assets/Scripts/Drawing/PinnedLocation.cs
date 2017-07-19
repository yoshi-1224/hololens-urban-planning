using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity.InputModule;
using System;
using Mapbox.Utils;

public class PinnedLocation : MonoBehaviour, IFocusable {
    private Vector2d Coordinates;
    private TextMesh textMesh;

    private void Start() {
        textMesh = GetComponentInChildren<TextMesh>();
        gameObject.AddComponent<DeleteOnVoice>();
        setCoordinates();
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
