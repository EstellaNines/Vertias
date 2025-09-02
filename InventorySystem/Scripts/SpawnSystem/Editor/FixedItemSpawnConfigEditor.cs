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
        private bool showContainerConfig = true;
        private bool showItemTemplates = true;
        private bool showAdvancedSettings = false;
        
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
            newTemplate.allowRotation = true;
            newTemplate.conflictResolution = ConflictResolutionType.Relocate;
            newTemplate.isUniqueSpawn = true;
            newTemplate.maxRetryAttempts = 3;
            newTemplate.scanPattern = ScanPattern.LeftToRight;
            
            return newTemplate;
        }
    }
}
