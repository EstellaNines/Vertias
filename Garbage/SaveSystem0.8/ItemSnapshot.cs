// using System;
// using UnityEngine;
// using InventorySystem;

// namespace InventorySystem.Save
// {
//     /// <summary>
//     /// 物品快照数据结构，用于保存/加载背包中的物品状态。
//     /// 已内置对 GlobalId 为空的兜底处理。
//     /// </summary>
//     [Serializable]
//     public class ItemSnapshot
//     {
//         /* ====== 序列化字段 ====== */
//         [SerializeField] private string globalId;
//         [SerializeField] private int itemId;
//         [SerializeField] private string gridId;
//         [SerializeField] private int gridX;
//         [SerializeField] private int gridY;
//         [SerializeField] private bool isRotated;
//         [SerializeField] private int stackCount;
//         [SerializeField] private float durability;
//         [SerializeField] private int usageCount;
//         [SerializeField] private string saveTimestamp;
//         [SerializeField] private int instanceId;

//         /* ====== 只读属性 ====== */
//         public string GlobalId => globalId;
//         public int ItemId => itemId;
//         public string GridId => gridId;
//         public int GridX => gridX;
//         public int GridY => gridY;
//         public bool IsRotated => isRotated;
//         public int StackCount => stackCount;
//         public float Durability => durability;
//         public int UsageCount => usageCount;
//         public string SaveTimestamp => saveTimestamp;
//         public int InstanceId => instanceId;

//         /* ====== 构造函数 ====== */
//         public ItemSnapshot() { }   // 供反序列化

//         public ItemSnapshot(string globalId,
//                             int itemId,
//                             string gridId,
//                             int gridX,
//                             int gridY,
//                             bool isRotated,
//                             int stackCount,
//                             float durability,
//                             int usageCount,
//                             int instanceId)
//         {
//             this.globalId = string.IsNullOrEmpty(globalId) ? $"id_{itemId}" : globalId;
//             this.itemId = itemId;
//             this.gridId = gridId;
//             this.gridX = gridX;
//             this.gridY = gridY;
//             this.isRotated = isRotated;
//             this.stackCount = stackCount;
//             this.durability = durability;
//             this.usageCount = usageCount;
//             this.instanceId = instanceId;
//             this.saveTimestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
//         }

//         /* ====== 工厂方法 ====== */
//         public static ItemSnapshot CreateFromItem(Item item, string gridId)
//         {
//             if (item == null)
//             {
//                 Debug.LogError("[ItemSnapshot] 无法从空 Item 创建快照");
//                 return null;
//             }

//             var reader = item.ItemDataReader;
//             if (reader?.ItemData == null)
//             {
//                 Debug.LogError($"[ItemSnapshot] Item 缺少 ItemData: {item.name}");
//                 return null;
//             }

//             var data = reader.ItemData;

//             // 兜底：GlobalId 为空时用 id_{itemId}
//             string gid = string.IsNullOrEmpty(data.GlobalId.ToString())
//                          ? $"id_{data.id}"
//                          : data.GlobalId.ToString();

//             return new ItemSnapshot(
//                 gid,
//                 data.id,
//                 gridId,
//                 item.OnGridPosition.x,
//                 item.OnGridPosition.y,
//                 item.IsRotated(),
//                 reader.CurrentStack,
//                 reader.CurrentDurability,
//                 reader.CurrentUsageCount,
//                 item.GetInstanceID()
//             );
//         }

//         /* ====== 有效性检查 ====== */
//         public bool IsValid()
//         {
//             // GridId 兜底处理
//             if (string.IsNullOrEmpty(gridId))
//             {
//                 Debug.LogWarning($"[ItemSnapshot] GridId为空，ItemId: {itemId}");
//                 return false;
//             }

//             // ItemId验证
//             if (itemId <= 0)
//             {
//                 Debug.LogWarning($"[ItemSnapshot] 无效的ItemId: {itemId}");
//                 return false;
//             }
            
//             // 位置验证
//             if (gridX < 0 || gridY < 0)
//             {
//                 Debug.LogWarning($"[ItemSnapshot] 无效的网格位置: ({gridX}, {gridY})");
//                 return false;
//             }
            
//             // 堆叠数量验证
//             if (stackCount <= 0)
//             {
//                 Debug.LogWarning($"[ItemSnapshot] 无效的堆叠数量: {stackCount}");
//                 return false;
//             }
            
//             // 耐久度验证
//             if (durability < 0)
//             {
//                 Debug.LogWarning($"[ItemSnapshot] 无效的耐久度: {durability}");
//                 return false;
//             }
            
//             return true;
//         }

//         /* ====== 调试 & 拷贝 ====== */
//         public override string ToString()
//         {
//             return $"ItemSnapshot[Global:{globalId}, Id:{itemId}, Grid:{gridId}, " +
//                    $"Pos:({gridX},{gridY}), Rot:{isRotated}, Stack:{stackCount}, " +
//                    $"Dur:{durability}, Usage:{usageCount}]";
//         }

//         public ItemSnapshot Clone() =>
//             new ItemSnapshot(globalId, itemId, gridId, gridX, gridY, isRotated,
//                              stackCount, durability, usageCount, instanceId);
//     }
// }