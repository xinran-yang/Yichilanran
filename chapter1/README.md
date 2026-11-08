# 《一尺蓝染》· 第一章三关 Demo

Unity **6000.3.28f1 / Universal 2D / URP / C#** 独立工程。包含第一关、重制第二关和回纹迷宫第三关，关间自动黑屏过渡。与仓库根目录其他成员的 Unity 工程分开，不要合并 Assets。

## 直接试玩（Windows x64）

从 [三关试玩版 Release](https://github.com/xinran-yang/Yichilanran/releases/tag/chapter1-demo-20261109-97905b8) 下载 ZIP，完整解压后运行 YiChiLanRan.exe。保留 Data、MonoBleedingEdge、D3D12 和 UnityPlayer.dll 等依赖；不需要 Unity Editor。请阅读包内 README.txt。该包不是 macOS、移动端或网页版。

## 在 Unity 中打开

1. 克隆本仓库，切换到 feature/chapter1-unity-demo（PR合并后可使用main）。
2. Unity Hub → Add project from disk，选择 **chapter1** 文件夹。
3. 使用6000.3.28f1，等待依赖导入。
4. 打开 Assets/Scenes/Chapter01.unity 并 Play。也可分别打开 Chapter01_Level02.unity 或 Chapter01_Level03.unity。

## 操作和路线

A/D或左右箭头移动；Space跳跃；S/下箭头空中冲跃；R重开当前关；Standalone可用Alt+F4退出。

第一关在莲花处冲跃换白，越过镜像凹谷到右侧出口。出口0.4秒淡出，全黑载入第二关，再0.4秒淡入，期间锁输入和清速度。

第二关左下A切白，沿回纹阶梯折返至B切蓝，从上层和右侧通路到出口，再以同一系统进入第三关。

第三关左下A切白，沿左回纹转折到B切蓝，借蓝面冲跃回弹上中央回纹；下方有可选信封支线。回下通路从高墙下方通过，右下C切白，从右侧阶梯抵达亭子。第三关出口预留后续接口；未制作第四关，不会进入永久黑屏。

## 系统与资源

三关共用ClothPlayer、ClothPlayer.prefab、CrossingPoint.prefab、ChapterWorld及LevelTransitionManager；人物0.28×0.38u、100PPU、12.8×7.2u视野和原运动参数保持。地图采用同一蓝形状与白补集，按关卡轴严格映射脚底与地形，角色正立、x不变；美术保持靛蓝/米白布纹。第三关仅按配置增加纵向跟随，仍用相同比例。

Collectible.prefab信封是可选奖励；每轮唯一ID只能收集一次，镜像和普通重生保留，R新一轮恢复。目前没有退出后的永久存档、复杂音效、箱子、尖刺或白脆玩法。正式画面默认不显示文字，调试HUD可在Inspector手动开启。

当前数据：Assets/Resources/Configuration/Chapter01.json、Level02.json、Level03.json；共享运动GameParameters.json，过渡LevelFlow.json。

正常试玩无需生成。第三关生成用YiChiLanRan → Build Level03 and Link，不重新生成前两关。旧连续关卡工具和历史说明不适用于当前三关版本。开发前请保存自己的场景工作并保留全部.meta。

## 验证和限制

原开发记录：前两关输入路线和生产黑屏进入第三关通过；首次第三关实际发现软锁，经局部90×70px连接修正后单独复测第三关通过，包括三次穿界、信封、重生和重开。没有将分段检查冒充最终地图在一次测试中完整跑过三关。三Scene Windows正式构建成功、0错误。

本次归档/打包核对源码与构建快照、完整依赖、ZIP逐文件SHA256，并短暂启动Windows程序验证渲染/程序集/输入初始化；未重新手动通关。图形、手感、不同电脑/分辨率、低帧率和所有逆向操作仍需人工验收，不宣称唯一解。

详细说明：[当前规则](docs/CURRENT_GAME_RULES.md)、[独立关卡](docs/LEVELS_REWORK.md)、[第三关](docs/LEVEL03_UNITY.md)、[第三关验证](docs/LEVEL03_VALIDATION.md)。旧连续方案文档仅作历史记录。

## 归档来源

游戏代码基于本地提交 **97905b8518097e355952063dd36e274620bf53db**，并包含当前磁盘的docs/level_references目录整理。归档日期2026-11-09。源工程Git历史没有合并入本团队仓库。Unity缓存、IDE个人配置、日志和构建文件不纳入源码；试玩ZIP通过Release提供。