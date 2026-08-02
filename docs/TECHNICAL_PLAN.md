# 传送书签：技术设计

## 1. 技术基线

参考 `MultiplayerNpcLocator`：

- C# / .NET 6；
- Stardew Valley 1.6.15 首发验证；
- SMAPI 4.5.2；
- `Pathoschild.Stardew.ModBuildConfig` 4.4.0；
- 可选集成 Generic Mod Config Menu；
- 首版避免 Harmony，优先使用 SMAPI 事件和游戏公开 API；
- `EnableModDeploy=false`，使用安全构建脚本显式安装；
- 默认英文和简体中文 i18n。

## 2. 建议目录

```text
WarpBookmarks/
├── WarpBookmarks.csproj
├── manifest.json
├── ModEntry.cs
├── Config/
│   └── ModConfig.cs
├── Models/
│   ├── BookmarkRecord.cs
│   ├── LocationReference.cs
│   └── PlayerBookmarkData.cs
├── Framework/
│   ├── BookmarkRepository.cs
│   ├── LocationResolver.cs
│   ├── AccessPolicyService.cs
│   ├── SafeTileFinder.cs
│   ├── WarpService.cs
│   └── CoordinateParser.cs
├── UI/
│   ├── WarpBookmarksMenu.cs
│   ├── CoordinateWarpDialog.cs
│   ├── BookmarkEditorDialog.cs
│   └── ConfirmDialog.cs
├── Integrations/
│   └── IGenericModConfigMenuApi.cs
├── i18n/
│   ├── default.json
│   └── zh.json
├── scripts/
│   └── Build-Windows.ps1
└── docs/
```

## 3. 数据模型

```text
PlayerBookmarkData
├── DataVersion
├── PlayerStableId
├── Bookmarks[]
├── FavoriteOrder[]
├── HiddenDefaultLocationIds[]
├── VisitedLocationKeys[]
├── PreviousWarpOrigin?
└── RecentlyDeleted[]
```

### BookmarkRecord

- `Id`: Mod 生成的稳定 GUID；
- `Name`；
- `IconKey`；
- `ColorKey`；
- `LocationReference`；
- `TileX`, `TileY`, `FacingDirection`；
- `CreatedAt`, `LastUsedAt`；
- `IsFavorite`；
- `DataVersion`。

### LocationReference

不能只保存 `MapName + X + Y`。需要分层记录：

- 游戏位置 `NameOrUniqueName` 或经验证的等价稳定标识；
- 地图资源名作为诊断和回退；
- 父级区域；
- 是否为建筑室内；
- 具体建筑实例的稳定标识（若 API 提供）；
- 建筑类型与农场建筑格子作为受控回退；
- 自定义位置来源 Mod ID（若能可靠获得）；
- 记录时的显示名称，仅用于提示，不作为身份依据。

阶段 0 必须用运行时探针确认 1.6.15 中多个畜棚、鸡舍、小屋和移动建筑的稳定字段，不能仅凭记忆实现。

## 4. 数据归属与持久化

目标是“每个存档、每个玩家一份数据”。界面配置和按键属于全局 `config.json`；地点数据跟随该存档中的具体玩家。

优先候选是为每个 `Farmer` 使用带 Mod UniqueID 的版本化 `modData`；它能自然区分玩家并随存档保存。阶段 0 必须实测：

- 联机加入者修改自己的字段是否正确同步并由主机保存；
- 重新加入、改角色显示名、移动小屋后是否仍能识别；
- 未安装 Mod 的玩家是否完全不受影响；
- 数据长度和 JSON 迁移是否可靠。

若实测不可靠，再改为主机权威的按玩家 ID 存储，并用最小 SMAPI 消息同步。即使采用消息，也不共享玩家书签内容给其他玩家的 UI。

## 5. 统一传送管线

所有入口只能调用同一个 `WarpService`：

```text
请求目标
→ 检查世界已载入
→ 检查玩家状态
→ 解析具体位置/建筑实例
→ 检查访问权限与特殊地图
→ 验证坐标边界
→ 搜索安全落点
→ 再次检查状态
→ 保存出发点快照
→ 播放短动画
→ 执行传送
→ 以实际落点更新历史与最近使用
```

任何失败都不修改“上一个位置”和最近使用。

## 6. 玩家状态检查

默认禁止：

- 事件、节日、婚礼、小游戏；
- 对话、商店、箱子和其他菜单；
- 钓鱼、工具动作、昏迷、睡觉流程；
- 骑马；
- 正在受击、传送或脚本控制；
- 地图正在切换或世界未准备。

是否允许一般战斗状态下主动传送可先允许，但受到伤害或控制时取消动画。

## 7. 安全落点

候选格必须：

- 在地图边界内；
- 可通行且允许玩家站立；
- 不是水、墙或禁止区域；
- 没有建筑、家具、大型物体、动物、NPC 或玩家阻挡；
- 不会触发明显不安全的自动碰撞/传送点。

搜索顺序以目标为中心按半径 0、1、2、3 向外扩展，并使用确定性排序。超过三格仍找不到则拒绝，不把玩家送到难以预测的远处。

对 `isTilePassable`、碰撞层、对象/角色占用和第三方地图属性的实际调用在阶段 0 探针后确定。

## 8. 地图与访问策略

建立显式拒绝列表和能力判断，不只依赖地图名称字符串：

- 当前事件/节日临时位置；
- 矿井随机层和骷髅洞穴随机层；
- 火山特殊/随机房间；
- 小游戏和剧情专用位置；
- 地图不存在或来源 Mod 未加载；
- 未满足最终确认的访问策略。

自定义地图不存在时保留书签并标记不可用。地图重新出现后重新解析。

## 9. 默认地点提供器

默认地点不混入玩家自定义数据。它们由 `DefaultLocationProvider` 在运行时生成，具有稳定系统 ID；隐藏状态只保存这些 ID。

这样可以：

- 更新坐标而不迁移玩家书签；
- 恢复隐藏地点；
- 按访问记录决定是否展示；
- 在 Mod 更新时添加新地点但保留旧隐藏状态。

首版系统 ID 固定为 `FarmTotem`、`MountainTotem`、`BeachTotem`、`DesertTotem`、`IslandTotem`，对应原版五种标准传送图腾目的地。沙漠和姜岛必须通过严格解锁检查；海滩在节日替换地图期间不得绕过事件规则。

## 10. 精准坐标解析

解析器接受受限文本格式，只提取地图标识和两个整数。禁止把输入当成控制台命令、路径、类型名或可执行内容。

成功解析不等于允许传送；仍需通过位置存在、访问权限、边界和安全格验证。

## 11. 兼容性策略

- 原版和标准 `GameLocation` 作为正式支持范围；
- SVE/自定义农场先承诺“通用兼容目标”，通过实际测试后再写进发布声明；
- 不写死 SVE 地图坐标；
- 不修改 NPC、日程、任务、地图资源或游戏传送点；
- 不要求 `MultiplayerNpcLocator`，只参考其 UI 和验证流程；
- 可同时安装 NPC Locator，默认快捷键必须不同。

## 12. 诊断能力

开发版提供只读/受控控制台命令：

- `wb_where`: 输出当前地图稳定标识、显示名、格子和建筑上下文；
- `wb_validate_location`: 验证当前书签所需字段；
- `wb_validate_tile <x> <y>`: 输出目标格安全检查原因；
- `wb_dump_bookmarks`: 仅输出当前玩家书签摘要，不输出其他玩家数据。

用于实际传送的测试命令必须清楚标记，并只在世界就绪且通过统一安全管线时执行。
