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
            _zoom = value;
            UpdateOnNewZoomLevelSet();
            MapDataDisplay.Instance.UpdateZoomInfo(_zoom);
            OnZoomChanged();
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

    public float UnityTileSize = 1.5f;

    MapboxAccess _fileSource;

    private Vector2d _mapCenterLatitudeLongitude;
    public Vector2d CenterLatitudeLongitude {
        get {
            return _mapCenterLatitudeLongitude;
        }
        set {
            _latitudeLongitudeString = string.Format("{0}, {1}", value.x, value.y);
            _mapCenterLatitudeLongitude = value;
            MapDataDisplay.Instance.UpdateCenterCoordinatesInfo(_latitudeLongitudeString);
        }
    }

    private Vector2d _mapCenterMercator;
    public Vector2d CenterMercator {
        get {
            return _mapCenterMercator;
        }
        set {
            _mapCenterMercator = value;
        }
    }

    public UnwrappedTileId CenterTileId {
        get {
            return TileCover.CoordinateToTileId(CenterLatitudeLongitude, Zoom);
        }
    }

    float _worldRelativeScale;
    public float WorldRelativeScale {
        get {
            return _worldRelativeScale;
        }

        private set {
            _worldRelativeScale = value;
            // including the fact that the parent is also scaled
            MapDataDisplay.Instance.UpdateWorldRelativeScaleInfo(_worldRelativeScale * transform.parent.transform.localScale.x);
        }
    }

    public event Action OnInitialized = delegate { };

    protected override void Awake() {
        base.Awake();
        _fileSource = MapboxAccess.Instance;
        _tileProvider.OnTileAdded += TileProvider_OnTileAdded;
        _tileProvider.OnTileRemoved += TileProvider_OnTileRemoved;
        if (!_root) {
            _root = transform;
        }
    }

    public float UnityTileLocalSize {
        get {
            return (UnityTileSize) / (transform.localScale.x);
        }
    }

    private void OnZoomChanged() {
        _mapVisualizer.OnZoomChanged();
        BuildingsPlacement.Instance.OnZoomChanged();
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        if (_tileProvider != null) {
            _tileProvider.OnTileAdded -= TileProvider_OnTileAdded;
            _tileProvider.OnTileRemoved -= TileProvider_OnTileRemoved;
        }

        _mapVisualizer.Destroy();
    }

    protected virtual void Start() {
        var latLonSplit = _latitudeLongitudeString.Split(',');
        CenterLatitudeLongitude = new Vector2d(double.Parse(latLonSplit[0]), double.Parse(latLonSplit[1]));
        Zoom = Zoom; // hack to invoke the body of setter
        CorrectCenterLatitudeLongitude();
        _mapVisualizer.Initialize(this, _fileSource);
        _tileProvider.Initialize(this);
        OnInitialized(); // use this event for something
    }

    /// <summary>
    /// updates mapCenterMercator and localScale in response to a new zoom value set
    /// </summary>
    private void UpdateOnNewZoomLevelSet() {
        var referenceTileRect = Conversions.TileBounds(TileCover.CoordinateToTileId(_mapCenterLatitudeLongitude, _zoom));
        CenterMercator = referenceTileRect.Center;
        WorldRelativeScale = (float)(UnityTileSize / referenceTileRect.Size.x);
        Root.localScale = Vector3.one * _worldRelativeScale;
        CorrectCenterLatitudeLongitude();
    }

    /// <summary>
    /// corrects and adjusts the mapCenterLatLong at the start and after zoom as the user-specified
    /// string is unlikely to be the actual center coordinates for the center tile at the start,
    /// or after zoom the coordinates much be updated
    /// </summary>
    private void CorrectCenterLatitudeLongitude() {
        UnwrappedTileId CenterTile = TileCover.CoordinateToTileId(_mapCenterLatitudeLongitude, _zoom);
        CenterLatitudeLongitude = Conversions.TileIdToCenterLatitudeLongitude(CenterTile.X, CenterTile.Y, CenterTile.Z);
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
