using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

public class BlockerGeneratorEditor : EditorWindow
{
    private const int DefaultMapWidth = 128;
    private const int DefaultMapHeight = 128;
    private const float BlockHeightThreshold = 0.1f;
    private const float RaycastStartHeight = 1000f;
    private const float RaycastDistance = 2000f;
    private const string OutputDirectory = "Resources/Data/Map";
    private const string OutputFileName = "GridBlockerMap.txt";
    private const string ConvexOutputFileName = "ConvexBlockerMap.txt";

    private int mapWidth = DefaultMapWidth;
    private int mapHeight = DefaultMapHeight;
    private bool[,] gridBlockers = new bool[DefaultMapWidth, DefaultMapHeight];

    [MenuItem("Tools/Blocker Generator")]
    public static void ShowWindow()
    {
        GetWindow<BlockerGeneratorEditor>("阻挡生成工具");
    }

    private void OnGUI()
    {
        GUILayout.Label("底层阻挡生成工具", EditorStyles.boldLabel);

        EditorGUILayout.Space();
        EditorGUILayout.BeginVertical("box");
        DrawMapSizeFields();

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("生成格子阻挡", GUILayout.Height(30f)))
        {
            GenerateGridBlocker();
        }

        EditorGUILayout.Space(6f);

        if (GUILayout.Button("生成凸边型阻挡", GUILayout.Height(30f)))
        {
            GenerateConvexEdgeBlocker();
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox("当前仅提供工具入口，具体生成逻辑后续接入。", MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private void DrawMapSizeFields()
    {
        mapWidth = EditorGUILayout.IntField("场景宽度", mapWidth);
        mapHeight = EditorGUILayout.IntField("场景高度", mapHeight);

        if (mapWidth < 1)
        {
            mapWidth = 1;
        }

        if (mapHeight < 1)
        {
            mapHeight = 1;
        }
    }

    private void GenerateGridBlocker()
    {
        EnsureGridBuffer();

        for (int z = 0; z < mapHeight; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float height = GetSceneHeight(x, z);
                gridBlockers[x, z] = height > BlockHeightThreshold;
            }
        }

        SaveGridBlockerToFile();
        Debug.Log(BuildGridBlockerLog());
    }

    private void GenerateConvexEdgeBlocker()
    {
        if (!LoadGridBlockerFromFile())
        {
            return;
        }

        List<List<Vector2>> polygons = BuildConvexPolygonsFromGrid();
        SaveConvexPolygonsToFile(polygons);
        Debug.Log($"[BlockerTool] 已生成凸多边形数量: {polygons.Count}");
    }

    private void EnsureGridBuffer()
    {
        if (gridBlockers.GetLength(0) == mapWidth && gridBlockers.GetLength(1) == mapHeight)
        {
            return;
        }

        gridBlockers = new bool[mapWidth, mapHeight];
    }

    private float GetSceneHeight(float x, float z)
    {
        Vector3 rayOrigin = new Vector3(x, RaycastStartHeight, z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, RaycastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            return hit.point.y;
        }

        Terrain terrain = Terrain.activeTerrain;
        if (terrain != null)
        {
            Vector3 terrainPosition = terrain.transform.position;
            Vector3 terrainSize = terrain.terrainData.size;
            bool insideTerrain =
                x >= terrainPosition.x &&
                z >= terrainPosition.z &&
                x <= terrainPosition.x + terrainSize.x &&
                z <= terrainPosition.z + terrainSize.z;

            if (insideTerrain)
            {
                return terrain.SampleHeight(new Vector3(x, 0f, z)) + terrainPosition.y;
            }
        }

        return 0f;
    }

    private string BuildGridBlockerLog()
    {
        StringBuilder builder = new StringBuilder(mapWidth * (mapHeight + 1));
        builder.AppendLine("[BlockerTool] 格子阻挡生成结果:");

        for (int z = mapHeight - 1; z >= 0; z--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                builder.Append(gridBlockers[x, z] ? '*' : ' ');
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private void SaveGridBlockerToFile()
    {
        string outputDirectory = Path.Combine(Application.dataPath, OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        string outputPath = Path.Combine(outputDirectory, OutputFileName);
        StringBuilder builder = new StringBuilder(mapWidth * (mapHeight + 1));

        for (int z = mapHeight - 1; z >= 0; z--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                builder.Append(gridBlockers[x, z] ? '1' : '0');
            }

            builder.AppendLine();
        }

        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        Debug.Log($"[BlockerTool] 阻挡文本已保存: {outputPath}");
        AssetDatabase.Refresh();
    }

    private bool LoadGridBlockerFromFile()
    {
        string inputPath = Path.Combine(Application.dataPath, OutputDirectory, OutputFileName);
        if (!File.Exists(inputPath))
        {
            Debug.LogWarning($"[BlockerTool] 未找到阻挡文件: {inputPath}");
            return false;
        }

        string[] lines = File.ReadAllText(inputPath).Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        List<string> validLines = new List<string>();
        foreach (string line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                validLines.Add(line.Trim());
            }
        }

        if (validLines.Count == 0)
        {
            Debug.LogWarning($"[BlockerTool] 阻挡文件为空: {inputPath}");
            return false;
        }

        mapHeight = validLines.Count;
        mapWidth = validLines[0].Length;
        EnsureGridBuffer();

        for (int row = 0; row < mapHeight; row++)
        {
            string line = validLines[row];
            int mapZ = mapHeight - 1 - row;
            for (int x = 0; x < mapWidth; x++)
            {
                gridBlockers[x, mapZ] = x < line.Length && line[x] == '1';
            }
        }

        return true;
    }

    private List<List<Vector2>> BuildConvexPolygonsFromGrid()
    {
        bool[,] visited = new bool[mapWidth, mapHeight];
        List<List<Vector2>> polygons = new List<List<Vector2>>();

        for (int z = 0; z < mapHeight; z++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (!gridBlockers[x, z] || visited[x, z])
                {
                    continue;
                }

                List<Vector2Int> regionCells = CollectConnectedRegion(x, z, visited);
                List<Vector2> polygon = BuildConvexHull(regionCells);
                if (polygon.Count >= 3)
                {
                    polygons.Add(polygon);
                }
            }
        }

        return polygons;
    }

    private List<Vector2Int> CollectConnectedRegion(int startX, int startZ, bool[,] visited)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startZ));
        visited[startX, startZ] = true;

        while (queue.Count > 0)
        {
            Vector2Int cell = queue.Dequeue();
            cells.Add(cell);

            TryEnqueueRegionCell(cell.x + 1, cell.y, visited, queue);
            TryEnqueueRegionCell(cell.x - 1, cell.y, visited, queue);
            TryEnqueueRegionCell(cell.x, cell.y + 1, visited, queue);
            TryEnqueueRegionCell(cell.x, cell.y - 1, visited, queue);
        }

        return cells;
    }

    private void TryEnqueueRegionCell(int x, int z, bool[,] visited, Queue<Vector2Int> queue)
    {
        if (x < 0 || x >= mapWidth || z < 0 || z >= mapHeight)
        {
            return;
        }

        if (!gridBlockers[x, z] || visited[x, z])
        {
            return;
        }

        visited[x, z] = true;
        queue.Enqueue(new Vector2Int(x, z));
    }

    private List<Vector2> BuildConvexHull(List<Vector2Int> regionCells)
    {
        List<Vector2> points = new List<Vector2>(regionCells.Count * 4);
        for (int i = 0; i < regionCells.Count; i++)
        {
            Vector2Int cell = regionCells[i];
            float minX = cell.x;
            float minZ = cell.y;
            float maxX = cell.x + 1f;
            float maxZ = cell.y + 1f;

            points.Add(new Vector2(minX, minZ));
            points.Add(new Vector2(maxX, minZ));
            points.Add(new Vector2(maxX, maxZ));
            points.Add(new Vector2(minX, maxZ));
        }

        points.Sort(ComparePoints);

        List<Vector2> lower = new List<Vector2>();
        for (int i = 0; i < points.Count; i++)
        {
            Vector2 point = points[i];
            while (lower.Count >= 2 && Cross(lower[lower.Count - 2], lower[lower.Count - 1], point) <= 0f)
            {
                lower.RemoveAt(lower.Count - 1);
            }

            if (lower.Count == 0 || lower[lower.Count - 1] != point)
            {
                lower.Add(point);
            }
        }

        List<Vector2> upper = new List<Vector2>();
        for (int i = points.Count - 1; i >= 0; i--)
        {
            Vector2 point = points[i];
            while (upper.Count >= 2 && Cross(upper[upper.Count - 2], upper[upper.Count - 1], point) <= 0f)
            {
                upper.RemoveAt(upper.Count - 1);
            }

            if (upper.Count == 0 || upper[upper.Count - 1] != point)
            {
                upper.Add(point);
            }
        }

        if (lower.Count > 0)
        {
            lower.RemoveAt(lower.Count - 1);
        }

        if (upper.Count > 0)
        {
            upper.RemoveAt(upper.Count - 1);
        }

        lower.AddRange(upper);
        return lower;
    }

    private int ComparePoints(Vector2 a, Vector2 b)
    {
        int compareX = a.x.CompareTo(b.x);
        if (compareX != 0)
        {
            return compareX;
        }

        return a.y.CompareTo(b.y);
    }

    private float Cross(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private void SaveConvexPolygonsToFile(List<List<Vector2>> polygons)
    {
        string outputDirectory = Path.Combine(Application.dataPath, OutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        string outputPath = Path.Combine(outputDirectory, ConvexOutputFileName);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"count={polygons.Count}");

        for (int i = 0; i < polygons.Count; i++)
        {
            List<Vector2> polygon = polygons[i];
            for (int j = 0; j < polygon.Count; j++)
            {
                Vector2 point = polygon[j];
                builder.Append(point.x.ToString("0.###", CultureInfo.InvariantCulture));
                builder.Append(",");
                builder.Append(point.y.ToString("0.###", CultureInfo.InvariantCulture));

                if (j < polygon.Count - 1)
                {
                    builder.Append(";");
                }
            }

            builder.AppendLine();
        }

        File.WriteAllText(outputPath, builder.ToString(), Encoding.UTF8);
        Debug.Log($"[BlockerTool] 凸多边形文件已保存: {outputPath}");
        AssetDatabase.Refresh();
    }
}
