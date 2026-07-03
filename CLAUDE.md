# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Casualties Unknown（未知伤亡）的 BepInEx 5.x 拼音搜索模组，为游戏合成界面的搜索框增加拼音搜索功能，支持全拼、首字母、混合输入等。灵感来源于 Minecraft 模组 [Just Enough Characters](https://github.com/Towdium/JustEnoughCharacters)。

- **前置依赖**: [BepInEx](https://bepinex.org/) 5.x（需预先安装在游戏目录）
- **仓库**: https://github.com/XeronOwO/Just-Unknown-Characters

## 构建与开发

### 前置准备

构建前需要在 `lib/` 目录放置游戏 DLL（因版权原因不包含在仓库中）：

- `Assembly-CSharp.dll` — 游戏主程序集
- `UnityEngine.dll` — `BaseUnityPlugin` 所需
- `UnityEngine.CoreModule.dll` — 游戏引用的 Unity 核心
- `0Harmony.dll` — Harmony 补丁库（从游戏 `BepInEx/core/` 复制）
- `netstandard.dll` — Unity Mono 兼容层

以上文件位于游戏的 `CasualtiesUnknown_Data\Managed\` 目录下，复制到 `lib/` 即可。详见 `lib/README.txt`。

### 构建命令

```bash
# 还原依赖并构建
dotnet restore src/JustUnknownCharacters.csproj
dotnet build src/JustUnknownCharacters.csproj

# 构建 Release 版本
dotnet build src/JustUnknownCharacters.csproj -c Release

# 清理构建产物
dotnet clean src/JustUnknownCharacters.csproj
```

构建输出路径为 `src/bin/Debug/net452/JustUnknownCharacters.dll`。

### 代码格式化

提交前运行 `dotnet format` 确保代码符合 `.editorconfig`：

```bash
dotnet format src/JustUnknownCharacters.csproj
```

代码规范：
- 使用 `using` 导入命名空间，禁止全限定名（如 `System.StringComparison`）
- 局部变量优先 `var`

如有不符合风格的代码，该命令会自动修复或报错。

## 项目结构

```
JustUnknownCharacters.slnx    — 解决方案
.editorconfig                 — 代码风格配置
src/
  JustUnknownCharacters.csproj — 项目配置
  Plugin.cs                   — 插件入口，继承 BaseUnityPlugin，Awake() 为启动点
  HarmonyPatches.cs           — Harmony 补丁（Prefix/Postfix），注入合成搜索流程
  PinyinMatcher.cs            — PinIn 音素 NFA 回溯匹配算法
  PinyinDict.cs               — 拼音字典加载，含拼音拆分与模糊音生成
  Resources/
    pinyin_data.txt           — PinIn 汉字→拼音映射字典
```

## 插件元数据

由 `BepInEx.PluginInfoProps` 自动生成 `MyPluginInfo.cs`（位于 `obj/`）：

- **GUID**: `JustUnknownCharacters`
- **名称**: `JustUnknownCharacters`
- **版本**: `1.0.0`

## 依赖

### NuGet 包

| 包 | 用途 |
|----|------|
| `BepInEx.Core` (5.*) | BepInEx 5 核心库 |
| `BepInEx.PluginInfoProps` (2.*) | 自动生成插件元数据常量 |

### 运行时依赖

| 依赖 | 说明 |
|------|------|
| [BepInEx](https://bepinex.org/) 5.x | 需预先安装在游戏目录，本模组基于此框架运行 |

NuGet 源除了 nuget.org 外还包括：
- `https://nuget.bepinex.dev/v3/index.json`
- `https://nuget.samboy.dev/v3/index.json`

## 架构要点

- 插件通过 `[BepInPlugin]` 特性注册，框架自动发现并调用 `Awake()`
- `BaseUnityPlugin.Logger` 是 `ManualLogSource`，启动后赋值给静态属性供全局使用
- 目标 `net452` 是为了兼容 Unity 的 Mono 运行时

## 致谢

本模组的拼音匹配算法和字典数据来源于 [Just Enough Characters](https://github.com/Towdium/JustEnoughCharacters)（Minecraft 模组）及其核心库 [PinIn](https://github.com/Towdium/PinIn)，由 [Towdium](https://github.com/Towdium) 开发。

`Resources/pinyin_data.txt` 直接取自 PinIn 项目，拼音匹配逻辑基于 PinIn 的 NFA 音素回溯算法移植。没有这些项目，本模组不可能实现。感谢 Towdium 的开源贡献。
