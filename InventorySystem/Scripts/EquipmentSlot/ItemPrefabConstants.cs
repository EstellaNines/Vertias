using UnityEngine;

namespace InventorySystem
{
    /// <summary>
    /// 物品预制件的常量配置
    /// 基于标准物品预制件结构定义的默认值
    /// </summary>
    public static class ItemPrefabConstants
    {
        /// <summary>
        /// 子组件名称
        /// </summary>
        public static class ChildNames
        {
            public const string ItemBackground = "ItemBackground";
            public const string ItemIcon = "ItemIcon";
            public const string ItemHighlight = "ItemHighlight";
            public const string ItemText = "ItemText";
        }
        
        /// <summary>
        /// 物品文本组件的默认配置
        /// 基于预制件中的设置: m_AnchoredPosition: {x: 93, y: -93}, m_SizeDelta: {x: 76.8, y: 57.600002}
        /// </summary>
        public static class ItemTextDefaults
        {
            public static readonly Vector2 OriginalPosition = new Vector2(93f, -93f);
            public static readonly Vector2 OriginalSize = new Vector2(76.8f, 57.6f);
            public const float OriginalFontSize = 32f;
            
            /// <summary>
            /// 字体缩放阈值，低于此值时会同步缩放字体大小
            /// </summary>
            public const float FontScaleThreshold = 0.8f;
        }
    }
}
