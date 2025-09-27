using UnityEngine;
using UnityEditor;
using InventorySystem; // ContainerSaveManager, EquipmentSlotManager, EquipmentPersistenceManager
using InventorySystem.SpawnSystem; // Spawn managers & integration

namespace InventorySystem.EditorTools
{
	/// <summary>
	/// 一键生成场景必备管理器的编辑器工具（不包含 BackpackSystemManager）
	/// 菜单路径：Tools/Inventory System/创建场景核心管理器
	/// </summary>
	public static class CreateSceneManagersUtility
	{
		private const string RootName = "Systems";

		[MenuItem("Tools/Inventory System/创建场景核心管理器", priority = 10)]
		public static void CreateAllCoreManagers()
		{
			var root = EnsureRoot();

			// 生成系统
			EnsureManagerGameObject<FixedItemSpawnManager>(root, nameof(FixedItemSpawnManager));
			EnsureManagerGameObject<ShelfRandomItemManager>(root, nameof(ShelfRandomItemManager));
			EnsureManagerGameObject<FixedItemProbabilitySpawnManager>(root, nameof(FixedItemProbabilitySpawnManager));
			EnsureManagerGameObject<SpawnSystemManager>(root, nameof(SpawnSystemManager));
			// 可选：仓库固定生成
			TryEnsureOptional<WarehouseFixedItemManager>(root, "WarehouseFixedItemManager");

			// 持久化/装备
			EnsureManagerGameObject<ContainerSaveManager>(root, "ContainerSystemManager");
			EnsureManagerGameObject<EquipmentPersistenceManager>(root, nameof(EquipmentPersistenceManager));
			EnsureManagerGameObject<EquipmentSlotManager>(root, nameof(EquipmentSlotManager));

			// UI（世界空间延迟UI管理器，按需）
			TryEnsureOptional<UnityEngine.MonoBehaviour>(root, "WorldSpaceDelayUIManager");

			EditorUtility.DisplayDialog("完成", "场景核心管理器已创建/补齐（忽略已存在的）。", "确定");
		}

		private static GameObject EnsureRoot()
		{
			var root = GameObject.Find(RootName);
			if (root == null)
			{
				root = new GameObject(RootName);
				Undo.RegisterCreatedObjectUndo(root, "Create Systems Root");
			}
			return root;
		}

		private static T EnsureManagerGameObject<T>(GameObject root, string goName) where T : Component
		{
			var existing = Object.FindObjectOfType<T>();
			if (existing != null)
			{
				Selection.activeObject = existing.gameObject;
				return existing;
			}

			var go = new GameObject(goName);
			Undo.RegisterCreatedObjectUndo(go, "Create Manager");
			go.transform.SetParent(root.transform, false);
			var comp = Undo.AddComponent<T>(go);
			Selection.activeObject = go;
			return comp;
		}

		/// <summary>
		/// 尝试按名称创建可选管理器（用于第三方/可缺省脚本）
		/// </summary>
		private static void TryEnsureOptional<TBase>(GameObject root, string typeName) where TBase : Component
		{
			// 通过反射寻找类型，若不存在则跳过
			var type = System.AppDomain.CurrentDomain.Load("Assembly-CSharp").GetType(typeName) ??
			           System.Type.GetType(typeName);
			if (type == null)
			{
				// 再试命名空间常见前缀
				type = System.Type.GetType($"InventorySystem.{typeName}") ?? System.Type.GetType($"InventorySystem.SpawnSystem.{typeName}");
			}
			if (type == null || !typeof(MonoBehaviour).IsAssignableFrom(type)) return;

			var existing = Object.FindObjectOfType(type);
			if (existing != null) return;

			var go = new GameObject(typeName);
			Undo.RegisterCreatedObjectUndo(go, "Create Optional Manager");
			go.transform.SetParent(root.transform, false);
			Undo.AddComponent(go, type);
		}
	}
}


