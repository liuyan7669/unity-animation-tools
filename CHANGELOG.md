# Changelog

## [Unreleased]

## [3.1.0] - 2026-11-14

- 接入 OpenUPM 官方 tag 发布工作流，自动触发扫描并检查注册表发布结果。

- 统一 Cowart 包版本为 3.1.0。

- 修复编译期间骨骼序列化结构变化后旧语义 Drawer 读取不存在字段的异常。

- 声明 Inspector Groups 3.1.0 依赖；移除包内 Inspector 自定义样式，业务操作复用统一绘制。

## [3.0.0] - 2026-11-12

### 破坏性变更

- `com.cowart.animation-tools` 改为 `com.cowart.animator-player`；更新业务项目的安装地址、程序集和源码引用。
- 完成包标识、目录、程序集、命名空间、脚本及文档改名，保持资源 GUID 和包独立性。

## [2.0.2] - 2026-11-08

- 统一 Cowart 包图标：重新设计为按功能类别配色的圆角图标（动画曲线 + 关键帧），同步更新 `Icon.png` 与 `Icon@2x.png`。

## [2.0.1] - 2026-08-23

- 新增包专用图标，并将包内组件脚本的默认图标统一替换为该图标。

## [2.0.0] - 2026-08-21

- Breaking: Renamed the Animator playback component to `AnimatorPlayer`.
- Breaking: Removed the previous playback controller interface.
- Added random single-state playback with the selected list index as the return value.
- Added random non-repeating list playback with per-state and all-finished callbacks.

## [1.0.1] - 2026-07-17

- Added optional DOTween transform and character controllers as a Package Manager sample.

## [1.0.0] - 2026-07-16

- Initial UPM package.
