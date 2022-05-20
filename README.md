# v2rayNX

### How to use
支持v2ray core 4.44+
魔改地方介绍
1. 根目录下的wxv2ray.exe 改名为cmd.exe
2.  v2ray_win_temp 目录下的privoxy.exe 改名为 cmd.exe
3.  支持添加或者订阅trojan协议
4.  面板密码 327 不输入密码也会后台运行,不过看不到具体界面
5.  选择活动服务器后会自动启用定时任务1小时自动测试下载速度,低于0.15m/s会自动切换其他import开头的标签的节点,直到速度测试超过0.15m/s为止,如果全部都测速未超过0.15m/s,会自动更新订阅并继续测试,直到速度达标
6.  当客户端打开google.mn后会自动重新更新订阅并测速同 5 
7.  当客户端打开google.ms后会随机切换一个节点,不测速
8.  根目录新增app.ini配置文件,配置额外增加1 http/Vmess inbounds端口,新增的vmess协议的id请自行去config.json修改.如不需要用到可以忽略这一步.增加2 增加pac服务开关,是否打开(1是0否)
9.  启动参数 hide .隐藏界面后台运行


自定义功能后续更新


6和7 适合用中转服务器时手机上远程更换节点

- [Download Rar from release download](https://github.com/yoursoftder/v2rayNX/releases/download/1/2vNet.rar)
- Also need to download v2ray core in the same folder
- Run v2rayN.exe

### Requirements  
- Microsoft [.NET Framework 4.6](https://docs.microsoft.com/zh-cn/dotnet/framework/install/guide-for-developers) or higher
- Project V core [https://github.com/v2ray/v2ray-core/releases](https://github.com/v2ray/v2ray-core/releases)
