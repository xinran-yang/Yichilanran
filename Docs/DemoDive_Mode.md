# 可玩性Demo：停用S下冲自动高回弹

按用户要求，当前Rules.EnableDiveBounce=false。S下冲落在蓝色地面或蓝箱上时正常落地，不再自动高回弹。原Bounce参数和代码保留，后续可恢复。

Space跳跃及其现有材质跳高、地图、移动和重力未改变；S黄色/绿色传送仍保留，绿色实际带箱处理优先于普通落地。

Unity6000.3.28f1独立副本实际检查通过：

- 蓝地面、蓝箱①：主动S下冲后最高自动向上速度均为0，最终接地。
- 2.1和2.2完整正常路线通过。
- 箱②绿色①双向白→蓝→白通过；其他箱不连带换色；两态入口仍在实际边界。
- C#编译通过。未执行人工键盘手感或Player Build验收。

修改运行时文件：LevelDefinition.cs、LevelRuntime.cs。新增专项检查DemoDiveChecks.cs；报告见DevReports/DemoDive-NoBounce.txt/csv。无需重新生成地图，停止Play并等待Unity编译后重新运行即可。
