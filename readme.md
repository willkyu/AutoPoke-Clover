# AutoPoke: Clover

宝可梦三代自动化刷闪工具，支持GB Operator、GBA模拟器、NS端火红叶绿。

> 交流群985084478

## 支持的内容

注意，点击窗口下面的版本（一开始应该是RS）切换到对应版本。

- 初始御三家，使用Stationary中的RSEStarters或FrLgStarters
- 各种简单定点，如卡比兽、超梦等，一般使用Stationary中的NormalHitA（一直按A直到进入战斗）（可选择是否指定方向移动一格，用于宝石的封面神等）
- 各种路闪，支持自动续喷雾（可能有bug），甜甜香气等。使用Move
- 各种礼物宝可梦，如伊布、买的鲤鱼王，使用Stationary中的Gift
- 钓鱼，使用Fish


- 简单的计数功能
- 出闪自动截图
- 按键绑定（NS端无需设置）
- 修改目标窗口名称（模拟器建议使用 mGBA）
- 支持Win系统通知、邮件通知
- 对接 OBS WebSocket，可以使用 OBS 自动录制出闪的视频

## NS端的使用方式

初始准备：

- 单片机接电脑与ns，切换到伊机控联机模式
- ns通过采集卡连接电脑
- 电脑上打开AutoPoke和PotPlayer或Obs

1. PotPlayer/Obs打开视频采集设备，播放ns画面。
2. 调整PotPlayer/Obs的布局等，使得整个窗口中游戏画面占80%以上
3. AutoPoke的设置中设置窗口名称为PotPlayer或者Obs（大小写要注意）（还没测试Obs应该填什么，可以试试OBS、obs和Obs），之后点击左上角的四叶草刷新窗口，确认当前窗口数量应该为1.
4. 点击AutoPoke左上角四叶草旁边的EZCon图标，灰色是不使用ns模式，红色是未找到单片机（此时要确认初始准备都对的），正常颜色就是ok
5. AutoPoke选择你要刷的内容（由于ns暂时只有火叶，所以最下面的RS要点击以切换到FrLg）
6. 游戏内站到正确的位置（例如火叶初始是在精灵球前），按下Start开始

## TODO

- [x] Finish the framwork.
- [ ] Replace `win32api` with `Window Graphics Capture` for capturing window image.
- [ ] More functions.
