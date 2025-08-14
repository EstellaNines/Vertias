using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // 添加TextMeshPro引用

public class StroageToBackpack : MonoBehaviour
{
    [Header("UI设置")]
    public TextMeshProUGUI tmpText; // 要激活的TMP文本组件
    public string displayText = "按Tab键打开仓库"; // 显示的文本内容

    [Header("背包系统")]
    public BackpackState backpackState; // 背包状态管理器引用

    private bool playerInRange = false; // 玩家是否在范围内
    private bool wasBackpackOpenWhenEntered = false; // 记录玩家进入时背包是否已打开

    void Start()
    {
        // 确保文本初始状态为隐藏
        if (tmpText != null)
        {
            tmpText.gameObject.SetActive(false);
        }

        // 如果没有手动设置BackpackState，尝试自动查找
        if (backpackState == null)
        {
            backpackState = FindObjectOfType<BackpackState>();
            if (backpackState == null)
            {
                Debug.LogWarning("未找到BackpackState组件！请确保场景中存在BackpackState脚本。");
            }
        }
    }

    // 移除Update方法中的E键检测，Tab键由BackpackState系统统一处理
    // Tab键输入现在完全由PlayerInputController -> BackpackState系统处理

    // 当其他碰撞体进入触发器时调用
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"触发器检测到对象: {other.name}, 标签: {other.tag}");

        // 检查是否是玩家进入
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            Debug.Log("玩家进入仓库触发器范围，可以使用Tab键打开背包");

            // 记录玩家进入时背包的状态
            if (backpackState != null)
            {
                wasBackpackOpenWhenEntered = backpackState.IsBackpackOpen();
            }

            // 激活TMP文本
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(true);
                tmpText.text = displayText;
            }
        }
    }

    // 当其他碰撞体离开触发器时调用
    private void OnTriggerExit2D(Collider2D other)
    {
        // 检查是否是玩家离开
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            Debug.Log("玩家离开仓库触发器范围");

            // 隐藏TMP文本
            if (tmpText != null)
            {
                tmpText.gameObject.SetActive(false);
            }

            // 如果玩家进入时背包是关闭的，离开时如果背包是打开的，则关闭它
            if (backpackState != null && !wasBackpackOpenWhenEntered && backpackState.IsBackpackOpen())
            {
                backpackState.ForceCloseBackpack();
                Debug.Log("玩家离开仓库范围，自动关闭背包界面");
            }
        }
    }

    // 公共方法：检查玩家是否在范围内（供其他脚本调用）
    public bool IsPlayerInRange()
    {
        return playerInRange;
    }
}
