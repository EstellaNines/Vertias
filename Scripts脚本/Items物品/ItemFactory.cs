using UnityEngine;
using System.Collections.Generic;

public class ItemFactory : MonoBehaviour
{
    [Header("武器类预制体")]
    public GameObject weaponPrefab;

    [Header("护甲类预制体")]
    public GameObject helmetPrefab;     // 头盔
    public GameObject armorPrefab;      // 护甲
    public GameObject chestrigPrefab;   // 胸挂

    [Header("消耗品类预制体")]
    public GameObject foodPrefab;       // 食物
    public GameObject drinkPrefab;      // 饮料
    public GameObject medicalPrefab;    // 医疗用品

    [Header("其他类预制体")]
    public GameObject ammunitionPrefab; // 子弹
    public GameObject currencyPrefab;   // 货币
    public GameObject intelligencePrefab; // 情报
    public GameObject backpackPrefab;   // 背包

    private Dictionary<ItemCategory, GameObject> categoryPrefabs;

    private void Awake()
    {
        InitializePrefabDictionary();
    }

    private void InitializePrefabDictionary()
    {
        categoryPrefabs = new Dictionary<ItemCategory, GameObject>
        {
            // 武器类
            { ItemCategory.枪械, weaponPrefab },
            
            // 护甲类 - 细分
            { ItemCategory.头盔, helmetPrefab },
            { ItemCategory.护甲, armorPrefab },
            { ItemCategory.胸挂, chestrigPrefab },
            
            // 消耗品类 - 细分
            { ItemCategory.食物, foodPrefab },
            { ItemCategory.饮料, drinkPrefab },
            { ItemCategory.治疗类医疗品, medicalPrefab },
            { ItemCategory.止血类医疗品, medicalPrefab },
            { ItemCategory.镇静类医疗品, medicalPrefab },
            
            // 其他类
            { ItemCategory.子弹, ammunitionPrefab },
            { ItemCategory.货币, currencyPrefab },
            { ItemCategory.情报, intelligencePrefab },
            { ItemCategory.背包, backpackPrefab }
        };
    }

    public BaseItem CreateItem(ItemData itemData)
    {
        if (!categoryPrefabs.ContainsKey(itemData.category))
        {
            Debug.LogError($"未找到类别 {itemData.category} 的预制体");
            return null;
        }

        GameObject prefab = categoryPrefabs[itemData.category];
        if (prefab == null)
        {
            Debug.LogError($"类别 {itemData.category} 的预制体引用为空，请在Inspector中分配预制体");
            return null;
        }

        GameObject itemObject = Instantiate(prefab);
        BaseItem item = itemObject.GetComponent<BaseItem>();

        if (item != null)
        {
            item.Set(itemData);
        }
        else
        {
            Debug.LogError($"预制体 {prefab.name} 缺少 BaseItem 组件");
            Destroy(itemObject);
        }

        return item;
    }

    // 验证所有预制体引用是否已分配
    [ContextMenu("验证预制体引用")]
    public void ValidatePrefabReferences()
    {
        bool allValid = true;
        
        if (weaponPrefab == null) { Debug.LogWarning("武器预制体未分配"); allValid = false; }
        if (helmetPrefab == null) { Debug.LogWarning("头盔预制体未分配"); allValid = false; }
        if (armorPrefab == null) { Debug.LogWarning("护甲预制体未分配"); allValid = false; }
        if (chestrigPrefab == null) { Debug.LogWarning("胸挂预制体未分配"); allValid = false; }
        if (foodPrefab == null) { Debug.LogWarning("食物预制体未分配"); allValid = false; }
        if (drinkPrefab == null) { Debug.LogWarning("饮料预制体未分配"); allValid = false; }
        if (medicalPrefab == null) { Debug.LogWarning("医疗用品预制体未分配"); allValid = false; }
        if (ammunitionPrefab == null) { Debug.LogWarning("子弹预制体未分配"); allValid = false; }
        if (currencyPrefab == null) { Debug.LogWarning("货币预制体未分配"); allValid = false; }
        if (intelligencePrefab == null) { Debug.LogWarning("情报预制体未分配"); allValid = false; }
        if (backpackPrefab == null) { Debug.LogWarning("背包预制体未分配"); allValid = false; }
        
        if (allValid)
        {
            Debug.Log("所有预制体引用都已正确分配！");
        }
    }
}