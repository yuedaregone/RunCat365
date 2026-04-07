# 浮动窗动画与移动同步优化设计

## 背景

当前浮动窗动画与移动不匹配，会造成滑步现象。原因是动画每一帧的移动距离是固定的，而实际上不同帧应该移动不同的距离，模拟真实奔跑时的步幅变化。

## 目标

为每个Runner配置独立的帧移动比例，使移动距离与动画帧同步，消除滑步现象。

## 设计方案

### 配置结构

在 `Runners/` 目录创建一个 `FrameMoveRatios.cs` 类，包含所有Runner的静态只读数组：

```csharp
namespace RunCat365.Runners
{
    public static class FrameMoveRatios
    {
        public static readonly float[] Cat = { 0.6f, 1.0f, 0.8f, 1.0f, 0.5f, 1.0f, 0.7f, 1.0f };
        public static readonly float[] Horse = { 0.5f, 1.0f, 0.7f, 1.0f, 0.6f, 1.0f, 0.8f, 1.0f };
        public static readonly float[] Parrot = { 0.7f, 1.0f, 0.9f, 1.0f, 0.8f, 1.0f, 0.6f, 1.0f };
    }
}
```

- 以Runner名称（如Cat、Horse、Parrot）作为静态字段名，值为该Runner的帧移动比例数组

### 实现修改

#### 1. 新增Runner配置类

在 `Runners/` 目录创建 `FrameMoveRatios.cs`，为所有Runner配置帧移动比例：

| Runner | 字段名 | 帧数 | 说明 |
|--------|--------|------|------|
| 猫 | Cat | 8 | 猫奔跑的步幅配置 |
| 马 | Horse | 8 | 马奔跑的步幅配置 |
| 鹦鹉 | Parrot | 8 | 鹦鹉飞行的步幅配置 |

#### 2. 修改 FloatingWindow.xaml.cs

- 新增字段存储当前Runner的帧移动比例数组
- 新增方法 `SetFrameMoveRatios(float[] ratios)` 供外部调用
- 修改 `OnMoveTimer` 方法，根据当前帧索引应用对应的移动比例

```csharp
private float[]? frameMoveRatios;

public void SetFrameMoveRatios(float[] ratios)
{
    frameMoveRatios = ratios;
}

private void OnMoveTimer(object? sender, EventArgs e)
{
    if (currentSpeed <= 0) return;

    float ratio = 1.0f;
    if (frameMoveRatios != null && currentFrame < frameMoveRatios.Length)
    {
        ratio = frameMoveRatios[currentFrame];
    }

    Left += currentSpeed * ratio;
    // ... 边界处理保持不变
}
```

- 新增字段 `currentFrame` 跟踪当前帧索引
- 修改 `SetFrame(int index)` 方法，在切换帧时同步更新 `currentFrame`

#### 3. 修改 ContextMenuManager 或 Runner.cs

在切换Runner时，调用 `FloatingWindow.SetFrameMoveRatios()` 加载对应配置。

### 数据流

```
用户切换Runner
    ↓
ContextMenuManager/Runner.cs
    ↓
FloatingWindow.SetFrameMoveRatios(ratios)
    ↓
AnimationTimer触发帧切换 → SetFrame(index)
    ↓
MoveTimer触发移动 → OnMoveTimer() 使用 frameMoveRatios[index] 计算实际移动距离
```

## 配置值说明

帧移动比例反映奔跑时的物理特性：

- 脚落地时（帧索引0、2、4等）：比例较小（如0.5-0.8），模拟支撑重量
- 腾空时（帧索引1、3、5等）：比例较大（如1.0），模拟跃步

具体值需要根据各Runner的动画素材实际测试调整。

## 风险与约束

1. **数组长度需与动画帧数匹配**：配置数组长度必须等于实际动画帧数，否则使用默认值1.0
2. **基准速度仍由番茄钟控制**：移动比例仅作为乘数，不改变基准速度逻辑
3. **向后兼容**：如果未配置某Runner的帧移动比例，默认使用1.0（等速移动）

## 验收标准

1. 切换到已配置帧移动比例的Runner时，动画帧与移动距离同步，无滑步现象
2. 未配置的Runner保持原有等速移动行为
3. 切换Runner时配置正确加载