using UnityEngine;
using Mapbox.Map;
using Mapbox.Unity.Map;
using System;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using System.Collections.Generic;
using System.Collections;
using HoloToolkit.Unity;

public class CustomRangeTileProvider : AbstractTileProvider {
    [SerializeField]
    Vector4 _preLoadedRange;

    static int visibleRange = 2;

    [SerializeField]
    private int zoomResponsiveness = 1;
    [SerializeField]
    private int maxZoomLevel = 20;
    [SerializeField]
    private int minZoomLevel = 11;

    public enum ZoomDirection {
        In,
        Out
    }
    /// <summary>
    /// used to identify which tile should be loaded next
    /// when the user pans the map in certain direction
    /// Note that when the zoom level changes this field should be updated
    /// appropriately
    /// </summary>
    public struct TileRangeLimits {
        public int maxXId;
        public int minXId;
        public int maxYId;
        public int minYId;
        public TileRangeLimits(int maxX, int minX, int maxY, int minY) {
            this.maxXId = maxX;
            this.minXId = minX;
            this.maxYId = maxY;
            this.minYId = minY;
        }

        public static TileRangeLimits Initial {
            get {
                return new TileRangeLimits(-1, int.MaxValue, -1, int.MaxValue);
            }
        }
    }

    private TileRangeLimits _currentLoadedRange;
    public enum Direction {
        East,
        West,
        North,
        South
    }
    public static event Action OnAllTilesLoaded;
    public static event Action<UnwrappedTileId> OnTileObjectAdded;
    private bool AtStart = true;

    /// <summary>
    /// UnityTiles will put the pair into this dictionary
    /// </summary>
    public static Dictionary<UnwrappedTileId, GameObject> InstantiatedTiles { get; set; }

    internal override void OnInitialized() {
        InstantiatedTiles = new Dictionary<UnwrappedTileId, GameObject>();
        _currentLoadedRange = TileRangeLimits.Initial;
        OnAllTilesLoaded += OnAllTilesLoadedHandler;
        //StartCoroutine(LoadNewTiles());
        LoadNewTilesAtStart();
    }

    public void PanTowards(Direction direction) {
        updateMapCenterMercatorAndCenterCoord(direction);
        shiftTiles(direction);
        StartCoroutine(addVisibleTiles());
    }

    /// <summary>
    /// tries to load all the tiles that should be visible. If the tile already exists 
    /// then it skips and load the next one
    /// </summary>
    /// <returns></returns>
    private IEnumerator addVisibleTiles() {
        var centerTileId = CustomMap.Instance.CenterTileId;
        for (int i = centerTileId.X - visibleRange; i <= centerTileId.X + visibleRange; i++) {
            for (int j = centerTileId.Y - visibleRange; j <= centerTileId.Y + visibleRange; j++) {
                UnwrappedTileId tileToAdd = new UnwrappedTileId(CustomMap.Instance.Zoom, i, j);
                if (InstantiatedTiles.ContainsKey(tileToAdd))
                    continue;
                AddTile(tileToAdd);
                yield return null;
            }
        }
    }

    private void shiftTiles(Direction direction) {
        Vector3 shiftAmount = Vector3.zero;
        var centerTileId = CustomMap.Instance.CenterTileId;
        switch (direction) {
            // shift in different direction to the pan
            case Direction.West:
                shiftAmount.x += CustomMap.Instance.UnityTileLocalSize;
                break;
            case Direction.East:
                shiftAmount.x -= CustomMap.Instance.UnityTileLocalSize;
                break;
            case Direction.North:
                shiftAmount.z -= CustomMap.Instance.UnityTileLocalSize;
                break;
            case Direction.South:
                shiftAmount.z += CustomMap.Instance.UnityTileLocalSize;
                break;
        }
        foreach (UnwrappedTileId key in InstantiatedTiles.Keys) {
            GameObject tileObject = InstantiatedTiles[key];
            int xOffset = key.X - centerTileId.X;
            int yOffset = key.Y - centerTileId.Y;
            Vector3 newLocalPosition = new Vector3(xOffset * CustomMap.Instance.UnityTileLocalSize,                                 0, -yOffset * CustomMap.Instance.UnityTileLocalSize);
            tileObject.transform.localPosition = newLocalPosition;

            // have to access all the tile objects anyways so just do it this way
            AdjustVisibility(xOffset, yOffset, tileObject);
        }
    }

    private static void AdjustVisibility(int xOffset, int yOffset, GameObject tileObject) {
        if (Math.Abs(xOffset) <= visibleRange && Math.Abs(yOffset) <= visibleRange) {
            Utils.SetLayerRecursively(tileObject, GameObjectNamesHolder.LAYER_VISIBLE_TILES);
        } else {
            Utils.SetLayerRecursively(tileObject, GameObjectNamesHolder.LAYER_INVISIBLE_TILES);
        }
    }

    private static void AdjustVisibility(GameObject tileObject) {
        string[] tileIds = tileObject.name.Split('/');
        var centerTileId = CustomMap.Instance.CenterTileId;
        int xOffset = int.Parse(tileIds[1]) - centerTileId.X;
        int yOffset = int.Parse(tileIds[2]) - centerTileId.Y;
        AdjustVisibility(xOffset, yOffset, tileObject);
    }

    public static void CacheTileObject(UnwrappedTileId tileId, GameObject tileObject) {
        InstantiatedTiles[tileId] = tileObject;
        AdjustVisibility(tileObject);
        OnTileObjectAdded.Invoke(tileId);
    }

    /// <summary>
    /// updates CenterMercator in CustomMap.Instance so that new tiles can be added
    /// in the correct positions relative to the parent.
    /// </summary>
    private void updateMapCenterMercatorAndCenterCoord(Direction panDirection) {
        UnwrappedTileId centerTileId = CustomMap.Instance.CenterTileId;
        int newCenterXId = centerTileId.X;
        int newCenterYId = centerTileId.Y;

        // simply increment the Y or X rather than taking the average of the range
        switch(panDirection) {
            case Direction.South:
                newCenterYId++;
                break;
            case Direction.North:
                newCenterYId--;
                break;
            case Direction.East:
                newCenterXId++;
                break;
            case Direction.West:
                newCenterXId--;
                break;
        }

        UnwrappedTileId newCenterTileId = new UnwrappedTileId(CustomMap.Instance.Zoom, newCenterXId, newCenterYId);
        var referenceTileRect = Conversions.TileBounds(newCenterTileId);

        CustomMap.Instance.CenterMercator = referenceTileRect.Center;
        CustomMap.Instance.CenterLatitudeLongitude = Conversions.TileIdToCenterLatitudeLongitude(newCenterTileId.X, newCenterTileId.Y, CustomMap.Instance.Zoom);
    }

    public void ChangeZoom(ZoomDirection zoom) {
        if (!isNextZoomLevelWithinLimit(zoom))
            return;

        if (zoom == ZoomDirection.In) {
            CustomMap.Instance.Zoom += zoomResponsiveness;
        } else if (zoom == ZoomDirection.Out) {
            CustomMap.Instance.Zoom -= zoomResponsiveness;
        }
        RemoveAllTiles();
        StartCoroutine(LoadNewTiles());
    }

    private bool isNextZoomLevelWithinLimit(ZoomDirection zoom) {
        int levelAfterZoom = 0;
        switch (zoom) {
            case ZoomDirection.Out:
                levelAfterZoom = CustomMap.Instance.Zoom - zoomResponsiveness;
                break;
            case ZoomDirection.In:
                levelAfterZoom = CustomMap.Instance.Zoom + zoomResponsiveness;
                break;
        }
        if (levelAfterZoom > maxZoomLevel || levelAfterZoom < minZoomLevel)
            return false;
        else
            return true;
    }

    /// <summary>
    /// loads new tiles upon zoom or change of area in the background
    /// </summary>
    internal IEnumerator LoadNewTiles() {
        var centerTile = CustomMap.Instance.CenterTileId;
        yield return null;

        for (int x = (int)(centerTile.X - _preLoadedRange.x); x <= (centerTile.X + _preLoadedRange.z); x++) {
            for (int y = (int)(centerTile.Y - _preLoadedRange.y); y <= (centerTile.Y + _preLoadedRange.w); y++) {
                AddTile(new UnwrappedTileId(_map.Zoom, x, y));
                yield return null; // stop here and resume at next frame
            }
        }

        OnAllTilesLoaded.Invoke();
    }

    /// <summary>
    /// loads new tiles upon zoom or change of area in the background
    /// </summary>
    internal void LoadNewTilesAtStart() {
        var centerTile = CustomMap.Instance.CenterTileId;

        for (int x = (int)(centerTile.X - _preLoadedRange.x); x <= (centerTile.X + _preLoadedRange.z); x++) {
            for (int y = (int)(centerTile.Y - _preLoadedRange.y); y <= (centerTile.Y + _preLoadedRange.w); y++) {
                AddTile(new UnwrappedTileId(_map.Zoom, x, y));
            }
        }

        OnAllTilesLoaded.Invoke();
    }

    public void OnAllTilesLoadedHandler() {
        if (AtStart) {
            InteractibleMap.Instance.PlacementStart();
            AtStart = false;
        }
    }

}
