using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SceneManagers : MonoBehaviour
{
    [Header("场景管理设置")]
    [Tooltip("场景切换快捷键映射")]
    public List<SceneMapping> sceneMappings = new List<SceneMapping>();

    [System.Serializable]
    public class SceneMapping
    {
        public string sceneName;
        public string displayName;
        public KeyCode hotkey;
    }

    private PlayerInputAction inputActions;
    private SceneTransitionController transitionController;

    void Awake()
    {
        inputActions = new PlayerInputAction();

        // 获取父对象的SceneTransitionController
        transitionController = GetComponentInParent<SceneTransitionController>();
        if (transitionController == null)
        {
            Debug.LogWarning("未找到SceneTransitionController组件");
        }
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Update()
    {
        // 检查快捷键输入
        foreach (var mapping in sceneMappings)
        {
            if (Input.GetKeyDown(mapping.hotkey))
            {
                SwitchToScene(mapping.sceneName);
            }
        }
    }

    public void SwitchToScene(string sceneName)
    {
        if (transitionController != null)
        {
            transitionController.DirectSceneTransition(sceneName);
        }
        else
        {
            // 备用方案：直接使用SceneLoader
            SceneLoader.SwitchScene(sceneName);
        }
    }

    // UI按钮调用的方法
    public void SwitchToSceneByName(string sceneName)
    {
        SwitchToScene(sceneName);
    }

    void OnDestroy()
    {
        if (inputActions != null)
        {
            inputActions.Dispose();
        }
    }
}
