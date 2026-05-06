using System;
using System.Collections;
using System.Collections.Generic;
using zUnity;

public class zPath
{
	/// <summary>
	/// 用于在移动行为时，检测路径是否与之前有变化
	/// 他不保证每个客户端都是一致的
	/// 如果想保证一直，要在每次开局时将ACC_PATHID重置
	/// </summary>
	public int pathID;

	public static int ACC_PATHID = 0;
	public List<zMapPoint> nodePath;
	public List<zVector3> posPath;

	public zVector3 startPos;
	public zVector3 endPos;

	public zPath()
	{
		pathID = ACC_PATHID++;
		this.nodePath = new List<zMapPoint>();
		this.posPath = new List<zVector3>();
	}

	/// <summary>
	/// 生成坐标路径
	/// </summary>
	public void CreateVectorPath(zVector3 __currPos)
	{
		List<zMapPoint> tempNodePath = zAStarTool.FloydSmooth(nodePath, __currPos);

		for (int i = 0; i < tempNodePath.Count; ++i)
		{
			posPath.Add(zAStarTool.TileToWorld(tempNodePath[i]));
		}
		//posPath[0] = __currPos;
		/*if (__currPos != posPath[0])
		{
			posPath.Insert(0, __currPos);
		}*/
		//int a = 0;

		/*for(int i = 0; i < nodePath.Count; ++i)
		{
			posPath.Add(zAStarTool.TileToWorld(nodePath[i]));
		}*/

	}

	/// <summary>
	/// 路径反转
	/// </summary>
	public void Invert()
	{
		int half = nodePath.Count / 2;
		for(int i = 0; i < half; ++i)
		{
			zMapPoint tp = nodePath[i];
			nodePath[i] = nodePath[nodePath.Count - 1 - i];
			nodePath[nodePath.Count - 1 - i] = tp;
		}
	}

	public int Count
	{
		get
		{
			if (nodePath != null)
				return nodePath.Count;
			else
				return 0;
		}
	}
}
