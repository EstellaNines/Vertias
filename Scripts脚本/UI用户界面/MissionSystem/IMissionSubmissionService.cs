using UnityEngine;

namespace Game.UI
{
	/// <summary>
	/// 提交任务判定与消耗服务接口。
	/// 由背包/仓库/玩家数据系统实现，以便 EmailMissionUIManager 查询与扣除。
	/// </summary>
	public interface IMissionSubmissionService
	{
		/// <summary>
		/// 当前玩家的情报总值（用于与 requirements.intelligence 比较）。
		/// </summary>
		int GetPlayerIntelligenceTotal();

		/// <summary>
		/// 是否拥有指定物品及数量（按 InventorySystemStaticDatabase.json 的 item id）。
		/// </summary>
		bool HasItem(int itemId, int count);

		/// <summary>
		/// 尝试消耗指定物品及数量，返回是否成功。
		/// </summary>
		bool TryConsumeItem(int itemId, int count);
	}
}


