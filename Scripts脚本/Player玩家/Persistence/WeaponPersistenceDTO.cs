using System;

namespace Persistence
{
	[Serializable]
	public class WeaponSlotStateDTO
	{
		public bool hasWeapon;
		public int itemId;           // 物品数据库ID（可选）
		public long itemGlobalId;    // SO 运行时全局ID（可选）
		public int ammo;             // 当前弹药
		public string fireMode;      // FullAuto / SemiAuto
		public bool isActive;        // 是否当前激活槽
		public float rotationZ;      // 武器本地Z轴旋转
		public long savedAt;         // 时间戳（ticks）
	}

	[Serializable]
	public class WeaponPersistStateDTO
	{
		public int version = 1;
		public int activeSlot = 0;
		public WeaponSlotStateDTO[] slots = new WeaponSlotStateDTO[2]
		{
			new WeaponSlotStateDTO(),
			new WeaponSlotStateDTO()
		};
	}
}
