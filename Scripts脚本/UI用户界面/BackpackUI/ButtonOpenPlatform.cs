using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonOpenPlatform : MonoBehaviour
{
    [Header("按钮设置")]
    [FieldLabel("按钮数组")] public Button[] buttons; // 按钮数组，需要在Inspector中拖拽赋值

    [Header("RawImage面板设置")]
    [FieldLabel("RawImage面板数组")] public RawImage[] rawImages; // RawImage面板数组，需要在Inspector中拖拽赋值

    [Header("按钮颜色设置")]
    [FieldLabel("正常状态颜色")] public Color normalColor = Color.white; // 正常状态颜色
    [FieldLabel("按下状态颜色")] public Color pressedColor = Color.gray; // 按下状态颜色
    [FieldLabel("悬停状态颜色")] public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 悬停状态颜色
    [FieldLabel("禁用状态颜色")] public Color disabledColor = Color.gray; // 禁用状态颜色

    // 当前选中的按钮索引，-1表示没有选中任何按钮
    private int currentSelectedIndex = -1;

    // 存储按钮原始颜色块设置
    private ColorBlock[] originalColorBlocks;

    private void Start()
    {
        rawImages[0].gameObject.SetActive(true); // 默认显示第一个RawImage
        InitializeButtons();
        InitializeRawImages();
    }

    // 初始化按钮设置
    private void InitializeButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置按钮数组！请在Inspector中拖拽赋值。");
            return;
        }

        // 保存原始颜色块设置
        originalColorBlocks = new ColorBlock[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                // 保存原始颜色块
                originalColorBlocks[i] = buttons[i].colors;

                // 设置自定义颜色
                ColorBlock colorBlock = buttons[i].colors;
                colorBlock.normalColor = normalColor;
                colorBlock.pressedColor = pressedColor;
                colorBlock.highlightedColor = hoverColor;
                colorBlock.disabledColor = disabledColor;
                buttons[i].colors = colorBlock;

                // 添加点击事件监听
                int index = i; // 闭包变量
                buttons[i].onClick.AddListener(() => OnButtonClicked(index));
            }
            else
            {
                Debug.LogWarning($"ButtonOpenPlatform: 按钮数组索引 {i} 为空！");
            }
        }

        Debug.Log($"ButtonOpenPlatform: 成功初始化 {buttons.Length} 个按钮。");
    }

    // 初始化RawImage面板设置
    private void InitializeRawImages()
    {
        if (rawImages == null || rawImages.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置RawImage数组！请在Inspector中拖拽赋值。");
            return;
        }

        // 初始时隐藏所有RawImage
        for (int i = 0; i < rawImages.Length; i++)
        {
            if (rawImages[i] != null)
            {
                rawImages[i].gameObject.SetActive(false);
            }
            else
            {
                Debug.LogWarning($"ButtonOpenPlatform: RawImage数组索引 {i} 为空！");
            }
        }

        Debug.Log($"ButtonOpenPlatform: 成功初始化 {rawImages.Length} 个RawImage面板。");
    }

    // 按钮点击事件处理
    private void OnButtonClicked(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= buttons.Length)
        {
            Debug.LogError($"ButtonOpenPlatform: 按钮索引 {buttonIndex} 超出范围！");
            return;
        }

        if (buttonIndex >= rawImages.Length)
        {
            Debug.LogError($"ButtonOpenPlatform: 按钮索引 {buttonIndex} 对应的RawImage不存在！");
            return;
        }

        // 如果点击的是当前已选中的按钮，则不做任何操作
        if (currentSelectedIndex == buttonIndex)
        {
            Debug.Log($"ButtonOpenPlatform: 按钮 {buttonIndex} 已经是选中状态。");
            return;
        }

        // 隐藏当前显示的RawImage
        if (currentSelectedIndex >= 0 && currentSelectedIndex < rawImages.Length)
        {
            if (rawImages[currentSelectedIndex] != null)
            {
                rawImages[currentSelectedIndex].gameObject.SetActive(false);
            }
        }

        // 显示新选中的RawImage
        if (rawImages[buttonIndex] != null)
        {
            rawImages[buttonIndex].gameObject.SetActive(true);
            currentSelectedIndex = buttonIndex;
            Debug.Log($"ButtonOpenPlatform: 切换到按钮 {buttonIndex} 对应的面板。");
        }
        else
        {
            Debug.LogError($"ButtonOpenPlatform: 索引 {buttonIndex} 对应的RawImage为空！");
        }
    }

    // 外部调用：选择指定按钮
    public void SelectButton(int buttonIndex)
    {
        OnButtonClicked(buttonIndex);
    }

    // 外部调用：清除所有选择
    public void ClearSelection()
    {
        // 隐藏当前显示的RawImage
        if (currentSelectedIndex >= 0 && currentSelectedIndex < rawImages.Length)
        {
            if (rawImages[currentSelectedIndex] != null)
            {
                rawImages[currentSelectedIndex].gameObject.SetActive(false);
            }
        }

        currentSelectedIndex = -1;
        Debug.Log("ButtonOpenPlatform: 清除所有选择。");
    }

    // 获取当前选中的按钮索引
    public int GetCurrentSelectedIndex()
    {
        return currentSelectedIndex;
    }

    // 启用/禁用指定按钮
    public void SetButtonEnabled(int buttonIndex, bool enabled)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Length && buttons[buttonIndex] != null)
        {
            buttons[buttonIndex].interactable = enabled;
            Debug.Log($"ButtonOpenPlatform: 按钮 {buttonIndex} 设置为 {(enabled ? "启用" : "禁用")}。");
        }
        else
        {
            Debug.LogWarning($"ButtonOpenPlatform: 无效的按钮索引 {buttonIndex}。");
        }
    }

    // 恢复按钮原始颜色设置
    public void RestoreOriginalColors()
    {
        if (originalColorBlocks != null)
        {
            for (int i = 0; i < buttons.Length && i < originalColorBlocks.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].colors = originalColorBlocks[i];
                }
            }
            Debug.Log("ButtonOpenPlatform: 恢复按钮原始颜色设置。");
        }
    }

    private void OnDestroy()
    {
        // 清理事件监听
        if (buttons != null)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                {
                    buttons[i].onClick.RemoveAllListeners();
                }
            }
        }
    }
}
