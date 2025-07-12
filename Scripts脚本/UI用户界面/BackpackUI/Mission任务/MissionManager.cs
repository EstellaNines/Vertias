using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // 添加TMPro命名空间
using System;

[System.Serializable]
public class MissionData
{
    public int id;                    // 任务唯一标识符
    public string name;               // 任务名称
    public string type;               // 任务类型："探索"、"战斗"、"交谈" 三种固定类型
    public string iconPath;           // 任务图标路径（Resources文件夹相对路径）
    public string legendPath;         // 任务图例路径（Resources文件夹相对路径）
    public string description;        // 任务详细描述
    public MissionReward reward;      // 任务奖励信息
    public string publisher;          // 任务发布者名称（简化为只有名称）
}

[System.Serializable]
public class MissionReward
{
    public int money;                 // 资金奖励数量
    public int weapon;                // 武器奖励数量
    public int food;                  // 食物奖励数量
    public int intelligence;          // 情报奖励数量
    public string moneyIconPath;      // 资金图标路径
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
    // ---------- 左侧任务栏设置 ----------
    [Header("任务选择设置")]
    [SerializeField] public int currentMissionCount = 0; // 当前任务数量（公开参数）
    [SerializeField] private GameObject missionItemPrefab; // 任务条预制体
    [SerializeField] private RawImage missionContainer; // 任务栏容器（RawImage组件）

    // ---------- 右侧任务描述设置 ----------
    [Header("任务描述显示设置")]
    [SerializeField] private GameObject missionDescriptionPanel; // 任务描述面板
    [SerializeField] private TextMeshProUGUI missionNameText; // 任务名称文本（TMP组件）
    [SerializeField] private Image missionIconImage; // 任务图标（用于代表任务类型）
    [SerializeField] private Image missionLegendImage; // 任务图例
    [SerializeField] private TextMeshProUGUI missionDescriptionText; // 任务描述文本（TMP组件）
    [SerializeField] private TextMeshProUGUI missionPublisherText; // 任务发布者文本（TMP组件）

    [Header("任务奖励显示设置")]
    [SerializeField] private TextMeshProUGUI moneyRewardText; // 资金奖励文本（TMP组件）
    [SerializeField] private TextMeshProUGUI weaponRewardText; // 武器奖励文本（TMP组件）
    [SerializeField] private TextMeshProUGUI foodRewardText; // 食物奖励文本（TMP组件）
    [SerializeField] private TextMeshProUGUI intelligenceRewardText; // 情报奖励文本（TMP组件）
    [SerializeField] private Image moneyRewardIcon; // 资金奖励图标
    [SerializeField] private Image weaponRewardIcon; // 武器奖励图标
    [SerializeField] private Image foodRewardIcon; // 食物奖励图标
    [SerializeField] private Image intelligenceRewardIcon; // 情报奖励图标

    [Header("任务数据设置")]
    [SerializeField] private string missionDataFileName = "MissionData"; // JSON文件名（不包含扩展名）

    // 任务数据
    private MissionDataCollection missionDataCollection;
    private Dictionary<int, MissionData> missionDataDict = new Dictionary<int, MissionData>();

    [Header("调试设置")]
    [SerializeField] private bool autoGenerateOnStart = true; // 是否在开始时自动生成任务条

    // 存储已生成的任务条实例
    private List<GameObject> generatedMissionItems = new List<GameObject>();

    // 任务栏的Vertical Layout Group组件
    private VerticalLayoutGroup layoutGroup;

    // 当前选中的任务条索引（-1表示没有选中）
    private int currentSelectedIndex = -1;

    // 任务类型常量
    public static readonly string[] MISSION_TYPES = { "explore", "combat", "talk" };

    void Start()
    {
        // 加载任务数据
        LoadMissionData();

        // 初始化组件引用
        InitializeComponents();

        // 初始化任务描述面板
        InitializeMissionDescriptionPanel();

        // 如果启用自动生成，则根据当前任务数量生成任务条
        if (autoGenerateOnStart)
        {
            GenerateMissionItems();
        }
    }

    // 加载任务数据从JSON文件
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

                // 将数据存储到字典中以便快速查找
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
                Debug.LogError($"MissionManager: 无法找到任务数据文件 {missionDataFileName}.json，请确保文件位于Resources文件夹中");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MissionManager: 加载任务数据时发生错误: {e.Message}");
        }
    }

    // 初始化任务描述面板
    private void InitializeMissionDescriptionPanel()
    {
        // 检查任务描述面板组件是否设置
        if (missionDescriptionPanel == null)
        {
            Debug.LogWarning("MissionManager: 任务描述面板未设置");
        }

        // 初始时隐藏任务描述面板
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(false);
        }

        // 检查各个UI组件
        CheckUIComponents();
    }

    // 检查UI组件是否正确设置
    private void CheckUIComponents()
    {
        if (missionNameText == null) Debug.LogWarning("MissionManager: 任务名称文本组件（TMP）未设置");
        // 移除任务类型文本组件检查 - 改为使用图标代表类型
        if (missionIconImage == null) Debug.LogWarning("MissionManager: 任务图标组件未设置（用于代表任务类型）");
        if (missionLegendImage == null) Debug.LogWarning("MissionManager: 任务图例组件未设置");
        if (missionDescriptionText == null) Debug.LogWarning("MissionManager: 任务描述文本组件（TMP）未设置");
        if (missionPublisherText == null) Debug.LogWarning("MissionManager: 任务发布者文本组件（TMP）未设置");

        // 检查奖励相关组件
        if (moneyRewardText == null) Debug.LogWarning("MissionManager: 资金奖励文本组件（TMP）未设置");
        if (weaponRewardText == null) Debug.LogWarning("MissionManager: 武器奖励文本组件（TMP）未设置");
        if (foodRewardText == null) Debug.LogWarning("MissionManager: 食物奖励文本组件（TMP）未设置");
        if (intelligenceRewardText == null) Debug.LogWarning("MissionManager: 情报奖励文本组件（TMP）未设置");
        if (moneyRewardIcon == null) Debug.LogWarning("MissionManager: 资金奖励图标组件未设置");
        if (weaponRewardIcon == null) Debug.LogWarning("MissionManager: 武器奖励图标组件未设置");
        if (foodRewardIcon == null) Debug.LogWarning("MissionManager: 食物奖励图标组件未设置");
        if (intelligenceRewardIcon == null) Debug.LogWarning("MissionManager: 情报奖励图标组件未设置");
    }

    // 验证任务类型是否有效
    public bool IsValidMissionType(string type)
    {
        return System.Array.Exists(MISSION_TYPES, t => t == type);
    }

    // 显示指定任务的描述信息
    public void ShowMissionDescription(int missionIndex)
    {
        // 检查任务数据是否存在
        if (!missionDataDict.ContainsKey(missionIndex))
        {
            Debug.LogWarning($"MissionManager: 找不到索引为 {missionIndex} 的任务数据");
            HideMissionDescription();
            return;
        }

        MissionData missionData = missionDataDict[missionIndex];

        // 显示任务描述面板
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(true);
        }

        // 更新各个UI组件的内容
        UpdateMissionUI(missionData);

        Debug.Log($"MissionManager: 显示任务 {missionIndex} 的描述信息");
    }

    // 更新任务UI显示
    private void UpdateMissionUI(MissionData missionData)
    {
        // 更新文本内容
        if (missionNameText != null) missionNameText.text = missionData.name;
        // 移除任务类型文本更新 - 改为使用图标代表类型
        if (missionDescriptionText != null) missionDescriptionText.text = missionData.description;
        if (missionPublisherText != null) missionPublisherText.text = missionData.publisher;

        // 加载并设置任务图标（用于代表任务类型）
        LoadAndSetSprite(missionData.iconPath, missionIconImage);

        // 加载并设置图例
        LoadAndSetSprite(missionData.legendPath, missionLegendImage);

        // 更新奖励显示
        UpdateRewardDisplay(missionData.reward);
    }

    // 更新奖励显示
    // 更新奖励显示
    private void UpdateRewardDisplay(MissionReward reward)
    {
        if (reward == null) return;

        // 更新奖励文本 - 使用"类型 x数量"的格式
        if (moneyRewardText != null)
            moneyRewardText.text = reward.money > 0 ? $"funds x{reward.money}" : "funds x0";
        if (weaponRewardText != null)
            weaponRewardText.text = reward.weapon > 0 ? $"weapon x{reward.weapon}" : "weapon x0";
        if (foodRewardText != null)
            foodRewardText.text = reward.food > 0 ? $"food x{reward.food}" : "food x0";
        if (intelligenceRewardText != null)
            intelligenceRewardText.text = reward.intelligence > 0 ? $"intelligence x{reward.intelligence}" : "intelligence x0";

        // 加载并设置奖励图标
        LoadAndSetSprite(reward.moneyIconPath, moneyRewardIcon);
        LoadAndSetSprite(reward.weaponIconPath, weaponRewardIcon);
        LoadAndSetSprite(reward.foodIconPath, foodRewardIcon);
        LoadAndSetSprite(reward.intelligenceIconPath, intelligenceRewardIcon);
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

    // 隐藏任务描述面板
    public void HideMissionDescription()
    {
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(false);
        }
        Debug.Log("MissionManager: 隐藏任务描述面板");
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

    // 重新加载任务数据（用于运行时更新）
    public void ReloadMissionData()
    {
        LoadMissionData();

        // 如果当前有选中的任务，重新显示其描述
        if (currentSelectedIndex != -1)
        {
            ShowMissionDescription(currentSelectedIndex);
        }
    }

    // 初始化组件引用
    private void InitializeComponents()
    {
        // 检查任务栏容器是否设置
        if (missionContainer == null)
        {
            Debug.LogError("MissionManager: 任务栏容器（RawImage）未设置！请在Inspector中拖拽RawImage组件");
            return;
        }

        // 获取Vertical Layout Group组件
        layoutGroup = missionContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.LogError("MissionManager: 任务栏容器上未找到Vertical Layout Group组件！");
        }

        // 检查任务条预制体是否设置
        if (missionItemPrefab == null)
        {
            Debug.LogError("MissionManager: 任务条预制体未设置！请在Inspector中拖拽预制体");
        }
    }

    // 根据任务数量生成任务条
    public void GenerateMissionItems()
    {
        if (missionContainer == null || missionItemPrefab == null)
        {
            Debug.LogWarning("MissionManager: 无法生成任务条，缺少必要组件");
            return;
        }

        // 清除现有的任务条
        ClearAllMissionItems();

        // 重置选中索引
        currentSelectedIndex = -1;

        // 根据任务数量生成新的任务条
        for (int i = 0; i < currentMissionCount; i++)
        {
            CreateMissionItem(i);
        }

        Debug.Log($"MissionManager: 已生成 {currentMissionCount} 个任务条");
    }

    // 创建单个任务条
    private void CreateMissionItem(int missionIndex)
    {
        // 实例化任务条预制体
        GameObject missionItem = Instantiate(missionItemPrefab, missionContainer.transform);

        // 设置任务条名称
        missionItem.name = $"MissionItem_{missionIndex}";

        // 添加到已生成列表
        generatedMissionItems.Add(missionItem);

        // 如果任务条有MissionSection组件，进行设置
        MissionSection missionSection = missionItem.GetComponent<MissionSection>();
        if (missionSection != null)
        {
            // 设置任务索引和管理器引用
            missionSection.SetMissionIndex(missionIndex);
            missionSection.SetMissionManager(this);

            Debug.Log($"MissionManager: 任务条 {missionIndex} 的MissionSection组件已配置");
        }

        Debug.Log($"MissionManager: 创建任务条 {missionIndex}");
    }

    // 任务条被点击时调用（由MissionSection调用）
    public void OnMissionItemClicked(int missionIndex)
    {
        // 检查任务数量是否大于0
        if (currentMissionCount <= 0)
        {
            Debug.LogWarning("MissionManager: 当前没有任务，无法选中");
            return;
        }

        // 检查索引是否有效
        if (missionIndex < 0 || missionIndex >= generatedMissionItems.Count)
        {
            Debug.LogWarning($"MissionManager: 无效的任务索引: {missionIndex}");
            return;
        }

        // 如果点击的是当前选中的任务条，则取消选中
        if (currentSelectedIndex == missionIndex)
        {
            DeselectCurrentMission();
            return;
        }

        // 取消之前选中的任务条
        if (currentSelectedIndex != -1)
        {
            SetMissionConfirmedState(currentSelectedIndex, false);
        }

        // 选中新的任务条
        SetMissionConfirmedState(missionIndex, true);
        currentSelectedIndex = missionIndex;

        // 显示选中任务的描述信息
        ShowMissionDescription(missionIndex);

        Debug.Log($"MissionManager: 选中任务条 {missionIndex}");
    }

    // 取消当前选中的任务
    public void DeselectCurrentMission()
    {
        if (currentSelectedIndex != -1)
        {
            SetMissionConfirmedState(currentSelectedIndex, false);
            currentSelectedIndex = -1;

            // 隐藏任务描述面板
            HideMissionDescription();

            Debug.Log("MissionManager: 取消选中当前任务");
        }
    }

    // 设置指定任务条的确认状态
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

    // 清除所有任务条
    public void ClearAllMissionItems()
    {
        // 重置选中索引
        currentSelectedIndex = -1;

        // 销毁所有已生成的任务条
        foreach (GameObject item in generatedMissionItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }

        // 清空列表
        generatedMissionItems.Clear();

        Debug.Log("MissionManager: 已清除所有任务条");
    }

    // 添加新任务（增加任务数量并生成对应任务条）
    public void AddMission()
    {
        currentMissionCount++;
        CreateMissionItem(currentMissionCount - 1);
        Debug.Log($"MissionManager: 添加新任务，当前任务数量: {currentMissionCount}");
    }

    // 移除任务（减少任务数量并移除对应任务条）
    public void RemoveMission()
    {
        if (currentMissionCount > 0 && generatedMissionItems.Count > 0)
        {
            // 如果要移除的是当前选中的任务条，重置选中状态
            if (currentSelectedIndex == currentMissionCount - 1)
            {
                currentSelectedIndex = -1;
            }
            else if (currentSelectedIndex > currentMissionCount - 1)
            {
                currentSelectedIndex = -1;
            }

            // 移除最后一个任务条
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

    // 设置任务数量（重新生成所有任务条）
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

    // 获取指定索引的任务条
    public GameObject GetMissionItem(int index)
    {
        if (index >= 0 && index < generatedMissionItems.Count)
        {
            return generatedMissionItems[index];
        }
        return null;
    }

    // 获取所有任务条
    public List<GameObject> GetAllMissionItems()
    {
        return new List<GameObject>(generatedMissionItems);
    }

    // 在Inspector中值改变时调用（仅在编辑器中有效）
    private void OnValidate()
    {
        // 确保任务数量不为负数
        if (currentMissionCount < 0)
        {
            currentMissionCount = 0;
        }
    }
}
