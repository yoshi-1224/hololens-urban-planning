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
    Vector4 _range;

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

    internal override void OnInitialized() {
        _currentRange = TileRangeLimits.Initial;

        var centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.Zoom);
        for (int x = (int)(centerTile.X - _range.x); x <= (centerTile.X + _range.z); x++) {
            for (int y = (int)(centerTile.Y - _range.y); y <= (centerTile.Y + _range.w); y++) {
                AddTile(new UnwrappedTileId(_map.Zoom, x, y));
            }
            // yield return here?
        }

        _currentRange.minXId = (int)(centerTile.X - _range.x);
        _currentRange.maxXId = (int)(centerTile.X + _range.z);
        _currentRange.minYId = (int)(centerTile.Y - _range.y);
        _currentRange.maxYId = (int)(centerTile.Y + _range.w);
    }

    public void PanTowards(Direction direction) {
        if (direction == Direction.East || direction == Direction.West) {
            bool isEast = (direction == Direction.East);
            int colIdToDelete = isEast ? _currentRange.minXId : _currentRange.maxXId;

            deleteColFromMap(colIdToDelete);
            updateRangeLimits(direction);
            updateMapCenterMercatorAndCenterCoord();
            shiftTiles(direction);
            addNewColToMap(isEast);

        } else if (direction == Direction.North || direction == Direction.South) {
            bool isNorth = (direction == Direction.North);
            int rowIdToDelete = isNorth ? _currentRange.maxYId : _currentRange.minYId;

            deleteRowFromMap(rowIdToDelete);
            updateRangeLimits(direction);
            updateMapCenterMercatorAndCenterCoord();
            shiftTiles(direction);
            addNewRowToMap(isNorth);
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

    private void addNewColToMap(bool isEast) {
        int colIdToAdd = isEast ? _currentRange.maxXId: _currentRange.minXId;

        for (int i = _currentRange.minYId; i <= _currentRange.maxYId; i++) {
            UnwrappedTileId tileToAdd = new UnwrappedTileId(CustomMap.Instance.Zoom, colIdToAdd, i);
            AddTile(tileToAdd);
        }
    }

    private void addNewRowToMap(bool isNorth) {
        int rowIdToAdd = isNorth ? _currentRange.minYId: _currentRange.maxYId;

        for (int i = _currentRange.minXId; i <= _currentRange.maxXId; i++) {
            UnwrappedTileId tileToAdd = new UnwrappedTileId(CustomMap.Instance.Zoom, i, rowIdToAdd);
            AddTile(tileToAdd);
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
        float TileSizeInLocalSpace = 0;
        //float TileSizeInLocalSpace = (CustomMap.Instance.UnityTileSize / transform.localScale.x); // from CustomMap
        // find the none-zero transform.position value in Children
        foreach(Transform child in GetComponentsInChildren<Transform>()) {
            if (child != transform) {
                float offset = Math.Abs(child.localPosition.x) < 1? child.localPosition.z : child.localPosition.x;
                if (offset > 1) {
                    TileSizeInLocalSpace = Math.Abs(offset);
                    break;
                }
            }
        }
        switch (direction) {
            // shift in different direction to the pan
            case Direction.West:
                shiftAmount.x += TileSizeInLocalSpace;
                break;
            case Direction.East:
                shiftAmount.x -= TileSizeInLocalSpace;
                break;
            case Direction.North:
                shiftAmount.z -= TileSizeInLocalSpace;
                break;
            case Direction.South:
                shiftAmount.z += TileSizeInLocalSpace;
                break;
        }
        // how about referencing the transform of other tiles and then use
        // that as the shift amount?
        foreach(Transform child in GetComponentsInChildren<Transform>()) {
            if (child != transform)
                // just the tiles and their children, and we want to modify the localPosition
                child.localPosition += shiftAmount;
        }
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
        int xCoord = (_currentRange.minXId + _currentRange.maxXId) / 2;
        int yCoord = (_currentRange.minYId + _currentRange.maxYId) / 2;
        UnwrappedTileId centerTile = new UnwrappedTileId(CustomMap.Instance.Zoom, xCoord, yCoord);
        var referenceTileRect = Conversions.TileBounds(centerTile);

        CustomMap.Instance.CenterMercator = referenceTileRect.Center;
        CustomMap.Instance.CenterLatitudeLongitude = Conversions.TileIdToCenterLatitudeLongitude(centerTile.X, centerTile.Y, CustomMap.Instance.Zoom);
    }

    public void ChangeZoom(ZoomDirection zoom) {
        int levelAfterZoom = zoom == ZoomDirection.Out ? CustomMap.Instance.Zoom - 1 : CustomMap.Instance.Zoom + 1;
        if (levelAfterZoom > maxZoomLevel || levelAfterZoom < minZoomLevel)
            return;

        if (zoom == ZoomDirection.In) {
            CustomMap.Instance.Zoom += zoomResponsiveness;
        } else if (zoom == ZoomDirection.Out) {
            CustomMap.Instance.Zoom -= zoomResponsiveness;
        }
        RemoveAllTiles();
        StartCoroutine("LoadNewTiles");
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
    /// Note that in Unity, Coroutines are run every Update()
    /// </summary>
    internal IEnumerator LoadNewTiles() {
        var centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.Zoom);
        yield return new WaitForSeconds(0.5f); // to get rid of the blinking effect
        for (int x = (int)(centerTile.X - _range.x); x <= (centerTile.X + _range.z); x++) {
            for (int y = (int)(centerTile.Y - _range.y); y <= (centerTile.Y + _range.w); y++) {
                AddTile(new UnwrappedTileId(_map.Zoom, x, y));
                yield return null; // stop here and resume at next frame
            }
        }

        _currentRange.minXId = (int)(centerTile.X - _range.x);
        _currentRange.maxXId = (int)(centerTile.X + _range.z);
        _currentRange.minYId = (int)(centerTile.Y - _range.y);
        _currentRange.maxYId = (int)(centerTile.Y + _range.w);
    }
}
