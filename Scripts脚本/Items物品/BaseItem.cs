using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

// 基础物品类
public abstract class BaseItem : MonoBehaviour
{
    public ItemData itemData;
    public int onGridPositionX;
    public int onGridPositionY;

    protected Image backgroundImage;
    protected Image iconImage;

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void InitializeComponents()
    {
        backgroundImage = GetComponent<Image>();
        if (backgroundImage == null)
        {
            backgroundImage = gameObject.AddComponent<Image>();
        }
    }

    public virtual void Set(ItemData itemData)
    {
        this.itemData = itemData;
        CreateIconObject();
        LoadItemDataFromJson(itemData);
        OnItemSet();
    }

    protected virtual void OnItemSet()
    {
        // 子类可以重写此方法来添加特定逻辑
    }

    private void CreateIconObject()
    {
        Transform iconTransform = transform.Find("ItemIcon");

        if (iconTransform == null)
        {
            GameObject iconObject = new GameObject("ItemIcon");
            iconObject.transform.SetParent(transform, false);
            iconImage = iconObject.AddComponent<Image>();

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

        iconImage.sprite = itemData.itemIcon;
    }

    // 从JSON加载物品数据、尺寸和背景颜色（保持原有Item.cs的功能）
    private void LoadItemDataFromJson(ItemData itemData)
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("ItemData");
        if (jsonFile == null)
        {
            Debug.LogError("无法加载ItemData.json文件");
            return;
        }

        ItemDatabase database = JsonUtility.FromJson<ItemDatabase>(jsonFile.text);
        if (database == null || database.categories == null)
        {
            Debug.LogError("JSON文件解析失败");
            return;
        }

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

        foreach (var category in allCategories)
        {
            if (category == null || category.items == null) continue;

            foreach (var item in category.items)
            {
                if (item.name == itemData.name)
                {
                    itemData.width = item.width;
                    itemData.height = item.height;

                    Vector2 size = new Vector2();
                    size.x = item.width * ItemGrid.tileSizeWidth;
                    size.y = item.height * ItemGrid.tileSizeHeight;
                    GetComponent<RectTransform>().sizeDelta = size;

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

    // 抽象方法，子类必须实现
    public abstract ItemCategory GetItemCategory();
    public abstract void OnUse();
    public abstract bool CanStackWith(BaseItem other);
}