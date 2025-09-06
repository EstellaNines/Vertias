using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// 插件引用修复工具 - 修复插件重组后的路径引用问题
/// Plugin Reference Fixer - Fix path reference issues after plugin reorganization
/// </summary>
public class PluginReferenceFixer : EditorWindow
{
    [MenuItem("Tools/Fix Plugin References")]
    public static void ShowWindow()
    {
        GetWindow<PluginReferenceFixer>("插件引用修复");
    }
    
    void OnGUI()
    {
        EditorGUILayout.LabelField("插件引用修复工具 | Plugin Reference Fixer", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox("此工具将修复插件重组后可能出现的引用问题。", MessageType.Info);
        EditorGUILayout.Space();
        
        if (GUILayout.Button("修复所有插件引用 | Fix All Plugin References"))
        {
            FixAllReferences();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("重新导入所有插件 | Reimport All Plugins"))
        {
            ReimportAllPlugins();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("刷新程序集 | Refresh Assemblies"))
        {
            RefreshAssemblies();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("如果插件仍有问题，请尝试以下步骤：\n1. 关闭Unity\n2. 删除Library文件夹\n3. 重新打开Unity项目", MessageType.Warning);
    }
    
    void FixAllReferences()
    {
        Debug.Log("开始修复插件引用...");
        
        try
        {
            // 修复meta文件中的旧路径引用
            FixMetaFileReferences();
            
            // 修复程序集定义文件
            FixAssemblyDefinitions();
            
            // 重新导入插件文件夹
            AssetDatabase.ImportAsset("Assets/ThirdPartyPlugins第三方插件", ImportAssetOptions.ImportRecursive);
            
            Debug.Log("插件引用修复完成！");
            EditorUtility.DisplayDialog("修复完成", "插件引用修复完成！建议重启Unity以确保所有更改生效。", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"修复插件引用时出错: {e.Message}");
            EditorUtility.DisplayDialog("修复失败", $"修复过程中出现错误: {e.Message}", "确定");
        }
    }
    
    void FixMetaFileReferences()
    {
        Debug.Log("正在修复meta文件引用...");
        
        string pluginsPath = "Assets/ThirdPartyPlugins第三方插件";
        string[] metaFiles = Directory.GetFiles(pluginsPath, "*.meta", SearchOption.AllDirectories);
        
        int fixedCount = 0;
        
        foreach (string metaFile in metaFiles)
        {
            try
            {
                string content = File.ReadAllText(metaFile);
                string originalContent = content;
                
                // 修复常见的旧路径引用
                content = content.Replace("Assets/Plugins/", "Assets/ThirdPartyPlugins第三方插件/");
                content = content.Replace("Assets/AstarPathfindingProject/", "Assets/ThirdPartyPlugins第三方插件/Pathfinding寻路算法/AstarPathfindingProject/");
                content = content.Replace("Assets/ConsolePro/", "Assets/ThirdPartyPlugins第三方插件/DeveloperTools开发工具/ConsolePro/");
                content = content.Replace("Assets/TextMesh Pro/", "Assets/ThirdPartyPlugins第三方插件/UIFramework界面框架/TextMesh Pro/");
                content = content.Replace("Assets/vFolders/", "Assets/ThirdPartyPlugins第三方插件/EditorEnhancements编辑器增强/vFolders/");
                content = content.Replace("Assets/vHierarchy/", "Assets/ThirdPartyPlugins第三方插件/EditorEnhancements编辑器增强/vHierarchy/");
                content = content.Replace("Assets/vInspector/", "Assets/ThirdPartyPlugins第三方插件/EditorEnhancements编辑器增强/vInspector/");
                content = content.Replace("Assets/vTabs/", "Assets/ThirdPartyPlugins第三方插件/EditorEnhancements编辑器增强/vTabs/");
                content = content.Replace("Assets/Wingman/", "Assets/ThirdPartyPlugins第三方插件/DeveloperTools开发工具/Wingman/");
                
                if (content != originalContent)
                {
                    File.WriteAllText(metaFile, content);
                    fixedCount++;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"修复meta文件 {metaFile} 时出错: {e.Message}");
            }
        }
        
        Debug.Log($"修复了 {fixedCount} 个meta文件的引用");
    }
    
    void FixAssemblyDefinitions()
    {
        Debug.Log("正在检查程序集定义文件...");
        
        // 程序集定义文件通常不需要修复路径，因为它们使用相对路径
        // 但我们可以验证它们是否存在并正确
        
        string[] asmdefFiles = Directory.GetFiles("Assets/ThirdPartyPlugins第三方插件", "*.asmdef", SearchOption.AllDirectories);
        
        foreach (string asmdefFile in asmdefFiles)
        {
            try
            {
                string content = File.ReadAllText(asmdefFile);
                Debug.Log($"找到程序集定义文件: {asmdefFile}");
                
                // 验证JSON格式是否正确
                JsonUtility.FromJson<object>(content);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"程序集定义文件 {asmdefFile} 可能已损坏: {e.Message}");
            }
        }
        
        Debug.Log($"检查了 {asmdefFiles.Length} 个程序集定义文件");
    }
    
    void ReimportAllPlugins()
    {
        Debug.Log("正在重新导入所有插件...");
        
        try
        {
            AssetDatabase.ImportAsset("Assets/ThirdPartyPlugins第三方插件", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            
            Debug.Log("插件重新导入完成！");
            EditorUtility.DisplayDialog("重新导入完成", "所有插件已重新导入！", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"重新导入插件时出错: {e.Message}");
            EditorUtility.DisplayDialog("重新导入失败", $"重新导入过程中出现错误: {e.Message}", "确定");
        }
    }
    
    void RefreshAssemblies()
    {
        Debug.Log("正在刷新程序集...");
        
        try
        {
            // 强制重新编译所有脚本
            AssetDatabase.Refresh();
            EditorUtility.RequestScriptReload();
            
            Debug.Log("程序集刷新完成！");
            EditorUtility.DisplayDialog("刷新完成", "程序集已刷新！Unity将重新编译所有脚本。", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"刷新程序集时出错: {e.Message}");
            EditorUtility.DisplayDialog("刷新失败", $"刷新过程中出现错误: {e.Message}", "确定");
        }
    }
}
