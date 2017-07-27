
public class DropDownPolygons : DropDownManagerBase<DropDownPolygons> {

    public override void DropDown_OnItemSelected(int index) {
        if (index == 0)
            return;
        index--; // as we have a dummy item at index = 0

        string polygonName = ItemNamesInDropdown[index];
        CoordinateBoundObject polygonObjectSelected;

        if (PolygonManager.Instance.GameObjectsInScene.TryGetValue(polygonName, out polygonObjectSelected)) {
            if (polygonObjectSelected.gameObject != null) // if NOT deleted
                LocationHelper.MoveMapToCoordinateAsCenter(polygonObjectSelected.coordinates);
        }

        dropdown.value = 0;
    }
}
