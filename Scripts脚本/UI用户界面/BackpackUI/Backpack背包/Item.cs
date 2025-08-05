using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Item : MonoBehaviour
{
    public ItemData itemData;
    public int onGridPositionX;//占用格子x坐标
    public int onGridPositionY;//占用格子y坐标

    private Image backgroundImage; // 物品背景图片
    private Image iconImage; // 物品图标图片

    private void Awake()
    {
        // 确保有背景图片组件
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            // 如果没有Image组件，添加一个
            backgroundImage = gameObject.AddComponent<Image>();
        }
    }

    public void Set(ItemData itemData)
    {
        this.itemData = itemData;

        // 创建一个子物品图标对象
        Transform iconTransform = transform.Find("ItemIcon");

        if (iconTransform == null)
        {
            // 如果不存在，创建一个新的子对象作为图标
            GameObject iconObject = new GameObject("ItemIcon");
            iconObject.transform.SetParent(transform, false);
            iconImage = iconObject.AddComponent<Image>();

            // 设置图标填充整个父对象
            RectTransform iconRect = iconImage.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
        }
        else
        {
            iconImage = iconTransform.GetComponent<Image>();
        }

        // 设置图标精灵
        iconImage.sprite = itemData.itemIcon;

        // 从JSON加载物品数据、尺寸和背景颜色
        LoadItemDataFromJson(itemData);
    }

    // 从JSON加载物品数据、尺寸和背景颜色
    private void LoadItemDataFromJson(ItemData itemData)
    {
        // 加载JSON文件
        TextAsset jsonFile = Resources.Load<TextAsset>("ItemData");
        if (jsonFile == null)
        {
            Debug.LogError("无法加载ItemData.json文件");
            return;
        }

        // 解析JSON
        ItemDatabase database = JsonUtility.FromJson<ItemDatabase>(jsonFile.text);
        if (database == null || database.categories == null)
        {
            Debug.LogError("JSON文件解析失败");
            return;
        }

        // 创建所有类别的列表
        List<CategoryData> allCategories = new List<CategoryData>
        {
            database.categories.weapons,
            database.categories.ammunition,
            database.categories.helmets,
            database.categories.armor,
            database.categories.chest_rigs,
            database.categories.medical,
            database.categories.food_drink,
            database.categories.currency,
            database.categories.intelligence,
            database.categories.backpacks
        };

        // 查找物品对应的类别和ID
        foreach (var category in allCategories)
        {
            if (category == null || category.items == null) continue;

            foreach (var item in category.items)
            {
                if (item.name == itemData.name)
                {
                    // 更新ItemData的尺寸属性（重要！）
                    itemData.width = item.width;
                    itemData.height = item.height;

                    // 设置物品视觉尺寸
                    Vector2 size = new Vector2();
                    size.x = item.width * ItemGrid.tileSizeWidth;
                    size.y = item.height * ItemGrid.tileSizeHeight;
                    GetComponent<RectTransform>().sizeDelta = size;

                    // 设置背景颜色
                    if (database.rarityColors != null && database.rarityColors.Length > 0)
                    {
                        int rarityLevel = item.rarityLevel;
                        if (rarityLevel >= 1 && rarityLevel <= database.rarityColors.Length)
                        {
                            string colorHex = database.rarityColors[rarityLevel - 1].colorHex;
                            Color color;
                            if (ColorUtility.TryParseHtmlString(colorHex, out color))
                            {
                                backgroundImage.color = color;
                            }
                        }
                    }
                    return;
                }
            }
        }

        Debug.LogWarning($"在JSON中未找到物品: {itemData.name}");
    }

    // 显示背景
    public void ShowBackground()
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
        }
    }

    // 隐藏背景
    public void HideBackground()
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = false;
        }
    }

    // 设置背景透明度
    public void SetBackgroundAlpha(float alpha)
    {
        if (backgroundImage != null)
        {
            Color color = backgroundImage.color;
            color.a = alpha;
            backgroundImage.color = color;
        }
    }
}

// 用于解析JSON的辅助类
[Serializable]
public class ItemDatabase
{
    public CategoryContainer categories;
    public RarityColorData[] rarityColors;
}

[Serializable]
public class CategoryContainer
{
    public CategoryData weapons;
    public CategoryData ammunition;
    public CategoryData helmets;
    public CategoryData armor;
    public CategoryData chest_rigs;
    public CategoryData medical;
    public CategoryData food_drink;
    public CategoryData currency;
    public CategoryData intelligence;
    public CategoryData backpacks;
}

[Serializable]
public class CategoryData
{
    public string name;
    public int id;
    public List<ItemJsonData> items;
}

[Serializable]
public class ItemJsonData
{
    public int id;
    public string name;
    public string description;
    public int rarityLevel;
    public int width = 1; // 默认宽度为1
    public int height = 1; // 默认高度为1
}

[Serializable]
public class RarityColorData
{
    public int level;
    public string colorHex;
}
