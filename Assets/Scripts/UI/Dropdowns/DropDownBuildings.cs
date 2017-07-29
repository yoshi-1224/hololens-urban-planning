using UnityEngine.UI;
/// <summary>
/// This class manages the Unity UI dropdown for buildings in scene and dictates
/// how the scene responds when a building is selected
/// </summary>
public class DropDownBuildings : DropDownManagerBase<DropDownBuildings> {

    public override void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;

        index--; // as we have a dummy item at index = 0
        string selectedBuildingName = ItemNamesInDropdown[index];
        CoordinateBoundObject buildingObject;

        if (BuildingManager.Instance.GameObjectsInScene.TryGetValue(selectedBuildingName, out buildingObject)) {
            if (buildingObject.gameObject == null)
                // this should not happen as the list is updated by BuildingManager
                return;

            // whether the building gameObject is on any of the active tiles does not matter
            // as we call LoadNewTiles() anyways
            LocationHelper.MoveMapToCoordinateAsCenter(buildingObject.coordinates);
        }

        dropdown.value = 0; // go back to the dummy value at the top of the list
    }

    public override void AddItemToDropdown(string itemName) {
        string buildingName = BuildingManager.Instance.GetBuildingName(itemName);
        if (string.IsNullOrEmpty(buildingName))
            buildingName = itemName;

        if (ItemNamesInDropdown.Contains(buildingName))
            return;

        // add the actual name of the building
        dropdown.options.Add(new Dropdown.OptionData(buildingName));
        ItemNamesInDropdown.Add(itemName);
        dropdown.RefreshShownValue();
    }

}

