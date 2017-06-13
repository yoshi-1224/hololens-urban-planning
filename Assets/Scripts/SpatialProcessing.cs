using System.Collections.Generic;
using UnityEngine;
using HoloToolkit.Unity; //added to make it work
using HoloToolkit.Unity.SpatialMapping; //added to make it work

/// <summary>
/// The SpatialProcessingTest class allows applications to scan the environment for a specified amount of time 
/// and then process the Spatial Mapping Mesh (find planes, remove vertices) after that time has expired.
/// </summary>
public class SpatialProcessing : Singleton<SpatialProcessing> {
    [Tooltip("How much time (in seconds) that the SurfaceObserver will run after being started; used when 'Limit Scanning By Time' is checked.")]
    public float scanTime = 30.0f;

    [Tooltip("Material to use when rendering Spatial Mapping meshes while the observer is running.")]
    public Material defaultMaterial;

    [Tooltip("Optional Material to use when rendering Spatial Mapping meshes after the observer has been stopped.")]
    public Material secondaryMaterial;

    [Tooltip("Minimum number of floor planes required in order to exit scanning/processing mode.")]
    public uint minimumFloors = 1;

    [Tooltip("Minimum number of wall planes required in order to exit scanning/processing mode.")]
    public uint minimumWalls = 1;

    private bool meshesProcessed = false;

    private void Start() {
        // Update surfaceObserver and storedMeshes to use the same material during scanning.
        SpatialMappingManager.Instance.SetSurfaceMaterial(defaultMaterial);

        // Register for the MakePlanesComplete event.
        SurfaceMeshesToPlanes.Instance.MakePlanesComplete += SurfaceMeshesToPlanes_MakePlanesComplete;
    }

    private void Update() {
        if (!meshesProcessed) {
            // Check to see if enough scanning time has passed since starting the observer.
            if ((Time.unscaledTime - SpatialMappingManager.Instance.StartTime) < scanTime) {
                // wait
            } else {
                // The user should be done scanning their environment,
                // so start processing the spatial mapping data
                if (SpatialMappingManager.Instance.IsObserverRunning())
                    SpatialMappingManager.Instance.StopObserver();
                    
                CreatePlanes();
                meshesProcessed = true;
            }
        }
    }

    /// <summary>
    /// Handler for the SurfaceMeshesToPlanes MakePlanesComplete event.
    /// </summary>
    private void SurfaceMeshesToPlanes_MakePlanesComplete(object source, System.EventArgs args) {
        // Collection of floor planes that we can use to set horizontal items on.
        List<GameObject> floors = new List<GameObject>();
        List<GameObject> walls = new List<GameObject>();

        // these are NOT instantiated here. The lists are used simply to check if we have enough walls and floors
        floors = SurfaceMeshesToPlanes.Instance.GetActivePlanes(PlaneTypes.Floor);
        walls = SurfaceMeshesToPlanes.Instance.GetActivePlanes(PlaneTypes.Wall);

        if (floors.Count >= minimumFloors && walls.Count >= minimumWalls) {
            RemoveVertices(SurfaceMeshesToPlanes.Instance.ActivePlanes);

            // After scanning is over, switch to the secondary (occlusion) material.
            SpatialMappingManager.Instance.SetSurfaceMaterial(secondaryMaterial);
            disableRoomMesh();
            // hide the scanning message and instantiate the map
            MapPlacement.Instance.InstantiateMap();
        } else {
            // Re-process spatial data after scanning completes since we don't have enough walls and floors
            SpatialMappingManager.Instance.StartObserver();
            meshesProcessed = false;
        }
    }

    /// <summary>
    /// Creates planes from the spatial mapping surfaces.
    /// </summary>
    private void CreatePlanes() {
        // Generate planes based on the spatial map.
        SurfaceMeshesToPlanes surfaceToPlanes = SurfaceMeshesToPlanes.Instance;
        if (surfaceToPlanes != null && surfaceToPlanes.enabled)
            surfaceToPlanes.MakePlanes();
    }

    /// <summary>
    /// Removes triangles from the spatial mapping surfaces.
    /// </summary>
    private void RemoveVertices(IEnumerable<GameObject> boundingObjects) {
        RemoveSurfaceVertices removeVerts = RemoveSurfaceVertices.Instance;
        if (removeVerts != null && removeVerts.enabled)
            removeVerts.RemoveSurfaceVerticesWithinBounds(boundingObjects);
    }

    protected override void OnDestroy() {
        if (SurfaceMeshesToPlanes.Instance != null)
            SurfaceMeshesToPlanes.Instance.MakePlanesComplete -= SurfaceMeshesToPlanes_MakePlanesComplete;

        base.OnDestroy();
    }

    private void disableRoomMesh() {
        foreach(Transform child in GameObject.Find("SpatialMapping").transform) {
            child.gameObject.SetActive(false);
        }
    }
}
