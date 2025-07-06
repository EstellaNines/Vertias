using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeathState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    private bool deathAnimationFinished = false;
    private float deathTimer = 0f;
    private float minDeathTime = 0.2f; // 最小死亡时间，确保动画播放完整
    
    // --- 构造函数 --- 
    public EnemyDeathState(Enemy enemy)
    {
        this.enemy = enemy;
    }
    
    // --- 状态方法 ---
    public void OnEnter()
    {
        Debug.Log($"敌人 {enemy.gameObject.name} 进入死亡状态");
        
        // 播放死亡动画
        if (enemy.animator != null)
        {
            enemy.animator.Play("Death");
        }
        
        // 武器掉落逻辑
        DropWeapon();
        
        // 停止所有移动
        if (enemy.RB != null)
        {
            enemy.RB.velocity = Vector2.zero;
            enemy.RB.isKinematic = true; // 设置为运动学模式，避免物理干扰
        }
        
        // 禁用碰撞器，防止继续被攻击
        if (enemy.Collider != null)
        {
            enemy.Collider.enabled = false;
        }
        
        // 停止寻路
        if (enemy.seeker != null)
        {
            enemy.seeker.enabled = false;
        }
        
        // 重置计时器
        deathTimer = 0f;
        deathAnimationFinished = false;
        
        // 触发死亡事件（如果有的话）
        enemy.OnDeath?.Invoke();
    }

    public void OnExit()
    {
        // 死亡状态通常不会退出，但为了完整性保留此方法
        Debug.Log($"敌人 {enemy.gameObject.name} 退出死亡状态");
    }

    public void OnFixedUpdate()
    {
        // 确保敌人保持静止
        if (enemy.RB != null && !enemy.RB.isKinematic)
        {
            enemy.RB.velocity = Vector2.zero;
        }
    }

    public void OnUpdate()
    {
        // 更新死亡计时器
        deathTimer += Time.deltaTime;
        
        // 检查死亡动画是否播放完毕
        if (enemy.animator != null && !deathAnimationFinished)
        {
            AnimatorStateInfo stateInfo = enemy.animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Death") && stateInfo.normalizedTime >= 0.95f)
            {
                deathAnimationFinished = true;
                OnDeathAnimationFinished();
            }
        }
        
        // 如果动画播放完毕且达到最小死亡时间，可以考虑销毁对象或其他处理
        if (deathAnimationFinished && deathTimer >= minDeathTime)
        {

        }
    }
    
    // 死亡动画播放完毕时的回调
    private void OnDeathAnimationFinished()
    {
        Debug.Log($"敌人 {enemy.gameObject.name} 死亡动画播放完毕");
    }
    
    // 延迟销毁协程（可选）
    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (enemy != null && enemy.gameObject != null)
        {
            Object.Destroy(enemy.gameObject);
        }
    }

    // 修改武器掉落方法
    private void DropWeapon()
    {
        if (enemy.weapon != null)
        {
            // 获取武器控制器
            WeaponManager weaponController = enemy.weapon.GetComponent<WeaponManager>();
            
            if (weaponController != null)
            {
                // 通知武器控制器设置为掉落状态
                weaponController.OnDropped();
            }
            else
            {
                // 如果没有武器控制器，添加一个
                weaponController = enemy.weapon.gameObject.AddComponent<WeaponManager>();
                weaponController.OnDropped();
            }
            
            // 将武器从敌人的子对象中分离
            enemy.weapon.SetParent(null);
            
            // 设置武器位置到敌人脚边
            Vector3 dropPosition = enemy.transform.position;
            dropPosition.y -= 0.5f;
            enemy.weapon.position = dropPosition;
            
            // 重置武器旋转为 (0,0,0)
            enemy.weapon.rotation = Quaternion.identity;
            
            Debug.Log($"武器已掉落到 {dropPosition}，旋转已重置，已配置为可拾取物品");
        }
    }
}
