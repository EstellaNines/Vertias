# 动态网格监控器算法设计文档

## 概述

动态网格监控器使用了一套**三重数据结构优化算法**来高效记录和查询网格占用情况，实现了实时的网格状态监控和可视化显示。

## 核心算法架构

### 1. 数据结构设计

#### 1.1 三重数据结构

系统采用三种互补的数据结构来存储网格占用状态：

```csharp
// 核心数据结构
protected bool[,] gridOccupancy;          // 二维布尔数组 - 直接状态存储
protected int[,] prefixSum;               // 二维前缀和数组 - 快速区域查询
protected HashSet<Vector2Int> occupiedCells; // 哈希集合 - 快速单点查询
```

#### 1.2 数据结构特性对比

| 数据结构 | 空间复杂度 | 单点查询 | 区域查询 | 占用率计算 | 更新复杂度 |
|---------|-----------|---------|---------|-----------|----------|
| bool[,] | O(w×h) | O(1) | O(w×h) | O(w×h) | O(1) |
| int[,] prefixSum | O((w+1)×(h+1)) | O(1) | O(1) | O(1) | O(w×h) |
| HashSet<Vector2Int> | O(占用格子数) | O(1) | O(占用格子数) | O(1) | O(1) |

### 2. 初始化算法

#### 2.1 网格数组初始化

```csharp
protected virtual void InitializeGridArrays()
{
    // 初始化布尔数组 - 记录每个格子的占用状态
    gridOccupancy = new bool[width, height];
    
    // 初始化前缀和数组 - 多分配一行一列用于边界处理
    prefixSum = new int[width + 1, height + 1];
    
    // 初始化哈希集合 - 存储被占用格子的坐标
    occupiedCells = new HashSet<Vector2Int>();
}
```

#### 2.2 初始化时机

- **Awake阶段**: 加载默认网格配置
- **Start阶段**: 初始化数据结构和保存系统
- **OnValidate阶段**: 处理编辑器中的参数变更

### 3. 占用状态更新算法

#### 3.1 核心更新方法

```csharp
protected virtual void MarkGridOccupied(Vector2Int position, Vector2Int size, bool occupied)
{
    // 遍历物品覆盖的所有格子
    for (int x = position.x; x < position.x + size.x; x++)
    {
        for (int y = position.y; y < position.y + size.y; y++)
        {
            // 1. 更新布尔数组
            gridOccupancy[x, y] = occupied;

            // 2. 更新哈希集合
            Vector2Int cell = new Vector2Int(x, y);
            if (occupied)
            {
                occupiedCells.Add(cell);     // 添加占用格子
            }
            else
            {
                occupiedCells.Remove(cell);  // 移除占用格子
            }
        }
    }

    // 3. 重新计算前缀和数组
    UpdatePrefixSum();
}
```

#### 3.2 更新算法特点

- **同步更新**: 三种数据结构同时更新，保证数据一致性
- **增量更新**: 只更新物品覆盖的格子，避免全量更新
- **延迟计算**: 前缀和数组在所有格子更新完成后统一重算

### 4. 前缀和优化算法

#### 4.1 二维前缀和计算

```csharp
protected virtual void UpdatePrefixSum()
{
    // 1. 重置前缀和数组
    for (int i = 0; i <= width; i++)
    {
        for (int j = 0; j <= height; j++)
        {
            prefixSum[i, j] = 0;
        }
    }

    // 2. 计算前缀和
    for (int i = 1; i <= width; i++)
    {
        for (int j = 1; j <= height; j++)
        {
            int cellValue = gridOccupancy[i - 1, j - 1] ? 1 : 0;
            prefixSum[i, j] = cellValue + prefixSum[i - 1, j] + 
                             prefixSum[i, j - 1] - prefixSum[i - 1, j - 1];
        }
    }
}
```

#### 4.2 前缀和算法原理

前缀和数组`prefixSum[i][j]`存储从`(0,0)`到`(i-1,j-1)`矩形区域内所有占用格子的总数。

**递推公式**:
```
prefixSum[i][j] = cellValue + prefixSum[i-1][j] + prefixSum[i][j-1] - prefixSum[i-1][j-1]
```

### 5. 快速查询算法

#### 5.1 O(1)区域占用检测

```csharp
public virtual bool CanPlaceItemFast(Vector2Int position, Vector2Int size)
{
    // 边界检查
    if (position.x < 0 || position.y < 0 ||
        position.x + size.x > width || position.y + size.y > height)
    {
        return false;
    }

    // 使用二维前缀和进行O(1)查询
    int x1 = position.x, y1 = position.y;
    int x2 = position.x + size.x - 1, y2 = position.y + size.y - 1;

    int occupiedCount = prefixSum[x2 + 1, y2 + 1]
                      - prefixSum[x1, y2 + 1]
                      - prefixSum[x2 + 1, y1]
                      + prefixSum[x1, y1];

    return occupiedCount == 0;
}
```

#### 5.2 区域查询公式推导

对于矩形区域`(x1,y1)`到`(x2,y2)`，占用格子数计算公式：

```
occupiedCount = S(x2+1, y2+1) - S(x1, y2+1) - S(x2+1, y1) + S(x1, y1)
```

其中`S(i,j) = prefixSum[i][j]`表示从`(0,0)`到`(i-1,j-1)`的前缀和。

### 6. 性能优化策略

#### 6.1 时间复杂度分析

| 操作类型 | 传统方法 | 优化后方法 | 性能提升 |
|---------|---------|-----------|----------|
| 单格子查询 | O(1) | O(1) | 无变化 |
| 区域占用检测 | O(w×h) | O(1) | 显著提升 |
| 占用率计算 | O(w×h) | O(1) | 显著提升 |
| 状态更新 | O(物品大小) | O(w×h) | 略有下降 |

#### 6.2 空间复杂度权衡

- **额外空间开销**: 约2倍原始网格大小
- **性能收益**: 查询操作从O(n?)降至O(1)
- **适用场景**: 查询频繁、更新相对较少的场景

### 7. 可视化渲染算法

#### 7.1 网格绘制流程

```csharp
private void DrawGridVisualization(BaseItemGrid grid, Vector2Int gridSize)
{
    // 1. 获取占用状态数据
    bool[,] occupancy = grid.GetGridOccupancy();
    
    // 2. 计算显示尺寸
    float maxDisplaySize = 150f;
    float cellDisplaySize = Mathf.Min(maxDisplaySize / Mathf.Max(gridSize.x, gridSize.y), 8f);
    cellDisplaySize = Mathf.Max(cellDisplaySize, 2f);
    
    // 3. 绘制网格背景
    Rect gridRect = GUILayoutUtility.GetRect(gridSize.x * cellDisplaySize, gridSize.y * cellDisplaySize);
    EditorGUI.DrawRect(gridRect, new Color(0.3f, 0.3f, 0.3f, 1f));
    
    // 4. 绘制每个格子
    for (int y = 0; y < gridSize.y; y++)
    {
        for (int x = 0; x < gridSize.x; x++)
        {
            Rect cellRect = new Rect(
                gridRect.x + x * cellDisplaySize,
                gridRect.y + y * cellDisplaySize,
                cellDisplaySize - 1f,
                cellDisplaySize - 1f
            );
            
            // 根据占用状态选择颜色
            Color cellColor = occupancy[x, y] ? Color.red : Color.white;
            EditorGUI.DrawRect(cellRect, cellColor);
        }
    }
    
    // 5. 绘制网格线
    DrawGridLines(gridRect, gridSize, cellDisplaySize);
}
```

#### 7.2 自适应显示算法

- **尺寸计算**: 根据网格大小自动调整显示尺寸
- **最大限制**: 防止大网格占用过多屏幕空间
- **最小保证**: 确保小网格仍然可见

### 8. 算法优势总结

#### 8.1 性能优势

1. **查询效率**: O(1)时间复杂度的区域查询
2. **内存优化**: 三种数据结构各司其职，避免冗余计算
3. **实时响应**: 支持实时的网格状态监控

#### 8.2 扩展性优势

1. **模块化设计**: 各算法模块独立，易于维护
2. **接口统一**: 提供统一的查询接口
3. **可视化支持**: 内置可视化渲染支持

#### 8.3 适用场景

- **库存管理系统**: 快速检测物品放置位置
- **地图编辑器**: 实时显示地块占用状态
- **游戏背包系统**: 高效的物品排列算法
- **资源分配系统**: 快速查找可用空间

## 结论

该算法设计通过三重数据结构的协同工作，实现了高效的网格占用状态管理。在保证查询性能的同时，提供了完整的可视化支持，为Unity游戏开发中的网格系统提供了强有力的技术支撑。

---

*文档版本: 1.0*  
*创建日期: 2024年*  
*适用项目: TPS v0.1-1 Unity项目*