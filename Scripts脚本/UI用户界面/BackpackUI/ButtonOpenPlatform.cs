using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonOpenPlatform : MonoBehaviour
{
    [Header("按钮设置")]
    [FieldLabel("按钮数组")] public Button[] buttons; // 按钮数组，需要在Inspector中拖拽按钮组件

    [Header("RawImage设置")]
    [FieldLabel("RawImage数组")] public RawImage[] rawImages; // RawImage数组，对应每个按钮显示的面板

    [Header("按钮颜色设置")]
    [FieldLabel("正常状态颜色")] public Color normalColor = Color.white; // 正常状态的颜色
    [FieldLabel("按下状态颜色")] public Color pressedColor = Color.gray; // 按下状态的颜色
    [FieldLabel("悬停状态颜色")] public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 悬停状态的颜色
    [FieldLabel("禁用状态颜色")] public Color disabledColor = Color.gray; // 禁用状态的颜色

    // 当前选中的按钮索引
    private int currentSelectedIndex = -1;

    // 保存原始的颜色块配置
    private ColorBlock[] originalColorBlocks;

    private void Start()
    {
        rawImages[0].gameObject.SetActive(true); // 默认显示第一个RawImage
        InitializeButtons();
        InitializeRawImages();
    }

    // 初始化按钮
    private void InitializeButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置按钮数组");
            return;
        }

        // 保存原始颜色块配置
        originalColorBlocks = new ColorBlock[buttons.Length];

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                // 保存原始颜色块配置
                originalColorBlocks[i] = buttons[i].colors;

                // 设置按钮的初始颜色状态
                SetButtonColors(i, false);

                // 为每个按钮添加点击事件监听器
                int buttonIndex = i; // 局部变量捕获
                buttons[i].onClick.AddListener(() => OnButtonClicked(buttonIndex));
            }
        }
    }

    // 初始化RawImage
    private void InitializeRawImages()
    {
        if (rawImages == null || rawImages.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置RawImage数组");
            return;
        }
        // 初始化时隐藏所有RawImage
        for (int i = 0; i < rawImages.Length; i++)
        {
            if (rawImages[i] != null)
            {
                rawImages[i].gameObject.SetActive(false);
            }
        }
    }

    // 按钮点击事件处理
    private void OnButtonClicked(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= buttons.Length || buttons[buttonIndex] == null)
            return;

        // 如果点击的是当前已选中的按钮，则不执行任何操作（可选择是否取消选中）
        if (currentSelectedIndex == buttonIndex)
        {
            Debug.Log("按钮 " + buttonIndex + " 已经处于选中状态，无需重复操作");
            return;
        }

        // 隐藏之前选中的按钮对应的RawImage
        if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Length)
        {
            HideRawImage(currentSelectedIndex);
            SetButtonColors(currentSelectedIndex, false);
        }

        // 显示新选中的按钮对应的RawImage
        ShowRawImage(buttonIndex);
        SetButtonColors(buttonIndex, true);

        // 更新当前选中的按钮索引
        currentSelectedIndex = buttonIndex;

        // 调用按钮选中时的回调方法
        OnButtonSelected(buttonIndex);

        Debug.Log("按钮 " + buttonIndex + " 被选中，显示对应的RawImage");
    }

    // 显示指定索引的RawImage
    private void ShowRawImage(int index)
    {
        if (rawImages != null && index >= 0 && index < rawImages.Length && rawImages[index] != null)
        {
            rawImages[index].gameObject.SetActive(true);
        }
    }

    // 隐藏指定索引的RawImage
    private void HideRawImage(int index)
    {
        if (rawImages != null && index >= 0 && index < rawImages.Length && rawImages[index] != null)
        {
            rawImages[index].gameObject.SetActive(false);
        }
    }

    // 隐藏所有RawImage
    private void HideAllRawImages()
    {
        if (rawImages != null)
        {
            for (int i = 0; i < rawImages.Length; i++)
            {
                HideRawImage(i);
            }
        }
    }

    // 设置按钮的颜色
    private void SetButtonColors(int buttonIndex, bool isSelected)
    {
        if (buttonIndex < 0 || buttonIndex >= buttons.Length || buttons[buttonIndex] == null)
            return;

        ColorBlock colorBlock = buttons[buttonIndex].colors;

        if (isSelected)
        {
            // 选中状态：所有状态都使用按下颜色
            colorBlock.normalColor = pressedColor;
            colorBlock.highlightedColor = pressedColor;
            colorBlock.pressedColor = pressedColor;
            colorBlock.selectedColor = pressedColor;
        }
        else
        {
            // 未选中状态：恢复正常的颜色配置
            colorBlock.normalColor = normalColor;
            colorBlock.highlightedColor = hoverColor;
            colorBlock.pressedColor = pressedColor;
            colorBlock.selectedColor = normalColor;
        }

        colorBlock.disabledColor = disabledColor;
        colorBlock.colorMultiplier = 1f;
        colorBlock.fadeDuration = 0.1f;

        buttons[buttonIndex].colors = colorBlock;
    }

    // 按钮选中时的回调方法（可以被子类重写以添加自定义逻辑）
    protected virtual void OnButtonSelected(int buttonIndex)
    {
        // 这里可以添加按钮选中时的自定义逻辑
        // 例如：播放音效、触发特定事件等

        switch (buttonIndex)
        {
            case 0:
                Debug.Log("选中了第一个按钮，显示第一个RawImage");
                break;
            case 1:
                Debug.Log("选中了第二个按钮，显示第二个RawImage");
                break;
            case 2:
                Debug.Log("选中了第三个按钮，显示第三个RawImage");
                break;
            default:
                Debug.Log("选中了第 " + (buttonIndex + 1) + " 个按钮，显示第 " + (buttonIndex + 1) + " 个RawImage");
                break;
        }
    }

    // 公共方法：外部调用以选中指定按钮
    public void SelectButton(int buttonIndex)
    {
        OnButtonClicked(buttonIndex);
    }

    // 公共方法：获取当前选中的按钮索引
    public int GetSelectedButtonIndex()
    {
        return currentSelectedIndex;
    }

    // 公共方法：清除所有选中状态
    public void ClearSelection()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Length)
        {
            SetButtonColors(currentSelectedIndex, false);
            HideRawImage(currentSelectedIndex);
        }
        currentSelectedIndex = -1;
    }

    // 公共方法：设置指定按钮的可交互状态
    public void SetButtonInteractable(int buttonIndex, bool interactable)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Length && buttons[buttonIndex] != null)
        {
            buttons[buttonIndex].interactable = interactable;
        }
    }

    // 公共方法：获取指定索引的RawImage
    public RawImage GetRawImage(int buttonIndex)
    {
        if (rawImages != null && buttonIndex >= 0 && buttonIndex < rawImages.Length)
        {
            return rawImages[buttonIndex];
        }
        return null;
    }

    // 公共方法：获取指定索引的按钮
    public Button GetButton(int buttonIndex)
    {
        if (buttons != null && buttonIndex >= 0 && buttonIndex < buttons.Length)
        {
            return buttons[buttonIndex];
        }
        return null;
    }
}
