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
        // 默认显示第一个面板
        if (panels.Length > 0) panels[0].gameObject.SetActive(true);
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

    private void OnCloseClicked()
    {
        if (closeImage != null)
        {
            closeImage.sprite = closeClickedSprite; // 点击时切换到点击状态Sprite作为视觉反馈
            StartCoroutine(ResetCloseSprite()); // 短暂延迟后恢复
        }
        CloseBackpack(); // 直接关闭背包
    }

    private void CloseBackpack()
    {
        if (isBackpackOpen)
        {
            isBackpackOpen = false;
            backpackCanvas.gameObject.SetActive(false);
            // 重置选中状态
        }
    }

    private IEnumerator ResetCloseSprite()
    {
        yield return new WaitForSeconds(0.2f); // 等待短暂时间
        if (closeImage != null)
        {
            closeImage.sprite = closeNormalSprite;
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
            // 打开背包时确保显示第一个面板，提供一致的UI体验
            // 调用BackpackState中的方法
        }
        else
        {
            // 关闭背包时的其他逻辑
        }
    }
}