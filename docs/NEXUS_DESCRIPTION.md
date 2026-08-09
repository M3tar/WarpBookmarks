# Nexus Mods 页面文案草案

## 简短描述

用可搜索的羊皮纸传送手册保存个人地点，快速回家、返回上一个位置，或输入地图格子坐标传送。支持单人和在线联机个人书签。

Save personal locations in a searchable parchment-style travel book. Return home, revisit your previous departure point, or teleport using map tile coordinates. Supports independent personal bookmarks in single-player and online multiplayer.

## 中文详细说明

《传送书签》是一个轻量的移动便利 Mod。它不增加实体道具、材料消耗、充能或升级系统，只提供一本可搜索的个人传送手册。

主要功能：

- `K` 打开手册，`Shift + K` 快速记录当前位置；
- 创建、命名、搜索、收藏、重命名和删除个人书签；
- 返回自己的农舍或联机小屋门外；
- 使用农场、山区、海滩、沙漠和姜岛五个默认地点；
- 在两个地点之间使用“上一个位置”往返；
- 选择已到访地图并输入 X/Y 格子坐标；
- 目标被阻挡时自动寻找附近三格内的安全落点；
- 单人与在线联机玩家的自定义书签互相隔离。

要求：Stardew Valley 1.6.15、SMAPI 4.5.2 或兼容的后续版本。Generic Mod Config Menu 可选，用于在游戏内修改快捷键。

## English description

Warp Bookmarks is a lightweight travel convenience mod. It adds no physical item, crafting recipe, charge, resource cost, cooldown, or upgrade system. Instead, it provides a searchable personal travel book.

Features:

- Open the book with `K`; quickly save your current position with `Shift + K`.
- Create, name, search, favorite, rename, and delete personal bookmarks.
- Return to the outside of your own farmhouse or multiplayer cabin.
- Use the five standard vanilla Warp Totem destinations.
- Travel back to the departure point of your previous successful teleport.
- Select a visited map and enter X/Y tile coordinates.
- Search within three tiles when the requested destination is blocked.
- Keep custom bookmark data separate for each save and multiplayer player.

Requirements: Stardew Valley 1.6.15 and SMAPI 4.5.2, or compatible later versions. Generic Mod Config Menu is optional and enables in-game shortcut configuration.

## 已知限制 / Known limitation

重命名书签时，左右方向键不能移动输入光标；可以退格后重新输入，不影响保存和传送。

The Left and Right arrow keys don't move the caret in the bookmark rename field. Backspace and retyping still work, and this doesn't affect saving or teleporting.

## 安装 / Installation

1. 安装 SMAPI / Install SMAPI.
2. 将压缩包解压到 `Stardew Valley/Mods` / Extract the archive into `Stardew Valley/Mods`.
3. 确认路径为 `Mods/WarpBookmarks/WarpBookmarks.dll`.
4. 通过 SMAPI 启动游戏 / Start the game through SMAPI.

## 开发说明 / Development disclosure

需求决策、产品取舍和 Windows 实机验收由 March3tar 完成；代码实现与页面文案使用 OpenAI Codex 辅助生成和整理。

Product decisions, scope, and Windows playtesting were led by March3tar. OpenAI Codex was used to assist with code implementation and mod-page copy.
