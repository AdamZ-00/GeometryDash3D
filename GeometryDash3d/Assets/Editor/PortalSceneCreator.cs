#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;

public class PortalSceneCreator : EditorWindow
{
    [MenuItem("Tools/GD3D/Create Portals In Scene")]
    public static void ShowWin()
    {
        var w = GetWindow<PortalSceneCreator>("Create Portals");
        w.minSize = new Vector2(280, 150);
    }

    // --- Params ---
    private float outerRadius = 0.9f;
    private float innerRadius = 0.6f;
    private float depth = 0.30f;

    private const string kFolder = "Assets/GD3D/Generated/Portals";

    void OnGUI()
    {
        GUILayout.Label("Hex Ring Params", EditorStyles.boldLabel);
        outerRadius = EditorGUILayout.Slider("Outer Radius", outerRadius, 0.6f, 1.3f);
        innerRadius = EditorGUILayout.Slider("Inner Radius", innerRadius, 0.3f, 1.2f);
        depth = EditorGUILayout.Slider("Depth (Z)", depth, 0.10f, 0.60f);

        EditorGUILayout.Space();
        if (GUILayout.Button("Create 3 Portals (Accel / Slow / Neutral)", GUILayout.Height(32)))
            CreateAll();
    }

    void CreateAll()
    {
        EnsureFolder(kFolder);

        // Parent (facultatif) : sous l’objet sélectionné sinon racine
        Transform parent = Selection.activeTransform;

        CreatePortal("Portal_Accel", new Color(0.85f, 0.10f, 0.10f), SpeedPortal.PortalKind.Accelerate, 1.25f, new Vector3(-4, 0, 0), parent);
        CreatePortal("Portal_Slow", new Color(0.12f, 0.45f, 0.95f), SpeedPortal.PortalKind.Slow, 0.75f, new Vector3(0, 0, 0), parent);
        CreatePortal("Portal_Neutral", new Color(0.95f, 0.90f, 0.35f), SpeedPortal.PortalKind.Neutral, 1.00f, new Vector3(4, 0, 0), parent);

        Debug.Log("[PortalSceneCreator] 3 portails créés (rouge/bleu/jaune).");
        EditorGUIUtility.PingObject(GameObject.Find("Portal_Accel"));
        Selection.activeObject = GameObject.Find("Portal_Accel");
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    void CreatePortal(string name, Color col, SpeedPortal.PortalKind kind, float mult, Vector3 pos, Transform parent)
    {
        Undo.IncrementCurrentGroup();
        int ug = Undo.GetCurrentGroup();

        // --- GO ---
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create Portal");
        if (parent) go.transform.SetParent(parent);
        go.transform.position = new Vector3(pos.x, 0f, pos.z); // posé au sol

        // --- Mesh & Material en ASSETS (persistants) ---
        var mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();

        var mesh = GetOrCreateHexRingAsset(outerRadius, innerRadius, depth);
        var mat = GetOrCreatePortalMaterial(col);

        mf.sharedMesh = mesh;
        mr.sharedMaterial = mat;

        // --- Collider trigger ---
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(1.8f, 2.0f, depth + 0.05f);

        // --- Logic ---
        var sp = go.AddComponent<SpeedPortal>();
        sp.kind = kind;
        sp.multiplier = mult;
        sp.transitionDuration = 0.2f;
        sp.playerTag = "Player";
        sp.respawnAfterPickup = true; // selon ton besoin
        sp.hideOnUse = true;

        // --- Petit spin pour la vie ---
        var spin = go.AddComponent<Spin>();
        spin.axis = Vector3.up;
        spin.speed = 30f;

        // --- Light enfant ---
        var lightGO = new GameObject("Light");
        Undo.RegisterCreatedObjectUndo(lightGO, "Create Portal Light");
        lightGO.transform.SetParent(go.transform, false);
        var light = lightGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = col;
        light.intensity = 2f;
        light.range = 6f;

        // Final
        EditorUtility.SetDirty(go);
        Undo.CollapseUndoOperations(ug);
    }

    // ---------- Assets helpers ----------

    static void EnsureFolder(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            var parts = folder.Split('/');
            string path = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string sub = path + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(sub))
                    AssetDatabase.CreateFolder(path, parts[i]);
                path = sub;
            }
        }
    }

    static string MeshAssetPath(float outerR, float innerR, float depth)
        => $"{kFolder}/HexRing_{outerR:0.00}_{innerR:0.00}_{depth:0.00}.asset";

    static string MatAssetPath(Color c)
        => $"{kFolder}/Portal_{Mathf.RoundToInt(c.r * 255)}_{Mathf.RoundToInt(c.g * 255)}_{Mathf.RoundToInt(c.b * 255)}.mat";

    Mesh GetOrCreateHexRingAsset(float outerR, float innerR, float d)
    {
        var path = MeshAssetPath(outerR, innerR, d);
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh) return mesh;

        // Génération
        mesh = GenerateHexRingMesh(outerR, innerR, d);
        AssetDatabase.CreateAsset(mesh, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return mesh;
    }

    Material GetOrCreatePortalMaterial(Color baseCol)
    {
        var path = MatAssetPath(baseCol);
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat) return mat;

        bool isURP = GraphicsSettings.currentRenderPipeline != null;
        Shader sh = Shader.Find(isURP ? "Universal Render Pipeline/Lit" : "Standard");
        if (sh == null) sh = Shader.Find("Standard");

        mat = new Material(sh);
        if (isURP)
        {
            mat.SetColor("_BaseColor", baseCol);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseCol * 2.0f);
        }
        else
        {
            mat.color = baseCol;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseCol * 2.0f);
            mat.SetFloat("_Glossiness", 0.35f);
            mat.SetFloat("_Metallic", 0.0f);
        }

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return mat;
    }

    // ---------- Mesh generation (persistant) ----------

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
            outerTop[i] = new Vector3(cx * outerR, sy * outerR, +halfZ);
            innerTop[i] = new Vector3(cx * innerR, sy * innerR, +halfZ);
            outerBot[i] = new Vector3(cx * outerR, sy * outerR, -halfZ);
            innerBot[i] = new Vector3(cx * innerR, sy * innerR, -halfZ);
        }

        var m = new Mesh(); m.name = $"HexRing_{outerR:0.00}_{innerR:0.00}_{depth:0.00}";
        var verts = new System.Collections.Generic.List<Vector3>();
        var tris = new System.Collections.Generic.List<int>();

        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int s = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c); verts.Add(d);
            tris.Add(s + 0); tris.Add(s + 1); tris.Add(s + 2);
            tris.Add(s + 0); tris.Add(s + 2); tris.Add(s + 3);
        }

        // face avant
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; Quad(outerTop[i], outerTop[j], innerTop[j], innerTop[i]); }
        // face arrière
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; Quad(innerBot[i], innerBot[j], outerBot[j], outerBot[i]); }
        // côtés extérieurs
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; Quad(outerTop[i], outerBot[i], outerBot[j], outerTop[j]); }
        // côtés intérieurs
        for (int i = 0; i < n; i++) { int j = (i + 1) % n; Quad(innerTop[i], innerTop[j], innerBot[j], innerBot[i]); }

        // pivot au sol (bas à y=0)
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
