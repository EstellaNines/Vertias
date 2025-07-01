using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPickUpState : IState
{
    // --- ���ʰȡ״̬�� ---
    public Player player;
    private bool hasProcessedPickup = false;
    
    // ���캯��
    public PlayerPickUpState(Player player)
    {
        this.player = player;
    }
    
    public void OnEnter()
    {
        hasProcessedPickup = false;
        Debug.Log("����ʰȡ״̬");
        
        // ��������ʰȡ�߼�
        ProcessPickup();
    }
    
    public void OnExit()
    {
        player.isPickingUp = false;
        hasProcessedPickup = false;
        Debug.Log("�˳�ʰȡ״̬");
    }
    
    public void OnFixedUpdate()
    {
        // ʰȡ״̬��ֹͣ�ƶ�
        player.PlayerRB2D.velocity = Vector2.zero;
    }
    
    public void OnUpdate()
    {
        // ������׼���ܣ��ӽǺ���׼������£�
        player.UpdateBasicAiming();

        // ʰȡ������ɺ���������л�״̬
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
            // �����ǰ�Ѿ�������Ʒ���ȶ�����ǰ��Ʒ
            if (player.currentPickedItem != null)
            {
                DropCurrentItem();
            }
            
            // ʰȡ����Ʒ
            PickUpItem(player.nearbyItem);
            player.nearbyItem = null; // ��ո�����Ʒ����
        }
        
        hasProcessedPickup = true;
    }
    
    // ʰȡ��Ʒ����
    private void PickUpItem(ItemBase item)
    {
        player.currentPickedItem = item;
        
        // ������ƷTag���ø����任
        Transform parentTransform = item.CompareTag("Weapon") ? player.Hand : player.Hand;
        
        item.transform.SetParent(parentTransform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.Euler(Vector3.zero);
        
        // ���������������ײ��
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
        
        Debug.Log($"�ɹ�ʰȡ��Ʒ: {item.name}");
    }
    
    // ������ǰ���е���Ʒ
    private void DropCurrentItem()
    {
        if (player.currentPickedItem == null) return;
        
        Transform itemTransform = player.currentPickedItem.transform;
        itemTransform.SetParent(null);
        itemTransform.rotation = Quaternion.Euler(Vector3.zero);
        itemTransform.localScale = Vector3.one;
        
        // ������Ʒλ��Ϊ���λ��
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
        
        Debug.Log($"������Ʒ: {player.currentPickedItem.name}");
        player.currentPickedItem = null;
    }
}