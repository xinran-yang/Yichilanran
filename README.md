# 一尺蓝染 · Unity 灰盒 Demo

Unity 6000.3.28f1（6.3 LTS），Universal 2D，C# / Rigidbody2D。

当前是保存开发进度的版本，仍有待用户验收、后续修正的问题，不代表最终发布版。

`develop` 保存后续可玩性 Demo 进度；`main` 保留首次上传版本。最新修正包括 2.2 地图与防绕行地形、两态传送点贴合道路边界、所有箱子的绿色带箱换色，以及暂时关闭 S 下冲自动高回弹。Space 跳跃保持现有参数。

当前说明见 [2.2 地图修正](Docs/Level22_Revision.md)、[传送点与换色修复](Docs/PortalBoundary_Fix.md)、[Demo 下冲模式](Docs/DemoDive_Mode.md)。

## 运行

用对应 Unity 版本打开项目，等待导入完成，打开 `Assets/_Game/Scenes/Level_2_1.unity` 并 Play。

- A/D 或方向键：移动；Space：跳跃；S/↓：下冲；R：重置。
- 2.1 完成后短暂黑屏进入 2.2；2.2 完成显示提示。
- 重新生成场景：`Tools > YiChiLanRan > 0 Generate complete greybox demo`。

## 当前进度

已实现两关灰盒、蓝白碰撞、统一上下镜像、箱子支撑保留、黄色/绿色传送和调试记录。最新修改包含白态地形轮廓、完整旗帜镜像、起点加厚和 2.1 平台高度调整；未新增白色中继落脚面。

独立 Unity 副本中的编译、两关连续输入物理路线及 2.1 坑内直跳/回弹检查已通过。人工画面、键盘手感及所有异常路线尚未完成验收，仍有用户观察到的问题待继续处理。具体尺寸、检查边界及文件清单见 [最新修正说明](Docs/RealmCorrection.md)，开发规则见 [AGENTS.md](AGENTS.md)。

保留 `Assets`、`Packages`、`ProjectSettings`、`Docs`、`Tools` 及轻量检查报告。Unity 缓存、临时测试项目、编译产物与旧资产备份不纳入版本控制。
