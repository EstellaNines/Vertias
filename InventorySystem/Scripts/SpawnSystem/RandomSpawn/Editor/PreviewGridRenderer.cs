using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using InventorySystem;

namespace InventorySystem.SpawnSystem.Editor
{
    /// <summary>
    /// 预览网格渲染器
    /// 在编辑器Inspector中提供可视化的网格预览功能，显示随机生成的物品布局
    /// </summary>
    public static class PreviewGridRenderer
    {
        /// <summary>
        /// 预览网格的默认尺寸
        /// </summary>
        public static readonly Vector2Int DefaultGridSize = new Vector2Int(10, 6);
        
        /// <summary>
        /// 网格单元格大小（像素）
        /// </summary>
        private const float CellSize = 32f;
        
        /// <summary>
        /// 网格线宽度
        /// </summary>
        private const float GridLineWidth = 1f;
        
        /// <summary>
        /// 预览物品数据
        /// </summary>
        public struct PreviewItemDisplay
        {
            public Vector2Int position;         // 网格位置
            public Vector2Int size;             // 物品尺寸
            public ItemDataSO itemData;         // 物品数据
            public ItemRarity rarity;           // 珍稀度
            public Color backgroundColor;       // 背景颜色
            public string displayText;          // 显示文本
        }
        
        /// <summary>
        /// 渲染预览网格
        /// </summary>
        /// <param name="rect">渲染区域</param>
        /// <param name="gridSize">网格尺寸</param>
        /// <param name="previewItems">预览物品列表</param>
        /// <param name="showGrid">是否显示网格线</param>
        /// <param name="showLabels">是否显示物品标签</param>
        public static void RenderPreviewGrid(Rect rect, Vector2Int gridSize, List<PreviewItemDisplay> previewItems = null, 
                                           bool showGrid = true, bool showLabels = true)
        {
            if (gridSize.x <= 0 || gridSize.y <= 0) return;
            
            // 计算网格渲染尺寸
            float gridWidth = gridSize.x * CellSize;
            float gridHeight = gridSize.y * CellSize;
            
            // 居中显示
            float offsetX = (rect.width - gridWidth) * 0.5f;
            float offsetY = (rect.height - gridHeight) * 0.5f;
            
            Rect gridRect = new Rect(rect.x + offsetX, rect.y + offsetY, gridWidth, gridHeight);
            
            // 绘制背景
            DrawGridBackground(gridRect, gridSize);
            
            // 绘制网格线
            if (showGrid)
            {
                DrawGridLines(gridRect, gridSize);
            }
            
            // 绘制预览物品
            if (previewItems != null && previewItems.Count > 0)
            {
                DrawPreviewItems(gridRect, gridSize, previewItems, showLabels);
            }
            
            // 绘制边框
            DrawGridBorder(gridRect);
        }
        
        /// <summary>
        /// 生成预览物品显示数据
        /// </summary>
        /// <param name="config">随机配置</param>
        /// <param name="gridSize">网格尺寸</param>
        /// <returns>预览物品显示列表</returns>
        public static List<PreviewItemDisplay> GeneratePreview(RandomItemSpawnConfig config, Vector2Int gridSize)
        {
            var previewItems = new List<PreviewItemDisplay>();
            
            if (config == null || !config.enablePreview) return previewItems;
            
            // 生成预览物品数据
            var selectedItems = config.GeneratePreviewItems();
            if (selectedItems.Count == 0) return previewItems;
            
            // 简单的网格放置算法
            var occupiedCells = new bool[gridSize.x, gridSize.y];
            int itemIndex = 0;
            
            foreach (var item in selectedItems)
            {
                if (itemIndex >= gridSize.x * gridSize.y) break;
                
                // 寻找可用位置
                Vector2Int position = FindAvailablePosition(occupiedCells, gridSize, GetItemSize(item.itemData));
                if (position.x >= 0 && position.y >= 0)
                {
                    var displayItem = new PreviewItemDisplay
                    {
                        position = position,
                        size = GetItemSize(item.itemData),
                        itemData = item.itemData,
                        rarity = item.rarity,
                        backgroundColor = GetRarityColor(item.rarity),
                        displayText = GetItemDisplayText(item.itemData, item.rarity)
                    };
                    
                    previewItems.Add(displayItem);
                    MarkCellsOccupied(occupiedCells, position, displayItem.size);
                }
                
                itemIndex++;
            }
            
            return previewItems;
        }
        
        /// <summary>
        /// 清除预览
        /// </summary>
        /// <param name="rect">渲染区域</param>
        /// <param name="gridSize">网格尺寸</param>
        public static void ClearPreview(Rect rect, Vector2Int gridSize)
        {
            RenderPreviewGrid(rect, gridSize, null, true, false);
        }
        
        /// <summary>
        /// 绘制预览控制按钮
        /// </summary>
        /// <param name="config">随机配置</param>
        /// <returns>是否需要重新生成预览</returns>
        public static bool DrawPreviewControls(RandomItemSpawnConfig config)
        {
            if (config == null) return false;
            
            EditorGUILayout.BeginHorizontal();
            
            bool regenerate = false;
            
            if (GUILayout.Button("生成预览", GUILayout.Width(80)))
            {
                regenerate = true;
            }
            
            if (GUILayout.Button("随机种子", GUILayout.Width(80)))
            {
                config.previewSeed = Random.Range(1, 999999);
                EditorUtility.SetDirty(config);
                regenerate = true;
            }
            
            if (GUILayout.Button("清空预览", GUILayout.Width(80)))
            {
                // 这里可以添加清空预览的逻辑
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 预览设置
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("预览种子:", GUILayout.Width(60));
            config.previewSeed = EditorGUILayout.IntField(config.previewSeed, GUILayout.Width(80));
            
            EditorGUILayout.LabelField("网格尺寸:", GUILayout.Width(60));
            config.previewGridSize = EditorGUILayout.Vector2IntField("", config.previewGridSize, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
            
            return regenerate;
        }
        
        #region 私有绘制方法
        
        /// <summary>
        /// 绘制网格背景
        /// </summary>
        private static void DrawGridBackground(Rect gridRect, Vector2Int gridSize)
        {
            EditorGUI.DrawRect(gridRect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
        }
        
        /// <summary>
        /// 绘制网格线
        /// </summary>
        private static void DrawGridLines(Rect gridRect, Vector2Int gridSize)
        {
            Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.8f);
            
            // 垂直线
            for (int x = 0; x <= gridSize.x; x++)
            {
                float lineX = gridRect.x + x * CellSize;
                Rect lineRect = new Rect(lineX, gridRect.y, GridLineWidth, gridRect.height);
                EditorGUI.DrawRect(lineRect, lineColor);
            }
            
            // 水平线
            for (int y = 0; y <= gridSize.y; y++)
            {
                float lineY = gridRect.y + y * CellSize;
                Rect lineRect = new Rect(gridRect.x, lineY, gridRect.width, GridLineWidth);
                EditorGUI.DrawRect(lineRect, lineColor);
            }
        }
        
        /// <summary>
        /// 绘制网格边框
        /// </summary>
        private static void DrawGridBorder(Rect gridRect)
        {
            Color borderColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            float borderWidth = 2f;
            
            // 上边框
            EditorGUI.DrawRect(new Rect(gridRect.x, gridRect.y, gridRect.width, borderWidth), borderColor);
            // 下边框
            EditorGUI.DrawRect(new Rect(gridRect.x, gridRect.yMax - borderWidth, gridRect.width, borderWidth), borderColor);
            // 左边框
            EditorGUI.DrawRect(new Rect(gridRect.x, gridRect.y, borderWidth, gridRect.height), borderColor);
            // 右边框
            EditorGUI.DrawRect(new Rect(gridRect.xMax - borderWidth, gridRect.y, borderWidth, gridRect.height), borderColor);
        }
        
        /// <summary>
        /// 绘制预览物品
        /// </summary>
        private static void DrawPreviewItems(Rect gridRect, Vector2Int gridSize, List<PreviewItemDisplay> previewItems, bool showLabels)
        {
            foreach (var item in previewItems)
            {
                DrawPreviewItem(gridRect, gridSize, item, showLabels);
            }
        }
        
        /// <summary>
        /// 绘制单个预览物品
        /// </summary>
        private static void DrawPreviewItem(Rect gridRect, Vector2Int gridSize, PreviewItemDisplay item, bool showLabels)
        {
            // 计算物品在网格中的位置和尺寸
            float itemX = gridRect.x + item.position.x * CellSize;
            float itemY = gridRect.y + item.position.y * CellSize;
            float itemWidth = item.size.x * CellSize;
            float itemHeight = item.size.y * CellSize;
            
            Rect itemRect = new Rect(itemX, itemY, itemWidth, itemHeight);
            
            // 绘制物品背景
            EditorGUI.DrawRect(itemRect, item.backgroundColor);
            
            // 绘制物品边框
            Color borderColor = Color.Lerp(item.backgroundColor, Color.black, 0.3f);
            DrawItemBorder(itemRect, borderColor);
            
            // 绘制物品图标（如果有）
            if (item.itemData != null && item.itemData.itemIcon != null)
            {
                DrawItemIcon(itemRect, item.itemData.itemIcon);
            }
            
            // 绘制标签
            if (showLabels && !string.IsNullOrEmpty(item.displayText))
            {
                DrawItemLabel(itemRect, item.displayText, item.rarity);
            }
        }
        
        /// <summary>
        /// 绘制物品边框
        /// </summary>
        private static void DrawItemBorder(Rect itemRect, Color borderColor)
        {
            float borderWidth = 1f;
            
            // 上边框
            EditorGUI.DrawRect(new Rect(itemRect.x, itemRect.y, itemRect.width, borderWidth), borderColor);
            // 下边框
            EditorGUI.DrawRect(new Rect(itemRect.x, itemRect.yMax - borderWidth, itemRect.width, borderWidth), borderColor);
            // 左边框
            EditorGUI.DrawRect(new Rect(itemRect.x, itemRect.y, borderWidth, itemRect.height), borderColor);
            // 右边框
            EditorGUI.DrawRect(new Rect(itemRect.xMax - borderWidth, itemRect.y, borderWidth, itemRect.height), borderColor);
        }
        
        /// <summary>
        /// 绘制物品图标
        /// </summary>
        private static void DrawItemIcon(Rect itemRect, Sprite icon)
        {
            if (icon == null) return;
            
            // 计算图标区域（留出边距）
            float margin = 4f;
            Rect iconRect = new Rect(
                itemRect.x + margin,
                itemRect.y + margin,
                itemRect.width - margin * 2,
                itemRect.height - margin * 2
            );
            
            // 绘制图标
            GUI.DrawTexture(iconRect, icon.texture, ScaleMode.ScaleToFit);
        }
        
        /// <summary>
        /// 绘制物品标签
        /// </summary>
        private static void DrawItemLabel(Rect itemRect, string text, ItemRarity rarity)
        {
            if (string.IsNullOrEmpty(text)) return;
            
            // 设置标签样式
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 8,
                normal = { textColor = GetRarityTextColor(rarity) }
            };
            
            // 绘制标签背景
            Rect labelRect = new Rect(itemRect.x, itemRect.yMax - 16, itemRect.width, 16);
            EditorGUI.DrawRect(labelRect, new Color(0, 0, 0, 0.7f));
            
            // 绘制标签文本
            EditorGUI.LabelField(labelRect, text, labelStyle);
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 获取物品尺寸
        /// </summary>
        private static Vector2Int GetItemSize(ItemDataSO itemData)
        {
            if (itemData == null) return Vector2Int.one;
            
            // 这里应该从ItemDataSO中获取实际尺寸
            // 目前使用默认值
            return new Vector2Int(itemData.width, itemData.height);
        }
        
        /// <summary>
        /// 获取珍稀度颜色
        /// </summary>
        private static Color GetRarityColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return new Color(0.8f, 0.8f, 0.8f, 0.7f);      // 灰色
                case ItemRarity.Rare:
                    return new Color(0.4f, 0.8f, 0.4f, 0.7f);      // 绿色
                case ItemRarity.Epic:
                    return new Color(0.6f, 0.4f, 0.8f, 0.7f);      // 紫色
                case ItemRarity.Legendary:
                    return new Color(1f, 0.8f, 0.2f, 0.7f);        // 金色
                default:
                    return new Color(0.5f, 0.5f, 0.5f, 0.7f);
            }
        }
        
        /// <summary>
        /// 获取珍稀度文本颜色
        /// </summary>
        private static Color GetRarityTextColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common:
                    return Color.white;
                case ItemRarity.Rare:
                    return Color.white;
                case ItemRarity.Epic:
                    return Color.white;
                case ItemRarity.Legendary:
                    return Color.black;
                default:
                    return Color.white;
            }
        }
        
        /// <summary>
        /// 获取物品显示文本
        /// </summary>
        private static string GetItemDisplayText(ItemDataSO itemData, ItemRarity rarity)
        {
            if (itemData == null) return "?";
            
            // 截取物品名称的前几个字符
            string itemName = itemData.itemName;
            if (itemName.Length > 4)
            {
                itemName = itemName.Substring(0, 4) + "..";
            }
            
            return itemName;
        }
        
        /// <summary>
        /// 寻找可用位置
        /// </summary>
        private static Vector2Int FindAvailablePosition(bool[,] occupiedCells, Vector2Int gridSize, Vector2Int itemSize)
        {
            for (int y = 0; y <= gridSize.y - itemSize.y; y++)
            {
                for (int x = 0; x <= gridSize.x - itemSize.x; x++)
                {
                    if (CanPlaceItem(occupiedCells, new Vector2Int(x, y), itemSize))
                    {
                        return new Vector2Int(x, y);
                    }
                }
            }
            
            return new Vector2Int(-1, -1); // 没有找到可用位置
        }
        
        /// <summary>
        /// 检查是否可以放置物品
        /// </summary>
        private static bool CanPlaceItem(bool[,] occupiedCells, Vector2Int position, Vector2Int itemSize)
        {
            for (int y = position.y; y < position.y + itemSize.y; y++)
            {
                for (int x = position.x; x < position.x + itemSize.x; x++)
                {
                    if (occupiedCells[x, y])
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        /// <summary>
        /// 标记单元格为已占用
        /// </summary>
        private static void MarkCellsOccupied(bool[,] occupiedCells, Vector2Int position, Vector2Int itemSize)
        {
            for (int y = position.y; y < position.y + itemSize.y; y++)
            {
                for (int x = position.x; x < position.x + itemSize.x; x++)
                {
                    occupiedCells[x, y] = true;
                }
            }
        }
        
        #endregion
    }
}
