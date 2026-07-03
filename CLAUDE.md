# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

BepInEx 6.x 模组插件，基于 C# / .NET Framework 4.5.2 (net452)。

## 构建与开发

```bash
# 还原依赖并构建
dotnet restore
dotnet build

# 构建 Release 版本
dotnet build -c Release

# 清理构建产物
dotnet clean
```

构建输出路径为 `bin/Debug/net452/JustUnknownCharacters.dll`。

## 项目结构

| 文件 | 用途 |
|------|------|
| `Plugin.cs` | 插件入口，继承 `BasePlugin`，`Load()` 为启动点 |
| `JustUnknownCharacters.csproj` | 项目配置，含自定义 NuGet 源 |

## 插件元数据

由 `BepInEx.PluginInfoProps` 自动生成 `MyPluginInfo.cs`（位于 `obj/`）：

- **GUID**: `JustUnknownCharacters`
- **名称**: `JustUnknownCharacters`
- **版本**: `1.0.0`

## 依赖

| 包 | 用途 |
|----|------|
| `BepInEx.NET.Framework.Launcher` (6.0.0-be.*) | BepInEx 框架启动器，仅编译时引用 |
| `BepInEx.PluginInfoProps` (2.*) | 自动生成插件元数据常量 |

NuGet 源除了 nuget.org 外还包括：
- `https://nuget.bepinex.dev/v3/index.json`
- `https://nuget.samboy.dev/v3/index.json`

## 架构要点

- 插件通过 `[BepInPlugin]` 特性注册，框架自动发现并调用 `Load()`
- `BasePlugin.Log` 是 `ManualLogSource`，启动后赋值给静态属性供全局使用
- 目标 `net452` 是为了兼容 Unity 的 Mono 运行时
