using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombiePatrolState : IState
{
    private Zombie zombie; // 获取引用
    private Vector2 direction;
    // 构造函数
    public ZombiePatrolState(Zombie zombie)
    {
        this.zombie = zombie;
    }
    public void OnEnter()
    {
        GeneratePatrolPoints(); // 进入巡逻状态生成随机巡逻点
        zombie.animator.Play("Walk");
    }

    public void OnExit()
    {

    }

    public void OnFixedUpdate()
    {
        zombie.move();
    }

    public void OnUpdate()
    {
        // 判断是否会受伤
        if (zombie.isHurt) // 判断是否受伤，则进入受伤状态
        {
            zombie.transitionState(ZombieStateType.Hurt); // 受伤状态转换
        }

        // 在巡逻过程中，发现玩家则进入攻击状态
        zombie.GetPlayerTransform(); // 获取玩家位置
        if (zombie.player != null)
        {
            zombie.transitionState(ZombieStateType.Chase); // 进入追击状态
        }

        // 判断路径点表是否为空，若为空则重新计算路径点
        if (zombie.PathPointList == null || zombie.PathPointList.Count <= 0)
        {
            GeneratePatrolPoints(); // 随机生成巡逻点
        }
        else
        {
            // 当敌人到达路径点，则增加索引值并计算路径
            if (Vector2.Distance(zombie.transform.position, zombie.PathPointList[zombie.currentIndex]) <= 0.1f)
            {
                zombie.currentIndex++;
                if (zombie.currentIndex >= zombie.PathPointList.Count)
                {
                    zombie.transitionState(ZombieStateType.Idle); // 巡逻点结束后恢复待机状态
                }
                else // 敌人没有到达路径点，则移动
                {
                    direction = (zombie.PathPointList[zombie.currentIndex] - zombie.transform.position).normalized; // 获取当前路径点位置
                    zombie.MovementInput = direction; // 移动方向传递个输入
                }
            }
            else // 相撞处理
            {
                // 敌人当前速度小于默认速度，并敌人没有到达巡逻点
                if (zombie.rb.velocity.magnitude < zombie.CurrentSpeed /*检测速度*/ && zombie.currentIndex < zombie.PathPointList.Count /*检测索引*/)
                {
                    if (zombie.rb.velocity.magnitude == 0)// 如果敌人速度为0，并在索敌范围外
                    {
                        direction = (zombie.PathPointList[zombie.currentIndex] - zombie.transform.position).normalized; // 获取当前路径点位置
                        zombie.MovementInput = direction; // 移动方向传递个输入
                    }
                    else // 敌人相撞
                    {
                        zombie.transitionState(ZombieStateType.Idle);
                    }
                }
            }
            
        }
    }

    // 获得随机巡逻点
    public void GeneratePatrolPoints()
    {
        while (true)
        {
            int index = Random.Range(0, zombie.PatrolPoints.Length); // 赋予索引一个随机值，范围在巡逻点数组的长度内

            // 排除当前索引值不能相同
            if (zombie.targetPointsIndex != index)
            {
                zombie.targetPointsIndex = index;
                break;
            }
        }

        // 将巡逻点赋到寻路算法中
        zombie.GeneratePath(zombie.PatrolPoints[zombie.targetPointsIndex].position);
    }
}
