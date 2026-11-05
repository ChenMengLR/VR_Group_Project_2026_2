# 空间站探索环线 · 2026-10-29

## 设计与游玩

新场景：`Assets/01_Scenes/Station_RecoveryRoute.unity`。在既有单房间任务基础上，把核心和插槽分开，新增高架补给区和双缓坡，让移动、观察、抓取及搬运成为任务的一部分。继续复用原能量核心、插槽、分体舱门动画、任务状态、音效和控制器操作。

1. 从较长的入口通廊进入。远处可见封闭舱门，入口侧面提供 Mission Console。
2. 沿蓝色地面灯带到西侧缓坡，进入高出主地面 0.6 米的补给区。
3. 在上层台面找到核心，按住 Grip 抓起。中央反应堆遮挡直接视线，转过平台后才能完整看见取芯位置。
4. 拿到核心后返程灯带切换为琥珀色。从另一端缓坡下行，沿中央通道回到原工作台前。玩家也可按原路返回。
5. 将核心放入插槽，松开 Grip；等待供电反馈与舱门动画完成，再进入绿色出口区域。
6. 用操作面板的 RESET MISSION 或电脑 R 重新开始。新场景会同时把玩家送回入口、核心放回上层台面；抓着核心时，会先提示松手再重置。

六个传送点分别覆盖起点、西侧坡脚、上层入口、核心前方、东侧返程落点和工作台前。它们提供较舒适的替代移动方式；没有放置舱门后的传送点。墙、栏杆、平台、坡面和舱门接入 Meta 传送阻挡面。

核心掉出关卡边界后自动回到补给台。高台和坡道下部封实，避免核心滚进无法伸手取回的空隙。出口使用含高度限制的区域检测。

## 打开方式

- 在 Windows Build Profile 下，选择 `Course > Exploration Level > Open Recovery Route - Simulator`，再按 Play。
- 连接 Quest Link / Air Link 后，选择 `Course > Open Station for Quest Air Link`，再按 Play。
- 左摇杆移动；右摇杆左右 30° 转向、前推显示传送弧线、松杆传送；Grip 抓放；Trigger 点击 UI；左手菜单键打开操作面板；B 关闭面板。
- `Course > Open Week 06 Team UI` 仍可打开原提交场景。

## 验证

2026-10-29 最终结果：Editor 结构、步行与传送候选检查 **30/30 通过**；窗口化 Meta XR Simulator Play 检查 **17/17 通过**，未发现意外运行错误。运行日志另单独记录 **1 条既有 Meta MRUK 原生日志解码异常**，没有将它隐藏或计为游戏逻辑通过证据。原始本地日志位于 `Logs/StationRoute_Build_20261029.log` 与 `Logs/StationRoute_PlaySmoke_20261029.log`。

`CourseValidation/Runtime/StationRoute_Validation.json` 记录场景重载后的结构、碰撞和 SDK 传送候选检查。自动输入调用生产用 Meta locomotor，走完西坡上行、上层取芯位、东坡下行和返回工作台的路线，并复查两条坡道反向通行、关门阻挡、开门通行及出口高度限制。

传送检查调用安装版本的 `TeleportCandidateComputer.ComputeCandidate`，使用自定义抛物线采样，验证六个热点与四条区段，以及反应堆和封闭舱门阻挡。这是 SDK 几何候选验证，不代表真实手柄姿态、舒适度或使用者接受度测试。

`StationRoute_PlaySmoke.json` 单独记录窗口化 Unity / Meta XR Simulator Play 检查：使用合成 Grabbable 指针事件和绝对位置移动，运行真实生命周期、物理与舱门协程。覆盖高台核心稳定落地、抓取后指引切换、持握重置等待释放、掉出地图回收、插入供电、动画开门、出口完成、出口侧重置和中途重置。测试中的核心使用 2 秒缓入缓出的合成移动后静止释放，避免瞬移造成不真实的高速抛掷；这不代表真人携带搬运测试。

预览图是 Unity Editor 相机渲染：`StationRoute_Entrance.png`、`StationRoute_UpperBay.png`、`StationRoute_Return.png`、`StationRoute_Overview.png`。总览临时隐藏屋顶和两面外墙，便于看清路线；这些图片不是头显实机截图。

本次未进行真实 Quest 佩戴试玩、性能测量或同学可用性测试。特别需要真人确认坡道舒适度、持握搬运、控制器射线点击和文字阅读距离。

## 版本范围

本次完成本地 Unity 新关卡，不覆盖已提交的第六周场景、第七周 APK、报告、PPT 或录像，也未上传 GitHub / Notion。Build Settings 中既有第七周场景保持不变；既有 APK 不包含新关卡。后续为新关卡构建 APK 时，应明确选择新场景并在 Quest 上重新验收。

编辑器构建器 `StationRouteBuilder` 从第六周场景生成这个独立副本；再次运行 Build Recovery Route 会重建新场景，覆盖对新场景的手动编辑，因此手调后应先备份或将变化同步回构建器。验证菜单会重新载入该场景，应先保存场景。
