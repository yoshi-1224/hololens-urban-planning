using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNet.Geometry;
//using Dynagon;

public static class PolygonGenerator {

    /// <summary>
    /// generates a polygon from given vertices. should be called from DrawingManager
    /// </summary>
	public static GameObject GeneratePolygonFromVertices(List<Vector3> bottomSurfacePoints, float height, Material polygonMat) {
        GameObject polygon = new GameObject("Polygon");
        int count = bottomSurfacePoints.Count;

        MeshFilter mf = polygon.AddComponent<MeshFilter>();
        MeshRenderer mr = polygon.AddComponent<MeshRenderer>();
        polygon.AddComponent<MeshCollider>();
        Mesh mesh = mf.mesh;
        mesh.Clear();

        // these values are set in Triangulate method
        List<int> bottomSurfaceTriangles = null;
        List<Vector3> bottomSurfaceVertices = null;
        List<int> topSurfaceTriangles = null;
        List<Vector3> topSurfaceVertices = null;

        //this does not do anything as we are ignoring the y-values of vertices anyways (so merely creates a copy)
        List<Vector3> topSurfacePoints = generateVerticesAboveHeight(bottomSurfacePoints, 0);

        List<Vector2> userGivenPoints2d = vector3Tovector2(bottomSurfacePoints);
        List<Vector2> topSurfacePoints2d = vector3Tovector2(topSurfacePoints);

        Triangulate(userGivenPoints2d, out bottomSurfaceTriangles, out bottomSurfaceVertices);
        Triangulate(topSurfacePoints2d, out topSurfaceTriangles, out topSurfaceVertices);

        correctHeightwrtMap(bottomSurfaceVertices);
        topSurfaceVertices = generateVerticesAboveHeight(bottomSurfaceVertices, 0.1f);

        Vector3[] finalVertices = concatTwoArrays(bottomSurfaceVertices.ToArray(), topSurfaceVertices.ToArray());

        mesh.vertices = finalVertices;

        // reverse the triangles for the bottom one to make it visible from bottom
        reverseTriangles(bottomSurfaceTriangles);

        // change the indexing of the triangles for the second array (shift)
        int bottomSurfaceVerticesLength = bottomSurfaceVertices.Count;
        for (int i = 0; i < topSurfaceTriangles.Count; i++) {
            bottomSurfaceTriangles.Add(topSurfaceTriangles[i] + bottomSurfaceVerticesLength);
        }

        mesh.triangles = concatTwoArrays(bottomSurfaceTriangles.ToArray(), generateWallsTriangles(finalVertices, generateNeighbouringVectorMapping(topSurfacePoints, topSurfaceVertices)));

        mr.material = polygonMat;
        Vector2[] uvs = new Vector2[mesh.vertices.Length];
        for (int i = 0; i < uvs.Length; i++) {
            uvs[i] = new Vector2(mesh.vertices[i].x, mesh.vertices[i].y);
        }
    
        mesh.uv = uvs;
        correctPositionAtCentre(polygon);
        return polygon;
    }

    private static Dictionary<int, int> generateNeighbouringVectorMapping(List<Vector3> original, List<Vector3> shuffled) {
        Dictionary<int, int> neighbourMap = new Dictionary<int, int>(original.Count);

        Dictionary<string, string> findNeighbour = new Dictionary<string, string>(original.Count);
        for (int i = 0; i < original.Count; i++) {
            bool isLastIndex = (i == original.Count - 1);
            Vector3 vector = original[i];
            vector.y = 0;
            Vector3 neighbour;
            if (isLastIndex) {
                neighbour = original[0];
            } else {
                neighbour = original[i + 1];
            }
            neighbour.y = 0;
            findNeighbour[vector.ToString("F1")] = neighbour.ToString("F1");
        }

        Dictionary<string, int> findIndexToVector = new Dictionary<string, int>();
        for (int i = 0; i < shuffled.Count; i++) {
            Vector3 vector = shuffled[i];
            vector.y = 0;
            findIndexToVector[vector.ToString("F1")] = i;
        }

        for (int i = 0; i < shuffled.Count; i++) {
            Vector3 vector = shuffled[i];
            vector.y = 0;
            int neighbourIndex = findIndexToVector[findNeighbour[vector.ToString("F1")]];
            neighbourMap[i] = neighbourIndex;
        }
        return neighbourMap;
    }

    private static void reverseTriangles(List<int> triangles) {
        if (triangles.Count % 3 != 0)
            return;
        Debug.Log("reversing triangles");
        for (int i = 0; i < triangles.Count; i += 3) {
            int secondIndex = triangles[i + 1];
            triangles[i + 1] = triangles[i + 2];
            triangles[i + 2] = secondIndex;
        }
    }

    private static void correctHeightwrtMap(List<Vector3> points) {
        float mapHeight = GameObject.Find("CustomizedMap").transform.position.y;
        Debug.Log("Map height = " + mapHeight);
        for (int i = 0; i < points.Count; i++) {
            Vector3 tempVector = points[i];
            tempVector.y = mapHeight;
            points[i] = tempVector;
        }
    }

    private static void debugInside(Vector3[] list) {
        for (int i = 0; i < list.Length; i++)
            Debug.Log("value at [" + i + "] = " + list[i]);
    }

    private static void correctPositionAtCentre(GameObject polygon) {
        Renderer renderer = polygon.GetComponent<Renderer>();
        Vector3 rendererCentre = renderer.bounds.center;
        Vector3 rendererExtents = renderer.bounds.extents;
        rendererCentre.y -= rendererExtents.y;
        Vector3 moveVector = rendererCentre - polygon.transform.position;
        var mesh = polygon.GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        polygon.transform.position = rendererCentre;

        for (int i = 0; i < vertices.Length; i++) {
            vertices[i] -= moveVector;
        }
        mesh.vertices = vertices; // must be reassigned
        Debug.Log("render center is at " + rendererCentre);
        polygon.transform.position = rendererCentre;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // optinal: correct the meshCollider if already attached
        Object.Destroy(polygon.GetComponent<MeshCollider>());
        polygon.AddComponent<MeshCollider>().convex = true;
    }

    private static void generateTopSurfaceMesh() {

    }

    private static void generateBottomSurfaceMesh() {

    }

    private static int[] generateWallsTriangles(Vector3[] vertices, Dictionary<int, int> neighbourMap) {
        List<int> triangles = new List<int>(vertices.Length * 6);
        int halfWay = vertices.Length / 2;
        for (int i = 0; i < halfWay; i++) {
            int neighbourIndex = neighbourMap[i];
                triangles.Add(i);
                triangles.Add(neighbourIndex);
                triangles.Add(neighbourIndex + halfWay);

                triangles.Add(i);
                triangles.Add(neighbourIndex + halfWay);
                triangles.Add(i + halfWay);
        }

        List<int> reversedTriangles = new List<int>(triangles.Count);
        for (int i = 0; i < triangles.Count; i++) {
            reversedTriangles.Add(triangles[i]);
        } // just copy
        reverseTriangles(reversedTriangles);

        return concatTwoArrays(triangles.ToArray(), reversedTriangles.ToArray());
    }

    private static List<Vector2> vector3Tovector2(List<Vector3> vector3s) {
        List<Vector2> vector2s = new List<Vector2>(vector3s.Count);
        for(int i= 0; i < vector3s.Count; i++) {
            vector2s.Add(new Vector2(vector3s[i].x, vector3s[i].z));
        }
        return vector2s;
    }

    /// <summary>
    /// use this to clone as well
    /// </summary>
    private static List<Vector3> generateVerticesAboveHeight(List<Vector3> vertices, float height) {
        // capacity of a list is different to length/size.
        // capacity does not allocate null values
        List<Vector3> newVertices = new List<Vector3>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++) {
            newVertices.Add(vertices[i] + new Vector3(0, height, 0));
        }

        return newVertices;
    }

    private static bool Triangulate(List<Vector2> points, out List<int> outPolygonTriangles, out List<Vector3> outPolygonVertices) {
        outPolygonTriangles = new List<int>();
        outPolygonVertices = new List<Vector3>();

        Polygon polygon = new Polygon();
        for (int i = 0; i < points.Count; i++) {
            polygon.Add(new Vertex(points[i].x, points[i].y));

            bool isLastPoint = (i == points.Count - 1);
            if (isLastPoint) {
                polygon.Add(new Segment(new Vertex(points[i].x, points[i].y), new Vertex(points[0].x, points[0].y)));
            }
            else {
                polygon.Add(new Segment(new Vertex(points[i].x, points[i].y), new Vertex(points[i + 1].x, points[i + 1].y)));
            }
        }

        var mesh = polygon.Triangulate();

        foreach (ITriangle triangle in mesh.Triangles) {
            for (int j = 2; j >= 0; j--) {
                // triangles have three sides
                bool found = false;
                for (int k = 0; k < outPolygonVertices.Count; k++) {
                    if (outPolygonVertices[k].x == triangle.GetVertex(j).X && outPolygonVertices[k].z == triangle.GetVertex(j).Y) {
                        outPolygonTriangles.Add(k);
                        found = true;
                        break;
                    }
                }

                // first one where polygonVertices.Count == 0 is always false
                if (!found) {
                    outPolygonVertices.Add(new Vector3((float)triangle.GetVertex(j).X, 0, (float)triangle.GetVertex(j).Y));
                    outPolygonTriangles.Add(outPolygonVertices.Count - 1);
                }
            }
        }

        return true;

    }

    /// <summary>
    /// list of vertices of the plane surface. Must be in clockwise order
    /// </summary>
    /// <param name="vertices"></param>
    private static void verticalPlaneGenerator(List<Vector3> vertices) {
        /// Order of vertices: in clockwise
        /// 3   0
        ///   
        /// 2   1 
        ///



    }

    private static T[] concatTwoArrays<T>(T[] first, T[] second) {
        T[] newArray = new T[first.Length + second.Length];
        int i = 0;
        for (; i < first.Length; i++) {
            newArray[i] = first[i];
        }

        for (int j= 0; i < newArray.Length; i++, j++) {
            newArray[i] = second[j];
        }

        return newArray;
    }
}
