# 第 6 周小组整合记录：VR UI

项目：《失联空间站：重启舱门》 / Lost Station: Restart the Hatch

小组：WANG HAOBIN（一人小组）

课程来源：[Week 6 VR UI](https://app.notion.com/p/VR-UI-3cf7c00fe5ad815c9b07de5c126580a3)

记录起始日期：2026-10-20

## 本周目标与设计

针对首次使用者“不知道先去哪、下一步做什么、何时成功”的问题，在第 5 周小组场景副本上加入英文世界空间任务面板。面板显示关卡状态，`SHOW HELP` / `HIDE HELP` 展开或收起三步操作，`RESET MISSION` 调用关卡重置。交互应通过现有 Meta Rig 的控制器射线完成，继续沿用原来的传送、抓取、空间音效和出口判定。

课程要求的场景副本路径采用 `Assets/01_Scenes/Week06_TeamUI_Wang.unity`；可复用 UI 预制件采用 `Assets/03_Prefabs/Week06_MissionConsole.prefab`。源码位于 `Assets/02_Scripts/SpaceStationMissionConsole.cs` 和 `Assets/Editor/Week06TeamUIBuilder.cs`；原有 `SpaceStationMission.cs` 对外提供任务状态。场景构建器使用 Meta Canvas Wizard 的 Ray Canvas Quick Action API，将 UI 接入现有 Rig，并检查场景只有一个 EventSystem / PointableCanvasModule。Unity 场景生成、结构检查和窗口化 Play 中的合成按钮回调已验证；真实控制器射线点击仍待验证。

## 一人小组分工

WANG HAOBIN 负责关卡、交互、UI、音效整合、场景和 APK、同学试玩安排、反馈修复、演示与提交。项目使用同一个正式小组仓库：[VR Group Project 2026 2](https://github.com/ChenMengLR/VR_Group_Project_2026_2)。没有多名组员的合并记录，也不把提供反馈的试玩同学写成开发组员。

## 当前状态与证据

- **场景副本 / UI 预制件：**`已生成，结构检查通过`。`Logs/Week06_ReadabilityRebuild.log` 记录最终面板的 `WEEK06_TEAM_UI_BUILD_OK`；场景和预制件均已落盘，`ProjectSettings/EditorBuildSettings.asset` 已启用第 6 周场景。构建器检查任务引用、世界空间 Canvas、两个按钮持久回调、PointableCanvas、一个 EventSystem / PointableCanvasModule 和零 Missing Script。
- **窗口化模拟器 Play：**`有限通过`。[运行报告](../CourseValidation/Runtime/Week06_TeamUI_PlaySmoke.json)显示 Meta XR Simulator 207 的 OpenXR 设备已激活，初始状态同步；用 `Button.onClick.Invoke()` 合成触发 Help 展开/收起和 Reset 后，按钮文字、能量核心位置及任务状态符合预期。[Game 相机画面](../CourseValidation/Runtime/Week06_TeamUI_PlaySmoke.png)显示面板在起始视野内。报告中的 `passed: true` 指这些检查通过；同时保留一条 Meta MR Utility Kit 原生日志 UTF-8 解码异常（`errorCount: 1`、`knownSdkLogDecodeExceptionCount: 1`、`unexpectedErrorCount: 0`）。这次 Play 没有真实点击控制器射线，也没有走完新版任务。
- **原有玩法保留：**`第 5 周流程已有模拟器证据；Week06 场景待完整复测`。需要在新版场景实际走通传送、抓取、插入、开门和出口，听到空间声，再验证 UI 状态跟随变化。
- **UI 图片：**[初始状态](../CourseValidation/Runtime/Week06_UI_Status_EditorPreview.png)与[展开帮助](../CourseValidation/Runtime/Week06_UI_Help_EditorPreview.png)是 Unity Editor 相机渲染；前述 Game 画面是 Meta XR Simulator 中的 Play 截图，三张均非 Quest 真机画面。还没有新版完整流程录像。
- **Quest APK / 真机：**`Android APK 已构建，Quest 尚未安装`。第 7 周 APK 使用本场景；仍须借到设备后实测控制器、画面、声音、舒适度和完整流程。旧模拟器视频不能证明新版 UI 在 Quest 可用。
- **同学首次使用测试：**`尚未测试`。用户已表示可找同学试玩；测试脚本、0–4 严重度、观察和同任务复测表见[第 6 周测试记录](Week06_UI_Peer_Test.md)。记录只在同学实际试玩后填写。
- **个人 UI 练习：**`待完成或核对`。课程的个人 XRI World Space UI 练习与这份 Meta 小组场景是不同成果，不将其混写。

## 整合检查顺序

1. 保存第 5 周小组场景副本；在副本生成并保存任务面板与预制件，不覆盖原场景。
2. 确认原有 Rig、EventSystem 和 PointableCanvasModule 只有各一个，控制器射线能瞄准并点按 `SHOW HELP` / `HIDE HELP`、`RESET MISSION`。
3. 进入 Play，确认状态依次响应抓取、供电、出口；帮助文字展开与收起；按钮的重置限制有可见提示。
4. 用 Meta XR Simulator 走通完整路径并拍摄标明模拟器的画面；再用 Quest 安装新版 APK 检查按钮、舒适度和声音。
5. 请一位同学进行约 10 分钟首次使用测试，按观察记录最高优先级问题；修改后请同一位同学完成同一任务复测。
6. 将小组仓库链接、个人和小组场景、UI 截图或视频、测试记录、贡献与剩余问题填入本组课程卡片。若没有可编辑的本组 Week 6 卡片，先留存材料并向老师确认入口。

## 剩余问题与下一周衔接

当前需要实际核验控制器射线点击、Quest 设备可读性、新场景完整流程和真实同学试玩。`RESET MISSION` 处理了原关卡的两种限制：抓住核心时显示 `RELEASE CORE / TO RESET MISSION`，松手后自动重置；已经走到舱门后时先回主舱再按重置，完成状态显示 `RETURN FOR REPLAY`。测试时要观察这些提示是否足够清楚；若同学被卡住，优先修复并记录同任务复测。第 7 周的英文论文初稿和可安装 APK 已准备，Quest 头显真实游玩视频待设备实测，不能拿模拟器录屏充作头显录像。
