using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using UnityEngine.UI;

public class DropDownPinnedLocations : Singleton<DropDownPinnedLocations> {

    /// <summary>
    /// dropdown component attached to this gameObject
    /// </summary>
    private Dropdown dropdown;

    private List<string> PinnedLocationNamesInDropDown;

    protected override void Awake() {
        base.Awake();
        dropdown = GetComponent<Dropdown>();
        PinnedLocationNamesInDropDown = new List<string>();
    }

    /// <summary>
    /// Unity event handler that is called when the prefab dropdown selected item is changed
    /// </summary>
    public void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;
        index--; // as we have a dummy item at index = 0
        InteractibleMap.Instance.HideAllTables();
        // easier to directly find it
        GameObject pinObjectSelected = GameObject.Find(PinnedLocationNamesInDropDown[index]);
        LocationHelper.MoveMapToWorldPositionAsCenter(pinObjectSelected.transform.position);
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
}
