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
    /// 物品文本组件的通用配置
    /// 基于相对位置计算，适用于任意尺寸的物品
    /// </summary>
    public static class ItemTextDefaults
    {
        /// <summary>
        /// 文本相对于物品右下角的偏移（像素）
        /// </summary>
        public static readonly Vector2 RightBottomOffset = new Vector2(-3f, 3f);
        
        /// <summary>
        /// 文本组件的默认尺寸（相对于物品尺寸的比例）
        /// </summary>
        public static readonly Vector2 SizeRatio = new Vector2(0.4f, 0.3f);
        
        /// <summary>
        /// 文本组件的最小尺寸（像素）
        /// </summary>
        public static readonly Vector2 MinSize = new Vector2(60f, 40f);
        
        /// <summary>
        /// 文本组件的最大尺寸（像素）
        /// </summary>
        public static readonly Vector2 MaxSize = new Vector2(120f, 80f);
        
        /// <summary>
        /// 默认字体大小
        /// </summary>
        public const float DefaultFontSize = 32f;
        
        /// <summary>
        /// 最小字体大小
        /// </summary>
        public const float MinFontSize = 16f;
        
        /// <summary>
        /// 字体缩放阈值，低于此值时会同步缩放字体大小
        /// </summary>
        public const float FontScaleThreshold = 0.8f;
        
        /// <summary>
        /// 计算文本在物品中的位置（右下角）
        /// </summary>
        /// <param name="itemSize">物品尺寸</param>
        /// <returns>文本锚点位置</returns>
        public static Vector2 CalculateTextPosition(Vector2 itemSize)
        {
            return new Vector2(
                itemSize.x * 0.5f + RightBottomOffset.x,
                -itemSize.y * 0.5f + RightBottomOffset.y
            );
        }
        
        /// <summary>
        /// 计算文本组件的尺寸
        /// </summary>
        /// <param name="itemSize">物品尺寸</param>
        /// <returns>文本组件尺寸</returns>
        public static Vector2 CalculateTextSize(Vector2 itemSize)
        {
            Vector2 calculatedSize = new Vector2(
                itemSize.x * SizeRatio.x,
                itemSize.y * SizeRatio.y
            );
            
            // 限制在最小和最大尺寸之间
            calculatedSize.x = Mathf.Clamp(calculatedSize.x, MinSize.x, MaxSize.x);
            calculatedSize.y = Mathf.Clamp(calculatedSize.y, MinSize.y, MaxSize.y);
            
            return calculatedSize;
        }
        
        /// <summary>
        /// 计算适合的字体大小
        /// </summary>
        /// <param name="itemSize">物品尺寸</param>
        /// <param name="scale">缩放比例</param>
        /// <returns>字体大小</returns>
        public static float CalculateFontSize(Vector2 itemSize, float scale = 1f)
        {
            // 基于物品尺寸的较小边计算字体大小
            float baseSize = Mathf.Min(itemSize.x, itemSize.y);
            float fontSize = (baseSize / 6f) * scale; // 基础比例
            
            // 限制字体大小范围
            fontSize = Mathf.Clamp(fontSize, MinFontSize, DefaultFontSize);
            
            return fontSize;
        }
    }
    }
}
