using System.Collections.Generic;
using UnityEngine;
using zUnity;
using ZLockstep.View;

namespace ZLockstep.Flow
{
    /// <summary>
    /// 简单的地图管理器实现
    /// 用于演示如何实现IFlowFieldMap接口
    /// </summary>
    public class SimpleMapManager : IFlowFieldMap
    {
        private int _width;
        private int _height;

        // 流场宽度和高度
        private int _flowGridWidth;
        private int _flowGridHeight;
        private int _flowSize;
        
        private bool[] walkableGrid;

        /// <summary>
        /// 初始化地图
        /// </summary>
        /// <param name="width">地图宽度（格子数）</param>
        /// <param name="height">地图高度（格子数）</param>
        /// <param name="flowSize">格子大小</param>
        public void Initialize(int width, int height, int flowSize)
        {
            _width = width;
            _height = height;
            
            // 初始化流场宽度和高度（与地图尺寸相同，也可以根据需求设置为不同值）
            _flowSize = flowSize;
            _flowGridWidth = width / flowSize;
            _flowGridHeight = height / flowSize;

            
            // 初始化格子数据
            walkableGrid = new bool[width * height];
            for (int i = 0; i < walkableGrid.Length; i++)
            {
                walkableGrid[i] = true;
            }

            LoadBlockerMap();
        }

        /// <summary>
        /// 从 Resources 读取阻挡文本，0=无阻挡，1=阻挡。
        /// 默认读取 Assets/Resources/Data/Map/GridBlockerMap.txt。
        /// </summary>
        public void LoadBlockerMap(string resourcePath = "Data/Map/GridBlockerMap")
        {
            if (walkableGrid == null || walkableGrid.Length != _width * _height)
            {
                walkableGrid = new bool[_width * _height];
                for (int i = 0; i < walkableGrid.Length; i++)
                {
                    walkableGrid[i] = true;
                }
            }

            TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogWarning($"[SimpleMapManager] 未找到阻挡文件: Resources/{resourcePath}.txt");
                return;
            }

            string[] lines = textAsset.text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            int loadedRowCount = 0;

            for (int fileRow = 0; fileRow < lines.Length && loadedRowCount < _height; fileRow++)
            {
                string line = lines[fileRow];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                int mapY = _height - 1 - loadedRowCount;
                int maxX = line.Length < _width ? line.Length : _width;
                for (int x = 0; x < maxX; x++)
                {
                    walkableGrid[mapY * _width + x] = line[x] != '1';
                }

                for (int x = maxX; x < _width; x++)
                {
                    walkableGrid[mapY * _width + x] = true;
                }

                loadedRowCount++;
            }

            Debug.Log($"[SimpleMapManager] 已加载阻挡文件: Resources/{resourcePath}.txt, 行数={loadedRowCount}, 地图尺寸={_width}x{_height}");
            return;
        }

        /// <summary>
        /// 设置格子的可行走性
        /// </summary>
        public void SetWalkable(int x, int y, bool walkable)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                walkableGrid[y * _width + x] = walkable;
            }
        }

        /// <summary>
        /// 设置矩形区域的可行走性（用于建筑）
        /// </summary>
        public void SetWalkableRect(int minX, int minY, int maxX, int maxY, bool walkable)
        {
            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    SetWalkable(x, y, walkable);
                }
            }
        }

        /// <summary>
        /// 设置圆形区域的可行走性（用于圆形障碍物）
        /// </summary>
        public void SetWalkableCircle(int centerX, int centerY, int radius, bool walkable)
        {
            int radiusSq = radius * radius;
            for (int y = centerY - radius; y <= centerY + radius; y++)
            {
                for (int x = centerX - radius; x <= centerX + radius; x++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx * dx + dy * dy <= radiusSq)
                    {
                        SetWalkable(x, y, walkable);
                    }
                }
            }
        }

        // ============ IFlowFieldMap 接口实现 ============

        public int GetWidth()
        {
            return _width;
        }

        public int GetHeight()
        {
            return _height;
        }

        public int GetFlowGridWidth()
        {
            return _flowGridWidth;
        }

        public int GetFlowGridHeight()
        {
            return _flowGridHeight;
        }

        public int GetFlowSize()
        {
            return _flowSize;
        }

        public bool IsWalkable(int x, int y)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                return walkableGrid[y * _width + x];
            }
            return false;
        }

        public bool IsFlowWalkable(int flowX, int flowY)
        {
            // 流场格子对应2x2的逻辑格子
            // 只要有一个逻辑格子可走，流场格子就可走
            int x = flowX * _flowSize;
            int y = flowY * _flowSize;
            
            for (int dy = 0; dy < _flowSize; dy++)
            {
                for (int dx = 0; dx < _flowSize; dx++)
                {
                    int lx = x + dx;
                    int ly = y + dy;
                    if (lx < _width && ly < _height)
                    {
                        if (walkableGrid[ly * _width + lx])
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        public zfloat GetTerrainCost(int gridX, int gridY)
        {
            // 简单实现：所有地形代价相同
            // 可以扩展为支持不同地形类型
            return zfloat.One;
        }

        public void WorldToFlow(zVector2 worldPos, out int flowX, out int flowY)
        {
            flowX = (int)(worldPos.x / _flowSize);
            flowY = (int)(worldPos.y / _flowSize);
            
            // 限制在地图范围内
            if (flowX < 0) flowX = 0;
            if (flowY < 0) flowY = 0;
            if (flowX >= _flowGridWidth) flowX = _flowGridWidth - 1;
            if (flowY >= _flowGridHeight) flowY = _flowGridHeight - 1;
        }

        public zVector2 FlowToWorld(int flowX, int flowY)
        {
            // 返回格子中心的世界坐标
            return new zVector2(
                (zfloat)flowX * _flowSize + _flowSize * zfloat.Half,
                (zfloat)flowY * _flowSize + _flowSize * zfloat.Half
            );
        }

        /// <summary>
        /// 清空地图（重置为全部可行走）
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < walkableGrid.Length; i++)
            {
                walkableGrid[i] = true;
            }
        }
    }
}
