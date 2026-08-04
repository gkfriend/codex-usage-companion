# Codex Usage Companion

[English](README.md) · [繁體中文](README.zh-Hant.md)

Codex Usage Companion 是开源 Windows 插件，会在 Codex Desktop 右下角显示简洁的剩余使用量面板。

## 界面预览

| English | 繁體中文 | 简体中文 |
| --- | --- | --- |
| ![英文使用量面板](assets/screenshots/overlay-en.png) | ![繁体中文使用量面板](assets/screenshots/overlay-zh-Hant.png) | ![简体中文使用量面板](assets/screenshots/overlay-zh-Hans.png) |

## 功能

- 直接应用 app-server 的实时使用量通知，并且最多每五分钟进行一次备用查询；重复的提示与回复 Hook 会合并，不会额外查询。
- 从 v0.3.6 开始，独立且不显示窗口的 Windows 计划任务会在 Codex 重新启动或常驻进程退出后约五分钟内恢复插件，即使 Codex Hook 没有运行也能恢复。
- Codex CLI 触发插件 Hook 时会返回有效 JSON 并安全退出；Codex Desktop 关闭时不会启动桌面插件。
- Codex 关闭时不会保留额外监控进程；每次计划检查完成后立即退出。
- 始终只保留一个常驻进程，不会出现在任务栏、Alt+Tab 或系统托盘。
- 面板会跟随 Codex 窗口；Codex 最小化时隐藏，Codex 关闭后自动退出。
- 使用五格 HP Bar，以绿、黄、橙、红、灰色快速表示剩余比例。
- 支持英文、繁体中文、简体中文。
- 五小时使用量功能完整保留，但默认隐藏。
- 只读取本地 Codex app-server，不会存储身份验证 Token。
- 没有遥测、分析或外部服务。

## 要求

- Windows 10 或更高版本，x64
- Codex Desktop

Release 已包含所需运行环境，无需另外安装 .NET Runtime。

## 从 GitHub 安装

> [!TIP]
> **不想阅读完整说明？**
>
> 直接把 `https://github.com/gkfriend/codex-usage-companion` 发给 Codex，然后告诉它：
>
> “请阅读这个项目的安装说明，帮我安装并启用 Codex Usage Companion。”
>
> Codex 会协助完成大部分安装步骤。如果出现权限或 `/hooks` 信任确认，按照画面提示批准即可。

运行：

```powershell
codex plugin marketplace add gkfriend/codex-usage-companion
```

打开 Codex 插件目录，选择 **Codex Usage Companion**，检查并信任内置的三个 Hook（`SessionStart`、`UserPromptSubmit` 和 `Stop`），然后安装并启用。安装后请打开新的 Codex 对话。每次更新后，如果 Codex 再次要求确认，请打开 `/hooks` 并重新信任当前的 Hook 定义。

也可以从 GitHub Releases 下载 Marketplace ZIP，解压后运行 `codex plugin marketplace add <文件夹>`。

## 自动恢复

进入已安装的 Codex Usage Companion 插件目录后运行：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install-recovery.ps1
```

这会创建当前用户专用的隐藏计划任务 `\CodexUsageCompanion\Recovery`、立即启动，并通过不显示控制台的 Windows Script Host 启动器每五分钟检查一次。无需管理员权限，也不会留下常驻 PowerShell 进程；原有 Hook 仍负责轻量的启动与更新通知。

如果只想移除自动恢复，但保留插件、设置和日志：

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\uninstall-recovery.ps1
```

## 设置

插件会在 Codex 插件数据目录创建 `settings.json`；如果无法使用该目录，则使用 `%LOCALAPPDATA%\CodexUsageCompanion\settings.json`。

```json
{
  "showFiveHourLimit": false,
  "language": "auto",
  "position": "bottom-right",
  "opacity": 1.0,
  "margin": 16
}
```

语言可设为 `auto`、`en`、`zh-Hant`、`zh-Hans`。

位置可设为 `top-left`、`top-right`、`bottom-left`、`bottom-right`。

透明度范围为 `0.5`–`1.0`，边距范围为 `0`–`64` 像素。

## 自行构建

安装 .NET 8 SDK 后运行：

```powershell
pwsh -File scripts/build.ps1
```

脚本会运行 Release 测试、创建自包含单文件 `win-x64` 程序、验证 Marketplace ZIP，并将 ZIP 与 SHA-256 放在 `artifacts`。

## 兼容性

此插件使用 Codex 实验性的本地 app-server 使用量 API。未来 Codex 更新可能需要同步调整。临时错误会保留最后一次有效数值，诊断信息只写入本地且有容量限制的日志文件。

隐私与许可请参阅 [PRIVACY.md](PRIVACY.md)、[SECURITY.md](SECURITY.md) 和 [MIT License](LICENSE)。
