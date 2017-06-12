using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
public class MapPlacement : Singleton<MapPlacement> {

    public GameObject mapPrefab;
    
    public void InstantiateMap() {
        GameObject map = Instantiate(mapPrefab);
        dismissScanMessage();
        // let the user choose the location to place first
        map.GetComponentInChildren<InteractibleMap>().OnInputClicked(null);

    }

    private void dismissScanMessage() {
        GameObject scanningMessage = GameObject.Find("ScanningMessage");
        if (scanningMessage != null)
            Destroy(scanningMessage);
    }
}
