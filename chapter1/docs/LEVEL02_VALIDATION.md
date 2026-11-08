# 连续第二段验证记录

日期：2026-11-08。引擎：Unity 6000.3.28f1。

## 结果

- Unity运行时与Editor代码编译通过。
- 连续Play验证：PASS，从第一关起点出发，沿原第一关路线完成首段，再在同一个场景、同一个玩家对象上完成连接穿界及第二关A/B穿界，最终脚底(43.21,-6.20)，总穿界次数4。
- Windows x64构建：Succeeded，errors=0。最终采用BuildOptions.None正式构建，避免Unity的Development Build文字覆盖层。构建输出放在artifacts/Build，必须保留exe旁的Data目录、UnityPlayer.dll等文件。
- 原第一关Chapter01.json、共享GameParameters.json、ClothPlayer.cs、CrossingPoint.cs、TerrainPiece.cs、Art与Prefabs均与稳定提交d084360一致；没有修改角色尺寸、运动参数或原莲花Prefab。

## 执行方式与实际检查

原编辑器保存扩展场景并通过第一关回归后不再更新日志，窗口控制activate_window超时，恢复时也未找到可操作窗口。未结束该编辑器进程。为完成必要实际验证，在Git忽略的.validation中复制相同Assets/Packages/ProjectSettings，并由Unity自身执行Editor API场景扩展、批处理Play和Windows构建；不是另建Demo或测试房间。核对了副本与正式项目C#源码哈希一致。交付场景为该次Editor API保存的已验证场景，保留原场景.meta及资源GUID。

自动验证直接向ClothPlayer输入接口提供按键意图，角色由正式Step、扫掠碰撞与TryCross运行，不通过瞬移伪造推进。只有检查点恢复测试主动调用正式Respawn方法，检查恢复脚底与颜色。

检查项：

1. 原第一关完整路线回归。
2. 同一场景handle、同一玩家instance ID贯穿两段。
3. 原入口蓝→白、连接入口白→蓝、A蓝→白、B白→蓝，穿界总数严格为4。
4. 入口脚底反射断言、每个地形的相位位置与Collider尺寸、两次反射恢复基础几何。
5. 连接检查点蓝态激活，Respawn恢复正确脚底、颜色及整图相位；B后检查点激活。
6. 白态凹谷与小台阶可达，蓝态60px阶梯可达，第二段进度节点可到达。
7. 正式场景showDebugOverlay=false。
8. A前370px墙超过最高回弹；B后白态420px出口墙及高处间距超过白跳预算。这是数值与几何检查，不宣称穷举所有操作绕路。

## 仍需人工试玩

本次最终通路运行是无图形批处理，未取得第二段最终图形验收截图。请打开Chapter01或运行构建，人工确认无文字画面、卷轴视觉连续、角色/莲花辨识度、印花效果、键盘手感、边缘往返穿界与自然跌落后的恢复。未做极端低帧率、所有分辨率、所有逆向路线的穷举。

没有新增第三关、箱子、尖刺、白脆系统或复杂存档界面；本轮仅连续第二段。原稳定提交和既有标签不改动，不合并master/main、不推送远程。
