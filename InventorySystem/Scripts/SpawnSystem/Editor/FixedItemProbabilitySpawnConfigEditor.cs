using UnityEngine;
using UnityEditor;
using InventorySystem; // ItemDataSO

namespace InventorySystem.SpawnSystem.Editor
{
	[CustomEditor(typeof(FixedItemProbabilitySpawnConfig))]
	public class FixedItemProbabilitySpawnConfigEditor : UnityEditor.Editor
	{
		private SerializedProperty configNameProp;
		private SerializedProperty descriptionProp;
		private SerializedProperty targetContainerTypeProp;
		private SerializedProperty minimumGridSizeProp;
		private SerializedProperty spawnTimingProp;
		private SerializedProperty cooldownTimeProp;
		private SerializedProperty fixedItemsProp;
		private SerializedProperty sortStrategyProp;
		private SerializedProperty continueOnFailureProp;
		private SerializedProperty maxGenerationTimeProp;
		private SerializedProperty enableDetailedLoggingProp;
		private SerializedProperty enablePreviewProp;

		private bool showBasicInfo = true;
		private bool showItemTemplates = true;
		private bool showVisualPreview = true;
		private bool showAdvancedSettings = false;

		private void OnEnable()
		{
			configNameProp = serializedObject.FindProperty("configName");
			descriptionProp = serializedObject.FindProperty("description");
			targetContainerTypeProp = serializedObject.FindProperty("targetContainerType");
			minimumGridSizeProp = serializedObject.FindProperty("minimumGridSize");
			spawnTimingProp = serializedObject.FindProperty("spawnTiming");
			cooldownTimeProp = serializedObject.FindProperty("cooldownTime");
			fixedItemsProp = serializedObject.FindProperty("fixedItems");
			sortStrategyProp = serializedObject.FindProperty("sortStrategy");
			continueOnFailureProp = serializedObject.FindProperty("continueOnFailure");
			maxGenerationTimeProp = serializedObject.FindProperty("maxGenerationTime");
			enableDetailedLoggingProp = serializedObject.FindProperty("enableDetailedLogging");
			enablePreviewProp = serializedObject.FindProperty("enablePreview");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var config = target as FixedItemProbabilitySpawnConfig;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("固定物品 概率生成配置", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			// 基础信息
			showBasicInfo = EditorGUILayout.Foldout(showBasicInfo, "基础信息", true);
			if (showBasicInfo)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(configNameProp, new GUIContent("配置名称"));
				EditorGUILayout.PropertyField(descriptionProp, new GUIContent("配置描述"));
				EditorGUILayout.PropertyField(targetContainerTypeProp, new GUIContent("目标容器类型"));
				EditorGUILayout.PropertyField(minimumGridSizeProp, new GUIContent("最小网格尺寸"));
				EditorGUILayout.PropertyField(spawnTimingProp, new GUIContent("生成时机"));
				EditorGUILayout.PropertyField(cooldownTimeProp, new GUIContent("冷却时间"));
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			// 物品模板
			showItemTemplates = EditorGUILayout.Foldout(showItemTemplates, "物品生成模板（带概率）", true);
			if (showItemTemplates)
			{
				EditorGUI.indentLevel++;
				DrawProbabilityItemsArray(config);
				EditorGUILayout.PropertyField(sortStrategyProp, new GUIContent("生成顺序策略"));
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			// 可视化预览
			showVisualPreview = EditorGUILayout.Foldout(showVisualPreview, "可视化网格预览", true);
			if (showVisualPreview)
			{
				EditorGUI.indentLevel++;
				Vector2Int gridSize = PreviewGridRenderer.DefaultGridSize;
				Rect previewRect = GUILayoutUtility.GetRect(gridSize.x * 32f + 8f, gridSize.y * 32f + 8f);

				// 使用过滤后的固定配置进行预览（更贴近真实生成）
				var filtered = config.ToFilteredFixedConfig();
				var items = GeneratePreviewItemsFromFixedConfig(filtered, gridSize);
				PreviewGridRenderer.RenderPreviewGrid(previewRect, gridSize, items, true, false);
				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			// 高级设置
			showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "高级设置", true);
			if (showAdvancedSettings)
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(continueOnFailureProp, new GUIContent("失败时继续"));
				EditorGUILayout.PropertyField(maxGenerationTimeProp, new GUIContent("最大生成时间"));
				EditorGUILayout.PropertyField(enableDetailedLoggingProp, new GUIContent("启用详细日志"));
				EditorGUILayout.PropertyField(enablePreviewProp, new GUIContent("生成预览"));
				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawProbabilityItemsArray(FixedItemProbabilitySpawnConfig config)
		{
			EditorGUILayout.LabelField("固定生成物品（概率）", EditorStyles.boldLabel);

			if (config.fixedItems == null)
			{
				config.fixedItems = new ProbabilityFixedItemTemplate[0];
			}

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

			for (int i = 0; i < config.fixedItems.Length; i++)
			{
				EditorGUILayout.BeginVertical("box");
				EditorGUILayout.BeginHorizontal();
				string elementTitle = string.IsNullOrEmpty(config.fixedItems[i]?.templateId)
					? $"物品模板 {i}"
					: config.fixedItems[i].templateId;
				EditorGUILayout.LabelField(elementTitle, EditorStyles.boldLabel);
				if (GUILayout.Button("删除", GUILayout.Width(50)))
				{
					RemoveItemTemplateAt(config, i);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
					break;
				}
				EditorGUILayout.EndHorizontal();

				if (config.fixedItems[i] != null)
				{
					DrawItemTemplateInspector(config.fixedItems[i]);
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

		private void DrawItemTemplateInspector(ProbabilityFixedItemTemplate template)
		{
			EditorGUI.indentLevel++;
			// 基础信息
			EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
			template.templateId = EditorGUILayout.TextField("物品唯一ID", template.templateId);
			template.itemData = EditorGUILayout.ObjectField("物品数据引用", template.itemData, typeof(ItemDataSO), false) as ItemDataSO;
			template.quantity = EditorGUILayout.IntSlider("最大生成数量", template.quantity, 1, 99);
			template.spawnChance = EditorGUILayout.Slider("单个实例生成概率", template.spawnChance, 0f, 1f);

			EditorGUILayout.Space();
			// 堆叠配置
			EditorGUILayout.LabelField("堆叠配置", EditorStyles.boldLabel);
			if (template.itemData != null && template.itemData.IsStackable())
			{
				template.stackAmount = EditorGUILayout.IntSlider("每个实例堆叠数量", template.stackAmount, 1, template.itemData.maxStack);
			}
			else
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.IntSlider("每个实例堆叠数量", 1, 1, 1);
				EditorGUI.EndDisabledGroup();
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
			// 条件/调试
			EditorGUILayout.LabelField("生成条件", EditorStyles.boldLabel);
			template.isUniqueSpawn = EditorGUILayout.Toggle("是否唯一生成", template.isUniqueSpawn);
			template.maxRetryAttempts = EditorGUILayout.IntSlider("最大重试次数", template.maxRetryAttempts, 1, 10);
			template.enableDebugLog = EditorGUILayout.Toggle("启用调试日志", template.enableDebugLog);
			EditorGUI.indentLevel--;
		}

		private void AddNewItemTemplate(FixedItemProbabilitySpawnConfig config)
		{
			var list = new System.Collections.Generic.List<ProbabilityFixedItemTemplate>(config.fixedItems);
			list.Add(CreateNewItemTemplate());
			config.fixedItems = list.ToArray();
			EditorUtility.SetDirty(config);
		}

		private void RemoveLastItemTemplate(FixedItemProbabilitySpawnConfig config)
		{
			if (config.fixedItems.Length > 0)
			{
				var list = new System.Collections.Generic.List<ProbabilityFixedItemTemplate>(config.fixedItems);
				list.RemoveAt(list.Count - 1);
				config.fixedItems = list.ToArray();
				EditorUtility.SetDirty(config);
			}
		}

		private void RemoveItemTemplateAt(FixedItemProbabilitySpawnConfig config, int index)
		{
			if (index >= 0 && index < config.fixedItems.Length)
			{
				var list = new System.Collections.Generic.List<ProbabilityFixedItemTemplate>(config.fixedItems);
				list.RemoveAt(index);
				config.fixedItems = list.ToArray();
				EditorUtility.SetDirty(config);
			}
		}

		private ProbabilityFixedItemTemplate CreateNewItemTemplate()
		{
			var t = new ProbabilityFixedItemTemplate();
			t.templateId = $"prob_item_{System.DateTime.Now.Ticks}";
			t.quantity = 1;
			t.stackAmount = 1;
			t.placementType = PlacementType.Smart;
			t.priority = SpawnPriority.Medium;
			t.allowRotation = true;
			t.conflictResolution = ConflictResolutionType.Relocate;
			t.isUniqueSpawn = true;
			t.maxRetryAttempts = 3;
			t.scanPattern = ScanPattern.LeftToRight;
			t.spawnChance = 1f;
			return t;
		}

		// 依据过滤后的 FixedItemSpawnConfig 生成预览项
		private System.Collections.Generic.List<PreviewGridRenderer.PreviewItemDisplay> GeneratePreviewItemsFromFixedConfig(FixedItemSpawnConfig fixedCfg, Vector2Int gridSize)
		{
			var result = new System.Collections.Generic.List<PreviewGridRenderer.PreviewItemDisplay>();
			if (fixedCfg == null || fixedCfg.fixedItems == null) return result;

			bool[,] occupied = new bool[gridSize.x, gridSize.y];
			System.Func<Vector2Int, Vector2Int, Vector2Int?> tryPlace = (pos, size) =>
			{
				for (int y = Mathf.Clamp(pos.y, 0, gridSize.y - 1); y <= gridSize.y - size.y; y++)
				{
					for (int x = Mathf.Clamp(pos.x, 0, gridSize.x - 1); x <= gridSize.x - size.x; x++)
					{
						bool ok = true;
						for (int yy = 0; yy < size.y && ok; yy++)
							for (int xx = 0; xx < size.x; xx++)
								if (occupied[x + xx, y + yy]) { ok = false; break; }
						if (!ok) continue;
						for (int yy = 0; yy < size.y; yy++)
							for (int xx = 0; xx < size.x; xx++)
								occupied[x + xx, y + yy] = true;
						return new Vector2Int(x, y);
					}
				}
				return null;
			};

			for (int i = 0; i < fixedCfg.fixedItems.Length; i++)
			{
				var t = fixedCfg.fixedItems[i];
				if (t == null || t.itemData == null) continue;
				var size = new Vector2Int(Mathf.Max(1, t.itemData.width), Mathf.Max(1, t.itemData.height));
				int count = Mathf.Max(1, t.quantity);
				for (int k = 0; k < count; k++)
				{
					Vector2Int desired;
					switch (t.placementType)
					{
						case PlacementType.Exact: desired = t.exactPosition; break;
						case PlacementType.AreaConstrained: desired = new Vector2Int(t.constrainedArea.x, t.constrainedArea.y); break;
						case PlacementType.Priority: desired = new Vector2Int(t.preferredArea.x, t.preferredArea.y); break;
						default: desired = new Vector2Int((i + k) % gridSize.x, (i + k) / gridSize.x); break;
					}
					var placed = tryPlace(desired, size) ?? tryPlace(Vector2Int.zero, size);
					if (placed.HasValue)
					{
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
			}
			return result;
		}
	}
}


