# Windows 阶段 0 构建与验证

## 1. 环境

- Windows 11；
- Stardew Valley 1.6.15 build 24356；
- SMAPI 4.5.2；
- .NET 6 SDK x64；
- GMCM 1.16.0 可选。

## 2. 构建与安装

在项目根目录 PowerShell 执行：

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\Test-PowerShellSyntax.ps1
.\scripts\Build-Windows.ps1 -Install
```

更新已安装的本 Mod：

```powershell
.\scripts\Build-Windows.ps1 -Install -UpdateExisting
```

非默认 Steam 路径：

```powershell
.\scripts\Build-Windows.ps1 -GamePath "D:\SteamLibrary\steamapps\common\Stardew Valley" -Install
```

源码交接包解压后会得到完整的 `WarpBookmarks` 文件夹，不需要逐个复制文件。构建成功时 PowerShell 会同时显示 DLL 路径和由 ModBuildConfig 生成的发布 zip 路径。

## 3. 基础加载

1. 通过 SMAPI 启动游戏；
2. 日志应出现 `Warp Bookmarks 0.0.4`；
3. 载入存档后不应出现快捷键 HUD；当前快捷键只在传送手册内部显示；
4. 安装 GMCM 时修改两个快捷键，重新载入确认提示使用新绑定；
5. K/Shift+K 当前只显示阶段 0 提示，不执行传送或写入书签；不应打开其他游戏界面。

## 4. 位置身份探针

分别站在以下地点运行：

```text
wb_where
```

- 农场室外；
- 主农舍；
- 温室；
- 酒窖；
- 两个不同但同类型的畜棚；
- 两个不同但同类型的鸡舍；
- 主机住宅和联机加入者小屋；
- 移动建筑前后；
- 山区、海滩、沙漠与姜岛图腾落点；
- 一个 SVE 或常规自定义地图（具备测试档时）。

目标是找出能稳定区分地图和具体建筑实例的字段。

## 5. 格子探针

对当前站立格、附近水格、墙、箱子、NPC、动物和另一名玩家所在格分别运行：

```text
wb_validate_tile 12 8
```

保留 Info 与 Trace 日志。阶段 0 不根据猜测直接调用候选方法，而是先确认 1.6.15 实际方法签名和集合类型。

## 6. 每位玩家持久化探针

主机和加入者分别运行不同值：

```text
wb_probe_data host-value
wb_probe_data farmhand-value
```

保存、退出、重新进入后，各自运行：

```text
wb_probe_data
```

确认每位玩家只读到自己的值。测试完成后清理：

```text
wb_probe_data clear
```

## 7. 回传证据

- PowerShell 完整构建输出；
- 主机与加入者完整 SMAPI 日志；
- 上述位置和格子探针输出；
- 持久化探针在重载前后的结果；
- K/Shift+K 以及 GMCM 改键截图；
- 使用的农场布局、建筑数量和地图 Mod 列表。
