using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro; // 添加TextMeshPro命名空间

[System.Serializable]
public class LoadingUIData
{
    public List<LoadingDataItem> LoadingData;
}

[System.Serializable]
public class LoadingDataItem
{
    public int id;
    public string name;              // 新增：地图名称
    public string description;
    public List<string> loadingImages;
}

public class LoadingUIController : MonoBehaviour
{
    [Header("UI组件")]
    public Image loadingImage;           // 显示加载图片的Image组件
    public TextMeshProUGUI nameText;     // 显示地图名称的TextMeshPro组件
    public TextMeshProUGUI descriptionText;         // 显示描述文本的TextMeshPro组件

    [Header("设置")]
    public string jsonFileName = "LoadingUIData";  // JSON文件名（不包含扩展名）
    public string imagesFolderPath = "LoadingBackground";  // 图片文件夹路径（Resources下的相对路径）

    [Header("调试信息")]
    public bool enableDebugLogs = true;  // 启用调试日志

    private LoadingUIData loadingUIData;
    private bool isDataLoaded = false;

    void Start()
    {
        LoadJsonData();
        // 移除自动显示随机内容，只加载数据
        
        // 验证UI组件
        ValidateUIComponents();
    }

    // 验证UI组件是否正确设置
    void ValidateUIComponents()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== LoadingUIController 组件验证 ===");
            Debug.Log($"loadingImage 是否为空: {loadingImage == null}");
            Debug.Log($"nameText 是否为空: {nameText == null}");
            Debug.Log($"descriptionText 是否为空: {descriptionText == null}");
            
            if (loadingImage != null)
            {
                Debug.Log($"loadingImage GameObject: {loadingImage.gameObject.name}");
                Debug.Log($"loadingImage 是否激活: {loadingImage.gameObject.activeInHierarchy}");
            }
            
            if (nameText != null)
            {
                Debug.Log($"nameText GameObject: {nameText.gameObject.name}");
                Debug.Log($"nameText 是否激活: {nameText.gameObject.activeInHierarchy}");
            }
            
            if (descriptionText != null)
            {
                Debug.Log($"descriptionText GameObject: {descriptionText.gameObject.name}");
                Debug.Log($"descriptionText 是否激活: {descriptionText.gameObject.activeInHierarchy}");
            }
        }
    }

    // 加载JSON数据
    void LoadJsonData()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"开始加载JSON文件: {jsonFileName}");
        }

        TextAsset jsonFile = Resources.Load<TextAsset>(jsonFileName);
        if (jsonFile != null)
        {
            try
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"JSON文件内容长度: {jsonFile.text.Length}");
                    Debug.Log($"JSON文件前100个字符: {jsonFile.text.Substring(0, Mathf.Min(100, jsonFile.text.Length))}");
                }

                loadingUIData = JsonUtility.FromJson<LoadingUIData>(jsonFile.text);
                isDataLoaded = true;
                
                if (loadingUIData != null && loadingUIData.LoadingData != null)
                {
                    Debug.Log($"成功加载JSON数据，包含 {loadingUIData.LoadingData.Count} 个加载项");
                    
                    if (enableDebugLogs)
                    {
                        for (int i = 0; i < loadingUIData.LoadingData.Count; i++)
                        {
                            var item = loadingUIData.LoadingData[i];
                            Debug.Log($"ID {item.id}: 描述长度={item.description?.Length ?? 0}, 图片数量={item.loadingImages?.Count ?? 0}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("JSON数据解析后为空");
                    isDataLoaded = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"解析JSON数据失败: {e.Message}");
                Debug.LogError($"堆栈跟踪: {e.StackTrace}");
                isDataLoaded = false;
            }
        }
        else
        {
            Debug.LogError($"无法找到JSON文件: {jsonFileName}，请确保文件在Resources文件夹中");
            isDataLoaded = false;
        }
    }

    // 显示随机的加载内容（每次传送时调用）
    public void DisplayRandomLoadingContent()
    {
        if (enableDebugLogs)
        {
            Debug.Log("=== 开始显示随机加载内容 ===");
        }

        // 如果数据未加载，先尝试加载
        if (!isDataLoaded)
        {
            if (enableDebugLogs) Debug.Log("数据未加载，重新加载JSON数据");
            LoadJsonData();
        }

        if (loadingUIData == null || loadingUIData.LoadingData == null || loadingUIData.LoadingData.Count == 0)
        {
            Debug.LogWarning("没有可用的加载数据");
            return;
        }

        // 随机选择一个加载项
        int randomIndex = Random.Range(0, loadingUIData.LoadingData.Count);
        LoadingDataItem selectedItem = loadingUIData.LoadingData[randomIndex];

        if (enableDebugLogs)
        {
            Debug.Log($"随机选择索引: {randomIndex}, ID: {selectedItem.id}");
            Debug.Log($"地图名称: {selectedItem.name}");
            Debug.Log($"描述内容: {selectedItem.description}");
            Debug.Log($"图片列表: {string.Join(", ", selectedItem.loadingImages ?? new List<string>())}");
        }

        // 显示地图名称
        DisplayMapName(selectedItem.name);
        
        // 显示描述文本
        DisplayDescription(selectedItem.description);

        // 显示随机图片
        DisplayRandomImage(selectedItem.loadingImages);

        Debug.Log($"传送时显示ID {selectedItem.id} ({selectedItem.name}) 的加载内容");
    }

    // 显示指定ID的加载内容
    public void DisplayLoadingContentById(int id)
    {
        if (!isDataLoaded)
        {
            LoadJsonData();
        }

        if (loadingUIData == null || loadingUIData.LoadingData == null)
        {
            Debug.LogWarning("没有可用的加载数据");
            return;
        }

        LoadingDataItem targetItem = loadingUIData.LoadingData.FirstOrDefault(item => item.id == id);
        if (targetItem != null)
        {
            DisplayMapName(targetItem.name);
            DisplayDescription(targetItem.description);
            DisplayRandomImage(targetItem.loadingImages);
            Debug.Log($"显示ID {id} ({targetItem.name}) 的加载内容");
        }
        else
        {
            Debug.LogWarning($"未找到ID为 {id} 的加载数据");
        }
    }

    // 显示地图名称
    void DisplayMapName(string mapName)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"=== 显示地图名称 ===");
            Debug.Log($"地图名称: {mapName}");
            Debug.Log($"nameText 组件是否为空: {nameText == null}");
        }

        if (nameText != null)
        {
            nameText.text = mapName;
            
            if (enableDebugLogs)
            {
                Debug.Log($"名称设置完成，当前文本: {nameText.text}");
                Debug.Log($"名称组件是否激活: {nameText.gameObject.activeInHierarchy}");
                Debug.Log($"名称组件颜色: {nameText.color}");
                Debug.Log($"名称组件字体大小: {nameText.fontSize}");
            }
        }
        else
        {
            Debug.LogWarning("地图名称文本组件未设置");
        }
    }

    // 显示描述文本
    // 参数 description: 描述内容
    void DisplayDescription(string description)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"=== 显示描述文本 ===");
            Debug.Log($"描述内容: {description}");
            Debug.Log($"descriptionText 组件是否为空: {descriptionText == null}");
        }

        if (descriptionText != null)
        {
            descriptionText.text = description;
            
            if (enableDebugLogs)
            {
                Debug.Log($"文本设置完成，当前文本: {descriptionText.text}");
                Debug.Log($"文本组件是否激活: {descriptionText.gameObject.activeInHierarchy}");
                Debug.Log($"文本组件颜色: {descriptionText.color}");
                Debug.Log($"文本组件字体大小: {descriptionText.fontSize}");
            }
        }
        else
        {
            Debug.LogWarning("描述文本组件未设置");
        }
    }

    // 显示随机图片
    // 参数 imageNames: 图片名称列表
    void DisplayRandomImage(List<string> imageNames)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"=== 显示随机图片 ===");
            Debug.Log($"loadingImage 组件是否为空: {loadingImage == null}");
            Debug.Log($"图片名称列表: {string.Join(", ", imageNames ?? new List<string>())}");
        }

        if (loadingImage == null)
        {
            Debug.LogWarning("加载图片组件未设置");
            return;
        }

        if (imageNames == null || imageNames.Count == 0)
        {
            Debug.LogWarning("没有可用的图片");
            return;
        }

        // 随机选择一张图片
        string randomImageName = imageNames[Random.Range(0, imageNames.Count)];
        
        if (enableDebugLogs)
        {
            Debug.Log($"随机选择的图片名称: {randomImageName}");
        }

        // 从Resources文件夹加载图片
        string imagePath = $"{imagesFolderPath}/{randomImageName}";
        
        if (enableDebugLogs)
        {
            Debug.Log($"完整图片路径: {imagePath}");
        }

        Sprite loadedSprite = Resources.Load<Sprite>(imagePath);

        if (loadedSprite != null)
        {
            loadingImage.sprite = loadedSprite;
            Debug.Log($"成功加载图片: {imagePath}");
            
            if (enableDebugLogs)
            {
                Debug.Log($"图片组件是否激活: {loadingImage.gameObject.activeInHierarchy}");
                Debug.Log($"图片尺寸: {loadedSprite.rect.width}x{loadedSprite.rect.height}");
            }
        }
        else
        {
            Debug.LogWarning($"无法加载图片: {imagePath}");
            
            if (enableDebugLogs)
            {
                // 尝试列出Resources文件夹中的所有资源
                Object[] allResources = Resources.LoadAll("", typeof(Sprite));
                Debug.Log($"Resources文件夹中的所有Sprite资源数量: {allResources.Length}");
                foreach (Object resource in allResources)
                {
                    Debug.Log($"找到资源: {resource.name}");
                }
            }
        }
    }

    // 刷新显示内容（重新随机选择）
    public void RefreshLoadingContent()
    {
        DisplayRandomLoadingContent();
    }

    // 获取所有可用的ID列表
    // 返回值: ID列表
    public List<int> GetAllAvailableIds()
    {
        if (loadingUIData?.LoadingData != null)
        {
            return loadingUIData.LoadingData.Select(item => item.id).ToList();
        }
        return new List<int>();
    }

    // 清空显示内容
    public void ClearLoadingContent()
    {
        if (loadingImage != null)
        {
            loadingImage.sprite = null;
        }
        if (nameText != null)
        {
            nameText.text = "";
        }
        if (descriptionText != null)
        {
            descriptionText.text = "";
        }
    }

    // 手动测试方法（在Inspector中可以调用）
    [ContextMenu("测试显示随机内容")]
    public void TestDisplayRandomContent()
    {
        DisplayRandomLoadingContent();
    }

    // 手动测试方法（测试组件设置）
    [ContextMenu("验证组件设置")]
    public void TestValidateComponents()
    {
        ValidateUIComponents();
    }
}