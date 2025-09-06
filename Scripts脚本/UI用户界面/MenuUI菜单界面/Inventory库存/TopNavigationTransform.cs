using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TopNavigationTransform : MonoBehaviour
{
    [Header("Image组件设置")]
    [Header("Sprite精灵设置")]
    [SerializeField][FieldLabel("导航栏图片组件数组")] private Image[] navigationImages; // 导航栏Image组件
    [SerializeField][FieldLabel("正常状态精灵数组")] private Sprite[] normalSprites; // 正常状态Sprite
    [SerializeField][FieldLabel("点击状态精灵数组")] private Sprite[] clickedSprites; // 点击状态Sprite

    [Header("关闭Image组件设置")]
    [SerializeField][FieldLabel("关闭按钮Image组件")] private Image closeImage; // 关闭Image组件
    [SerializeField][FieldLabel("关闭按钮正常状态精灵")] private Sprite closeNormalSprite; // 关闭按钮正常状态Sprite
    [SerializeField][FieldLabel("关闭按钮点击状态精灵")] private Sprite closeClickedSprite; // 关闭按钮点击状态Sprite

    [Header("面板和画布设置")]
    [SerializeField] private RawImage[] panels; // 面板数组
    [SerializeField] private Canvas backpackCanvas; // 背包Canvas

    private int currentSelectedIndex = -1;
    private bool isBackpackOpen = false;
    private BackpackState backpackState; // BackpackState引用

    private void Start()
    {
        InitializeNavigation();
        InitializeCloseImage();
    }

    private void InitializeNavigation()
    {
        for (int i = 0; i < navigationImages.Length; i++)
        {
            int index = i;
            EventTrigger trigger = navigationImages[i].gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) => OnNavigationClicked(index));
            trigger.triggers.Add(entry);

            navigationImages[i].sprite = normalSprites[i];
        }
        // 默认显示第一个面板（背包网格界面）并设置正确的状态
        if (panels.Length > 0)
        {
            OnNavigationClicked(0);
        }
    }

    private void InitializeCloseImage()
    {
        if (closeImage != null)
        {
            closeImage.sprite = closeNormalSprite;
            EventTrigger trigger = closeImage.gameObject.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((data) => OnCloseClicked());
            trigger.triggers.Add(entry);
        }
    }

    private void OnNavigationClicked(int index)
    {
        if (currentSelectedIndex == index) return;

        if (currentSelectedIndex >= 0)
        {
            panels[currentSelectedIndex].gameObject.SetActive(false);
            navigationImages[currentSelectedIndex].sprite = normalSprites[currentSelectedIndex];
        }

        panels[index].gameObject.SetActive(true);
        navigationImages[index].sprite = clickedSprites[index];

        currentSelectedIndex = index;
    }

    // 设置BackpackState引用
    public void SetBackpackState(BackpackState state)
    {
        backpackState = state;
    }

    /// <summary>
    /// 公共方法：切换到指定索引的导航面板
    /// </summary>
    /// <param name="index">面板索引</param>
    public void SwitchToPanel(int index)
    {
        OnNavigationClicked(index);
    }

    private void OnCloseClicked()
    {
        if (closeImage != null)
        {
            closeImage.sprite = closeClickedSprite; // 点击时切换到点击状态Sprite作为视觉反馈
            StartCoroutine(ResetCloseSprite()); // 短暂延迟后恢复
        }
        CloseBackpack(); // 直接关闭背包
    }

    // 添加缺失的ResetCloseSprite协程方法
    private IEnumerator ResetCloseSprite()
    {
        yield return new WaitForSeconds(0.1f); // 等待0.1秒
        if (closeImage != null)
        {
            closeImage.sprite = closeNormalSprite; // 恢复到正常状态
        }
    }

    private void CloseBackpack()
    {
        if (isBackpackOpen)
        {
            isBackpackOpen = false;
            backpackCanvas.gameObject.SetActive(false);

            // 调用BackpackState的关闭方法来处理输入控制
            if (backpackState != null)
            {
                backpackState.CloseBackpack();
            }
        }
    }

    public void ToggleBackpack()
    {
        isBackpackOpen = !isBackpackOpen;
        backpackCanvas.gameObject.SetActive(isBackpackOpen);

        if (isBackpackOpen)
        {
            // 默认显示第一个面板（假设为backpack界面）
            OnNavigationClicked(0);

            // 调用BackpackState的打开方法来处理输入控制
            if (backpackState != null)
            {
                backpackState.OpenBackpack();
            }
        }
        else
        {
            // 调用BackpackState的关闭方法来处理输入控制
            if (backpackState != null)
            {
                backpackState.CloseBackpack();
            }
        }
    }

    /// <summary>
    /// 直接打开背包并切换到指定面板
    /// </summary>
    /// <param name="panelIndex">要打开的面板索引</param>
    public void OpenToSpecificPanel(int panelIndex)
    {
        if (!isBackpackOpen)
        {
            isBackpackOpen = true;
            backpackCanvas.gameObject.SetActive(true);

            // 调用BackpackState的打开方法来处理输入控制
            if (backpackState != null)
            {
                backpackState.OpenBackpack();
            }
        }

        // 切换到指定面板
        OnNavigationClicked(panelIndex);
    }
}