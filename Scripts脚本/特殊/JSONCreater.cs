using UnityEngine;
using UnityEditor;
using System.IO;
public class JSONCreater : MonoBehaviour
{
    // 创建空的JSON对象
    [MenuItem("Assets/Create/JSON Templates/Empty Object", false, 81)]
    public static void CreateEmptyJSONObject()
    {
        CreateJSONFileWithTemplate("EmptyObject.json", @"{
    
}");
    }

    // 创建空的JSON数组
    [MenuItem("Assets/Create/JSON Templates/Empty Array", false, 82)]
    public static void CreateEmptyJSONArray()
    {
        CreateJSONFileWithTemplate("EmptyArray.json", @"[
    
]");
    }

    // 创建基础配置JSON模板
    [MenuItem("Assets/Create/JSON Templates/Basic Config", false, 83)]
    public static void CreateBasicConfigJSON()
    {
        string template = @"{
    ""name"": """",
    ""id"": 0,
    ""enabled"": true
}";
        CreateJSONFileWithTemplate("Config.json", template);
    }

    // 创建数据列表JSON模板
    [MenuItem("Assets/Create/JSON Templates/Data List", false, 84)]
    public static void CreateDataListJSON()
    {
        string template = @"{
    ""data"": [
        {
            ""id"": 0,
            ""name"": """"
        }
    ]
}";
        CreateJSONFileWithTemplate("DataList.json", template);
    }

    // 创建设置JSON模板
    [MenuItem("Assets/Create/JSON Templates/Settings", false, 85)]
    public static void CreateSettingsJSON()
    {
        string template = @"{
    ""settings"": {
        ""key"": ""value""
    }
}";
        CreateJSONFileWithTemplate("Settings.json", template);
    }

    // 通用的JSON文件创建方法
    private static void CreateJSONFileWithTemplate(string defaultFileName, string template)
    {
        // 获取当前选中的文件夹路径
        string selectedPath = GetSelectedPathOrFallback();

        // 创建文件路径
        string fullPath = Path.Combine(selectedPath, defaultFileName);

        // 确保文件名唯一
        fullPath = AssetDatabase.GenerateUniqueAssetPath(fullPath);

        // 写入文件
        File.WriteAllText(fullPath, template);

        // 刷新AssetDatabase以显示新文件
        AssetDatabase.Refresh();

        // 选中新创建的文件
        Object newAsset = AssetDatabase.LoadAssetAtPath<Object>(fullPath);
        Selection.activeObject = newAsset;
        EditorGUIUtility.PingObject(newAsset);

        // 进入重命名模式
        EditorApplication.delayCall += () =>
        {
            EditorApplication.ExecuteMenuItem("Assets/Rename");
        };

        Debug.Log($"JSON文件已创建: {fullPath}");
    }

    // 获取当前选中的路径
    private static string GetSelectedPathOrFallback()
    {
        string path = "Assets";

        foreach (Object obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                break;
            }
            else if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            {
                break;
            }
        }

        return path;
    }
}
