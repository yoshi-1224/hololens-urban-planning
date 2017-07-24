using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity;
using System;
using Mapbox.Utils;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using System.Linq;

public class CoordinateBoundObjectsManagerBase<T> : MonoBehaviour where T : CoordinateBoundObjectsManagerBase<T> {

    [Serializable]
    public struct CoordinateBoundObject {
        public float latitude;
        public float longitude;
        public Vector2d coordinates {
            get {
                return new Vector2d(latitude, longitude); // note that x = lat, y = long
            }
        }
        public GameObject gameObject;
    }

    private static T instance;
    public static T Instance {
        get {
            return instance;
        }
    }

    public Dictionary<string, CoordinateBoundObject> GameObjectsInScene { get; set; }

    /// <summary>
    /// use this to process buildings one by one in order to minimize the computing cost
    /// on each frame
    /// </summary>
    private Queue<CoordinateBoundObject> ObjectsToLoad;

    private bool shouldStartLoadingObjects;

    /// <summary>
    /// Returns whether the instance has been initialized or not.
    /// </summary>
    public static bool IsInitialized {
        get {
            return instance != null;
        }
    }

    /// <summary>
    /// Base awake method that sets the singleton's unique instance.
    /// </summary>
    protected virtual void Awake() {
        if (instance != null) {
            Debug.LogErrorFormat("Trying to instantiate a second instance of singleton class {0}", GetType().Name);
            return;
        }

        instance = (T)this;
        ObjectsToLoad = new Queue<CoordinateBoundObject>();
        GameObjectsInScene = new Dictionary<string, CoordinateBoundObject>();
        CustomRangeTileProvider.OnTileObjectAdded += TileProvider_OnTileAdded;
        CustomRangeTileProvider.OnAllTilesLoaded += TileProvider_OnAllTilesLoaded;
        shouldStartLoadingObjects = false;
    }

    protected virtual void TileProvider_OnAllTilesLoaded() {
        shouldStartLoadingObjects = true;
    }

    protected virtual void TileProvider_OnTileAdded(UnwrappedTileId tileId) {
        queryObjectsWithinTileBounds(tileId);
    }

    protected virtual void Update() {
        if (shouldStartLoadingObjects && ObjectsToLoad.Count > 0) {
            loadObject(ObjectsToLoad.Dequeue());
            if (ObjectsToLoad.Count == 0)
                shouldStartLoadingObjects = false;
        }
    }

    protected virtual void OnDestroy() {
        if (instance == this) {
            instance = null;
        }

        ObjectsToLoad = null;
        GameObjectsInScene = null;
        CustomRangeTileProvider.OnTileObjectAdded -= TileProvider_OnTileAdded;
        CustomRangeTileProvider.OnAllTilesLoaded -= TileProvider_OnAllTilesLoaded;
    }

    protected virtual void queryObjectsWithinTileBounds(UnwrappedTileId tileId) {
        Vector2dBounds bounds = Conversions.TileIdToBounds(tileId);
        var objectsWithinBound = GameObjectsInScene.Where((obj) => {
            if (obj.Value.coordinates.x.InRange(bounds.North, bounds.South) &&
            obj.Value.coordinates.y.InRange(bounds.West, bounds.East))
                return true;

            return false;
        });

        foreach (var obj in objectsWithinBound) {
            ObjectsToLoad.Enqueue(obj.Value);
        }
    }

    protected virtual void queryObjectsWithinCoordinateBounds(Vector2dBounds bounds) {
        // must be overriden
    }

    protected virtual void loadObject(CoordinateBoundObject obj) {
        // must be overridens
    }



}
