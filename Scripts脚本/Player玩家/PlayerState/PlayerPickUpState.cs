using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickUpState : IState
{
    // --- 拾取状态相关变量 ---
    public Player player;
    private bool hasProcessedPickup = false;
    
    // 构造函数
    public PlayerPickUpState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        hasProcessedPickup = false;
        Debug.Log("进入拾取状态");
        
        // 立即处理拾取逻辑
        ProcessPickup();
    }
    
    public void OnExit()
    {
        player.isPickingUp = false;
        hasProcessedPickup = false;
        Debug.Log("退出拾取状态");
    }
    
    public void OnFixedUpdate()
    {
        // 拾取状态下停止移动
        player.PlayerRB2D.velocity = Vector2.zero;
    }
    
    public void OnUpdate()
    {
        // 保持基础瞄准功能，允许玩家在拾取时调整视角
        player.UpdateBasicAiming();

        // 拾取处理完成后，根据输入切换状态
        if (hasProcessedPickup)
        {
            if (player.InputDirection != Vector2.zero)
            {
                player.transitionState(PlayerStateType.Move);
            }
            else
            {
                player.transitionState(PlayerStateType.Idle);
            }
        }
    }
    
    private void ProcessPickup()
    {
        if (player.nearbyItem != null)
        {
            // 如果玩家已经持有物品，先丢弃当前物品
            if (player.currentPickedItem != null)
            {
                DropCurrentItem();
            }
            
            // 拾取新物品
            PickUpItem(player.nearbyItem);
            player.nearbyItem = null; // 清空附近物品引用
        }
        
        hasProcessedPickup = true;
    }
    
    // 拾取物品方法
    private void PickUpItem(ItemBase item)
    {
        player.currentPickedItem = item;
        
        // 根据物品Tag设置父对象位置
        Transform parentTransform = item.CompareTag("Weapon") ? player.Hand : player.Hand;
        
        item.transform.SetParent(parentTransform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.Euler(Vector3.zero);
        
        // 禁用物理组件，防止干扰
        Rigidbody2D rb = item.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
        
        Collider2D collider = item.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        Debug.Log($"成功拾取物品: {item.name}");
    }
    
    // 丢弃当前持有的物品
    private void DropCurrentItem()
    {
        if (player.currentPickedItem == null) return;
        
        Transform itemTransform = player.currentPickedItem.transform;
        itemTransform.SetParent(null);
        itemTransform.rotation = Quaternion.Euler(Vector3.zero);
        itemTransform.localScale = Vector3.one;
        
        // 将物品放置在玩家位置
        itemTransform.position = player.transform.position;
        
        Rigidbody2D rb = itemTransform.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = itemTransform.gameObject.AddComponent<Rigidbody2D>();
        }
        rb.isKinematic = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        
        Collider2D collider = itemTransform.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        Debug.Log($"丢弃物品: {player.currentPickedItem.name}");
        player.currentPickedItem = null;
    }
}