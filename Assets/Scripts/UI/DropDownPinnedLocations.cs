using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using UnityEngine.UI;

/// <summary>
/// This should not be attached to a gameobject which can be disabled AT THE START.
/// </summary>
public class DropDownPinnedLocations : Singleton<DropDownPinnedLocations> {

    private Dropdown dropdown;
    private List<string> PinnedLocationNamesInDropDown;

    protected override void Awake() {
        base.Awake();
        dropdown = GetComponentInChildren<Dropdown>();
        PinnedLocationNamesInDropDown = new List<string>();
    }

    /// <summary>
    /// Unity event handler that is called when the prefab dropdown selected item is changed
    /// </summary>
    public void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;
        index--; // as we have a dummy item at index = 0

        string pinName = PinnedLocationNamesInDropDown[index];
        BuildingManager.CoordinateBoundObject pinObjectSelected;

        if (PinnedLocationManager.Instance.pinsInScene.TryGetValue(pinName, out pinObjectSelected)) {
            if (pinObjectSelected.gameObject == null) // if deleted
                return;
            LocationHelper.MoveMapToCoordinateAsCenter(pinObjectSelected.coordinates);
        }
        dropdown.value = 0;
    }

    /// <summary>
    /// adds the building name to the dropdown menu. The index in dropdown should correspond
    /// with the index in the list
    /// </summary>
    public void AddPinToDropDown(string pinName) {
        List<Dropdown.OptionData> pinData = new List<Dropdown.OptionData>();
        pinData.Add(new Dropdown.OptionData(pinName));
        PinnedLocationNamesInDropDown.Add(pinName);
        dropdown.AddOptions(pinData);
        dropdown.RefreshShownValue();
    }

    /// <summary>
    /// should be called when the toolbar moves
    /// </summary>
    public void HideDropdownList() {
        dropdown.Hide();
    }

    public void OnPinDeleted(string pinDeleted) {
        int indexToDelete = PinnedLocationNamesInDropDown.IndexOf(pinDeleted);
        PinnedLocationNamesInDropDown.RemoveAt(indexToDelete);
        DeleteItemAndRefresh(indexToDelete + 1); // off by one
    }

    private void DeleteItemAndRefresh(int indexToDelete) {
        dropdown.options.RemoveAt(indexToDelete);
        dropdown.RefreshShownValue();
    }
}
