
/// <summary>
/// This should not be attached to a gameobject which can be disabled AT THE START.
/// </summary>
public class DropDownPinnedLocations : DropDownManagerBase<DropDownPinnedLocations> {

    public override void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;
        index--; // as we have a dummy item at index = 0

        string pinName = ItemNamesInDropdown[index];
        CoordinateBoundObject pinObjectSelected;

        if (PinnedLocationManager.Instance.GameObjectsInScene.TryGetValue(pinName, out pinObjectSelected)) {
            if (pinObjectSelected.gameObject != null) // if NOT deleted
                LocationHelper.MoveMapToCoordinateAsCenter(pinObjectSelected.coordinates);
        }

        dropdown.value = 0;
    }
}

