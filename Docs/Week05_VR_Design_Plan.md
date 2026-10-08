# 第 5 周 VR 策划案｜失联空间站：重启舱门

负责人：WANG HAOBIN（一人小组）

更新：2026-10-08

课程模块：[Spatial Sound and Sound Interaction](https://app.notion.com/p/Spatial-Sound-and-Sound-Interaction-3cf7c00fe5ad81049defe55c2eb0e122)

## 作品定位与变化

**名称与主题：**《失联空间站：重启舱门》。玩家在停电的单间空间站里恢复舱门电力，随后进入出口。体验控制在一间舱室、一枚能量核心、一个插槽、一扇门，预计 1–2 分钟。

**为什么使用 VR：**玩家需用手柄指向传送热点、靠近维修台、伸手抓起核心并在空间中的插槽处松手。插槽底噪从固定方位传来，手中的抓放声随核心移动；距离和方位改变时，声音提供找目标与确认动作的线索。立体视角、手部动作、位置音效和开门反馈组成一个连贯任务。

**与原型相比：**第 4 周已有传送、真实抓放、插槽判定、开门与重置，以及运行时合成的插槽底噪/提示音。第 5 周将音频替换成仓库内可检查的 WAV 资源，新增独立的抓取声和松开声，分别接入 Meta 指针事件；环境声、核心声、插槽成功声、出口结论声都使用 3D AudioSource 与 Meta XR Audio 空间化。保留第 4 周游戏流程和操作。

## 场景顺序与交互

| 顺序 | 场景/目标 | 物体与玩家动作 | 完成判定和反馈 |
| --- | --- | --- | --- |
| 1 | 主舱起点，找到维修台 | 玩家查看停电状态板，选择 Teleport，传送至蓝色维修热点 | 进入维修区；插槽环境声更清晰 |
| 2 | 取得能量核心 | 用右或左控制器握持键抓住青色核心 | Meta `Select` 触发核心位置的短促抓取声；状态板提示放入插槽 |
| 3 | 给舱门供电 | 把核心移动到橙色插槽附近并松开 | Meta `Unselect` 先播放核心位置的松开声；曾抓取过且释放位置距插槽不超过 0.20 m，核心吸附、灯变绿、插槽成功音与手柄短振动触发，舱门抬升 |
| 4 | 离开空间站 | 穿过开启的舱门走到 EXIT 标记 | 出口位置播放结论音；状态板显示 `MISSION COMPLETE` |
| 5 | 重玩 | 回到主舱，按右手柄 B；电脑模拟器可按 R | 核心、灯、舱门和状态板复位；空间底噪持续 |

一人小组的个人练习场景是 `Assets/Scenes/Week05_WangHaobin_SpatialAudio.unity`，小组整合并用于构建的场景是 `Assets/01_Scenes/Solo_SpaceStation.unity`。两者由同一构建器同步生成，避免个人练习与集成版本出现交互差异。

## 第 5 周声音设计

| 声音 | 资源、位置与参数 | 触发时机 | 传达的信息 |
| --- | --- | --- | --- |
| 环境声 | `AMBIENT/airflow_loop_8s.wav`；插槽位置；Loop；音量 0.45；线性衰减 0.6–8 m | 进入场景后持续 | 维修区有电力系统，帮助玩家听方位、靠近插槽 |
| 抓取声 | `SFX/grab_core.wav`；核心上的 AudioSource；音量 0.72；0.35–7 m | `PointableUnityEventWrapper.WhenSelect` | 已抓住核心 |
| 松开声 | `SFX/release_core.wav`；同一核心 AudioSource | `WhenUnselect`，无论是否放准插槽 | 核心已脱手，和成功音分开 |
| 供电成功 | `SFX/power_restored.wav`；插槽位置；音量 0.60；0.5–8 m | 有效释放到插槽并开始开门 | 核心插入成功，舱门正在开启 |
| 任务结论 | `SFX/mission_complete.wav`；出口标记上方；音量 0.58；0.5–8 m | 经过开启的舱门并抵达 EXIT | 整段任务结束 |

所有文件为自制的 44.1 kHz、16-bit、mono PCM WAV；来源、生成脚本与使用条件见 `Assets/07_Sounds/README.md`。场景中四个 AudioSource 均设为 Spatial Blend 1、Spatialize 开、Linear Rolloff、Doppler 0；Meta XR Audio 已在项目 AudioManager 中选为 Spatializer。头显相机保留唯一启用的 AudioListener。音量与衰减距离是初版值，可在有 Quest 设备时按实际听感调整。

## 第 6 周输入与反馈计划

- **控制器：**继续使用 Meta XR Controller Grab，Select/Unselect 作为抓放与声音事件；右手柄 B 重置。现有成功时的左右手柄 0.18 秒振动在 Quest 上确认强度与舒适度。
- **手追踪：**本项目当前使用 ControllersOnly。第 6 周先评估是否能在不增加玩法复杂度的情况下提供手追踪备选，若引入则复测抓放事件与空间声，不把尚未实现的手追踪写成已完成。
- **语音：**任务已有可见状态板与 3D 音频提示，本阶段不加入语音识别。第 6 周可评估单句语音提示是否有助于首次玩家，但不会用语音取代抓取、插入与开门的直接反馈。

## 组件、脚本与验证

- `SpaceStationMission.cs`：插入条件、门动画、复位、成功/结论声音与振动。
- `SpaceStationGrabAudio.cs`：核心随动 AudioSource 的抓取/松开 OneShot；由 `PointableUnityEventWrapper` 的持久 UnityEvent 调用。
- `SoloStationBuilder.cs`：生成两份场景、装配音源与事件、核对唯一 AudioListener、资源、空间音频配置和 Missing Script。
- `Generate-Week05Audio.py`：可重复生成全部五个声音资源。
- `SoloStationPlaySmoke.cs`：Windows Editor + Meta XR Simulator 启动检查。第 5 周还需检查运行中的音源配置与抓放事件。

**验收方式：**先做资源/场景结构检查；再在 Meta XR Simulator 中戴立体声耳机测试远近与左右方位，并完成传送→抓取→松开→插入→开门→出口，记录实际听到的五个声音、画面和运行日志。最后在 Quest 上录制含真实游戏声音的连续视频，重点包含环境声方位、抓取声和松开声。没有 Quest 时模拟器检查可作为阶段证据，不能标作真机视频。

## 进度

- **第 5 周实现：**WAV 资源、3D 声源、Meta 抓放事件、个人练习/小组整合场景与策划案已接入。电脑模拟器完成真实模拟 Grip 抓放、供电与出口流程，并录制了含系统音频的连续视频；构建与运行结果见 `CourseValidation/Runtime/Week05_Validation.md`。Quest 真机有声视频尚待设备实测。
- **第 6 周：**Quest 控制器手感、振动与声场实测；按结果调音量与衰减，评估手追踪或语音提示的价值。
- **后续：**完整可试玩原型、面向课程的连续演示与提交。Quest 真机视频取决于可用设备，不以电脑模拟器冒充。
