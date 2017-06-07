using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
public class MapPlacement : Singleton<MapPlacement> {

    public GameObject mapPrefab;
    
    public void InstantiateMap() {
        GameObject map = Instantiate(mapPrefab);
        map.transform.parent = transform;
        // let the user choose the location to place
        map.GetComponent<Placeable>().OnInputClicked(null);

    }
}
