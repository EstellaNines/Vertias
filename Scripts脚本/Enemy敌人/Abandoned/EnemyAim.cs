using UnityEngine;

public class EnemyAim : MonoBehaviour
{
    [Header("检测设置")]
    public float DetectionRdaius = 5f; //检测范围
    public LayerMask layerMask; //检测图层

    [Header("武器设置")]
    [SerializeField] private Transform weapon; // 武器引用
    [SerializeField] private Transform targetPlayer; // 玩家引用
    private bool isDetecting = true; // 检测状态切换
    public bool isAiming = false; // 瞄准状态切换
    private void Awake()
    {
        CircleCollider2D circleCollider2D = GetComponent<CircleCollider2D>();
        if (circleCollider2D != null)
        {
            circleCollider2D.radius = DetectionRdaius; // 设置半径为检测范围半径
            circleCollider2D.isTrigger = true; // 设置为触发器
        }
    }

    private void Update()
    {
        // 如果有目标玩家且武器存在，持续瞄准
        if (targetPlayer != null && weapon != null)
        {
            AimAtPlayer();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDetecting && layerMask.value == (layerMask.value | (1 << other.gameObject.layer)))
        {
            Debug.Log("进入范围");
            targetPlayer = other.transform; // 记录玩家引用
            isAiming = true; // 启用瞄准状态
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (isDetecting && layerMask.value == (layerMask.value | (1 << other.gameObject.layer)))
        {
            Debug.Log("离开范围");
            targetPlayer = null; // 清除玩家引用
            isAiming = false; // 停止瞄准
        }
    }

    // 瞄准方法
    private void AimAtPlayer()
    {
        Vector2 direction = targetPlayer.position - weapon.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        weapon.rotation = Quaternion.Euler(0, 0, angle);
    }
}
