using System;
namespace zUnity
{
	/// <summary>
	/// 二维点,   跟Vector2的区别  是,Point中, x,y 都是int
	/// </summary>
	[System.Serializable]
	public struct zMapPoint
	{
		/// <summary>
		/// 用来表示无意义的点
		/// (正负一万, 应该不会有这么巧重合吧)
		/// </summary>
		public static readonly zMapPoint NULL = new zMapPoint(-100000, 100000);

		/// <summary>
		/// x坐标
		/// </summary>
		public int x;

		/// <summary>
		/// y坐标
		/// </summary>
		public int y;

		public zMapPoint(int __x, int __y)
		{
			x = __x;
			y = __y;
		}

		public zMapPoint(zMapPoint __p)
		{
			x = __p.x;
			y = __p.y;
		}


		public override string ToString()
		{
			return " x=" + x + "  y=" + y;
		}

		/// <summary>
		/// 是否为无意义的点
		/// </summary>
		/// <returns></returns>
		public bool IsNull()
		{
			return x == -100000 && y == 100000;
		}

		/// <summary>
		/// 静态方法 判定是否为同一个点
		/// </summary>
		/// <param name="pt1"></param>
		/// <param name="pt2"></param>
		/// <returns></returns>
		public static bool IsSamePos(zMapPoint pt1, zMapPoint pt2)
		{
			return
					pt1.x == pt2.x
					&&
					pt1.y == pt2.y;
		}
	}
}