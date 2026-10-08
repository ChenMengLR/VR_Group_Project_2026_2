# 第 5 周小组整合记录

小组：WANG HAOBIN（一人小组）

日期：2026-10-08

主场景：`Assets/01_Scenes/Solo_SpaceStation.unity`

个人练习：`Assets/Scenes/Week05_WangHaobin_SpatialAudio.unity`

| 项目 | 整合情况 |
| --- | --- |
| 场景与负责人 | 同一人维护个人练习与小组主场景。两份场景由 `SoloStationBuilder` 同步生成；Android 构建只使用小组主场景。 |
| 脚本 | `SpaceStationMission.cs` 负责关卡、供电/出口提示；`SpaceStationGrabAudio.cs` 负责核心抓放声音；`SoloStationBuilder.cs` 设置 Meta 指针事件和 3D 音源。 |
| 音频 | `Assets/07_Sounds/AMBIENT` 一段循环声，`SFX` 四段短声。自制资源来源、生成方式和使用条件见目录 README。 |
| 接口与冲突 | Meta `PointableUnityEventWrapper.WhenSelect/WhenUnselect` 连接无参声音回调。构建器自动生成时需初始化 UnityEvent；已处理。Android 构建切换后有时缺少 Windows Operator 层清单，`Week03SimulatorSetup` 在再次进入模拟器前会修复。 |
| 测试 | 场景结构检查、窗口化 PlaySmoke、Meta XR Simulator 真实模拟 Grip 抓放与完整开门/出口流程、[电脑模拟器有声视频](../CourseValidation/Week05_Simulator_Audio_Demo.mp4)、Android APK 构建均已完成。详见[第 5 周验证](../CourseValidation/Runtime/Week05_Validation.md)。 |
| 尚待设备 | Quest 真机戴耳机核对方向与距离、音量、舒适度；录制带游戏声音的 Quest 连续视频。当前模拟器素材须明确标为非 Quest 真机。 |
| 下一步 | 第 6 周确认控制器振动强度和重置操作，视设备条件决定是否评估手追踪或简短语音提示；之后完成课程演示材料。 |

课程提交时可使用[小组仓库](https://github.com/ChenMengLR/VR_Group_Project_2026_2)链接、该整合记录和[更新的策划案](Week05_VR_Design_Plan.md)。最新提交号以本次实际推送结果为准。Quest 有声录像待设备实测，不把本次模拟器视频与截图标成真机证据。
