// // DebugLoader.cs
// using UnityEngine;
// using InventorySystem.Save;

// public class DebugLoader : MonoBehaviour
// {
//     [SerializeField] private InventorySaveSystem saveSystem;

//     void Start()
//     {
//         if (saveSystem == null) saveSystem = FindObjectOfType<InventorySaveSystem>();
//         if (saveSystem == null) return;

//         // 强制打印存档内容
//         var json = saveSystem.LoadFromJsonFile();
//         if (!string.IsNullOrEmpty(json))
//         {
//             var data = Newtonsoft.Json.JsonConvert.DeserializeObject<InventoryData>(json);
//             Debug.Log($"存档版本:{data.version}  物品数:{data.items.Count}");
//             foreach (var s in data.items)
//             {
//                 Debug.Log($"GridId:{s.GridId}  ItemId:{s.ItemId}  Pos:({s.GridX},{s.GridY})  Valid:{s.IsValid()}");
//             }
//         }

//         // 打印运行时注册到的网格
//         foreach (var k in saveSystem.registeredGrids.Keys)
//             Debug.Log($"运行时注册网格:{k}");
//     }
// }