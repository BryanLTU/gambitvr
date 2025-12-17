using System.Collections.Generic;
using UnityEngine;

public static class MeshPreviewCache
{
    static readonly Dictionary<GameObject, (MeshFilter, Renderer)[]> _cache = new();

    public static (MeshFilter, Renderer)[] Get(GameObject go)
    {
        if (go == null)
            return null;

        if (_cache.TryGetValue(go, out var cached))
            return cached;

        var filters = go.GetComponentsInChildren<MeshFilter>(true);
        var list = new List<(MeshFilter, Renderer)>();

        foreach (var mf in filters)
        {
            if (!mf || !mf.sharedMesh)
                continue;

            var mr = mf.GetComponent<Renderer>();
            if (!mr || !mr.enabled)
                continue;

            list.Add((mf, mr));
        }

        cached = list.ToArray();
        _cache[go] = cached;
        return cached;
    }
}
