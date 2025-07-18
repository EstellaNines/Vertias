using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.UI;
using TMPro;

public class OutsideTransform : MonoBehaviour
{
    [Header("传送到内部设置")]
    [FieldLabel("内部玩家目标位置")] public Transform insideTargetPosition; // 内部玩家传送位置
    [FieldLabel("内部摄像机目标位置")] public Transform insideCameraTargetPosition; // 内部摄像机目标位置
    [FieldLabel("内部碰撞限制区")] public Collider2D insideConfinerCollider; // 内部碰撞限制区域

    [Header("通用设置")]
    [FieldLabel("虚拟摄像机")] public CinemachineVirtualCamera virtualCamera; // 虚拟摄像机对象
    [FieldLabel("玩家输入控制器")] public PlayerInputController playerInputController; // 玩家输入控制器

    [Header("UI设置")]
    [FieldLabel("提示UI对象")] public GameObject promptUI; // 提示UI对象
    [FieldLabel("提示文本组件")] public TextMeshProUGUI promptText; // 提示文本组件（可选，如果UI中有文本）
    [FieldLabel("提示文本内容")] public string promptMessage = "按F键进入房间"; // 提示文本内容

    private bool playerInRange = false; // 玩家是否在范围内
    private GameObject playerInTrigger; // 在触发器内的玩家对象

    void Start()
    {
        // 如果没有手动分配输入控制器，尝试从玩家对象获取
        if (playerInputController == null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                playerInputController = player.playerInputController;
            }
        }

        // 订阅F键操作事件
        if (playerInputController != null)
        {
            playerInputController.onOperate += OnOperatePressed;
            playerInputController.EnabledUIInput(); // 启用UI输入
        }

        // 初始化时隐藏UI
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }

        // 设置提示文本内容
        if (promptText != null)
        {
            promptText.text = promptMessage;
        }
    }

    void OnDestroy()
    {
        // 取消订阅事件
        if (playerInputController != null)
        {
            playerInputController.onOperate -= OnOperatePressed;
        }
    }

    // F键按下时的处理
    void OnOperatePressed()
    {
        if (playerInRange && playerInTrigger != null)
        {
            TeleportToInside(playerInTrigger);
        }
    }

    // 传送到内部的方法
    void TeleportToInside(GameObject player)
    {
        // 检查目标位置是否设置
        if (insideTargetPosition == null)
        {
            Debug.LogWarning("传送失败：内部目标位置未设置！");
            return;
        }

        // 隐藏UI（传送前）
        HidePromptUI();

        // 玩家传送逻辑
        player.transform.position = insideTargetPosition.position;

        // 摄像机同步逻辑
        if (virtualCamera != null && insideCameraTargetPosition != null)
        {
            // 获取虚拟摄像机的CinemachineConfiner2D组件
            CinemachineConfiner2D confiner = virtualCamera.GetComponent<CinemachineConfiner2D>();

            // 移动摄像机位置
            virtualCamera.transform.position = insideCameraTargetPosition.position;

            // 更新碰撞区域（直接设置Bounding Shape2D）
            if (confiner != null && insideConfinerCollider != null)
            {
                confiner.m_BoundingShape2D = insideConfinerCollider;
                Debug.Log("摄像机已同步至内部位置并更新碰撞区域");
            }
        }

        Debug.Log("玩家已从外部传送至内部");
    }

    // 显示提示UI
    void ShowPromptUI()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(true);
            Debug.Log("显示进入房间提示UI");
        }
    }

    // 隐藏提示UI
    void HidePromptUI()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
            Debug.Log("隐藏进入房间提示UI");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        // 检查碰撞对象是否带有"Player"标签
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;
            playerInTrigger = collision.gameObject;
            ShowPromptUI(); // 显示UI
            Debug.Log("玩家进入外部传送区域，按F键进入内部");
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;
            playerInTrigger = null;
            HidePromptUI(); // 隐藏UI
            Debug.Log("玩家离开外部传送区域");
        }
    }
}
