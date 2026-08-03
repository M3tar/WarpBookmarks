# 传送书签（Warp Bookmarks）

一个面向《星露谷物语》1.6 的 SMAPI Mod：玩家可以保存自己常去的位置，通过可搜索的“传送手册”快速回家、返回上一个位置、使用自定义书签或输入地图坐标传送。

当前版本为 `0.5.2`：已经可以记录、分类和搜索个人书签，使用“常用”首页快速返回住宅、上一个位置或收藏地点。传送成功提示不会重复显示书签名称中已有的坐标；只有安全落点与请求坐标不同时，才额外说明实际落点。

默认快捷键：`K` 打开/关闭手册，`Shift + K` 记录当前位置；两者都可在 Generic Mod Config Menu 或 `config.json` 中修改。

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
- [0.5.2 Windows 合并测试](docs/WINDOWS_TEST_0.5.2.md)
- [已知问题](docs/KNOWN_ISSUES.md)
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
