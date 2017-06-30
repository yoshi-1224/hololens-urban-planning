using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
public class MapPlacement : Singleton<MapPlacement> {

    public GameObject mapPrefab;
    public static int MapObjectsLayer = 30;
    private void Start() {
        InstantiateMap();
    }

    public void InstantiateMap() {
        GameObject map = Instantiate(mapPrefab);
        // let the user choose the location to place first
        map.GetComponentInChildren<InteractibleMap>().OnInputClicked(null);
        //SetLayerRecursively(map, MapObjectsLayer);
        //dismissScanMessage();
    }

    private void dismissScanMessage() {
        GameObject scanningMessage = GameObject.Find("ScanningMessage");
        if (scanningMessage != null)
            Destroy(scanningMessage);
    }

    public static void SetLayerRecursively(GameObject go, int layerNumber) {
        foreach (Transform trans in go.GetComponentsInChildren<Transform>(true)) {
            trans.gameObject.layer = layerNumber;
        }
    }
}
