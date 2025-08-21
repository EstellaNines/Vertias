using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace EditorTools
{
    /// <summary>
    /// 编码转换器 - 用于检查和转换项目中所有脚本文件的编码为UTF-8
    /// </summary>
    public class EncodingConverter : EditorWindow
    {
        private Vector2 scrollPosition;
        private List<string> scriptFiles = new List<string>();
        private List<EncodingInfo> encodingInfos = new List<EncodingInfo>();
        private bool isScanning = false;
        private bool hasScanned = false;

        /// <summary>
        /// 编码信息结构体
        /// </summary>
        [System.Serializable]
        public struct EncodingInfo
        {
            public string filePath;      // 文件路径
            public string encodingName;  // 编码名称
            public bool isUTF8;         // 是否为UTF-8编码
            public bool needsConversion; // 是否需要转换
        }

        /// <summary>
        /// 在Unity菜单中添加工具入口
        /// </summary>
        [MenuItem("Tools/编码转换器/检查脚本编码")]
        public static void ShowWindow()
        {
            EncodingConverter window = GetWindow<EncodingConverter>("脚本编码转换器");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        /// <summary>
        /// 绘制编辑器窗口界面
        /// </summary>
        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 标题
            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Unity项目脚本编码转换器", titleStyle);

            EditorGUILayout.Space(10);

            // 说明文字
            EditorGUILayout.HelpBox("此工具用于检查项目中所有C#脚本文件的编码格式，并可将非UTF-8编码的文件转换为UTF-8编码（不改变文件内容）。", MessageType.Info);

            EditorGUILayout.Space(10);

            // 操作按钮区域
            EditorGUILayout.BeginHorizontal();

            // 扫描按钮
            GUI.enabled = !isScanning;
            if (GUILayout.Button("扫描所有脚本文件", GUILayout.Height(30)))
            {
                ScanAllScripts();
            }

            // 转换按钮
            GUI.enabled = hasScanned && !isScanning && encodingInfos.Any(info => info.needsConversion);
            if (GUILayout.Button("转换为UTF-8编码", GUILayout.Height(30)))
            {
                ConvertToUTF8();
            }

            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 显示扫描状态
            if (isScanning)
            {
                EditorGUILayout.LabelField("正在扫描脚本文件...", EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space(5);
            }

            // 显示扫描结果
            if (hasScanned && encodingInfos.Count > 0)
            {
                DrawScanResults();
            }
        }

        /// <summary>
        /// 扫描项目中的所有脚本文件
        /// </summary>
        private void ScanAllScripts()
        {
            isScanning = true;
            hasScanned = false;
            encodingInfos.Clear();

            try
            {
                // 获取Assets文件夹下所有.cs文件
                string[] allFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

                foreach (string filePath in allFiles)
                {
                    // 检测文件编码
                    Encoding detectedEncoding = DetectFileEncoding(filePath);

                    EncodingInfo info = new EncodingInfo
                    {
                        filePath = filePath,
                        encodingName = detectedEncoding.EncodingName,
                        isUTF8 = IsUTF8Encoding(detectedEncoding),
                        needsConversion = !IsUTF8Encoding(detectedEncoding)
                    };

                    encodingInfos.Add(info);
                }

                hasScanned = true;
                Debug.Log($"扫描完成！共找到 {encodingInfos.Count} 个脚本文件，其中 {encodingInfos.Count(info => info.needsConversion)} 个需要转换为UTF-8编码。");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"扫描过程中发生错误: {ex.Message}");
            }
            finally
            {
                isScanning = false;
            }
        }

        /// <summary>
        /// 检测文件编码格式
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>检测到的编码格式</returns>
        private Encoding DetectFileEncoding(string filePath)
        {
            try
            {
                // 读取文件的前几个字节来检测BOM
                byte[] buffer = new byte[4];
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(buffer, 0, 4);
                }

                // 检测UTF-8 BOM
                if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                {
                    return Encoding.UTF8;
                }

                // 检测UTF-16 LE BOM
                if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                {
                    return Encoding.Unicode;
                }

                // 检测UTF-16 BE BOM
                if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                {
                    return Encoding.BigEndianUnicode;
                }

                // 检测UTF-32 LE BOM
                if (buffer[0] == 0xFF && buffer[1] == 0xFE && buffer[2] == 0x00 && buffer[3] == 0x00)
                {
                    return Encoding.UTF32;
                }

                // 如果没有BOM，尝试读取文件内容来判断编码
                string content = File.ReadAllText(filePath, Encoding.Default);

                // 尝试用UTF-8解码，如果成功且没有替换字符，则可能是UTF-8
                try
                {
                    byte[] utf8Bytes = File.ReadAllBytes(filePath);
                    string utf8Content = Encoding.UTF8.GetString(utf8Bytes);

                    // 检查是否包含UTF-8解码错误的替换字符
                    if (!utf8Content.Contains("\uFFFD"))
                    {
                        return Encoding.UTF8;
                    }
                }
                catch
                {
                    // UTF-8解码失败，继续其他检测
                }

                // 默认返回系统默认编码（通常是GBK或其他本地编码）
                return Encoding.Default;
            }
            catch
            {
                // 如果检测失败，返回默认编码
                return Encoding.Default;
            }
        }

        /// <summary>
        /// 判断编码是否为UTF-8
        /// </summary>
        /// <param name="encoding">编码对象</param>
        /// <returns>是否为UTF-8编码</returns>
        private bool IsUTF8Encoding(Encoding encoding)
        {
            return encoding.Equals(Encoding.UTF8) || encoding.CodePage == 65001;
        }

        /// <summary>
        /// 将非UTF-8编码的文件转换为UTF-8编码
        /// </summary>
        private void ConvertToUTF8()
        {
            int convertedCount = 0;
            int errorCount = 0;

            try
            {
                foreach (var info in encodingInfos.Where(info => info.needsConversion))
                {
                    try
                    {
                        // 使用原编码读取文件内容
                        Encoding originalEncoding = DetectFileEncoding(info.filePath);
                        string content = File.ReadAllText(info.filePath, originalEncoding);

                        // 使用UTF-8编码写回文件（不带BOM）
                        File.WriteAllText(info.filePath, content, new UTF8Encoding(false));

                        convertedCount++;
                        Debug.Log($"已转换: {Path.GetFileName(info.filePath)}");
                    }
                    catch (System.Exception ex)
                    {
                        errorCount++;
                        Debug.LogError($"转换文件失败 {Path.GetFileName(info.filePath)}: {ex.Message}");
                    }
                }

                // 转换完成后重新扫描
                if (convertedCount > 0)
                {
                    Debug.Log($"编码转换完成！成功转换 {convertedCount} 个文件，失败 {errorCount} 个文件。");

                    // 刷新Unity资源数据库
                    AssetDatabase.Refresh();

                    // 重新扫描以更新状态
                    ScanAllScripts();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"批量转换过程中发生错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 绘制扫描结果界面
        /// </summary>
        private void DrawScanResults()
        {
            // 统计信息
            int utf8Count = encodingInfos.Count(info => info.isUTF8);
            int needConversionCount = encodingInfos.Count(info => info.needsConversion);

            EditorGUILayout.LabelField($"扫描结果: 共 {encodingInfos.Count} 个文件，UTF-8: {utf8Count}，需转换: {needConversionCount}", EditorStyles.boldLabel);

            EditorGUILayout.Space(5);

            // 文件列表
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var info in encodingInfos)
            {
                EditorGUILayout.BeginHorizontal();

                // 文件状态图标
                string statusIcon = info.isUTF8 ? "?" : "?";
                Color statusColor = info.isUTF8 ? Color.green : Color.yellow;

                GUIStyle statusStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = statusColor },
                    fontStyle = FontStyle.Bold
                };

                EditorGUILayout.LabelField(statusIcon, statusStyle, GUILayout.Width(20));

                // 文件路径（相对于Assets文件夹）
                string relativePath = info.filePath.Replace(Application.dataPath, "Assets");
                EditorGUILayout.LabelField(relativePath, GUILayout.ExpandWidth(true));

                // 编码信息
                EditorGUILayout.LabelField(info.encodingName, GUILayout.Width(150));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}