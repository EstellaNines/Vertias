using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

namespace Persistence
{
	public static class ItemDataRepository
	{
		private static bool _initialized;
		private static readonly Dictionary<int, ItemDataSO> _idToSO = new Dictionary<int, ItemDataSO>();
		private static readonly Dictionary<long, ItemDataSO> _gidToSO = new Dictionary<long, ItemDataSO>();

		// 在首次调用时从 Resources 扫描全部 ItemDataSO（可根据你的资源路径调整）
		public static void EnsureInitialized()
		{
			if (_initialized) return;
			_idToSO.Clear();
			_gidToSO.Clear();

			// 提示：若你的 SO 不在 Resources，可改为加载地址或由外部注入
			var all = Resources.LoadAll<ItemDataSO>("");
			foreach (var so in all)
			{
				if (so == null) continue;
				if (!_idToSO.ContainsKey(so.id)) _idToSO[so.id] = so;
				if (!_gidToSO.ContainsKey(so.GlobalId)) _gidToSO[so.GlobalId] = so;
			}
			_initialized = true;
		}

		public static bool TryGetById(int id, out ItemDataSO so)
		{
			EnsureInitialized();
			return _idToSO.TryGetValue(id, out so);
		}

		public static bool TryGetByGlobalId(long gid, out ItemDataSO so)
		{
			EnsureInitialized();
			return _gidToSO.TryGetValue(gid, out so);
		}
	}
}
