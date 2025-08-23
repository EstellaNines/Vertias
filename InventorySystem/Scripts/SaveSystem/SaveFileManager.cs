using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;

namespace InventorySystem.SaveSystem
{
    /// <summary>
    /// 保存文件管理器 - 负责保存文件的创建、读取、写入和管理
    /// 提供统一的文件操作接口，支持多存档管理和文件安全性保障
    /// </summary>
    public class SaveFileManager : MonoBehaviour
    {
        #region 字段和属性
        [Header("文件管理配置")]
        [SerializeField] private string saveDirectory = "SaveData"; // 保存目录名
        [SerializeField] private string fileExtension = ".json"; // 文件扩展名
        [SerializeField] private bool enableBackup = true; // 是否启用备份
        [SerializeField] private int maxBackupCount = 5; // 最大备份数量
        [SerializeField] private bool enableCompression = false; // 是否启用压缩
        [SerializeField] private bool enableLogging = true; // 是否启用日志记录

        // 文件路径相关
        private string fullSaveDirectory;
        private string backupDirectory;

        // 文件操作统计
        private int readOperations = 0;
        private int writeOperations = 0;
        private long totalBytesRead = 0;
        private long totalBytesWritten = 0;

        // 文件锁定管理
        private HashSet<string> lockedFiles = new HashSet<string>();

        // 支持的文件格式
        private readonly string[] supportedExtensions = { ".json", ".dat", ".save" };
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化文件管理器
        /// </summary>
        public void Initialize()
        {
            SetupDirectories();
            ValidateConfiguration();
            LogMessage("SaveFileManager已初始化");
        }

        /// <summary>
        /// 设置目录结构
        /// </summary>
        private void SetupDirectories()
        {
            // 设置主保存目录
            fullSaveDirectory = Path.Combine(Application.persistentDataPath, saveDirectory);

            // 设置备份目录
            backupDirectory = Path.Combine(fullSaveDirectory, "Backups");

            // 创建目录（如果不存在）
            CreateDirectoryIfNotExists(fullSaveDirectory);

            if (enableBackup)
            {
                CreateDirectoryIfNotExists(backupDirectory);
            }

            LogMessage($"保存目录: {fullSaveDirectory}");
            LogMessage($"备份目录: {backupDirectory}");
        }

        /// <summary>
        /// 验证配置
        /// </summary>
        private void ValidateConfiguration()
        {
            // 验证文件扩展名
            if (!fileExtension.StartsWith("."))
            {
                fileExtension = "." + fileExtension;
            }

            if (!supportedExtensions.Contains(fileExtension))
            {
                LogWarning($"不支持的文件扩展名: {fileExtension}，将使用默认的.json");
                fileExtension = ".json";
            }

            // 验证备份数量
            if (maxBackupCount < 1)
            {
                maxBackupCount = 1;
                LogWarning("最大备份数量不能小于1，已设置为1");
            }

            if (maxBackupCount > 20)
            {
                maxBackupCount = 20;
                LogWarning("最大备份数量不能大于20，已设置为20");
            }
        }

        /// <summary>
        /// 创建目录（如果不存在）
        /// </summary>
        private void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    LogMessage($"创建目录: {path}");
                }
                catch (Exception ex)
                {
                    LogError($"创建目录失败: {path}, 错误: {ex.Message}");
                }
            }
        }
        #endregion

        #region 文件写入操作
        /// <summary>
        /// 写入保存文件
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <param name="content">文件内容</param>
        /// <returns>是否写入成功</returns>
        public bool WriteSaveFile(string fileName, string content)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(content))
            {
                LogWarning("文件名或内容为空，无法写入");
                return false;
            }

            string filePath = GetFullFilePath(fileName);

            // 检查文件是否被锁定
            if (IsFileLocked(filePath))
            {
                LogWarning($"文件被锁定，无法写入: {fileName}");
                return false;
            }

            try
            {
                // 锁定文件
                LockFile(filePath);

                // 创建备份（如果文件已存在）
                if (enableBackup && File.Exists(filePath))
                {
                    CreateBackup(filePath);
                }

                // 写入文件
                if (enableCompression)
                {
                    WriteCompressedFile(filePath, content);
                }
                else
                {
                    File.WriteAllText(filePath, content, System.Text.Encoding.UTF8);
                }

                // 更新统计信息
                writeOperations++;
                totalBytesWritten += System.Text.Encoding.UTF8.GetByteCount(content);

                LogMessage($"文件写入成功: {fileName}, 大小: {content.Length}字符");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"文件写入失败: {fileName}, 错误: {ex.Message}");
                return false;
            }
            finally
            {
                // 解锁文件
                UnlockFile(filePath);
            }
        }

        /// <summary>
        /// 异步写入保存文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="content">文件内容</param>
        /// <returns>写入任务</returns>
        public async Task<bool> WriteSaveFileAsync(string fileName, string content)
        {
            return await Task.Run(() => WriteSaveFile(fileName, content));
        }

        /// <summary>
        /// 写入压缩文件
        /// </summary>
        private void WriteCompressedFile(string filePath, string content)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(content);
            byte[] compressedData = CompressData(data);
            File.WriteAllBytes(filePath, compressedData);
        }
        #endregion

        #region 文件读取操作
        /// <summary>
        /// 读取保存文件
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>文件内容，失败返回null</returns>
        public string ReadSaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                LogWarning("文件名为空，无法读取");
                return null;
            }

            string filePath = GetFullFilePath(fileName);

            if (!File.Exists(filePath))
            {
                LogWarning($"文件不存在: {fileName}");
                return null;
            }

            // 检查文件是否被锁定
            if (IsFileLocked(filePath))
            {
                LogWarning($"文件被锁定，无法读取: {fileName}");
                return null;
            }

            try
            {
                // 锁定文件
                LockFile(filePath);

                string content;

                if (enableCompression)
                {
                    content = ReadCompressedFile(filePath);
                }
                else
                {
                    content = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                }

                // 更新统计信息
                readOperations++;
                totalBytesRead += System.Text.Encoding.UTF8.GetByteCount(content);

                LogMessage($"文件读取成功: {fileName}, 大小: {content.Length}字符");
                return content;
            }
            catch (Exception ex)
            {
                LogError($"文件读取失败: {fileName}, 错误: {ex.Message}");
                return null;
            }
            finally
            {
                // 解锁文件
                UnlockFile(filePath);
            }
        }

        /// <summary>
        /// 异步读取保存文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>读取任务</returns>
        public async Task<string> ReadSaveFileAsync(string fileName)
        {
            return await Task.Run(() => ReadSaveFile(fileName));
        }

        /// <summary>
        /// 读取压缩文件
        /// </summary>
        private string ReadCompressedFile(string filePath)
        {
            byte[] compressedData = File.ReadAllBytes(filePath);
            byte[] data = DecompressData(compressedData);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// 加载保存数据
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>保存游戏数据，失败返回null</returns>
        public SaveGameData LoadSaveData(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                LogWarning("文件名为空，无法加载保存数据");
                return null;
            }

            string content = ReadSaveFile(fileName);
            if (string.IsNullOrEmpty(content))
            {
                LogWarning($"无法读取保存文件: {fileName}");
                return null;
            }

            try
            {
                SaveGameData saveData = JsonUtility.FromJson<SaveGameData>(content);
                if (saveData == null)
                {
                    LogError($"保存数据反序列化失败: {fileName}");
                    return null;
                }

                LogMessage($"保存数据加载成功: {fileName}");
                return saveData;
            }
            catch (Exception ex)
            {
                LogError($"保存数据加载失败: {fileName}, 错误: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region 文件管理操作
        /// <summary>
        /// 检查保存文件是否存在
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否存在</returns>
        public bool SaveFileExists(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            string filePath = GetFullFilePath(fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// 删除保存文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>是否删除成功</returns>
        public bool DeleteSaveFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                LogWarning("文件名为空，无法删除");
                return false;
            }

            string filePath = GetFullFilePath(fileName);

            if (!File.Exists(filePath))
            {
                LogWarning($"文件不存在，无法删除: {fileName}");
                return false;
            }

            // 检查文件是否被锁定
            if (IsFileLocked(filePath))
            {
                LogWarning($"文件被锁定，无法删除: {fileName}");
                return false;
            }

            try
            {
                // 创建备份（如果启用）
                if (enableBackup)
                {
                    CreateBackup(filePath);
                }

                File.Delete(filePath);
                LogMessage($"文件删除成功: {fileName}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"文件删除失败: {fileName}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 复制保存文件
        /// </summary>
        /// <param name="sourceFileName">源文件名</param>
        /// <param name="targetFileName">目标文件名</param>
        /// <returns>是否复制成功</returns>
        public bool CopySaveFile(string sourceFileName, string targetFileName)
        {
            if (string.IsNullOrEmpty(sourceFileName) || string.IsNullOrEmpty(targetFileName))
            {
                LogWarning("源文件名或目标文件名为空，无法复制");
                return false;
            }

            string sourcePath = GetFullFilePath(sourceFileName);
            string targetPath = GetFullFilePath(targetFileName);

            if (!File.Exists(sourcePath))
            {
                LogWarning($"源文件不存在: {sourceFileName}");
                return false;
            }

            try
            {
                File.Copy(sourcePath, targetPath, true);
                LogMessage($"文件复制成功: {sourceFileName} -> {targetFileName}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"文件复制失败: {sourceFileName} -> {targetFileName}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 重命名保存文件
        /// </summary>
        /// <param name="oldFileName">旧文件名</param>
        /// <param name="newFileName">新文件名</param>
        /// <returns>是否重命名成功</returns>
        public bool RenameSaveFile(string oldFileName, string newFileName)
        {
            if (string.IsNullOrEmpty(oldFileName) || string.IsNullOrEmpty(newFileName))
            {
                LogWarning("旧文件名或新文件名为空，无法重命名");
                return false;
            }

            string oldPath = GetFullFilePath(oldFileName);
            string newPath = GetFullFilePath(newFileName);

            if (!File.Exists(oldPath))
            {
                LogWarning($"源文件不存在: {oldFileName}");
                return false;
            }

            if (File.Exists(newPath))
            {
                LogWarning($"目标文件已存在: {newFileName}");
                return false;
            }

            try
            {
                File.Move(oldPath, newPath);
                LogMessage($"文件重命名成功: {oldFileName} -> {newFileName}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"文件重命名失败: {oldFileName} -> {newFileName}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取所有保存文件列表
        /// </summary>
        /// <returns>文件信息列表</returns>
        public List<SaveFileInfo> GetAllSaveFiles()
        {
            var saveFiles = new List<SaveFileInfo>();

            if (!Directory.Exists(fullSaveDirectory))
            {
                return saveFiles;
            }

            try
            {
                var files = Directory.GetFiles(fullSaveDirectory, $"*{fileExtension}")
                    .Where(f => !Path.GetFileName(f).StartsWith(".")) // 排除隐藏文件
                    .OrderByDescending(f => File.GetLastWriteTime(f));

                foreach (string filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var saveFileInfo = new SaveFileInfo
                    {
                        fileName = Path.GetFileNameWithoutExtension(fileInfo.Name),
                        fullPath = filePath,
                        size = fileInfo.Length,
                        creationTime = fileInfo.CreationTime,
                        lastWriteTime = fileInfo.LastWriteTime,
                        isReadOnly = fileInfo.IsReadOnly
                    };

                    saveFiles.Add(saveFileInfo);
                }

                LogMessage($"找到{saveFiles.Count}个保存文件");
            }
            catch (Exception ex)
            {
                LogError($"获取保存文件列表失败: {ex.Message}");
            }

            return saveFiles;
        }
        #endregion

        #region 备份管理
        /// <summary>
        /// 创建文件备份
        /// </summary>
        /// <param name="filePath">原文件路径</param>
        private void CreateBackup(string filePath)
        {
            if (!enableBackup || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"{fileName}_backup_{timestamp}{fileExtension}";
                string backupPath = Path.Combine(backupDirectory, backupFileName);

                File.Copy(filePath, backupPath, true);

                // 清理旧备份
                CleanupOldBackups(fileName);

                LogMessage($"创建备份: {backupFileName}");
            }
            catch (Exception ex)
            {
                LogError($"创建备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理旧备份文件
        /// </summary>
        /// <param name="fileName">原文件名</param>
        private void CleanupOldBackups(string fileName)
        {
            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, $"{fileName}_backup_*{fileExtension}")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Skip(maxBackupCount);

                foreach (string oldBackup in backupFiles)
                {
                    File.Delete(oldBackup);
                    LogMessage($"删除旧备份: {Path.GetFileName(oldBackup)}");
                }
            }
            catch (Exception ex)
            {
                LogError($"清理旧备份失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复备份文件
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="backupIndex">备份索引（0为最新）</param>
        /// <returns>是否恢复成功</returns>
        public bool RestoreFromBackup(string fileName, int backupIndex = 0)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                LogWarning("文件名为空，无法恢复备份");
                return false;
            }

            try
            {
                var backupFiles = Directory.GetFiles(backupDirectory, $"{fileName}_backup_*{fileExtension}")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .ToArray();

                if (backupIndex >= backupFiles.Length)
                {
                    LogWarning($"备份索引超出范围: {backupIndex}, 可用备份数: {backupFiles.Length}");
                    return false;
                }

                string backupPath = backupFiles[backupIndex];
                string targetPath = GetFullFilePath(fileName);

                File.Copy(backupPath, targetPath, true);

                LogMessage($"从备份恢复成功: {fileName}, 备份索引: {backupIndex}");
                return true;
            }
            catch (Exception ex)
            {
                LogError($"从备份恢复失败: {fileName}, 错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取备份文件列表
        /// </summary>
        /// <param name="fileName">原文件名</param>
        /// <returns>备份文件信息列表</returns>
        public List<BackupFileInfo> GetBackupFiles(string fileName)
        {
            var backupFiles = new List<BackupFileInfo>();

            if (string.IsNullOrEmpty(fileName) || !Directory.Exists(backupDirectory))
            {
                return backupFiles;
            }

            try
            {
                var files = Directory.GetFiles(backupDirectory, $"{fileName}_backup_*{fileExtension}")
                    .OrderByDescending(f => File.GetCreationTime(f));

                int index = 0;
                foreach (string filePath in files)
                {
                    var fileInfo = new FileInfo(filePath);
                    var backupInfo = new BackupFileInfo
                    {
                        index = index++,
                        fileName = fileInfo.Name,
                        fullPath = filePath,
                        size = fileInfo.Length,
                        creationTime = fileInfo.CreationTime
                    };

                    backupFiles.Add(backupInfo);
                }
            }
            catch (Exception ex)
            {
                LogError($"获取备份文件列表失败: {ex.Message}");
            }

            return backupFiles;
        }
        #endregion

        #region 文件锁定管理
        /// <summary>
        /// 锁定文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private void LockFile(string filePath)
        {
            lockedFiles.Add(filePath);
        }

        /// <summary>
        /// 解锁文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        private void UnlockFile(string filePath)
        {
            lockedFiles.Remove(filePath);
        }

        /// <summary>
        /// 检查文件是否被锁定
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否被锁定</returns>
        private bool IsFileLocked(string filePath)
        {
            return lockedFiles.Contains(filePath);
        }
        #endregion

        #region 压缩和解压缩
        /// <summary>
        /// 压缩数据
        /// </summary>
        /// <param name="data">原始数据</param>
        /// <returns>压缩后的数据</returns>
        private byte[] CompressData(byte[] data)
        {
            // 这里可以实现具体的压缩算法，如GZip
            // 为了简化，暂时返回原始数据
            return data;
        }

        /// <summary>
        /// 解压缩数据
        /// </summary>
        /// <param name="compressedData">压缩数据</param>
        /// <returns>解压缩后的数据</returns>
        private byte[] DecompressData(byte[] compressedData)
        {
            // 这里可以实现具体的解压缩算法，如GZip
            // 为了简化，暂时返回原始数据
            return compressedData;
        }
        #endregion

        #region 实用方法
        /// <summary>
        /// 获取完整文件路径
        /// </summary>
        /// <param name="fileName">文件名（不含扩展名）</param>
        /// <returns>完整文件路径</returns>
        private string GetFullFilePath(string fileName)
        {
            return Path.Combine(fullSaveDirectory, fileName + fileExtension);
        }

        /// <summary>
        /// 获取文件管理器统计信息
        /// </summary>
        /// <returns>统计信息字符串</returns>
        public string GetStatistics()
        {
            return $"读取操作: {readOperations}, " +
                   $"写入操作: {writeOperations}, " +
                   $"总读取字节数: {totalBytesRead}, " +
                   $"总写入字节数: {totalBytesWritten}, " +
                   $"锁定文件数: {lockedFiles.Count}";
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            readOperations = 0;
            writeOperations = 0;
            totalBytesRead = 0;
            totalBytesWritten = 0;
            LogMessage("统计信息已重置");
        }

        /// <summary>
        /// 获取保存目录路径
        /// </summary>
        /// <returns>保存目录路径</returns>
        public string GetSaveDirectoryPath()
        {
            return fullSaveDirectory;
        }

        /// <summary>
        /// 获取备份目录路径
        /// </summary>
        /// <returns>备份目录路径</returns>
        public string GetBackupDirectoryPath()
        {
            return backupDirectory;
        }
        #endregion

        #region 日志方法
        /// <summary>
        /// 记录日志消息
        /// </summary>
        private void LogMessage(string message)
        {
            if (enableLogging)
            {
                Debug.Log($"[SaveFileManager] {message}");
            }
        }

        /// <summary>
        /// 记录警告消息
        /// </summary>
        private void LogWarning(string message)
        {
            if (enableLogging)
            {
                Debug.LogWarning($"[SaveFileManager] {message}");
            }
        }

        /// <summary>
        /// 记录错误消息
        /// </summary>
        private void LogError(string message)
        {
            if (enableLogging)
            {
                Debug.LogError($"[SaveFileManager] {message}");
            }
        }
        #endregion
    }

    #region 数据结构
    /// <summary>
    /// 保存文件信息
    /// </summary>
    [Serializable]
    public class SaveFileInfo
    {
        public string fileName;         // 文件名（不含扩展名）
        public string fullPath;         // 完整路径
        public long size;               // 文件大小（字节）
        public DateTime creationTime;   // 创建时间
        public DateTime lastWriteTime;  // 最后修改时间
        public bool isReadOnly;         // 是否只读
    }

    /// <summary>
    /// 备份文件信息
    /// </summary>
    [Serializable]
    public class BackupFileInfo
    {
        public int index;               // 备份索引
        public string fileName;         // 文件名
        public string fullPath;         // 完整路径
        public long size;               // 文件大小（字节）
        public DateTime creationTime;   // 创建时间
    }
    #endregion
}