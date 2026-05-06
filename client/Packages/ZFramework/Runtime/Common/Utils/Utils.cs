using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZLib
{
    /// <summary>
    /// 工具类(静态)
    /// </summary>
    static public class Utils
    {
        #region 字符串转换

        /// <summary>
        /// string 转换成 byte[]
        /// </summary>
        /// <param name="str">需要转换的string数据</param>
        /// <returns></returns>
        public static byte[] StringToBytes(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;

            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// byte[] 转换成 string
        /// </summary>
        /// <param name="b">需要转换的byte[]数据</param>
        /// <returns></returns>
        public static string BytesToString(byte[] b)
        {
            if (b == null || b.Length == 0)
                return null;

            return Encoding.UTF8.GetString(b);
        }

        /// <summary>
        /// 从字符串中提取数据 例如 10xxff 返回的就是10
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int StringToInt(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            // 正则表达式剔除非数字字符（不包含小数点.） 
            str = Regex.Replace(str, @"[^\d\d]", "");
            // 如果是数字，则转换为int类型 
            if (Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
            {
                return GetIntFromString(str);
            }

            return 0;
        }

        /// <summary>
        /// 字符串转 uint
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static uint StringToUint(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            // 正则表达式剔除非数字字符（不包含小数点.） 
            str = Regex.Replace(str, @"[^\d\d]", "");
            // 如果是数字，则转换为int类型 
            if (Regex.IsMatch(str, @"^[+-]?\d*[.]?\d*$"))
            {
                return GetUInt32FromString(str);
            }

            return 0;
        }

        #region 数据类型转换

        /// <summary>
        /// 字符串转换为 byte
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte GetByteFromString(string str)
        {
            byte b = 0;

            if (string.IsNullOrEmpty(str))
            {
                return b;
            }
            if (byte.TryParse(str, out b))
                return b;
            else
                Debug.LogWarning("数据解析出错 string->byte" + str);

            return b;
        }

        /// <summary>
        /// 字符串转换为 sbyte
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static sbyte GetSByteFromString(string str)
        {
            sbyte b = -1;
            if (string.IsNullOrEmpty(str))
            {
                return b;
            }
            if (sbyte.TryParse(str, out b))
                return b;
            else
                Debug.LogWarning("数据解析出错 string->sbyte" + str);

            return b;
        }

        /// <summary>
        /// 字符串转换为 short
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static short GetShortFromString(string str)
        {
            short s = -1;
            if (string.IsNullOrEmpty(str))
            {
                return s;
            }
            if (short.TryParse(str, out s))
                return s;
            else
                Debug.LogWarning("数据解析出错 string->short" + str);

            return s;
        }

        /// <summary>
        /// 字符串转换为 float
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static float GetFloatFromString(string str)
        {
            float f = -1f;
            if (string.IsNullOrEmpty(str))
            {
                return f;
            }
            if (float.TryParse(str, out f))
                return f;
            else
                Debug.LogWarning("数据解析出错 string->float" + str);

            return f;
        }

        /// <summary>
        /// 字符串转换为 double
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static double GetDoubleFromString(string str)
        {
            double d = 0;
            if (string.IsNullOrEmpty(str))
            {
                return d;
            }
            if (double.TryParse(str, out d))
                return d;
            else
                Debug.LogWarning("数据解析出错 string->double" + str);

            return d;
        }

        /// <summary>
        /// 字符串转换为 int
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetIntFromString(string str)
        {
            int i = 0;
            if (string.IsNullOrEmpty(str))
            {
                return i;
            }

            if (int.TryParse(str, out i))
                return i;
            else
                Debug.LogWarning("数据解析出错 string->int" + str);

            return i;
        }

        /// <summary>
        /// 字符串转换为 uint
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static uint GetUIntFromString(string str)
        {
            uint i = 0;
            if (string.IsNullOrEmpty(str))
            {
                return i;
            }

            if (uint.TryParse(str, out i))
                return i;
            else
                Debug.LogWarning("数据解析出错 string->unit" + str);

            return i;
        }

        /// <summary>
        /// 字符串转换为 long
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long GetLongFromString(string str)
        {
            long l = 0;
            if (string.IsNullOrEmpty(str))
            {
                return l;
            }
            if (long.TryParse(str, out l))
                return l;
            else
                Debug.LogWarning("数据解析出错 string->long" + str);

            return l;
        }

        /// <summary>
        /// 字符串转换为 bool
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool GetBoolFromString(string str)
        {
            bool b = false;

            if (string.IsNullOrEmpty(str))
            {
                return b;
            }

            if (bool.TryParse(str, out b))
                return b;
            else
                Debug.LogWarning("数据解析出错 string->bool" + str);

            return b;
        }

        /// <summary>
        /// 字符串转换为 char
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static char GetCharFromString(string str)
        {
            char c = char.MinValue;

            if (string.IsNullOrEmpty(str))
            {
                return c;
            }
            if (char.TryParse(str, out c))
                return c;
            else
                Debug.LogWarning("数据解析出错 string->char" + str);

            return c;
        }

        /// <summary>
        /// 字符串转换为 UInt16
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static ushort GetUInt16FromString(string str)
        {
            ushort i = 0;
            if (string.IsNullOrEmpty(str))
            {
                return i;
            }
            if (ushort.TryParse(str, out i))
                return i;
            else
                Debug.LogWarning("数据解析出错 string->ushort" + str);
            return i;
        }

        /// <summary>
        /// 字符串转换为 UInt32
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static uint GetUInt32FromString(string str)
        {
            uint i = 0;
            if (string.IsNullOrEmpty(str))
            {
                return i;
            }
            if (uint.TryParse(str, out i))
                return i;
            else
                Debug.LogWarning("数据解析出错string->uint" + str);
            return i;
        }

        /// <summary>
        /// 字符串转换为 UInt32
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static ulong GetUInt64FromString(string str)
        {
            ulong i = 0;
            if (string.IsNullOrEmpty(str))
            {
                return i;
            }
            if (ulong.TryParse(str, out i))
                return i;
            else
                Debug.LogWarning("数据解析出错string->ulong" + str);
            return i;
        }

        #endregion

        #endregion
    }
}
