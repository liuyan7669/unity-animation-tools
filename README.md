# Animator Player

面向 Unity 项目的动画辅助工具包，提供 Animator 状态播放、完成回调、顺序与重复播放、运行时动画事件，以及 Humanoid 骨骼节点映射功能。

包内还提供可选的 DOTween 控制器 Sample，用于按速度控制 Transform 和角色的移动、路径移动及旋转。

## 功能

### Animator 播放控制

`AnimatorPlayer` 对 Unity Animator 的常用播放操作进行了统一封装：

- 播放指定 Animator State
- 从指定归一化时间开始播放
- 动画播放完成回调
- 从 StateName 列表中随机播放一个动画
- 随机且不重复地播放 StateName 列表中的全部动画
- 按列表顺序连续播放多个动画
- 按指定次数重复播放动画
- 固定到动画第一帧、指定进度或最后一帧
- 调整 Animator 播放速度
- 暂停和恢复当前动画
- 查询当前动画片段与 Animator State 信息
- 支持继承并重写 `virtual` 方法扩展项目行为

### 运行时动画事件

可以通过代码为动画片段注册带唯一键的运行时事件：

- 按动画片段名称和事件键添加回调
- 指定事件触发时间
- 可选择替换已存在的同键事件
- 移除单个事件
- 清除指定动画片段或全部动画片段的事件
- 根据事件键手动触发回调

### Humanoid 骨骼映射

`CharacterBodyParts` 用于维护 `HumanBodyBones` 与场景 Transform 的对应关系：

- Reset 时自动生成全部有效 Humanoid 骨骼配置项
- 在 Inspector 中配置骨骼对应节点
- 通过 `GetPart` 获取指定骨骼节点
- 通过 `TryGetPart` 安全查询骨骼节点
- 提供专用属性绘制器，便于在 Inspector 中编辑映射

## 安装

### 通过 OpenUPM 安装

在 Unity 的 `Edit > Project Settings > Package Manager` 中添加：

```text
Name: package.openupm.com
URL: https://package.openupm.com
Scope: com.cowart.animator-player
```

然后在 Package Manager 中选择 `Add package by name`，输入：

```text
com.cowart.animator-player
```

### 通过 Git URL 安装

在 Package Manager 中选择 `Add package from git URL`，输入：

```text
https://github.com/liuyan7669/unity-animator-player.git#3.0.0
```

## 使用方法

运行时代码统一使用以下命名空间：

```csharp
using Cowart.AnimatorPlayer;
```

### 播放单个动画

将 `AnimatorPlayer` 添加到带有 Animator 的 GameObject，然后调用：

```csharp
using Cowart.AnimatorPlayer;
using UnityEngine;

public class AnimationExample : MonoBehaviour
{
    [SerializeField] private AnimatorPlayer animationController;

    private void Start()
    {
        animationController.Play("Walk", () =>
        {
            Debug.Log("Walk 播放完成");
        });
    }
}
```

如果目标 GameObject 没有 Animator，首次访问 `AnimatorComponent` 时会自动添加。

### 从指定进度播放

`normalizedTime` 使用 0 到 1 的归一化时间：

```csharp
animationController.Play(
    "Walk",
    0.5f,
    () => Debug.Log("从中间开始播放并完成"));
```

### 随机播放动画

从列表中的有效 StateName 里随机选择一个动画播放，播放完成后执行回调：

```csharp
using System.Collections.Generic;

List<string> states = new List<string>
{
    "Idle",
    "Walk",
    "Run"
};

int selectedIndex = animationController.PlayRandom(
    states,
    () => Debug.Log("随机动画播放完成"));

Debug.Log($"随机抽中的列表索引：{selectedIndex}");
```

返回值是随机动画在原列表中的索引；列表中没有有效 StateName 时返回 `-1`。

### 随机播放整个动画列表

随机抽取并播放列表中的动画。每个动画只会被抽取一次，全部播放完成后执行回调：

```csharp
animationController.PlayRandomList(
    states,
    stateName => Debug.Log($"{stateName} 播放完成"),
    () => Debug.Log("随机列表全部播放完成"));
```

每个随机动画播放完成时会通过 `stateName` 返回动画状态名称。方法会创建内部列表副本，并只从副本中移除已抽取项，不会修改传入的 `states`。

### 顺序播放动画列表

```csharp
using System.Collections.Generic;

List<string> states = new List<string>
{
    "Idle",
    "Walk",
    "Run"
};

animationController.PlayList(
    0,
    states,
    index => Debug.Log($"第 {index} 个动画播放完成"),
    () => Debug.Log("动画列表全部播放完成"));
```

### 重复播放动画

```csharp
animationController.PlayWhile(
    "Attack",
    3,
    () => Debug.Log("重复播放完成"),
    remaining => Debug.Log($"开始播放，剩余次数：{remaining}"),
    remaining => Debug.Log($"本次播放完成，剩余次数：{remaining}"));
```

### 固定动画画面

```csharp
animationController.FixedToFirstFrame("Idle");
animationController.FixedToNormalizedTime("Walk", 0.5f);
animationController.FixedToLastFrame("Victory");
```

### 设置速度与暂停状态

```csharp
animationController.PlaySpeed = 1.5f;
animationController.IsPause = true;
animationController.IsPause = false;
```

### 添加运行时动画事件

```csharp
animationController.AddEvent(
    "Attack",
    "ApplyDamage",
    () => Debug.Log("触发伤害判定"),
    0.35f);
```

管理事件：

```csharp
animationController.ManuallyCallEvent("Attack", "ApplyDamage");
animationController.RemoveEvent("Attack", "ApplyDamage");
animationController.ClearEvent("Attack");
animationController.ClearEvent();
```

同一动画片段内的 `actionKey` 应保持唯一。需要覆盖已存在事件时，将 `forceReplace` 设置为 `true`。

### 查询 Humanoid 骨骼节点

将 `CharacterBodyParts` 添加到角色根节点，通过 Inspector 配置骨骼映射：

```csharp
using Cowart.AnimatorPlayer;
using UnityEngine;

public class BodyPartExample : MonoBehaviour
{
    [SerializeField] private CharacterBodyParts bodyParts;

    private void Start()
    {
        Transform rightHand = bodyParts.GetPart(HumanBodyBones.RightHand);

        if (bodyParts.TryGetPart(HumanBodyBones.Head, out Transform head))
        {
            Debug.Log($"头部节点：{head.name}");
        }
    }
}
```

## DOTween Controllers Sample

从 `1.0.1` 开始，Package Manager 的 Samples 区域提供：

```text
DOTween Controllers
```

导入前必须先安装 DOTween。该 Sample 不会在默认安装包时参与编译，只有点击 Import 后才会复制到项目的 `Assets/Samples` 目录。

导入后的代码使用：

```csharp
using Cowart.AnimatorPlayer.DOTween;
```

### TransformTweenController

提供以下能力：

- 按世界单位/秒移动到目标位置
- 沿 Transform 路径点以恒定速度移动
- 路径移动期间自动朝向前方
- 路径点开始和到达回调
- 按角度/秒旋转到目标朝向
- 单独停止移动或旋转补间
- 自动清理正在运行的 Tween

示例：

```csharp
using Cowart.AnimatorPlayer.DOTween;
using UnityEngine;

public class TweenMovementExample : MonoBehaviour
{
    [SerializeField] private TransformTweenController controller;
    [SerializeField] private Transform target;
    [SerializeField] private Transform[] pathPoints;

    public void MoveToTarget()
    {
        controller.MoveTo(target, () => Debug.Log("移动完成"));
    }

    public void MoveAlongPath()
    {
        controller.MoveAlongPath(
            pathPoints,
            true,
            () => Debug.Log("路径移动完成"));
    }
}
```

### CharacterTweenController

`CharacterTweenController` 当前继承 `TransformTweenController` 的全部行为，并作为角色专用扩展入口，便于后续增加角色相关控制。

## 主要类型

| 类型 | 说明 |
| --- | --- |
| `AnimatorPlayer` | Animator 播放控制组件 |
| `AnimatorAction` | 运行时动画事件数据 |
| `CharacterBodyParts` | Humanoid 骨骼节点映射组件 |
| `TransformTweenController` | DOTween Transform 移动与旋转控制器，仅 Sample |
| `CharacterTweenController` | 角色补间控制扩展入口，仅 Sample |

## 环境要求

- Unity 2021.3 或更高版本
- DOTween，仅在导入 `DOTween Controllers` Sample 时需要

## 许可证

本包使用 MIT License，详情参见仓库中的许可证文件。

## 改名迁移（3.0.0）

- 包标识：`com.cowart.animation-tools` → `com.cowart.animator-player`。
- Git 仓库：`https://github.com/liuyan7669/unity-animation-tools.git` → `https://github.com/liuyan7669/unity-animator-player.git`；原有提交和旧 tag 保留。
- 程序集、命名空间、示例和测试已同步使用新名称。业务项目需更新 manifest、asmdef 和 using；旧包与新包不可同时安装。
- 资源 .meta GUID 保留；已改命名空间的运行时类型提供 MovedFrom 迁移标记。已有源码仍需按新命名空间修改。
- 示例中的可选集成依赖保持可选，核心包不新增其他 Cowart 包依赖。
