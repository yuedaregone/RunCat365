# 浮动窗动画与移动同步优化实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为猫、马、鹦鹉三个Runner配置帧移动比例，使动画帧与移动距离同步，消除滑步现象。

**Architecture:** 创建静态配置类存储各Runner的帧移动比例数组，FloatingWindow在移动时根据当前帧索引应用对应比例。

**Tech Stack:** C# (.NET 9.0 WPF)

---

## 文件结构

- 创建: `RunCat365/Runners/FrameMoveRatios.cs` - 帧移动比例配置类
- 修改: `RunCat365/FloatingWindow.xaml.cs` - 应用帧移动比例进行移动

---

### Task 1: 创建帧移动比例配置类

**Files:**
- Create: `RunCat365/Runners/FrameMoveRatios.cs`

根据 Runner.cs 中的帧数信息（Cat: 5帧, Horse: 5帧, Parrot: 10帧）配置对应长度的数组。

- [ ] **Step 1: 创建 Runners 目录和 FrameMoveRatios.cs 文件**

```csharp
namespace RunCat365.Runners
{
    public static class FrameMoveRatios
    {
        public static readonly float[] Cat = { 0.3f, 1.0f, 0.9f, 0.7f, 0.1f };
        public static readonly float[] Horse = { 0.5f, 1.0f, 0.7f, 1.0f, 0.9f };
        public static readonly float[] Parrot = { 0.7f, 1.0f, 0.9f, 1.0f, 0.8f, 1.0f, 0.6f, 1.0f, 0.7f, 1.0f };
    }
}
```

- [ ] **Step 2: 在项目中添加新文件**

将 FrameMoveRatios.cs 添加到 .csproj 中确保编译包含。

- [ ] **Step 3: 验证编译**

Run: `dotnet build RunCat365.sln -p:Platform=x64 -p:Configuration=Debug`
Expected: 编译成功

---

### Task 2: 修改 FloatingWindow 应用帧移动比例

**Files:**
- Modify: `RunCat365/FloatingWindow.xaml.cs`

- [ ] **Step 1: 添加 using 和字段**

在文件顶部添加：
```csharp
using RunCat365.Runners;
```

在类中添加两个字段（注意Runner类型来自RunCat365命名空间）：
```csharp
private float[]? frameMoveRatios;
private int currentFrame;
```

- [ ] **Step 2: 添加 SetFrameMoveRatios 方法**

在 SetSpeed 方法后添加：
```csharp
public void SetFrameMoveRatios(Runner runner)
{
    frameMoveRatios = runner switch
    {
        Runner.Cat => FrameMoveRatios.Cat,
        Runner.Horse => FrameMoveRatios.Horse,
        Runner.Parrot => FrameMoveRatios.Parrot,
        _ => null
    };
}
```

- [ ] **Step 3: 修改 SetFrame 方法跟踪帧索引**

将现有的 SetFrame 方法修改为同时更新 currentFrame：
```csharp
public void SetFrame(int index)
{
    if (spritesheet is null) return;

    currentFrame = index;
    double x = index * frameWidth;
    brush.Viewbox = new Rect(x, 0, frameWidth, frameHeight);
}
```

- [ ] **Step 4: 修改 OnMoveTimer 方法应用移动比例**

将现有 OnMoveTimer 方法修改：
```csharp
private void OnMoveTimer(object? sender, EventArgs e)
{
    if (currentSpeed <= 0) return;

    float ratio = 1.0f;
    if (frameMoveRatios != null && currentFrame < frameMoveRatios.Length)
    {
        ratio = frameMoveRatios[currentFrame];
    }

    Left += currentSpeed * ratio;

    double screenWidth = SystemParameters.PrimaryScreenWidth;
    if (Left > screenWidth)
    {
        Left = -Width;
    }
    else if (Left < -Width)
    {
        Left = screenWidth;
    }
}
```

- [ ] **Step 5: 在启动时加载当前Runner的配置**

找到 FloatingWindow 构造函数或 StartMoveTimer 方法，在初始化时调用 SetFrameMoveRatios。

需要从 AppConfig.Instance.Runner 获取当前Runner并转换类型。

- [ ] **Step 6: 验证编译**

Run: `dotnet build RunCat365.sln -p:Platform=x64 -p:Configuration=Debug`
Expected: 编译成功

---

### Task 3: 验证功能

- [ ] **Step 1: 运行应用测试**

Run: `dotnet run --project RunCat365/RunCat365.csproj`

观察浮动窗移动时是否有滑步现象。根据实际效果调整 FrameMoveRatios.cs 中的比例值。

- [ ] **Step 2: 提交代码**

```bash
git add RunCat365/Runners/FrameMoveRatios.cs RunCat365/FloatingWindow.xaml.cs
git commit -m "feat: 添加帧移动比例配置，消除动画与移动不匹配的滑步现象"
```

---

## 验收标准

1. 编译通过，无错误
2. 运行时浮动窗的移动距离与动画帧同步，无明显滑步现象
3. 切换Runner时自动加载对应配置