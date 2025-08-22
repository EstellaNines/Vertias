# 物品系统保存功能使用指南

## 概述

本保存系统为 Unity 物品系统提供了完整的数据持久化解决方案，支持物品数据、网格配置、生成器状态等的自动保存和加载。

## 系统架构

### 核心组件

1. **SaveManager** - 保存管理器核心类

   - 单例模式，负责协调所有保存操作
   - 管理 ISaveable 对象的注册和依赖关系
   - 提供保存/加载的统一接口

2. **SaveDataSerializer** - JSON 序列化器

   - 处理数据的序列化和反序列化
   - 支持 Unity 特殊类型（Vector3、Quaternion 等）
   - 提供数据验证和错误处理

3. **SaveFileManager** - 文件管理器

   - 处理存档文件的读写操作
   - 支持文件备份和恢复
   - 提供文件锁定和安全机制

4. **SaveSystemInitializer** - 系统初始化器

   - 自动初始化保存系统
   - 配置自动保存功能
   - 验证 ISaveable 对象集成

5. **ISaveable 接口** - 可保存对象接口
   - 定义保存和加载的标准方法
   - 支持唯一 ID 生成和数据验证

## 快速开始

### 1. 系统初始化

在场景中添加 SaveSystemInitializer 组件：

```csharp
// 在任意GameObject上添加SaveSystemInitializer组件
// 系统会自动初始化并发现场景中的ISaveable对象
```

### 2. 基本保存操作

```csharp
// 获取SaveManager实例
SaveManager saveManager = SaveManager.Instance;

// 保存游戏数据
bool success = saveManager.SaveGame("MyGameSave");

// 加载游戏数据
bool loaded = saveManager.LoadGame("MyGameSave");

// 检查存档是否存在
var saveInfo = saveManager.GetSaveInfo("MyGameSave");
if (saveInfo != null)
{
    Debug.Log($"存档创建时间: {saveInfo.creationTime}");
}
```

### 3. 自动保存配置

```csharp
// 启用自动保存
saveManager.SetAutoSaveEnabled(true);

// 设置自动保存间隔（秒）
saveManager.SetAutoSaveInterval(300f); // 5分钟
```

## ISaveable 接口实现

系统已为以下类实现了 ISaveable 接口：

### 1. ItemDataHolder

- 保存物品数据路径和实例数据
- 支持动态属性和状态信息
- 自动生成唯一 SaveID

### 2. BaseItemSpawn

- 保存生成器配置和状态
- 记录物品生成历史
- 支持生成器启用/禁用状态

### 3. ItemSpawner

- 继承 BaseItemSpawn 的保存功能
- 额外保存生成器特定配置
- 支持生成队列和计时器状态

### 4. ItemGrid

- 保存网格配置和状态
- 支持静态网格配置保存
- 记录网格同步状态和描述

## 配置选项

### SaveSystemInitializer 配置

```csharp
[Header("保存系统配置")]
public bool autoInitializeOnStart = true;     // 自动初始化
public bool enableAutoSave = true;            // 启用自动保存
public float autoSaveInterval = 300f;         // 自动保存间隔
public bool enableDebugLogging = true;        // 调试日志

[Header("初始化延迟设置")]
public float initializationDelay = 1f;       // 初始化延迟
public bool waitForSceneLoad = true;          // 等待场景加载
```

### SaveFileManager 配置

```csharp
[Header("文件管理配置")]
public string saveDirectory = "SaveData";     // 保存目录
public string fileExtension = ".json";       // 文件扩展名
public bool enableBackup = true;              // 启用备份
public int maxBackupCount = 5;                // 最大备份数量
public bool enableCompression = false;        // 启用压缩
```

## 使用示例

### 示例 1：基本保存加载

```csharp
public class GameController : MonoBehaviour
{
    private SaveManager saveManager;

    void Start()
    {
        saveManager = SaveManager.Instance;
    }

    public void SaveGame()
    {
        if (saveManager.SaveGame("PlayerSave"))
        {
            Debug.Log("游戏保存成功");
        }
    }

    public void LoadGame()
    {
        if (saveManager.LoadGame("PlayerSave"))
        {
            Debug.Log("游戏加载成功");
        }
    }
}
```

### 示例 2：自定义 ISaveable 实现

```csharp
public class CustomSaveableObject : MonoBehaviour, ISaveable
{
    [System.Serializable]
    public class CustomSaveData : BaseSaveData
    {
        public int customValue;
        public string customString;
    }

    private int myValue = 100;
    private string myString = "Hello";

    public string SaveID => GenerateNewSaveID();

    public string GenerateNewSaveID()
    {
        return $"Custom_{name}_{GetInstanceID()}";
    }

    public object GetSaveData()
    {
        return new CustomSaveData
        {
            customValue = myValue,
            customString = myString
        };
    }

    public void LoadSaveData(object data)
    {
        if (data is CustomSaveData saveData)
        {
            myValue = saveData.customValue;
            myString = saveData.customString;
        }
    }

    public bool ValidateData(object data)
    {
        return data is CustomSaveData;
    }

    public void InitializeSaveSystem()
    {
        // 初始化逻辑
    }
}
```

## 快捷键支持

在使用 SaveSystemExample 组件时，支持以下快捷键：

- **F5** - 快速保存
- **F9** - 快速加载
- **F12** - 显示保存系统状态

## 错误处理

系统提供了完善的错误处理机制：

1. **数据验证** - 保存前验证数据完整性
2. **文件锁定** - 防止并发访问冲突
3. **备份恢复** - 自动创建备份文件
4. **异常捕获** - 详细的错误日志记录

## 性能优化

1. **增量保存** - 只保存变化的数据
2. **异步操作** - 支持异步文件 I/O
3. **数据压缩** - 可选的数据压缩功能
4. **依赖排序** - 优化保存顺序

## 调试和监控

### 控制台命令

在 SaveSystemExample 组件的上下文菜单中提供：

- **保存游戏** - 手动触发保存
- **加载游戏** - 手动触发加载
- **显示保存系统状态** - 查看系统状态
- **测试保存加载流程** - 完整流程测试

### 日志输出

系统提供详细的日志输出，包括：

- 初始化状态
- 保存/加载进度
- 错误和警告信息
- 性能统计数据

## 注意事项

1. **SaveID 唯一性** - 确保每个 ISaveable 对象的 SaveID 唯一
2. **数据序列化** - 确保保存的数据可以被 JSON 序列化
3. **依赖关系** - 注意对象间的依赖关系，系统会自动处理加载顺序
4. **文件权限** - 确保应用程序有写入保存目录的权限
5. **版本兼容** - 保存数据格式变更时需要考虑向后兼容性

## 扩展功能

系统设计为可扩展的，可以轻松添加：

1. **自定义序列化格式** - 支持二进制、XML 等格式
2. **云存储集成** - 集成云存储服务
3. **加密功能** - 添加数据加密保护
4. **版本控制** - 支持存档版本管理
5. **多用户支持** - 支持多用户存档

## 故障排除

### 常见问题

1. **保存失败**

   - 检查文件权限
   - 验证保存目录是否存在
   - 查看控制台错误信息

2. **加载失败**

   - 确认存档文件存在
   - 检查数据格式是否正确
   - 验证 ISaveable 对象是否正确注册

3. **自动保存不工作**
   - 确认自动保存已启用
   - 检查保存间隔设置
   - 验证 SaveManager 是否正确初始化

### 调试步骤

1. 启用调试日志
2. 使用 SaveSystemExample 进行测试
3. 检查保存文件内容
4. 验证 ISaveable 对象注册状态
5. 查看 Unity 控制台错误信息

## 更新日志

### v1.0.0 (当前版本)

- 实现基础保存系统架构
- 支持 JSON 序列化
- 实现文件管理和备份
- 集成 ISaveable 接口
- 添加自动保存功能
- 提供完整的示例和文档

---

如有问题或建议，请查看代码注释或联系开发团队。
