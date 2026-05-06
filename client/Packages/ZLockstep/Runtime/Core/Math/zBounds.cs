
namespace zUnity
{
	/// <summary>
	/// 实现结构体Bounds
	/// </summary>
	public struct zBounds
	{
		private zVector3 m_Center;
		private zVector3 m_Extents;
		public zVector3 center
		{
			get
			{
				return this.m_Center;
			}
			set
			{
				this.m_Center = value;
			}
		}
		public zVector3 size
		{
			get
			{
				return this.m_Extents * zfloat.Two;
			}
			set
			{
				this.m_Extents = value * zfloat.Half;
			}
		}
		public zVector3 extents
		{
			get
			{
				return this.m_Extents;
			}
			set
			{
				this.m_Extents = value;
			}
		}
		public zVector3 min
		{
			get
			{
				return this.center - this.extents;
			}
			set
			{
				this.SetMinMax(value, this.max);
			}
		}
		public zVector3 max
		{
			get
			{
				return this.center + this.extents;
			}
			set
			{
				this.SetMinMax(this.min, value);
			}
		}
		public zBounds(zVector3 center, zVector3 size)
		{
			this.m_Center = center;
			this.m_Extents = size * zfloat.Half;
		}
		public override int GetHashCode()
		{
			return this.center.GetHashCode() ^ this.extents.GetHashCode() << 2;
		}
		public override bool Equals(object other)
		{
			if (!(other is zBounds))
			{
				return false;
			}
			zBounds bounds = (zBounds)other;
			return this.center.Equals(bounds.center) && this.extents.Equals(bounds.extents);
		}
		public void SetMinMax(zVector3 min, zVector3 max)
		{
			this.extents = (max - min) * zfloat.Half;
			this.center = min + this.extents;
		}
		public void Encapsulate(zVector3 point)
		{
			this.SetMinMax(zVector3.Min(this.min, point), zVector3.Max(this.max, point));
		}
		public void Encapsulate(zBounds bounds)
		{
			this.Encapsulate(bounds.center - bounds.extents);
			this.Encapsulate(bounds.center + bounds.extents);
		}
		public void Expand(zfloat amount)
		{
			amount *= zfloat.Half;
			this.extents += new zVector3(amount, amount, amount);
		}
		public void Expand(zVector3 amount)
		{
			this.extents += amount * zfloat.Half;
		}
		public bool Intersects(zBounds bounds)
		{
			return this.min.x <= bounds.max.x && this.max.x >= bounds.min.x && this.min.y <= bounds.max.y && this.max.y >= bounds.min.y && this.min.z <= bounds.max.z && this.max.z >= bounds.min.z;
		}
	}
}