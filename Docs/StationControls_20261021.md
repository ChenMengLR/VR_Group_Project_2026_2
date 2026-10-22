# 空间站移动与手柄操作更新

日期：2026-10-21。适用：`Assets/01_Scenes/Week06_TeamUI_Wang.unity` 当前小组场景。

## 如何操作

- **左摇杆**：前进、后退、左右平移。行走方向根据头部朝向在地面的投影计算；正常满幅速度约 1.5 米/秒。松开摇杆会快速减速停止，斜向移动不额外加速。
- **右摇杆向左/右拨动**：向对应方向转 30°。持续拨住只转一次，回中再拨才继续。
- **右摇杆向前推住**：用右控制器瞄准有效传送平台；出现可用目标后松开摇杆传送。按下右摇杆可以取消。瞄准时暂时停止行走和转向。
- **中指侧握持键 Grip**：按住抓起能量核心，保持按住携带；在插槽中松开让核心插入。
- **食指扳机 Trigger**：用控制器射线指向界面按钮并点按。
- **左手 ≡ 菜单键**：在面前打开/关闭英文操作面板；其中有 RESUME 和 RESET MISSION。
- **右手 B**：关闭操作面板，游戏过程中不会直接重置任务。
- **A / X / Y**：没有新增任务动作。右手 Meta 系统键仍用于系统界面。
- **电脑键盘 R**：保留编辑器练习重置快捷键。

从菜单返回游戏、重新戴上头显或恢复输入焦点后，先松开两个摇杆让其回中，再开始移动。

### 完整任务

左摇杆走到维修台（或右摇杆传送到蓝色平台）→ Grip 抓起 E-07 核心 → 放入配套插槽并松手 → 等待夹紧、解锁和双门打开 → 用左摇杆穿过舱门到达出口。

重置时，如果拿着核心，先松手；如果已走到舱门另一侧，先返回主舱。菜单和墙面控制台会显示相应提示。

## 在哪里打开

Air Link：先在 Quest 中连接电脑，再打开 Unity 项目，活动 Build Profile 使用 Windows，选择 `Course > Open Station for Quest Air Link`，然后按 Play。此入口停用模拟器并打开当前团队场景；电脑 Meta Quest Link 应作为活动 OpenXR 运行时。

电脑模拟器：选择 `Course > Open Week 06 Team UI`。Simulator 的键鼠映射用于模拟左右 Touch 控制器，以上左右手柄布局保持一致；第 3 周原示例场景仍保留自己的 Locomotion Settings。

## 改动内容

1. 新增 `SpaceStationControls`，使用原 Meta `FirstPersonLocomotor` 和 `CharacterController`，通过 LocomotionEvent 发出输入；不直接平移 Rig，保留地面、碰撞、重力与头部支点转向。
2. 修复当前场景 CharacterController 碰撞层从 6 到 7，包含 Default 层的空间站地板、墙、桌面和舱门。旧 Air Link 日志曾出现 ground-not-found 导致移动禁用。
3. 关闭两侧旧 Slide/Turn/Jump/Crouch/Run 输入和设置覆盖，保留双手抓取、射线，以及右手完整官方传送结构。
4. 新增 Meta Ray Canvas 操作面板和按钮，替换当前小组场景的 SDK 示例菜单。操作面板打开期间暂停摇杆移动；失去输入焦点也停止水平速度，保留重力。
5. 对无 `SpaceStationControls` 的旧第 5 周场景保留 B 重置，避免历史练习的操作说明失效；当前第 6 周团队场景以 B 关闭菜单。
6. `Week06TeamUIBuilder` 重建场景时自动应用新操作与菜单；APK 构建版本更新为 0.4.0 / versionCode 3。

## 已完成的验证

- [确定性检查结果](../CourseValidation/Runtime/StationControls_Validation.json)：28 项通过，运行生产代码和实际保存场景中的碰撞体。
- 60 Hz 和 120 Hz 的两秒前进距离分别约 2.936 米与 2.946 米；验证前后左右、斜向限速、抬头时水平移动、摇杆回中、单次转向和头部支点。
- 实际地板、墙壁、工作台和舱门参与碰撞检查。关门时挡住玩家，完整打开后可通过，重置后重新阻挡；行走输入不会取消重力。
- [菜单预览](../CourseValidation/Runtime/StationControls_Menu_EditorPreview.png)：Unity Editor 相机渲染，已检查排版，不是 Quest 截图。

- [实际 Play 检查结果](../CourseValidation/Runtime/StationControls_PlaySmoke.json)：窗口化 Meta XR Simulator 中 6 项通过。真实 Start 能找到地板，合成左摇杆输入推动角色，菜单阻止移动，关闭并回中后恢复传送。运行过程中 0 条地面缺失警告、0 条其他错误；单独记录 1 条已有 Meta MRUK 原生日志解码异常。此检查没有录制视频，未使用真实手柄点击。

## 新版安装包

- 文件：`Deliverables/Week07/Wang_VR_Week07.apk`，106,982,987 字节。
- Android `aapt` 已核对：`com.wanghaobin.loststation`，版本 **0.4.0**，versionCode **3**，ARM64，minSdk 32 / targetSdk 34。
- SHA256：`B7CD05D4F7B81864A3DC31EE9D7D39E2B68AF4B34043693FB8545D1EAA51FA3D`。
- 构建脚本正常退出，Builds 与 Deliverables 两份文件的 SHA256 一致。首次构建成功后曾遇到 Unity 插件后台任务导致退出超时；构建入口已显式保存并退出，第二次构建以退出码 0 完成。
- 此更新保存为本地项目和本地 APK；此前仓库中的 0.3.0 是旧操作版本。设备安装仍需使用这份 0.4.0 包。

## 尚未验收的范围

2026-10-20 的 Quest 2 Air Link 连接已由用户确认能进入空间站并随头部转动。之后用户在 2026-10-21 通过 USB Link 完成实际头显和控制器游玩，保存了[54 秒连续 PC VR 录像](../Deliverables/Week07/Wang_VR_Week07_PCLink_Demo.mp4)，可见 Reset、Help、移动、核心抓取与重新摆放、插入供电、双门动画和出口完成。用户确认听到游戏声音。详见[录像证据与限制](../CourseValidation/Runtime/Quest_PCLink_Demo_20261021.md)。

这段录像不代表所有按键和传送手势均已验收，也不证明精确转角、振动感知或持续行走舒适度。原生 APK 安装、独立运行、头显 Camera 录制及同学首次使用测试仍待完成。此处状态于 2026-10-22 按现有证据更新。
