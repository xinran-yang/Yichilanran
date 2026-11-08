# 本轮镜像轮廓及 2.1 高度修正

用户最新选择：取消白色中继落脚面，降低原蓝色主平台，保持手感，并禁止从蓝态凹坑直接跳上绿色平台。

## 实现尺寸

地图逻辑边界24×12，镜像轴 y=6。人物0.56×0.76，箱子1×1；正交镜头、16:9逻辑画幅和1920×1080默认窗口不变。

| 2.1 项目 | 当前值（Unity单位） | 用途 |
|---|---:|---|
| 起点/箱①地面顶 | 1.25 | 比旧版加厚一层1.00 |
| 黄色侧蓝色主台顶 | 2.00 | 原3.00；降低后白态借②可达中央 |
| 凹坑范围 | x=13.10～17.70，底0.95 | 加宽至4.60，抑制从主台横越 |
| 右侧绿色台顶 | 3.70 | 坑底至此高差2.75，挡直接跳跃和回弹 |
| 原有中层/旗台顶 | 6.45 / 8.15 | 随右台上移0.70，维持借③路线 |
| 白态主路/中央顶 | 8.96 / 11.05 | 高差2.09；借箱②配原白跳通过 |

未新增中继面。Rules的速度、跳跃、下冲、回弹和重力数值不变。为同时满足正常解与防直跳，除降低黄色侧主台外，右侧台阶和坑宽有上述联动调整。

## 上一版的错误及修正

上一版中心反射公式本身正确，但白色道路使用另外配置的薄平台，图上蓝色区域与白路承重边界分离。首轮切到白态显得道路位置错误，切回蓝态又恢复。

现在WhiteRealmGeometry从明确的白态实体轮廓生成填色和碰撞体，扣掉浮岛的严格镜像区域。两个关卡都使用这一通用构建工具与RealmVisuals。所有地形、箱子仍只按(x,12-y)反射，箱子保留原支撑关系，不重新寻找平台；黄色不改箱色，绿色只改参与箱颜色。

旗帜除了根节点中心外，旗杆和旗面也完整上下反射，二次切换和R可恢复。地形装饰同样反射，但不对刚体根节点使用负缩放。

## 实际检查结果

在独立Unity 6000.3.28f1副本运行并编译成功，不争用用户打开的工程。

- RealmCorrection-2.1.txt / 2.2.txt：检查所有地形和箱子中心严格反射、两次恢复、完整旗杆反射以及R恢复。该项是几何事务夹具，没有模拟物理下落。
- ReferenceRoute-2.1.txt / 2.2.txt及CSV：通过连续移动、跳跃、下冲指令执行真实PhysicsScene2D.Simulate，正常路线无中途修改人物/箱子坐标。两关均抵达终点，传送及换色顺序符合口述。
- PitGuard-2.1.txt：明确将人物放入坑内的针对性夹具，随后用正常控制与物理模拟。普通跳脚底最高2.7567；跳后下冲回弹脚底最高3.6215；绿色台顶3.7000，两种尝试均被挡住。

这不是人工键盘试玩，也不是所有异常跳法的穷举。Unity实际画面、操作容错与最新美术轮廓仍需用户验收。未运行Player Build。本轮没有重新运行最小场景的Play Mode长时测试；旧报告保留为历史结果。

## 运行与调试

生成的资产已写回主项目。先停止Play，打开Assets/_Game/Scenes/Level_2_1.unity；如果原场景已开着，重新打开以载入保存的新内容。A/D或方向键移动，Space跳，S/↓下冲，R重置。2.1后黑屏进入2.2。

如需重新生成：Tools > YiChiLanRan > 0 Generate complete greybox demo。无需手动摆地图、箱子或挂脚本。

F3重打逐箱镜像审计；Console筛选[MirrorAudit]、[MirrorBoxLive]、[MirrorTerrainLive]。记录包含轴、中心前/后/目标、公式误差、颜色、参与标记、gravityScale、自定义重力、支撑ID和碰撞状态。提交瞬间精确坐标与后续物理帧必须分开看。

最小检查场景MirrorInspection.unity仍保留：T切换、C观察者换色、A/D调试推动、F3记录、R恢复。正式关卡不启用这些调试传送输入。

Tools/Run-UnityChecks.ps1在新副本执行当前几何检查、正常路线、防直跳，再运行最小Play Mode检查；加-PublishGenerated才写回生成资产。

## 主要修改文件

- Assets/_Game/Editor/LevelCatalog.cs：两关数据及2.1联动尺寸。
- Assets/_Game/Editor/WhiteRealmGeometry.cs：统一白色实体/边界构建。
- Assets/_Game/Editor/GreyboxBuilder.cs：白态渲染组件及实体网格装饰。
- Assets/_Game/Scripts/RealmVisuals.cs：白态蓝色空间/白色实体显示。
- Assets/_Game/Scripts/WorldMirrorSystem.cs、LevelRuntime.cs：完整旗帜、装饰反射及恢复。
- Assets/_Game/Editor/RealmCorrectionChecks.cs、ReferenceRouteChecks.cs：针对性检查与更新路线。
- Assets/_Game/Data/Levels、Scenes、Prefabs：重生成的关卡数据、场景及预制体。
- Tools/Render-ReferencePreview.ps1、Run-UnityChecks.ps1、AGENTS.md及本说明。

源数据结构预览位于DevReports/ReferenceLayouts/Level-2.1.png、Level-2.1-White.png、Level-2.2.png、Level-2.2-White.png，非Unity截图。旧资产备份在DevReports/BeforeRealmCorrection，原图及Word资料未修改。
