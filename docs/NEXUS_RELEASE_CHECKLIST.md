# Nexus Mods 1.0.0 发布清单

## 已完成

- [x] 首版功能冻结；
- [x] 单人主要流程实机验证；
- [x] 在线联机书签隔离实机验证；
- [x] 中英文界面文本；
- [x] 中英文玩家说明；
- [x] CHANGELOG 和已知限制；
- [x] Windows 构建、安装和 Nexus 打包脚本；
- [x] 静态回归检查。

## 需要在 Windows 完成

- [ ] 运行 `.\scripts\Package-Windows.ps1`；
- [ ] 确认生成 `dist\WarpBookmarks-1.0.0.zip` 和 SHA-256；
- [ ] 使用新压缩包进行一次干净安装；
- [ ] 执行 `docs\WINDOWS_RELEASE_TEST_1.0.0.md`；
- [ ] 检查 SMAPI 日志没有本 Mod 的红色错误；
- [ ] 保留最终测试日志和包哈希。

## Nexus Mods 页面

- [ ] 选择授权方式；未加入 LICENSE 前按保留所有权利处理；
- [ ] 按 Nexus 规则选择 `AI-Generated Content` 标签，不隐瞒 Codex 参与的代码和文案生成；
- [ ] 上传封面图和至少两张实机界面截图；
- [x] 使用 `docs\NEXUS_DESCRIPTION.bbcode.txt` 创建并公开 [Nexus Mods 页面](https://www.nexusmods.com/stardewvalley/mods/50310?published=1)；
- [x] 记录 Nexus Mod ID：`50310`；
- [x] 将 `"Nexus:50310"` 填入 `manifest.json` 的 `UpdateKeys`；
- [ ] 重新运行 `.\scripts\Package-Windows.ps1` 并做最终干净安装；
- [ ] 上传带 UpdateKeys 的 `WarpBookmarks-1.0.0.zip`，不上传 `source.zip` 作为主文件；
- [ ] 发布前再核对版本号、依赖、已知限制和下载文件。
