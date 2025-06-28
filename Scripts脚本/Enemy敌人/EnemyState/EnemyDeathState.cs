using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDeathState : IState
{
    // --- 控制器引用 ---
    Enemy enemy;
    // --- 构造函数 --- 
    public EnemyDeathState(Enemy enemy)
    {
        this.enemy = enemy;
    }
    // --- 状态方法 ---
    public void OnEnter()
    {

    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {

    }

    public void OnUpdate()
    {
 
    }
}
