using System;
namespace zUnity
{
	/// <summary>
	/// 射线结构体
	/// </summary>
	public struct zRay
	{

		public zVector3 origin;

		public zVector3 direction;

		public zRay(zVector3 __origin, zVector3 __direction)
		{
			this.origin = __origin;

			this.direction = __direction;
		}
		public zVector3 GetOrigin()
		{
			return origin;
		}

		public zVector3 GetDirection()
		{
			return direction;
		}
	}
}