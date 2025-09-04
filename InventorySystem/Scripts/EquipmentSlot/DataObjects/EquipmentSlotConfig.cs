using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace InventorySystem
{
    /// <summary>
    /// 装备槽配置数据
    /// 用于配置装备槽的各种属性和限制条件
    /// </summary>
    [CreateAssetMenu(fileName = "New Equipment Slot Config", menuName = "Inventory System/Equipment Slot Config")]
    public class EquipmentSlotConfig : ScriptableObject
    {
        [Header("槽位基础信息")]
        [FieldLabel("槽位类型")]
        public EquipmentSlotType slotType = EquipmentSlotType.Helmet;

        [FieldLabel("槽位名称")]
        public string slotName = "装备槽";

        [FieldLabel("槽位描述")]
        [TextArea(2, 4)]
        public string slotDescription = "装备槽描述";

        [Header("装备限制配置")]
        [FieldLabel("允许的物品类别")]
        [Tooltip("该槽位可以装备的物品类别列表")]
        public List<ItemCategory> allowedCategories = new List<ItemCategory>();

        [FieldLabel("最大物品尺寸")]
        [Tooltip("允许装备的物品最大尺寸（宽x高）")]
        public Vector2Int maxItemSize = new Vector2Int(3, 3);

        [FieldLabel("最小物品尺寸")]
        [Tooltip("允许装备的物品最小尺寸（宽x高）")]
        public Vector2Int minItemSize = new Vector2Int(1, 1);

        [FieldLabel("允许旋转装备")]
        [Tooltip("是否允许物品旋转后装备")]
        public bool allowRotatedItems = false;

        [FieldLabel("严格尺寸匹配")]
        [Tooltip("是否要求物品尺寸严格匹配槽位大小")]
        public bool strictSizeMatch = false;

        [Header("空槽显示配置")]
        [FieldLabel("空槽图标")]
        [Tooltip("槽位为空时显示的图标")]
        public Sprite emptySlotIcon;

        [FieldLabel("空槽文本")]
        [Tooltip("槽位为空时显示的文本")]
        public string emptySlotText = "空";

        [FieldLabel("空槽颜色")]
        [Tooltip("槽位为空时的背景颜色")]
        public Color emptySlotColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);

        [Header("装备状态显示")]
        [FieldLabel("装备状态颜色")]
        [Tooltip("有装备时的背景颜色")]
        public Color equippedSlotColor = new Color(1f, 1f, 1f, 1f);

        [FieldLabel("可装备高亮颜色")]
        [Tooltip("可以装备物品时的高亮颜色")]
        public Color canEquipHighlightColor = new Color(0f, 1f, 0f, 0.5f);

        [FieldLabel("不可装备高亮颜色")]
        [Tooltip("不能装备物品时的高亮颜色")]
        public Color cannotEquipHighlightColor = new Color(1f, 0f, 0f, 0.5f);

        [Header("视觉配置")]
        [FieldLabel("装备物品内边距")]
        [Tooltip("装备物品在槽位内的内边距（像素）")]
        [Range(0f, 20f)]
        public float itemPadding = 6f;

        [Header("容器特性配置")]
        [FieldLabel("是否为容器槽位")]
        [Tooltip("该槽位是否支持容器类装备（如背包、战术背心）")]
        public bool isContainerSlot = false;

        [FieldLabel("容器网格预制件")]
        [Tooltip("容器装备时显示的网格预制件")]
        public GameObject containerGridPrefab;

        [FieldLabel("默认容器网格尺寸")]
        [Tooltip("容器的默认内部网格尺寸")]
        public Vector2Int defaultContainerSize = new Vector2Int(4, 4);

        [FieldLabel("容器显示位置偏移")]
        [Tooltip("容器网格相对于槽位的显示偏移")]
        public Vector2 containerDisplayOffset = Vector2.zero;

        [Header("预设数据配置")]
        [FieldLabel("兼容物品列表")]
        [Tooltip("该槽位兼容的物品数据列表，用于验证和提示")]
        public List<ItemDataSO> compatibleItems = new List<ItemDataSO>();

        [FieldLabel("默认装备物品")]
        [Tooltip("槽位的默认装备物品（可选）")]
        public ItemDataSO defaultEquipment;

        [Header("高级配置")]
        [FieldLabel("装备条件检查")]
        [Tooltip("是否启用额外的装备条件检查")]
        public bool enableAdvancedValidation = false;

        [FieldLabel("允许替换装备")]
        [Tooltip("是否允许直接替换已装备的物品")]
        public bool allowEquipmentReplacement = true;

        [FieldLabel("卸装时返回原位置")]
        [Tooltip("卸装物品时是否尝试返回原网格位置")]
        public bool returnToOriginalPosition = true;

        [FieldLabel("装备槽优先级")]
        [Tooltip("装备槽的优先级，用于自动装备时的选择")]
        [Range(1, 10)]
        public int slotPriority = 5;

        /// <summary>
        /// 检查物品是否与该槽位兼容
        /// </summary>
        /// <param name="itemData">要检查的物品数据</param>
        /// <returns>是否兼容</returns>
        public bool IsItemCompatible(ItemDataSO itemData)
        {
            if (itemData == null) return false;

            // 检查类别是否允许
            if (!allowedCategories.Contains(itemData.category))
                return false;

            // 检查尺寸是否符合要求
            Vector2Int itemSize = new Vector2Int(itemData.width, itemData.height);

            if (!IsItemSizeValid(itemSize))
                return false;

            return true;
        }

        /// <summary>
        /// 检查物品尺寸是否有效
        /// </summary>
        /// <param name="itemSize">物品尺寸</param>
        /// <returns>尺寸是否有效</returns>
        public bool IsItemSizeValid(Vector2Int itemSize)
        {
            // 检查最大尺寸限制
            if (itemSize.x > maxItemSize.x || itemSize.y > maxItemSize.y)
                return false;

            // 检查最小尺寸限制
            if (itemSize.x < minItemSize.x || itemSize.y < minItemSize.y)
                return false;

            // 如果需要严格匹配，检查是否完全相等
            if (strictSizeMatch)
            {
                return itemSize == maxItemSize;
            }

            return true;
        }

        /// <summary>
        /// 获取槽位类型的显示名称
        /// </summary>
        /// <returns>显示名称</returns>
        public string GetSlotTypeDisplayName()
        {
            switch (slotType)
            {
                case EquipmentSlotType.Helmet: return "头盔";
                case EquipmentSlotType.Armor: return "护甲";
                case EquipmentSlotType.TacticalRig: return "战术背心";
                case EquipmentSlotType.Backpack: return "背包";
                case EquipmentSlotType.PrimaryWeapon: return "主武器";
                case EquipmentSlotType.SecondaryWeapon: return "副武器";
                default: return slotType.ToString();
            }
        }

        /// <summary>
        /// 获取允许的物品类别的显示字符串
        /// </summary>
        /// <returns>类别显示字符串</returns>
        public string GetAllowedCategoriesDisplay()
        {
            if (allowedCategories == null || allowedCategories.Count == 0)
                return "无限制";

            var categoryNames = new List<string>();
            foreach (var category in allowedCategories)
            {
                categoryNames.Add(GetCategoryDisplayName(category));
            }

            return string.Join(", ", categoryNames);
        }

        /// <summary>
        /// 获取物品类别的显示名称
        /// </summary>
        /// <param name="category">物品类别</param>
        /// <returns>显示名称</returns>
        private string GetCategoryDisplayName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Helmet: return "头盔";
                case ItemCategory.Armor: return "护甲";
                case ItemCategory.TacticalRig: return "战术背心";
                case ItemCategory.Backpack: return "背包";
                case ItemCategory.Weapon: return "武器";
                case ItemCategory.Ammunition: return "弹药";
                case ItemCategory.Food: return "食物";
                case ItemCategory.Drink: return "饮料";
                case ItemCategory.Sedative: return "镇静剂";
                case ItemCategory.Hemostatic: return "止血剂";
                case ItemCategory.Healing: return "治疗药物";
                case ItemCategory.Intelligence: return "情报";
                case ItemCategory.Currency: return "货币";
                case ItemCategory.Special: return "特殊物品";
                default: return category.ToString();
            }
        }

        /// <summary>
        /// 验证配置的有效性
        /// </summary>
        /// <returns>验证结果和错误信息</returns>
        public (bool isValid, string errorMessage) ValidateConfig()
        {
            // 检查基础配置
            if (string.IsNullOrEmpty(slotName))
                return (false, "槽位名称不能为空");

            if (allowedCategories == null || allowedCategories.Count == 0)
                return (false, "至少需要指定一个允许的物品类别");

            // 检查尺寸配置
            if (maxItemSize.x <= 0 || maxItemSize.y <= 0)
                return (false, "最大物品尺寸必须大于0");

            if (minItemSize.x <= 0 || minItemSize.y <= 0)
                return (false, "最小物品尺寸必须大于0");

            if (minItemSize.x > maxItemSize.x || minItemSize.y > maxItemSize.y)
                return (false, "最小物品尺寸不能大于最大物品尺寸");

            // 检查容器配置
            if (isContainerSlot)
            {
                if (defaultContainerSize.x <= 0 || defaultContainerSize.y <= 0)
                    return (false, "容器网格尺寸必须大于0");
            }

            return (true, string.Empty);
        }

#if UNITY_EDITOR
        /// <summary>
        /// 在Inspector中验证配置
        /// </summary>
        private void OnValidate()
        {
            // 确保尺寸值合理
            maxItemSize.x = Mathf.Max(1, maxItemSize.x);
            maxItemSize.y = Mathf.Max(1, maxItemSize.y);
            minItemSize.x = Mathf.Max(1, minItemSize.x);
            minItemSize.y = Mathf.Max(1, minItemSize.y);

            // 确保最小尺寸不大于最大尺寸
            if (minItemSize.x > maxItemSize.x) minItemSize.x = maxItemSize.x;
            if (minItemSize.y > maxItemSize.y) minItemSize.y = maxItemSize.y;

            // 确保容器尺寸合理
            if (isContainerSlot)
            {
                defaultContainerSize.x = Mathf.Max(1, defaultContainerSize.x);
                defaultContainerSize.y = Mathf.Max(1, defaultContainerSize.y);
            }

            // 自动设置槽位名称
            if (string.IsNullOrEmpty(slotName))
            {
                slotName = GetSlotTypeDisplayName();
            }
        }
#endif
    }
}
