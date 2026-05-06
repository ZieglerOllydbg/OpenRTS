using System;
using System.Collections.Generic;


namespace zUnity
{
	/// <summary>
	/// 当前所处的世界场景(区别于UI场景)的地图数据
	/// 注意, 当角色处于UI场景中时, mapConfig仍旧保持之前的世界场景数据
	/// </summary>
	public class zMapConfig
	{
		public zfloat TILE_SIZE = zfloat.FromRaw(15000);// zfloat.Half;
		/// <summary>
		/// // 无效坐标
		/// </summary>
		public static readonly zfloat INVAINCOOR = new zfloat(1000001);

		/// <summary>
		/// 当前地图ID
		/// </summary>
		public int mapID
		{
			set;
			get;
		}


		/// <summary>
		/// 地图横向格子总数 
		/// </summary>
		public int gridX
		{
			private set;
			get;
		}

		/// <summary>
		/// 地图纵向格子总数 
		///	<summary>	
		public int gridY
		{
			private set;
			get;
		}

		/// <summary>
		/// 地图宽
		/// </summary>
		public zfloat width
		{
			private set;
			get;
		}


		/// <summary>
		/// 地图高
		/// </summary>
		public zfloat height
		{
			private set;
			get;
		}



		//顶层地图
		//========================================================================

		///<summary>
		/// 顶层地图切片图地址 井号(#)作为占位符
		/// </summary>
		public string zoneMapUrl;
		/// <summary>
		/// 顶层地图缩略图地址
		/// </summary>
		public string smallMapUrl;





		/// <summary>
		/// 地图逻辑格数据 
		/// 类型:com.zcp.engine.vo.map.MapTile
		/// </summary>
		public List<List<zMapTile>> mapTiles;

		/// <summary>
		/// 地图逻辑格障碍数据（专门用来存放障碍点信息，比用mapTiles更加快速） 
		/// 索引从0开始
		/// 取值: 1 or 0,   0表示障碍点.  1表示非障碍
		/// </summary>
		public byte[][] mapSolids;

		public int solidsLength;

		/// <summary>
		/// 地图高
		/// </summary>
		public zfloat[][] mapHeights;
		/// <summary>
		/// 创建地图配置数据,  注意  地图格子横向,纵向数量  一旦初始化  无法动态变更!
		/// </summary>
		/// <param name="__id">地图id</param>
		/// <param name="__gridH">横向地图格子数量</param>
		/// <param name="__gridV">纵向地图格子数量</param>
		public zMapConfig(int __id, int __gridH, int __gridV)
		{
			mapID = __id;

			gridX = __gridH;
			gridY = __gridV;

			//地图宽高的赋值  只能通过此处计算得到   外部只允许使用
			width = TILE_SIZE * gridX;
			height = TILE_SIZE * gridY;

			mapTiles = new List<List<zMapTile>>();
			//mapSolids = new List<List<byte>>();
		}




		/// <summary>
		/// 清除当前缓存的所有数据
		/// </summary>
		public void Clear()
		{
			mapID = 0;
			mapTiles = null;
			mapSolids = null;
			mapHeights = null;
			height = zfloat.Zero;
			width = zfloat.Zero;
			gridX = 0;
			gridY = 0;
		}

		/// <summary>
		/// 设置地图格数据
		/// </summary>
		/// <param name="__tileX"></param>
		/// <param name="__tileY"></param>
		/// <param name="__isSolids"></param>
		public void SetTileData(int __tileX, int __tileY, bool __isSolids)
		{
			mapTiles[__tileX][__tileY].isSolid = __isSolids;
			mapSolids[__tileX][__tileY] = (byte)(__isSolids ? 0 : 1);
		}

		/// <summary>
		/// 设置格子高
		/// </summary>
		/// <param name="__tileX"></param>
		/// <param name="__tileY"></param>
		/// <param name="__height"></param>
		public void SetTileHeight(int __tileX, int __tileY, int __height)
		{
			mapTiles[__tileX][__tileY].heights = zfloat.FromRaw(__height);
		}


	}
}