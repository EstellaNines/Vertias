using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SceneTransitionController : MonoBehaviour
{
    [Header("场景切换系统组件")]
    [Tooltip("场景管理器组件")]
    public SceneManagers sceneManagers;

    [Tooltip("场景加载器组件")]
    public SceneLoader sceneLoader;

    [Tooltip("随机背景控制器")]
    public RandomBackground randomBackground;

    void Awake()
    {
        // 自动查找组件（如果未手动设置）
        AutoFindComponents();
    }

    private void AutoFindComponents()
    {
        // 自动查找SceneManagers组件
        if (sceneManagers == null)
            sceneManagers = GetComponentInChildren<SceneManagers>();

        // 自动查找SceneLoader组件
        if (sceneLoader == null)
            sceneLoader = GetComponentInChildren<SceneLoader>();

        // 自动查找RandomBackground组件
        if (randomBackground == null)
            randomBackground = GetComponentInChildren<RandomBackground>();

        // 验证组件
        ValidateComponents();
    }

    private void ValidateComponents()
    {
        if (sceneManagers == null)
            Debug.LogWarning("[SceneTransitionController] SceneManagers组件未找到");

        if (sceneLoader == null)
            Debug.LogWarning("[SceneTransitionController] SceneLoader组件未找到");

        if (randomBackground == null)
            Debug.LogWarning("[SceneTransitionController] RandomBackground组件未找到");
    }

    // 公共方法：通过UI按钮切换场景
    public void SwitchSceneByButton(string sceneName)
    {
        if (sceneLoader != null)
        {
            Debug.Log($"通过UI按钮切换到场景: {sceneName}");
            sceneLoader.TransitionToScene(sceneName);
        }
        else
        {
            Debug.LogError("SceneLoader组件未设置，无法切换场景");
        }
    }

    // 公共方法：直接切换场景
    public void DirectSceneTransition(string sceneName)
    {
        SwitchSceneByButton(sceneName);
    }

    // 公共方法：获取场景管理器
    public SceneManagers GetSceneManagers()
    {
        return sceneManagers;
    }

    // 公共方法：获取场景加载器
    public SceneLoader GetSceneLoader()
    {
        return sceneLoader;
    }

    // 公共方法：获取随机背景控制器
    public RandomBackground GetRandomBackground()
    {
        return randomBackground;
    }
}
