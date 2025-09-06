using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// 插件健康检查工具 - 检测插件重组后是否正常工作
/// Plugin Health Check Tool - Detect if plugins work properly after reorganization
/// </summary>
public class PluginHealthCheck : EditorWindow
{
    private Vector2 scrollPosition;
    private List<PluginStatus> pluginStatuses = new List<PluginStatus>();
    
    [MenuItem("Tools/Plugin Health Check")]
    public static void ShowWindow()
    {
        GetWindow<PluginHealthCheck>("插件健康检查");
    }
    
    void OnEnable()
    {
        CheckAllPlugins();
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("插件健康检查报告 | Plugin Health Check Report", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("重新检查 | Refresh Check"))
        {
            CheckAllPlugins();
        }
        
        EditorGUILayout.Space();
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        foreach (var status in pluginStatuses)
        {
            EditorGUILayout.BeginHorizontal();
            
            // 状态图标
            GUIStyle statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.normal.textColor = status.IsWorking ? Color.green : Color.red;
            
            EditorGUILayout.LabelField(status.IsWorking ? "�7�3" : "�7�4", statusStyle, GUILayout.Width(30));
            EditorGUILayout.LabelField(status.PluginName, GUILayout.Width(200));
            EditorGUILayout.LabelField(status.Status, GUILayout.ExpandWidth(true));
            
            EditorGUILayout.EndHorizontal();
            
            if (!status.IsWorking && !string.IsNullOrEmpty(status.ErrorMessage))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("", GUILayout.Width(30));
                EditorGUILayout.LabelField($"错误: {status.ErrorMessage}", EditorStyles.helpBox);
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.Space(5);
        }
        
        EditorGUILayout.EndScrollView();
        
        // 总结信息
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("总结 | Summary", EditorStyles.boldLabel);
        int workingCount = pluginStatuses.Count(p => p.IsWorking);
        int totalCount = pluginStatuses.Count;
        
        GUIStyle summaryStyle = new GUIStyle(GUI.skin.label);
        summaryStyle.normal.textColor = workingCount == totalCount ? Color.green : Color.yellow;
        
        EditorGUILayout.LabelField($"正常工作: {workingCount}/{totalCount} | Working: {workingCount}/{totalCount}", summaryStyle);
    }
    
    void CheckAllPlugins()
    {
        pluginStatuses.Clear();
        
        // 检查DOTween
        CheckDOTween();
        
        // 检查A*寻路
        CheckAstarPathfinding();
        
        // 检查Easy Save 3
        CheckEasySave3();
        
        // 检查Ultimate Editor Enhancer
        CheckUltimateEditorEnhancer();
        
        // 检查Better Hierarchy
        CheckBetterHierarchy();
        
        // 检查TextMesh Pro
        CheckTextMeshPro();
        
        // 检查v系列工具
        CheckVTools();
        
        // 检查ConsolePro
        CheckConsolePro();
        
        // 检查Wingman
        CheckWingman();
        
        // 检查Easy Framerate Counter
        CheckEasyFramerateCounter();
    }
    
    void CheckDOTween()
    {
        try
        {
            var dotweenType = System.Type.GetType("DG.Tweening.DOTween, DOTween");
            bool isWorking = dotweenType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "DOTween",
                IsWorking = isWorking,
                Status = isWorking ? "动画系统正常" : "未找到DOTween类型",
                ErrorMessage = isWorking ? "" : "DOTween程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "DOTween",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckAstarPathfinding()
    {
        try
        {
            var astarType = System.Type.GetType("Pathfinding.AstarPath, AstarPathfindingProject");
            bool isWorking = astarType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "A* Pathfinding",
                IsWorking = isWorking,
                Status = isWorking ? "寻路系统正常" : "未找到A*类型",
                ErrorMessage = isWorking ? "" : "A*寻路程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "A* Pathfinding",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckEasySave3()
    {
        try
        {
            var es3Type = System.Type.GetType("ES3, Easy Save 3");
            bool isWorking = es3Type != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Easy Save 3",
                IsWorking = isWorking,
                Status = isWorking ? "存档系统正常" : "未找到ES3类型",
                ErrorMessage = isWorking ? "" : "Easy Save 3程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Easy Save 3",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckUltimateEditorEnhancer()
    {
        try
        {
            var ueueType = System.Type.GetType("InfinityCode.UltimateEditorEnhancer.EditorMenus, Ultimate Editor Enhancer-Editor");
            bool isWorking = ueueType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Ultimate Editor Enhancer",
                IsWorking = isWorking,
                Status = isWorking ? "编辑器增强正常" : "未找到UEE类型",
                ErrorMessage = isWorking ? "" : "Ultimate Editor Enhancer程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Ultimate Editor Enhancer",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckBetterHierarchy()
    {
        try
        {
            var bhType = System.Type.GetType("BetterHierarchy.BetterHierarchyIconDisplayer, BetterHierarchy");
            bool isWorking = bhType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Better Hierarchy",
                IsWorking = isWorking,
                Status = isWorking ? "层级增强正常" : "未找到BH类型",
                ErrorMessage = isWorking ? "" : "Better Hierarchy程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Better Hierarchy",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckTextMeshPro()
    {
        try
        {
            var tmpType = System.Type.GetType("TMPro.TextMeshPro, Unity.TextMeshPro");
            bool isWorking = tmpType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "TextMesh Pro",
                IsWorking = isWorking,
                Status = isWorking ? "文本渲染正常" : "未找到TMP类型",
                ErrorMessage = isWorking ? "" : "TextMesh Pro程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "TextMesh Pro",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckVTools()
    {
        // 检查vFolders
        try
        {
            var vfType = System.Type.GetType("VFolders.VFolders, VFolders");
            bool vfWorking = vfType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "vFolders",
                IsWorking = vfWorking,
                Status = vfWorking ? "文件夹工具正常" : "未找到vFolders类型",
                ErrorMessage = vfWorking ? "" : "vFolders程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "vFolders",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
        
        // 检查vHierarchy
        try
        {
            var vhType = System.Type.GetType("VHierarchy.VHierarchy, VHierarchy");
            bool vhWorking = vhType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "vHierarchy",
                IsWorking = vhWorking,
                Status = vhWorking ? "层级工具正常" : "未找到vHierarchy类型",
                ErrorMessage = vhWorking ? "" : "vHierarchy程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "vHierarchy",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckConsolePro()
    {
        try
        {
            bool hasConsoleProScript = File.Exists("Assets/ThirdPartyPlugins第三方插件/DeveloperTools开发工具/ConsolePro/ConsoleProDebug.cs");
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "ConsolePro",
                IsWorking = hasConsoleProScript,
                Status = hasConsoleProScript ? "控制台工具正常" : "未找到ConsolePro脚本",
                ErrorMessage = hasConsoleProScript ? "" : "ConsolePro文件可能丢失"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "ConsolePro",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckWingman()
    {
        try
        {
            var wingmanType = System.Type.GetType("Wingman.Wingman, Wingman");
            bool isWorking = wingmanType != null;
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Wingman",
                IsWorking = isWorking,
                Status = isWorking ? "辅助工具正常" : "未找到Wingman类型",
                ErrorMessage = isWorking ? "" : "Wingman程序集可能未正确加载"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Wingman",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    void CheckEasyFramerateCounter()
    {
        try
        {
            bool hasEFCScript = Directory.Exists("Assets/ThirdPartyPlugins第三方插件/PerformanceMonitoring性能监控/Alterego Games/Easy Framerate Counter");
            
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Easy Framerate Counter",
                IsWorking = hasEFCScript,
                Status = hasEFCScript ? "性能监控正常" : "未找到EFC文件夹",
                ErrorMessage = hasEFCScript ? "" : "Easy Framerate Counter文件可能丢失"
            });
        }
        catch (System.Exception e)
        {
            pluginStatuses.Add(new PluginStatus
            {
                PluginName = "Easy Framerate Counter",
                IsWorking = false,
                Status = "检查失败",
                ErrorMessage = e.Message
            });
        }
    }
    
    [System.Serializable]
    public class PluginStatus
    {
        public string PluginName;
        public bool IsWorking;
        public string Status;
        public string ErrorMessage;
    }
}
