using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BlockerPolygonPreview : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private bool showPreview = true;
    [SerializeField] private string resourcePath = "Data/Map/ConvexBlockerMap";

    [Header("Draw")]
    [SerializeField] private float drawHeight = 0.1f;
    [SerializeField] private Vector3 worldOffset = Vector3.zero;
    [SerializeField] private Color lineColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color vertexColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float vertexRadius = 0.12f;
    [SerializeField] private bool showVertexIndex = false;

    [Header("Debug")]
    [SerializeField] private bool autoReload = true;

    [SerializeField, HideInInspector] private string loadedTextCache = string.Empty;

    private readonly List<List<Vector2>> polygons = new List<List<Vector2>>();

    private void OnEnable()
    {
        ReloadFromResource();
    }

    private void OnValidate()
    {
        if (autoReload)
        {
            ReloadFromResource();
        }
    }

    private void OnDrawGizmos()
    {
        if (!showPreview)
        {
            return;
        }

        if (autoReload)
        {
            TryReloadIfChanged();
        }

        if (polygons.Count == 0)
        {
            return;
        }

        DrawPolygons();
    }

    [ContextMenu("Reload Preview")]
    public void ReloadFromResource()
    {
        polygons.Clear();

        TextAsset textAsset = LoadTextAsset();
        if (textAsset == null)
        {
            loadedTextCache = string.Empty;
            return;
        }

        loadedTextCache = textAsset.text ?? string.Empty;
        ParsePolygons(loadedTextCache);
    }

    private void TryReloadIfChanged()
    {
        TextAsset textAsset = LoadTextAsset();
        string latestText = textAsset != null ? textAsset.text ?? string.Empty : string.Empty;
        if (latestText == loadedTextCache)
        {
            return;
        }

        polygons.Clear();
        loadedTextCache = latestText;

        if (!string.IsNullOrEmpty(latestText))
        {
            ParsePolygons(latestText);
        }
    }

    private TextAsset LoadTextAsset()
    {
#if UNITY_EDITOR
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        string normalizedPath = resourcePath.Replace('\\', '/').Trim();
        if (normalizedPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
        {
            return AssetDatabase.LoadAssetAtPath<TextAsset>(normalizedPath);
        }

        string[] candidatePaths =
        {
            normalizedPath,
            $"Assets/{normalizedPath}.txt",
            $"Assets/Resources/{normalizedPath}.txt",
            $"Assets/Resources_moved/{normalizedPath}.txt",
        };

        for (int i = 0; i < candidatePaths.Length; i++)
        {
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(candidatePaths[i]);
            if (textAsset != null)
            {
                return textAsset;
            }
        }
#endif

        return null;
    }

    private void ParsePolygons(string rawText)
    {
        string[] lines = rawText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("count=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            List<Vector2> polygon = ParsePolygonLine(line);
            if (polygon.Count >= 3)
            {
                polygons.Add(polygon);
            }
        }
    }

    private List<Vector2> ParsePolygonLine(string line)
    {
        List<Vector2> polygon = new List<Vector2>();
        string[] pointTokens = line.Split(';');

        for (int i = 0; i < pointTokens.Length; i++)
        {
            string pointToken = pointTokens[i].Trim();
            if (string.IsNullOrEmpty(pointToken))
            {
                continue;
            }

            string[] xy = pointToken.Split(',');
            if (xy.Length != 2)
            {
                continue;
            }

            if (!float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
            {
                continue;
            }

            if (!float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                continue;
            }

            polygon.Add(new Vector2(x, z));
        }

        return polygon;
    }

    private void DrawPolygons()
    {
        Gizmos.color = lineColor;

        for (int polygonIndex = 0; polygonIndex < polygons.Count; polygonIndex++)
        {
            List<Vector2> polygon = polygons[polygonIndex];
            for (int pointIndex = 0; pointIndex < polygon.Count; pointIndex++)
            {
                Vector3 current = ToWorldPoint(polygon[pointIndex]);
                Vector3 next = ToWorldPoint(polygon[(pointIndex + 1) % polygon.Count]);

                Gizmos.color = lineColor;
                Gizmos.DrawLine(current, next);

                Gizmos.color = vertexColor;
                Gizmos.DrawWireSphere(current, vertexRadius);

#if UNITY_EDITOR
                if (showVertexIndex)
                {
                    Handles.Label(current + Vector3.up * 0.1f, $"{polygonIndex}:{pointIndex}");
                }
#endif
            }
        }
    }

    private Vector3 ToWorldPoint(Vector2 point)
    {
        return new Vector3(point.x, drawHeight, point.y) + worldOffset;
    }
}
