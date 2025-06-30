using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDieState : IState
{
    // --- 获取玩家组件 ---
    public Player player;
    private bool deathAnimationFinished = false; // 死亡动画是否完成
    private string currentDeathAnimation; // 当前播放的死亡动画名称
    
    // 构造函数
    public PlayerDieState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        Debug.Log("进入死亡状态");
        
        // 停止所有移动
        if (player.PlayerRB2D != null)
        {
            player.PlayerRB2D.velocity = Vector2.zero;
        }
        
        // 禁用玩家碰撞体
        if (player.collider2D != null)
        {
            player.collider2D.enabled = false;
            Debug.Log("玩家碰撞体已禁用");
        }
        
        // 检查Hand是否有子对象，如果有则掉落武器
        if (player.Hand != null && player.Hand.childCount > 0)
        {
            DropWeaponAtFeet();
        }
        
        // 设置死亡动画参数，让动画器自动过渡到死亡动画
        player.AIMTOR.SetBool("isDying?", true);
        
        // 无论是否有武器都播放Death动画
        currentDeathAnimation = "Death";
        Debug.Log("设置isDying?为true，将播放普通死亡动画");
        
        deathAnimationFinished = false;
        
        // 禁用玩家输入
        if (player.playerInputController != null)
        {
            player.playerInputController.DisableGameplayInput();
        }
    }

    public void OnExit()
    {
        Debug.Log("退出死亡状态");
        
        // 重新启用玩家碰撞体（如果需要复活功能）
        if (player.collider2D != null)
        {
            player.collider2D.enabled = true;
            Debug.Log("玩家碰撞体已重新启用");
        }
        
        // 重置死亡动画参数
        player.AIMTOR.SetBool("isDying?", false);
    }

    public void OnFixedUpdate()
    {
        // 死亡状态下完全静止
        if (player.PlayerRB2D != null)
        {
            player.PlayerRB2D.velocity = Vector2.zero;
        }
    }

    public void OnUpdate()
    {
        // 检查死亡动画是否播放完成
        if (!deathAnimationFinished)
        {
            AnimatorStateInfo currentStateInfo = player.AIMTOR.GetCurrentAnimatorStateInfo(0);
            
            // 检查是否在播放对应的死亡动画且动画已播放完成
            if (currentStateInfo.IsName(currentDeathAnimation) && currentStateInfo.normalizedTime >= 1.0f)
            {
                OnDeathAnimationFinished();
            }
        }
        
        // 死亡状态下不进行任何状态切换，永远保持死亡状态
    }
    
    // 死亡动画播放完成后的处理
    public void OnDeathAnimationFinished()
    {
        if (!deathAnimationFinished)
        {
            deathAnimationFinished = true;
            
            // 设置isDying?为false，让动画器过渡到Exit状态
            player.AIMTOR.SetBool("isDying?", false);
            
            Debug.Log($"死亡动画 {currentDeathAnimation} 播放完成，设置isDying?为false");
            
            // 玩家状态机仍然保持在死亡状态
        }
    }
    
    // 掉落武器到玩家脚下
    private void DropWeaponAtFeet()
    {
        if (player.Hand == null || player.Hand.childCount == 0) return;
        
        // 获取武器对象
        Transform weaponTransform = player.Hand.GetChild(0);
        
        // 从Hand中移除武器
        weaponTransform.SetParent(null);
        weaponTransform.rotation = Quaternion.Euler(Vector3.zero);
        weaponTransform.localScale = Vector3.one;
        
        // 设置武器位置为玩家脚下（稍微偏移避免重叠）
        Vector3 dropPosition = player.transform.position;
        dropPosition.y -= 0.5f; // 向下偏移0.5单位到脚下
        dropPosition.x += Random.Range(-0.3f, 0.3f); // 随机水平偏移
        weaponTransform.position = dropPosition;
        
        // 恢复武器的物理属性
        Rigidbody2D rb = weaponTransform.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = weaponTransform.gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        // 启用碰撞器
        Collider2D collider = weaponTransform.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        // 重置玩家的武器状态
        player.isWeaponInHand = false;
        player.currentWeapon = null;
        
        Debug.Log($"武器 {weaponTransform.name} 已掉落到玩家脚下");
    }
}