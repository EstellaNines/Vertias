# Third-Party Plugins Management

This document records the organizational structure, functionality, and usage guide for all third-party plugins in the TPS project.

## 📁 Plugin Category Structure

### 🎬 Animation Plugins

#### DOTween & DOTweenPro (Demigiant)

- **Function**: Unity's most popular animation tweening library, providing high-performance object animation
- **Version**: Pro version with additional advanced features
- **Usage**: UI animations, object movement, rotation, scaling and other tween animations
- **Documentation**: `readme_DOTweenPro.txt`
- **Examples**: `DOTweenPro Examples/` folder contains complete example scenes

### 🛤️ Pathfinding Algorithms

#### A\* Pathfinding Project

- **Function**: Unity's most powerful 2D/3D pathfinding solution
- **Usage**: Enemy AI navigation, NPC movement path planning
- **Features**:
  - Supports grid graphs, navmesh, point graphs and various graph types
  - High-performance multi-threaded computation
  - Visual debugging tools
- **Documentation**: `Documentation/` folder
- **Configuration**: Configure through A\* Inspector window

### 💾 Data Persistence

#### Easy Save 3

- **Function**: Unity's powerful save system supporting multiple data formats
- **Usage**: Game progress saving, settings storage, player data persistence
- **Features**:
  - Supports JSON, binary, encrypted storage
  - Automatic reference management
  - Cloud storage support
- **Documentation**: `Change Log/Change Log.txt`
- **Network**: Contains PHP cloud storage scripts

### 🔧 Editor Enhancements

#### Ultimate Editor Enhancer (Infinity Code)

- **Function**: Unity editor super enhancement toolkit
- **Features**:
  - Smart object selection
  - Enhanced scene view tools
  - Improved inspector interface
  - Quick action panels
- **Configuration**: Setup through Window > Ultimate Editor Enhancer

#### Better Hierarchy

- **Function**: Enhance Unity hierarchy panel display
- **Features**:
  - Colored icon display
  - Component status indicators
  - Custom display rules
- **Examples**: `Demo Scene/` contains demonstration scenes

#### vFolders

- **Function**: Project window folder beautification tool
- **Features**: Custom folder icons and colors

#### vHierarchy

- **Function**: Hierarchy window enhancement tool
- **Features**: Improved hierarchy display and operations

#### vInspector

- **Function**: Inspector panel enhancement tool
- **Features**: Improved property display and editing experience

#### vTabs

- **Function**: Editor tab management tool
- **Features**: Enhanced window tab management

### 🛠️ Developer Tools

#### ConsolePro

- **Function**: Enhanced Unity console tool
- **Features**:
  - Improved log display
  - Remote debugging support
  - Advanced filtering capabilities
- **Documentation**: `Editor Console Pro Documentation.pdf`

#### Wingman

- **Function**: Unity editor assistant tool
- **Features**:
  - Quick component copying
  - Clipboard enhancement
  - Shortcut operation support

### ⚡ Performance Monitoring

#### Easy Framerate Counter (Alterego Games)

- **Function**: Simple and easy-to-use framerate monitoring tool
- **Features**:
  - Real-time FPS display
  - Performance statistics
  - Lightweight implementation
- **Documentation**: `Documentation.docx`

### 🖥️ UI Framework

#### TextMesh Pro

- **Function**: Unity's official advanced text rendering system
- **Features**:
  - High-quality text rendering
  - Rich text styling
  - Dynamic font support
- **Documentation**: `Documentation/` folder
- **Fonts**: `Fonts/` contains various font resources
- **Shaders**: `Shaders/` contains specialized shaders

## 🔗 Package Manager Dependencies

The following plugins are managed through Unity Package Manager:

### Unity Official Packages

- **Input System** (1.11.2) - New input system
- **Universal RP** (14.0.11) - Universal render pipeline
- **Cinemachine** (2.10.3) - Virtual camera system
- **2D Feature** (2.0.1) - 2D game development features
- **Shader Graph** (14.0.11) - Visual shader editor
- **Visual Scripting** (1.9.4) - Visual scripting system

### Third-Party Packages

- **Unity MCP Bridge** - MCP tools integration
- **Cursor IDE Integration** - Cursor editor integration
- **Trae IDE Integration** - Trae editor integration

## 📋 Usage Guidelines

### Installing New Plugins

1. Place plugin in corresponding category folder
2. Update this document to record plugin information
3. Check plugin dependencies
4. Test plugin functionality completeness

### Plugin Updates

1. Backup current plugin version
2. Follow plugin official update guidelines
3. Test project compatibility
4. Update documentation version information

### Removing Plugins

1. Check plugin references in project
2. Remove related script and asset references
3. Delete plugin folder
4. Clean up project settings

## 🔍 Plugin Status Check

### Currently Active Plugins

✅ DOTween - Animation system running normally  
✅ A\* Pathfinding - Enemy navigation system in use  
✅ Easy Save 3 - Save system integrated  
✅ Ultimate Editor Enhancer - Editor enhancement active  
✅ TextMesh Pro - UI text rendering  
✅ Easy Framerate Counter - Performance monitoring

### Plugin Dependencies

- DOTween ← UI animation system
- A\* Pathfinding ← Enemy AI system
- Easy Save 3 ← Game save system
- TextMesh Pro ← All UI text display

## ⚠️ Important Notes

1. **Version Compatibility**: Ensure all plugins are compatible with Unity 2022.3 LTS
2. **Performance Impact**: Regularly check plugin impact on project performance
3. **Licenses**: Comply with each plugin's license agreements
4. **Backup**: Regularly backup important plugin configurations
5. **Documentation Updates**: Update this document promptly when plugins change

## 📞 Technical Support

If you encounter plugin-related issues:

1. Consult plugin official documentation
2. Check Unity Console error information
3. Verify plugin version compatibility
4. Contact plugin developer technical support

---

_Last Updated: September 6, 2025_
_Document Version: v1.0_
