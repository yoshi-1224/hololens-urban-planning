#region Abstract
using UnityEngine;
using UnityEditor;

/// <summary>
/// PivotManager made by Caue Rego
///  completely extracted from SetPivot by Yilmaz Kiymaz (@VoxelBoy) and meant to be a simplified version of it.
/// This finds the object pivot through its mesh bounds and then moves the pivot to the object center  by moving its transform position in one direction and then moving all vertices of the mesh in the opposite direction.
/// WTFPL
/// 2012-06-05
/// </summary>
public class PivotManager {

    static GameObject selectedObject;
    static Mesh selectedObjectMesh;
    static Vector3 selectedObjectPivot;

    [MenuItem("GameObject/Center Pivot %#&c")]
    [MenuItem("CONTEXT/Transform/Center Pivot %#&c")]
    static void CenterPivot() {
        RecognizeSelectedObject();
        if (CheckSelectedObject()) {
            Debug.Log("Pivot " + selectedObjectPivot + "  -  Bounds " + selectedObjectMesh.bounds.ToString());
            CenterObjectPivot();
        }
    }

    [MenuItem("GameObject/Create Child Pivot %#&p")]
    [MenuItem("CONTEXT/Transform/Create Child Pivot %#&p")]
    static void CreateChildPivot() {
        RecognizeSelectedObject();
        if (CheckSelectedObject()) {
            Debug.Log("Pivot Created " + selectedObjectPivot);
            CreateObjectPivot();
        }
    }

    /// <summary>
    /// When a selection change notification is received - this is an Editor predefined function.
    /// </summary>
    static void OnSelectionChange() {
        RecognizeSelectedObject();
    }

    #endregion
    #region Auxiliary

    static bool CheckSelectedObject() {
        if (!selectedObject) {
            Debug.Log("No object selected in Hierarchy.");
            return false;
        }
        if (!selectedObjectMesh) {
            Debug.Log("Selected object does not have a Mesh specified.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Gather references for the selected object and its components
    ///  and update the pivot vector if the object has a Mesh.
    /// </summary>
    static void RecognizeSelectedObject() {
        selectedObjectMesh = null;

        Transform recognizedTransform = Selection.activeTransform;
        if (recognizedTransform) {
            selectedObject = recognizedTransform.gameObject;
            if (selectedObject) {

                MeshFilter selectedObjectMeshFilter = selectedObject.GetComponent<MeshFilter>();
                if (selectedObjectMeshFilter) {
                    selectedObjectMesh = selectedObjectMeshFilter.sharedMesh;
                    if (selectedObjectMesh) {
                        selectedObjectPivot = FindObjectPivot(selectedObjectMesh.bounds);
                    }
                }

            }
        }
    }

    /// <summary>
    /// The 'center' parameter of certain colliders need to be adjusted when the transform position is modified.
    /// </summary>
    static void FixColliders(Vector3 scaleDiff) {
        Collider selectedObjectCollider = selectedObject.GetComponent<Collider>();

        if (selectedObjectCollider) {
            if (selectedObjectCollider is BoxCollider) {
                ((BoxCollider)selectedObjectCollider).center += scaleDiff;
            }
            else if (selectedObjectCollider is CapsuleCollider) {
                ((CapsuleCollider)selectedObjectCollider).center += scaleDiff;
            }
            else if (selectedObjectCollider is SphereCollider) {
                ((SphereCollider)selectedObjectCollider).center += scaleDiff;
            }
            // missing calculation to compensate for MeshCollider
        }

        selectedObjectPivot = Vector3.zero;
    }

    #endregion
    #region Main

    /// <summary>
    /// Moves the Object's Pivot into the Object's Center thus centering the Pivot!  \o/
    /// Few experiments shows this doesn't quite work on FBX's
    ///  because it will move the Object into the Pivot instead.
    /// Either way, now we can rotate the object around its own center.
    /// </summary>
    static void CenterObjectPivot() {
        // Move object position by taking localScale into account
        selectedObject.transform.position -= Vector3.Scale(selectedObjectPivot, selectedObject.transform.localScale);

        // Iterate over all vertices and move them in the opposite direction of the object position movement
        Vector3[] verts = selectedObjectMesh.vertices;
        for (int i = 0; i < verts.Length; i++) {
            verts[i] += selectedObjectPivot;
        }
        selectedObjectMesh.vertices = verts; //Assign the vertex array back to the mesh
        selectedObjectMesh.RecalculateBounds(); //Recalculate bounds of the mesh, for the renderer's sake

        FixColliders(selectedObjectPivot);
    }

    /// <summary>
    /// Creates a Pivot Mirror as child of Selected Object
    ///  which is used just as a reference
    /// </summary>
    static void CreateObjectPivot() {
        GameObject pivotReference = new GameObject();
        pivotReference.name = selectedObject.name + ".PivotReference";
        pivotReference.transform.position = selectedObjectPivot;
        pivotReference.transform.parent = selectedObject.transform;
    }

    /// <summary>
    /// Calculate the pivot position by comparing its bounds center offset with its extents.
    /// The bounds may come (for instance) from mesh, renderer or collider.
    /// </summary>
    static public Vector3 FindObjectPivot(Bounds bounds) {
        Vector3 offset = -1 * bounds.center;
        Vector3 extent = new Vector3(offset.x / bounds.extents.x, offset.y / bounds.extents.y, offset.z / bounds.extents.z);
        return Vector3.Scale(bounds.extents, extent);
    }
}
#endregion