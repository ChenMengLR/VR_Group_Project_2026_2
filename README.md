# VR Project 2026 2

最新个人作业状态见 [个人作业进度](Docs/Current_Personal_Assignment_Status.md)。2026 年 10 月 1 日用户又启动独立一人的小组项目，项目说明见 [《失联空间站：重启舱门》](Docs/Solo_SpaceStation_Project.md)。小组第 6 周的英文任务 UI 和第 7 周交付状态见 [整合记录](Docs/Week06_Team_Integration.md)与 [Week 07 交付目录](Docs/Week07/)。

**2026-10-20 外观与动画更新：**当前小组场景已换成 E-07 能量核心、三夹爪配套插槽和双扇科技舱门。插入后按“夹紧 → 解锁 → 双门滑开 → 绿灯通行”播放 2.65 秒动画；15 项自动 Play 检查通过。新版 APK 为 `0.3.0`，最新截图、6 秒无声动画和验证边界见[硬件升级记录](Docs/Week07/Hardware_Upgrade.md)。打开新版请使用 `Course > Open Week 06 Team UI`；`Open Solo Space Station` 保留第 5 周旧场景。

WANG HAOBIN 的 VR 课程项目。第 2 周使用 Unity XR Interaction Toolkit 学习抓取，第 3 周使用 Meta XR Interaction SDK 学习移动与传送。独立小组项目复用第 3 周交互基础，制作单房间空间站任务。Quest 设备计划借用，当前尚未连接或实测；以前的电脑端演示使用 Meta XR Simulator，未把模拟器描述为 Quest 真机。课程方是否接受该替代方式仍需以课程要求或教师意见为准。

## 打开和练习

本机项目：`C:\Users\31797\Documents\ChatGPT\作业 3\VR_Project_2026_2`。双击根目录 `Open-Course.cmd`，或通过 Unity Hub 打开本目录。

编辑器启动完成后，先确认 Console 没有编译错误。保存当前场景、退出 Play，再用以下菜单切换：

如果刚构建过 Android APK，先在 Unity 的 Build Profiles 将活动平台切回 Windows，等待导入完成，再打开 Meta XR Simulator 练习场景。课程菜单会在活动平台不是 Windows 时给出提示。

- `Course > Open Week 02 Computer Practice`：打开第 2 周场景、关闭 Meta 模拟器及桌面 OpenXR 自动启动，使用 XRI 键鼠模拟器。
- `Course > Open Week 03 Computer Practice`：打开第 3 周场景，启用桌面 OpenXR 自动启动及本机 Meta 模拟器。
- `Course > Open Solo Space Station`：打开独立小组项目场景，启用桌面 OpenXR 与 Meta XR Simulator。
- `Course > Open Week 05 Personal Sound Practice`：打开第 5 周个人练习场景，启用同一模拟器；戴立体声耳机比较插槽远近与左右方位，再试抓取/松开音效。
- `Course > Open Week 06 Team UI`：打开第 6 周团队场景，查看英文 Mission Console；控制器射线交互需在 Play 中继续核验。

第 2 周点击 Game 获得焦点后，用 WASD 移动，H 切换头部，[ / ] 选择左/右设备。G 抓取右手、T 激活右手；左手加 Shift。反复按同一括号会切换 Controller/Hand，练习时保持 Controller。详见 Docs 中的课程指南。

第 3 周本机 Simulator 205 绑定为 M 菜单、Y 摇杆向上、U 抓握、T Trigger。I 是按下摇杆，不是向上。打开 Locomotion Settings 切换 Slide/Teleport、Snap/Smooth Turn；按键以本机 Input Bindings 为准。

## 环境版本

- Unity Editor `6000.3.24f1`，Universal 3D / URP `17.3.0`。
- Input System `1.20.0`；XR Interaction Toolkit `3.3.2`；OpenXR `1.18.0`。
- Meta XR All-in-One / Interaction SDK `205.0.0`；独立 Windows Meta XR Simulator `205.0`。
- Android Build Support；OpenJDK `17.0.18+8`；NDK `27.2.12479018`（r27c）；SDK Build/Platform Tools `36.0.0`；Command-line Tools `16.0`；CMake `3.22.1`。

Windows 和 Android 已配置 OpenXR/Oculus Touch，Android 启用 Meta Quest Support。课程使用 ControllersOnly，不启用真实手追踪。独立小组场景已成功构建 Android APK，并在 Windows 编辑器的 Meta XR Simulator 中走通控制器传送、抓取、插入核心与开门流程；尚未进行 Quest 真机测试。

## 场景和任务

`Assets/01_Scenes/Week02_XR_Basics.unity` 包含一个官方 XR Origin、一个 XR Interaction Simulator、静态地面、两个方块和独立球体 ExtensionObject。颜色依次表现普通、悬停、选择、激活状态。RotateObject 脚本保留，最终抓取场景中禁用旋转组件。

`Assets/01_Scenes/Week03_Meta_Locomotion.unity` 基于官方 LocomotionExamples 副本，保留原来的 Meta Rig、移动环境与设置 UI。新增绿色 StartZone、蓝色 TargetZone、支持 SnapPositionAndRotation 的热点，以及带真实 Meta Grab/Rigidbody 的 MissionObject。官方原样例未改动。

`Assets/01_Scenes/Solo_SpaceStation.unity` 是第 5 周独立小组项目《失联空间站：重启舱门》。保留 Meta Rig、控制器交互和移动设置 UI，关闭旧场景环境；新建封闭空间站、维修台、可抓取能量核心、插槽、滑动舱门、出口与空间音效。把能量核心抓起并释放在插槽内才会通电开门。第 5 周个人练习副本在 `Assets/Scenes/Week05_WangHaobin_SpatialAudio.unity`，由同一构建器同步生成。第 6 周复制成 `Assets/01_Scenes/Week06_TeamUI_Wang.unity`，这是现在 Build Settings 中启用、也是第 7 周 APK 使用的场景。

第 5 周声音资源在 `Assets/07_Sounds/AMBIENT` 与 `Assets/07_Sounds/SFX`，均为项目自制单声道 WAV，可由 `python scripts/Generate-Week05Audio.py` 重建；来源与使用条件见 [音频说明](Assets/07_Sounds/README.md)。插槽底噪循环播放，能量核心的 Meta Select/Unselect 事件分别触发抓取与松开声，插槽和出口播放不同的完成提示。四个 AudioSource 设为 3D、启用 Spatialize，由 Meta XR Audio 处理空间化；场景只有头显相机一个启用的 AudioListener。[第 5 周策划案](Docs/Week05_VR_Design_Plan.md)列出声音位置、触发时机和后续计划。

一人小组的[第 5 周整合记录](Docs/Week05_Team_Integration.md)列出场景、脚本、音频、验证结果和第 6 周事项。需要复查模拟器输入时，在 Windows 构建目标的窗口化 Unity 中运行 `Course > Week 05 > Start Simulator Sound Flow`，本地 Operator 端点会在 Play 时可用；`scripts/Week05OperatorClient.py` 可连接它。退出测试前先退出 Play 并正常关闭 Editor，避免留下场景恢复提示。

第 6 周团队场景加入英文世界空间 Mission Console，显示任务状态，提供 `SHOW HELP` / `HIDE HELP` 与 `RESET MISSION` 按钮。`Course > Week 06 > Build Team UI Scene` 会从第 5 周小组场景重新生成副本，借助 Meta Ray Canvas Wizard 接入原 Rig，并保存 UI 预制件；`Course > Week 06 > Validate Team UI Scene` 检查引用和接线。[状态图](CourseValidation/Runtime/Week06_UI_Status_EditorPreview.png)和[帮助图](CourseValidation/Runtime/Week06_UI_Help_EditorPreview.png)是 Unity Editor 相机渲染，不能当作 Quest 画面。与同学试玩有关的步骤、观察和同任务复测记录见[第 6 周测试表](Docs/Week06_UI_Peer_Test.md)；尚未测试的项目保持待填。

第 7 周英文报告初稿在 [DOCX](Docs/Week07/Wang_VR_Week07_CreativeAward_Draft.docx)，Quest Android 包在 `Deliverables/Week07/Wang_VR_Week07.apk`（Git LFS）。本机构建命令为 `powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/Build-Week07QuestApk.ps1`，会将实际构建产物复制到交付目录并校验哈希。APK 构建成功不代表已在 Quest 安装或录制真机游玩视频。

## 核验记录

- 两周场景均已编译、执行构建器并通过结构检查，Missing Script 为 0。
- 第 2 周在真实 Play Mode 中完成 79 项自动 API 交互检查，全部通过；检查物理落地、六类 XRI 事件、材质颜色、实际跟随、释放与物体独立性，无运行错误。
- [第 2 周运行报告](CourseValidation/Runtime/Week02_Runtime.md)、[机器可读结果](CourseValidation/Runtime/Week02_Runtime.json)、[连续相机测试视频](CourseValidation/Week02_Automated_Play_Test.mp4)。视频 1280×720，8 fps，约 15.9 秒，记录自动验证过程。
- [第 2 周虚拟键盘输入检查](CourseValidation/Runtime/Week02_KeyboardInput.md) 一次通过：G → G+T → G → 释放，经官方模拟器触发真实 Select / Activate / Deactivate / SelectExit。临时输入设备与设置已恢复。
- [第 3 周运行报告](CourseValidation/Runtime/Week03_Runtime.md)：电脑端 Meta Simulator 的实际控制器输入完成 StartZone → TargetZone 传送、抓球、抬升 0.60 m 与释放落地；另一个关闭 Operator 的普通 Simulator 会话持续 125.6 秒 / 7,401 帧。该报告和后续视频均属于模拟器证据，未使用 Quest；连续视频已完成并补入 `CourseValidation/Week03_Simulator_Demo.mp4`；运行报告和机器可读检查点在 `CourseValidation/Runtime/Week03_Simulator_Demo_20260918.md` 与 `.json`，均注明“Meta XR Simulator / 非 Quest 真机”。
- Meta 205 的 Windows Editor 原生日志解码异常使用项目内兼容脚本处理，原始日志保留于忽略的 Logs 目录。初始化窗口和早期一次 SES 重入失败的边界见运行报告。
- 独立小组场景已通过场景结构检查与实际 Windows Editor Play 启动检查：模拟器 XR 运行时和加载器工作，HMD 追踪高度约 1.70 m，核心受物理控制，舱门关闭，空间底噪播放，无 Unity 运行错误。[运行结果](CourseValidation/Runtime/SoloStation_PlaySmoke.json)、[运行截图](CourseValidation/Runtime/SoloStation_PlaySmoke.png)、[静态预览](CourseValidation/Runtime/SoloStation_EditorPreview.png)。Android APK 已构建成功，保存在本机被 Git 忽略的 `Builds/SoloSpaceStation.apk`。本检查未执行实际控制器抓取、插入、开门或 Quest 真机试玩。
- 独立小组场景又在一次连续的模拟器 Play 会话中完成右控制器传送、Grip 抓取、持握移动、插槽放手、舱门开启与到达出口；[流程记录](CourseValidation/Runtime/SoloStation_ControllerFlow_20261001.md)、[机器可读状态](CourseValidation/Runtime/SoloStation_ControllerFlow_20261001.json)、[结束画面](CourseValidation/Runtime/SoloStation_ControllerFlow_Final.png)。出口使用模拟头显的物理位移；此证据不代表 Quest 真机或学生本人手动演示。
- 第 5 周窗口化 Meta XR Simulator Play 已检查五个 WAV、四个 3D 音源、Meta XR Audio 选择、唯一 AudioListener、插槽环境声实际播放以及合成的 Select/Unselect 回调，运行错误 0；[最新启动检查](CourseValidation/Runtime/SoloStation_PlaySmoke.json)。合成回调只验证事件接线，尚未替代真实手柄与 Quest 听感检查。[第 5 周验证记录](CourseValidation/Runtime/Week05_Validation.md)说明实现和边界。
- 第 5 周又在独立连续的 Meta XR Simulator 会话中使用官方 Operator 右控制器 Grip 抓放，核心移入插槽后开门，模拟头显抵达出口。Unity 日志依次确认抓取、松开、供电成功、任务完成四种 OneShot 被实际调用；[流程数据](CourseValidation/Runtime/Week05_OperatorFlow.json)与[开门画面](CourseValidation/Runtime/Week05_Operator_PoweredHatch.png)可核对。该次流程只保存了日志和截图。
- 另录制[第 5 周有声模拟器演示](CourseValidation/Week05_Simulator_Audio_Demo.mp4)，同一 Play 会话完成模拟手柄抓放、供电和出口；Game Bar 捕获真实 Windows 游戏音频，发布版统一加 8 dB 便于展示。视频明确标注 Meta XR Simulator / 非 Quest 真机，且不是学生手动操作。Quest 耳机声场与真机录像仍待设备实测。
- 第 6 周场景通过 Unity 构建器结构检查：Meta Ray Canvas、两个按钮的持久回调、唯一 EventSystem / PointableCanvasModule、零 Missing Script；两张 Editor 相机预览已逐张查看。窗口化 Meta XR Simulator [Play 报告](CourseValidation/Runtime/Week06_TeamUI_PlaySmoke.json)验证 Help/Reset 的合成回调和状态同步；报告保留一条 Meta MR Utility Kit 原生日志解码异常。第 7 周 Quest APK `0.2.0` 已构建，Android `aapt` 确认包名 `com.wanghaobin.loststation`。此阶段不声称按钮已在真实控制器射线下点中，也不声称 Quest 或同学试玩已经完成。

测试视频和截图属于自动协助验证，不能声称为学生本人键鼠演示或 Quest 真机录像。真实运行报告会明确记录验证方式及范围。

## 提交作业

第 2 周：个人仓库链接、ExtensionObject 的 Inspector 截图和短抓取视频。

第 3 周：个人仓库链接，以及 Start → Teleport → Grab → Release 的连续演示。按用户确认，本次采用 Meta XR Simulator 作为 Quest 真机视频的替代证据，明确标注未使用 Quest；老师或课程方是否接受这一替代方式仍待确认。

GitHub 上传与 Notion 作业提交是两个步骤。[个人作业卡](https://app.notion.com/p/Wang-Haobin-3d67c00fe5ad81288de5d923b15cf0d2) 的编辑权限已恢复，GitHub Repository 字段已填写。

- [第 2 周 Notion 作业](https://app.notion.com/p/2-2-3dd7c00fe5ad8163920ed7bbd12cd78f)：2026 年 9 月 17 日已填写 GitHub 链接，上传 `Week02_Automated_Play_Test.mp4`，在正文附上 ExtensionObject Inspector 截图并完成视觉读回；状态为“已提交”。作业中已明确标注视频是自动协助 Play 验证，不代表学生本人手动操作或 Quest 真机录像。
- [第 3 周 Notion 作业](https://app.notion.com/p/3-3-3dd7c00fe5ad8183a849f8b55c3b8133)：已填写仓库链接和电脑端报告说明；按用户确认使用 Meta XR Simulator 完成连续演示，视频已放入仓库。Notion 视频上传和提交状态仍待完成；页面和仓库会明确写明“模拟器、非 Quest 真机”，不声称教师已批准该替代方式。

独立小组项目的制作范围、操作步骤和验证记录见 [项目说明](Docs/Solo_SpaceStation_Project.md)；新场景的实际结果以其独立核验记录为准。

[独立小组项目仓库](https://github.com/ChenMengLR/VR_Group_Project_2026_2) 已公开（Public），保存可克隆的 Unity 项目、场景和验证记录。[个人课程仓库](https://github.com/ChenMengLR/VR_Project_2026_2) 继续保留原有第 2、3 周历史；这两个地址用途不同。Unity 构建输出位于本机忽略的 `Builds/`；第 7 周交付副本为 `Deliverables/Week07/Wang_VR_Week07.apk`，由 Git LFS 跟踪。

## 保存并上传新版本

在容器根目录打开 PowerShell，先保存 Unity 场景与代码，再使用统一脚本（`-Push` 才上传 GitHub）：

```powershell
.\scripts\Submit-CourseProject.ps1 -Project VR -Week 05 -Summary "完善空间音频与交互验证"
# 如果此次提交就要上传，在首次执行时加 -Push；每次提交前检查暂存文件清单。
```

提交 `Assets` 及其 `.meta`、`Packages`、`ProjectSettings`。`Library`、`Temp`、`Logs`、`UserSettings` 和构建输出由 `.gitignore` 排除。较大二进制资源按 `.gitattributes` 使用 Git LFS；不要删除或遗漏 `.meta`。

换电脑先安装相同 Editor 和必要模块，再 `git clone`、`git lfs pull`，通过 Hub 打开项目，等待包下载与资源导入。`Open-Course.cmd` 中的编辑器路径是本机路径，换电脑后按实际位置修改或直接使用 Hub。

## 课程来源

- [第 2 周 Unity Editor Basics and First XR Interaction](https://app.notion.com/p/3cf7c00fe5ad8118803cedb669e28446)
- [第 3 周 Meta XR Locomotion Lab](https://app.notion.com/p/3cf7c00fe5ad8111b891c9900e29a307)
- [第 5 周 Spatial Sound and Sound Interaction](https://app.notion.com/p/Spatial-Sound-and-Sound-Interaction-3cf7c00fe5ad81049defe55c2eb0e122)
- [第 6 周 VR UI](https://app.notion.com/p/VR-UI-3cf7c00fe5ad815c9b07de5c126580a3)
- [第 7 周 Midterm Project Presentation](https://app.notion.com/p/7-Midterm-Project-Presentation-3cf7c00fe5ad8131bac3ee94920ce56f)

Unity 与 Meta 官方导入资源保留其原许可与来源。`Assets/Editor` 的 Course 工具用于搭建、切换和自动检查；初次学习按说明理解组件后再修改，不应把构建器的输出误写成独立完成的学习经历。
