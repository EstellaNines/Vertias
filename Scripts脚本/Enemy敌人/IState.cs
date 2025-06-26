using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    // 进入状态
    void OnEnter();

    // 更新状态
    void OnUpdate();

    // 固定更新状态
    void OnFixedUpdate();

    // 退出状态
    void OnExit();
}
