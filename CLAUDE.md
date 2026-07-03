# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Casualties Unknown（未知伤亡）的 BepInEx 6.x 拼音搜索模组，为游戏合成界面的搜索框增加拼音搜索功能，支持全拼、首字母、混合输入等。灵感来源于 Minecraft 模组 [Just Enough Characters](https://github.com/Towdium/JustEnoughCharacters)。

- **前置依赖**: [BepInEx](https://bepinex.org/) 6.x（需预先安装在游戏目录）
- **仓库**: https://github.com/XeronOwO/Just-Unknown-Characters

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

### NuGet 包

| 包 | 用途 |
|----|------|
| `BepInEx.NET.Framework.Launcher` (6.0.0-be.*) | BepInEx 框架启动器，仅编译时引用 |
| `BepInEx.PluginInfoProps` (2.*) | 自动生成插件元数据常量 |

### 运行时依赖

| 依赖 | 说明 |
|------|------|
| [BepInEx](https://bepinex.org/) 6.x | 需预先安装在游戏目录，本模组基于此框架运行 |

NuGet 源除了 nuget.org 外还包括：
- `https://nuget.bepinex.dev/v3/index.json`
- `https://nuget.samboy.dev/v3/index.json`

## 架构要点

- 插件通过 `[BepInPlugin]` 特性注册，框架自动发现并调用 `Load()`
- `BasePlugin.Log` 是 `ManualLogSource`，启动后赋值给静态属性供全局使用
- 目标 `net452` 是为了兼容 Unity 的 Mono 运行时

## 致谢

本模组的拼音匹配算法和字典数据来源于 [Just Enough Characters](https://github.com/Towdium/JustEnoughCharacters)（Minecraft 模组）及其核心库 [PinIn](https://github.com/Towdium/PinIn)，由 [Towdium](https://github.com/Towdium) 开发。

`Resources/pinyin_data.txt` 直接取自 PinIn 项目，拼音匹配逻辑为 PinIn 算法的简化 C# 移植。没有这些项目，本模组不可能实现。感谢 Towdium 的开源贡献。
