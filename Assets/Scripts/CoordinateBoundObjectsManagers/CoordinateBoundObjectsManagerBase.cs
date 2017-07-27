using System.Collections.Generic;
using UnityEngine;
using Mapbox.Utils;
using Mapbox.Map;
using Mapbox.Unity.Utilities;
using System;

/// <summary>
/// This struct is used to store the reference to a map-bound game object together with its reference.
/// </summary>
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


/// <summary>
/// This is the base class for managers that store references to game objects of a particular type.
/// </summary>
public class CoordinateBoundObjectsManagerBase<T> : MonoBehaviour where T : CoordinateBoundObjectsManagerBase<T> {

    private static T instance;
    public static T Instance {
        get {
            return instance;
        }
    }

    /// <summary>
    /// This Dict provides mapping from gameObject.name to CoordinateBoundObject
    /// </summary>
    public Dictionary<string, CoordinateBoundObject> GameObjectsInScene { get; set; }

    /// <summary>
    /// use this queue to load objects one by one each frame after the tiles have been loaded in order to distribute the computing cost
    /// </summary>
    protected Queue<CoordinateBoundObject> ObjectsToLoad;


    protected bool shouldStartLoadingObjects;

    /// <summary>
    /// Returns whether the instance has been initialized or not.
    /// </summary>
    public static bool IsInitialized {
        get {
            return instance != null;
        }
    }

    /// <summary>
    /// Base awake method that sets the singleton's unique instance, initialize data structures and set up event listeners
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
        CustomRangeTileProvider.OnAllTilesAdded += TileProvider_OnAllTilesAdded;
        shouldStartLoadingObjects = false;
    }

    protected virtual void TileProvider_OnAllTilesAdded() {
        shouldStartLoadingObjects = true;
    }

    /// <summary>
    /// event listener for OnTileAdded
    /// </summary>
    protected virtual void TileProvider_OnTileAdded(UnwrappedTileId tileId) {
        queryObjectsWithinTileBounds(tileId);
    }

    protected virtual void Update() {
        if (shouldStartLoadingObjects && ObjectsToLoad.Count > 0) {
            loadObject(ObjectsToLoad.Dequeue());
            if (ObjectsToLoad.Count == 0)
                shouldStartLoadingObjects = false; // no more to load
        }
    }

    protected virtual void OnDestroy() {
        if (instance == this) {
            instance = null;
        }

        ObjectsToLoad = null;
        GameObjectsInScene = null;
        CustomRangeTileProvider.OnTileObjectAdded -= TileProvider_OnTileAdded;
        CustomRangeTileProvider.OnAllTilesAdded -= TileProvider_OnAllTilesAdded;
    }

    protected virtual void queryObjectsWithinTileBounds(UnwrappedTileId tileId) {
        queryObjectsWithinCoordinateBounds(Conversions.TileIdToBounds(tileId));
    }

    /// <summary>
    /// iterates through the Dict values and enqueue game objects whose coordinates fit
    /// within the bound
    /// </summary>
    protected virtual void queryObjectsWithinCoordinateBounds(Vector2dBounds bounds) {
        foreach (CoordinateBoundObject value in GameObjectsInScene.Values) {
            if (value.coordinates.x.InRange(bounds.North, bounds.South) &&
            value.coordinates.y.InRange(bounds.West, bounds.East)) {
                ObjectsToLoad.Enqueue(value);
            }
        }
    }

    /// <summary>
    /// This is to be overriden by the child classes
    /// </summary>
    protected virtual void loadObject(CoordinateBoundObject obj) {
    }

    /// <summary>
    /// This is to be overriden by the child classes
    /// </summary>
    public virtual void OnZoomChanged() {
    }

}
