using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using InventorySystem;

namespace InventorySystem.SpawnSystem
{
	/// <summary>
	/// 带概率的固定物品模板：在固定模板基础上增加生成概率
	/// </summary>
	[System.Serializable]
	public class ProbabilityFixedItemTemplate : FixedItemTemplate
	{
		[Range(0f, 1f)]
		[Tooltip("此物品的单个实例生成概率（0-1）\n注意：按实例掷点，quantity 为最大实例数")]
		public float spawnChance = 1f;
	}

	/// <summary>
	/// 固定物品概率生成配置
	/// 与 FixedItemSpawnConfig 结构一致，但每个模板带有生成概率
	/// </summary>
	[CreateAssetMenu(fileName = "New Fixed Item Probability Spawn Config",
		menuName = "Inventory System/Spawn System/Fixed Item Probability Spawn Config")]
	public class FixedItemProbabilitySpawnConfig : ScriptableObject
	{
		[Header("配置基础信息")]
		[FieldLabel("配置名称")] public string configName;
		[FieldLabel("配置描述")][TextArea(2,4)] public string description;
		[FieldLabel("配置版本")] public string version = "1.0.0";

		[Header("目标容器配置")]
		[FieldLabel("适用容器类型")] public ContainerType targetContainerType = ContainerType.Warehouse;
		[FieldLabel("容器标识符")]
		[Tooltip("目标容器的唯一标识符，留空则适用于所有同类型容器")]
		public string containerIdentifier;
		[FieldLabel("网格尺寸验证")] public Vector2Int minimumGridSize = new Vector2Int(10, 10);

		[Header("生成时机控制")]
		[FieldLabel("生成时机")] public SpawnTiming spawnTiming = SpawnTiming.ContainerFirstOpen;
		[FieldLabel("冷却时间")] public float cooldownTime = 0f;

		[Header("物品生成模板（带概率）")]
		[FieldLabel("固定生成物品（概率）")]
		public ProbabilityFixedItemTemplate[] fixedItems;

		[Header("生成策略")]
		[FieldLabel("生成顺序策略")] public FixedItemSpawnConfig.ItemSortStrategy sortStrategy = FixedItemSpawnConfig.ItemSortStrategy.PriorityThenSize;
		[FieldLabel("失败时继续")] public bool continueOnFailure = true;
		[FieldLabel("最大生成时间")] public float maxGenerationTime = 10f;

		[Header("调试配置")]
		[FieldLabel("启用详细日志")] public bool enableDetailedLogging = false;
		[FieldLabel("生成预览")] public bool enablePreview = false;

		/// <summary>
		/// 生成一个"已掷点过滤"后的 FixedItemSpawnConfig（用于复用现有固定生成器）
		/// </summary>
		public FixedItemSpawnConfig ToFilteredFixedConfig(int randomSeed = -1)
		{
			var previous = UnityEngine.Random.state;
			try
			{
				if (randomSeed >= 0) UnityEngine.Random.InitState(randomSeed);
				var cfg = ScriptableObject.CreateInstance<FixedItemSpawnConfig>();
				cfg.configName = $"Filtered_{configName}";
				cfg.description = description;
				cfg.version = version;
				cfg.targetContainerType = targetContainerType;
				cfg.minimumGridSize = minimumGridSize;
				cfg.sortStrategy = sortStrategy;
				cfg.continueOnFailure = continueOnFailure;
				cfg.maxGenerationTime = maxGenerationTime;
				cfg.enableDetailedLogging = enableDetailedLogging;

				var list = new List<FixedItemTemplate>();
				if (fixedItems != null)
				{
					foreach (var t in fixedItems)
					{
						if (t == null || t.itemData == null) continue;
						int maxCount = Mathf.Max(1, t.quantity);
						int accepted = 0;
						for (int i = 0; i < maxCount; i++)
						{
							if (UnityEngine.Random.value <= Mathf.Clamp01(t.spawnChance)) accepted++;
						}
						if (accepted <= 0) continue;

						// 拷贝为固定模板，数量为accepted
						var copy = new FixedItemTemplate
						{
							templateId = t.templateId,
							itemData = t.itemData,
							quantity = accepted,
							stackAmount = t.stackAmount,
							placementType = t.placementType,
							exactPosition = t.exactPosition,
							constrainedArea = t.constrainedArea,
							preferredArea = t.preferredArea,
							priority = t.priority,
							scanPattern = t.scanPattern,
							allowRotation = t.allowRotation,
							conflictResolution = t.conflictResolution,
							isUniqueSpawn = t.isUniqueSpawn,
							conditionTags = t.conditionTags,
							maxRetryAttempts = t.maxRetryAttempts,
							enableDebugLog = t.enableDebugLog
						};
						list.Add(copy);
					}
				}
				cfg.fixedItems = list.ToArray();
				return cfg;
			}
			finally
			{
				UnityEngine.Random.state = previous;
			}
		}

		#if UNITY_EDITOR
		private void OnValidate()
		{
			if (string.IsNullOrEmpty(configName))
			{
				configName = $"Probability Fixed Items - {name}";
			}
		}
		#endif
	}
}


