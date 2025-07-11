using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonOpenPlatform : MonoBehaviour
{
    [Header("按钮组设置")]
    [FieldLabel("按钮列表")] public Button[] buttons; // 按钮数组，可在Inspector中自定义数量
    
    [Header("返回按钮设置")]
    [FieldLabel("返回按钮")] public Button backButton; // 返回按钮
    [FieldLabel("控制的平面")] public GameObject targetPlane; // 要控制的平面
    
    [Header("RawImage设置")]
    [FieldLabel("RawImage列表")] public RawImage[] rawImages; // RawImage数组，与按钮一一对应
    
    [Header("颜色设置")]
    [FieldLabel("正常颜色")] public Color normalColor = Color.white; // 正常状态颜色
    [FieldLabel("按下颜色")] public Color pressedColor = Color.gray; // 按下状态颜色
    [FieldLabel("悬停颜色")] public Color hoverColor = new Color(0.8f, 0.8f, 0.8f, 1f); // 悬停状态颜色（浅灰色）
    [FieldLabel("禁用颜色")] public Color disabledColor = Color.gray; // 禁用状态颜色
    
    // 当前选中的按钮索引
    private int currentSelectedIndex = -1;
    
    // 存储每个按钮的原始颜色块
    private ColorBlock[] originalColorBlocks;
    
    // 平面的显示状态
    private bool isPlaneVisible = false;
    
    private void Start()
    {
        InitializeButtons();
        InitializeRawImages();
        InitializeBackButton();
        InitializePlane();
    }
    
    // 初始化按钮
    private void InitializeButtons()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置按钮！");
            return;
        }
        
        // 保存原始颜色块
        originalColorBlocks = new ColorBlock[buttons.Length];
        
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null)
            {
                // 保存原始颜色块
                originalColorBlocks[i] = buttons[i].colors;
                
                // 设置自定义颜色
                SetButtonColors(i, false);
                
                // 添加点击事件监听器
                int buttonIndex = i; // 闭包变量
                buttons[i].onClick.AddListener(() => OnButtonClicked(buttonIndex));
            }
        }
    }
    
    // 初始化返回按钮
    private void InitializeBackButton()
    {
        if (backButton != null)
        {
            // 设置返回按钮的颜色样式（与基础按钮相同）
            SetBackButtonColors();
            
            // 添加返回按钮点击事件
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
        else
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置返回按钮！");
        }
    }
    
    // 初始化平面
    private void InitializePlane()
    {
        if (targetPlane != null)
        {
            // 初始时隐藏平面
            targetPlane.SetActive(false);
            isPlaneVisible = false;
        }
        else
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置目标平面！");
        }
    }
    
    // 初始化RawImage
    private void InitializeRawImages()
    {
        if (rawImages == null || rawImages.Length == 0)
        {
            Debug.LogWarning("ButtonOpenPlatform: 没有设置RawImage！");
            return;
        }
        
        // 初始时隐藏所有RawImage
        for (int i = 0; i < rawImages.Length; i++)
        {
            if (rawImages[i] != null)
            {
                rawImages[i].gameObject.SetActive(false);
            }
        }
    }
    
    // 返回按钮点击事件处理
    private void OnBackButtonClicked()
    {
        if (targetPlane != null)
        {
            // 切换平面的显示状态
            isPlaneVisible = !isPlaneVisible;
            targetPlane.SetActive(isPlaneVisible);
            
            Debug.Log("返回按钮被点击，平面状态: " + (isPlaneVisible ? "显示" : "隐藏"));
        }
    }
    
    // 设置返回按钮颜色（与基础按钮样式相同）
    private void SetBackButtonColors()
    {
        if (backButton == null) return;
        
        ColorBlock colorBlock = backButton.colors;
        
        // 使用与基础按钮相同的颜色设置
        colorBlock.normalColor = normalColor;
        colorBlock.highlightedColor = hoverColor;
        colorBlock.pressedColor = pressedColor;
        colorBlock.selectedColor = normalColor;
        colorBlock.disabledColor = disabledColor;
        colorBlock.colorMultiplier = 1f;
        colorBlock.fadeDuration = 0.1f;
        
        backButton.colors = colorBlock;
    }
    
    // 按钮点击事件处理
    private void OnButtonClicked(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= buttons.Length || buttons[buttonIndex] == null)
            return;
            
        // 如果点击的是当前选中的按钮，隐藏当前RawImage并清除选中状态
        if (currentSelectedIndex == buttonIndex)
        {
            HideRawImage(currentSelectedIndex);
            SetButtonColors(currentSelectedIndex, false);
            currentSelectedIndex = -1;
            Debug.Log("隐藏按钮 " + buttonIndex + " 对应的RawImage");
            return;
        }
            
        // 隐藏之前选中按钮对应的RawImage
        if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Length)
        {
            HideRawImage(currentSelectedIndex);
            SetButtonColors(currentSelectedIndex, false);
        }
        
        // 显示新选中按钮对应的RawImage
        ShowRawImage(buttonIndex);
        SetButtonColors(buttonIndex, true);
        
        // 更新当前选中索引
        currentSelectedIndex = buttonIndex;
        
        // 触发按钮选中事件
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
    
    // 设置按钮颜色
    private void SetButtonColors(int buttonIndex, bool isSelected)
    {
        if (buttonIndex < 0 || buttonIndex >= buttons.Length || buttons[buttonIndex] == null)
            return;
            
        ColorBlock colorBlock = buttons[buttonIndex].colors;
        
        if (isSelected)
        {
            // 选中状态：按下颜色保持不变
            colorBlock.normalColor = pressedColor;
            colorBlock.highlightedColor = pressedColor;
            colorBlock.pressedColor = pressedColor;
            colorBlock.selectedColor = pressedColor;
        }
        else
        {
            // 正常状态：使用自定义颜色
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
    
    // 按钮选中事件（可以被子类重写或添加更多逻辑）
    protected virtual void OnButtonSelected(int buttonIndex)
    {
        // 在这里可以添加按钮选中后的具体逻辑
        // 例如：切换平台、显示不同内容等
        
        switch (buttonIndex)
        {
            case 0:
                Debug.Log("选中了第一个平台，显示第一个RawImage");
                break;
            case 1:
                Debug.Log("选中了第二个平台，显示第二个RawImage");
                break;
            case 2:
                Debug.Log("选中了第三个平台，显示第三个RawImage");
                break;
            default:
                Debug.Log("选中了第 " + (buttonIndex + 1) + " 个平台，显示第 " + (buttonIndex + 1) + " 个RawImage");
                break;
        }
    }
    
    // 公共方法：程序化选中按钮
    public void SelectButton(int buttonIndex)
    {
        OnButtonClicked(buttonIndex);
    }
    
    // 公共方法：获取当前选中的按钮索引
    public int GetSelectedButtonIndex()
    {
        return currentSelectedIndex;
    }
    
    // 公共方法：清除所有按钮选中状态
    public void ClearSelection()
    {
        if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Length)
        {
            SetButtonColors(currentSelectedIndex, false);
            HideRawImage(currentSelectedIndex);
        }
        currentSelectedIndex = -1;
    }
    
    // 公共方法：设置按钮是否可交互
    public void SetButtonInteractable(int buttonIndex, bool interactable)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Length && buttons[buttonIndex] != null)
        {
            buttons[buttonIndex].interactable = interactable;
        }
    }
    
    // 公共方法：设置返回按钮是否可交互
    public void SetBackButtonInteractable(bool interactable)
    {
        if (backButton != null)
        {
            backButton.interactable = interactable;
        }
    }
    
    // 公共方法：程序化触发返回按钮
    public void TriggerBackButton()
    {
        OnBackButtonClicked();
    }
    
    // 公共方法：设置平面显示状态
    public void SetPlaneVisible(bool visible)
    {
        if (targetPlane != null)
        {
            isPlaneVisible = visible;
            targetPlane.SetActive(isPlaneVisible);
        }
    }
    
    // 公共方法：获取平面显示状态
    public bool IsPlaneVisible()
    {
        return isPlaneVisible;
    }
    
    // 公共方法：设置目标平面
    public void SetTargetPlane(GameObject plane)
    {
        targetPlane = plane;
        if (plane != null)
        {
            plane.SetActive(isPlaneVisible);
        }
    }
    
    // 公共方法：添加新按钮到组中
    public void AddButton(Button newButton, RawImage newRawImage = null)
    {
        if (newButton == null) return;
        
        // 扩展按钮数组
        System.Array.Resize(ref buttons, buttons.Length + 1);
        System.Array.Resize(ref originalColorBlocks, originalColorBlocks.Length + 1);
        
        int newIndex = buttons.Length - 1;
        buttons[newIndex] = newButton;
        
        // 设置新按钮
        originalColorBlocks[newIndex] = newButton.colors;
        SetButtonColors(newIndex, false);
        newButton.onClick.AddListener(() => OnButtonClicked(newIndex));
        
        // 扩展RawImage数组
        if (newRawImage != null)
        {
            if (rawImages == null)
            {
                rawImages = new RawImage[1];
            }
            else
            {
                System.Array.Resize(ref rawImages, rawImages.Length + 1);
            }
            rawImages[newIndex] = newRawImage;
            newRawImage.gameObject.SetActive(false);
        }
    }
    
    // 公共方法：设置指定按钮对应的RawImage
    public void SetRawImage(int buttonIndex, RawImage rawImage)
    {
        if (buttonIndex >= 0 && buttonIndex < buttons.Length)
        {
            if (rawImages == null || rawImages.Length <= buttonIndex)
            {
                System.Array.Resize(ref rawImages, buttonIndex + 1);
            }
            rawImages[buttonIndex] = rawImage;
            if (rawImage != null)
            {
                rawImage.gameObject.SetActive(false);
            }
        }
    }
    
    // 公共方法：获取指定按钮对应的RawImage
    public RawImage GetRawImage(int buttonIndex)
    {
        if (rawImages != null && buttonIndex >= 0 && buttonIndex < rawImages.Length)
        {
            return rawImages[buttonIndex];
        }
        return null;
    }
    
    private void OnDestroy()
    {
        // 清理事件监听器
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
        
        // 清理返回按钮事件监听器
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
        }
    }
}
