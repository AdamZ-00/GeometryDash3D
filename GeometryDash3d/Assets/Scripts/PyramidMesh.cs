using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class PyramidMesh : MonoBehaviour
{
    void OnEnable()
    {
        GenerateMesh();
    }

    void GenerateMesh()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        MeshCollider mc = GetComponent<MeshCollider>();

        Mesh mesh = new Mesh();
        mesh.name = "PyramidMesh";

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0f, 1f, 0f)
        };

        int[] triangles = new int[]
        {
            0, 1, 2,
            0, 2, 3,
            0, 4, 1,
            1, 4, 2,
            2, 4, 3,
            3, 4, 0
        };

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        // assign mesh au MeshFilter et au MeshCollider
        mf.sharedMesh = mesh;
        mc.sharedMesh = mesh;
        mc.convex = true;
    }
}
