using UnityEngine;
using HoloToolkit.Unity.InputModule;
using Mapbox.Utils;
using UnityEngine.UI;

/// <summary>
/// This component is to be attached to pin objects that bookmark a coordinate.
/// </summary>
public class PinnedLocation : MonoBehaviour, IFocusable {
    [SerializeField]
    private GameObject textHolder;
    private Vector2d Coordinates;
    private Text text;

    private void Start() {
        text =  textHolder.GetComponentInChildren<Text>();
        textHolder.SetActive(false);
        gameObject.AddComponent<DeleteOnVoice>().OnBeforeDelete += DeleteOnVoiceComponent_OnBeforeDelete;
        setCoordinates();
    }

    private void DeleteOnVoiceComponent_OnBeforeDelete(DeleteOnVoice component) {
        component.OnBeforeDelete -= DeleteOnVoiceComponent_OnBeforeDelete;
        
        // delete this object from the list
        if (DropDownPinnedLocations.Instance != null)
            DropDownPinnedLocations.Instance.OnItemDeleted(gameObject.name);
    }

    public void setCoordinates() {
        this.Coordinates = LocationHelper.WorldPositionToGeoCoordinate(transform.position);
        updateTextToDisplay();
    }

    private void updateTextToDisplay() {
        string textToDisplay = Utils.ChangeTextSize(gameObject.name, 25);
        textToDisplay = Utils.RenderBold(textToDisplay);
        textToDisplay += "\n" + Utils.FormatLatLong(Coordinates);
        text.text = textToDisplay;
    }

    private void displayInfo() {
        textHolder.SetActive(true);
        textHolder.transform.LookAt(Camera.main.transform);
    }

    private void hideInfo() {
        textHolder.SetActive(false);
    }

    public void OnFocusEnter() {
        displayInfo();
    }

    public void OnFocusExit() {
        hideInfo();
    }
}
