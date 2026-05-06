

namespace zUnity
{
	/// <summary>
	/// 逻辑格,  地砖类
	/// </summary>
	public class zMapTile
	{
		public zMapTile()
		{
			tilePos = new zMapPoint();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="__tile_x"></param>
		/// <param name="__tile_y"></param>
		/// <param name="__isSolid"></param>
		/// <param name="__isIland"></param>
		public zMapTile(int __tile_x, int __tile_y, bool __isSolid, bool __isIland = false)
		{
			tilePos = new zMapPoint(__tile_x, __tile_y);
			//worldPos = Transformer.TileToWorld(tilePos);		//暂时用不上 就不浪费性能计算了!	TODO TODO ***********
			isSolid = __isSolid;
			isIland = __isIland;
		}

		/// <summary>
		/// 逻辑格坐标		(用来做寻路)
		/// </summary>
		public zMapPoint tilePos;

		/// <summary>
		/// 分别记录格子四个角的高度值 0：上 1:下 2:左 3:右
		/// </summary>
		public zfloat heights;

		// 	/// <summary>
		// 	/// 世界真实坐标
		// 	/// </summary>
		// 	public Vector3 worldPos;			

		/// <summary>
		/// 是否障碍点
		/// </summary>
		public bool isSolid;

		/// <summary>
		/// 是否孤岛
		/// </summary>
		public bool isIland;

		/// <summary>
		/// 是否锁定 用于恢复正常的格子
		/// </summary>
		public bool isLock = false;

	}
}