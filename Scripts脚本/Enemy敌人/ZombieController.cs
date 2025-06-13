using System.Collections;
using UnityEngine;

public class ZombieController : MonoBehaviour
{
    [Header("属性")]
    [SerializeField] private float CurrentSpeed = 0;
    [Header("碰撞检测")]
    [SerializeField] private CapsuleCollider2D physicalCollider; // 物理碰撞体

    public Vector2 MovementInput { get; set; }
    [Header("攻击")]
    //[SerializeField] private float AttackCoolDuration = 0.5f;
    private BoxCollider2D attackCollider; // 攻击触发器

    [Header("死亡视觉效果")]
    public Sprite deadSpriteLeft;  // 左方向死亡精灵
    public Sprite deadSpriteRight; // 右方向死亡精灵
    private float currentHorizontalDirection; // 缓存最后一次水平方向

    private Rigidbody2D rb;
    private Animator Animator;
    private bool isDead;
    private SpriteRenderer spriteRenderer;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        physicalCollider = GetComponent<CapsuleCollider2D>();
        attackCollider = GetComponent<BoxCollider2D>();
    }

    private void FixedUpdate()
    {
        if (!isDead)
            move();
        SetAnimation();
    }

    void move()
    {
        // 检查 rb 是否存在且未死亡
        if (isDead || rb == null)
            return;

        if (MovementInput.magnitude > 0.1f && CurrentSpeed >= 0)
        {
            Vector2 targetPosition = (Vector2)transform.position + MovementInput * CurrentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
            float horizontalDirection = MovementInput.x > 0 ? 1.0f : 0.0f;
            currentHorizontalDirection = horizontalDirection;
            Animator.SetFloat("Horizontial", horizontalDirection);
        }
        else
        {
            rb.velocity = Vector2.zero;
        }
    }

    public void attack()
    {
        // 检查 rb 是否存在且未死亡
        if (isDead || rb == null)
            return;

        MovementInput = Vector2.zero;
        rb.velocity = Vector2.zero;
        Animator.SetTrigger("attackTrigger");

    }

    public void EnemyHurt()
    {
        Animator.SetTrigger("HurtTrigger");
    }

    public void EnemyDead()
    {
        isDead = true;
        Animator.SetFloat("Speed", 0f);
        Animator.SetBool("isDie", true);
        // 禁用 Animator 避免动画覆盖设置
        if (Animator != null)
        {
            Animator.enabled = false;
        }

        // 替换精灵图片
        spriteRenderer.sprite = currentHorizontalDirection > 0.5f
            ? deadSpriteRight
            : deadSpriteLeft;
        // 设置颜色为灰色
        spriteRenderer.color = new Color(0.5f, 0.5f, 0.5f); // 灰色
        // 绕 Z 轴旋转 -90 度
        transform.eulerAngles = new Vector3(0, 0, -90f);
        spriteRenderer.sortingLayerName = "Death";
        // 触发死亡动画后清理
        StartCoroutine(CleanupAfterAnimation());
    }

    private IEnumerator CleanupAfterAnimation()
    {
        // 等待死亡动画播放完成（假设动画长度为 1 秒）
        yield return new WaitForSeconds(1f);

        // 仅销毁物理组件
        if (rb != null)
        {
            Destroy(rb); // 销毁刚体
        }

        Collider2D collider = GetComponent<Collider2D>();
        // 禁用两种碰撞体
        if (physicalCollider != null)
        {
            physicalCollider.enabled = false;
        }

        if (attackCollider != null)
        {
            attackCollider.enabled = false;
        }

    }

    void SetAnimation()
    {
        // 安全访问检查
        if (Animator == null || !Animator.isActiveAndEnabled || isDead)
            return;

        // 仅在非攻击状态更新 Speed 参数
        if (!Animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
        {
            Animator.SetFloat("Speed", MovementInput.magnitude);
        }
    }



}
