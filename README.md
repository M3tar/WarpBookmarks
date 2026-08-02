# 传送书签（Warp Bookmarks）

一个面向《星露谷物语》1.6 的 SMAPI Mod：玩家可以保存自己常去的位置，通过可搜索的“传送手册”快速回家、返回上一个位置、使用自定义书签或输入地图坐标传送。

本项目当前处于阶段 0：工程骨架和运行时探针已建立，等待 Windows 目标环境构建与实机验证。正式传送功能尚未启用。

## 已确认的产品方向

- 不制作实体道具，不占用背包格。
- 不加入成长、升级、制作配方、充能、材料消耗或冷却。
- 使用可自定义快捷键打开“传送手册”。
- 自定义书签按玩家、按存档隔离；联机玩家互不共享书签。
- 支持输入地图与格子坐标进行一次性精准传送。
- 默认地点可以隐藏，自定义书签可以编辑、重新绑定和删除。
- 所有入口共用地图、状态与安全落点验证。
- UI 参考 `MultiplayerNpcLocator` 的原版羊皮纸风格与输入习惯。
- 首发测试环境参考 `MultiplayerNpcLocator`：Windows 11、Stardew Valley 1.6.15、SMAPI 4.5.2、.NET 6 SDK。

## 文档索引

- [产品规格](docs/PRODUCT_SPEC.md)
- [UI 与交互规格](docs/UX_UI_SPEC.md)
- [技术设计](docs/TECHNICAL_PLAN.md)
- [开发计划](docs/DEVELOPMENT_PLAN.md)
- [测试计划](docs/TEST_PLAN.md)
- [产品决策](docs/DECISIONS.md)
- [待确认问题](docs/OPEN_QUESTIONS.md)
- [阶段 0 Windows 验证](docs/PHASE0_VALIDATION.md)
- [当前状态](docs/STATUS.md)

## 暂定项目标识

| 项目 | 值 |
|---|---|
| 中文名 | 传送书签 |
| 英文名 | Warp Bookmarks |
| 项目文件夹 | `WarpBookmarks` |
| 程序集 | `WarpBookmarks.dll` |
| C# 命名空间 | `WarpBookmarks` |
| Mod UniqueID | `Mercury.WarpBookmarks` |
