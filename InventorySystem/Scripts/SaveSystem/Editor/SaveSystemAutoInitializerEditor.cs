using UnityEngine;
using UnityEditor;
using System.Linq;
using InventorySystem.SaveSystem;

/// <summary>
/// SaveSystemAutoInitializer的自定义编辑器
/// 提供可视化的配置界面和系统管理功能
/// </summary>
[CustomEditor(typeof(SaveSystemAutoInitializer))]
public class SaveSystemAutoInitializerEditor : Editor
{
    private SaveSystemAutoInitializer autoInitializer;
    private bool showInitializationConfig = true;
    private bool showComponentConfig = true;
    private bool showIntegrationConfig = true;
    private bool showSystemStatus = true;
    private bool showAdvancedOptions = false;
    private bool showInitializationStats = true;

    // 样式缓存
    private GUIStyle headerStyle;
    private GUIStyle boxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle statusStyle;

    private void OnEnable()
    {
        autoInitializer = (SaveSystemAutoInitializer)target;
    }

    public override void OnInspectorGUI()
    {
        // 初始化样式
        InitializeStyles();

        serializedObject.Update();

        // 标题
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("保存系统自动初始化器", headerStyle);
        EditorGUILayout.Space();

        // 系统状态显示
        DrawSystemStatus();

        EditorGUILayout.Space();

        // 初始化配置
        DrawInitializationConfig();

        EditorGUILayout.Space();

        // 组件配置
        DrawComponentConfig();

        EditorGUILayout.Space();

        // 集成配置
        DrawIntegrationConfig();

        EditorGUILayout.Space();

        // 初始化统计
        DrawInitializationStats();

        EditorGUILayout.Space();

        // 高级选项
        DrawAdvancedOptions();

        EditorGUILayout.Space();

        // 操作按钮
        DrawActionButtons();

        serializedObject.ApplyModifiedProperties();

        // 自动刷新
        if (Application.isPlaying)
        {
            Repaint();
        }
    }

    /// <summary>
    /// 初始化样式
    /// </summary>
    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (boxStyle == null)
        {
            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };
        }

        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 25
            };
        }

        if (statusStyle == null)
        {
            statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };
        }
    }

    /// <summary>
    /// 绘制系统状态
    /// </summary>
    private void DrawSystemStatus()
    {
        showSystemStatus = EditorGUILayout.Foldout(showSystemStatus, "系统状态", true);

        if (showSystemStatus)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // 初始化状态
            bool isInitialized = Application.isPlaying ? autoInitializer.IsSystemInitialized() : false;
            string statusText = isInitialized ? "已初始化" : "未初始化";
            Color statusColor = isInitialized ? Color.green : Color.yellow;

            var originalColor = GUI.color;
            GUI.color = statusColor;
            EditorGUILayout.LabelField("初始化状态:", statusText, statusStyle);
            GUI.color = originalColor;

            // 组件状态
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("组件状态:", EditorStyles.boldLabel);

                var saveManager = autoInitializer.GetSaveManager();
                var idManager = autoInitializer.GetIDManager();
                var deepIntegrator = autoInitializer.GetDeepIntegrator();
                var systemInitializer = autoInitializer.GetSystemInitializer();

                DrawComponentStatus("SaveManager", saveManager != null);
                DrawComponentStatus("ItemInstanceIDManager", idManager != null);
                DrawComponentStatus("DeepIntegrator", deepIntegrator != null);
                DrawComponentStatus("SystemInitializer", systemInitializer != null);
            }
            else
            {
                EditorGUILayout.HelpBox("运行时才能显示详细状态信息", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制组件状态
    /// </summary>
    private void DrawComponentStatus(string componentName, bool exists)
    {
        var originalColor = GUI.color;
        GUI.color = exists ? Color.green : Color.red;

        string status = exists ? "?" : "?";
        EditorGUILayout.LabelField($"  {status} {componentName}", exists ? "存在" : "缺失");

        GUI.color = originalColor;
    }

    /// <summary>
    /// 绘制初始化配置
    /// </summary>
    private void DrawInitializationConfig()
    {
        showInitializationConfig = EditorGUILayout.Foldout(showInitializationConfig, "初始化配置", true);

        if (showInitializationConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // 基础配置
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableAutoInitialization"), new GUIContent("启用自动初始化", "在场景启动时自动初始化保存系统"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableScenePersistence"), new GUIContent("启用场景持久化", "保持组件在场景切换时不被销毁"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("initializationDelay"), new GUIContent("初始化延迟(秒)", "延迟初始化的时间，确保其他组件先加载"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDebugLogging"), new GUIContent("启用调试日志", "在控制台输出详细的调试信息"));

            EditorGUILayout.Space(5);

            // 组件创建配置
            EditorGUILayout.LabelField("组件创建配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("createSaveManagerIfMissing"), new GUIContent("创建SaveManager", "如果场景中没有SaveManager则自动创建"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("createIDManagerIfMissing"), new GUIContent("创建IDManager", "如果场景中没有ItemInstanceIDManager则自动创建"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("createDeepIntegratorIfMissing"), new GUIContent("创建深度集成器", "如果场景中没有深度集成器则自动创建"));

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制组件配置
    /// </summary>
    private void DrawComponentConfig()
    {
        showComponentConfig = EditorGUILayout.Foldout(showComponentConfig, "组件配置", true);

        if (showComponentConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            // 组件配置开关
            EditorGUILayout.PropertyField(serializedObject.FindProperty("configureSaveManager"), new GUIContent("配置SaveManager", "自动配置SaveManager的参数"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("configureIDManager"), new GUIContent("配置IDManager", "自动配置ItemInstanceIDManager的参数"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("configureDeepIntegrator"), new GUIContent("配置深度集成器", "自动配置深度集成器的参数"));

            EditorGUILayout.Space(5);

            // 自动保存配置
            EditorGUILayout.LabelField("自动保存配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableAutoSave"), new GUIContent("启用自动保存", "定期自动保存游戏数据"));

            var enableAutoSave = serializedObject.FindProperty("enableAutoSave");
            if (enableAutoSave.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoSaveInterval"), new GUIContent("自动保存间隔(秒)", "自动保存的时间间隔"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制集成配置
    /// </summary>
    private void DrawIntegrationConfig()
    {
        showIntegrationConfig = EditorGUILayout.Foldout(showIntegrationConfig, "集成配置", true);

        if (showIntegrationConfig)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDeepIntegration"), new GUIContent("启用深度集成", "自动执行与现有系统的深度集成"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableBackwardCompatibility"), new GUIContent("启用向后兼容性", "保持与旧版本保存数据的兼容性"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableConflictDetection"), new GUIContent("启用冲突检测", "自动检测和解决ID冲突"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDataValidation"), new GUIContent("启用数据验证", "验证保存数据的完整性和有效性"));

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制初始化统计
    /// </summary>
    private void DrawInitializationStats()
    {
        showInitializationStats = EditorGUILayout.Foldout(showInitializationStats, "初始化统计", true);

        if (showInitializationStats)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            if (Application.isPlaying)
            {
                var stats = autoInitializer.GetInitializationStats();

                EditorGUILayout.LabelField("组件创建统计:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  SaveManager: {(stats.saveManagerCreated ? "已创建" : "未创建")}");
                EditorGUILayout.LabelField($"  IDManager: {(stats.idManagerCreated ? "已创建" : "未创建")}");
                EditorGUILayout.LabelField($"  深度集成器: {(stats.deepIntegratorCreated ? "已创建" : "未创建")}");
                EditorGUILayout.LabelField($"  系统初始化器: {(stats.systemInitializerCreated ? "已创建" : "未创建")}");

                EditorGUILayout.Space(5);

                EditorGUILayout.LabelField("配置统计:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  配置组件数量: {stats.componentsConfigured}");
                EditorGUILayout.LabelField($"  集成错误数量: {stats.integrationErrors}");

                if (!string.IsNullOrEmpty(stats.initializationTime))
                {
                    EditorGUILayout.LabelField($"  初始化时间: {stats.initializationTime}");
                }

                if (!string.IsNullOrEmpty(stats.lastError))
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("最后错误:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(stats.lastError, MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("运行时才能显示统计信息", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制高级选项
    /// </summary>
    private void DrawAdvancedOptions()
    {
        showAdvancedOptions = EditorGUILayout.Foldout(showAdvancedOptions, "高级选项", true);

        if (showAdvancedOptions)
        {
            EditorGUILayout.BeginVertical(boxStyle);

            EditorGUILayout.HelpBox("高级选项可能影响系统稳定性，请谨慎使用", MessageType.Warning);

            EditorGUILayout.Space(5);

            // 系统验证
            if (GUILayout.Button("验证系统完整性", buttonStyle))
            {
                ValidateSystemIntegrity();
            }

            // 重置初始化状态
            if (Application.isPlaying)
            {
                if (GUILayout.Button("重置初始化状态", buttonStyle))
                {
                    if (EditorUtility.DisplayDialog("确认重置", "确定要重置初始化状态吗？这将清除当前的初始化记录。", "确定", "取消"))
                    {
                        autoInitializer.ResetInitializationState();
                        Debug.Log("[SaveSystemAutoInitializerEditor] 初始化状态已重置");
                    }
                }
            }

            // 查找现有组件
            if (GUILayout.Button("查找现有组件", buttonStyle))
            {
                FindExistingComponents();
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 绘制操作按钮
    /// </summary>
    private void DrawActionButtons()
    {
        EditorGUILayout.BeginVertical(boxStyle);

        EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // 手动初始化按钮
        if (Application.isPlaying)
        {
            if (GUILayout.Button("手动初始化系统", buttonStyle))
            {
                try
                {
                    autoInitializer.ManualInitialization();
                    Debug.Log("[SaveSystemAutoInitializerEditor] 手动初始化完成");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SaveSystemAutoInitializerEditor] 手动初始化失败: {ex.Message}");
                }
            }
        }
        else
        {
            EditorGUILayout.HelpBox("需要在运行时才能执行初始化操作", MessageType.Info);
        }

        // 刷新界面按钮
        if (GUILayout.Button("刷新界面", buttonStyle))
        {
            Repaint();
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// 验证系统完整性
    /// </summary>
    private void ValidateSystemIntegrity()
    {
        Debug.Log("[SaveSystemAutoInitializerEditor] 开始验证系统完整性...");

        // 检查脚本文件是否存在
        string[] requiredScripts = {
            "SaveManager",
            "ItemInstanceIDManager",
            "ItemInstanceIDManagerDeepIntegrator",
            "SaveSystemInitializer",
            "SaveDataSerializer",
            "SaveFileManager"
        };

        bool allScriptsFound = true;
        foreach (string scriptName in requiredScripts)
        {
            var scriptAssets = AssetDatabase.FindAssets($"t:MonoScript {scriptName}");
            if (scriptAssets.Length == 0)
            {
                Debug.LogError($"[SaveSystemAutoInitializerEditor] 缺失脚本: {scriptName}");
                allScriptsFound = false;
            }
            else
            {
                Debug.Log($"[SaveSystemAutoInitializerEditor] 找到脚本: {scriptName}");
            }
        }

        if (Application.isPlaying)
        {
            bool systemValid = autoInitializer.ValidateSystemIntegrity();
            if (systemValid && allScriptsFound)
            {
                Debug.Log("[SaveSystemAutoInitializerEditor] 系统完整性验证通过");
                EditorUtility.DisplayDialog("验证结果", "系统完整性验证通过！", "确定");
            }
            else
            {
                Debug.LogWarning("[SaveSystemAutoInitializerEditor] 系统完整性验证失败");
                EditorUtility.DisplayDialog("验证结果", "系统完整性验证失败，请检查控制台日志。", "确定");
            }
        }
        else
        {
            if (allScriptsFound)
            {
                Debug.Log("[SaveSystemAutoInitializerEditor] 脚本文件验证通过（运行时验证需要在Play模式下进行）");
                EditorUtility.DisplayDialog("验证结果", "脚本文件验证通过！\n运行时验证需要在Play模式下进行。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("验证结果", "发现缺失的脚本文件，请检查控制台日志。", "确定");
            }
        }
    }

    /// <summary>
    /// 查找现有组件
    /// </summary>
    private void FindExistingComponents()
    {
        Debug.Log("[SaveSystemAutoInitializerEditor] 查找现有组件...");

        var saveManagers = FindObjectsOfType<SaveManager>();
        var idManagers = FindObjectsOfType<ItemInstanceIDManager>();
        var deepIntegrators = FindObjectsOfType<ItemInstanceIDManagerDeepIntegrator>();
        var systemInitializers = FindObjectsOfType<SaveSystemInitializer>();
        var autoInitializers = FindObjectsOfType<SaveSystemAutoInitializer>();

        Debug.Log($"[SaveSystemAutoInitializerEditor] 找到组件数量:");
        Debug.Log($"  SaveManager: {saveManagers.Length}");
        Debug.Log($"  ItemInstanceIDManager: {idManagers.Length}");
        Debug.Log($"  ItemInstanceIDManagerDeepIntegrator: {deepIntegrators.Length}");
        Debug.Log($"  SaveSystemInitializer: {systemInitializers.Length}");
        Debug.Log($"  SaveSystemAutoInitializer: {autoInitializers.Length}");

        // 检查重复组件
        if (autoInitializers.Length > 1)
        {
            Debug.LogWarning($"[SaveSystemAutoInitializerEditor] 发现{autoInitializers.Length}个SaveSystemAutoInitializer实例，建议只保留一个");
        }

        string summary = $"组件查找完成:\n" +
                        $"SaveManager: {saveManagers.Length}\n" +
                        $"ItemInstanceIDManager: {idManagers.Length}\n" +
                        $"DeepIntegrator: {deepIntegrators.Length}\n" +
                        $"SystemInitializer: {systemInitializers.Length}\n" +
                        $"AutoInitializer: {autoInitializers.Length}";

        EditorUtility.DisplayDialog("组件查找结果", summary, "确定");
    }
}