using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;

/// <summary>
/// This class holds onto the references of prefabs in order to make the other codes less
/// complicated. Also implements object pooling to improve performance.
/// </summary>
public class PrefabHolder : Singleton<PrefabHolder> {
    public GameObject tablePrefab;
    public List<GameObject> tablePrefabPool;
    private int pooledAmount = 5;

    protected override void Awake() {
        base.Awake();
        tablePrefabPool = new List<GameObject>(pooledAmount); // 5 is the amount to be pooled
        for (int i = 0; i < pooledAmount; i++) {
            GameObject obj = Instantiate(tablePrefab);
            obj.SetActive(false); // hide them at the start
            tablePrefabPool.Add(obj);
        }
    }

    public GameObject GetPooledTable() {
        for (int i = 0; i < tablePrefabPool.Count; i++) {
            if (!tablePrefabPool[i].activeSelf) // inactive means not used
                return tablePrefabPool[i];
        }

        GameObject newTableObject = Instantiate(tablePrefab);
        tablePrefabPool.Add(newTableObject);
        return newTableObject;
    }

}
