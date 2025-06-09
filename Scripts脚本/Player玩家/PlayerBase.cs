using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(BoxCollider2D))]
public class PlayerBase : MonoBehaviour
{
    #region 基础属性
    [Header("角色属性")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float maxInfection = 100f;

    private float currentHealth;
    private float currentHunger;
    private float currentInfection;

    // 属性变化事件
    public event System.Action<float> OnHealthChanged;
    public event System.Action<float> OnHungerChanged;
    public event System.Action<float> OnInfectionChanged;
    public float CurrentHealth => currentHealth;
    #endregion

    #region 组件引用
    protected Rigidbody2D RB2D { get; private set; }
    protected Animator Animator { get; private set; }

    // 功能模块引用
    public PlayerController Controller { get; private set; }
    public PlayerFire Fire { get; private set; }
    public PlayerAim Aim { get; private set; }
    public PlayerPerspectiveChange Perspective { get; private set; }
    public PlayerPickItems PickItems { get; private set; }

    // 手部位置缓存
    private Transform handTransform;
    #endregion

    private void Awake()
    {
        // 初始化属性
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        currentInfection = maxInfection;

        // 获取核心组件
        RB2D = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();

        // 获取功能组件
        Controller = GetComponent<PlayerController>();
        Fire = GetComponent<PlayerFire>();
        Aim = GetComponent<PlayerAim>();
        Perspective = GetComponent<PlayerPerspectiveChange>();
        PickItems = GetComponent<PlayerPickItems>();

        // 获取手部位置
        // if (PickItems != null)
        //     handTransform = PickItems.handTransform;
    }

    #region 属性管理方法
    public void ChangeHealth(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }

    public void ChangeHunger(float amount)
    {
        currentHunger = Mathf.Clamp(currentHunger + amount, 0, maxHunger);
        OnHungerChanged?.Invoke(currentHunger);
    }

    public void ChangeInfection(float amount)
    {
        currentInfection = Mathf.Clamp(currentInfection + amount, 0, maxInfection);
        OnInfectionChanged?.Invoke(currentInfection);
    }

    // 获取属性百分比
    public float GetHealthRatio() => currentHealth / maxHealth;
    public float GetHungerRatio() => currentHunger / maxHunger;
    public float GetInfectionRatio() => currentInfection / maxInfection;
    #endregion

    // 获取手部位置
    public Transform GetHandTransform() => handTransform;
}