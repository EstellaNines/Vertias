using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace InventorySystem
{
    /// <summary>
    /// 容器内容保存数据
    /// </summary>
    [System.Serializable]
    public class ContainerSaveData
    {
        public string containerItemID;              // 容器物品ID
        public string containerGlobalID;            // 容器全局唯一ID
        public EquipmentSlotType slotType;          // 装备槽类型
        public List<ItemSaveData> containerItems;  // 容器内的物品数据
        public string saveTime;                     // 保存时间
        
        public ContainerSaveData()
        {
            containerItems = new List<ItemSaveData>();
            saveTime = System.DateTime.Now.ToBinary().ToString();
        }
        
        public ContainerSaveData(string itemID, string globalID, EquipmentSlotType type, ItemGrid containerGrid)
        {
            containerItemID = itemID;
            containerGlobalID = globalID;
            slotType = type;
            containerItems = new List<ItemSaveData>();
            saveTime = System.DateTime.Now.ToBinary().ToString();
            
            // 收集容器网格中的所有物品
            if (containerGrid != null)
            {
                CollectContainerItems(containerGrid);
            }
        }
        
        /// <summary>
        /// 收集容器网格中的所有物品
        /// </summary>
        private void CollectContainerItems(ItemGrid containerGrid)
        {
            containerItems.Clear();
            
            // 检查容器网格是否已正确初始化
            if (containerGrid == null)
            {
                Debug.LogWarning("[ContainerSaveManager] 容器网格为null，无法收集物品");
                return;
            }
            
            if (!containerGrid.IsGridInitialized)
            {
                Debug.LogWarning($"[ContainerSaveManager] 容器网格未完全初始化，跳过物品收集");
                return;
            }
            
            // 遍历网格收集物品
            HashSet<Item> processedItems = new HashSet<Item>();
            
            for (int x = 0; x < containerGrid.CurrentWidth; x++)
            {
                for (int y = 0; y < containerGrid.CurrentHeight; y++)
                {
                    try
                    {
                        Item item = containerGrid.GetItemAt(x, y);
                        if (item != null && !processedItems.Contains(item))
                        {
                            processedItems.Add(item);
                            
                            ItemDataReader itemReader = item.GetComponent<ItemDataReader>();
                            if (itemReader != null)
                            {
                                ItemSaveData itemSaveData = new ItemSaveData(itemReader, item.OnGridPosition);
                                containerItems.Add(itemSaveData);
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[ContainerSaveManager] 收集位置({x},{y})的物品时出错: {e.Message}");
                    }
                }
            }
            
            Debug.Log($"[ContainerSaveManager] 收集到 {containerItems.Count} 个容器物品");
        }
    }
    
    /// <summary>
    /// 容器保存管理器
    /// 负责管理容器内容的保存和加载
    /// </summary>
    public class ContainerSaveManager : MonoBehaviour
    {
        private static ContainerSaveManager instance;
        public static ContainerSaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ContainerSaveManager>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("ContainerSaveManager");
                        instance = go.AddComponent<ContainerSaveManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }
        
        private Dictionary<string, ContainerSaveData> containerSaveData = new Dictionary<string, ContainerSaveData>();
        private const string CONTAINER_SAVE_KEY = "ContainerData";
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAllContainerData();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 保存容器内容
        /// </summary>
        /// <param name="containerItem">容器物品</param>
        /// <param name="slotType">装备槽类型</param>
        /// <param name="containerGrid">容器网格</param>
        public void SaveContainerContent(ItemDataReader containerItem, EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (containerItem == null || containerGrid == null) return;
            
            string containerKey = GetContainerKey(containerItem, slotType);
            ContainerSaveData saveData = new ContainerSaveData(
                containerItem.ItemData.id.ToString(),
                containerItem.ItemData.GlobalId.ToString(),
                slotType,
                containerGrid
            );
            
            containerSaveData[containerKey] = saveData;
            SaveToPlayerPrefs();
            
            Debug.Log($"[ContainerSaveManager] 保存容器内容: {containerKey}, 物品数量: {saveData.containerItems.Count}");
        }
        
        /// <summary>
        /// 加载容器内容
        /// </summary>
        /// <param name="containerItem">容器物品</param>
        /// <param name="slotType">装备槽类型</param>
        /// <param name="containerGrid">容器网格</param>
        public void LoadContainerContent(ItemDataReader containerItem, EquipmentSlotType slotType, ItemGrid containerGrid)
        {
            if (containerItem == null || containerGrid == null) return;
            
            string containerKey = GetContainerKey(containerItem, slotType);
            
            if (containerSaveData.TryGetValue(containerKey, out ContainerSaveData saveData))
            {
                StartCoroutine(RestoreContainerItems(saveData, containerGrid));
                Debug.Log($"[ContainerSaveManager] 加载容器内容: {containerKey}, 物品数量: {saveData.containerItems.Count}");
            }
            else
            {
                Debug.Log($"[ContainerSaveManager] 没有找到容器保存数据: {containerKey}");
            }
        }
        
        /// <summary>
        /// 恢复容器中的物品
        /// </summary>
        private System.Collections.IEnumerator RestoreContainerItems(ContainerSaveData saveData, ItemGrid containerGrid)
        {
            yield return null; // 等待一帧确保网格完全初始化
            
            foreach (ItemSaveData itemData in saveData.containerItems)
            {
                GameObject itemPrefab = LoadItemPrefab(itemData);
                if (itemPrefab != null)
                {
                    GameObject itemInstance = Instantiate(itemPrefab);
                    ItemDataReader itemReader = itemInstance.GetComponent<ItemDataReader>();
                    
                    if (itemReader != null)
                    {
                        // 恢复物品状态
                        itemReader.SetStack(itemData.stackCount);
                        itemReader.SetDurability((int)itemData.durability);
                        itemReader.SetUsageCount(itemData.usageCount);
                        
                        // 尝试放置到容器网格中
                        if (containerGrid.PlaceItem(itemInstance.GetComponent<Item>(), itemData.gridPosition.x, itemData.gridPosition.y))
                        {
                            Debug.Log($"[ContainerSaveManager] 成功恢复物品: {itemReader.ItemData.itemName} 到位置 {itemData.gridPosition}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ContainerSaveManager] 无法放置物品: {itemReader.ItemData.itemName} 到位置 {itemData.gridPosition}");
                            Destroy(itemInstance);
                        }
                    }
                    else
                    {
                        Destroy(itemInstance);
                    }
                }
                
                yield return null; // 每个物品之间等待一帧
            }
        }
        
        /// <summary>
        /// 生成容器唯一键
        /// </summary>
        private string GetContainerKey(ItemDataReader containerItem, EquipmentSlotType slotType)
        {
            return $"{slotType}_{containerItem.ItemData.id}_{containerItem.ItemData.GlobalId}";
        }
        
        /// <summary>
        /// 加载物品预制体
        /// </summary>
        private GameObject LoadItemPrefab(ItemSaveData itemData)
        {
            // 根据物品ID和类别ID查找预制体
            ItemCategory category = (ItemCategory)itemData.categoryID;
            string categoryFolder = GetCategoryFolderName(category);
            string prefabPath = $"InventorySystemResources/Prefabs/{categoryFolder}";
            
            // 查找匹配的预制体
            GameObject[] prefabs = Resources.LoadAll<GameObject>(prefabPath);
            foreach (GameObject prefab in prefabs)
            {
                ItemDataReader reader = prefab.GetComponent<ItemDataReader>();
                if (reader != null && reader.ItemData != null && reader.ItemData.id.ToString() == itemData.itemID)
                {
                    return prefab;
                }
            }
            
            Debug.LogWarning($"[ContainerSaveManager] 未找到物品预制体: ID={itemData.itemID}, Category={category}");
            return null;
        }
        
        /// <summary>
        /// 获取类别文件夹名称
        /// </summary>
        private string GetCategoryFolderName(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Helmet: return "Helmet_头盔";
                case ItemCategory.Armor: return "Armor_护甲";
                case ItemCategory.TacticalRig: return "TacticalRig_战术背心";
                case ItemCategory.Backpack: return "Backpack_背包";
                case ItemCategory.Weapon: return "Weapon_武器";
                case ItemCategory.Ammunition: return "Ammunition_弹药";
                case ItemCategory.Food: return "Food_食物";
                case ItemCategory.Drink: return "Drink_饮料";
                case ItemCategory.Sedative: return "Sedative_镇静剂";
                case ItemCategory.Hemostatic: return "Hemostatic_止血剂";
                case ItemCategory.Healing: return "Healing_治疗药物";
                case ItemCategory.Intelligence: return "Intelligence_情报";
                case ItemCategory.Currency: return "Currency_货币";
                case ItemCategory.Special: return "Special_特殊物品";
                default: return "Other";
            }
        }
        
        /// <summary>
        /// 保存到PlayerPrefs
        /// </summary>
        private void SaveToPlayerPrefs()
        {
            try
            {
                ContainerSaveDataCollection collection = new ContainerSaveDataCollection();
                collection.containers = containerSaveData.Values.ToList();
                
                string json = JsonUtility.ToJson(collection, true);
                PlayerPrefs.SetString(CONTAINER_SAVE_KEY, json);
                PlayerPrefs.Save();
                
                Debug.Log($"[ContainerSaveManager] 保存了 {containerSaveData.Count} 个容器数据到PlayerPrefs");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContainerSaveManager] 保存容器数据失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 从PlayerPrefs加载
        /// </summary>
        private void LoadAllContainerData()
        {
            try
            {
                if (PlayerPrefs.HasKey(CONTAINER_SAVE_KEY))
                {
                    string json = PlayerPrefs.GetString(CONTAINER_SAVE_KEY);
                    ContainerSaveDataCollection collection = JsonUtility.FromJson<ContainerSaveDataCollection>(json);
                    
                    containerSaveData.Clear();
                    foreach (ContainerSaveData data in collection.containers)
                    {
                        string key = $"{data.slotType}_{data.containerItemID}_{data.containerGlobalID}";
                        containerSaveData[key] = data;
                    }
                    
                    Debug.Log($"[ContainerSaveManager] 从PlayerPrefs加载了 {containerSaveData.Count} 个容器数据");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ContainerSaveManager] 加载容器数据失败: {e.Message}");
            }
        }
        
        /// <summary>
        /// 清除所有容器保存数据
        /// </summary>
        public void ClearAllContainerData()
        {
            containerSaveData.Clear();
            PlayerPrefs.DeleteKey(CONTAINER_SAVE_KEY);
            PlayerPrefs.Save();
            Debug.Log("[ContainerSaveManager] 清除了所有容器保存数据");
        }
    }
    
    /// <summary>
    /// 容器保存数据集合（用于JSON序列化）
    /// </summary>
    [System.Serializable]
    public class ContainerSaveDataCollection
    {
        public List<ContainerSaveData> containers = new List<ContainerSaveData>();
    }
}
