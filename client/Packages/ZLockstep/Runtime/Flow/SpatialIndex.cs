
using System;
using System.Collections.Generic;
using zUnity;


namespace ZLockstep.Flow
{
    /// <summary>
    /// 空间索引数据结构，用于存储实体的空间信息
    /// </summary>
    public class SpatialEntry
    {
        public int EntityId;
        public zVector3 Position;
        public int CampId;
    }

    /// <summary>
    /// 简化的 2D KD-tree 实现，专用于 CombatSystem 空间邻近查询
    /// </summary>
    public class SpatialIndex
    {
        private static readonly SpatialIndex instance_ = new();
        public static SpatialIndex Instance
        {
            get
            {
                return instance_;
            }
        }

        private class Node
        {
            public SpatialEntry entry;
            public Node left;
            public Node right;
            public float splitValue; // 分割值
            public int splitAxis; // 分割轴 (0=x, 1=z)
        }

        private Node root;
        private List<SpatialEntry> entries = new List<SpatialEntry>();
        private bool needsRebuild = true;

        /// <summary>
        /// 添加实体条目
        /// </summary>
        public void Add(int entityId, zVector3 position, int campId)
        {
            entries.Add(new SpatialEntry
            {
                EntityId = entityId,
                Position = position,
                CampId = campId
            });
            needsRebuild = true;
        }

        /// <summary>
        /// 清空索引
        /// </summary>
        public void Clear()
        {
            entries.Clear();
            root = null;
            needsRebuild = false;
        }

        /// <summary>
        /// 重建 KD-tree
        /// </summary>
        public void Rebuild()
        {
            if (!needsRebuild && root != null)
                return;

            root = BuildTree(entries.ToArray(), 0, entries.Count - 1, 0);
            needsRebuild = false;
        }

        private Node BuildTree(SpatialEntry[] entriesArray, int start, int end, int depth)
        {
            if (start > end)
                return null;

            int axis = depth % 2; // 交替选择 x 或 z 轴
            int mid = start + (end - start) / 2;

            // 按当前轴排序
            Array.Sort(entriesArray, start, end - start + 1, new EntryComparer(axis));

            var node = new Node
            {
                entry = entriesArray[mid],
                splitAxis = axis,
                splitValue = axis == 0 ? (float)entriesArray[mid].Position.x : (float)entriesArray[mid].Position.z
            };

            node.left = BuildTree(entriesArray, start, mid - 1, depth + 1);
            node.right = BuildTree(entriesArray, mid + 1, end, depth + 1);

            return node;
        }

        /// <summary>
        /// 径向搜索：查找给定点周围半径范围内的所有实体
        /// </summary>
        public List<SpatialEntry> RadialSearch(zVector3 center, float radius)
        {
            return RadialSearch(center, radius, -1, null);
        }

        /// <summary>
        /// 径向搜索：查找给定点周围半径范围内的实体（支持数量限制和谓词筛选）
        /// </summary>
        /// <param name="center">搜索中心</param>
        /// <param name="radius">搜索半径</param>
        /// <param name="maxCount">最大返回数量（-1 表示不限制）</param>
        /// <param name="predicate">筛选谓词</param>
        /// <returns>符合条件的实体列表</returns>
        public List<SpatialEntry> RadialSearch(zVector3 center, float radius, int maxCount, Func<SpatialEntry, bool> predicate)
        {
            if (root == null)
                return new List<SpatialEntry>();

            var result = new List<SpatialEntry>();
            float radiusSq = radius * radius;
            float[] centerPoint = new float[] { (float)center.x, (float)center.z };
            int count = 0;

            SearchRadial(root, centerPoint, radius, radiusSq, result, maxCount, predicate, ref count);
            return result;
        }

        private void SearchRadial(Node node, float[] centerPoint, float radius, float radiusSq, List<SpatialEntry> result, int maxCount, Func<SpatialEntry, bool> predicate, ref int count)
        {
            if (node == null)
                return;

            // 检查是否已达到数量上限
            if (maxCount > 0 && count >= maxCount)
                return;

            // 计算当前节点与查询点的距离
            float dx = (float)node.entry.Position.x - centerPoint[0];
            float dz = (float)node.entry.Position.z - centerPoint[1];
            float distSq = dx * dx + dz * dz;

            // 如果在范围内且不是同一个实体
            if (distSq <= radiusSq && distSq > 0.0001f)
            {
                // 应用谓词筛选（如果有）
                if (predicate == null || predicate(node.entry))
                {
                    result.Add(node.entry);
                    count++;
                    
                    // 检查是否已达到数量上限，提前终止
                    if (maxCount > 0 && count >= maxCount)
                        return;
                }
            }

            // 决定搜索哪个子树
            float diff = centerPoint[node.splitAxis] - node.splitValue;

            if (diff <= radius)
            {
                SearchRadial(node.left, centerPoint, radius, radiusSq, result, maxCount, predicate, ref count);
                
                // 检查左子树搜索后是否已达到数量上限
                if (maxCount > 0 && count >= maxCount)
                    return;
            }

            if (diff >= -radius)
            {
                SearchRadial(node.right, centerPoint, radius, radiusSq, result, maxCount, predicate, ref count);
            }
        }

        private class EntryComparer : IComparer<SpatialEntry>
        {
            private int axis;

            public EntryComparer(int axis)
            {
                this.axis = axis;
            }

            public int Compare(SpatialEntry a, SpatialEntry b)
            {
                float valA = axis == 0 ? (float)a.Position.x : (float)a.Position.z;
                float valB = axis == 0 ? (float)b.Position.x : (float)b.Position.z;
                return valA.CompareTo(valB);
            }
        }
    }
}