using UnityEngine;

public class EnemyWeaponController : MonoBehaviour
{
    [Header("武器设置")]
    [SerializeField] private Transform weaponTransform; // 武器Transform引用
    [SerializeField] private SpriteRenderer weaponSprite; // 武器的SpriteRenderer组件
    [SerializeField] private float flipAngleThreshold = 90f; // 翻转阈值角度，默认90度
    
    [Header("目标设置")]
    [SerializeField] private LayerMask targetLayer; // 目标层（玩家所在层）
    
    private Transform playerTransform; // 玩家Transform引用
    private bool isWeaponFlipped = false; // 武器是否已翻转
    private RaycastFOV fov; // 视野检测组件引用
    private Enemy enemyComponent; // 敌人组件引用
    private EnemyState currentEnemyState; // 当前敌人状态
    private WeaponManager weaponManager; // 武器管理器引用
    
    void Awake()
    {
        // 获取必要组件
        fov = GetComponentInParent<RaycastFOV>();
        enemyComponent = GetComponentInParent<Enemy>();
        
        // 如果没有手动设置武器Transform，尝试自动查找
        if (weaponTransform == null)
        {
            weaponTransform = transform;
        }
        
        // 如果没有手动设置武器SpriteRenderer，尝试从武器对象获取
        if (weaponSprite == null && weaponTransform != null)
        {
            weaponSprite = weaponTransform.GetComponent<SpriteRenderer>();
        }
        
        // 查找玩家
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
        
        // 获取武器管理器组件
        weaponManager = GetComponent<WeaponManager>();
    }
    
    void Update()
    {
        // 检查武器是否仍然属于敌人
        if (!IsWeaponOwnedByEnemy())
        {
            return; // 如果武器不属于敌人，则不控制旋转
        }
        
        // 获取当前敌人状态
        if (enemyComponent != null)
        {
            currentEnemyState = enemyComponent.GetCurrentState();
        }
        
        // 检查是否应该旋转武器（仅当玩家被检测到时）
        bool shouldRotate = ShouldRotateWeapon();
        
        if (shouldRotate && playerTransform != null)
        {
            RotateWeaponTowardsPlayer();
        }
        else
        {
            ResetWeaponRotation();
        }
    }
    
    // 检查武器是否仍然属于敌人
    private bool IsWeaponOwnedByEnemy()
    {
        // 方法1：检查武器管理器的持有者
        if (weaponManager != null)
        {
            return weaponManager.GetCurrentOwner() == enemyComponent.transform;
        }
        
        // 方法2：检查武器是否仍然是敌人的子对象
        if (enemyComponent != null)
        {
            return transform.IsChildOf(enemyComponent.transform);
        }
        
        return false;
    }
    
    // 判断是否应该旋转武器
    private bool ShouldRotateWeapon()
    {
        // 仅当玩家被检测到时才旋转武器
        // 如果有FOV组件且检测到玩家，则旋转武器
        if (fov != null && fov.IsPlayerDetected())
        {
            return true;
        }
        
        // 如果有Enemy组件且玩家被检测到，则旋转武器
        if (enemyComponent != null && enemyComponent.IsPlayerDetected())
        {
            return true;
        }
        
        return false;
    }
    
    // 旋转武器朝向玩家
    private void RotateWeaponTowardsPlayer()
    {
        if (weaponTransform == null || playerTransform == null) return;
        
        // 计算朝向玩家的方向
        Vector2 direction = (playerTransform.position - weaponTransform.position).normalized;
        
        // 计算旋转角度
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        // 应用旋转
        weaponTransform.rotation = Quaternion.Euler(0, 0, angle);
        
        // 检查是否需要翻转Sprite
        CheckAndFlipSprite(angle);
        
        // 如果敌人有方向组件，同步更新敌人朝向
        if (enemyComponent != null)
        {
            enemyComponent.SetDirection(direction);
        }
    }
    
    // 检查并翻转Sprite
    private void CheckAndFlipSprite(float angle)
    {
        if (weaponSprite == null) return;
        
        // 标准化角度到-180~180范围
        if (angle > 180) angle -= 360;
        
        // 判断是否需要翻转
        bool shouldFlip = (angle > flipAngleThreshold || angle < -flipAngleThreshold);
        
        // 如果翻转状态改变，则执行翻转
        if (shouldFlip != isWeaponFlipped)
        {
            FlipWeaponSprite(shouldFlip);
            isWeaponFlipped = shouldFlip;
        }
    }
    
    // 翻转武器Sprite
    private void FlipWeaponSprite(bool flip)
    {
        if (weaponSprite == null) return;
        
        // 翻转Y轴
        Vector3 scale = weaponSprite.transform.localScale;
        scale.y = flip ? -Mathf.Abs(scale.y) : Mathf.Abs(scale.y);
        weaponSprite.transform.localScale = scale;
    }
    
    // 重置武器旋转
    private void ResetWeaponRotation()
    {
        if (weaponTransform == null) return;
        
        // 重置旋转
        weaponTransform.rotation = Quaternion.identity;
        
        // 如果已翻转，则恢复
        if (isWeaponFlipped)
        {
            FlipWeaponSprite(false);
            isWeaponFlipped = false;
        }
    }
    
}