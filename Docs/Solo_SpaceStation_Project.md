# 独立小组项目：《失联空间站：重启舱门》

负责人：WANG HAOBIN（一人小组）。建立日期：2026 年 10 月 1 日。

## 项目目标

玩家在停电的空间站舱室里，传送到维修区，抓起一个能量核心，将它放进插槽，看到指示灯变绿、听到空间音效、感到控制器振动，等待舱门打开后走到出口。任务限定为一间舱室、一个核心、一个插槽和一扇门，方便完成和演示。

## 对应课程模块

- 第 3 周：复用已实现的 Meta XR 移动、传送和控制器抓取；新场景需再次运行验证。
- 第 4 周：同一场景里“物体进入目标区域 → 开门”的核心机制。
- 第 5 周：插槽位置的空间底噪和成功提示音。
- 第 6 周：控制器抓取与完成时的短振动；B 键重置。桌面模拟器还可用 R 键重置。
- 第 7 周：完整的可试玩原型和录制素材。

## 场景与实现

- 主场景：`Assets/01_Scenes/Solo_SpaceStation.unity`，由 `Course > Build Solo Space Station` 生成；原第 3 周场景和官方 Meta 样例不受修改。
- 场景搭建：`Assets/Editor/SoloStationBuilder.cs`。保留一个 Meta Rig、设置 UI 和两处传送热点，关闭旧样例环境，建立带碰撞体的新地面和舱室。
- 任务逻辑：`Assets/02_Scripts/SpaceStationMission.cs`。必须先真实抓起核心，再在插槽附近放手，才会吸附核心、切换指示灯、播放声音并打开舱门。抵达出口显示完成；B / R 重置。
- 美术范围：Unity 基础几何体和 URP 材质；音效由运行时合成，不需要第三方资产。
- 构建场景：`ProjectSettings/EditorBuildSettings.asset` 中启用新场景作为 Android 启动场景。

## 打开与操作

1. 用 Unity `6000.3.24f1` 打开仓库，等待资源导入与编译完成。
2. 使用 `Course > Open Solo Space Station` 打开场景并启用 Meta XR Simulator，然后进入 Play。
3. 在 Meta 的移动设置菜单中选择 Teleport。瞄准维修区蓝色传送热点，传送过去。
4. 用控制器握持键抓起青色能量核心，移到桌上的橙色插槽后松手。绿色指示灯与抬升的舱门表示成功。
5. 移动至舱门后的 EXIT 地标。右控制器 B 键或桌面 R 键可重置；若已经站在门后，先穿过仍敞开的舱门返回主舱，再按重置键。

第 3 周本机模拟器绑定曾是 M 菜单、Y 摇杆向上、U 握持、T Trigger。新场景实际键位以本机 Meta XR Simulator 的 Input Bindings 和控制器设置为准。

## 团队仓库

第 4 周的团队共享仓库：[ChenMengLR/VR_Group_Project_2026_2](https://github.com/ChenMengLR/VR_Group_Project_2026_2)。本小组目前只有 WANG HAOBIN 一人；团队仓库保存项目 `Assets`、`.meta`、`Packages`、`ProjectSettings`、说明和验证记录。克隆后用 Unity `6000.3.24f1` 打开项目，等待导入，再使用 `Course > Open Solo Space Station`。`Library`、`Temp`、`Logs` 和 APK 构建产物不上传，APK 可用 `scripts/Build-SoloStationAndroid.ps1` 在具备 Android 构建模块的 Windows 机器上生成。

## 验证边界

场景生成器的结构检查会核对 Meta Rig、传送热点、抓取组件、插槽、舱门与 Missing Script。该检查不能替代 Play 操作、画面检查、Android 构建或 Quest 真机验证。当前没有 Quest 设备；任何电脑端模拟器录像须标注“Meta XR Simulator / 非 Quest 真机”。课程提交、老师审核和真机结果需按实际完成状态记录。

2026 年 10 月 1 日的本机验证：场景生成与结构检查通过，Missing Script 为 0；[编辑器预览](../CourseValidation/Runtime/SoloStation_EditorPreview.png)确认静态布局和材质。Windows 编辑器实际进入 Meta XR Simulator Play 后，[自动启动报告](../CourseValidation/Runtime/SoloStation_PlaySmoke.json)为通过，XR 运行时与加载器有效、HMD 高度约 1.70 m、能量核心保持动态物理、舱门关闭、空间底噪播放、无 Unity 错误；[运行截图](../CourseValidation/Runtime/SoloStation_PlaySmoke.png)保留了当时的视角。该启动检查本身只覆盖初始状态。

另一次连续的模拟器 Play 会话已用 Meta Operator 注入右控制器输入：传送到维修区、Grip 抓起核心、持握移动到插槽并松手，运行状态读回 `IsInserted=true`，舱门从局部 Y=1.24 抬升到 Y=3.74。随后以模拟头显的物理位移穿过门，到达出口并读回 `IsEscaped=true` 和 `MISSION COMPLETE`。[完整过程记录](../CourseValidation/Runtime/SoloStation_ControllerFlow_20261001.md)、[机器可读结果](../CourseValidation/Runtime/SoloStation_ControllerFlow_20261001.json)与[结束画面](../CourseValidation/Runtime/SoloStation_ControllerFlow_Final.png)均注明了模拟器方法及边界。这一步没有声称通过控制器移动到出口，也不代表学生本人手动演示。

Android APK 已成功构建，输出到本机被 Git 忽略的 `Builds/SoloSpaceStation.apk`，大小约 100 MiB；包名 `com.wanghaobin.loststation`。Unity 在含中文的原项目路径下首次构建失败，此后使用临时英文盘符成功构建；可在容器根目录运行 `./VR_Project_2026_2/scripts/Build-SoloStationAndroid.ps1` 重建，该脚本会在结束时移除临时盘符。也可以在英文路径中通过 Unity 的 `Course > Build Solo Space Station APK` 菜单构建。

仍需在有设备时进行 Quest 真机测试、佩戴舒适度与性能检查。电脑端截图与报告不得标记为 Quest 真机结果；课程提交与老师审核也需按实际状态记录。
