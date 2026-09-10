# Warp Bookmarks 1.0.1 发布清单

本清单按 V2 分阶段流程执行。最终 ZIP 与截图通过仓库审核后，可以在本地切换 README 的 `1.0.1` 链接；作者确认最终差异前，不推送代码，也不创建 `v1.0.1` GitHub Release。

## A. 源码与文档冻结

- [x] `manifest.json` 版本为 `1.0.1`，`UniqueID` 仍为 `Mercury.WarpBookmarks`；
- [x] `CHANGELOG.md` 已记录本轮英文布局、跨语言中文名称、搜索框、像素星标、下拉箭头和滚动条修复；
- [x] Windows 测试步骤、Nexus 双语更新说明和本发布清单已更新；
- [x] README 在最终包与截图审核前保留 `1.0.0` 链接，审核后已在本地切换为 `1.0.1`，尚未推送；
- [x] 重新生成 `dist/WarpBookmarks-1.0.1-rc-source.zip` 及其 SHA-256，确保源码候选包与当前代码、文档和 README 素材一致。

## B. Windows 构建、安装与实机验证

- [x] 在源码候选包根目录完成 Windows Release 构建；`Package-Windows.ps1` 已自动调用构建脚本；
- [x] 确认 SMAPI 加载 `Warp Bookmarks 1.0.1`，且没有本 Mod 的红色错误；
- [ ] 完整执行 `docs\WINDOWS_RELEASE_TEST_1.0.1.md`；
- [x] 保存英文、中文和跨语言中文名称的关键截图；
- [x] 确认英文界面保存和传送中文书签后，右下角 HUD 提示不再出现 `?`；
- [x] 退出游戏后运行 `.\scripts\Package-Windows.ps1`，不要添加 `-Install`；
- [x] 确认生成 `dist\WarpBookmarks-1.0.1.zip`，并保存脚本输出的 SHA-256。

## C. 最终 ZIP 干净安装

- [x] 确认个人书签保存在存档的玩家 `modData` 中，不在 `Mods\WarpBookmarks` 目录；
- [x] 删除或移走旧的 `Mods\WarpBookmarks` 目录；
- [x] 只从最终 `WarpBookmarks-1.0.1.zip` 解压新的 `WarpBookmarks` 文件夹到 `Mods`；
- [x] 确认最终路径是 `Mods\WarpBookmarks\WarpBookmarks.dll`，没有重复目录层级；
- [x] 重新载入原存档，确认已有书签、常用状态与隐藏状态仍然存在；
- [x] 再次确认 SMAPI 无本 Mod 红色错误，核心记录、传送、重命名和中英文切换正常。

## D. 回传给 Codex 的发布材料

- [x] 最终 `dist\WarpBookmarks-1.0.1.zip`；
- [x] ZIP 的 SHA-256：`309b0e163b3a8d52f94971620cb6e17b7e8b0a3d11ea81281a501e0626977c5f`；
- [x] SMAPI 无本 Mod 红色错误；
- [x] 准备用于 README 的中英文新截图；
- [ ] 100%/125%/150% 缩放结果，以及英文、中文、跨语言名称是否全部通过。

## E. 最终仓库审核

- [x] 核对 ZIP 内只有 `WarpBookmarks.dll`、`manifest.json` 与 `i18n` 发布文件；
- [x] 核对 ZIP 内 `manifest.json` 为 `1.0.1`，并验证回传的 SHA-256；
- [x] 将新截图放入 `assets/readme/screenshots/`，更新中英文 README 的截图、说明和替代文本；
- [x] 将 README 的版本、文件名和 GitHub 下载链接由 `1.0.0` 切换为 `1.0.1`；
- [x] 将项目状态从“候选验证”切换为“准备发布”；
- [x] 运行当前环境可执行的截图静态回归、README 素材、JSON/i18n、ZIP 结构与发布哈希检查；
- [ ] 在 Windows 运行 PowerShell 与发布就绪检查；
- [x] 已向作者展示最终差异，并收到明确发布确认。

## F. GitHub Release v1.0.1

- [ ] 仅在作者确认最终差异后提交并推送代码；
- [ ] 创建并推送标签 `v1.0.1`；
- [ ] 创建 GitHub Release `v1.0.1`，使用 `CHANGELOG.md` / `docs/NEXUS_UPDATE_1.0.1.md` 中的对应说明；
- [ ] 上传经审核的 `WarpBookmarks-1.0.1.zip`，不要上传 `-rc-source.zip` 作为安装包；
- [ ] 验证 Release 页面、下载文件名、文件内容和 SHA-256。

## G. Nexus Mods 更新

- [ ] 主文件名填写 `Warp Bookmarks 1.0.1`；
- [ ] 使用 `docs/NEXUS_UPDATE_1.0.1.md` 中的双语更新说明；
- [ ] 将文件标记为适用于 Stardew Valley 1.6.15 / SMAPI 4.5.2 或兼容后续版本；
- [ ] 保留 `1.0.0` 为旧文件，不把源码候选包上传成主文件；
- [ ] 上传前再次核对 ZIP 内只有 `WarpBookmarks.dll`、`manifest.json` 与 `i18n`；
- [ ] 发布后从 Nexus 下载一次，验证压缩包哈希、目录结构与安装结果。
