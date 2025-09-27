using UnityEngine;

namespace InventorySystem.SpawnSystem
{
	/// <summary>
	/// 固定物品概率生成管理器：先根据概率过滤，再复用 FixedItemSpawnManager 进行实际放置
	/// </summary>
	[AddComponentMenu("Inventory System/Spawn System/Fixed Item Probability Spawn Manager")]
	public class FixedItemProbabilitySpawnManager : MonoBehaviour
	{
		private static FixedItemProbabilitySpawnManager instance;
		public static FixedItemProbabilitySpawnManager Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindObjectOfType<FixedItemProbabilitySpawnManager>();
					if (instance == null)
					{
						var go = new GameObject("FixedItemProbabilitySpawnManager");
						instance = go.AddComponent<FixedItemProbabilitySpawnManager>();
						DontDestroyOnLoad(go);
					}
				}
				return instance;
			}
		}

		[SerializeField] private bool enableDebugLog = true;

		public void SpawnWithProbability(ItemGrid targetGrid, FixedItemProbabilitySpawnConfig probConfig, string containerId = null)
		{
			if (targetGrid == null || probConfig == null)
			{
				Debug.LogWarning("[FixedItemProbabilitySpawnManager] 参数无效");
				return;
			}
			var filtered = probConfig.ToFilteredFixedConfig();
			if (filtered.fixedItems == null || filtered.fixedItems.Length == 0)
			{
				if (enableDebugLog) Debug.Log("[FixedItemProbabilitySpawnManager] 掷点后无可生成物品，跳过");
				return;
			}
			FixedItemSpawnManager.Instance.SpawnFixedItems(targetGrid, filtered, containerId);
		}
	}
}


