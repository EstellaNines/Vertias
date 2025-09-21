using UnityEngine;

namespace InventorySystem
{
	/// <summary>
	/// 标记组件：使当前 <see cref="EquipmentSlot"/> 排除在全局装备系统之外。
	/// - 不被 EquipmentSlotManager 注册
	/// - 不参与 EquipmentPersistenceManager 的保存/收集
	/// - 不触发装备/卸装事件，也不写入容器存档
	/// 典型用例：任务面板的“镜像装备槽”。
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class ExcludeFromEquipmentSystem : MonoBehaviour {}
}


