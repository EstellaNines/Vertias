using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

public class ItemDataGenerator : EditorWindow
{
    private string jsonFilePath = "Assets/Resources/InventorySystemResources/InventorySystemDatabase/InventorySystemStaticDatabase.json";
    private string outputPath = "Assets/InventorySystem/Database/Scriptable Object数据对象/";

    [MenuItem("Tools/Generate Item Data from JSON")]
    [MenuItem("InventorySystem/物品数据生成器")]
    public static void ShowWindow()
    {
        GetWindow<ItemDataGenerator>("物品数据生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("物品数据生成器", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        GUILayout.Label("JSON文件路径:");
        jsonFilePath = EditorGUILayout.TextField(jsonFilePath);

        EditorGUILayout.Space();

        GUILayout.Label("输出路径:");
        outputPath = EditorGUILayout.TextField(outputPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("生成ScriptableObject数据", GUILayout.Height(30)))
        {
            GenerateItemData();
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("清空现有数据", GUILayout.Height(25)))
        {
            ClearExistingData();
        }
    }

    private void GenerateItemData()
    {
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError($"JSON文件不存在: {jsonFilePath}");
            return;
        }

        // 确保输出目录存在
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            InventorySystemJsonDataStructure jsonData = JsonUtility.FromJson<InventorySystemJsonDataStructure>(jsonContent);

            int totalCreated = 0;

            // 使用反射获取所有类别
            FieldInfo[] categoryFields = typeof(InventorySystemCategoryData).GetFields();

            foreach (FieldInfo field in categoryFields)
            {
                InventorySystemJsonCategory category = (InventorySystemJsonCategory)field.GetValue(jsonData.category);
                if (category != null && category.items != null)
                {
                    totalCreated += CreateItemsForCategory(category, field.Name);
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"成功创建了 {totalCreated} 个物品数据对象!");
            EditorUtility.DisplayDialog("完成", $"成功创建了 {totalCreated} 个物品数据对象!", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"生成物品数据时出错: {e.Message}");
            EditorUtility.DisplayDialog("错误", $"生成失败: {e.Message}", "确定");
        }
    }

    private int CreateItemsForCategory(InventorySystemJsonCategory category, string categoryName)
    {
        int created = 0;

        // 创建分类文件夹
        string categoryFolderPath = Path.Combine(outputPath, GetCategoryFolderName(categoryName));
        if (!Directory.Exists(categoryFolderPath))
        {
            Directory.CreateDirectory(categoryFolderPath);
            AssetDatabase.Refresh();
        }

        foreach (InventorySystemJsonItemData jsonItem in category.items)
        {
            // 创建ScriptableObject实例
            InventorySystemItemDataSO itemData = ScriptableObject.CreateInstance<InventorySystemItemDataSO>();

            // 填充数据
            itemData.id = jsonItem.id;
            itemData.itemName = jsonItem.name;
            itemData.shortName = jsonItem.shortName;
            itemData.height = jsonItem.height;
            itemData.width = jsonItem.width;
            itemData.rarity = jsonItem.rarity;
            itemData.CellH = jsonItem.cellH;
            itemData.CellV = jsonItem.cellV;
            itemData.BulletType = jsonItem.type;
            itemData.backgroundColor = jsonItem.BackgroundColor;
            itemData.durability = jsonItem.durability;
            itemData.usageCount = jsonItem.usageCount;
            itemData.maxHealAmount = jsonItem.maxHealAmount;
            itemData.maxStack = jsonItem.maxStack;
            itemData.intelligenceValue = jsonItem.intelligenceValue;

            // 设置正确的枚举类别而不是字符串
            if (System.Enum.TryParse<InventorySystemItemCategory>(categoryName, out InventorySystemItemCategory categoryEnum))
            {
                itemData.itemCategory = categoryEnum;
            }
            else
            {
                Debug.LogWarning($"无法解析类别: {categoryName}，使用默认值 Helmet");
                itemData.itemCategory = InventorySystemItemCategory.Helmet;
            }
            itemData.category = categoryName;

            // 使用JSON中的ItemIcon字段加载图标
            if (!string.IsNullOrEmpty(jsonItem.ItemIcon))
            {
                string spritePath = FindSpriteByFileName(jsonItem.ItemIcon, categoryName);
                if (!string.IsNullOrEmpty(spritePath))
                {
                    itemData.itemIcon = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                }
                else
                {
                    Debug.LogWarning($"未找到图标文件: {jsonItem.ItemIcon} (物品: {jsonItem.name})");
                }
            }

            // 生成文件名（清理特殊字符）
            string fileName = CleanFileName(jsonItem.name);
            string assetPath = Path.Combine(categoryFolderPath, $"{fileName}.asset");

            // 如果文件已存在，添加ID后缀
            if (File.Exists(assetPath))
            {
                assetPath = Path.Combine(categoryFolderPath, $"{fileName}_{jsonItem.id}.asset");
            }

            // 创建资产文件
            AssetDatabase.CreateAsset(itemData, assetPath);
            created++;
        }

        return created;
    }

    private string GetCategoryFolderName(string categoryName)
    {
        // 为每个分类返回中文文件夹名称
        switch (categoryName)
        {
            case "Helmet":
                return "头盔Helmet";
            case "Armor":
                return "护甲Armor";
            case "TacticalRig":
                return "战术挂具TacticalRig";
            case "Backpack":
                return "背包Backpack";
            case "Weapon":
                return "武器Weapon";
            case "Ammunition":
                return "弹药Ammunition";
            case "Food":
                return "食物Food";
            case "Drink":
                return "饮料Drink";
            case "Sedative":
                return "镇静剂Sedative";
            case "Hemostatic":
                return "止血剂Hemostatic";
            case "Healing":
                return "治疗用品Healing";
            case "Intelligence":
                return "情报Intelligence";
            case "Currency":
                return "货币Currency";
            default:
                return categoryName;
        }
    }

    private string FindSpriteByFileName(string fileName, string categoryName)
    {
        // 在InventorySystem/Sprite目录中查找
        string[] searchPaths = {
            $"Assets/InventorySystem/Sprite/{categoryName}",
            $"Assets/InventorySystem/Sprite",
            $"Assets/UI/ItemsUI/{GetUICategoryName(categoryName)}",
            $"Assets/Sprites精灵"
        };

        foreach (string searchPath in searchPaths)
        {
            if (Directory.Exists(searchPath))
            {
                string[] files = Directory.GetFiles(searchPath, fileName, SearchOption.AllDirectories);
                if (files.Length > 0)
                {
                    return files[0];
                }
            }
        }

        return null;
    }

    private string GetUICategoryName(string categoryName)
    {
        switch (categoryName)
        {
            case "Helmet":
            case "Armor":
            case "TacticalRig":
                return "Armor护甲UI";
            case "Backpack":
                return "Backpack背包UI";
            case "Weapon":
                return "Guns枪械UI";
            case "Ammunition":
                return "Bullets子弹UI";
            case "Food":
                return "Food食物UI";
            case "Drink":
            case "Sedative":
            case "Hemostatic":
            case "Healing":
                return "Medical医疗UI";
            case "Intelligence":
                return "Intelligence情报UI";
            case "Currency":
                return "Money金钱UI";
            default:
                return "";
        }
    }

    private string CleanFileName(string fileName)
    {
        // 清理文件名中的特殊字符
        string cleaned = fileName;
        char[] invalidChars = Path.GetInvalidFileNameChars();

        foreach (char c in invalidChars)
        {
            cleaned = cleaned.Replace(c, '_');
        }

        // 替换其他可能有问题的字符
        cleaned = cleaned.Replace(" ", "_")
                        .Replace(".", "")  // 删除小数点而不是替换为下划线
                        .Replace("-", "_")
                        .Replace("(", "_")
                        .Replace(")", "_");

        return cleaned;
    }

    private void ClearExistingData()
    {
        if (EditorUtility.DisplayDialog("确认清空", "确定要删除所有现有的物品数据吗？此操作不可撤销！", "确定", "取消"))
        {
            if (Directory.Exists(outputPath))
            {
                // 删除所有分类文件夹及其内容
                string[] categoryFolders = Directory.GetDirectories(outputPath);
                foreach (string folder in categoryFolders)
                {
                    Directory.Delete(folder, true);
                }

                // 删除根目录下的.asset文件
                string[] assetFiles = Directory.GetFiles(outputPath, "*.asset");
                foreach (string file in assetFiles)
                {
                    AssetDatabase.DeleteAsset(file);
                }

                AssetDatabase.Refresh();
                Debug.Log($"已清空所有现有数据文件和文件夹");
            }
        }
    }
}