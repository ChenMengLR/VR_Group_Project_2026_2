# 第 5 周空间音频验证记录

日期：2026-10-08

项目：独立一人小组《失联空间站：重启舱门》

课程要求：[Spatial Sound and Sound Interaction](https://app.notion.com/p/Spatial-Sound-and-Sound-Interaction-3cf7c00fe5ad81049defe55c2eb0e122)

## 本次完成

1. 在 `Assets/07_Sounds/AMBIENT` 和 `SFX` 放入一段 8 秒循环环境声，以及抓取、松开、供电成功、任务完成四段不同的短音效。全部是项目脚本生成的 44.1 kHz、16-bit、mono WAV；音频说明记录了来源与使用条件。
2. 在能量核心上添加 `PointableUnityEventWrapper`。Inspector 中 `WhenSelect → SpaceStationGrabAudio.PlayGrab`，`WhenUnselect → PlayRelease` 是持久事件；OneShot 来自跟随核心移动的 3D 音源。松开声在所有松开动作发生，是否放准插槽由任务脚本独立判定。
3. 在插槽放置循环环境声和供电成功声，在出口放置任务完成声。四个 AudioSource 的 `Spatial Blend=1`、`Spatialize=On`、`Linear Rolloff`、`Doppler=0`；AudioManager 选择 `Meta XR Audio`。头显相机为唯一启用的 AudioListener。
4. 同步生成个人练习场景 `Assets/Scenes/Week05_WangHaobin_SpatialAudio.unity` 和小组主场景 `Assets/01_Scenes/Solo_SpaceStation.unity`，主场景仍是 Android 启动场景。更新策划案，列出声音位置、时机、目标信息及第 6 周输入方案。

## 已执行的检查

| 检查 | 方法与结果 | 可证明的范围 |
| --- | --- | --- |
| 音频文件完整性 | Python `wave` 读回 5 个 WAV：均为 44.1 kHz 单声道，长度 8.00/0.22/0.28/0.62/0.72 秒；峰值均低于 0.63；循环环境声的首尾样本差约 0.00067 | 文件格式与生成结果，不能证明设备上的实际音量 |
| 场景生成 | Unity `6000.3.24f1` 执行 `CourseSetup.SoloStationBuilder.Build`，日志出现 `SOLO_STATION_BUILD_OK`，场景结构检查通过且 Missing Script 为 0 | 组件、资源与事件接线；批处理里曾有无图形音频插件警告，窗口化 Play 另行检查 |
| 模拟器 Play 启动 | 窗口化 Unity + Meta XR Simulator；[机器可读报告](SoloStation_PlaySmoke.json) `passed=true`、570 帧、运行错误 0，XR 头显与加载器有效 | 插槽循环声处于播放状态，Meta XR Audio 被选中且原生插件在窗口化日志中初始化；唯一 AudioListener、四个空间化音源、五个音频引用均有效 |
| 抓取/松开回调 | Play 检查直接调用包装组件的 `WhenSelect.Invoke` / `WhenUnselect.Invoke`，`GrabCount`/`ReleaseCount` 各增加一次 | 此单项只检查持久 UnityEvent 到 OneShot 方法的接线，真实模拟控制器另见下一项 |
| 实际模拟器输入流程 | 另一次连续 Play 中，Meta XR Operator 用右控制器 Grip 抓住核心，从 `(1.15, 0.97, 1.30)` 移动到插槽 `(1.85, 1.01, 1.30)` 并松开；舱门由 Y=1.24 升到 3.74。模拟头显走到 `(4.45, 1.70, 0.75)`；[机器可读记录](Week05_OperatorFlow.json)、[开门画面](Week05_Operator_PoweredHatch.png)、[出口画面](Week05_Operator_Final.png) | 同一会话的 Unity 日志先后记录 `grab_core`、`release_core`、`power_restored`、`mission_complete` 的实际 `AudioSource.PlayOneShot` 调用。此会话使用模拟头显物理位移到维修区与出口，未重测控制器传送 |
| 有声模拟器演示 | [94 秒连续视频](../Week05_Simulator_Audio_Demo.mp4)：另一次 Windows Editor Play 会话中，Game Bar 捕获游戏窗口和系统双声道音频；Meta Operator 移动模拟头显、右控制器 Grip 抓取与松开，顺序完成供电和出口。该会话 Unity 日志也依次出现抓取、松开、供电和任务完成四条播放记录 | FFmpeg 读回 H.264 1280×720、30 fps 和 AAC 48 kHz 双声道；声音轨非静音，抓放与任务提示已进入录屏。视频是自动协助的 Meta XR Simulator 演示，**不是 Quest 真机录像或学生手动操作记录** |
| Android 构建 | Unity `-buildTarget Android` 执行 `BuildAndroidApk`，日志出现 `SOLO_STATION_APK_OK`，最终版 APK 于 2026-10-08 13:12 更新，退出码 0；本机被 Git 忽略的 `Builds/SoloSpaceStation.apk` 为 106,838,539 字节 | 新音频和场景可纳入 APK 构建；**不等于 Quest 安装与运行** |

窗口化 Play 日志明确出现 `Meta XR Audio Native Interface initialized with Unity plugin`。批处理无音频设备时出现的空间化初始化警告不作为玩家运行结果；真实运行以窗口化检查和设备听感为准。

## 流程与声音验收边界

第 4 周版本已有一次[完整的 Meta XR Simulator 控制器流程](SoloStation_ControllerFlow_20261001.md)：传送→真实 Grip 抓取→持握移动→在插槽松开→舱门打开→模拟头显到出口。本轮新流程重新验证了真实**模拟** Grip 抓放与四种 OneShot 调用，并补录了含 Windows 音频输出的连续视频。录屏通过 Xbox Game Bar 制作；发布版裁掉了部分编辑器边框、缩至 1280×720，并为演示清晰度对整段音频统一增加 8 dB，保留了原始左右声道关系。音轨最大峰值约 -4.5 dBFS。分段测得左右声道电平存在差异，但录屏和日志仍不能替代佩戴耳机确认远近、左右定位和 Quest 实际响度。尚未产出课程所要求的 Quest 真机有声试玩录像。

实际试玩时请戴立体声耳机，先在起点听插槽声，再靠近维修台、转头比较左/右方位；抓住核心确认抓取声，任意位置松开一次确认松开声；重置后将核心放入插槽，确认松开声与供电成功声先后可辨，舱门抬升；走到 EXIT 确认结论声。Quest 设备可用时，用系统录屏保留真实游戏音频，连续展示这条流程，并把录像标为 Quest 真机。现阶段有声演示和截图均只能标为 `Meta XR Simulator / 非 Quest 真机`，课程方是否接受替代仍待确认。
