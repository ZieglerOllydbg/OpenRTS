using UnityEngine;
using System;
using zUnity;

public static class StringToVector3Converter
{
    /// <summary>
    /// 将格式为 "x,y,z" 的字符串转换为 zVector3
    /// </summary>
    /// <param name="input">输入的字符串，格式应为 "x,y,z"</param>
    /// <returns>对应的zVector3对象</returns>
    public static zVector3 StringToZVector3(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return zVector3.zero;
        }

        string[] parts = input.Split(',');
        
        if (parts.Length < 3)
        {
            Debug.LogWarning($"输入字符串 '{input}' 格式不正确，需要至少3个数字用逗号分隔");
            return zVector3.zero;
        }

        float x, y, z;
        if (float.TryParse(parts[0].Trim(), out x) &&
            float.TryParse(parts[1].Trim(), out y) &&
            float.TryParse(parts[2].Trim(), out z))
        {
            return new zVector3((zfloat)x, (zfloat)y, (zfloat)z);
        }
        else
        {
            Debug.LogWarning($"无法解析字符串 '{input}' 为zVector3，数值格式不正确");
            return zVector3.zero;
        }
    }
    
    /// <summary>
    /// 将zVector3转换为 "x,y,z" 格式的字符串
    /// </summary>
    /// <param name="vector">要转换的zVector3</param>
    /// <returns>格式为 "x,y,z" 的字符串</returns>
    public static string ZVector3ToString(zVector3 vector)
    {
        return $"{vector.x},{vector.y},{vector.z}";
    }
    
    /// <summary>
    /// 将字符串数组转换为zVector3数组
    /// </summary>
    /// <param name="inputs">字符串数组，每个元素都是"x,y,z"格式</param>
    /// <returns>对应的zVector3数组</returns>
    public static zVector3[] StringArrayToZVector3Array(string[] inputs)
    {
        if (inputs == null)
        {
            return new zVector3[0];
        }
        
        zVector3[] result = new zVector3[inputs.Length];
        for (int i = 0; i < inputs.Length; i++)
        {
            result[i] = StringToZVector3(inputs[i]);
        }
        
        return result;
    }
}