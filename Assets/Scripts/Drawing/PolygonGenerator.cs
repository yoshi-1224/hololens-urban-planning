using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TriangleNetForPolygon.Geometry;
// the TriangleNet source code is different to the one I downloaded.
// So the one I downloaded is renamed to TriangleNet4

public static class PolygonGenerator {

    /// <summary>
    /// generates a polygon from given vertices. should be called from DrawingManager
    /// </summary>
	public static GameObject GeneratePolygonFromVertices(List<Vector3> points, float height, Material polygonMat, out Dictionary<int, int> neighbouringVertexMapping) {

        GameObject polygon = new GameObject("Polygon");
        MeshFilter mf = polygon.AddComponent<MeshFilter>();
        MeshRenderer mr = polygon.AddComponent<MeshRenderer>();
        Mesh mesh = mf.mesh;
        mesh.Clear();

        // these values are set in Triangulate method.
        List<Vector3> bottomSurfaceVertices = null;
        List<int> topSurfaceTriangles = null; // this is top not bottom as the triangles face up

        List<Vector2> pointsIn2d = vector3Tovector2(points);
        Triangulate(pointsIn2d, out topSurfaceTriangles, out bottomSurfaceVertices);

        fixHeightwrtMap(bottomSurfaceVertices);
        List<Vector3> topSurfaceVertices = generateVerticesAboveHeight(bottomSurfaceVertices, 0.06f);

        Vector3[] finalVertices = concatTwoArrays(bottomSurfaceVertices.ToArray(), topSurfaceVertices.ToArray());
        mesh.vertices = finalVertices;
        // use mesh.setVertices(List<Vector3>()) => faster

        // reverse the triangles for the bottom one to make it visible from bottom
        List<int> bottomSurfaceTriangles = createReversedTriangles(topSurfaceTriangles);

        // change the indexing of the triangles for the second array (shift)
        int bottomSurfaceVerticesLength = bottomSurfaceVertices.Count;
        int bottomSurfaceTrianglesLength = bottomSurfaceTriangles.Count;
        for (int i = 0; i < bottomSurfaceTrianglesLength; i++) {
                bottomSurfaceTriangles.Add(topSurfaceTriangles[i] + bottomSurfaceVerticesLength);
        }

        neighbouringVertexMapping = GenerateNeighbouringVectorMapping(points, bottomSurfaceVertices);

        mesh.triangles = concatTwoArrays(bottomSurfaceTriangles.ToArray(), generateWallsTriangles(finalVertices, neighbouringVertexMapping));

        mr.material = polygonMat;
        Vector2[] uvs = new Vector2[mesh.vertices.Length];
        for (int i = 0; i < uvs.Length; i++) {
            uvs[i] = new Vector2(mesh.vertices[i].x, mesh.vertices[i].y);
        }
    
        mesh.uv = uvs;
        correctPositionAtCentre(polygon);
        polygon.AddComponent<MeshCollider>().convex = true;
        return polygon;
    }

    /// <summary>
    /// generates a dictionary that maps index of a vertex to the index of its neighbouring vertex (clockwise)
    /// This is required because the vertices list created by Triangulate() method can be shuffled,
    /// meaning that the predicate "vertex at index i is a neighbour to vertex at index i + 1" no longer holds
    /// </summary>
    public static Dictionary<int, int> GenerateNeighbouringVectorMapping(List<Vector3> original, List<Vector3> shuffled) {
        Dictionary<int, int> neighbourMap = new Dictionary<int, int>(original.Count);
        string precisionFormat = "F4";

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
            findNeighbour[vector.ToString(precisionFormat)] = neighbour.ToString(precisionFormat);
        }

        Dictionary<string, int> findIndexToVector = new Dictionary<string, int>();
        for (int i = 0; i < shuffled.Count; i++) {
            Vector3 vector = shuffled[i];
            vector.y = 0;
            findIndexToVector[vector.ToString(precisionFormat)] = i;
        }

        for (int i = 0; i < shuffled.Count; i++) {
            Vector3 vector = shuffled[i];
            vector.y = 0;
            int neighbourIndex = findIndexToVector[findNeighbour[vector.ToString(precisionFormat)]];
            neighbourMap[i] = neighbourIndex;
        }
        return neighbourMap;
    }

    private static List<int> createReversedTriangles(List<int> triangles) {
        if (triangles.Count % 3 != 0)
            return null;

        List<int> reversedTriangles = new List<int>(triangles.Count);
        for (int i = 0; i < triangles.Count; i += 3) {
            reversedTriangles.Add(triangles[i]);
            reversedTriangles.Add(triangles[i + 2]);
            reversedTriangles.Add(triangles[i + 1]);
        }

        return reversedTriangles;
    }

    private static void fixHeightwrtMap(List<Vector3> points) {
        float mapHeight = GameObject.Find(GameObjectNamesHolder.NAME_MAP_COLLIDER).transform.position.y;
        for (int i = 0; i < points.Count; i++) {
            Vector3 tempVector = points[i];
            tempVector.y = mapHeight;
            points[i] = tempVector;
        }
    }

    public static void debugInside(Vector3[] list) {
        for (int i = 0; i < list.Length; i++)
            Debug.Log("value at [" + i + "] = " + list[i].ToString("F3"));
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
        polygon.transform.position = rendererCentre;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    /// <summary>
    /// generates triangles to render the walls of the polygon. 
    /// Precondition: the first half of the vertices array must contain the top surface vertices,
    /// while the second half contains the bottom vertices whose indices correspond with the top ones.
    /// (i.e. top-surface vertex at index i has a bottom-surface vertex at index (i + halfLength) )
    /// </summary>
    private static int[] generateWallsTriangles(Vector3[] vertices, Dictionary<int, int> neighbourMap) {
        List<int> triangles = new List<int>(vertices.Length * 6); // 2 * 3 = 6
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

        List<int> reversedTriangles = createReversedTriangles(triangles);
        return concatTwoArrays(triangles.ToArray(), reversedTriangles.ToArray());
    }

    private static List<Vector2> vector3Tovector2(List<Vector3> vector3s) {
        List<Vector2> vector2s = new List<Vector2>(vector3s.Count);
        for(int i= 0; i < vector3s.Count; i++) {
            vector2s.Add(new Vector2(vector3s[i].x, vector3s[i].z));
        }
        return vector2s;
    }

    private static List<Vector3> generateVerticesAboveHeight(List<Vector3> vertices, float height) {
        // capacity of a list is different to length/size.
        // capacity does not allocate null values
        List<Vector3> newVertices = new List<Vector3>(vertices.Count);
        for (int i = 0; i < vertices.Count; i++) {
            newVertices.Add(vertices[i] + new Vector3(0, height, 0));
        }

        return newVertices;
    }

    /// <summary>
    /// triangulates any polygon-forming set of vertices. uses Triangle.NET
    /// </summary>
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

    private static T[] concatTwoArrays<T>(T[] first, T[] second) {
        T[] newArray = new T[first.Length + second.Length];
        int i = 0;
        for (; i < first.Length; i++) {
            newArray[i] = first[i];
        }

        for (int j = 0; i < newArray.Length; i++, j++) {
            newArray[i] = second[j];
        }

        return newArray;
    }
}
