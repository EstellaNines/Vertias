using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.IO;

#if UNITY_EDITOR
public class ItemPrefabCreator : EditorWindow
{
    [Header("预制体生成设置")]
    private string prefabOutputPath = "Assets/InventorySystem/Prefab/";
    private string itemDataPath = "Assets/InventorySystem/Database/Scriptable Object数据对象/";
    
    [MenuItem("InventorySystem/物品预制体生成器")]
    public static void ShowWindow()
    {
        GetWindow<ItemPrefabCreator>("物品预制体生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("物品预制体生成器", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 路径设置
        GUILayout.Label("路径设置:", EditorStyles.boldLabel);
        prefabOutputPath = EditorGUILayout.TextField("预制体输出路径:", prefabOutputPath);
        itemDataPath = EditorGUILayout.TextField("物品数据路径:", itemDataPath);
        
        GUILayout.Space(10);

        // 生成按钮
        if (GUILayout.Button("生成所有物品预制体", GUILayout.Height(30)))
        {
            GenerateAllItemPrefabs();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("清理现有预制体", GUILayout.Height(25)))
        {
            ClearExistingPrefabs();
        }
    }

    private void GenerateAllItemPrefabs()
    {
        // 确保输出目录存在
        if (!Directory.Exists(prefabOutputPath))
        {
            Directory.CreateDirectory(prefabOutputPath);
        }

        // 查找所有InventorySystemItemDataSO文件
        string[] guids = AssetDatabase.FindAssets("t:InventorySystemItemDataSO", new[] { itemDataPath });
        
        if (guids.Length == 0)
        {
            Debug.LogWarning("未找到任何InventorySystemItemDataSO数据文件！");
            return;
        }

        int successCount = 0;
        int totalCount = guids.Length;

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
            InventorySystemItemDataSO itemData = AssetDatabase.LoadAssetAtPath<InventorySystemItemDataSO>(assetPath);
            
            if (itemData != null)
            {
                // 显示进度
                EditorUtility.DisplayProgressBar("生成物品预制体", 
                    $"正在生成: {itemData.itemName} ({i + 1}/{totalCount})", 
                    (float)(i + 1) / totalCount);
                
                if (CreateItemPrefab(itemData))
                {
                    successCount++;
                }
            }
        }
        
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        
        Debug.Log($"预制体生成完成！成功生成 {successCount}/{totalCount} 个预制体。");
        
        if (successCount < totalCount)
        {
            Debug.LogWarning($"有 {totalCount - successCount} 个预制体生成失败，请检查控制台错误信息。");
        }
    }

    private bool CreateItemPrefab(InventorySystemItemDataSO itemData)
    {
        try
        {
            // 创建主GameObject
            GameObject mainObject = new GameObject(itemData.itemName);
            
            // 添加RectTransform组件
            RectTransform mainRect = mainObject.AddComponent<RectTransform>();
            mainRect.sizeDelta = new Vector2(itemData.width * 50f, itemData.height * 50f); // 每格50像素
            mainObject.layer = 5; // UI层

            // 创建ItemBackground子对象
            GameObject backgroundObject = new GameObject("ItemBackground");
            backgroundObject.transform.SetParent(mainObject.transform);
            backgroundObject.layer = 5;
            
            RectTransform backgroundRect = backgroundObject.AddComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
            backgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
            backgroundRect.anchoredPosition = Vector2.zero;
            backgroundRect.sizeDelta = new Vector2(itemData.width * 50f, itemData.height * 50f);
            
            // 添加RawImage组件
            RawImage rawImage = backgroundObject.AddComponent<RawImage>();
            rawImage.color = GetColorFromString(itemData.backgroundColor, 191f / 255f); // 透明度191
            
            // 创建ItemSprite子对象
            GameObject spriteObject = new GameObject("ItemSprite");
            spriteObject.transform.SetParent(mainObject.transform);
            spriteObject.layer = 5;
            
            RectTransform spriteRect = spriteObject.AddComponent<RectTransform>();
            spriteRect.anchorMin = new Vector2(0.5f, 0.5f);
            spriteRect.anchorMax = new Vector2(0.5f, 0.5f);
            spriteRect.anchoredPosition = Vector2.zero;
            spriteRect.sizeDelta = new Vector2(itemData.width * 50f, itemData.height * 50f);
            
            // 添加Image组件
            Image image = spriteObject.AddComponent<Image>();
            image.sprite = itemData.itemIcon;
            image.preserveAspect = true;
            
            // 创建ItemScriptableObject子对象
            GameObject scriptObject = new GameObject("ItemScriptableObject");
            scriptObject.transform.SetParent(mainObject.transform);
            scriptObject.layer = 5;
            
            RectTransform scriptRect = scriptObject.AddComponent<RectTransform>();
            scriptRect.anchorMin = new Vector2(0.5f, 0.5f);
            scriptRect.anchorMax = new Vector2(0.5f, 0.5f);
            scriptRect.anchoredPosition = Vector2.zero;
            scriptRect.sizeDelta = new Vector2(itemData.width * 50f, itemData.height * 50f);
            
            // 添加ItemDataHolder脚本
            ItemDataHolder dataHolder = scriptObject.AddComponent<ItemDataHolder>();
            
            // 通过反射设置私有字段
            var itemDataField = typeof(ItemDataHolder).GetField("itemData", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var itemIconField = typeof(ItemDataHolder).GetField("itemIconImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var backgroundField = typeof(ItemDataHolder).GetField("backgroundImage", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            itemDataField?.SetValue(dataHolder, itemData);
            itemIconField?.SetValue(dataHolder, image);
            backgroundField?.SetValue(dataHolder, rawImage);

            // 确保分类文件夹存在
            string categoryFolder = Path.Combine(prefabOutputPath, GetCategoryFolderName(itemData.category));
            if (!Directory.Exists(categoryFolder))
            {
                Directory.CreateDirectory(categoryFolder);
            }

            // 保存为预制体
            string prefabPath = Path.Combine(categoryFolder, CleanFileName(itemData.itemName) + ".prefab");
            prefabPath = prefabPath.Replace("\\", "/"); // 统一路径分隔符
            
            // 检查是否已存在同名预制体
            if (File.Exists(prefabPath))
            {
                Debug.LogWarning($"预制体已存在，将覆盖: {prefabPath}");
            }
            
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(mainObject, prefabPath);
            
            // 清理临时对象
            DestroyImmediate(mainObject);
            
            if (prefab != null)
            {
                Debug.Log($"成功创建预制体: {prefabPath}");
                return true;
            }
            else
            {
                Debug.LogError($"创建预制体失败: {itemData.itemName}");
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建预制体时发生错误 [{itemData.itemName}]: {e.Message}");
            return false;
        }
    }

    // 根据背景颜色字符串获取Color
    private Color GetColorFromString(string colorName, float alpha = 1f)
    {
        Color baseColor;
        switch (colorName?.ToLower())
        {
            case "blue":
                ColorUtility.TryParseHtmlString("#2d3c4b", out baseColor);
                break;
            case "violet":
            case "purple":
                ColorUtility.TryParseHtmlString("#583b80", out baseColor);
                break;
            case "yellow":
                ColorUtility.TryParseHtmlString("#80550d", out baseColor);
                break;
            case "red":
                ColorUtility.TryParseHtmlString("#350000", out baseColor);
                break;
            default:
                baseColor = Color.white;
                break;
        }
        
        baseColor.a = alpha;
        return baseColor;
    }

    // 获取分类文件夹名称
    private string GetCategoryFolderName(string category)
    {
        switch (category?.ToLower())
        {
            case "helmet": return "头盔Helmet";
            case "armor": return "护甲Armor";
            case "backpack": return "背包Backpack";
            case "tacticalrig": return "战术挂具TacticalRig";
            case "weapon": return "武器Weapon";
            case "ammunition": return "弹药Ammunition";
            case "healing": return "治疗用品Healing";
            case "hemostatic": return "止血剂Hemostatic";
            case "sedative": return "镇静剂Sedative";
            case "food": return "食物Food";
            case "drink": return "饮料Drink";
            case "intelligence": return "情报Intelligence";
            case "currency": return "货币Currency";
            default: return "其他Other";
        }
    }

    // 清理文件名
    private string CleanFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "UnknownItem";
        
        // 移除或替换不合法的文件名字符
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char c in invalidChars)
        {
            fileName = fileName.Replace(c, '_');
        }
        
        // 移除小数点
        fileName = fileName.Replace(".", "");
        
        return fileName.Trim();
    }

    // 清理现有预制体
    private void ClearExistingPrefabs()
    {
        if (EditorUtility.DisplayDialog("确认清理", 
            "这将删除输出路径下的所有预制体文件，此操作不可撤销！\n确定要继续吗？", 
            "确定", "取消"))
        {
            try
            {
                if (Directory.Exists(prefabOutputPath))
                {
                    string[] prefabFiles = Directory.GetFiles(prefabOutputPath, "*.prefab", SearchOption.AllDirectories);
                    
                    foreach (string file in prefabFiles)
                    {
                        AssetDatabase.DeleteAsset(file.Replace("\\", "/").Replace(Application.dataPath, "Assets"));
                    }
                    
                    AssetDatabase.Refresh();
                    Debug.Log($"已清理 {prefabFiles.Length} 个预制体文件。");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"清理预制体时发生错误: {e.Message}");
            }
        }
    }
}
#endif