using UnityEngine;
using UnityEditor;
using InventorySystem.SpawnSystem;

namespace InventorySystem.SpawnSystem.Editor
{
    /// <summary>
    /// 固定物品生成配置的自定义编辑器
    /// </summary>
    [CustomEditor(typeof(FixedItemSpawnConfig))]
    public class FixedItemSpawnConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty configNameProp;
        private SerializedProperty descriptionProp;
        private SerializedProperty targetContainerTypeProp;
        private SerializedProperty fixedItemsProp;
        private SerializedProperty sortStrategyProp;
        private SerializedProperty enableDetailedLoggingProp;
        
        private bool showBasicInfo = true;
        private bool showItemTemplates = true;
        private bool showAdvancedSettings = false;
        private bool showVisualPreview = true;
        private float cellSize = 48f;            // 单格像素
        private float itemMargin = 4f;           // 物品外边距
        private Vector2 previewScroll;
        
        private void OnEnable()
        {
            configNameProp = serializedObject.FindProperty("configName");
            descriptionProp = serializedObject.FindProperty("description");
            targetContainerTypeProp = serializedObject.FindProperty("targetContainerType");
            fixedItemsProp = serializedObject.FindProperty("fixedItems");
            sortStrategyProp = serializedObject.FindProperty("sortStrategy");
            enableDetailedLoggingProp = serializedObject.FindProperty("enableDetailedLogging");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var config = target as FixedItemSpawnConfig;
            
            // 标题
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("固定物品生成配置", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 基础信息
            showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "基础信息", true);
            if (showBasicInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(configNameProp, new GUIContent("配置名称"));
                EditorGUILayout.PropertyField(descriptionProp, new GUIContent("配置描述"));
                EditorGUILayout.PropertyField(targetContainerTypeProp, new GUIContent("目标容器类型"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 物品模板配置
            showItemTemplates = EditorGUILayout.Foldout(showItemTemplates, "物品生成模板", true);
            if (showItemTemplates)
            {
                EditorGUI.indentLevel++;
                
                // 显示统计信息
                if (config.fixedItems != null && config.fixedItems.Length > 0)
                {
                    var stats = config.GetStatistics();
                    EditorGUILayout.HelpBox(
                        $"总物品数: {stats.totalItems} | 有效物品: {stats.validItems} | " +
                        $"关键物品: {stats.criticalItems} | 总数量: {stats.totalQuantity}",
                        MessageType.Info);
                }
                
                // 自定义绘制固定生成物品数组
                DrawFixedItemsArray(config);
                EditorGUILayout.PropertyField(sortStrategyProp, new GUIContent("生成顺序策略"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();

            // 可视化物品网格预览（与随机生成风格一致：网格 + 物品位置/尺寸）
            showVisualPreview = EditorGUILayout.Foldout(showVisualPreview, "可视化网格预览", true);
            if (showVisualPreview)
            {
                EditorGUI.indentLevel++;
                Vector2Int gridSize = PreviewGridRenderer.DefaultGridSize;
                Rect previewRect = GUILayoutUtility.GetRect(gridSize.x * 32f + 8f, gridSize.y * 32f + 8f);

                var items = GenerateFixedPreviewItems(config, gridSize);
                PreviewGridRenderer.RenderPreviewGrid(previewRect, gridSize, items, true, false);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            
            // 高级设置
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置", true);
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                DrawPropertiesExcluding(serializedObject, 
                    "m_Script", "configName", "description", "targetContainerType", 
                    "fixedItems", "sortStrategy");
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // 操作按钮
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("验证配置"))
            {
                if (config.ValidateConfig(out var errors))
                {
                    EditorUtility.DisplayDialog("验证结果", "配置验证通过！", "确定");
                }
                else
                {
                    EditorUtility.DisplayDialog("验证失败", 
                        $"配置验证失败:\n{string.Join("\n", errors)}", "确定");
                }
            }
            
            if (GUILayout.Button("显示统计"))
            {
                config.ShowStatistics();
            }
            
            if (GUILayout.Button("创建模板"))
            {
                AddNewItemTemplate(config);
            }
            
            EditorGUILayout.EndHorizontal();
            
            serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary>
        /// 自定义绘制固定物品数组
        /// </summary>
        private void DrawFixedItemsArray(FixedItemSpawnConfig config)
        {
            EditorGUILayout.LabelField("固定生成物品", EditorStyles.boldLabel);
            
            if (config.fixedItems == null)
            {
                config.fixedItems = new FixedItemTemplate[0];
            }
            
            // 绘制数组大小控制
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"数组大小: {config.fixedItems.Length}");
            
            if (GUILayout.Button("添加", GUILayout.Width(50)))
            {
                AddNewItemTemplate(config);
            }
            
            if (config.fixedItems.Length > 0 && GUILayout.Button("删除最后一个", GUILayout.Width(80)))
            {
                RemoveLastItemTemplate(config);
            }
            
            EditorGUILayout.EndHorizontal();
            
            // 绘制每个元素
            for (int i = 0; i < config.fixedItems.Length; i++)
            {
                EditorGUILayout.BeginVertical("box");
                
                // 元素标题
                EditorGUILayout.BeginHorizontal();
                string elementTitle = string.IsNullOrEmpty(config.fixedItems[i]?.templateId) 
                    ? $"物品模板 {i}" 
                    : config.fixedItems[i].templateId;
                
                EditorGUILayout.LabelField(elementTitle, EditorStyles.boldLabel);
                
                if (GUILayout.Button("删除", GUILayout.Width(50)))
                {
                    RemoveItemTemplateAt(config, i);
                    break; // 跳出循环，因为数组已改变
                }
                
                EditorGUILayout.EndHorizontal();
                
                // 绘制模板属性
                if (config.fixedItems[i] != null)
                {
                    DrawItemTemplateInspector(config.fixedItems[i], i);
                }
                else
                {
                    EditorGUILayout.HelpBox("空的物品模板", MessageType.Warning);
                    if (GUILayout.Button("创建新模板"))
                    {
                        config.fixedItems[i] = CreateNewItemTemplate();
                        EditorUtility.SetDirty(config);
                    }
                }
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
        
        /// <summary>
        /// 绘制单个物品模板的Inspector
        /// </summary>
        private void DrawItemTemplateInspector(FixedItemTemplate template, int index)
        {
            EditorGUI.indentLevel++;
            
            // 基础信息
            EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
            template.templateId = EditorGUILayout.TextField("物品唯一ID", template.templateId);
            template.itemData = EditorGUILayout.ObjectField("物品数据引用", template.itemData, typeof(ItemDataSO), false) as ItemDataSO;
            template.quantity = EditorGUILayout.IntSlider("生成数量", template.quantity, 1, 99);
            
            EditorGUILayout.Space();
            
            // 堆叠配置
            EditorGUILayout.LabelField("堆叠配置", EditorStyles.boldLabel);
            
            // 显示物品是否可堆叠的信息
            if (template.itemData != null)
            {
                bool isStackable = template.itemData.IsStackable();
                string stackableInfo = isStackable ? 
                    $"可堆叠物品 (最大堆叠: {template.itemData.maxStack})" : 
                    "不可堆叠物品";
                EditorGUILayout.HelpBox(stackableInfo, isStackable ? MessageType.Info : MessageType.None);
                
                if (isStackable)
                {
                    // 只有可堆叠物品才显示堆叠数量设置
                    template.stackAmount = EditorGUILayout.IntSlider("每个实例堆叠数量", template.stackAmount, 1, template.itemData.maxStack);
                    
                    // 显示有效堆叠数量
                    int effectiveStack = template.GetEffectiveStackAmount();
                    if (effectiveStack != template.stackAmount)
                    {
                        EditorGUILayout.HelpBox($"实际堆叠数量: {effectiveStack} (受物品最大堆叠限制)", MessageType.Warning);
                    }
                }
                else
                {
                    // 不可堆叠物品显示灰色的堆叠数量字段
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.IntSlider("每个实例堆叠数量", 1, 1, 1);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.HelpBox("此物品不支持堆叠", MessageType.Info);
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.IntSlider("每个实例堆叠数量", template.stackAmount, 1, 1000);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.HelpBox("请先选择物品数据", MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // 位置配置
            EditorGUILayout.LabelField("位置配置", EditorStyles.boldLabel);
            template.placementType = (PlacementType)EditorGUILayout.EnumPopup("放置类型", template.placementType);
            
            switch (template.placementType)
            {
                case PlacementType.Exact:
                    template.exactPosition = EditorGUILayout.Vector2IntField("精确位置", template.exactPosition);
                    break;
                case PlacementType.AreaConstrained:
                    template.constrainedArea = EditorGUILayout.RectIntField("约束区域", template.constrainedArea);
                    break;
                case PlacementType.Priority:
                    template.preferredArea = EditorGUILayout.RectIntField("优先区域", template.preferredArea);
                    break;
            }
            
            EditorGUILayout.Space();
            
            // 生成策略
            EditorGUILayout.LabelField("生成策略", EditorStyles.boldLabel);
            template.priority = (SpawnPriority)EditorGUILayout.EnumPopup("生成优先级", template.priority);
            template.scanPattern = (ScanPattern)EditorGUILayout.EnumPopup("扫描模式", template.scanPattern);
            template.allowRotation = EditorGUILayout.Toggle("允许旋转", template.allowRotation);
            template.conflictResolution = (ConflictResolutionType)EditorGUILayout.EnumPopup("冲突解决策略", template.conflictResolution);
            
            EditorGUILayout.Space();
            
            // 生成条件
            EditorGUILayout.LabelField("生成条件", EditorStyles.boldLabel);
            template.isUniqueSpawn = EditorGUILayout.Toggle("是否唯一生成", template.isUniqueSpawn);
            template.maxRetryAttempts = EditorGUILayout.IntSlider("最大重试次数", template.maxRetryAttempts, 1, 10);
            template.enableDebugLog = EditorGUILayout.Toggle("启用调试日志", template.enableDebugLog);
            
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// 可视化预览：将 fixedItems 以网格形式展示物品ICON与背景
        /// </summary>
        private void DrawVisualPreview(FixedItemSpawnConfig config)
        {
            if (config.fixedItems == null || config.fixedItems.Length == 0)
            {
                EditorGUILayout.HelpBox("无固定物品。请在上方添加。", MessageType.Info);
                return;
            }

            // 1) 计算网格边界（根据精确位置或区域）以避免大面积空白
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;

            System.Action<int,int> expandByCell = (gx, gy) =>
            {
                if (gx < minX) minX = gx;
                if (gy < minY) minY = gy;
                if (gx > maxX) maxX = gx;
                if (gy > maxY) maxY = gy;
            };

            foreach (var t in config.fixedItems)
            {
                if (t == null) continue;
                int w = t.itemData != null ? Mathf.Max(1, t.itemData.width) : 1;
                int h = t.itemData != null ? Mathf.Max(1, t.itemData.height) : 1;

                switch (t.placementType)
                {
                    case PlacementType.Exact:
                        {
                            for (int x = 0; x < w; x++)
                                for (int y = 0; y < h; y++)
                                    expandByCell(t.exactPosition.x + x, t.exactPosition.y + y);
                            break;
                        }
                    case PlacementType.AreaConstrained:
                        {
                            var r = t.constrainedArea;
                            expandByCell(r.xMin, r.yMin);
                            expandByCell(r.xMax, r.yMax);
                            break;
                        }
                    case PlacementType.Priority:
                        {
                            var r = t.preferredArea;
                            expandByCell(r.xMin, r.yMin);
                            expandByCell(r.xMax, r.yMax);
                            break;
                        }
                    default:
                        {
                            // 未指定位置的，先放入一个临时条带区域
                            // 简单地按顺序水平排列占位
                            int index = System.Array.IndexOf(config.fixedItems, t);
                            int gx = index * (w + 1);
                            int gy = 0;
                            for (int x = 0; x < w; x++)
                                for (int y = 0; y < h; y++)
                                    expandByCell(gx + x, gy + y);
                            break;
                        }
                }
            }

            if (minX == int.MaxValue)
            {
                minX = minY = 0; maxX = 10; maxY = 6; // 兜底
            }

            int gridCols = Mathf.Max(1, (maxX - minX + 1));
            int gridRows = Mathf.Max(1, (maxY - minY + 1));

            float gridWidth = gridCols * (cellSize + itemMargin) + itemMargin;
            float gridHeight = gridRows * (cellSize + itemMargin) + itemMargin;

            Rect outerRect = GUILayoutUtility.GetRect(gridWidth, gridHeight, GUILayout.ExpandWidth(false));
            outerRect.width = gridWidth;
            outerRect.height = gridHeight;

            // 背景
            Handles.DrawSolidRectangleWithOutline(outerRect, new Color(0,0,0,0.025f), new Color(0,0,0,0.15f));

            // 2) 绘制网格
            Handles.color = new Color(0,0,0,0.1f);
            for (int cx = 0; cx <= gridCols; cx++)
            {
                float x = outerRect.x + itemMargin + cx*(cellSize+itemMargin) - itemMargin*0.5f;
                Handles.DrawLine(new Vector3(x, outerRect.y + itemMargin - itemMargin*0.5f),
                                 new Vector3(x, outerRect.y + gridHeight - itemMargin + itemMargin*0.5f));
            }
            for (int cy = 0; cy <= gridRows; cy++)
            {
                float y = outerRect.y + itemMargin + cy*(cellSize+itemMargin) - itemMargin*0.5f;
                Handles.DrawLine(new Vector3(outerRect.x + itemMargin - itemMargin*0.5f, y),
                                 new Vector3(outerRect.x + gridWidth - itemMargin + itemMargin*0.5f, y));
            }

            // 3) 先画区域，再画精确物品
            foreach (var t in config.fixedItems)
            {
                if (t == null) continue;
                // 区域显示
                RectInt? area = null;
                if (t.placementType == PlacementType.AreaConstrained) area = t.constrainedArea;
                if (t.placementType == PlacementType.Priority) area = t.preferredArea;
                if (area.HasValue)
                {
                    var ar = area.Value;
                    Rect px = new Rect(
                        outerRect.x + itemMargin + (ar.xMin - minX)*(cellSize+itemMargin),
                        outerRect.y + itemMargin + (ar.yMin - minY)*(cellSize+itemMargin),
                        ar.width*(cellSize+itemMargin) - itemMargin,
                        ar.height*(cellSize+itemMargin) - itemMargin);
                    EditorGUI.DrawRect(px, new Color(0.2f, 0.6f, 1f, 0.12f));
                    Handles.color = new Color(0.2f, 0.6f, 1f, 0.8f);
                    Handles.DrawAAPolyLine(2f,
                        new Vector3(px.xMin, px.yMin), new Vector3(px.xMax, px.yMin),
                        new Vector3(px.xMax, px.yMax), new Vector3(px.xMin, px.yMax), new Vector3(px.xMin, px.yMin));
                }
            }

            foreach (var t in config.fixedItems)
            {
                if (t == null) continue;
                if (t.placementType != PlacementType.Exact && t.placementType != PlacementType.Smart)
                    continue;

                int w = t.itemData != null ? Mathf.Max(1, t.itemData.width) : 1;
                int h = t.itemData != null ? Mathf.Max(1, t.itemData.height) : 1;

                Vector2Int pos;
                if (t.placementType == PlacementType.Exact)
                    pos = t.exactPosition;
                else
                {
                    int index = System.Array.IndexOf(config.fixedItems, t);
                    pos = new Vector2Int(index * (w + 1) + minX, minY); // 简单条带排布
                }

                Rect cellRect = new Rect(
                    outerRect.x + itemMargin + (pos.x - minX)*(cellSize+itemMargin),
                    outerRect.y + itemMargin + (pos.y - minY)*(cellSize+itemMargin),
                    w*(cellSize+itemMargin) - itemMargin,
                    h*(cellSize+itemMargin) - itemMargin);

                // 背景
                Color bg = t.itemData != null ? t.itemData.backgroundColor : new Color(0.15f, 0.15f, 0.15f, 1);
                EditorGUI.DrawRect(cellRect, new Color(bg.r, bg.g, bg.b, 0.85f));

                // 图标
                if (t.itemData != null && t.itemData.itemIcon != null)
                {
                    Texture2D iconTex = AssetPreview.GetAssetPreview(t.itemData.itemIcon);
                    if (iconTex == null) iconTex = AssetPreview.GetMiniThumbnail(t.itemData.itemIcon);
                    if (iconTex != null)
                    {
                        GUI.DrawTexture(cellRect, iconTex, ScaleMode.ScaleToFit, true);
                    }
                }

                // 轮廓
                Handles.color = new Color(0, 0, 0, 0.45f);
                Handles.DrawAAPolyLine(2f,
                    new Vector3(cellRect.xMin, cellRect.yMin), new Vector3(cellRect.xMax, cellRect.yMin),
                    new Vector3(cellRect.xMax, cellRect.yMax), new Vector3(cellRect.xMin, cellRect.yMax),
                    new Vector3(cellRect.xMin, cellRect.yMin));
            }
        }

        /// <summary>
        /// 生成固定生成配置的预览项：按“完整数量”展开，自动避让叠放
        /// </summary>
        private System.Collections.Generic.List<PreviewGridRenderer.PreviewItemDisplay> GenerateFixedPreviewItems(FixedItemSpawnConfig config, Vector2Int gridSize)
        {
            var result = new System.Collections.Generic.List<PreviewGridRenderer.PreviewItemDisplay>();
            if (config == null || config.fixedItems == null) return result;

            // 网格占用表，避免重叠
            bool[,] occupied = new bool[gridSize.x, gridSize.y];

            // 尝试占用一块区域
            System.Func<Vector2Int, Vector2Int, Vector2Int?> tryPlace = (pos, size) =>
            {
                // 越界修正
                for (int y = Mathf.Clamp(pos.y, 0, gridSize.y - 1); y <= gridSize.y - size.y; y++)
                {
                    for (int x = Mathf.Clamp(pos.x, 0, gridSize.x - 1); x <= gridSize.x - size.x; x++)
                    {
                        bool ok = true;
                        for (int yy = 0; yy < size.y && ok; yy++)
                            for (int xx = 0; xx < size.x; xx++)
                                if (occupied[x + xx, y + yy]) { ok = false; break; }
                        if (!ok) continue;
                        // 标记
                        for (int yy = 0; yy < size.y; yy++)
                            for (int xx = 0; xx < size.x; xx++)
                                occupied[x + xx, y + yy] = true;
                        return new Vector2Int(x, y);
                    }
                }
                return null;
            };

            // 依序展开每个模板的数量
            for (int i = 0; i < config.fixedItems.Length; i++)
            {
                var t = config.fixedItems[i];
                if (t == null || t.itemData == null) continue;

                var size = new Vector2Int(Mathf.Max(1, t.itemData.width), Mathf.Max(1, t.itemData.height));
                int count = Mathf.Max(1, t.quantity);

                for (int k = 0; k < count; k++)
                {
                    // 期望位置
                    Vector2Int desired = Vector2Int.zero;
                    switch (t.placementType)
                    {
                        case PlacementType.Exact:
                            desired = t.exactPosition; break;
                        case PlacementType.AreaConstrained:
                            desired = new Vector2Int(t.constrainedArea.x, t.constrainedArea.y); break;
                        case PlacementType.Priority:
                            desired = new Vector2Int(t.preferredArea.x, t.preferredArea.y); break;
                        default:
                            desired = new Vector2Int((i + k) % gridSize.x, (i + k) / gridSize.x); break;
                    }

                    // 尝试从期望位置开始，向右下扫描寻找可放置区域
                    var placed = tryPlace(desired, size);
                    if (!placed.HasValue)
                    {
                        // 从(0,0)兜底扫描
                        placed = tryPlace(Vector2Int.zero, size);
                        if (!placed.HasValue) break; // 网格满了
                    }

                    result.Add(new PreviewGridRenderer.PreviewItemDisplay
                    {
                        position = placed.Value,
                        size = size,
                        itemData = t.itemData,
                        rarity = ItemRarity.Common,
                        backgroundColor = t.itemData.backgroundColor,
                        displayText = t.itemData.itemName
                    });
                }
            }

            return result;
        }
        
        /// <summary>
        /// 添加新的物品模板
        /// </summary>
        private void AddNewItemTemplate(FixedItemSpawnConfig config)
        {
            var list = new System.Collections.Generic.List<FixedItemTemplate>(config.fixedItems);
            list.Add(CreateNewItemTemplate());
            config.fixedItems = list.ToArray();
            EditorUtility.SetDirty(config);
        }
        
        /// <summary>
        /// 移除最后一个物品模板
        /// </summary>
        private void RemoveLastItemTemplate(FixedItemSpawnConfig config)
        {
            if (config.fixedItems.Length > 0)
            {
                var list = new System.Collections.Generic.List<FixedItemTemplate>(config.fixedItems);
                list.RemoveAt(list.Count - 1);
                config.fixedItems = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
        
        /// <summary>
        /// 移除指定索引的物品模板
        /// </summary>
        private void RemoveItemTemplateAt(FixedItemSpawnConfig config, int index)
        {
            if (index >= 0 && index < config.fixedItems.Length)
            {
                var list = new System.Collections.Generic.List<FixedItemTemplate>(config.fixedItems);
                list.RemoveAt(index);
                config.fixedItems = list.ToArray();
                EditorUtility.SetDirty(config);
            }
        }
        
        /// <summary>
        /// 创建新的物品模板
        /// </summary>
        private FixedItemTemplate CreateNewItemTemplate()
        {
            var newTemplate = new FixedItemTemplate();
            newTemplate.templateId = $"item_template_{System.DateTime.Now.Ticks}";
            newTemplate.placementType = PlacementType.Smart;
            newTemplate.priority = SpawnPriority.Medium;
            newTemplate.quantity = 1;
            newTemplate.stackAmount = 1; // 默认堆叠数量为1
            newTemplate.allowRotation = true;
            newTemplate.conflictResolution = ConflictResolutionType.Relocate;
            newTemplate.isUniqueSpawn = true;
            newTemplate.maxRetryAttempts = 3;
            newTemplate.scanPattern = ScanPattern.LeftToRight;
            
            return newTemplate;
        }
    }
}
