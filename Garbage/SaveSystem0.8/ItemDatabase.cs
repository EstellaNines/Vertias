// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using InventorySystem;
// #if UNITY_EDITOR
// using UnityEditor;
// #endif

// namespace InventorySystem
// {
//     /// <summary>
//     /// ????? - ???????ItemDataSO??????
//     /// ?????????????
//     /// </summary>
//     [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory System/Item Database")]
//     public class ItemDatabase : ScriptableObject
//     {
//         [Header("?????")]
//         [SerializeField, FieldLabel("?????")]
//         private string databasePath = "Assets/InventorySystem/Database/Scriptable Object????";

//         [SerializeField, FieldLabel("????")]
//         private bool autoLoadOnAwake = true;

//         [Header("?????")]
//         [SerializeField, FieldLabel("???????")]
//         public int loadedItemCount = 0;

//         [SerializeField, FieldLabel("?????")]
//         public string databaseStatus = "????";

//         // ???????
//         private Dictionary<int, ItemDataSO> itemIdIndex = new Dictionary<int, ItemDataSO>();
//         private Dictionary<long, ItemDataSO> globalIdIndex = new Dictionary<long, ItemDataSO>();
//         private Dictionary<ItemCategory, List<ItemDataSO>> categoryIndex = new Dictionary<ItemCategory, List<ItemDataSO>>();
//         private List<ItemDataSO> allItems = new List<ItemDataSO>();

//         // ????
//         private static ItemDatabase _instance;
//         public static ItemDatabase Instance
//         {
//             get
//             {
//                 if (_instance == null)
//                 {
//                     _instance = Resources.Load<ItemDatabase>("ItemDatabase");
//                     if (_instance == null)
//                     {
//                         Debug.LogError("ItemDatabase: ???ItemDatabase???????Resources??????");
//                     }
//                     else
//                     {
//                         _instance.Initialize();
//                     }
//                 }
//                 return _instance;
//             }
//         }

//         private void Awake()
//         {
//             if (_instance == null)
//             {
//                 _instance = this;
//                 if (autoLoadOnAwake)
//                 {
//                     Initialize();
//                 }
//             }
//         }

//         /// <summary>
//         /// ???????????ItemDataSO
//         /// </summary>
//         public void Initialize()
//         {
//             try
//             {
//                 databaseStatus = "????...";
//                 ClearIndexes();

// #if UNITY_EDITOR
//                 LoadItemsInEditor();
// #else
//                 LoadItemsInBuild();
// #endif

//                 BuildIndexes();
//                 loadedItemCount = allItems.Count;
//                 databaseStatus = $"??? {loadedItemCount} ???";

//                 Debug.Log($"ItemDatabase: ????????? {loadedItemCount} ???");
//             }
//             catch (System.Exception e)
//             {
//                 databaseStatus = $"????: {e.Message}";
//                 Debug.LogError($"ItemDatabase: ????? - {e.Message}");
//             }
//         }

// #if UNITY_EDITOR
//         /// <summary>
//         /// ????????????
//         /// </summary>
//         private void LoadItemsInEditor()
//         {
//             string[] guids = AssetDatabase.FindAssets("t:ItemDataSO", new[] { databasePath });

//             foreach (string guid in guids)
//             {
//                 string assetPath = AssetDatabase.GUIDToAssetPath(guid);
//                 ItemDataSO itemData = AssetDatabase.LoadAssetAtPath<ItemDataSO>(assetPath);

//                 if (itemData != null)
//                 {
//                     allItems.Add(itemData);
//                 }
//             }
//         }
// #endif

//         /// <summary>
//         /// ????????????
//         /// </summary>
//         private void LoadItemsInBuild()
//         {
//             // ??????????ItemDataSO??Resources????
//             ItemDataSO[] items = Resources.LoadAll<ItemDataSO>("");
//             allItems.AddRange(items);
//         }

//         /// <summary>
//         /// ????
//         /// </summary>
//         private void BuildIndexes()
//         {
//             foreach (ItemDataSO item in allItems)
//             {
//                 // ??ID??
//                 if (!itemIdIndex.ContainsKey(item.id))
//                 {
//                     itemIdIndex[item.id] = item;
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"ItemDatabase: ???????ID {item.id} - {item.itemName}");
//                 }

//                 // ????ID??
//                 if (!globalIdIndex.ContainsKey(item.GlobalId))
//                 {
//                     globalIdIndex[item.GlobalId] = item;
//                 }

//                 // ??????
//                 if (!categoryIndex.ContainsKey(item.category))
//                 {
//                     categoryIndex[item.category] = new List<ItemDataSO>();
//                 }
//                 categoryIndex[item.category].Add(item);
//             }
//         }

//         /// <summary>
//         /// ??????
//         /// </summary>
//         private void ClearIndexes()
//         {
//             itemIdIndex.Clear();
//             globalIdIndex.Clear();
//             categoryIndex.Clear();
//             allItems.Clear();
//         }

//         /// <summary>
//         /// ????ID??????
//         /// </summary>
//         /// <param name="itemId">??ID</param>
//         /// <returns>??????????null</returns>
//         public ItemDataSO GetItemData(int itemId)
//         {
//             if (itemIdIndex.TryGetValue(itemId, out ItemDataSO itemData))
//             {
//                 return itemData;
//             }

//             Debug.LogWarning($"ItemDatabase: ?????ID {itemId} ?????");
//             return null;
//         }

//         /// <summary>
//         /// ????ID??????
//         /// </summary>
//         /// <param name="globalId">??ID</param>
//         /// <returns>??????????null</returns>
//         public ItemDataSO GetItemDataByGlobalId(long globalId)
//         {
//             if (globalIdIndex.TryGetValue(globalId, out ItemDataSO itemData))
//             {
//                 return itemData;
//             }

//             Debug.LogWarning($"ItemDatabase: ?????ID {globalId} ?????");
//             return null;
//         }

//         /// <summary>
//         /// ????????????
//         /// </summary>
//         /// <param name="category">????</param>
//         /// <returns>???????????</returns>
//         public List<ItemDataSO> GetItemsByCategory(ItemCategory category)
//         {
//             if (categoryIndex.TryGetValue(category, out List<ItemDataSO> items))
//             {
//                 return new List<ItemDataSO>(items); // ??????????
//             }

//             return new List<ItemDataSO>();
//         }

//         /// <summary>
//         /// ????????
//         /// </summary>
//         /// <returns>?????????</returns>
//         public List<ItemDataSO> GetAllItems()
//         {
//             return new List<ItemDataSO>(allItems);
//         }

//         /// <summary>
//         /// ????????
//         /// </summary>
//         /// <param name="itemId">??ID</param>
//         /// <returns>????</returns>
//         public bool ContainsItem(int itemId)
//         {
//             return itemIdIndex.ContainsKey(itemId);
//         }

//         /// <summary>
//         /// ?????????
//         /// </summary>
//         /// <returns>???????</returns>
//         public string GetDatabaseStats()
//         {
//             var stats = new System.Text.StringBuilder();
//             stats.AppendLine($"????: {allItems.Count}");
//             stats.AppendLine($"????:");

//             foreach (var category in categoryIndex)
//             {
//                 stats.AppendLine($"  {category.Key}: {category.Value.Count} ???");
//             }

//             return stats.ToString();
//         }

//         /// <summary>
//         /// ???????
//         /// </summary>
//         [ContextMenu("???????")]
//         public void ReloadDatabase()
//         {
//             Initialize();
//         }

// #if UNITY_EDITOR
//         /// <summary>
//         /// ????????????
//         /// </summary>
//         [ContextMenu("????????")]
//         public void ValidateDatabase()
//         {
//             Initialize();

//             Debug.Log("=== ItemDatabase ???? ===");
//             Debug.Log(GetDatabaseStats());

//             // ????ID
//             var duplicateIds = allItems.GroupBy(item => item.id)
//                                      .Where(group => group.Count() > 1)
//                                      .Select(group => group.Key);

//             foreach (int duplicateId in duplicateIds)
//             {
//                 var duplicateItems = allItems.Where(item => item.id == duplicateId);
//                 Debug.LogWarning($"????ID {duplicateId}: {string.Join(", ", duplicateItems.Select(item => item.itemName))}");
//             }

//             // ??????
//             var itemsWithMissingData = allItems.Where(item =>
//                 string.IsNullOrEmpty(item.itemName) ||
//                 item.itemIcon == null
//             ).ToList();

//             foreach (var item in itemsWithMissingData)
//             {
//                 Debug.LogWarning($"?? {item.id} ?????: ??={item.itemName}, ??={item.itemIcon}");
//             }

//             Debug.Log("=== ???? ===");
//         }
// #endif
//     }
// }