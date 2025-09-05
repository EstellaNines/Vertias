using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 引用TMPro命名空间
using System;

[System.Serializable]
public class MissionData
{
    public int id;                    // 任务唯一标识符
    public string name;               // 任务名称
    public string type;               // 任务类型："explore"、"combat"、"talk" 等类型
    public string category;           // 任务分类："main"（主线）、"side"（支线）、"daily"（日常）
    public string iconPath;           // 任务图标路径（相对于Resources文件夹的路径）
    public string legendPath;         // 任务图例路径（相对于Resources文件夹的路径）
    public string description;        // 任务描述信息
    public MissionReward reward;      // 任务奖励数据
    public string publisher;          // 任务发布者（例如："政府"、"商人"等）
}

[System.Serializable]
public class MissionReward
{
    public int money;                 // 金钱奖励数量
    public int weapon;                // 武器奖励数量
    public int food;                  // 食物奖励数量
    public int intelligence;          // 情报奖励数量
    public string moneyIconPath;      // 金钱图标路径
    public string weaponIconPath;     // 武器图标路径
    public string foodIconPath;       // 食物图标路径
    public string intelligenceIconPath; // 情报图标路径
}

[System.Serializable]
public class MissionDataCollection
{
    public List<MissionData> missions; // 任务数据列表
}

public class MissionManager : MonoBehaviour
{
    // ---------- 任务生成相关设置 ----------
    [Header("任务生成设置")]
    [SerializeField] public int currentMissionCount = 0; // 当前任务数量（可在Inspector中调整）
    [SerializeField] private GameObject missionItemPrefab; // 任务项预制体
    [SerializeField] private RawImage missionContainer; // 任务容器（带有RawImage组件）

    // ---------- 任务详情显示相关 ----------
    [Header("任务详情显示设置")]
    [SerializeField] private GameObject missionDescriptionPanel; // 任务详情面板
    [SerializeField] private TextMeshProUGUI missionNameText; // 任务名称文本（TMP组件）
    [SerializeField] private Image missionIconImage; // 任务图标显示（Image组件）
    [SerializeField] private Image missionLegendImage; // 任务图例图标
    [SerializeField] private TextMeshProUGUI missionDescriptionText; // 任务描述文本（TMP组件）
    [SerializeField] private TextMeshProUGUI missionPublisherText; // 任务发布者文本（TMP组件）

    [Header("奖励显示设置")]
    [SerializeField] private RawImage rewardContainer; // 奖励容器（需要有Vertical Layout Group组件）
    [SerializeField] private GameObject rewardItemPrefab; // 单个奖励项预制体

    [Header("数据文件设置")]
    [SerializeField] private string missionDataFileName = "MissionData"; // JSON文件名（不包含扩展名）

    // 内部数据存储
    private MissionDataCollection missionDataCollection;
    private Dictionary<int, MissionData> missionDataDict = new Dictionary<int, MissionData>();

    [Header("自动生成设置")]
    [SerializeField] private bool autoGenerateOnStart = true; // 是否在Start时自动生成任务项

    // 存储生成的任务项对象
    private List<GameObject> generatedMissionItems = new List<GameObject>();

    // 存储生成的奖励项对象
    private List<GameObject> generatedRewardItems = new List<GameObject>();

    // 布局组件Vertical Layout Group引用
    private VerticalLayoutGroup layoutGroup;

    // 当前选中的任务索引（-1表示没有选中）
    private int currentSelectedIndex = -1;

    // 任务类型常量定义
    public static readonly string[] MISSION_TYPES = { "explore", "combat", "talk" };
    
    // 任务分类常量定义
    public static readonly string[] MISSION_CATEGORIES = { "main", "side", "daily" };

    void Start()
    {
        // 加载任务数据
        LoadMissionData();

        // 初始化组件引用
        InitializeComponents();

        // 初始化任务详情面板
        InitializeMissionDescriptionPanel();

        // 如果启用了自动生成，则在开始时生成任务项
        if (autoGenerateOnStart)
        {
            GenerateMissionItems();
        }
    }

    // 从Resources文件夹加载JSON数据
    private void LoadMissionData()
    {
        try
        {
            // 从Resources文件夹加载JSON文件
            TextAsset jsonFile = Resources.Load<TextAsset>(missionDataFileName);
            if (jsonFile != null)
            {
                string jsonContent = jsonFile.text;
                missionDataCollection = JsonUtility.FromJson<MissionDataCollection>(jsonContent);

                // 构建字典以便快速查找
                missionDataDict.Clear();
                if (missionDataCollection != null && missionDataCollection.missions != null)
                {
                    foreach (MissionData mission in missionDataCollection.missions)
                    {
                        missionDataDict[mission.id] = mission;
                    }
                    Debug.Log($"MissionManager: 成功加载 {missionDataCollection.missions.Count} 个任务数据");
                }
                else
                {
                    Debug.LogWarning("MissionManager: JSON文件格式错误或为空");
                }
            }
            else
            {
                Debug.LogError($"MissionManager: 无法找到文件 {missionDataFileName}.json，请确保文件位于Resources文件夹中");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MissionManager: 加载任务数据时发生错误: {e.Message}");
        }
    }

    // 初始化任务详情面板
    private void InitializeMissionDescriptionPanel()
    {
        // 确保任务详情面板在开始时是隐藏的
        if (missionDescriptionPanel == null)
        {
            Debug.LogWarning("MissionManager: 任务详情面板未分配");
        }

        // 初始时隐藏任务详情面板
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(false);
        }

        // 检查UI组件
        CheckUIComponents();
    }

    // 检查UI组件是否正确分配
    private void CheckUIComponents()
    {
        if (missionNameText == null) Debug.LogWarning("MissionManager: 任务名称文本（TMP组件）未分配");
        if (missionIconImage == null) Debug.LogWarning("MissionManager: 任务图标显示（Image组件）未分配");
        if (missionLegendImage == null) Debug.LogWarning("MissionManager: 任务图例图标未分配");
        if (missionDescriptionText == null) Debug.LogWarning("MissionManager: 任务描述文本（TMP组件）未分配");
        if (missionPublisherText == null) Debug.LogWarning("MissionManager: 任务发布者文本（TMP组件）未分配");

        // 检查奖励相关组件
        if (rewardContainer == null) Debug.LogWarning("MissionManager: 奖励容器（RawImage组件）未分配");
        if (rewardItemPrefab == null) Debug.LogWarning("MissionManager: 奖励项预制体未分配");
    }

    // 验证任务类型是否有效
    public bool IsValidMissionType(string type)
    {
        return System.Array.Exists(MISSION_TYPES, t => t == type);
    }
    
    // 验证任务分类是否有效
    public bool IsValidMissionCategory(string category)
    {
        return System.Array.Exists(MISSION_CATEGORIES, c => c == category);
    }

    // 显示指定任务的详细信息
    public void ShowMissionDescription(int missionIndex)
    {
        // 检查任务索引是否有效
        if (!missionDataDict.ContainsKey(missionIndex))
        {
            Debug.LogWarning($"MissionManager: 任务索引 {missionIndex} 不存在");
            HideMissionDescription();
            return;
        }

        MissionData missionData = missionDataDict[missionIndex];

        // 显示任务详情面板
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(true);
        }

        // 更新UI显示内容
        UpdateMissionUI(missionData);

        Debug.Log($"MissionManager: 显示任务 {missionIndex} 的详细信息");
    }

    // 更新任务UI显示
    private void UpdateMissionUI(MissionData missionData)
    {
        // 更新文本内容
        if (missionNameText != null) missionNameText.text = missionData.name;
        if (missionPublisherText != null) missionPublisherText.text = missionData.publisher;

        // 显示任务描述
        if (missionDescriptionText != null)
        {
            missionDescriptionText.text = missionData.description;
        }

        // 加载并设置任务图标（Image组件）
        LoadAndSetSprite(missionData.iconPath, missionIconImage);

        // 加载并设置任务图例
        LoadAndSetSprite(missionData.legendPath, missionLegendImage);

        // 更新奖励显示
        UpdateRewardDisplay(missionData.reward);
    }

    // 更新奖励显示 - 使用GridLayoutGroup布局
    private void UpdateRewardDisplay(MissionReward reward)
    {
        if (reward == null)
        {
            Debug.LogWarning("MissionManager: 奖励数据为空");
            return;
        }

        // 清除之前的奖励项
        ClearRewardItems();
        Debug.Log($"MissionManager: 开始显示奖励 - Money:{reward.money}, Weapon:{reward.weapon}, Food:{reward.food}, Intelligence:{reward.intelligence}");

        // 定义奖励类型数组
        var rewardTypes = new[]
        {
            new { name = "Funds", amount = reward.money, iconPath = reward.moneyIconPath },
            new { name = "Random Weapon", amount = reward.weapon, iconPath = reward.weaponIconPath },
            new { name = "Food", amount = reward.food, iconPath = reward.foodIconPath },
            new { name = "Intelligence", amount = reward.intelligence, iconPath = reward.intelligenceIconPath }
        };

        // 只为数量大于0的奖励创建UI元素
        int createdCount = 0;
        foreach (var rewardType in rewardTypes)
        {
            if (rewardType.amount > 0)
            {
                Debug.Log($"MissionManager: 创建奖励项 - {rewardType.name}, 数量: {rewardType.amount}");
                CreateRewardItem(rewardType.name, rewardType.amount, rewardType.iconPath);
                createdCount++;
            }
        }

        Debug.Log($"MissionManager: 奖励显示完成，共创建 {createdCount} 个奖励项");
    }

    // 创建单个奖励项UI
    private void CreateRewardItem(string itemName, int num, string iconPath)
    {
        if (rewardItemPrefab == null || rewardContainer == null)
        {
            Debug.LogWarning($"MissionManager: 无法创建{itemName}奖励项，缺少必要组件");
            return;
        }

        // 实例化奖励项预制体
        GameObject rewardItem = Instantiate(rewardItemPrefab, rewardContainer.transform);
        generatedRewardItems.Add(rewardItem);

        // 设置奖励项名称
        rewardItem.name = $"{itemName}RewardItem";

        // 查找并设置TextMeshProUGUI组件
        TextMeshProUGUI[] textComponents = rewardItem.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            // 根据组件名称设置对应的文本内容
            if (textComponent.name == "ItemName")
            {
                textComponent.text = itemName; // 设置奖励名称（来自JSON数据）
            }
            else if (textComponent.name == "num")
            {
                textComponent.text = num.ToString(); // 设置奖励数量（来自JSON数据）
            }
        }

        // 设置奖励图标
        Image rewardIcon = rewardItem.GetComponentInChildren<Image>();
        if (rewardIcon != null && !string.IsNullOrEmpty(iconPath))
        {
            LoadAndSetSprite(iconPath, rewardIcon);
        }
        else if (rewardIcon == null)
        {
            Debug.LogWarning($"MissionManager: {itemName}奖励项中未找到Image组件");
        }

        Debug.Log($"MissionManager: 成功创建{itemName}奖励项，数量: {num}（来自JSON数据）");
    }

    // 加载并设置精灵图片
    private void LoadAndSetSprite(string spritePath, Image targetImage)
    {
        if (targetImage == null || string.IsNullOrEmpty(spritePath))
        {
            return;
        }

        try
        {
            Sprite sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                targetImage.sprite = sprite;
                targetImage.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"MissionManager: 无法加载精灵图片: {spritePath}");
                targetImage.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MissionManager: 加载精灵图片时发生错误: {e.Message}");
            targetImage.gameObject.SetActive(false);
        }
    }

    // 清除所有奖励项
    private void ClearRewardItems()
    {
        foreach (GameObject item in generatedRewardItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }
        generatedRewardItems.Clear();
    }

    // 隐藏任务详情面板
    public void HideMissionDescription()
    {
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(false);
        }

        // 同时清除奖励显示
        ClearRewardItems();

        Debug.Log("MissionManager: 隐藏任务详情面板");
    }

    // 获取指定索引的任务数据
    public MissionData GetMissionData(int missionIndex)
    {
        if (missionDataDict.ContainsKey(missionIndex))
        {
            return missionDataDict[missionIndex];
        }
        return null;
    }
    
    // 根据分类获取任务列表
    public List<MissionData> GetMissionsByCategory(string category)
    {
        List<MissionData> categoryMissions = new List<MissionData>();
        if (missionDataCollection != null && missionDataCollection.missions != null)
        {
            foreach (MissionData mission in missionDataCollection.missions)
            {
                if (mission.category == category)
                {
                    categoryMissions.Add(mission);
                }
            }
        }
        return categoryMissions;
    }
    
    // 获取主线任务
    public List<MissionData> GetMainMissions()
    {
        return GetMissionsByCategory("main");
    }
    
    // 获取支线任务
    public List<MissionData> GetSideMissions()
    {
        return GetMissionsByCategory("side");
    }
    
    // 获取日常任务
    public List<MissionData> GetDailyMissions()
    {
        return GetMissionsByCategory("daily");
    }

    // 重新加载任务数据（用于运行时更新数据）
    public void ReloadMissionData()
    {
        LoadMissionData();

        // 如果当前有选中的任务，重新显示其详情
        if (currentSelectedIndex != -1)
        {
            ShowMissionDescription(currentSelectedIndex);
        }
    }

    // 初始化组件引用
    private void InitializeComponents()
    {
        // 检查任务容器是否正确分配
        if (missionContainer == null)
        {
            Debug.LogError("MissionManager: 任务容器（RawImage组件）未分配，请在Inspector中分配RawImage组件");
            return;
        }

        // 获取Vertical Layout Group组件
        layoutGroup = missionContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.LogError("MissionManager: 任务容器未找到Vertical Layout Group组件");
        }

        // 检查任务项预制体是否分配
        if (missionItemPrefab == null)
        {
            Debug.LogError("MissionManager: 任务项预制体未分配，请在Inspector中分配预制体");
        }
    }

    // 生成指定数量的任务项
    public void GenerateMissionItems()
    {
        if (missionContainer == null || missionItemPrefab == null)
        {
            Debug.LogWarning("MissionManager: 无法生成任务项，缺少必要组件");
            return;
        }

        // 清除现有的任务项
        ClearAllMissionItems();

        // 重置选中索引
        currentSelectedIndex = -1;

        // 根据当前任务数量生成任务项
        for (int i = 0; i < currentMissionCount; i++)
        {
            CreateMissionItem(i);
        }

        Debug.Log($"MissionManager: 生成了 {currentMissionCount} 个任务项");
    }

    // 创建单个任务项
    private void CreateMissionItem(int missionIndex)
    {
        // 实例化任务项预制体
        GameObject missionItem = Instantiate(missionItemPrefab, missionContainer.transform);

        // 设置任务项名称
        missionItem.name = $"MissionItem_{missionIndex}";

        // 添加到生成列表
        generatedMissionItems.Add(missionItem);

        // 获取并配置MissionSection组件
        MissionSection missionSection = missionItem.GetComponent<MissionSection>();
        if (missionSection != null)
        {
            // 设置任务索引和管理器引用
            missionSection.SetMissionIndex(missionIndex);
            missionSection.SetMissionManager(this);

            Debug.Log($"MissionManager: 为任务 {missionIndex} 配置MissionSection组件");
        }

        Debug.Log($"MissionManager: 创建任务项 {missionIndex}");
    }

    // 处理任务项点击事件（由MissionSection调用）
    public void OnMissionItemClicked(int missionIndex)
    {
        // 检查当前任务数量是否大于0
        if (currentMissionCount <= 0)
        {
            Debug.LogWarning("MissionManager: 当前没有可用任务");
            return;
        }

        // 检查任务索引是否有效
        if (missionIndex < 0 || missionIndex >= generatedMissionItems.Count)
        {
            Debug.LogWarning($"MissionManager: 无效的任务索引: {missionIndex}");
            return;
        }

        // 如果点击的是当前选中的任务，则取消选择
        if (currentSelectedIndex == missionIndex)
        {
            DeselectCurrentMission();
            return;
        }

        // 取消之前选中的任务
        if (currentSelectedIndex != -1)
        {
            SetMissionConfirmedState(currentSelectedIndex, false);
        }

        // 选中新的任务
        SetMissionConfirmedState(missionIndex, true);
        currentSelectedIndex = missionIndex;

        // 显示任务详细信息
        ShowMissionDescription(missionIndex);

        Debug.Log($"MissionManager: 选中任务 {missionIndex}");
    }

    // 取消当前选中的任务
    public void DeselectCurrentMission()
    {
        if (currentSelectedIndex != -1)
        {
            SetMissionConfirmedState(currentSelectedIndex, false);
            currentSelectedIndex = -1;

            // 隐藏任务详情面板
            HideMissionDescription();

            Debug.Log("MissionManager: 取消选中任务");
        }
    }

    // 设置任务的确认状态（选中/未选中）
    private void SetMissionConfirmedState(int missionIndex, bool confirmed)
    {
        if (missionIndex >= 0 && missionIndex < generatedMissionItems.Count)
        {
            GameObject missionItem = generatedMissionItems[missionIndex];
            if (missionItem != null)
            {
                MissionSection missionSection = missionItem.GetComponent<MissionSection>();
                if (missionSection != null)
                {
                    missionSection.SetConfirmedStateDirectly(confirmed);
                }
            }
        }
    }

    // 获取当前选中的任务索引
    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }

    // 清除所有任务项
    public void ClearAllMissionItems()
    {
        // 重置选中索引
        currentSelectedIndex = -1;

        // 销毁所有生成的任务项
        foreach (GameObject item in generatedMissionItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }

        // 清空列表
        generatedMissionItems.Clear();

        Debug.Log("MissionManager: 清除所有任务项");
    }

    // 添加一个新任务项（运行时动态添加）
    public void AddMission()
    {
        currentMissionCount++;
        CreateMissionItem(currentMissionCount - 1);
        Debug.Log($"MissionManager: 添加任务，当前任务数量: {currentMissionCount}");
    }

    // 移除最后一个任务项（运行时动态移除）
    public void RemoveMission()
    {
        if (currentMissionCount > 0 && generatedMissionItems.Count > 0)
        {
            // 如果移除的是当前选中的任务，则重置选中状态
            if (currentSelectedIndex == currentMissionCount - 1)
            {
                currentSelectedIndex = -1;
            }
            else if (currentSelectedIndex > currentMissionCount - 1)
            {
                currentSelectedIndex = -1;
            }

            // 移除最后一个任务项
            GameObject lastItem = generatedMissionItems[generatedMissionItems.Count - 1];
            generatedMissionItems.RemoveAt(generatedMissionItems.Count - 1);

            if (lastItem != null)
            {
                DestroyImmediate(lastItem);
            }

            currentMissionCount--;
            Debug.Log($"MissionManager: 移除任务，当前任务数量: {currentMissionCount}");
        }
    }

    // 设置任务数量（会重新生成所有任务项）
    public void SetMissionCount(int count)
    {
        if (count < 0)
        {
            Debug.LogWarning("MissionManager: 任务数量不能为负数");
            return;
        }

        currentMissionCount = count;
        GenerateMissionItems();
    }

    // 获取当前任务数量
    public int GetMissionCount()
    {
        return currentMissionCount;
    }

    // 获取指定索引的任务项GameObject
    public GameObject GetMissionItem(int index)
    {
        if (index >= 0 && index < generatedMissionItems.Count)
        {
            return generatedMissionItems[index];
        }
        return null;
    }

    // 获取所有任务项的副本
    public List<GameObject> GetAllMissionItems()
    {
        return new List<GameObject>(generatedMissionItems);
    }

    // Inspector中值改变时的验证方法
    private void OnValidate()
    {
        // 确保任务数量不为负数
        if (currentMissionCount < 0)
        {
            currentMissionCount = 0;
        }
    }
}