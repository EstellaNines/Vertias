using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReloadState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    private bool reloadStarted = false;
    private float reloadTimer = 0f; // 添加本地计时器
    private float reloadDuration = 0f; // 添加换弹持续时间

    // 构造函数
    public PlayerReloadState(Player player)
    {
        this.player = player;
    }

    public void OnEnter()
    {
        Debug.Log("进入换弹状态");
        reloadStarted = false;

        // 停止射击
        player.isFiring = false;
        player.isAttacking = false;

        // 确保武器停止射击
        if (player.currentWeaponController != null)
        {
            player.currentWeaponController.SetFiring(false);
        }

        // 播放换弹动画（如果有的话）
        if (player.AIMTOR != null)
        {
            player.AIMTOR.Play("Shoot_Idle");
        }
    }

    public void OnExit()
    {
        Debug.Log("退出换弹状态");
        reloadStarted = false;

        // 停止换弹UI动画
        player.TriggerReloadStopped();
    }

    public void OnFixedUpdate()
    {
        // 换弹状态下可以移动但速度较慢
        player.PlayerRB2D.velocity = player.InputDirection * (player.WalkSpeed * 0.5f);
    }

    public void OnUpdate()
    {
        // 基础瞄准更新
        player.UpdateBasicAiming();

        // 检查是否有武器
        if (!player.isWeaponInHand || player.currentWeaponController == null)
        {
            Debug.Log("没有武器，退出换弹状态");
            player.transitionState(PlayerStateType.Idle);
            return;
        }

        // 受伤检查
        if (player.isHurt)
        {
            player.transitionState(PlayerStateType.Hurt);
            return;
        }

        // 闪避检查
        if (player.isDodged)
        {
            player.transitionState(PlayerStateType.Dodge);
            return;
        }

        // 拾取检查
        if (player.isPickingUp)
        {
            player.transitionState(PlayerStateType.PickUp);
            return;
        }

        // 检查是否按下R键开始换弹
        if (!reloadStarted && player.playerInputController != null)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                StartReloadProcess();
            }
        }

        // 使用本地计时器检查换弹是否完成，而不是依赖武器的IsReloading状态
        if (reloadStarted)
        {
            reloadTimer += Time.deltaTime;
            
            // 当本地计时器达到换弹时间时，才认为换弹完成
            if (reloadTimer >= reloadDuration)
            {
                Debug.Log("换弹完成");
                reloadStarted = false;
                reloadTimer = 0f;
                // 换弹完成后，根据当前输入状态决定下一个状态
                DetermineNextStateBasedOnInput();
                return;
            }
        }

        // 如果没有开始换弹且武器不需要换弹，退出状态
        if (!reloadStarted && !player.currentWeaponController.NeedsReload())
        {
            Debug.Log("武器不需要换弹，退出换弹状态");
            DetermineNextStateBasedOnInput();
            return;
        }

        // 在换弹过程中，允许玩家根据输入切换移动状态
        if (reloadStarted)
        {
            HandleMovementDuringReload();
        }
    }

    /// <summary>
    /// 开始换弹过程（包括武器换弹和UI动画）
    /// </summary>
    private void StartReloadProcess()
    {
        // 开始武器换弹
        player.currentWeaponController.StartReload();
        reloadStarted = true;
        reloadTimer = 0f; // 重置本地计时器

        // 获取换弹时间并启动UI动画
        reloadDuration = player.currentWeaponController.GetReloadTime();
        player.TriggerReloadStarted(reloadDuration);

        Debug.Log($"开始换弹，预计时间: {reloadDuration}秒");
    }

    // 处理换弹期间的移动状态切换
    private void HandleMovementDuringReload()
    {
        // 检查移动输入
        if (player.InputDirection != Vector2.zero)
        {
            // 有移动输入时，根据移动类型切换状态
            if (player.isRunning)
            {
                player.transitionState(PlayerStateType.Run);
            }
            else if (player.isCrouching)
            {
                player.transitionState(PlayerStateType.Crouch);
            }
            else
            {
                player.transitionState(PlayerStateType.Move);
            }
        }
        else
        {
            // 没有移动输入时，切换到待机状态
            player.transitionState(PlayerStateType.Idle);
        }
    }

    // 根据当前输入状态决定下一个状态
    private void DetermineNextStateBasedOnInput()
    {
        if (player.InputDirection != Vector2.zero)
        {
            if (player.isRunning)
            {
                player.transitionState(PlayerStateType.Run);
            }
            else if (player.isCrouching)
            {
                player.transitionState(PlayerStateType.Crouch);
            }
            else
            {
                player.transitionState(PlayerStateType.Move);
            }
        }
        else
        {
            player.transitionState(PlayerStateType.Idle);
        }
    }
}