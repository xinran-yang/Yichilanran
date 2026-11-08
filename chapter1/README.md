# 《一尺蓝染》· Chapter 1 Unity 工程

独立的 Unity 6000.3.28f1（6.3 LTS）/ Universal 2D / URP / C# 工程，包含第一关与第二关的连续横向地图。

本目录是归档工程，与团队仓库根目录已有 Unity 工程并列保存。请单独将 chapter1 添加到 Unity Hub，不要把本目录的 Assets 合并进根工程。

## 打开项目

1. 克隆本团队仓库，并切换到 feature/chapter1-unity-demo 分支；PR 合并后可使用 main。
2. 在 Unity Hub 中选择 Projects → Add → Add project from disk，选择本 README 所在的 chapter1 文件夹。
3. 使用 Unity 6000.3.28f1 打开，等待 Unity 根据 Packages 导入依赖；Library 等缓存会在本机重新生成。
4. 打开 Assets/Scenes/Chapter01.unity，点击 Play，再点击 Game 视图获取键盘焦点。

## 操作与连续路线

- A / D 或左右方向键：移动。
- Space：跳跃。
- S 或向下方向键：空中向下冲跃。
- R：从第一关起点重新开始。

第一关从蓝态开始，在起点附近莲花标记处跃起后冲跃，切至白态，跨过镜像凹谷和平台到达首段出口。出口后的连接莲花允许主动冲跃回蓝态，然后直接向右进入第二关低地，无加载、选关或重新出生。

第二关在低处莲花 A 冲跃变白，跨凹谷并沿小台阶到高处莲花 B，再冲跃回蓝，沿右侧阶梯到区域末端旗标。A/B 只是文档中的称呼，正式画面不显示文字标签。

## 已实现

- 同一 Chapter01 场景、同一角色和相机贯穿两关；第二关位于世界 x=18～44.5。
- 蓝白同色碰撞、固定入口合法冲跃、整张基础几何严格上下镜像。
- 所有入口共用世界 y=-3.95 轴，角色以脚底中心严格映射，保持正立和左右操作方向。
- 检查点与相位恢复、首段/第二段进度节点、靛蓝米白布纹和统一莲花 Prefab。
- 100 PPU；人物显示及碰撞框均为 0.28×0.38 单位；视野 12.8×7.2 单位；基础参数集中于 Assets/Resources/Configuration/GameParameters.json。
- 正式文字 HUD 默认隐藏；ChapterWorld Inspector 中可手动启用调试覆盖层。

## 验证与已知限制

原开发记录表明：2026-11-08 在 Unity 6000.3.28f1 无图形 Play 验证中，同一角色完成两关连续路线，共四次合法穿界；Windows 正式构建成功，0 错误。详情见 [验证记录](docs/LEVEL02_VALIDATION.md)。这些是归档版本的既有结果，不代表本次上传重新执行了 Unity 通关或构建。

本次归档执行文件完整性、复制哈希、meta、依赖与敏感文件检查。尚需人工确认最终图形、键盘手感、入口边缘往返、自然跌落恢复、不同分辨率及低帧率表现，未穷举所有逆向路线。

没有箱子、尖刺、白色脆裂、复杂音效、持久化存档或第三关。检查点只保留本次游玩；R 会重开整段。蓝色弹性起跳与固定冲跃回弹已接入。

## 配置、生成与协作

- 第一关数据：Assets/Resources/Configuration/Chapter01.json。
- 第二关扩展：Assets/Resources/Configuration/Chapter01Level02.json。
- 角色与世界：Assets/Scripts/Gameplay/ClothPlayer.cs、ChapterWorld.cs。
- 穿界 Prefab：Assets/Prefabs/CrossingPoint.prefab。
- 当前规则：[CURRENT_GAME_RULES.md](docs/CURRENT_GAME_RULES.md)；第二关详细说明：[LEVEL02_UNITY.md](docs/LEVEL02_UNITY.md)。

已保存正式场景，正常打开不必重新生成。仅需重建时，停止 Play 后先执行 YiChiLanRan → Build Chapter01，再执行 Extend Chapter01 with Level02；生成操作会更新生成资源，请先保存自己的工作。

提交并保留所有 .meta；不要提交 Library、Temp、Logs、UserSettings、构建目录或个人 IDE 配置。只打开 chapter1 工程开发其内容，避免影响根工程。根工程的 2.1/2.2 灰盒与本目录的第一/第二关是不同工程，不应混用规则或数据。

## 归档来源

源项目分支：feature/chapter01-level02。
源提交：b9fcd5d846f49308d14c3081285a2695ede4e7b8。
归档日期：2026-11-09（Asia/Shanghai）。源项目 Git 历史未导入团队仓库，构建产物与临时验证副本未上传。参考数值/SVG中的旧 Demo 说明仅作历史参考，以当前规则和 C# 实现为准。
