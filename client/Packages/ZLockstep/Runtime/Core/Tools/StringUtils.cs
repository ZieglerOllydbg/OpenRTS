using System;
using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    /// <summary>
    /// 字符串工具类，提供常用的字符串解析和转换方法
    /// </summary>
    public static class StringUtils
    {
        /// <summary>
        /// 解析位置字符串为 Vector3 数组
        /// 格式："110,0,112;112,0,110"
        /// </summary>
        /// <param name="positionStr">位置字符串，多个坐标用分号分隔，每个坐标的三个分量用逗号分隔</param>
        /// <returns>Vector3 数组</returns>
        public static Vector3[] ParsePositions(string positionStr)
        {
            if (string.IsNullOrEmpty(positionStr))
                return new Vector3[0];

            string[] positionStrings = positionStr.Split(';');
            List<Vector3> positions = new List<Vector3>();

            foreach (string posStr in positionStrings)
            {
                string[] coords = posStr.Split(',');
                if (coords.Length == 3)
                {
                    if (float.TryParse(coords[0], out float x) &&
                        float.TryParse(coords[1], out float y) &&
                        float.TryParse(coords[2], out float z))
                    {
                        positions.Add(new Vector3(x, y, z));
                    }
                }
            }

            return positions.ToArray();
        }

        /// <summary>
        /// 解析字符串为 int 数组
        /// 格式："1,2,3,4,5"
        /// </summary>
        /// <param name="intStr">整数数组字符串，多个整数用逗号分隔</param>
        /// <param name="separator">分隔符，默认为逗号</param>
        /// <returns>int 数组</returns>
        public static int[] ParseIntegers(string intStr, char separator = ',')
        {
            if (string.IsNullOrEmpty(intStr))
                return new int[0];

            string[] intStrings = intStr.Split(separator);
            List<int> integers = new List<int>();

            foreach (string str in intStrings)
            {
                if (int.TryParse(str.Trim(), out int value))
                {
                    integers.Add(value);
                }
            }

            return integers.ToArray();
        }
    }
}
