# 阶段 0 单人验证记录

> 首轮日志日期：2026-08-02  
> 玩家：九月  
> 游戏：Stardew Valley 1.6.15 build 24356

## 已验证

- 阶段 0 工程已在 Windows 成功编译、安装并载入游戏；
- `wb_where` 在农场和鹈鹕镇可正常运行；
- 同一农场不同坐标 `(63,19)` 与 `(75,19)` 均返回 `NameOrUniqueName=Farm`；
- 鹈鹕镇返回 `NameOrUniqueName=Town`；
- 农场 `DisplayName` 是玩家自定义农场名称，不能作为稳定位置键；
- 玩家稳定 ID 为有符号 `long`，本次值为负数，因此实现不能假设 ID 为正；
- `Farmer.homeLocation`、`lastSleepLocation` 和 `locationsVisited` 在 1.6.15 运行时存在；
- 普通室外地图 `ParentBuilding=null`，符合预期。
- 0.0.3 已在 Windows 成功构建、安装和加载；
- 主农舍内部返回 `NameOrUniqueName=FarmHouse` 且 `ParentBuilding` 非空；建筑对象暴露稳定 `id`、父位置和 tileX/tileY 候选字段；
- 海滩返回稳定 `NameOrUniqueName=Beach`；
- 当前日志已足够开始单人功能实现，不再要求用户准备重复建筑、多个存档或专门睡觉保存。

## 当前推论

- 普通固定地图优先使用 `NameOrUniqueName` 作为位置身份；
- 显示名称只用于 UI；
- 严格访问控制可基于当前玩家的 `locationsVisited`，但仍需读取实际集合值；
- 个人住宅解析应从 `homeLocation` 开始，并验证其实际值和门外入口；
- 具体建筑实例仍需在畜棚、鸡舍等室内验证。

## 下一轮单人验证

1. 主农舍内部运行 `wb_where`；
2. 温室内部运行 `wb_where`；
3. 如果存档中已有畜棚或鸡舍，可任选一个内部运行 `wb_where`；没有则跳过；
4. 在方便到达的山区或海滩任选一处运行 `wb_where`；
5. 对当前站立空格运行一次 `wb_validate_tile <x> <y>`；
6. 数据保存重载、重复建筑和多个存档不作为当前阶段的用户必测项；
7. 更新后只需确认日志显示 `open=K; create=LeftShift + K`，K/Shift+K 均只触发 Mod 提示。

## 暂缓

- 联机加入者独立 `modData`；
- 主机/加入者住宅差异；
- 多人同时占用落点；
- 断线重连和联机改名。
