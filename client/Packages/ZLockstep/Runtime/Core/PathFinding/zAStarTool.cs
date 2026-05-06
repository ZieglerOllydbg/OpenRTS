using System;
using System.Collections;
using System.Collections.Generic;

namespace zUnity
{
	public class zAStarTool
	{
		[ThreadStatic]
		static zMapConfig map;
		[ThreadStatic]
		private static Dictionary<int, SearchingPoint> dict_openex = new Dictionary<int, SearchingPoint>();
		[ThreadStatic]
		private static Dictionary<int, SearchingPoint> dict_closeex = new Dictionary<int, SearchingPoint>();
		[ThreadStatic]
		private static List<SearchingPoint> openList = new List<SearchingPoint>();
		[ThreadStatic]
		private static List<SearchingPoint> closeList = new List<SearchingPoint>();

		/// <summary>
		/// 8方向数组
		/// </summary>
		[ThreadStatic]
		public static Dictionary<int, zMapPoint> ro;

		public static void SetMap(zMapConfig __map)
		{
			map = __map;
		}

		static zAStarTool()
		{
			ro = new Dictionary<int, zMapPoint>();

			ro.Add(1, new zMapPoint(-1, -1));	//7左上
			ro.Add(2, new zMapPoint(0, -1));	//6上
			ro.Add(3, new zMapPoint(1, -1));	//5右上
			ro.Add(4, new zMapPoint(1, 0));	//4右
			ro.Add(5, new zMapPoint(1, 1));	//3右下
			ro.Add(6, new zMapPoint(0, 1));	//2下
			ro.Add(7, new zMapPoint(-1, 1));	//1左下
			ro.Add(8, new zMapPoint(-1, 0));	//8左
		}

		/// <summary>
		/// 通用回调委托
		/// </summary>
		/// <param name="__path">逻辑格数组,路径</param>
		public delegate void SearchComplete(zPath __path);

		public static void SearchPath(zVector3 startPos, zVector3 endPos, SearchComplete __onCom)
		{
			//获取离目标点最近的一个非障碍逻辑格
			zMapPoint nearTargetPT = GetNearestWalkableTile(endPos);
			zMapPoint curPT = WorldToTile(startPos);
			//找不到有意义的点  就不行走!
			if (nearTargetPT.IsNull())
			{
				zUDebug.LogError("Error! 找不到有意义的可行走点!   世界坐标 = " + endPos);
				return;
			}
			//如果当前站立在障碍上  肯定是出问题了   要给予提示		//后期可能考虑要回拉!			TODO TODO **************
			if (IsSolid(curPT))
			{
				zUDebug.LogError("Error!	起始点为障碍点！" + startPos);
				return;
			}

			zPath roadPath = new zPath();
			roadPath.startPos = startPos;
			roadPath.endPos = endPos;

			SearchPath(map.mapSolids, curPT, nearTargetPT, roadPath, __onCom);
		}


		protected static void SearchPath(byte[][] __tileList, zMapPoint __startP, zMapPoint __endP, zPath __path, SearchComplete __onCom, int __tryCount = -1)
		{

			// Debug.Log("***************************** Start Find Path ********************************");
#if DEBUG_ASTAR
            Debug.Log("start: " + __startP.x + "," + __startP.y + "  end:" + __endP.x + "," + __endP.y);
#endif
			//  __startP = new Point(71, 27);
			//  __endP = new Point(77, 42);
			//先创建一个空路径   作为找好的路径
			//List<zMapPoint> roadPath = new List<zMapPoint>();
			zPath roadPath = __path;


			//获取起点和终点的逻辑格坐标
			int x1 = __startP.x;
			int y1 = __startP.y;
			int x2 = __endP.x;
			int y2 = __endP.y;

			//数据不对  直接退出
			if (__tileList == null || __tileList.Length == 0)
			{
				if (__onCom != null)
				{
					__onCom(roadPath);
				}
				return;
			}

			//2点重合 直接退出
			if (x1 == x2 && y1 == y2)
			{
				if (__onCom != null)
				{
					__onCom(roadPath);
				}
				return;
			}


			//寻路初始化
			openList.Clear();//开启列表
			closeList.Clear();//关闭列表

			dict_openex.Clear();
			dict_closeex.Clear();
			//var dict_closeex = new Dictionary<int, SearchingPoint>();

			//			Dictionary<string, SearchingPoint> dict_open = new Dictionary<string, SearchingPoint>();			//快速查找列表
			//Dictionary<int, Dictionary<int, SearchingPoint>> dict_open = new Dictionary<int, Dictionary<int, SearchingPoint>>();			//快速查找列表
			//			Dictionary<string, SearchingPoint> dict_close = new Dictionary<string, SearchingPoint>();			//快速查找列表
			//Dictionary<int, Dictionary<int, SearchingPoint>> dict_close = new Dictionary<int, Dictionary<int, SearchingPoint>>();			//快速查找列表


			SearchingPoint startPoint = new SearchingPoint();//起点
			startPoint.x = x1;
			startPoint.y = y1;
			startPoint.g = 0;
			int h1 = (x1 - x2) < 0 ? (x1 - x2) * -1 : (x1 - x2);
			int h2 = (y1 - y2) < 0 ? (y1 - y2) * -1 : (y1 - y2);
			startPoint.h = (h1 + h2) * ((zfloat)10);
			startPoint.f = startPoint.g + startPoint.h;
			startPoint.parentpoint = null;

			//将起点添加到开启列表
			int openListLen = 0;
			updatePoint(ref openList, startPoint, openListLen++);		//注意  是[后]加加,   先传参, 后计算+1




			//			dict_open.Add(startPoint.x + "_" + startPoint.y, startPoint);		//给这个起始点分配一个key	
			//用x,y做索引  存储此点
			//dict_open[startPoint.x] = new Dictionary<int, SearchingPoint>();
			//dict_open[startPoint.x][startPoint.y] = startPoint;

			dict_openex[(startPoint.x << 16) + startPoint.y] = startPoint;



			//正式寻路 ---------------------------------

			//循环做以下事情
			int seq = 0;//寻路次数
			SearchingPoint minFPoint;//最小F值的节点
			SearchingPoint p;//生成路径时用到的指针
			SearchingPoint nodePoint;
			int i;
			int sx;
			int sy;
			bool odd;
			int newG;
			while (true)
			{
				seq++;

				//超过限定次数还未找到
				if (__tryCount > 0)
				{
					if (seq > __tryCount)
					{
						if (__onCom != null)
						{
							__onCom(roadPath);
						}
						return;
					}
				}

				//检查开启列表是否为空，为空代表死路，直接返回
				if (openList.Count == 0 && dict_openex.Count == 0)
				{
					if (__onCom != null)
					{
						__onCom(roadPath);
					}
					return;
				}

				//从开启列表里取出最小F值的节点
				minFPoint = delPoint(ref openList, openListLen--);
				// Debug.Log("Get Point: " + minFPoint.x + " , " + minFPoint.y);
				closeList.Add(minFPoint);//将该点添加到关闭列表中
				if (minFPoint == null)
				{
					zUDebug.LogError("Error!  路径查找失败!  Start:" + __startP.x + "," + __startP.y + "   End:" + __endP.x + "," + __endP.y);
					//查找失败就直接退出
					return;
				}



				// 				//如果不包含检测点, 就添加进去
				// 				if (!dict_close.ContainsKey(minFPoint.x + "_" + minFPoint.y))
				// 					dict_close.Add(minFPoint.x + "_" + minFPoint.y, minFPoint);//给这个点分配一个key

				//if (!dict_close.ContainsKey(minFPoint.x))
				//{
				//	dict_close[minFPoint.x] = new Dictionary<int, SearchingPoint>();
				//}
				dict_closeex[(minFPoint.x << 16) + minFPoint.y] = minFPoint;



				//判断该关闭的节点是否为目标点(是则找到路径)
				if (minFPoint.x == x2 && minFPoint.y == y2)
				{
					p = minFPoint;
					roadPath.nodePath.Add(new zMapPoint(p.x, p.y));
					while (true)
					{
						//返回路径
						if (p.parentpoint == null)
						{
							if (__onCom != null)
							{
								__onCom(roadPath);
							}
							return;
						}
						p = p.parentpoint;
						roadPath.nodePath.Add(new zMapPoint(p.x, p.y));
					}
				}

				//循环遍历周围8个点
				for (i = 1; i <= 8; i++)
				{
					//var temppoint = ro[i];
					//计算出该周围点的x和y
					sx = minFPoint.x + ro[i].x;
					sy = minFPoint.y + ro[i].y;

					//注意这里判断的顺序，先判断是否出边界，然后在判断是否是障碍物，否则会报错
					if (
						GetMapTileWalkAble(__tileList, sx, sy) != 1
						||
						//						dict_close.ContainsKey(sx + "_" + sy)//已经在关闭列表中
						(dict_closeex.ContainsKey((sx << 16) + sy))
					)
					{
						continue;
					}


					//取余，1为斜角，0为直角
					odd = (i & 1) == 0;//111111111111111111111111111可优化
					//斜角处理，判断它的上下或者左右是否有障碍物，有就跳过
					if (!odd && (GetMapTileWalkAble(__tileList, minFPoint.x, sy) != 1 || GetMapTileWalkAble(__tileList, sx, minFPoint.y) != 1))
					{
						continue;
					}



					//					//再检查是否在开启列表里，如果不是就加到开启列表里，如果是就重新评估
					//					nodePoint = dict_open.ContainsKey(sx + "_" + sy) ? dict_open[sx + "_" + sy] : null;
					nodePoint = null;

					dict_openex.TryGetValue((sx << 16) + sy, out nodePoint);

					newG = minFPoint.g + (odd ? 10 : 14);	//直角g为10,斜角为14
					if (nodePoint != null)
					{
						//在开启列表里，重新评估g
						if (newG < nodePoint.g)
						{
							//更改G、F值、父节点
							nodePoint.g = newG;
							nodePoint.f = nodePoint.g + nodePoint.h;
							nodePoint.parentpoint = minFPoint;
							//更新排序(非二叉堆方法中无需此)
							updatePoint(ref openList, nodePoint, openList.IndexOf(nodePoint));
						}
					}
					else
					{
						//未在开启列表里，计算该节点的f、g、h并加入到开启列表里
						nodePoint = new SearchingPoint();
						nodePoint.x = sx;
						nodePoint.y = sy;
						//计算G
						nodePoint.g = newG;
						//计算H
						h1 = (sx - x2) < 0 ? (sx - x2) * -1 : (sx - x2);
						h2 = (sy - y2) < 0 ? (sy - y2) * -1 : (sy - y2);
						nodePoint.h = (h1 + h2) * ((zfloat)10);
						//计算F
						nodePoint.f = nodePoint.g + nodePoint.h;
						//加到开启列表里
						nodePoint.parentpoint = minFPoint;//记录当前的点的父节点
						updatePoint(ref openList, nodePoint, openListLen++);


						//						dict_open.Add(nodePoint.x + "_" + nodePoint.y, nodePoint);//给这个起始点分配一个key，用于以后索引
						//if (!dict_open.ContainsKey(nodePoint.x))
						//{
						//	dict_open[nodePoint.x] = new Dictionary<int, SearchingPoint>();
						//}
						//用x,y做索引  存储此点
						dict_openex[(nodePoint.x << 16) + nodePoint.y] = nodePoint;



					}
				}
			}


		}

		/// <summary>
		/// 快速判断某个逻辑格 是否为障碍点
		/// </summary>
		/// <param name="__tile"> 逻辑格坐标 </param>
		/// <returns></returns>
		public static bool IsSolid(zMapPoint __tile)
		{
			return IsSolid(__tile.x, __tile.y);
		}

		/// <summary>
		/// 是否是下边缘
		/// 即该格子的负y方向的第一个格子，是障碍格
		/// </summary>
		/// <param name="__tile"></param>
		/// <returns></returns>
		public static bool IsDownEdge(zMapPoint __tile)
		{
			return IsDownEdge(__tile.x, __tile.y);
		}

		public static bool IsDownEdge(int __x, int __y)
		{
			if (IsSolid(__x, __y))
			{
				return false;
			}
			--__y;
			return IsSolid(__x, __y);
		}

		/// <summary>
		/// 是否是上边缘
		/// 即该格子的正y方向的第一个格子，是障碍格
		/// </summary>
		/// <param name="__tile"></param>
		/// <returns></returns>
		public static bool IsUpEdge(zMapPoint __tile)
		{
			return IsUpEdge(__tile.x, __tile.y);
		}

		public static bool IsUpEdge(int __x, int __y)
		{
			if (IsSolid(__x, __y))
			{
				return false;
			}
			++__y;
			return IsSolid(__x, __y);
		}

		/// <summary>
		/// 是否是左边缘
		/// 即该格子的负x方向的第一个格子，是障碍格
		/// </summary>
		/// <param name="__tile"></param>
		/// <returns></returns>
		public static bool IsLeftEdge(zMapPoint __tile)
		{
			return IsLeftEdge(__tile.x, __tile.y);
		}

		public static bool IsLeftEdge(int __x, int __y)
		{
			if (IsSolid(__x, __y))
			{
				return false;
			}
			--__x;
			return IsSolid(__x, __y);
		}

		/// <summary>
		/// 是否是右边缘
		/// 即该格子的正x方向的第一个格子，是障碍格
		/// </summary>
		/// <param name="__tile"></param>
		/// <returns></returns>
		public static bool IsRightEdge(zMapPoint __tile)
		{
			return IsRightEdge(__tile.x, __tile.y);
		}

		public static bool IsRightEdge(int __x, int __y)
		{
			if (IsSolid(__x, __y))
			{
				return false;
			}
			++__x;
			return IsSolid(__x, __y);
		}

		/// <summary>
		/// 快速判断某个逻辑格 是否为障碍点
		/// </summary>
		/// <param name="__tile"> 逻辑格坐标 </param>
		/// <returns></returns>
		public static bool IsSolid(int __x, int __y)
		{
			//return map.mapSolids[__x][__y] == 0;
			//非法逻辑格 认为是障碍
			if (__x < 0 || __y < 0)
			{
				return true;
			}

			//先判断数据是否合法
			//if (map != null && map.mapSolids != null && map.solidsLength > 0)
			//{
			if (__x < map.solidsLength)
			{
				var cache = map.mapSolids[__x];
				//如果在障碍数组中, 没有越界, 并且值为0  说明是
				if (__y < cache.Length)
				{
					return cache[__y] == 0;
				}
				else       //数据越界 也认为是障碍	
				{
					return true;
				}
			}
			else
				return true;
			//}
			//else	//非法数据 也认为是障碍
			//{
			//	return true;
			//}
		}

		/// <summary>
		/// 世界坐标转换到Tile坐标
		/// </summary>
		/// <param name="__worldPos"></param>
		/// <returns></returns>
		public static zMapPoint WorldToTile(zVector3 __worldPos)
		{
			zfloat pixelX = __worldPos.x;
			zfloat pixelZ = __worldPos.z;
			zMapPoint p;// = new zMapPoint();
			//除数被除数都乘以10, 是为了防止精度损失,  比如  10.5, 系统会认为是 10.49 或者10.59  这样很坑!

			p.x = (int)((pixelX.value) * zfloat.SCALE_10000 / (map.TILE_SIZE.value) / zfloat.SCALE_10000);		//	形如:	(int) (22.7 / 0.5)	= 227/5 = 45.4 取整===> 45;
			p.y = (int)((pixelZ.value) * zfloat.SCALE_10000 / (map.TILE_SIZE.value) / zfloat.SCALE_10000);
			return p;
		}

		public static void WorldToTile(zVector3 __worldPos, out int __tileX, out int __tileY)
		{
			long pixelX;
			pixelX = __worldPos.x.value;
			long pixelZ;
			pixelZ = __worldPos.z.value;
			//zMapPoint p;// = new zMapPoint();
			//除数被除数都乘以10, 是为了防止精度损失,  比如  10.5, 系统会认为是 10.49 或者10.59  这样很坑!

			__tileX = (int)((pixelX) / (map.TILE_SIZE.value));		//	形如:	(int) (22.7 / 0.5)	= 227/5 = 45.4 取整===> 45;
			__tileY = (int)((pixelZ) / (map.TILE_SIZE.value));

		}


		/// <summary>
		/// 逻辑地砖 转换成 世界场景坐标
		/// 注意 这个方法返回的坐标, 永远是逻辑格[中心]的位置坐标,  也就是 如果一个世界坐标点, 经过了 worldToTile再经过 tileToWorld之后, 有可能数值会发生小变化.(从格子的角变到了中心)
		/// </summary>
		/// <param name="__tile">逻辑格坐标</param>
		/// <param name="__needRealY">是否需要真实Y值  这个有性能消耗, 不是必要的话  就传false</param>
		/// <returns></returns>
		public static zVector3 TileToWorld(zMapPoint __tile, bool __needRealY = true)
		{
			return TileToWorld(__tile.x, __tile.y, __needRealY);
		}

		/// <summary>
		/// 逻辑地砖 转换成 世界场景坐标			//目前是使用中心点作为锚点的
		/// </summary>
		/// <param name="__tileX">逻辑格坐标X</param>
		/// <param name="__tileY">逻辑格坐标Y</param>
		/// <param name="__needRealY"> 是否需要真实Y值  这个有性能消耗, 不是必要的话  就传false</param>
		/// <returns>返回世界场景坐标Vector3,  注意默认返回的Y, 值是0   如果需要真实Y值需最后一个参数传true</returns>
		public static zVector3 TileToWorld(int __tileX, int __tileY, bool __needRealY = true)
		{
			//先创建一个X和Z正确,  但是Y是0的世界坐标
			zVector3 targetPos = new zVector3(__tileX * map.TILE_SIZE + map.TILE_SIZE * zfloat.Half, zfloat.Zero, __tileY * map.TILE_SIZE + map.TILE_SIZE * zfloat.Half);
			if (__needRealY)
			{
				//求出真实的Y
				targetPos = GetPositionFromXZ(targetPos);
			}

			//使用左下角作为锚点(原理等同于flash中 使用左上角作为锚点)
			//return new Vector3(__tileX * MoveManager.TILE_SIZE, yy, __tileY * MoveManager.TILE_SIZE);

			//使用中心点作为锚点				//以后如果需要使用左下角点   可以改为使用上面的代码		TODO TODO ****************
			return targetPos;
		}

		/// <summary>
		/// 对路径数组使用弗洛伊德平滑算法
		/// </summary>
		/// <param name="__tilePath">路径数组</param>
		public static List<zMapPoint> FloydSmooth(List<zMapPoint> __tilePath, zVector3 __currPos)
		{
#if DEBUG_ASTAR
            Debug.Log("******************************** FloydSmooth ***********************************");
            Debug.Log("Path Count: " + __tilePath.Count);
#endif
			//克隆一下
			List<zMapPoint> pathArr = __tilePath.GetRange(0, __tilePath.Count);
			List<zMapPoint> temp;
			zVector3 offsetPos = TileToWorld(__tilePath[0], false) - __currPos;
			offsetPos.y = zfloat.Zero;

			//佛罗伊德平滑
			if (pathArr.Count > 2)
			{
#if DEBUG_ASTAR
                Debug.Log("start:" + __tilePath[0].x + "," + __tilePath[0].y + "   end:" + __tilePath[__tilePath.Count - 1].x + "," + __tilePath[__tilePath.Count - 1].y);
#endif
				int headIndex = 0;
				while (true)
				{
					//最多检测到倒数第二个
					if (headIndex >= pathArr.Count - 1)
						break;

					//去掉中间点
					zMapPoint head = pathArr[headIndex];
					for (int i = pathArr.Count - 1; i > headIndex; i--)
					{
						zMapPoint tail = pathArr[i];
						zfloat offsetX = zfloat.Zero;
						zfloat offsetY = zfloat.Zero;
						if (headIndex == 0)
						{
							offsetX = offsetPos.x;
							offsetY = offsetPos.z;
						}
						//MapTile mt1 = MoveUtils.GetMapTile(head);
						//MapTile mt2 = MoveUtils.GetMapTile(tail);
						//如果两点之间没有障碍，则去掉中心点，进入下一检测			
						//或者俩点是横向或者纵向的相邻点,  也直接去掉两点中, 靠前的点
						if (!HasExactSolidBetween2MapTile(head, tail, offsetX, offsetY))
						{
							//去掉从headIndex+1到i的元素
							//pathArr.slice(0,headIndex+1)		表示取出,  从起点一直到当前作为检测点的临时起点   这之间所有的数据
							//pathArr.slice(i)					表示取出,  从当前作为检测点的临时终点,到真正终点之间的所有数据
							//2者合并, 得到的就是删除掉了 平滑点之后的 最终数据
							//例如[0,1,2,3,4,5,6,7]的数组, headIndex=2, i=5,  得出的结果就是    [0,1,2,5,6,7]
							int count = headIndex + 1;
							temp = pathArr.GetRange(0, count);
							temp.AddRange(pathArr.GetRange(i, pathArr.Count - i));
							pathArr = temp;

							headIndex++;
							break;
						}

						//如果已经到达了末尾，但并没有找到可去除的点，则进入下一检测
						if (i == headIndex + 1)
						{
							headIndex++;
							break;
						}
					}
				}

			}

			return pathArr;
		}
		[ThreadStatic]
		public static List<zVector2> _tempVec2List = new List<zVector2>();
		[ThreadStatic]
		public static List<zVector2> _tempVec2List2 = new List<zVector2>();
		/// <summary>
		/// 获取方向上的从$fromMT到$toMT的直线内是否有障碍点		(精确判定)
		/// </summary>
		/// <param name="__fromMT">开始的逻辑格位置</param>
		/// <param name="__toMT">目标的逻辑格位置</param>
		/// <returns>true有障碍, false没障碍</returns>
		public static bool HasExactSolidBetween2MapTile(zMapPoint __fromMT, zMapPoint __toMT, zfloat fromOffsetX, zfloat fromOffsetY)
		{
			//在这个算法中，以格子的线，为整数坐标。
			//而Point的坐标是以格子中心为整数坐标，所以要加0.5
			zfloat fromX = __fromMT.x + zfloat.Half + fromOffsetX / map.TILE_SIZE;
			zfloat fromY = __fromMT.y + zfloat.Half + fromOffsetY / map.TILE_SIZE;
			zfloat toX = __toMT.x + zfloat.Half;
			zfloat toY = __toMT.y + zfloat.Half;

			zfloat dx = toX - fromX;
			zfloat dy = toY - fromY;
			//如果起点与终点，x坐标相同
			if (zMathf.Abs(dx) <= zfloat.Epsilon)
			{
				int startIndex = 0;
				int endIndex = 0;
				if (__fromMT.y < __toMT.y)
				{
					startIndex = __fromMT.y;
					endIndex = __toMT.y;
				}
				else
				{
					startIndex = __toMT.y;
					endIndex = __fromMT.y;
				}
				zMapPoint tp;
				for (int i = startIndex; i < endIndex; ++i)
				{
					tp.x = __fromMT.x;
					tp.y = i;
					if (IsSolid(tp))
					{
						return true;
					}
				}
				return false;
			}
			//起点与终点，y坐标相同
			if (zMathf.Abs(dy) <= zfloat.Epsilon)
			{
				int startIndex = 0;
				int endIndex = 0;
				if (__fromMT.x < __toMT.x)
				{
					startIndex = __fromMT.x;
					endIndex = __toMT.x;
				}
				else
				{
					startIndex = __toMT.x;
					endIndex = __fromMT.x;
				}
				zMapPoint tp;
				for (int i = startIndex; i < endIndex; ++i)
				{
					tp.x = i;
					tp.y = __fromMT.y;
					if (IsSolid(tp))
					{
						return true;
					}
				}
				return false;
			}
			//斜率
			zfloat k = dy / dx;
			//TODO TODO ############ 这是个临时处理，k有可能因为精度不够变成0
			if (k == zfloat.Zero)
			{
				k = zfloat.Epsilon * zMathf.Sign(dy) * zMathf.Sign(dx);
			}
			//斜率的倒数
			zfloat rec_k = 1 / k;
			zfloat b = fromY - k * fromX;
			//X,Y的起始线与结束线
			int startGridX = __fromMT.x + 1;
			int startGridY = __fromMT.y + 1;
			int endGridX = __toMT.x + 1;
			int endGridY = __toMT.y + 1;
			//使起始点为较小值
			if (startGridX > endGridX)
			{
				int m = startGridX;
				startGridX = endGridX;
				endGridX = m;
			}
			if (startGridY > endGridY)
			{
				int m = startGridY;
				startGridY = endGridY;
				endGridY = m;
			}
			//循环计算与x方向和y方向的格线的交点
			_tempVec2List.Clear();
			_tempVec2List2.Clear();
			if (fromX < toX)
			{
				_tempVec2List.Add(new zVector2(fromX, fromY));//添加起点
			}
			else
			{
				_tempVec2List.Add(new zVector2(toX, toY));//添加终点
			}
			//循环计算直线路径与地图x格线的交点
			for (int i = startGridX; i < endGridX; ++i)
			{
				zfloat tx = (zfloat)i;
				zfloat ty = k * tx + b;
				_tempVec2List.Add(new zVector2(tx, ty));
			}
			if (fromX >= toX)
			{
				_tempVec2List.Add(new zVector2(fromX, fromY));//添加起点
			}
			else
			{
				_tempVec2List.Add(new zVector2(toX, toY));//添加终点
			}
			//_tempVec2List.Add(new zVector2(toX, toY));//添加终点
			//循环计算直线路径与地图y格线的交点
			for (int i = startGridY; i < endGridY; ++i)
			{
				zfloat ty = (zfloat)i;
				zfloat tx = (ty - b) * rec_k;
				_tempVec2List2.Add(new zVector2(tx, ty));
			}
			//对所有交点进行x方向排序,两列表都为有序列表，所以直接归并即可。
			if (_tempVec2List2.Count > 0)
			{
				//列表2中的点，x呈递减
				if (_tempVec2List2[0].x > _tempVec2List2[_tempVec2List2.Count - 1].x)
				{
					int currList2Index = 0;
					for (int i = _tempVec2List.Count - 1; i >= 0; --i)
					{
						if (_tempVec2List[i].x < _tempVec2List2[currList2Index].x)
						{
							_tempVec2List.Insert(i + 1, _tempVec2List2[currList2Index]);
							++currList2Index;
							++i;
							if (currList2Index >= _tempVec2List2.Count)
							{
								break;
							}
						}
					}
					if (currList2Index < _tempVec2List2.Count)
					{
						for (int i = currList2Index; i < _tempVec2List2.Count; ++i)
						{
							_tempVec2List.Insert(0, _tempVec2List2[i]);
						}
					}
				}
				//x呈递增
				else
				{
					int currList2Index = _tempVec2List2.Count - 1;
					for (int i = _tempVec2List.Count - 1; i >= 0; --i)
					{
						if (_tempVec2List[i].x < _tempVec2List2[currList2Index].x)
						{
							_tempVec2List.Insert(i + 1, _tempVec2List2[currList2Index]);
							--currList2Index;
							++i;
							if (currList2Index < 0)
							{
								break;
							}
						}
					}
					if (currList2Index >= 0)
					{
						for (int i = currList2Index; i >= 0; --i)
						{
							_tempVec2List.Insert(0, _tempVec2List2[i]);
						}
					}
				}
			}

			//移除相近点
			for (int i = _tempVec2List.Count - 1; i >= 1; --i)
			{
				if (_tempVec2List[i].x - _tempVec2List[i - 1].x < zfloat.Epsilon
					&& _tempVec2List[i].y - _tempVec2List[i - 1].y < zfloat.Epsilon)
				{
					_tempVec2List.RemoveAt(i);
				}
			}

			//求每个相邻点的中点（求出经过的逻辑格子）
			zMapPoint tempPoint;
			for (int i = 1; i < _tempVec2List.Count; ++i)
			{
				zfloat mx = (_tempVec2List[i].x + _tempVec2List[i - 1].x) * zfloat.Half;
				zfloat my = (_tempVec2List[i].y + _tempVec2List[i - 1].y) * zfloat.Half;
				tempPoint.x = (int)mx;
				tempPoint.y = (int)my;
				if (IsSolid(tempPoint))
				{
					return true;
				}
			}
			return false;



			#region 逐像素计算的写法				//如果后期发现有性能问题, 再改成 按照格子 的算法!
			//先把逻辑格转为世界坐标,  此处无需计算真实Y值
			/*	Vector3 from = Transformer.TileToWorld(__fromMT, false);
				Vector3 to = Transformer.TileToWorld(__toMT, false);

				//检查路径有无阻挡，要是没有，那么清空路径，改为设置目标
				Vector3 dir = to - from;
				dir.y = 0;
				dir.Normalize();
				//去掉Y坐标信息
				Vector3 tdir = new Vector3(dir.x, 0, dir.z);

				Point p;
				while (Vector3.Distance(from, to) > 0.01f)
				{
					p = Transformer.WorldToTile(from);
					if (MoveUtils.IsSolid(p))
					{
						return true;
					}
					from += tdir * 0.01f;
				}

				return false;*/
			#endregion

			#region 按照格子计算		(算法不够优化,暂时屏蔽)
			//			//如果起点终点是同一个点那傻子都知道它们间是没有障碍物的
			//			if( __fromMT.tilePos.x == __toMT.tilePos.x && __fromMT.tilePos.y == __toMT.tilePos.y )
			//				return false;


			//			float startX = __fromMT.worldPos.x;
			//			float startZ = __fromMT.worldPos.z;
			//			float endX = __toMT.worldPos.x;
			//			float endZ = __toMT.worldPos.z;


			//			//起点,终点,两节点所在的世界坐标位置
			//			Vector2 point1: = new Vector2( startX, startZ );
			//			Vector2 point2 = new Vector2( endX, endZ );

			//			//根据起点终点间横纵向距离的大小来判断遍历方向
			//			float distX = Math.Abs(endX - startX);
			//			float distY = Math.Abs(endZ - startZ);                                                                        

			//			/**遍历方向，为true则为横向遍历，否则为纵向遍历*/
			//			bool loopDirection = distX > distY ? true : false;

			//			/**起始点与终点的连线方程*/
			//			var lineFuction:Function;

			//			/** 循环递增量 */
			//			float i;

			//			/** 循环起始值 */
			//			float loopStart;
			//			/** 循环起始点的另外一个坐标 */
			//			float loopStart2;

			//			/** 循环终结值 */
			//			float loopEnd;

			//			/** 起终点连线所经过的节点 */
			//			var passedNodeList:Array;
			//			var passedNode:Point;
			//			MapTile passTile;
			//			var tileP:Point;

			////			//调试打印
			////			if( ! Fun.isVisible(container))
			////				Scene.scene.addChild(container);

			//			if( loopDirection )
			//			{                                
			//				lineFuction = getLineFunc(point1, point2, 0);

			//				//无论起点终点的相互位置如何,  X方向都是使用从左向右遍历
			//				loopStart = Math.Min( startX, endX );
			//				loopEnd = Math.Max( startX, endX );

			//				loopStart2 = (loopStart == startX) ? startZ : endZ;

			////				Fun.clearChildren(container);

			//				//开始横向遍历起点与终点间的节点看是否存在障碍(不可移动点) 
			//				for( i=loopStart; i<=loopEnd; i+=MoveManager.TILE_SIZE )
			//				{
			//					//不用判断起点,  所以如果是起点, 那么取出右侧的格子, 然后使用右侧格子的X坐标, 作为要遍历的点
			//					if( i==loopStart )
			//					{
			//						//像素转地图格
			//						tileP = Transformer.transPixelPoint2TilePoint( new Point(loopStart + MoveManager.TILE_SIZE, loopStart2) );
			//						i = getMapTile(tileP.x, tileP.y).piexl_x;	
			//					}

			//					//根据x得到直线上的y值		//这个是像素点
			//					float yPos = lineFuction(i);

			//					//获取点所在的地砖数组		//注意,此处没有做超出界限的校验
			//					passedNodeList = getNodesUnderPoint( i, yPos, loopDirection );

			//					//检查经过的节点是否有障碍物，若有则返回true
			//					for each( passedNode in passedNodeList )
			//					{
			////						//for test			//TODO TODO
			////						var tileX:uint = passedNode.x;
			////						var tileY:uint = passedNode.y;
			////						var sp:Sprite = getRect(25,25,0xffff00,1,tileX+"\r"+tileY);
			////						sp.x = tileX*SceneConfig.TILE_WIDTH;
			////						sp.y = tileY*SceneConfig.TILE_HEIGHT;
			////						container.addChild(sp);

			//						passTile = getMapTile(passedNode.x, passedNode.y);

			//						//注意 只有地砖存在, 并且是障碍, 才返回true   如果地砖不存在, 就无视, 因为获取地砖列表的时候, 没有存在性校验
			//						if( passTile && passTile.isSolid )		
			//							return true;
			//					}
			//				}


			//			}
			//			else
			//			{
			//				lineFuction = getLineFunc(point1, point2, 1);

			//				//无论起点终点的相互位置如何,  Y方向都是使用从上向下遍历
			//				loopStart = Math.min( startZ, endZ );
			//				loopEnd = Math.max( startZ, endZ );

			//				loopStart2 = (loopStart == startZ) ? startX : endX;

			////				Fun.clearChildren(container);

			//				//开始纵向遍历起点与终点间的节点看是否存在障碍(不可移动点)
			//				for( i=loopStart; i<=loopEnd; i+=SceneConfig.TILE_HEIGHT )
			//				{
			//					//不用判断起点,  所以如果是起点, 那么取出右侧的格子, 然后使用右侧格子的X坐标, 作为要遍历的点
			//					if( i==loopStart )
			//					{
			//						//像素转地图格
			//						tileP = Transformer.transPixelPoint2TilePoint( new Point(loopStart2, loopStart + SceneConfig.TILE_HEIGHT) );
			//						i = getMapTile(tileP.x, tileP.y).piexl_y;
			//					}
			//					//根据y得到直线上的x值		//这个是像素点
			//					var xPos:Number = lineFuction(i);

			//					//获取点所在的地砖数组		//注意,此处没有做超出界限的校验
			//					passedNodeList = getNodesUnderPoint( xPos, i, loopDirection );

			//					//检查经过的节点是否有障碍物，若有则返回true	
			//					for each( passedNode in passedNodeList )
			//					{
			////						//for test			//TODO TODO
			////						var tileX:uint = passedNode.x;
			////						var tileY:uint = passedNode.y;
			////						var sp:Sprite = getRect(25,25,0xffff00,1,tileX+"\r"+tileY);
			////						sp.x = tileX*SceneConfig.TILE_WIDTH;
			////						sp.y = tileY*SceneConfig.TILE_HEIGHT;
			////						container.addChild(sp);

			//						passTile = getMapTile(passedNode.x, passedNode.y);
			//						//注意 只有地砖存在, 并且是障碍, 才返回true   如果地砖不存在, 就无视, 因为获取地砖列表的时候, 没有存在性校验
			//						if( passTile && passTile.isSolid )		
			//							return true;
			//					}
			//				}
			//			}

			//			return false;     
			#endregion
		}

		/// <summary>
		/// 根据xz坐标，获取y坐标，并返回真正的xyz坐标
		/// </summary>
		/// <param name="__pos"></param>
		/// <returns></returns>
		public static zVector3 GetPositionFromXZ(zVector3 __pos)
		{
			return __pos;
		}

		public static zMapTile GetMapTile(zMapPoint __p)
		{
			return GetMapTile(__p.x, __p.y);
		}

		public static zMapTile GetMapTile(int __tileX, int __tileY)
		{
			var vecX = map.mapTiles;
			if (__tileX >= 0 && vecX.Count > __tileX)				//在二维数组中取出对应的地块对象
			{
				var vecY = vecX[__tileX];
				if (__tileY >= 0 && vecY.Count > __tileY)
				{
					return vecY[__tileY];
				}
			}

			return null;
		}

		/// <summary>
		/// 获取格子的可行走形
		/// </summary>
		/// <param name="__x"></param>
		/// <param name="__y"></param>
		/// <returns></returns>
		public static int GetMapTileWalkAble(byte[][] __map, int __x, int __y)
		{
			if (__x >= 0)
				if (__y >= 0)
					if (__map.Length > __x)
					{
						var mapx = __map[__x];
						if (mapx.Length > __y)
							return mapx[__y];
					}
			return -1;
		}

		/// <summary>
		/// 返回离参数位置最近的  可行走的[逻辑格]坐标
		/// </summary>
		/// <param name="__pos"> 世界坐标 </param>
		/// <returns></returns>
		public static zMapPoint GetNearestWalkableTile(zVector3 __pos)
		{
			zMapPoint p = WorldToTile(__pos);
			return GetNearestWalkableTile(p);
		}

		/// <summary>
		/// 返回离参数位置最近的  可行走的[逻辑格]坐标
		/// </summary>
		/// <param name="__tile"> 逻辑格坐标 </param>
		/// <returns></returns>
		public static zMapPoint GetNearestWalkableTile(zMapPoint __tile)
		{
			//获取地砖对象
			zMapTile mt = GetMapTile(__tile);

			if (mt == null)
				return zMapPoint.NULL;

			//如果目标点所处地砖可以行走  则直接返回
			if (mt != null && !mt.isSolid)
				return __tile;

			zMapPoint leftRight = new zMapPoint(__tile.x, __tile.x);
			zMapPoint topBottom = new zMapPoint(__tile.y, __tile.y);


			while (true)
			{
				//向外扩展一圈
				leftRight.x -= 1;
				leftRight.y += 1;
				topBottom.x -= 1;
				topBottom.y += 1;

				//遍历该圈
				zMapPoint tileP;
				zMapTile mapTile;
				int i;
				//必须每个循环重新new
				List<zMapPoint> lpArr = new List<zMapPoint>();

				//上下两行
				for (i = leftRight.x; i <= leftRight.y; i++)
				{
					lpArr.Add(new zMapPoint(i, topBottom.x));
					lpArr.Add(new zMapPoint(i, topBottom.y));
				}
				//左右两行
				for (i = topBottom.x + 1; i < topBottom.y - 1; i++)
				{
					lpArr.Add(new zMapPoint(leftRight.x, i));
					lpArr.Add(new zMapPoint(leftRight.y, i));
				}

				for (i = 0; i < lpArr.Count; i++)
				{
					tileP = lpArr[i];
					//查看存在性
					mapTile = GetMapTile(tileP);
					if (mapTile == null)
					{
						continue;
					}
					//判断障碍
					if (!mapTile.isSolid)
					{
						return tileP;
					}
				}
			}
		}

		#region 二叉树排序
		//======================================================================================================================================================

		/// <summary>
		/// 往开启列表中增加节点 (并进行二叉堆排序)
		/// </summary>
		/// <param name="__open">开启列表</param>
		/// <param name="__nodePoint">要处理的节点</param>
		/// <param name="__index">要更新的的索引</param>
		private static void updatePoint(ref List<SearchingPoint> __open, SearchingPoint __nodePoint, int __index)
		{
			//更新 或 添加
			if (__open.Count <= __index)		//新增
			{
				__open.Add(__nodePoint);
			}
			else            //更新
			{
				__open.RemoveAt(__index);
				__open.Insert(__index, __nodePoint);
			}


			//上冒排序
			int childPos = __index + 1;
			int parentPos = childPos >> 1;
			SearchingPoint child;
			SearchingPoint parent;
			while (parentPos > 0)
			{
				child = __open[childPos - 1];
				parent = __open[parentPos - 1];
				if (child.f < parent.f)
				{
					__open[childPos - 1] = parent;
					__open[parentPos - 1] = child;
					childPos = parentPos;
					parentPos = childPos >> 1;
				}
				else
				{
					break;
				}
			}
		}


		/// <summary>
		/// 从开始列表中 删除某节点  (并进行二叉堆排序)
		/// </summary>
		/// <param name="__openArr">开启列表</param>
		/// <param name="__len">当前长度</param>
		/// <returns>被删除的节点</returns>
		private static SearchingPoint delPoint(ref List<SearchingPoint> __openArr, int __len)
		{
			SearchingPoint rePoint = null;
			//取得首位元素
			if (__openArr.Count > 0)
			{
				rePoint = __openArr[0];
			}
			//下沉排序
			//========================================================================================
			int lastIndex = __len - 1;
			//并将末位元素提到首位，并删除尾部元素
			if (__openArr.Count > 0)
			{
				__openArr[0] = __openArr[lastIndex];
				__openArr.RemoveAt(lastIndex);
			}

			int parentIndex = 0;
			int child1Pos;
			int child2Pos;
			SearchingPoint parent;
			SearchingPoint child1;
			SearchingPoint child2;
			while (((parentIndex + 1) << 1) < lastIndex)
			{//子元素存在
				parent = __openArr[parentIndex];
				child1Pos = ((parentIndex + 1) << 1);
				child2Pos = child1Pos + 1;
				child1 = __openArr[child1Pos - 1];
				child2 = (child2Pos != lastIndex) ? __openArr[child2Pos - 1] : null;

				if (child2 != null)
				{
					if (parent.f < child1.f && parent.f < child2.f) break;

					if (child1.f <= child2.f)
					{
						if (parent.f > child1.f)
						{
							__openArr[parentIndex] = child1;
							__openArr[child1Pos - 1] = parent;
							parentIndex = child1Pos - 1;
						}
						else
						{
							__openArr[parentIndex] = child2;
							__openArr[child2Pos - 1] = parent;
							parentIndex = child2Pos - 1;
						}
					}
					else
					{
						if (parent.f > child2.f)
						{
							__openArr[parentIndex] = child2;
							__openArr[child2Pos - 1] = parent;
							parentIndex = child2Pos - 1;
						}
						else
						{
							__openArr[parentIndex] = child1;
							__openArr[child1Pos - 1] = parent;
							parentIndex = child1Pos - 1;
						}
					}
				}
				else
				{
					if (parent.f > child1.f)
					{
						__openArr[parentIndex] = child1;
						__openArr[child1Pos - 1] = parent;
						parentIndex = child1Pos - 1;
					}
					else
					{
						break;
					}
				}
			}
			//========================================================================================
			return rePoint;
		}


		#endregion


		class SearchingPoint
		{
			public int x;
			public int y;
			public int g;
			public zfloat f;
			public zfloat h;
			public SearchingPoint parentpoint;
		}
	}
}