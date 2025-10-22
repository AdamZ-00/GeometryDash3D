#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class PortalSceneCreator : EditorWindow
{
    [MenuItem("Tools/GD3D/Create Portals In Scene")]
    public static void ShowWin()
    {
        var w = GetWindow<PortalSceneCreator>("Create Portals");
        w.minSize = new Vector2(260, 120);
    }

    private float outerRadius = 0.9f;
    private float innerRadius = 0.6f;
    private float depth = 0.30f;

    void OnGUI()
    {
        GUILayout.Label("Hex Ring Params", EditorStyles.boldLabel);
        outerRadius = EditorGUILayout.Slider("Outer Radius", outerRadius, 0.6f, 1.3f);
        innerRadius = EditorGUILayout.Slider("Inner Radius", innerRadius, 0.3f, 1.2f);
        depth = EditorGUILayout.Slider("Depth", depth, 0.1f, 0.6f);

        if (GUILayout.Button("Create 3 Portals (Accel / Slow / Neutral)"))
            CreateAll();
    }

    void CreateAll()
    {
        // positions espacées pour bien les voir
        CreatePortal("Portal_Accel",   new Color(0.85f,0.10f,0.10f), SpeedPortal.PortalKind.Accelerate, 1.25f, new Vector3(-4,0,0));
        CreatePortal("Portal_Slow",    new Color(0.12f,0.45f,0.95f), SpeedPortal.PortalKind.Slow,       0.75f, new Vector3( 0,0,0));
        CreatePortal("Portal_Neutral", new Color(0.95f,0.90f,0.35f), SpeedPortal.PortalKind.Neutral,    1.00f, new Vector3( 4,0,0));
        Debug.Log("[PortalSceneCreator] 3 portails créés (rouge/bleu/jaune).");
        Selection.activeObject = GameObject.Find("Portal_Accel");
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    void CreatePortal(string name, Color col, SpeedPortal.PortalKind kind, float mult, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;

        // Mesh hex ring
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = GenerateHexRingMesh(outerRadius, innerRadius, depth);

        var mr = go.AddComponent<MeshRenderer>();
        var mat = CreatePortalMaterial(col);
        mr.sharedMaterial = mat;

        // Collider trigger
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.8f, 2.0f, depth + 0.05f);

        // Logic + spin + light
        var sp = go.AddComponent<SpeedPortal>();
        sp.kind = kind;
        sp.multiplier = mult;
        sp.transitionDuration = 0.2f;
        sp.playerTag = "Player";

        var spin = go.AddComponent<Spin>();
        spin.axis = Vector3.up;
        spin.speed = 30f;

        var lightGO = new GameObject("Light");
        lightGO.transform.SetParent(go.transform, false);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = col;
        light.intensity = 2f;
        light.range = 6f;

        // Pose sur le sol (pivot au bas)
        var p = go.transform.position;
        go.transform.position = new Vector3(p.x, 0f, p.z);
    }

    // --- Pipeline-aware material creation ---
    static Material CreatePortalMaterial(Color baseCol)
    {
        bool isURP = GraphicsSettings.currentRenderPipeline != null;
        Shader sh = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        if (sh == null)
        {
            // fallback ultime pour éviter le magenta
            var fallback = new Material(Shader.Find("Standard"));
            fallback.color = baseCol;
            fallback.EnableKeyword("_EMISSION");
            fallback.SetColor("_EmissionColor", baseCol * 2.0f);
            return fallback;
        }

        var mat = new Material(sh);
        if (isURP)
        {
            mat.SetColor("_BaseColor", baseCol);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseCol * 2.0f);
        }
        else
        {
            // Built-in Standard
            mat.color = baseCol;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseCol * 2.0f);
            // Petite brillance soft
            mat.SetFloat("_Glossiness", 0.35f);
            mat.SetFloat("_Metallic", 0.0f);
        }
        return mat;
    }

    static Mesh GenerateHexRingMesh(float outerR, float innerR, float depth)
    {
        int n = 6;
        float halfZ = depth * 0.5f;
        Vector3[] outerTop = new Vector3[n];
        Vector3[] innerTop = new Vector3[n];
        Vector3[] outerBot = new Vector3[n];
        Vector3[] innerBot = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            float ang = Mathf.Deg2Rad * (60f * i + 30f);
            float cx = Mathf.Cos(ang), sy = Mathf.Sin(ang);
            outerTop[i] = new Vector3(cx*outerR, sy*outerR, +halfZ);
            innerTop[i] = new Vector3(cx*innerR, sy*innerR, +halfZ);
            outerBot[i] = new Vector3(cx*outerR, sy*outerR, -halfZ);
            innerBot[i] = new Vector3(cx*innerR, sy*innerR, -halfZ);
        }

        var m = new Mesh(); m.name = "HexRing";
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris  = new System.Collections.Generic.List<int>();

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(s+0); tris.Add(s+1); tris.Add(s+2);
            tris.Add(s+0); tris.Add(s+2); tris.Add(s+3);
        }

        // face avant
        for (int i = 0; i < n; i++) { int j = (i+1)%n; Quad(outerTop[i], outerTop[j], innerTop[j], innerTop[i]); }
        // face arrière
        for (int i = 0; i < n; i++) { int j = (i+1)%n; Quad(innerBot[i], innerBot[j], outerBot[j], outerBot[i]); }
        // côtés extérieurs
        for (int i = 0; i < n; i++) { int j = (i+1)%n; Quad(outerTop[i], outerBot[i], outerBot[j], outerTop[j]); }
        // côtés intérieurs
        for (int i = 0; i < n; i++) { int j = (i+1)%n; Quad(innerTop[i], innerTop[j], innerBot[j], innerBot[i]); }

        // pivot au sol : décale pour que le bas soit à y=0
        float minY = float.MaxValue;
        foreach (var v in verts) if (v.y < minY) minY = v.y;
        for (int i = 0; i < verts.Count; i++) verts[i] = new Vector3(verts[i].x, verts[i].y - minY, verts[i].z);

        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }
}
#endif
