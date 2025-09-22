using UnityEngine;

namespace InventorySystem
{
    [CreateAssetMenu(fileName = "New Item", menuName = "Inventory System/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        [System.Serializable]
        public class WeaponSpec
        {
            [Header("资源地址（推荐 Addressables/Resources 路径）")]
            [FieldLabel("武器预制体地址")] public string weaponPrefabAddress;
            [FieldLabel("玩家子弹预制体地址")] public string playerBulletPrefabAddress;
            [FieldLabel("敌人子弹预制体地址")] public string enemyBulletPrefabAddress;

            [Header("射击配置")]
            [FieldLabel("开火方式")] public string fireMode; // FullAuto / SemiAuto
            [FieldLabel("射击间隔")] public float fireRate;
            [FieldLabel("散射角度")] public float spreadAngle;
            [FieldLabel("子弹速度")] public float bulletSpeed;
            [FieldLabel("射程")] public float range;
            [FieldLabel("伤害")] public float damage;
            [FieldLabel("弹夹容量")] public int magazineCapacity;
            [FieldLabel("换弹时间")] public float reloadTime;

            /// <summary>
            /// 资源地址是否为 Resources 路径（不含扩展名）
            /// </summary>
            public bool IsResourcesPathValid()
            {
                return !string.IsNullOrEmpty(weaponPrefabAddress);
            }
        }
        [Header("基础信息")]
        [FieldLabel("物品ID")]
        public int id;                          // 物品原始ID
        [FieldLabel("物品名称")]
        public string itemName;                 // 物品名称
        [FieldLabel("物品简称")]
        public string shortName;                // 物品简称
        [FieldLabel("物品类别")]
        public ItemCategory category;           // 物品类别
        [FieldLabel("稀有度")]
        public string rarity;                   // 稀有度
        [FieldLabel("背景颜色")]
        public Color backgroundColor;           // 背景颜色（直接显示颜色）
        [FieldLabel("物品图标")]
        public Sprite itemIcon;                 // 物品图标精灵引用

        [Header("尺寸属性")]
        [FieldLabel("物品高度")]
        public int height = 1;                  // 物品高度
        [FieldLabel("物品宽度")]
        public int width = 1;                   // 物品宽度

        [Header("装备属性")]
        [FieldLabel("耐久度")]
        public int durability = 0;              // 耐久度（头盔、护甲等）

        [Header("容器属性")]
        [FieldLabel("水平格子数")]
        public int cellH = 0;                   // 容器水平格子数（战术背心、背包等）
        [FieldLabel("垂直格子数")]
        public int cellV = 0;                   // 容器垂直格子数（战术背心、背包等）

        [Header("弹药属性")]
        [FieldLabel("弹药类型")]
        public string ammunitionType;           // 弹药类型（pistol, assault_rifle, submachine_gun等）
        [FieldLabel("最大堆叠数量")]
        public int maxStack = 1;                // 最大堆叠数量

        [Header("消耗品属性")]
        [FieldLabel("使用次数")]
        public int usageCount = 0;              // 使用次数（食物、饮料、药品等）

        [Header("治疗属性")]
        [FieldLabel("治疗量")]
        public int maxHealAmount = 0;           // 治疗量（治疗药物）
        [FieldLabel("单次治疗量")]
        public int healPerUse = 0;              // 每次使用的治疗量（治疗药物）

        [Header("生存恢复属性")]
        [FieldLabel("恢复饱食度")]
        public int hungerRestore = 0;           // 食物/饮料使用时回复的饱食度
        [FieldLabel("恢复精神值")]
        public int mentalRestore = 0;           // 镇静类药品使用时回复的精神值

        [Header("情报属性")]
        [FieldLabel("情报值")]
        public int intelligenceValue = 0;       // 情报值（情报物品）

		[Header("武器弹药（仅武器类使用）")]
		[FieldLabel("可用弹药列表")]
		public string[] ammunitionOptions;       // 武器可用的弹药名列表（来自 JSON 的 Ammunition 数组）

        [Header("武器扩展（仅武器类使用）")]
        public WeaponSpec weapon;               // 可为空；当为武器类目时由生成器填充

        [Header("运行时数据")]
        [FieldLabel("全局唯一ID")]
        [SerializeField] private long globalId; // 全局唯一ID（long类型）

        /// <summary>
        /// 获取全局唯一ID
        /// </summary>
        public long GlobalId => globalId;

        /// <summary>
        /// 设置全局唯一ID（仅供生成器使用）
        /// </summary>
        public void SetGlobalId(long id)
        {
            globalId = id;
        }

        /// <summary>
        /// 根据珍贵等级获取背景颜色
        /// </summary>
        public Color GetBackgroundColor()
        {
            switch (rarity)
            {
                case "1":
                    return HexToColor("#2d3c4b"); // 普通 - 深蓝灰色
                case "2":
                    return HexToColor("#583b80"); // 稀有 - 紫色
                case "3":
                    return HexToColor("#80550d"); // 史诗 - 橙色
                case "4":
                    return HexToColor("#350000"); // 传说 - 深红色
                default:
                    return HexToColor("#2d3c4b"); // 默认普通等级
            }
        }

        /// <summary>
        /// 将十六进制颜色字符串转换为Color
        /// </summary>
        private Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                return color;
            }
            return Color.white; // 解析失败时返回白色
        }

        /// <summary>
        /// 设置物品背景颜色（根据珍贵等级自动设置）
        /// </summary>
        public void SetBackgroundColorByRarity()
        {
            backgroundColor = GetBackgroundColor();
        }

        /// <summary>
        /// 创建物品实例的副本
        /// </summary>
        public ItemDataSO CreateInstance()
        {
            ItemDataSO instance = Instantiate(this);
            return instance;
        }

        /// <summary>
        /// 获取物品显示名称
        /// </summary>
        public string GetDisplayName()
        {
            return !string.IsNullOrEmpty(shortName) ? shortName : itemName;
        }

        /// <summary>
        /// 检查是否为容器类物品
        /// </summary>
        public bool IsContainer()
        {
            return cellH > 0 && cellV > 0;
        }

        /// <summary>
        /// 检查是否为可堆叠物品
        /// </summary>
        public bool IsStackable()
        {
            return maxStack > 1;
        }

        /// <summary>
        /// 检查是否为消耗品
        /// </summary>
        public bool IsConsumable()
        {
            return usageCount > 0;
        }

        /// <summary>
        /// 检查是否有耐久度
        /// </summary>
        public bool HasDurability()
        {
            return durability > 0;
        }

        // -------------------- Resources 加载便捷方法（仅在使用 Resources 工作流时调用） --------------------
        /// <summary>
        /// 从 Resources 加载武器预制体（无扩展名路径）。返回 null 表示未配置或未找到。
        /// </summary>
        public GameObject LoadWeaponPrefabFromResources()
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.weaponPrefabAddress)) return null;
            return Resources.Load<GameObject>(weapon.weaponPrefabAddress);
        }

        /// <summary>
        /// 从 Resources 加载玩家子弹预制体（无扩展名路径）。返回 null 表示未配置或未找到。
        /// </summary>
        public GameObject LoadPlayerBulletPrefabFromResources()
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.playerBulletPrefabAddress)) return null;
            return Resources.Load<GameObject>(weapon.playerBulletPrefabAddress);
        }

        /// <summary>
        /// 从 Resources 加载敌人子弹预制体（无扩展名路径）。返回 null 表示未配置或未找到。
        /// </summary>
        public GameObject LoadEnemyBulletPrefabFromResources()
        {
            if (weapon == null || string.IsNullOrEmpty(weapon.enemyBulletPrefabAddress)) return null;
            return Resources.Load<GameObject>(weapon.enemyBulletPrefabAddress);
        }
    }
}