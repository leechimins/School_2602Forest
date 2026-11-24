using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Registers the PolyOne trees as terrain tree prototypes and scatters them around the walking path.
// Deterministic (fixed seed) so conditions A and B always share the exact same tree layout.
public static class ForestTreePlacer
{
    const string TerrainDataPath = "Assets/Forest/ForestTerrain.asset";
    const string PrefabFolder = "Assets/PolyOne/Free Tree/Prefabs";
    const int PathLayer = 2;      // Muddy (walking path)
    const int ShoreLayer = 3;     // Pebbles_A (pond shore)
    static readonly Vector2 PondCenter = new Vector2(135f, 125f);
    const float PondClear = 22f;  // pond radius 16 m + shore margin
    const int Seed = 2602;

    [MenuItem("Forest/Place Trees")]
    public static void PlaceTrees()
    {
        var td = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        var prefabs = new List<GameObject>();
        for (int i = 1; i <= 8; i++)
        {
            var p = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/SM_FreeTree_0{i}.prefab");
            if (p == null) { Debug.LogError($"Tree prefab missing: SM_FreeTree_0{i} (import 'Free Pack - Tree')"); return; }
            prefabs.Add(p);
        }

        var protos = new TreePrototype[prefabs.Count];
        var radius = new float[prefabs.Count];
        for (int i = 0; i < prefabs.Count; i++)
        {
            protos[i] = new TreePrototype { prefab = prefabs[i] };
            var b = prefabs[i].GetComponent<Renderer>().bounds;
            radius[i] = Mathf.Max(b.size.x, b.size.z) * 0.5f;
        }

        // Path / shore cells from the splat map (cell centres in world XZ).
        int res = td.alphamapResolution;
        var alpha = td.GetAlphamaps(0, 0, res, res);
        float cell = td.size.x / res;
        var pathCells = new List<Vector2>();
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
                if (alpha[y, x, PathLayer] > 0.3f)
                    pathCells.Add(new Vector2((x + 0.5f) * cell, (y + 0.5f) * cell));

        var rng = new System.Random(Seed);
        var trees = new List<TreeInstance>();
        const float spacing = 4f;
        for (float z = spacing * 0.5f; z < td.size.z; z += spacing)
            for (float x = spacing * 0.5f; x < td.size.x; x += spacing)
            {
                var pos = new Vector2(x + (float)(rng.NextDouble() - 0.5) * spacing * 0.8f,
                                      z + (float)(rng.NextDouble() - 0.5) * spacing * 0.8f);
                pos.x = Mathf.Clamp(pos.x, 1f, td.size.x - 1f);
                pos.y = Mathf.Clamp(pos.y, 1f, td.size.z - 1f);

                int ax = Mathf.Clamp((int)(pos.x / cell), 0, res - 1);
                int ay = Mathf.Clamp((int)(pos.y / cell), 0, res - 1);
                if (alpha[ay, ax, ShoreLayer] > 0.1f) continue;
                if (Vector2.Distance(pos, PondCenter) < PondClear) continue;

                int proto = rng.Next(protos.Length);
                float scale = 3f + (float)rng.NextDouble();          // trees are 1-4 m tall -> 3-17 m
                float dPath = DistanceToPath(pos, pathCells);
                float clear = 2f + radius[proto] * scale;             // keep canopy off the path
                if (dPath < clear) continue;

                // Denser right along the path, thinner farther out (still enclosing the view).
                float keep = dPath < clear + 8f ? 0.85f : dPath < 30f ? 0.55f : 0.3f;
                if (rng.NextDouble() > keep) continue;

                trees.Add(new TreeInstance
                {
                    prototypeIndex = proto,
                    position = new Vector3(pos.x / td.size.x, 0f, pos.y / td.size.z),
                    widthScale = scale,
                    heightScale = scale,
                    rotation = (float)(rng.NextDouble() * Mathf.PI * 2),
                    color = Color.white,
                    lightmapColor = Color.white
                });
            }

        Undo.RegisterCompleteObjectUndo(td, "Place Trees");
        td.treePrototypes = protos;
        td.SetTreeInstances(trees.ToArray(), true);   // snap to terrain height
        EditorUtility.SetDirty(td);
        AssetDatabase.SaveAssetIfDirty(td);
        Debug.Log($"ForestTreePlacer: {trees.Count} trees, {protos.Length} prototypes, {pathCells.Count} path cells");
    }

    static float DistanceToPath(Vector2 p, List<Vector2> pathCells)
    {
        float best = float.MaxValue;
        foreach (var c in pathCells)
        {
            float d = (c - p).sqrMagnitude;
            if (d < best) best = d;
        }
        return Mathf.Sqrt(best);
    }
}
