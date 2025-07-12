using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // 添加TMPro命名空间

public class MissionSection : MonoBehaviour, IPointerClickHandler
{
    [Header("任务条设置")]
    [SerializeField] private Image missionBarImage; // 任务条的Image组件

    [Header("任务内容显示组件")]
    [SerializeField] private TextMeshProUGUI missionNameText; // 任务名称文本组件（TMP）
    [SerializeField] private Image missionTypeIcon; // 任务类型图标组件

    [Header("精灵图片设置")]
    [SerializeField] private Sprite normalSprite; // 任务的原始精灵图片
    [SerializeField] private Sprite confirmedSprite; // 确认后的精灵图片

    [Header("状态设置")]
    private bool isConfirmed = false; // 任务是否已确认

    // 任务索引和管理器引用
    private int missionIndex = -1;
    private MissionManager missionManager;

    // 存储所有可点击的对象（包括主Image和所有子对象）
    private List<GameObject> clickableObjects = new List<GameObject>();

    private void Start()
    {
        // 检查必要组件是否设置
        if (missionBarImage == null)
        {
            Debug.LogError("MissionSection: 任务条Image未设置！请在Inspector中拖拽Image组件");
        }

        if (normalSprite == null)
        {
            Debug.LogError("MissionSection: 原始精灵图片未设置！请在Inspector中拖拽Sprite资源");
        }

        if (confirmedSprite == null)
        {
            Debug.LogError("MissionSection: 确认精灵图片未设置！请在Inspector中拖拽Sprite资源");
        }

        // 检查任务内容显示组件
        if (missionNameText == null)
        {
            Debug.LogWarning("MissionSection: 任务名称文本组件（TMP）未设置！请在Inspector中拖拽TextMeshProUGUI组件");
        }

        if (missionTypeIcon == null)
        {
            Debug.LogWarning("MissionSection: 任务类型图标组件未设置！请在Inspector中拖拽Image组件");
        }

        // 初始化为原始精灵图片
        if (missionBarImage != null && normalSprite != null)
        {
            missionBarImage.sprite = normalSprite;
        }

        // 收集所有可点击的对象
        CollectClickableObjects();

        // 初始化任务内容显示
        InitializeMissionDisplay();
    }

    // 初始化任务内容显示
    private void InitializeMissionDisplay()
    {
        // 如果有MissionManager引用且任务索引有效，则更新显示内容
        if (missionManager != null && missionIndex >= 0)
        {
            UpdateMissionDisplay();
        }
    }

    // 更新任务显示内容
    private void UpdateMissionDisplay()
    {
        if (missionManager == null || missionIndex < 0) return;

        // 从MissionManager获取任务数据
        MissionData missionData = missionManager.GetMissionData(missionIndex);
        if (missionData != null)
        {
            // 设置任务名称
            if (missionNameText != null)
            {
                missionNameText.text = missionData.name;
            }

            // 设置任务类型图标
            if (missionTypeIcon != null && !string.IsNullOrEmpty(missionData.iconPath))
            {
                LoadAndSetIcon(missionData.iconPath);
            }
        }
        else
        {
            // 如果没有找到任务数据，显示默认内容
            if (missionNameText != null)
            {
                missionNameText.text = $"任务 {missionIndex + 1}";
            }

            if (missionTypeIcon != null)
            {
                missionTypeIcon.gameObject.SetActive(false);
            }
        }
    }

    // 加载并设置任务类型图标
    private void LoadAndSetIcon(string iconPath)
    {
        if (missionTypeIcon == null || string.IsNullOrEmpty(iconPath))
        {
            return;
        }

        try
        {
            Sprite iconSprite = Resources.Load<Sprite>(iconPath);
            if (iconSprite != null)
            {
                missionTypeIcon.sprite = iconSprite;
                missionTypeIcon.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"MissionSection: 无法加载任务类型图标: {iconPath}");
                missionTypeIcon.gameObject.SetActive(false);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"MissionSection: 加载任务类型图标时发生错误: {e.Message}");
            missionTypeIcon.gameObject.SetActive(false);
        }
    }

    // 收集Image组件及其所有子对象
    private void CollectClickableObjects()
    {
        clickableObjects.Clear();

        if (missionBarImage != null)
        {
            // 添加主Image对象
            clickableObjects.Add(missionBarImage.gameObject);

            // 递归添加所有子对象
            CollectChildObjects(missionBarImage.transform);
        }

        Debug.Log($"MissionSection: 收集到 {clickableObjects.Count} 个可点击对象");
    }

    // 递归收集所有子对象
    private void CollectChildObjects(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            clickableObjects.Add(child.gameObject);

            // 递归处理子对象的子对象
            if (child.childCount > 0)
            {
                CollectChildObjects(child);
            }
        }
    }

    // 实现IPointerClickHandler接口，处理点击事件
    public void OnPointerClick(PointerEventData eventData)
    {
        // 检查点击的对象是否在可点击列表中
        GameObject clickedObject = eventData.pointerCurrentRaycast.gameObject;

        if (IsClickableObject(clickedObject))
        {
            // 通知MissionManager处理点击事件
            if (missionManager != null)
            {
                missionManager.OnMissionItemClicked(missionIndex);
            }
            else
            {
                Debug.LogWarning("MissionSection: MissionManager引用未设置，无法处理点击事件");
            }
        }
    }

    // 检查点击的对象是否为可点击对象
    private bool IsClickableObject(GameObject clickedObject)
    {
        // 直接检查是否在可点击列表中
        if (clickableObjects.Contains(clickedObject))
        {
            return true;
        }

        // 如果不在列表中，检查是否为missionBarImage的子对象
        // 这是为了处理动态添加的子对象
        if (missionBarImage != null)
        {
            Transform clickedTransform = clickedObject.transform;
            while (clickedTransform != null)
            {
                if (clickedTransform.gameObject == missionBarImage.gameObject)
                {
                    return true;
                }
                clickedTransform = clickedTransform.parent;
            }
        }

        return false;
    }

    // 设置任务索引（由MissionManager调用）
    public void SetMissionIndex(int index)
    {
        missionIndex = index;
        // 更新任务显示内容
        UpdateMissionDisplay();
    }

    // 设置MissionManager引用（由MissionManager调用）
    public void SetMissionManager(MissionManager manager)
    {
        missionManager = manager;
        // 更新任务显示内容
        UpdateMissionDisplay();
    }

    // 直接设置确认状态（由MissionManager调用，不触发事件）
    public void SetConfirmedStateDirectly(bool confirmed)
    {
        isConfirmed = confirmed;
        if (missionBarImage != null)
        {
            missionBarImage.sprite = isConfirmed ? confirmedSprite : normalSprite;
        }

        Debug.Log($"MissionSection {missionIndex}: 状态设置为 {(confirmed ? "确认" : "未确认")}");
    }

    // 任务确认时的回调
    protected virtual void OnMissionConfirmed()
    {
        // 在这里可以添加任务确认时的逻辑
        // 例如：播放音效、触发任务事件等
    }

    // 任务取消确认时的回调
    protected virtual void OnMissionUnconfirmed()
    {
        // 在这里可以添加取消确认时的逻辑
        // 例如：播放音效、重置任务状态等
    }

    // 公共方法：获取确认状态
    public bool IsConfirmed()
    {
        return isConfirmed;
    }

    // 公共方法：获取任务索引
    public int GetMissionIndex()
    {
        return missionIndex;
    }

    // 公共方法：设置任务条Image引用（运行时设置）
    public void SetMissionBarImage(Image image)
    {
        missionBarImage = image;
        // 重新收集可点击对象
        CollectClickableObjects();
    }

    // 公共方法：设置原始精灵图片（运行时设置）
    public void SetNormalSprite(Sprite sprite)
    {
        normalSprite = sprite;
        if (missionBarImage != null && !isConfirmed)
        {
            missionBarImage.sprite = normalSprite;
        }
    }

    // 公共方法：设置确认精灵图片（运行时设置）
    public void SetConfirmedSprite(Sprite sprite)
    {
        confirmedSprite = sprite;
        if (missionBarImage != null && isConfirmed)
        {
            missionBarImage.sprite = confirmedSprite;
        }
    }

    // 公共方法：刷新可点击对象列表（当子对象发生变化时调用）
    public void RefreshClickableObjects()
    {
        CollectClickableObjects();
    }

    // 公共方法：设置任务名称文本组件引用（运行时设置）
    public void SetMissionNameText(TextMeshProUGUI textComponent)
    {
        missionNameText = textComponent;
        UpdateMissionDisplay();
    }

    // 公共方法：设置任务类型图标组件引用（运行时设置）
    public void SetMissionTypeIcon(Image iconComponent)
    {
        missionTypeIcon = iconComponent;
        UpdateMissionDisplay();
    }

    // 公共方法：手动更新任务显示（当任务数据发生变化时调用）
    public void RefreshMissionDisplay()
    {
        UpdateMissionDisplay();
    }
}
