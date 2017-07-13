using UnityEngine;
using Mapbox.Map;
using Mapbox.Unity.Map;
using System;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;
using System.Collections.Generic;
using System.Collections;

public class CustomRangeTileProvider : AbstractTileProvider {
    [SerializeField]
    Vector4 _preLoadedRange;

    static Vector2 _visibleRange = new Vector2(2, 2);

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

    private TileRangeLimits _currentRange;

    public enum Direction {
        East,
        West,
        North,
        South
    }
    public event Action OnAllTilesLoaded;
    private bool AtStart = true;

    /// <summary>
    /// UnityTiles will put the pair into this dictionary
    /// </summary>
    public static Dictionary<string, GameObject> InstantiatedTiles { get; set; }

    internal override void OnInitialized() {
        InstantiatedTiles = new Dictionary<string, GameObject>();
        _currentRange = TileRangeLimits.Initial;
        UpdateCurrentRange();
        StartCoroutine("LoadNewTiles");
        OnAllTilesLoaded += OnAllTilesLoadedHandler;
    }

    public void PanTowards(Direction direction) {
        if (direction == Direction.East || direction == Direction.West) {
            bool isEast = (direction == Direction.East);
            int colIdToDelete = isEast ? _currentRange.minXId : _currentRange.maxXId;

            deleteColFromMap(colIdToDelete);
            updateRangeLimits(direction);
            updateMapCenterMercatorAndCenterCoord();
            shiftTiles(direction);
            StartCoroutine(addNewColToMap(isEast));

        } else if (direction == Direction.North || direction == Direction.South) {
            bool isNorth = (direction == Direction.North);
            int rowIdToDelete = isNorth ? _currentRange.maxYId : _currentRange.minYId;

            deleteRowFromMap(rowIdToDelete);
            updateRangeLimits(direction);
            updateMapCenterMercatorAndCenterCoord();
            shiftTiles(direction);
            StartCoroutine(addNewRowToMap(isNorth));
        }

    }

    private void updateRangeLimits(Direction direction) {
        switch(direction) {
            case Direction.North:
                --_currentRange.maxYId;
                --_currentRange.minYId;
                break;
            case Direction.South:
                ++_currentRange.maxYId;
                ++_currentRange.minYId;
                break;
            case Direction.East:
                ++_currentRange.maxXId;
                ++_currentRange.minXId;
                break;
            case Direction.West:
                --_currentRange.maxXId;
                --_currentRange.minXId;
                break;
        }
    }

    private IEnumerator addNewColToMap(bool isEast) {
        int colIdToAdd = isEast ? _currentRange.maxXId: _currentRange.minXId;

        for (int i = _currentRange.minYId; i <= _currentRange.maxYId; i++) {
            UnwrappedTileId tileToAdd = new UnwrappedTileId(CustomMap.Instance.Zoom, colIdToAdd, i);
            AddTile(tileToAdd);
            yield return null;
        }
    }

    private IEnumerator addNewRowToMap(bool isNorth) {
        int rowIdToAdd = isNorth ? _currentRange.minYId: _currentRange.maxYId;

        for (int i = _currentRange.minXId; i <= _currentRange.maxXId; i++) {
            UnwrappedTileId tileToAdd = new UnwrappedTileId(CustomMap.Instance.Zoom, i, rowIdToAdd);
            AddTile(tileToAdd);
            yield return null;
        }
    }

    private void deleteColFromMap(int colIdToDelete) {
        for (int i = _currentRange.minYId; i <= _currentRange.maxYId; i++) {
            UnwrappedTileId tileToDelete = new UnwrappedTileId(CustomMap.Instance.Zoom, colIdToDelete, i);
            RemoveTile(tileToDelete);
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
        foreach (string key in InstantiatedTiles.Keys) {
            GameObject tileObject = InstantiatedTiles[key];
            string[] tileIds = key.Split('/');
            int xOffset = int.Parse(tileIds[1]) - centerTileId.X;
            int yOffset = int.Parse(tileIds[2]) - centerTileId.Y;
            Vector3 newLocalPosition = new Vector3(xOffset * CustomMap.Instance.UnityTileLocalSize,                                 0, -yOffset * CustomMap.Instance.UnityTileLocalSize);
            tileObject.transform.localPosition = newLocalPosition;

            AdjustVisibility(xOffset, yOffset, tileObject);
        }
    }

    private static void AdjustVisibility(int xOffset, int yOffset, GameObject tileObject) {
        // hide this object if not within the visible range by setting its layer
        if (Math.Abs(xOffset) <= _visibleRange.x && Math.Abs(yOffset) <= _visibleRange.y) {
            tileObject.layer = GameObjectNamesHolder.LAYER_VISIBLE_TILES;
        } else {
            tileObject.layer = GameObjectNamesHolder.LAYER_INVISIBLE_TILES;
        }
    }

    public static void CacheTileObject(UnwrappedTileId tileId, GameObject tileObject) {
        InstantiatedTiles[tileId.ToString()] = tileObject;
        AdjustVisibility(tileObject);
    }

    private static void AdjustVisibility(GameObject tileObject) {
        string[] tileIds = tileObject.name.Split('/');
        var centerTileId = CustomMap.Instance.CenterTileId;
        int xOffset = int.Parse(tileIds[1]) - centerTileId.X;
        int yOffset = int.Parse(tileIds[2]) - centerTileId.Y;
        AdjustVisibility(xOffset, yOffset, tileObject);
    }

    private void deleteRowFromMap(int rowIdToDelete) {
        for (int i = _currentRange.minXId; i <= _currentRange.maxXId; i++) {
            UnwrappedTileId tileToDelete = new UnwrappedTileId(CustomMap.Instance.Zoom, i, rowIdToDelete);
            RemoveTile(tileToDelete);
        }
    }

    /// <summary>
    /// updates CenterMercator in CustomMap.Instance so that new tiles can be added
    /// in the correct positions relative to the parent.
    /// Assumption is that there are 9 tiles in total, the map being 3 by 3. 
    /// </summary>
    private void updateMapCenterMercatorAndCenterCoord() {
        int newCenterXId = (_currentRange.minXId + _currentRange.maxXId) / 2;
        int newCenterYId = (_currentRange.minYId + _currentRange.maxYId) / 2;
        UnwrappedTileId centerTile = new UnwrappedTileId(CustomMap.Instance.Zoom, newCenterXId, newCenterYId);
        var referenceTileRect = Conversions.TileBounds(centerTile);

        CustomMap.Instance.CenterMercator = referenceTileRect.Center;
        CustomMap.Instance.CenterLatitudeLongitude = Conversions.TileIdToCenterLatitudeLongitude(centerTile.X, centerTile.Y, CustomMap.Instance.Zoom);
    }

    public void ChangeZoom(ZoomDirection zoom) {
        if (!isNextZoomLevelWithinLimit(zoom))
            return;

        if (zoom == ZoomDirection.In) {
            CustomMap.Instance.Zoom += zoomResponsiveness;
        } else if (zoom == ZoomDirection.Out) {
            CustomMap.Instance.Zoom -= zoomResponsiveness;
        }
        CustomMap.Instance.UnityTileLocalSize = (CustomMap.Instance.UnityTileSize) / (transform.localScale.x);
        RemoveAllTiles();
        UpdateCurrentRange();
        StartCoroutine("LoadNewTiles");
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

    public void UpdateCurrentRange() {
        var centerTile = CustomMap.Instance.CenterTileId;
        _currentRange.minXId = (int)(centerTile.X - _preLoadedRange.x);
        _currentRange.maxXId = (int)(centerTile.X + _preLoadedRange.z);
        _currentRange.minYId = (int)(centerTile.Y - _preLoadedRange.y);
        _currentRange.maxYId = (int)(centerTile.Y + _preLoadedRange.w);
    }

    /// <summary>
    /// returns the coordinates bounds for the map in (SW, NE) format
    /// </summary>
    public Vector2dBounds GetMapGeoBounds() {
        Vector2dBounds northEastTileBounds = Conversions.TileIdToBounds(_currentRange.minXId, _currentRange.maxYId, CustomMap.Instance.Zoom);
        Vector2dBounds southWestTileBounds = Conversions.TileIdToBounds(_currentRange.maxXId, _currentRange.minYId, CustomMap.Instance.Zoom);
        return new Vector2dBounds(southWestTileBounds.SouthWest, northEastTileBounds.NorthEast);
    }

    /// <summary>
    /// for debugging LocationHelper.geoCoordinateToWorldPosition
    /// </summary>
    public void PrintPositions() {
        foreach(UnwrappedTileId id in _activeTiles) {
            Debug.Log(id.ToString() + " :" + LocationHelper.geoCoordinateToWorldPosition(Conversions.TileIdToCenterLatitudeLongitude(id.X, id.Y, id.Z)) + " = " + GameObject.Find(id.ToString()).transform.position);
        }
    }

    /// <summary>
    /// for debugging LocationHelper.WorldPositionToGeoCoordinate
    /// </summary>
    public void PrintCoordinates() {
        foreach(Transform child in GetComponentsInChildren<Transform>()) {
            if (child != transform) {
                string[] names = child.gameObject.name.Split('/');
                Debug.Log(child.gameObject.name + " :" + Conversions.TileIdToCenterLatitudeLongitude(int.Parse(names[1]), int.Parse(names[2]), int.Parse(names[0])) + " " + LocationHelper.worldPositionToGeoCoordinate(child.position));
            }
        }
    }

    /// <summary>
    /// loads new tiles upon zoom or change of area in the background
    /// Note that in Unity, Coroutines are checked every Update()
    /// </summary>
    internal IEnumerator LoadNewTiles() {
        var centerTile = CustomMap.Instance.CenterTileId;
        yield return new WaitForSeconds(0.1f); // to get rid of the blinking effect

        for (int x = (int)(centerTile.X - _preLoadedRange.x); x <= (centerTile.X + _preLoadedRange.z); x++) {
            for (int y = (int)(centerTile.Y - _preLoadedRange.y); y <= (centerTile.Y + _preLoadedRange.w); y++) {
                AddTile(new UnwrappedTileId(_map.Zoom, x, y));
                yield return null; // stop here and resume at next frame
            }
        }

        if (AtStart)
            OnAllTilesLoaded.Invoke();
    }

    public void OnAllTilesLoadedHandler() {
        AtStart = false;
        InteractibleMap.Instance.PlacementStart();
    }

}
