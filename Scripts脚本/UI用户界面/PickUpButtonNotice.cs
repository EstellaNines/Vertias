using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PickUpButtonNotice : MonoBehaviour
{
    [Header("拾取提示设置")]
    [Tooltip("拾取提示文本组件")]
    public TextMeshProUGUI pickUpText;
    
    [Tooltip("提示文本内容")]
    public string pickUpMessage = "· PickUp";
    
    private static PickUpButtonNotice instance;
    
    void Awake()
    {
        // 设置单例
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // 初始化文本透明度为0
        if (pickUpText != null)
        {
            Color textColor = pickUpText.color;
            textColor.a = 0f;
            pickUpText.color = textColor;
        }
    }
    
    // 显示拾取提示
    public static void ShowPickUpNotice()
    {
        if (instance != null && instance.pickUpText != null)
        {
            // 设置固定的文本内容
            instance.pickUpText.text = instance.pickUpMessage;
            
            // 设置透明度为255 (1.0f)
            Color textColor = instance.pickUpText.color;
            textColor.a = 1f;
            instance.pickUpText.color = textColor;
            
            Debug.Log($"显示拾取提示: {instance.pickUpText.text}");
        }
    }
    
    // 隐藏拾取提示
    public static void HidePickUpNotice()
    {
        if (instance != null && instance.pickUpText != null)
        {
            // 设置透明度为0
            Color textColor = instance.pickUpText.color;
            textColor.a = 0f;
            instance.pickUpText.color = textColor;
            
            Debug.Log("隐藏拾取提示");
        }
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
