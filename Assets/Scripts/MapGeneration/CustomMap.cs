using Mapbox.Unity.Map;
using System;
using Mapbox.Unity.MeshGeneration;
using Mapbox.Unity.Utilities;
using Mapbox.Utils;
using UnityEngine;
using Mapbox.Map;
using Mapbox.Unity;

[RequireComponent(typeof(AbstractTileProvider))]

public class CustomMap : HoloToolkit.Unity.Singleton<CustomMap>, IMap {
    [Geocode]
    [SerializeField]
    string _latitudeLongitudeString;

    [SerializeField]
    int _zoom;
    public int Zoom {
        get {
            return _zoom;
        }
        set {
            // update the world scale as well
            _zoom = value;
            var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_mapCenterLatitudeLongitude, _zoom));
            _mapCenterMercator = referenceTileRect.Center;
            _worldRelativeScale = (float)(_unityTileSize / referenceTileRect.Size.x);
            Root.localScale = Vector3.one * _worldRelativeScale;
        }
    }

    [SerializeField]
    Transform _root;
    public Transform Root {
        get {
            return _root;
        }
    }

    [SerializeField]
    AbstractTileProvider _tileProvider;

    [SerializeField]
    MapVisualizer _mapVisualizer;

    [SerializeField]
    float _unityTileSize = 1.5f;

    MapboxAccess _fileSource;

    Vector2d _mapCenterLatitudeLongitude;
    public Vector2d CenterLatitudeLongitude {
        get {
            return _mapCenterLatitudeLongitude;
        }
        set {
            _latitudeLongitudeString = string.Format("{0}, {1}", value.x, value.y);
            _mapCenterLatitudeLongitude = value;
        }
    }

    Vector2d _mapCenterMercator;
    public Vector2d CenterMercator {
        get {
            return _mapCenterMercator;
        }
        set {
            _mapCenterMercator = value;
        }
    }

    float _worldRelativeScale;
    public float WorldRelativeScale {
        get {
            return _worldRelativeScale;
        }
    }

    public event Action OnInitialized = delegate { };

    protected override void Awake() {
        base.Awake();
        _fileSource = MapboxAccess.Instance;
        _tileProvider.OnTileAdded += TileProvider_OnTileAdded;
        _tileProvider.OnTileRemoved += TileProvider_OnTileRemoved;
        _tileProvider.OnZoomChanged += TileProvider_OnZoomChanged;
        if (!_root) {
            _root = transform;
        }
    }

    private void TileProvider_OnZoomChanged() {
        _mapVisualizer.OnZoomChanged();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (_tileProvider != null) {
            _tileProvider.OnTileAdded -= TileProvider_OnTileAdded;
            _tileProvider.OnTileRemoved -= TileProvider_OnTileRemoved;
            _tileProvider.OnZoomChanged -= TileProvider_OnZoomChanged;
        }

        _mapVisualizer.Destroy();
    }

    protected virtual void Start() {
        var latLonSplit = _latitudeLongitudeString.Split(',');
        _mapCenterLatitudeLongitude = new Vector2d(double.Parse(latLonSplit[0]), double.Parse(latLonSplit[1]));

        var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_mapCenterLatitudeLongitude, _zoom));
        _mapCenterMercator = referenceTileRect.Center;

        _worldRelativeScale = (float)(_unityTileSize / referenceTileRect.Size.x);
        Root.localScale = Vector3.one * _worldRelativeScale;

        _mapVisualizer.Initialize(this, _fileSource);
        _tileProvider.Initialize(this);

        OnInitialized(); // use this event for something
    }

    protected void TileProvider_OnTileAdded(UnwrappedTileId tileId) {
        // event handler
        _mapVisualizer.LoadTile(tileId);
    }

    protected void TileProvider_OnTileRemoved(UnwrappedTileId tileId) {
        // event handler
        _mapVisualizer.DisposeTile(tileId);
    }


}
