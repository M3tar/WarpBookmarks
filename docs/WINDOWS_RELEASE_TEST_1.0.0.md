# 传送书签 1.0.0 Windows 发布测试

## 1. 生成发布包

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\scripts\Package-Windows.ps1
```

如果脚本无法自动找到游戏：

```powershell
.\scripts\Package-Windows.ps1 -GamePath "C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley"
```

确认输出包为 `dist\WarpBookmarks-1.0.0.zip`，并记录脚本显示的 SHA-256。

## 2. 干净安装

1. 关闭游戏和 SMAPI。
2. 将现有 `Mods\WarpBookmarks` 移到一个临时备份位置。
3. 将 `WarpBookmarks-1.0.0.zip` 直接解压到 `Mods`。
4. 确认路径为 `Mods\WarpBookmarks\WarpBookmarks.dll`，没有多套一层目录。
5. 启动 SMAPI，确认显示 `Warp Bookmarks 1.0.0`且没有红色错误。

## 3. 最终冒烟

1. `K` 正常打开和关闭手册，菜单内显示当前实际快捷键。
2. `Shift + K` 快速记录当前位置。
3. 自定义书签可命名、搜索、收藏、重命名、删除和传送。
4. 返回住宅、默认地点和上一位置可用。
5. 精准坐标的地图搜索、X/Y 输入、预览、传送和保存可用。
6. 目标格被占用时使用附近安全格，且成功提示不重复坐标。
7. 事件、小游戏和特殊地图限制仍生效。
8. 修复前已有书签仍然存在。

## 4. 联机回归

如果当前方便进入联机存档：

1. 联机和个人存档分别新建一个唯一名称的书签。
2. 切换存档后确认对方书签没有出现。
3. 必要时在两个存档中执行 `wb_dump_bookmarks` 对比归属。

`0.5.3` 已完成这项实机验证；本节是发布包对同一行为的最终回归。

## 5. 上传前记录

请保留：

- 完整 SMAPI 日志；
- `WarpBookmarks-1.0.0.zip` 的 SHA-256；
- 一张手册主界面截图；
- 一张精准坐标界面截图。
