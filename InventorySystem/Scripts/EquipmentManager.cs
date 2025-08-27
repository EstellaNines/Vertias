using System.Collections.Generic;
using UnityEngine;
using InventorySystem;

/// <summary>
/// 装备管理器 - 统一管理所有装备槽位
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [Header("装备槽位GameObject")]
    [SerializeField] private GameObject helmetSlotObject;      // 头盔槽位GameObject
    [SerializeField] private GameObject armorSlotObject;       // 护甲槽位GameObject
    [SerializeField] private GameObject tacticalRigSlotObject; // 战术背心槽位GameObject
    [SerializeField] private GameObject backpackSlotObject;    // 背包槽位GameObject
    [SerializeField] private GameObject primaryWeaponSlotObject;   // 主武器槽位GameObject
    [SerializeField] private GameObject secondaryWeaponSlotObject; // 副武器槽位GameObject

    // 装备槽位引用 - 动态获取
    private EquipmentSlot helmetSlot;      // 头盔槽位
    private EquipmentSlot armorSlot;       // 护甲槽位
    private EquipmentSlot tacticalRigSlot; // 战术背心槽位
    private EquipmentSlot backpackSlot;    // 背包槽位
    private EquipmentSlot primaryWeaponSlot;   // 主武器槽位
    private EquipmentSlot secondaryWeaponSlot; // 副武器槽位

    // 装备槽位字典，便于管理
    private Dictionary<ItemCategory, List<EquipmentSlot>> equipmentSlots;

    // 单例模式
    private static EquipmentManager instance;
    public static EquipmentManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<EquipmentManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        // 确保单例
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 延迟初始化，确保所有预制件都已实例化
        InitializeEquipmentSlots();
    }

    /// <summary>
    /// 初始化装备槽位 - 从各个GameObject中查找
    /// </summary>
    private void InitializeEquipmentSlots()
    {
        equipmentSlots = new Dictionary<ItemCategory, List<EquipmentSlot>>();

        // 从各个GameObject中查找EquipmentSlot组件
        FindEquipmentSlots();

        // 注册各个装备槽位
        RegisterEquipmentSlot(ItemCategory.Helmet, helmetSlot);
        RegisterEquipmentSlot(ItemCategory.Armor, armorSlot);
        RegisterEquipmentSlot(ItemCategory.TacticalRig, tacticalRigSlot);
        RegisterEquipmentSlot(ItemCategory.Backpack, backpackSlot);

        // 注册两个武器槽位
        RegisterEquipmentSlot(ItemCategory.Weapon, primaryWeaponSlot);
        RegisterEquipmentSlot(ItemCategory.Weapon, secondaryWeaponSlot);

        
    }

    /// <summary>
    /// 从各个GameObject中查找EquipmentSlot组件
    /// </summary>
    private void FindEquipmentSlots()
    {
        // 头盔槽位
        if (helmetSlotObject != null)
        {
            helmetSlot = helmetSlotObject.GetComponentInChildren<EquipmentSlot>();
            if (helmetSlot != null)
            {
                    
            }
            else
                Debug.LogWarning("头盔槽位GameObject中未找到EquipmentSlot组件: " + helmetSlotObject.name);
        }
        else
        {
            Debug.LogWarning("头盔槽位GameObject未设置！");
        }

        // 护甲槽位
        if (armorSlotObject != null)
        {
            armorSlot = armorSlotObject.GetComponentInChildren<EquipmentSlot>();
            if (armorSlot != null)
            {

            }
            else
                Debug.LogWarning("护甲槽位GameObject中未找到EquipmentSlot组件: " + armorSlotObject.name);
        }
        else
        {
            Debug.LogWarning("护甲槽位GameObject未设置！");
        }

        // 战术背心槽位
        if (tacticalRigSlotObject != null)
        {
            tacticalRigSlot = tacticalRigSlotObject.GetComponentInChildren<EquipmentSlot>();
            if (tacticalRigSlot != null)
            {

            }
            else
                Debug.LogWarning("战术背心槽位GameObject中未找到EquipmentSlot组件: " + tacticalRigSlotObject.name);
        }
        else
        {
            Debug.LogWarning("战术背心槽位GameObject未设置！");
        }

        // 背包槽位
        if (backpackSlotObject != null)
        {
            backpackSlot = backpackSlotObject.GetComponentInChildren<EquipmentSlot>();
            if (backpackSlot != null)
            {

            }
            else
                Debug.LogWarning("背包槽位GameObject中未找到EquipmentSlot组件: " + backpackSlotObject.name);
        }
        else
        {
            Debug.LogWarning("背包槽位GameObject未设置！");
        }

        // 主武器槽位
        if (primaryWeaponSlotObject != null)
        {
            primaryWeaponSlot = primaryWeaponSlotObject.GetComponentInChildren<EquipmentSlot>();
            if (primaryWeaponSlot != null)
            {

            }
            else
                Debug.LogWarning("主武器槽位GameObject中未找到EquipmentSlot组件: " + primaryWeaponSlotObject.name);
        }
        else
        {
            Debug.LogWarning("主武器槽位GameObject未设置！");
        }

        // 副武器槽位
        if (secondaryWeaponSlotObject != null)
        {
            secondaryWeaponSlot = secondaryWeaponSlotObject.GetComponentInChildren<EquipmentSlot>();
            if (secondaryWeaponSlot != null)
            {
                
            }
            else
                Debug.LogWarning("副武器槽位GameObject中未找到EquipmentSlot组件: " + secondaryWeaponSlotObject.name);
        }
        else
        {
            Debug.LogWarning("副武器槽位GameObject未设置！");
        }
    }

    /// <summary>
    /// 注册装备槽位
    /// </summary>
    /// <param name="category">装备类型</param>
    /// <param name="slot">装备槽位</param>
    private void RegisterEquipmentSlot(ItemCategory category, EquipmentSlot slot)
    {
        if (slot != null)
        {
            slot.SetAcceptedItemType(category);

            if (!equipmentSlots.ContainsKey(category))
            {
                equipmentSlots[category] = new List<EquipmentSlot>();
            }
            equipmentSlots[category].Add(slot);
        }
    }

    /// <summary>
    /// 获取指定类型的第一个可用装备槽位
    /// </summary>
    /// <param name="category">装备类型</param>
    /// <returns>装备槽位</returns>
    public EquipmentSlot GetAvailableEquipmentSlot(ItemCategory category)
    {
        if (equipmentSlots.TryGetValue(category, out List<EquipmentSlot> slots))
        {
            // 优先返回空的槽位
            foreach (var slot in slots)
            {
                if (slot.GetCurrentEquippedItem() == null)
                {
                    return slot;
                }
            }
            // 如果没有空槽位，返回第一个
            return slots.Count > 0 ? slots[0] : null;
        }
        return null;
    }

    /// <summary>
    /// 获取指定类型的所有装备槽位
    /// </summary>
    /// <param name="category">装备类型</param>
    /// <returns>装备槽位列表</returns>
    public List<EquipmentSlot> GetEquipmentSlots(ItemCategory category)
    {
        equipmentSlots.TryGetValue(category, out List<EquipmentSlot> slots);
        return slots ?? new List<EquipmentSlot>();
    }

    /// <summary>
    /// 获取主武器槽位
    /// </summary>
    /// <returns>主武器槽位</returns>
    public EquipmentSlot GetPrimaryWeaponSlot()
    {
        return primaryWeaponSlot;
    }

    /// <summary>
    /// 获取副武器槽位
    /// </summary>
    /// <returns>副武器槽位</returns>
    public EquipmentSlot GetSecondaryWeaponSlot()
    {
        return secondaryWeaponSlot;
    }

    /// <summary>
    /// 装备物品到指定槽位
    /// </summary>
    /// <param name="item">要装备的物品</param>
    /// <param name="category">装备类型</param>
    /// <returns>是否装备成功</returns>
    public bool EquipItem(GameObject item, ItemCategory category)
    {
        EquipmentSlot slot = GetAvailableEquipmentSlot(category);
        if (slot != null)
        {
            ItemDataReader itemDataReader = item.GetComponent<ItemDataReader>();
            if (itemDataReader != null && itemDataReader.ItemData.category == category)
            {
                // 这里可以添加装备逻辑
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 卸下指定类型的装备
    /// </summary>
    /// <param name="category">装备类型</param>
    /// <returns>被卸下的装备</returns>
    public GameObject UnequipItem(ItemCategory category)
    {
        if (equipmentSlots.TryGetValue(category, out List<EquipmentSlot> slots))
        {
            foreach (var slot in slots)
            {
                GameObject equippedItem = slot.GetCurrentEquippedItem();
                if (equippedItem != null)
                {
                    slot.UnequipItem();
                    return equippedItem;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 获取当前装备的所有物品
    /// </summary>
    /// <returns>装备物品列表</returns>
    public List<GameObject> GetAllEquippedItems()
    {
        List<GameObject> equippedItems = new List<GameObject>();

        foreach (var slotList in equipmentSlots.Values)
        {
            foreach (var slot in slotList)
            {
                GameObject item = slot.GetCurrentEquippedItem();
                if (item != null)
                {
                    equippedItems.Add(item);
                }
            }
        }

        return equippedItems;
    }

    /// <summary>
    /// 检查是否有指定类型的装备
    /// </summary>
    /// <param name="category">装备类型</param>
    /// <returns>是否有装备</returns>
    public bool HasEquipment(ItemCategory category)
    {
        if (equipmentSlots.TryGetValue(category, out List<EquipmentSlot> slots))
        {
            foreach (var slot in slots)
            {
                if (slot.GetCurrentEquippedItem() != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 清空所有装备
    /// </summary>
    public void ClearAllEquipment()
    {
        foreach (var slotList in equipmentSlots.Values)
        {
            foreach (var slot in slotList)
            {
                slot.UnequipItem();
            }
        }
    }
}