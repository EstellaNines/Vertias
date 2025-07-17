using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // ����TMPro�����ռ�
using System;

[System.Serializable]
public class MissionData
{
    public int id;                    // ����Ψһ��ʶ��
    public string name;               // ��������
    public string type;               // �������ͣ�"̽��"��"ս��"��"��̸" ���̶ֹ�����
    public string iconPath;           // ����ͼ��·����Resources�ļ������·����
    public string legendPath;         // ����ͼ��·����Resources�ļ������·����
    public string description;        // ������ϸ����
    public MissionReward reward;      // ��������Ϣ
    public string publisher;          // ���񷢲������ƣ���Ϊֻ�����ƣ�
}

[System.Serializable]
public class MissionReward
{
    public int money;                 // �ʽ�������
    public int weapon;                // ������������
    public int food;                  // ʳ�ｱ������
    public int intelligence;          // �鱨��������
    public string moneyIconPath;      // �ʽ�ͼ��·��
    public string weaponIconPath;     // ����ͼ��·��
    public string foodIconPath;       // ʳ��ͼ��·��
    public string intelligenceIconPath; // �鱨ͼ��·��
}

[System.Serializable]
public class MissionDataCollection
{
    public List<MissionData> missions; // ���������б�
}

public class MissionManager : MonoBehaviour
{
    // ---------- ������������� ----------
    [Header("����ѡ������")]
    [SerializeField] public int currentMissionCount = 0; // ��ǰ��������������������
    [SerializeField] private GameObject missionItemPrefab; // ������Ԥ����
    [SerializeField] private RawImage missionContainer; // ������������RawImage�����

    // ---------- �Ҳ������������� ----------
    [Header("����������ʾ����")]
    [SerializeField] private GameObject missionDescriptionPanel; // �����������
    [SerializeField] private TextMeshProUGUI missionNameText; // ���������ı���TMP�����
    [SerializeField] private Image missionIconImage; // ����ͼ�꣨���ڴ����������ͣ�
    [SerializeField] private Image missionLegendImage; // ����ͼ��
    [SerializeField] private TextMeshProUGUI missionDescriptionText; // ���������ı���TMP�����
    [SerializeField] private TextMeshProUGUI missionPublisherText; // ���񷢲����ı���TMP�����

    [Header("��������ʾ����")]
    [SerializeField] private RawImage rewardContainer; // ��������������Vertical Layout Group��
    [SerializeField] private GameObject rewardItemPrefab; // ͨ�ý�����Ԥ����

    [Header("������������")]
    [SerializeField] private string missionDataFileName = "MissionData"; // JSON�ļ�������������չ����

    // ��������
    private MissionDataCollection missionDataCollection;
    private Dictionary<int, MissionData> missionDataDict = new Dictionary<int, MissionData>();

    [Header("��������")]
    [SerializeField] private bool autoGenerateOnStart = true; // �Ƿ��ڿ�ʼʱ�Զ�����������

    // �洢�����ɵ�������ʵ��
    private List<GameObject> generatedMissionItems = new List<GameObject>();

    // �洢��̬���ɵĽ�����ʵ��
    private List<GameObject> generatedRewardItems = new List<GameObject>();

    // ��������Vertical Layout Group���
    private VerticalLayoutGroup layoutGroup;

    // ��ǰѡ�е�������������-1��ʾû��ѡ�У�
    private int currentSelectedIndex = -1;

    // �������ͳ���
    public static readonly string[] MISSION_TYPES = { "explore", "combat", "talk" };

    void Start()
    {
        // ������������
        LoadMissionData();

        // ��ʼ���������
        InitializeComponents();

        // ��ʼ�������������
        InitializeMissionDescriptionPanel();

        // ��������Զ����ɣ�����ݵ�ǰ������������������
        if (autoGenerateOnStart)
        {
            GenerateMissionItems();
        }
    }

    // �����������ݴ�JSON�ļ�
    private void LoadMissionData()
    {
        try
        {
            // ��Resources�ļ��м���JSON�ļ�
            TextAsset jsonFile = Resources.Load<TextAsset>(missionDataFileName);
            if (jsonFile != null)
            {
                string jsonContent = jsonFile.text;
                missionDataCollection = JsonUtility.FromJson<MissionDataCollection>(jsonContent);

                // �����ݴ洢���ֵ����Ա���ٲ���
                missionDataDict.Clear();
                if (missionDataCollection != null && missionDataCollection.missions != null)
                {
                    foreach (MissionData mission in missionDataCollection.missions)
                    {
                        missionDataDict[mission.id] = mission;
                    }
                    Debug.Log($"MissionManager: �ɹ����� {missionDataCollection.missions.Count} ����������");
                }
                else
                {
                    Debug.LogWarning("MissionManager: JSON�ļ���ʽ�����Ϊ��");
                }
            }
            else
            {
                Debug.LogError($"MissionManager: �޷��ҵ����������ļ� {missionDataFileName}.json����ȷ���ļ�λ��Resources�ļ�����");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MissionManager: ������������ʱ��������: {e.Message}");
        }
    }

    // ��ʼ�������������
    private void InitializeMissionDescriptionPanel()
    {
        // ������������������Ƿ�����
        if (missionDescriptionPanel == null)
        {
            Debug.LogWarning("MissionManager: �����������δ����");
        }

        // ��ʼʱ���������������
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(false);
        }

        // ������UI���
        CheckUIComponents();
    }

    // ���UI����Ƿ���ȷ����
    private void CheckUIComponents()
    {
        if (missionNameText == null) Debug.LogWarning("MissionManager: ���������ı������TMP��δ����");
        if (missionIconImage == null) Debug.LogWarning("MissionManager: ����ͼ�����δ���ã����ڴ����������ͣ�");
        if (missionLegendImage == null) Debug.LogWarning("MissionManager: ����ͼ�����δ����");
        if (missionDescriptionText == null) Debug.LogWarning("MissionManager: ���������ı������TMP��δ����");
        if (missionPublisherText == null) Debug.LogWarning("MissionManager: ���񷢲����ı������TMP��δ����");

        // ��齱��������
        if (rewardContainer == null) Debug.LogWarning("MissionManager: ����������RawImage��δ����");
        if (rewardItemPrefab == null) Debug.LogWarning("MissionManager: ������Ԥ����δ����");
    }

    // ��֤���������Ƿ���Ч
    public bool IsValidMissionType(string type)
    {
        return System.Array.Exists(MISSION_TYPES, t => t == type);
    }

    // ��ʾָ�������������Ϣ
    public void ShowMissionDescription(int missionIndex)
    {
        // ������������Ƿ����
        if (!missionDataDict.ContainsKey(missionIndex))
        {
            Debug.LogWarning($"MissionManager: �Ҳ�������Ϊ {missionIndex} ����������");
            HideMissionDescription();
            return;
        }

        MissionData missionData = missionDataDict[missionIndex];

        // ��ʾ�����������
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(true);
        }

        // ���¸���UI���������
        UpdateMissionUI(missionData);

        Debug.Log($"MissionManager: ��ʾ���� {missionIndex} ��������Ϣ");
    }

    // ��������UI��ʾ
    private void UpdateMissionUI(MissionData missionData)
    {
        // �����ı�����
        if (missionNameText != null) missionNameText.text = missionData.name;
        if (missionDescriptionText != null) missionDescriptionText.text = missionData.description;
        if (missionPublisherText != null) missionPublisherText.text = missionData.publisher;

        // ���ز���������ͼ�꣨���ڴ����������ͣ�
        LoadAndSetSprite(missionData.iconPath, missionIconImage);

        // ���ز�����ͼ��
        LoadAndSetSprite(missionData.legendPath, missionLegendImage);

        // ���½�����ʾ
        UpdateRewardDisplay(missionData.reward);
    }

    // ���½�����ʾ - ʹ��GridLayoutGroupʵ�����в���
    private void UpdateRewardDisplay(MissionReward reward)
    {
        if (reward == null)
        {
            Debug.LogWarning("MissionManager: ��������Ϊ��");
            return;
        }

        // ���֮ǰ���ɵĽ�����
        ClearRewardItems();
        Debug.Log($"MissionManager: ��ʼ���½�����ʾ - Money:{reward.money}, Weapon:{reward.weapon}, Food:{reward.food}, Intelligence:{reward.intelligence}");

        // ���影����������
        var rewardTypes = new[]
        {
            new { name = "Funds", amount = reward.money, iconPath = reward.moneyIconPath },
            new { name = "Random Weapon", amount = reward.weapon, iconPath = reward.weaponIconPath },
            new { name = "Food", amount = reward.food, iconPath = reward.foodIconPath },
            new { name = "Intelligence", amount = reward.intelligence, iconPath = reward.intelligenceIconPath }
        };

        // ֻΪ��������0�Ľ������ʹ���UI��
        int createdCount = 0;
        foreach (var rewardType in rewardTypes)
        {
            if (rewardType.amount > 0)
            {
                Debug.Log($"MissionManager: ���������� - {rewardType.name}, ����: {rewardType.amount}");
                CreateRewardItem(rewardType.name, rewardType.amount, rewardType.iconPath);
                createdCount++;
            }
        }

        Debug.Log($"MissionManager: ������ʾ������ɣ������� {createdCount} ��������");
    }

    // ��������������
    private void CreateRewardItem(string itemName, int num, string iconPath)
    {
        if (rewardItemPrefab == null || rewardContainer == null)
        {
            Debug.LogWarning($"MissionManager: �޷�����{itemName}�����ȱ��Ԥ���������");
            return;
        }

        // ʵ��������Ԥ����
        GameObject rewardItem = Instantiate(rewardItemPrefab, rewardContainer.transform);
        generatedRewardItems.Add(rewardItem);

        // ���ý���������
        rewardItem.name = $"{itemName}RewardItem";

        // ����Ԥ�����е�����TextMeshProUGUI���
        TextMeshProUGUI[] textComponents = rewardItem.GetComponentsInChildren<TextMeshProUGUI>();

        foreach (TextMeshProUGUI textComponent in textComponents)
        {
            // ��������������ö�Ӧ���ı�����
            if (textComponent.name == "ItemName")
            {
                textComponent.text = itemName; // ������Ʒ���ƣ���JSON��ȡ��
            }
            else if (textComponent.name == "num")
            {
                textComponent.text = num.ToString(); // ������������JSON��ȡ��
            }
        }

        // ���Ҳ����ý���ͼ��
        Image rewardIcon = rewardItem.GetComponentInChildren<Image>();
        if (rewardIcon != null && !string.IsNullOrEmpty(iconPath))
        {
            LoadAndSetSprite(iconPath, rewardIcon);
        }
        else if (rewardIcon == null)
        {
            Debug.LogWarning($"MissionManager: ��{itemName}����Ԥ������δ�ҵ�Image���");
        }

        Debug.Log($"MissionManager: ����{itemName}���������: {num}������JSON�ļ���");
    }

    // ���ز����þ���ͼƬ
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
                Debug.LogWarning($"MissionManager: �޷����ؾ���ͼƬ: {spritePath}");
                targetImage.gameObject.SetActive(false);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MissionManager: ���ؾ���ͼƬʱ��������: {e.Message}");
            targetImage.gameObject.SetActive(false);
        }
    }

    // ������ж�̬���ɵĽ�����
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

    // ���������������
    public void HideMissionDescription()
    {
        if (missionDescriptionPanel != null)
        {
            missionDescriptionPanel.SetActive(false);
        }

        // �������ʱҲ���������
        ClearRewardItems();

        Debug.Log("MissionManager: ���������������");
    }

    // ��ȡָ����������������
    public MissionData GetMissionData(int missionIndex)
    {
        if (missionDataDict.ContainsKey(missionIndex))
        {
            return missionDataDict[missionIndex];
        }
        return null;
    }

    // ���¼����������ݣ���������ʱ���£�
    public void ReloadMissionData()
    {
        LoadMissionData();

        // �����ǰ��ѡ�е�����������ʾ������
        if (currentSelectedIndex != -1)
        {
            ShowMissionDescription(currentSelectedIndex);
        }
    }

    // ��ʼ���������
    private void InitializeComponents()
    {
        // ��������������Ƿ�����
        if (missionContainer == null)
        {
            Debug.LogError("MissionManager: ������������RawImage��δ���ã�����Inspector����קRawImage���");
            return;
        }

        // ��ȡVertical Layout Group���
        layoutGroup = missionContainer.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            Debug.LogError("MissionManager: ������������δ�ҵ�Vertical Layout Group�����");
        }

        // ���������Ԥ�����Ƿ�����
        if (missionItemPrefab == null)
        {
            Debug.LogError("MissionManager: ������Ԥ����δ���ã�����Inspector����קԤ����");
        }
    }

    // ����������������������
    public void GenerateMissionItems()
    {
        if (missionContainer == null || missionItemPrefab == null)
        {
            Debug.LogWarning("MissionManager: �޷�������������ȱ�ٱ�Ҫ���");
            return;
        }

        // ������е�������
        ClearAllMissionItems();

        // ����ѡ������
        currentSelectedIndex = -1;

        // �����������������µ�������
        for (int i = 0; i < currentMissionCount; i++)
        {
            CreateMissionItem(i);
        }

        Debug.Log($"MissionManager: ������ {currentMissionCount} ��������");
    }

    // ��������������
    private void CreateMissionItem(int missionIndex)
    {
        // ʵ����������Ԥ����
        GameObject missionItem = Instantiate(missionItemPrefab, missionContainer.transform);

        // ��������������
        missionItem.name = $"MissionItem_{missionIndex}";

        // ���ӵ��������б�
        generatedMissionItems.Add(missionItem);

        // �����������MissionSection�������������
        MissionSection missionSection = missionItem.GetComponent<MissionSection>();
        if (missionSection != null)
        {
            // �������������͹���������
            missionSection.SetMissionIndex(missionIndex);
            missionSection.SetMissionManager(this);

            Debug.Log($"MissionManager: ������ {missionIndex} ��MissionSection���������");
        }

        Debug.Log($"MissionManager: ���������� {missionIndex}");
    }

    // �����������ʱ���ã���MissionSection���ã�
    public void OnMissionItemClicked(int missionIndex)
    {
        // ������������Ƿ����0
        if (currentMissionCount <= 0)
        {
            Debug.LogWarning("MissionManager: ��ǰû�������޷�ѡ��");
            return;
        }

        // ��������Ƿ���Ч
        if (missionIndex < 0 || missionIndex >= generatedMissionItems.Count)
        {
            Debug.LogWarning($"MissionManager: ��Ч����������: {missionIndex}");
            return;
        }

        // ���������ǵ�ǰѡ�е�����������ȡ��ѡ��
        if (currentSelectedIndex == missionIndex)
        {
            DeselectCurrentMission();
            return;
        }

        // ȡ��֮ǰѡ�е�������
        if (currentSelectedIndex != -1)
        {
            SetMissionConfirmedState(currentSelectedIndex, false);
        }

        // ѡ���µ�������
        SetMissionConfirmedState(missionIndex, true);
        currentSelectedIndex = missionIndex;

        // ��ʾѡ�������������Ϣ
        ShowMissionDescription(missionIndex);

        Debug.Log($"MissionManager: ѡ�������� {missionIndex}");
    }

    // ȡ����ǰѡ�е�����
    public void DeselectCurrentMission()
    {
        if (currentSelectedIndex != -1)
        {
            SetMissionConfirmedState(currentSelectedIndex, false);
            currentSelectedIndex = -1;

            // ���������������
            HideMissionDescription();

            Debug.Log("MissionManager: ȡ��ѡ�е�ǰ����");
        }
    }

    // ����ָ����������ȷ��״̬
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

    // ��ȡ��ǰѡ�е���������
    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }

    // �������������
    public void ClearAllMissionItems()
    {
        // ����ѡ������
        currentSelectedIndex = -1;

        // �������������ɵ�������
        foreach (GameObject item in generatedMissionItems)
        {
            if (item != null)
            {
                DestroyImmediate(item);
            }
        }

        // ����б�
        generatedMissionItems.Clear();

        Debug.Log("MissionManager: ���������������");
    }

    // ���������������������������ɶ�Ӧ��������
    public void AddMission()
    {
        currentMissionCount++;
        CreateMissionItem(currentMissionCount - 1);
        Debug.Log($"MissionManager: ���������񣬵�ǰ��������: {currentMissionCount}");
    }

    // �Ƴ����񣨼��������������Ƴ���Ӧ��������
    public void RemoveMission()
    {
        if (currentMissionCount > 0 && generatedMissionItems.Count > 0)
        {
            // ���Ҫ�Ƴ����ǵ�ǰѡ�е�������������ѡ��״̬
            if (currentSelectedIndex == currentMissionCount - 1)
            {
                currentSelectedIndex = -1;
            }
            else if (currentSelectedIndex > currentMissionCount - 1)
            {
                currentSelectedIndex = -1;
            }

            // �Ƴ����һ��������
            GameObject lastItem = generatedMissionItems[generatedMissionItems.Count - 1];
            generatedMissionItems.RemoveAt(generatedMissionItems.Count - 1);

            if (lastItem != null)
            {
                DestroyImmediate(lastItem);
            }

            currentMissionCount--;
            Debug.Log($"MissionManager: �Ƴ����񣬵�ǰ��������: {currentMissionCount}");
        }
    }

    // ����������������������������������
    public void SetMissionCount(int count)
    {
        if (count < 0)
        {
            Debug.LogWarning("MissionManager: ������������Ϊ����");
            return;
        }

        currentMissionCount = count;
        GenerateMissionItems();
    }

    // ��ȡ��ǰ��������
    public int GetMissionCount()
    {
        return currentMissionCount;
    }

    // ��ȡָ��������������
    public GameObject GetMissionItem(int index)
    {
        if (index >= 0 && index < generatedMissionItems.Count)
        {
            return generatedMissionItems[index];
        }
        return null;
    }

    // ��ȡ����������
    public List<GameObject> GetAllMissionItems()
    {
        return new List<GameObject>(generatedMissionItems);
    }

    // ��Inspector��ֵ�ı�ʱ���ã����ڱ༭������Ч��
    private void OnValidate()
    {
        // ȷ������������Ϊ����
        if (currentMissionCount < 0)
        {
            currentMissionCount = 0;
        }
    }
}
