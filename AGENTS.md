# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 构建与开发指令
- **构建解决方案**: `dotnet build RunCat365.sln -p:Platform=x64 -p:Configuration=Debug`
- **运行应用**: `dotnet run --project RunCat365/RunCat365.csproj`
- **发布应用**: `dotnet publish RunCat365/RunCat365.csproj -c Release -r win-x64 --self-contained`

## 项目现状
- **技术栈**: 纯 **WPF** 架构 (.NET 9.0)，已废弃并移除 WinForms。使用 `Hardcodet.NotifyIcon.Wpf` 处理系统托盘逻辑。
- **功能目标**: 该项目目前作为**番茄钟**运行，动画逻辑完全依赖番茄钟进度。已废弃所有系统负载监控（CPU/内存等）功能。
- **文件状态**: 磁盘上相关的 `CPURepository.cs` 等旧文件以及 `EndlessGameForm.cs` 已废弃，`.csproj` 已相应更新。

## 架构说明
- **入口**: `Program.cs`。直接启动 WPF `Application`，使用 `RunCat365ApplicationContext` 管理托盘、悬浮窗和番茄钟。
- **UI 管理**: 
  - `ContextMenuManager.cs`: 基于 WPF 实现의 托盘菜单，管理 Runner 切换、番茄钟控制及开机自启。
  - `FloatingWindow.xaml`: 桌面动态悬浮窗。
- **核心逻辑**:
  - `TomatoClock.cs`: 番茄钟核心计时器，支持开始、暂停、重置。
  - `FrameAnimationEngine.cs`: 基于番茄钟进度驱动的 Runner 逐帧动画引擎。
  - `LaunchAtStartupManager.cs`: 负责开机自启动逻辑。
- **多语言与资源**: 
  - 文本资源位于 `RunCat365/Properties/Strings.resx`（支持 7 种语言）。
  - 动画图片资源位于 `RunCat365/resources/runners/`。

## 编码规范
- **回复语言**: 始终使用**中文**。
- **核心原则**: 
  - 严禁在源码中写注释。
  - 遵循纯 WPF 模式，避免任何 WinForms 命名空间或习惯。
- **格式规范**: 
  - Allman 缩进风格（大括号另起一行）。
  - 显式类型优于 `var`（仅在类型声明冗余时使用 `var`）。
- **命名规范**: 
  - PascalCase 用于类/方法，camelCase 用于变量/参数。
  - 缩写（如 URL, ID）需全大写或全小写（如使用 `url` 或 `URL`，而非 `Url`）。
  - 禁止使用模糊简写（如使用 `image` 而非 `img`）。
