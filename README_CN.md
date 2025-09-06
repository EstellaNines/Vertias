# TPS 战斗系统

[![Unity 版本](https://img.shields.io/badge/Unity-2022.3+-blue.svg?logo=unity)](https://unity3d.com/get-unity/download)
[![C# 版本](https://img.shields.io/badge/C%23-9.0+-purple.svg?logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![许可证](https://img.shields.io/badge/License-MIT-green.svg?logo=opensourceinitiative)](LICENSE)
[![平台支持](https://img.shields.io/badge/Platform-Windows%20%7C%20Mac%20%7C%20Linux-lightgrey.svg?logo=unity)](https://unity3d.com/)
[![开发状态](https://img.shields.io/badge/Status-开发中-yellow.svg?logo=github)](https://github.com/yourusername/TPS-Combat-System)
[![构建状态](https://img.shields.io/badge/Build-通过-brightgreen.svg?logo=github-actions)](https://github.com/yourusername/TPS-Combat-System/actions)
[![代码质量](https://img.shields.io/badge/Code%20Quality-A-brightgreen.svg?logo=codeclimate)](https://github.com/yourusername/TPS-Combat-System)
[![贡献者](https://img.shields.io/badge/Contributors-欢迎-orange.svg?logo=github)](CONTRIBUTING.md)

**[English](README_EN.md) | 中文**

一个基于 Unity 开发的 2D 第三人称射击（TPS）战斗系统，具有先进的 AI、库存管理和动态游戏机制。

## 📊 项目统计

![GitHub 仓库大小](https://img.shields.io/github/repo-size/yourusername/TPS-Combat-System?logo=github)
![GitHub 代码大小](https://img.shields.io/github/languages/code-size/yourusername/TPS-Combat-System?logo=github)
![代码行数](https://img.shields.io/tokei/lines/github/yourusername/TPS-Combat-System?logo=github)
![GitHub 最后提交](https://img.shields.io/github/last-commit/yourusername/TPS-Combat-System?logo=github)
![GitHub 提交活跃度](https://img.shields.io/github/commit-activity/m/yourusername/TPS-Combat-System?logo=github)

## 🛠️ 技术栈

| 技术         | 徽章                                                                                                                                                                                             |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **游戏引擎** | [![Unity](https://img.shields.io/badge/Unity-2022.3+-000000?style=for-the-badge&logo=unity)](https://unity3d.com/)                                                                               |
| **编程语言** | [![C#](https://img.shields.io/badge/C%23-9.0+-239120?style=for-the-badge&logo=csharp)](https://docs.microsoft.com/en-us/dotnet/csharp/)                                                          |
| **AI 寻路**  | [![A* Pathfinding](https://img.shields.io/badge/A*_Pathfinding-Project-blue?style=for-the-badge&logo=unity)](https://arongranberg.com/astar/)                                                    |
| **UI 框架**  | [![TextMeshPro](https://img.shields.io/badge/TextMeshPro-Unity-red?style=for-the-badge&logo=unity)](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html)               |
| **输入系统** | [![Unity Input System](https://img.shields.io/badge/Unity_Input_System-New-green?style=for-the-badge&logo=unity)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/index.html) |
| **版本控制** | [![Git](https://img.shields.io/badge/Git-F05032?style=for-the-badge&logo=git&logoColor=white)](https://git-scm.com/)                                                                             |

## 🎮 功能特性

- 🤖 **先进 AI 系统**：基于状态机的敌人和丧尸 AI，支持寻路功能
- 🎒 **库存管理**：基于网格的库存系统，支持装备栏位
- ⚔️ **战斗机制**：真实的子弹物理系统和散布模式
- 🌍 **场景管理**：无缝场景过渡和加载界面
- 🖥️ **UI 系统**：全面的用户界面，包括库存、任务和地图
- 🥷 **潜行系统**：基于蹲伏的潜行机制和敌人检测
- 🔫 **武器系统**：玩家和敌人的动态武器处理
- 🎯 **任务系统**：基于 JSON 的动态任务管理
- 🗺️ **地图系统**：交互式世界地图和快速传送功能
- 💾 **保存系统**：持久化数据存储，通过 114+稳定性测试

## 📸 游戏截图与演示

### 🎮 游戏截图

<div align="center">

|                                     战斗系统                                      |                                       库存管理                                       |
| :-------------------------------------------------------------------------------: | :----------------------------------------------------------------------------------: |
| ![战斗演示](https://via.placeholder.com/400x300/2D5AA0/FFFFFF?text=Combat+System) | ![库存演示](https://via.placeholder.com/400x300/28A745/FFFFFF?text=Inventory+System) |

|                                     AI 寻路                                      |                                    地图系统                                    |
| :------------------------------------------------------------------------------: | :----------------------------------------------------------------------------: |
| ![AI演示](https://via.placeholder.com/400x300/DC3545/FFFFFF?text=AI+Pathfinding) | ![地图演示](https://via.placeholder.com/400x300/FFC107/000000?text=Map+System) |

</div>

### 🎬 演示视频

- 🎯 [**战斗系统演示**](https://github.com/yourusername/TPS-Combat-System/releases) - 展示武器机制和 AI 战斗
- 🎒 [**库存系统演示**](https://github.com/yourusername/TPS-Combat-System/releases) - 基于网格的库存管理
- 🤖 [**AI 寻路演示**](https://github.com/yourusername/TPS-Combat-System/releases) - 敌人 AI 和导航系统

## 🔧 安装

### 前置要求

- Unity 2022.3 LTS 或更高版本
- Git 版本控制

### 快速开始

1. 克隆仓库：

```bash
git clone https://github.com/yourusername/TPS-Combat-System.git
```

2. 在 Unity Hub 中打开项目

3. 导入必需的包：

   - A\* Pathfinding Project
   - TextMeshPro
   - Input System

4. 加载主场景：`Scenes场景/Shelter.unity`
5. 按下播放按钮开始测试

## 🎮 操作控制

| 操作     | 按键       | 描述          |
| -------- | ---------- | ------------- |
| 移动     | WASD       | 角色移动      |
| 瞄准     | 鼠标       | 瞄准武器      |
| 开火     | 鼠标左键   | 射击武器      |
| 换弹     | R          | 重新装弹      |
| 蹲伏     | C          | 进入潜行模式  |
| 奔跑     | Left Shift | 冲刺          |
| 闪避     | Left Ctrl  | 翻滚闪避      |
| 库存界面 | Tab        | 打开/关闭库存 |
| 拾取     | F          | 拾取物品      |

## 🏗️ 系统架构

### 🏛️ 核心系统

| 系统             | 描述                       | 状态    |
| ---------------- | -------------------------- | ------- |
| 🤖 **状态机 AI** | 敌人和丧尸的模块化 AI 系统 | ✅ 完成 |
| 🎒 **网格库存**  | 高级库存管理与持久化存储   | ✅ 完成 |
| 🌍 **场景管理**  | 无缝过渡与加载界面         | ✅ 完成 |
| ⚔️ **战斗系统**  | 真实弹道学和武器处理       | ✅ 完成 |
| 🖥️ **UI 框架**   | 全面的界面系统             | ✅ 完成 |
| 💾 **保存系统**  | 持久化数据，通过 114+测试  | ✅ 完成 |
| 🎯 **任务系统**  | 基于 JSON 的任务管理       | ✅ 完成 |
| 🗺️ **地图系统**  | 交互式世界导航             | ✅ 完成 |

### 主要组件

```
Assets/
├── Scripts脚本/
│   ├── Player玩家/         # 玩家控制器和状态
│   ├── Enemy敌人/          # AI敌人系统
│   ├── UI用户界面/         # 用户界面组件
│   ├── Weapon武器/         # 武器和战斗系统
│   └── Items物品/          # 物品管理
├── Scenes场景/             # 游戏场景
├── Prefab预制体/           # 游戏对象预制体
└── Resources/              # 游戏资源和数据
```

## 📊 开发统计

### 📈 项目指标

| 指标              | 数值     | 描述           |
| ----------------- | -------- | -------------- |
| 💻 **总提交数**   | 300+     | 活跃的开发历史 |
| ⏰ **开发时间**   | 3+ 个月  | 持续开发       |
| 🧪 **稳定性测试** | 114 通过 | 库存系统可靠性 |
| 🏗️ **核心系统**   | 8 已实现 | 主要游戏系统   |
| 📁 **脚本文件**   | 76+      | C# 游戏脚本    |
| 🎨 **UI 面板**    | 15+      | 用户界面组件   |
| 🎮 **游戏场景**   | 5        | 可玩环境       |
| 🔫 **武器类型**   | 10+      | 不同武器类别   |

### 🎯 完成状态

```
🤖 AI系统           ████████████████████ 100%
🎒 库存系统         ████████████████████ 100%
⚔️ 战斗系统         ████████████████████ 100%
🌍 场景管理         ████████████████████ 100%
🖥️ UI系统           ████████████████████ 100%
💾 保存系统         ████████████████████ 100%
🎯 任务系统         ████████████████████ 100%
🗺️ 地图系统         ████████████████████ 100%
```

### 🏆 质量保证

- ✅ **代码质量**：A 级，具有清晰的架构
- ✅ **性能**：优化的状态机和对象池
- ✅ **稳定性**：114 项保存/加载功能的综合测试
- ✅ **可维护性**：文档完善的模块化代码库

## 📋 更新日志

### 🎯 v0.2.x - 高级系统实现 (2024 年 6-9 月)

#### v0.2.8 - 完整库存系统 (2024-09-04) 🎒

- **✨ 主要功能**：完整库存系统实现，包含 6 个装备栏位：头盔、护甲、主武器、副武器、战术挂具、背包
- **🔧 技术改进**：动态网格系统，支持仓库和地面存储，持久化保存系统
- **🎮 用户体验**：114 项稳定性测试通过，网格内物品自由移动，高亮选择和放置指示器

#### v0.2.7 - 背包功能重置 (2024-08-28)

- **🎒 背包系统**：玩家 UI 背包具备拖拽、放置、高亮显示、放置提示、旋转物品、保存物品、加载物品、装备物品到装备栏功能

#### v0.2.6 - 网格系统完善 (2024-08-10)

- **🔧 网格系统修复**：重新设计网格系统，解决坐标错误问题
- **🎒 装备栏设计**：成功实现物品放入装备栏功能
- **📐 坐标系统**：修复生成器正常生成物品的坐标系统

#### v0.2.5 - 物品系统重构 (2024-08-01)

- **📦 物品数据重设**：采用 JSON 文件设计基础保存数据，创建 SO 对象生成脚本
- **🔄 物品结构修改**：使用独立预制体，通过脚本一键创建基于 SO 对象的预制体
- **🎮 网格背包构建**：完成基础网格背包，支持自定义背包大小、选取、存放、边界控制、高亮功能

#### v0.2.4 - UI 系统完善 (2024-07-13)

- **🖥️ UI 交互功能**：增加整个界面的交互功能，支持点击按钮打开对应 UI 界面
- **📋 任务界面**：完成任务界面完整布置，添加任务列表、任务描述等界面元素
- **📄 JSON 数据管理**：使用 JSON 文件存储任务信息，增加任务管理器

#### v0.2.3 - 场景与 UI 扩展 (2024-07-05)

- **🗺️ 场景系统**：增加购物中心场景，完善藏身处地图碰撞和边界
- **🔄 场景切换**：增加通用传送脚本，支持室内外场景切换
- **🎮 UI 显示**：增加换弹 UI 显示、武器 UI 显示

#### v0.2.2 - 武器系统分离 (2024-07-01)

- **🔫 武器管理**：武器与玩家敌人操作分离，通过管理器脚本将武器单独隔离
- **🤖 敌人系统**：敌人受伤和死亡状态加入状态机，敌人拥有生命值可被击杀
- **🧟 丧尸修复**：修复丧尸攻击机制，攻击完成后正常巡逻和追击

#### v0.2.1 - AI 系统重构 (2024-06-23) 🎯

- **🤖 完整丧尸状态机**：重置所有丧尸敌人脚本，使用 IState 接口统一状态控制
- **🔧 开发工具**：检查器中文变量显示，改善开发者体验
- **⚡ 性能改进**：移除动画器过渡状态，使用状态机控制，废弃旧版功能脚本

#### v0.2.0 - 项目扩展 (2024-06-10)

- **📦 项目管理**：新增 Gitee 仓库存放和备份项目，保障源代码安全
- **🤝 协作增强**：方便团队成员代码共享、协作和版本管理

### 🚀 v0.1.x - 基础系统建立 (2024 年 6 月)

#### v0.1.2-1 - 武器系统优化 (2024-06-09)

- **🐛 武器修复**：修复武器 Y 轴翻转错误，改善武器显示和操作方向
- **🔧 逻辑优化**：将翻转逻辑从 Hand 对象转移至 Weapon 对象
- **📝 脚本新增**：新建物品管理脚本，为物品系统扩展提供基础

#### v0.1.2 - 系统稳定性 (2024-06-08)

- **🐛 碰撞修复**：修复玩家角色碰撞体触发器问题
- **⚡ 性能优化**：重制玩家拾取物品功能，提高系统稳定性和流畅度

#### v0.1.1 - 战斗机制 (2024-06-07)

- **✨ 新功能**：添加子弹散布功能，提供更真实的射击体验
- **🐛 问题修复**：修复角色、敌人、丧尸、障碍图层的碰撞与触发器问题

## 🤝 贡献

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开 Pull Request

## 📝 许可证

本项目基于 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 🙏 致谢

- A\* Pathfinding Project 提供导航系统
- Unity Technologies 提供游戏引擎
- 社区贡献者和测试人员

---

_最后更新：2025 年 9 月_
