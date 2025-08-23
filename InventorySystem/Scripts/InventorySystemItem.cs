using System;
using UnityEngine;

// 物品系统项目 - 支持实例ID和序列化的数据容器
[System.Serializable]
public class ItemSaveData
{
    public string instanceID;
    public int itemDataID;
    public string instanceDataJson;
    public Vector2Int gridPosition;
    public bool isDraggable;
    public string itemDataPath; // 物品数据资源路径

    public ItemSaveData()
    {
        instanceID = System.Guid.NewGuid().ToString();
        itemDataID = 0;
        instanceDataJson = "";
        gridPosition = Vector2Int.zero;
        isDraggable = true;
        itemDataPath = "";
    }
}

public class InventorySystemItem : MonoBehaviour
{
    [Header("实例标识")]
    [SerializeField] private string instanceID;

    [Header("组件引用")]
    ItemDataHolder holder;

    [Header("属性设置")]
    private bool isDraggable = true;

    [Header("网格位置")]
    [SerializeField] private Vector2Int gridPosition = Vector2Int.zero;

    // 基础属性
    public Vector2Int Size => holder != null ? new Vector2Int(holder.ItemWidth, holder.ItemHeight) : Vector2Int.one;
    public InventorySystemItemDataSO Data => holder?.GetItemData();
    public bool IsDraggable { get => isDraggable; set => isDraggable = value; }

    // 实例ID属性
    public string InstanceID
    {
        get
        {
            if (string.IsNullOrEmpty(instanceID))
            {
                instanceID = System.Guid.NewGuid().ToString();
            }
            return instanceID;
        }
        private set => instanceID = value;
    }

    // 网格位置属性
    public Vector2Int GridPosition
    {
        get => gridPosition;
        set => gridPosition = value;
    }

    void Awake()
    {
        // 查找子对象中的 ItemDataHolder
        holder = GetComponentInChildren<ItemDataHolder>();
        if (holder == null)
        {
            Debug.LogError($"在 {gameObject.name} 子对象中未找到 ItemDataHolder 组件！");
        }

        // 确保有唯一的实例ID
        if (string.IsNullOrEmpty(instanceID))
        {
            instanceID = System.Guid.NewGuid().ToString();
        }
    }

    // === 实例ID管理方法 ===

    // 获取实例ID
    public string GetItemInstanceID()
    {
        return InstanceID;
    }

    // 设置实例ID（通常用于加载保存数据时）
    public void SetItemInstanceID(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            instanceID = id;
        }
        else
        {
            Debug.LogWarning($"尝试设置空的实例ID到物品 {gameObject.name}，将生成新的ID");
            instanceID = System.Guid.NewGuid().ToString();
        }
    }

    // 生成新的实例ID
    public string GenerateNewItemInstanceID()
    {
        instanceID = System.Guid.NewGuid().ToString();
        return instanceID;
    }

    // 检查实例ID是否有效
    public bool IsItemInstanceIDValid()
    {
        return !string.IsNullOrEmpty(instanceID) && instanceID.Length > 0;
    }

    // === 数据访问方法 ===

    // 获取ItemDataHolder组件
    public ItemDataHolder GetItemDataHolder()
    {
        return holder;
    }

    // 获取物品数据ID
    public int GetItemDataID()
    {
        return Data?.id ?? 0;
    }

    // 获取物品名称
    public string GetItemName()
    {
        return Data?.itemName ?? "未知物品";
    }

    // 检查是否有有效的物品数据
    public bool HasValidItemData()
    {
        return holder != null && Data != null;
    }

    // === 序列化支持方法 ===

    // 创建保存数据
    public ItemSaveData CreateSaveData()
    {
        ItemSaveData saveData = new ItemSaveData();

        saveData.instanceID = InstanceID;
        saveData.itemDataID = GetItemDataID();
        saveData.gridPosition = gridPosition;
        saveData.isDraggable = isDraggable;

        // 设置物品数据路径
        if (Data != null)
        {
#if UNITY_EDITOR
            saveData.itemDataPath = UnityEditor.AssetDatabase.GetAssetPath(Data);
#endif
        }

        // 序列化实例数据
        if (holder != null)
        {
            saveData.instanceDataJson = holder.SerializeInstanceData();
        }

        return saveData;
    }

    // 从保存数据加载
    public bool LoadFromSaveData(ItemSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError("保存数据为空，无法加载");
            return false;
        }

        // 设置实例ID
        SetItemInstanceID(saveData.instanceID);

        // 设置基础属性
        gridPosition = saveData.gridPosition;
        isDraggable = saveData.isDraggable;

        // 加载实例数据
        if (holder != null && !string.IsNullOrEmpty(saveData.instanceDataJson))
        {
            bool success = holder.DeserializeInstanceData(saveData.instanceDataJson);
            if (!success)
            {
                Debug.LogWarning($"物品 {GetItemName()} 的实例数据加载失败，使用默认值");
            }
        }

        return true;
    }

    // 序列化为JSON字符串
    public string SerializeToJson()
    {
        try
        {
            ItemSaveData saveData = CreateSaveData();
            return JsonUtility.ToJson(saveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"序列化物品 {GetItemName()} 失败: {e.Message}");
            return "";
        }
    }

    // 从JSON字符串反序列化
    public bool DeserializeFromJson(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
        {
            Debug.LogError("JSON数据为空，无法反序列化");
            return false;
        }

        try
        {
            ItemSaveData saveData = JsonUtility.FromJson<ItemSaveData>(jsonData);
            return LoadFromSaveData(saveData);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"反序列化物品数据失败: {e.Message}");
            return false;
        }
    }

    // === 实用方法 ===

    // 复制物品（创建新的实例ID）
    public InventorySystemItem CreateCopy()
    {
        // 创建新的GameObject
        GameObject newObj = Instantiate(gameObject);
        InventorySystemItem newItem = newObj.GetComponent<InventorySystemItem>();

        if (newItem != null)
        {
            // 生成新的实例ID
            newItem.GenerateNewItemInstanceID();

            // 复制实例数据
            if (holder != null && newItem.holder != null)
            {
                string instanceDataJson = holder.SerializeInstanceData();
                newItem.holder.DeserializeInstanceData(instanceDataJson);
            }
        }

        return newItem;
    }

    // 验证物品完整性
    public bool ValidateItem()
    {
        bool isValid = true;

        // 检查实例ID
        if (!IsItemInstanceIDValid())
        {
            Debug.LogWarning($"物品 {gameObject.name} 的实例ID无效，正在生成新ID");
            GenerateNewItemInstanceID();
            isValid = false;
        }

        // 检查ItemDataHolder
        if (holder == null)
        {
            Debug.LogError($"物品 {gameObject.name} 缺少ItemDataHolder组件");
            isValid = false;
        }
        else
        {
            // 验证实例数据
            if (!holder.ValidateAndRepairInstanceData())
            {
                Debug.LogWarning($"物品 {GetItemName()} 的实例数据已修复");
                isValid = false;
            }
        }

        return isValid;
    }

    // === 调试和测试方法 ===

    // 打印物品详细信息
    [ContextMenu("打印物品详细信息")]
    public void LogItemDetails()
    {
        Debug.Log($"=== 物品详细信息 ===\n" +
                  $"实例ID: {InstanceID}\n" +
                  $"物品名称: {GetItemName()}\n" +
                  $"物品数据ID: {GetItemDataID()}\n" +
                  $"网格位置: {gridPosition}\n" +
                  $"可拖拽: {isDraggable}\n" +
                  $"尺寸: {Size}\n" +
                  $"数据有效性: {HasValidItemData()}");

        // 如果有ItemDataHolder，也打印其信息
        if (holder != null)
        {
            holder.LogItemInfo();
        }
    }

    // 测试序列化
    [ContextMenu("测试序列化")]
    public void TestSerialization()
    {
        string json = SerializeToJson();
        Debug.Log($"序列化结果: {json}");

        // 测试反序列化
        bool success = DeserializeFromJson(json);
        Debug.Log($"反序列化结果: {(success ? "成功" : "失败")}");
    }

    // 验证物品测试
    [ContextMenu("验证物品完整性")]
    public void TestValidateItem()
    {
        bool isValid = ValidateItem();
        Debug.Log($"物品验证结果: {(isValid ? "完整" : "已修复")}");
        LogItemDetails();
    }
}