# Nexus Mods 1.0.1 更新文案

## 文件名称

`Warp Bookmarks 1.0.1`

## 简短更新说明

修复英文界面的分类与详情文本重叠，并让中文创建的书签名称在英文界面中保持可读。已有书签数据无需迁移。

Fixes overlapping category/detail text in the English UI and keeps Chinese bookmark names readable after switching the game to English. Existing bookmark data requires no migration.

## 更新内容

- 五个分类现在使用羊皮纸顶部完整宽度，不再互相遮挡；
- 详情区移动到分类栏下方，长空状态、字段值和动态按钮会限制在各自区域内；
- 英文详情使用半角冒号，避免中文标点在英文字体中显示成缺字符号；
- 中文或中英混合书签名在英文界面中按需使用游戏中文界面的 `SmallFont` 绘制；
- 中文回退字形会匹配所在列表、标题或按钮的字号与垂直位置；常用状态改为行末固定位置的小型金色像素星，不再紧贴或挤压名称；
- `Common / Favorite / Unfavorite` 改为更明确的 `Quick Access / Pin / Unpin`；中文继续使用“常用 / 加入常用 / 移出常用”；
- 搜索提示移入输入框内部，改善标题、提示和搜索框之间的间距；
- 精准坐标的下拉状态改用像素三角形，不再依赖字体中的 `▲/▼` 字形；
- 中文主传送按钮移除不受支持的角括号，地点名前后不再显示问号；
- 主列表滚动条与实际可见行底边对齐；中文“我的书签”分类精简为“书签”，其他分类字距保持不变；
- 精准坐标窗口的英文操作按钮重新分配宽度，避免 `Save bookmark` 越过边框；
- 用户填写的书签名始终原样保留，地图显示名则根据当前游戏语言刷新；
- 存档格式与 `1.0.0` 兼容，不会重命名或重建已有书签。

## English changelog

- The five categories now use the full parchment width and no longer overlap.
- The details panel starts below the category bar; long empty states, values, and action labels stay within their regions.
- English details use localized Latin punctuation instead of unsupported Chinese punctuation.
- Chinese and mixed-language bookmark names use the Chinese UI's localized `SmallFont` when the active Latin font lacks them.
- Fallback glyph sizing and vertical alignment now match the surrounding list, title, or button; pinned rows use a small font-independent gold star in a fixed trailing slot.
- `Common / Favorite / Unfavorite` are now `Quick Access / Pin / Unpin`, with matching Chinese terminology.
- Search uses an in-field placeholder, leaving stable spacing between the title and input.
- The coordinate map dropdown now uses a font-independent pixel caret, and the Chinese warp action no longer renders unsupported punctuation as question marks.
- The main scrollbar now ends at the final visible row; the Chinese Bookmarks tab uses a shorter label without changing global CJK spacing.
- Coordinate-dialog actions have wider, bounded labels so `Save bookmark` stays inside its button.
- User-entered bookmark names remain unchanged, while map display names refresh for the current game language.
- Save data remains compatible with `1.0.0`; no bookmark migration is required.

## 安装提示

覆盖更新即可：删除旧版 `Mods/WarpBookmarks` 文件夹后，再将新压缩包解压到 `Stardew Valley/Mods`。不要同时保留两个版本。

For a clean update, remove the old `Mods/WarpBookmarks` folder, then extract the new archive into `Stardew Valley/Mods`. Do not keep two versions installed together.
