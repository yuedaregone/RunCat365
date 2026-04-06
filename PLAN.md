# RunCat 365 重构与清理计划

该计划分为两个阶段：第一阶段侧重于物理文件的清理和核心功能的解耦；第二阶段侧重于性能优化、架构改进和运行稳定性的提升。

## 第一阶段：基础重构与物理清理

### 1. 物理文件与资源清理
- **移除游戏素材**: 删除 `RunCat365/resources/game/` 目录及其下的所有 PNG 文件。
- **清理本地化资源**: 在 `Strings.resx`（及所有语言版本）中移除 `Menu_CPU`, `Menu_Memory`, `Menu_Game` 等废弃键值。
- **项目文件校验**: 确保 `RunCat365.csproj` 中不再包含对已删除文件的任何显式或隐式引用。

### 2. 代码解耦与精简
- **ResourceLoader.cs 瘦身**: 移除 `GetGamePixels` 等游戏相关方法，专注于 `runners` 加载。
- **动画逻辑解耦**: `FrameAnimationEngine.cs` 仅保留依赖 `TomatoClock` 进度的计算逻辑。
- **菜单精简**: `ContextMenuManager.cs` 中移除所有与系统监控相关的菜单项，仅保留番茄钟控制和 Runner 切换。

### 3. 符合 CLAUDE.md 规范
- **代码风格**: 统一使用 **Allman 缩进**。
- **显式类型**: 将冗余的 `var` 替换为显式类型声明。
- **移除注释**: 清理所有源文件中的冗余中文/英文注释。

---

## 第二阶段：性能优化与深度改进

### 1. 性能与能效优化
- **动画引擎升级**: 将 `FrameAnimationEngine` 从 `CompositionTarget.Rendering` 切换为按需唤醒的 `DispatcherTimer`，显著降低 CPU 占用。
- **精灵图多级缓存**: 在 `ResourceLoader` 中实现 `WriteableBitmap` 缓存，避免反复切换 Runner 时重复生成精灵图。
- **资源索引加速**: 启动时预扫描一次资源清单 (Manifest Resource Names) 并建立哈希表，消除高频查找开销。

### 2. 架构与现代化
- **局部 MVVM 重构**: 引入数据绑定机制，将 `TomatoClock` 作为模型，通过 ViewModel 驱动悬浮窗显示。
- **配置系统升级**: 考虑将 `.settings` 迁移至现代化的 `JSON` 配置，提高可读性和扩展性。
- **视觉平滑化**: 在动画速度计算中引入缓动函数 (Easing)，使番茄钟快结束时的加速过程更平滑。

### 3. 健壮性增强
- **异常拦截**: 加强资源加载时的容错处理，防止因单个图片损坏导致应用崩溃。
- **多实例协同**: 优化 `Mutex` 逻辑，当重复启动时自动置顶/激活当前悬浮窗。
