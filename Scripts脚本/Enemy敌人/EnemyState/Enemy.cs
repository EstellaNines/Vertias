using System.Collections.Generic;
using UnityEngine;
using Pathfinding;


// 敌人状态枚举
public enum EnemyState
{
    Idle/*待机*/, Move/*移动*/, Attack/*攻击*/, Hurt/*受伤*/, Dead/*死亡*/, Patrol/*巡逻*/, Aim/*瞄准*/
}
public class Enemy : MonoBehaviour
{
    // --- 基础属性 ---
    [Header("移动")]
    [FieldLabel("移动速度")] public float MoveSpeed = 3f; // 移动速度
    [HideInInspector] public float currentSpeed; // 当前速度

    // --- 内部属性 --- 
    [HideInInspector] public Rigidbody2D RB; // 刚体
    [HideInInspector] public Animator animator; // 动画器
    [HideInInspector] public Collider2D Collider; // 碰撞器
    [HideInInspector] public Seeker seeker; // 寻路
    private IState currentState; // 当前状态
    private Dictionary<EnemyState, IState> states = new Dictionary<EnemyState, IState>(); // 状态字典

    // --- 函数 ---
    private void Awake()
    {
        // 组件引用
        RB = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        Collider = GetComponent<Collider2D>();
        seeker = GetComponent<Seeker>();

        InitalStateMachine(); // 初始化状态机
        transitionState(EnemyState.Idle); // 默认为待机状态
    }

    // 初始化状态
    private void InitalStateMachine()
    {
        // 创建状态
        states.Add(EnemyState.Idle, new EnemyIdleState(this)); // 待机状态
        states.Add(EnemyState.Move, new EnemyMoveState(this)); // 移动状态
        states.Add(EnemyState.Attack, new EnemyAttackState(this)); // 攻击状态
        states.Add(EnemyState.Hurt, new EnemyHurtState(this)); // 受伤状态
        states.Add(EnemyState.Dead, new EnemyDeathState(this)); // 死亡状态
        states.Add(EnemyState.Patrol, new EnemyPatrolState(this)); // 巡逻状态
        states.Add(EnemyState.Aim, new EnemyAimState(this)); // 瞄准状态
    }
    // 状态转换
    public void transitionState(EnemyState type)
    {
        // 当前状态是否为空
        // 当前状态不为空，退出当前状态
        if (currentState != null)
        {
            currentState.OnExit();
        }
        // 通过字典的键找到对应的状态
        currentState = states[type];
        currentState.OnEnter();
    }

    private void Update()
    {
        currentState.OnUpdate();
    }
    private void FixedUpdate()
    {
        currentState.OnFixedUpdate();
    }
}
