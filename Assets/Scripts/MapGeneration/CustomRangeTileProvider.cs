using UnityEngine;
using Mapbox.Map;
using Mapbox.Unity.Map;
using System;
using Mapbox.Utils;
using Mapbox.Unity.Utilities;

public class CustomRangeTileProvider : AbstractTileProvider {
    [SerializeField]
    Vector4 _range;

    [SerializeField]
    private int zoomResponsiveness = 1;

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
        public int maxXCoord;
        public int minXCoord;
        public int maxYCoord;
        public int minYCoord;
        public TileRangeLimits(int maxX, int minX, int maxY, int minY) {
            this.maxXCoord = maxX;
            this.minXCoord = minX;
            this.maxYCoord = maxY;
            this.minYCoord = minY;
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

    public event EventHandler<MapStatsEventArgs> MapStatsChanged;

    internal override void OnInitialized() {
        _currentRange = TileRangeLimits.Initial;

        var centerTile = TileCover.CoordinateToTileId(_map.CenterLatitudeLongitude, _map.Zoom);
        for (int x = (int)(centerTile.X - _range.x); x <= (centerTile.X + _range.z); x++) {
            for (int y = (int)(centerTile.Y - _range.y); y <= (centerTile.Y + _range.w); y++) {
                AddTile(new UnwrappedTileId(_map.Zoom, x, y));
            }
        }

        _currentRange.minXCoord = (int)(centerTile.X - _range.x);
        _currentRange.maxXCoord = (int)(centerTile.X + _range.z);
        _currentRange.minYCoord = (int)(centerTile.Y - _range.y);
        _currentRange.maxYCoord = (int)(centerTile.Y + _range.w);
    }

    public void PanTowards(Direction direction) {
        if (direction == Direction.East || direction == Direction.West) {
            bool isEast = (direction == Direction.East);
            int colIdToDelete = isEast ? _currentRange.minXCoord : _currentRange.maxXCoord;

            deleteColFromMap(colIdToDelete);
            updateRangeLimits(direction);
            updateMapCenterMercatorAndCenterCoord();
            shiftTiles(direction);
            addNewColToMap(isEast);

        } else if (direction == Direction.North || direction == Direction.South) {
            bool isNorth = (direction == Direction.North);
            int rowIdToDelete = isNorth ? _currentRange.maxYCoord : _currentRange.minYCoord;

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
                --_currentRange.maxYCoord;
                --_currentRange.minYCoord;
                break;
            case Direction.South:
                ++_currentRange.maxYCoord;
                ++_currentRange.minYCoord;
                break;
            case Direction.East:
                ++_currentRange.maxXCoord;
                ++_currentRange.minXCoord;
                break;
            case Direction.West:
                --_currentRange.maxXCoord;
                --_currentRange.minXCoord;
                break;
        }
    }
    private void addNewColToMap(bool isEast) {
        int colIdToAdd = isEast ? _currentRange.maxXCoord: _currentRange.minXCoord;

        for (int i = _currentRange.minYCoord; i <= _currentRange.maxYCoord; i++) {
            UnwrappedTileId tileToAdd = new UnwrappedTileId(CustomMap.Instance.Zoom, colIdToAdd, i);
            AddTile(tileToAdd);
        }
        Debug.Log("Added " + colIdToAdd + " cols");
    }

    private void addNewRowToMap(bool isNorth) {
        int rowIdToAdd = isNorth ? _currentRange.minYCoord: _currentRange.maxYCoord;

        for (int i = _currentRange.minXCoord; i <= _currentRange.maxXCoord; i++) {
            UnwrappedTileId tileToAdd = new UnwrappedTileId(CustomMap.Instance.Zoom, i, rowIdToAdd);
            AddTile(tileToAdd);
        }
        Debug.Log("Added " + rowIdToAdd + " rows");
    }

    private void deleteColFromMap(int colIdToDelete) {
        for (int i = _currentRange.minYCoord; i <= _currentRange.maxYCoord; i++) {
            UnwrappedTileId tileToDelete = new UnwrappedTileId(CustomMap.Instance.Zoom, colIdToDelete, i);
            RemoveTile(tileToDelete);
        }
    }

    private void shiftTiles(Direction direction) {
        Vector3 shiftAmount = Vector3.zero;
        float unityTileSize = 1.5f; // from CustomMap
        switch(direction) {
            // shift in different direction to the pan
            case Direction.West:
                shiftAmount.x += unityTileSize;
                break;
            case Direction.East:
                shiftAmount.x -= unityTileSize;
                break;
            case Direction.North:
                shiftAmount.z -= unityTileSize;
                break;
            case Direction.South:
                shiftAmount.z += unityTileSize;
                break;
        }

        foreach(Transform child in GetComponentsInChildren<Transform>()) {
            if (child != transform)
                child.position += shiftAmount; // just the tiles and their children
        }
    }

    private void deleteRowFromMap(int rowIdToDelete) {
        for (int i = _currentRange.minXCoord; i <= _currentRange.maxXCoord; i++) {
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
        int xCoord = (_currentRange.minXCoord + _currentRange.maxXCoord) / 2;
        int yCoord = (_currentRange.minYCoord + _currentRange.maxYCoord) / 2;
        UnwrappedTileId centerTile = new UnwrappedTileId(CustomMap.Instance.Zoom, xCoord, yCoord);
        var referenceTileRect = Conversions.TileBounds(centerTile);

        CustomMap.Instance.CenterMercator = referenceTileRect.Center;
        CustomMap.Instance.CenterLatitudeLongitude = Conversions.TileIdToCenterLatitudeLongitude(centerTile.X, centerTile.Y, CustomMap.Instance.Zoom);
        Debug.Log("Center tile is of id = " + centerTile);
    }

    public void ChangeZoom(ZoomDirection zoom) {
        RemoveAllTiles();
        if (zoom == ZoomDirection.In) {
            CustomMap.Instance.Zoom += zoomResponsiveness;
        } else if (zoom == ZoomDirection.Out) {
            CustomMap.Instance.Zoom -= zoomResponsiveness;
        }
        OnInitialized(); // just a hack to reload just as in Start()
    }
}
