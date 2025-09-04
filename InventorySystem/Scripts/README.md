# 库存系统脚本目录结构

## �9�7 目录概览

本目录包含四个主要功能模块，每个模块都有明确的职责划分：

### �9�4 EquipmentSlot（装备栏系统）

装备槽系统负责管理角色身上的装备，包括头盔、护甲、背包、武器等。

### �9�4 SaveSystem（保存系统）

通用的库存保存加载系统，处理网格数据的持久化。

### �9�5 SpawnSystem（生成系统）

物品生成系统，包括固定物品生成、仓库物品生成等功能。

### �9�6 Database（数据库系统）

物品数据库管理，处理 ItemDataSO 的加载和查询。

---

## �9�4 EquipmentSlot 详细结构

装备栏系统被重新组织为四个清晰的子模块：

### �9�7 DataObjects（数据对象）

装备槽相关的数据结构和配置文件

```
DataObjects/
├── EquipmentSlotConfig.cs          # 装备槽配置类
├── EquipmentSlotType.cs            # 装备槽类型枚举
├── EquipmentPersistenceData.cs     # 装备持久化数据结构
├── ArmorConfig.asset               # 护甲槽配置
├── BackpackConfig.asset            # 背包槽配置
├── HelmetConfig.asset              # 头盔槽配置
├── PrimaryWeponConfig.asset        # 主武器槽配置
├── SecondaryWeaponConfig.asset     # 副武器槽配置
└── TacticalRigConfig.asset         # 战术背心槽配置
```

### �7�5�1�5 Core（核心装备栏功能）

装备槽的核心逻辑和管理功能

```
Core/
├── EquipmentSlot.cs                # 装备槽核心组件
├── EquipmentSlotManager.cs         # 装备槽管理器
├── EquipmentSlotIntegration.cs     # 装备槽集成组件
├── EquipmentSlotGridExtension.cs   # 装备槽网格扩展
└── ItemPrefabConstants.cs          # 物品预制件常量
```

### �9�4 Persistence（装备栏保存机制）

装备槽数据的持久化保存功能

```
Persistence/
├── EquipmentPersistenceManager.cs      # 装备持久化管理器
├── EquipmentPersistenceIntegration.cs  # 装备持久化集成
├── EquipmentSlotSaveExtension.cs       # 装备槽保存扩展
└── EquipmentSystemMigration.cs         # 装备系统迁移工具
```

### �9�4 ContainerPersistence（容器保存机制）

专门处理背包/挂具等容器类装备的内容保存

```
ContainerPersistence/
├── ContainerSaveManager.cs           # 容器保存管理器
├── ContainerSessionManager.cs        # 容器会话管理器
├── BackpackEquipmentEventHandler.cs  # 背包装备事件处理器
├── ContainerSaveManagerTester.cs     # 容器保存管理器测试器
└── CrossSessionPersistenceTest.cs    # 跨会话持久化测试
```

---

## �9�5 SpawnSystem 详细结构

生成系统被重新组织为四个专门的子模块：

### �9�8 FixedSpawn（固定物品生成器）

处理固定位置和配置的物品生成

```
FixedSpawn/
├── FixedItemSpawnConfig.cs        # 固定物品生成配置
├── FixedItemSpawnManager.cs       # 固定物品生成管理器
├── FixedItemSpawnSetup.cs         # 固定物品生成设置组件
└── FixedItemTemplate.cs           # 固定物品模板
```

### �9�4 WarehouseSpawn（仓库固定物品生成器）

专门处理仓库的固定物品生成

```
WarehouseSpawn/
└── WarehouseFixedItemManager.cs   # 仓库固定物品管理器
```

### �9�6 RandomSpawn（随机生成器）

_预留模块 - 用于未来的随机物品生成功能_

```
RandomSpawn/
└── (预留给随机生成功能)
```

### �7�5�1�5 Core（生成系统核心）

生成系统的核心算法和基础设施

```
Core/
├── GridOccupancyAnalyzer.cs       # 网格占用分析器
├── SmartPlacementStrategy.cs      # 智能放置策略
├── SpawnStateTracker.cs           # 生成状态跟踪器
├── SpawnSystemIntegration.cs      # 生成系统集成
├── SpawnSystemManager.cs          # 生成系统管理器
└── InventorySpawnTag.cs           # 库存生成标记
```

---

## �9�4 SaveSystem 结构

保存系统保持原有结构，处理通用的网格数据保存：

```
SaveSystem/
├── GridSaveData.cs                # 网格保存数据
├── InventorySaveData.cs           # 库存保存数据
├── InventorySaveManager.cs        # 库存保存管理器
├── ItemSaveData.cs                # 物品保存数据
└── SaveTriggerExample.cs          # 保存触发示例
```

---

## �9�6 Database 结构

数据库系统处理物品数据的管理：

```
Database/
├── ItemDatabase.cs                # 物品数据库管理器
└── (其他数据库相关文件)
```

---

## �9�9 使用指南

### 装备栏系统开发

1. **数据配置**：在 `DataObjects/` 中修改装备槽配置
2. **核心功能**：在 `Core/` 中开发装备槽逻辑
3. **持久化**：在 `Persistence/` 中处理装备保存
4. **容器功能**：在 `ContainerPersistence/` 中开发背包/挂具相关功能

### 生成系统开发

1. **固定生成**：在 `FixedSpawn/` 中配置和管理固定物品生成
2. **仓库生成**：在 `WarehouseSpawn/` 中处理仓库专用功能
3. **随机生成**：未来在 `RandomSpawn/` 中开发随机生成功能
4. **核心算法**：在 `Core/` 中改进生成算法和策略

### 命名空间

所有脚本都位于 `InventorySystem` 命名空间下，按照模块功能进行组织。

---

## �9�5 更新日志

### 2024-09-04

- 重新组织 EquipmentSlot 为四个子模块
- 重新组织 SpawnSystem 为四个子模块
- 创建了清晰的职责划分和模块边界
- 为 RandomSpawn 预留了扩展空间

---

## �0�3 贡献指南

在添加新功能时，请遵循以下原则：

1. 将代码放在正确的模块目录中
2. 保持单一职责原则
3. 使用清晰的命名约定
4. 添加适当的注释和文档
