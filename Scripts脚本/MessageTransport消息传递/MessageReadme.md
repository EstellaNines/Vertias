```markdown
# Unity Global Messaging Center  
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> 一个 **零依赖、高性能、类型安全** 的全局消息/事件系统，适用于 Unity 任意项目规模。  

---

## �7�8 功能亮点
| 特性 | 描述 |
|---|---|
| �0�4 **无反射** | 纯 C# 泛型实现，零 GC、零装箱 |
| �9�8 **类型安全** | 编译期即可发现消息类型错误 |
| �0�7 **即插即用** | 单文件即可集成，无需额外资产 |
| �9�4 **跨场景** | 场景切换后仍可收发 |
| �0�8 **易调试** | 可在 IDE/控制台直接查看消息流 |
| �0�3 **自动清理** | 提供 `Clear()` 防止内存泄漏 |

---

## �9�4 快速开始（30 秒）

### 1. 复制脚本
将下列文件放入 `Assets/Scripts/GlobalMessaging/`  
- `IMessage.cs`  
- `MessagingCenter.cs`  

### 2. 定义一条消息
```csharp
public struct PlayerDamaged : IMessage
{
    public int Amount;
    public PlayerDamaged(int amount) => Amount = amount;
}
```

### 3. 监听 & 发送

```csharp
// 监听
MessagingCenter.Instance.Register<PlayerDamaged>(OnPlayerDamaged);

// 发送
MessagingCenter.Instance.Send(new PlayerDamaged(25));
```

---

## �9�2 API 速查

| 方法 | 用途 |
|---|---|
| `Register<T>(Action<T>)` | 订阅消息 |
| `Unregister<T>(Action<T>)` | 取消订阅 |
| `Send<T>(T message)` | 广播消息 |
| `Clear()` | 清空所有订阅（场景切换时调用） |

---

## �0�0 最佳实践

1. **生命周期配对**  
   在 `MonoBehaviour.OnEnable` 订阅，在 `OnDisable` 取消订阅。  

   ```csharp
   void OnEnable()  => MessagingCenter.Instance.Register<XXX>(OnXXX);
   void OnDisable() => MessagingCenter.Instance.Unregister<XXX>(OnXXX);
   ```

2. **场景切换**  
   在场景管理器中添加：

   ```csharp
   void OnSceneLoaded(Scene scene, LoadSceneMode mode)
   {
       MessagingCenter.Instance.Clear();
   }
   ```

3. **性能敏感场景**  
   - 避免在 `Update` 中高频 `Send`，可使用 `MessageBuffer` 批量推送。  
   - 对于大型项目，可扩展为带优先级的 `IPriorityMessage` 接口。

---

## �0�8 示例场景

| 场景 | 消息类 | 监听者 | 触发者 |
|---|---|---|---|
| 玩家受伤 | `PlayerDamaged` | `HUDManager` | `HealthSystem` |
| 游戏暂停 | `GamePaused` | `AudioManager` | `PauseMenu` |
| 任务完成 | `QuestCompleted` | `QuestUI` | `QuestManager` |

---

## �9�3 常见问题

**Q1：可以跨线程吗？**  
A：当前实现为单线程，如需多线程请自行加锁或使用 `ConcurrentQueue`。

**Q2：如何调试消息流？**  
A：在 `MessagingCenter.Send<T>` 内加入 `Debug.Log($"[Msg] {typeof(T).Name}")`。

**Q3：与 UnityEvent 有何区别？**  
A：UnityEvent 需要 Inspector 配置，适合 UI；本系统纯代码，适合逻辑层解耦。

---

## �9�0 License

MIT �0�8 2024 YourName

---

## �0�3 贡献

欢迎 PR & Issue！一起让它更强大 �9�5

```
