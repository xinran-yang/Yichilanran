# 传送点贴合分界与箱②绿色换色修复

## 原因与当前行为

原蓝/白道路之间显式留有箱体空间，但传送点直接按蓝态y'=12-y计算白态高度，导致部分点比白态实际路面高1.04。箱②推上绿色①时，脚底在白路4.88附近，而旧点在5.90附近，只能贴到箱顶；原支撑判定因此没有将其识别为带箱候选。

现在每个点配置两态实际道路界面。传送点渲染位置与触发位置共用Floor；单人传送落位也使用目标界面。不再通过同时匹配任意箱面掩盖悬高。箱子优先以实际同色路面的支撑面参与判定，只有真实倒挂且无脚底支撑时才使用箱顶。保留同色、完整入区、落稳、主动Dive、目标安全等条件。

所有箱子ID共用这一判定；实际绿色带箱传送必换色。远处随世界镜像的箱子不变色，黄色不改箱色。

| 关卡/入口 | 蓝态边界Y | 白态边界Y |
|---|---:|---:|
| 2.1 黄色 | 2.00 | 8.96 |
| 2.1 绿色 | 3.70 | 7.26 |
| 2.2 黄色 | 1.05 | 10.95 |
| 2.2 绿色① | 6.10 | 4.86 |
| 2.2 绿色② | 4.10 | 6.86 |

两关地形、箱子初始位置、出生点和旗帜数据与修复前逐项比较未变；本轮仅重生成两关入口配置及包含它们的场景。地形/箱子/旗帜仍按同一轴严格镜像。入口两态高度是明确配置，不进行临时找地面或吸附箱子。保留未配置旧沙盒的反射回退以兼容已有资料。

## 实际运行结果

独立Unity6000.3.28f1编译通过。两关从出生点到旗帜的连续输入PhysicsScene2D物理路线通过，传送顺序与箱色符合现有标准解。

PortalBoundary-2.1.txt / 2.2.txt检查每个入口在蓝态、白态、再次蓝态时，整个宽度有同色实体顶面精确承接。不是只比较一个中心点。

GreenBoxColor-2.2.txt/csv由出生点开始，不在路线中放置人物或箱子。黄色后把箱②向左推到绿色①，实测脚底与点相差0.02（接触缝隙），箱顶距离点1.02，支撑为White approach region、side=1。有效Dive后记录Green 1 + Box 2，箱②白→蓝；再次有效Dive蓝→白。其余箱①蓝、箱③白保持，所有箱镜像审计通过；R恢复原始颜色和计数。

箱②上方白块压低跳跃空间，验证使用较早的空中Dive输入；地面按下冲仍不传送。人工键盘容错、视觉表现、所有异常路线穷举及Player Build未执行。

## 使用

停止Play并重新打开Level_2_1.unity或Level_2_2.unity，等待Unity编译。无需手动移动传送点。F3或Console筛选MirrorAudit查看zone、箱子颜色、participated及formulaVerified。

重新生成入口数据可用原生成菜单。专项自动验证入口为YiChiLanRan.Editor.GreenBoxColorChecks.BuildAndRun，应在独立副本批处理执行；它会重生成两关并检查正常路线、完整界面覆盖、箱②往返换色。

## 文件

- Assets/_Game/Scripts/LevelDefinition.cs：两态入口边界配置。
- Assets/_Game/Scripts/TransferZone2D.cs：界面位置及真实支撑面判定。
- Assets/_Game/Scripts/WorldMirrorSystem.cs：入口目标快照和单人目标界面落位。
- Assets/_Game/Editor/LevelCatalog.cs / Level22Layout.cs：入口明确高度，地形不改。
- Assets/_Game/Editor/GreenBoxColorChecks.cs及meta：专项检查。
- 两关对应的Scenes与Data/Levels资产；AGENTS.md、预览脚本、报告与本说明。

旧入口资产备份在DevReports/BeforePortalBoundary。预览已按新边界更新，仍是数据绘图而非Unity截图。
