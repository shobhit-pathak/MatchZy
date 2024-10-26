MatchZy - CS2的满十插件!
==============

MatchZy 是 CS2的一个插件，用于运行和管理 practice/pugs/scrims/matches 的CS2 服务器.

[![Discord](https://discordapp.com/api/guilds/1169549878490304574/widget.png?style=banner2)](https://discord.gg/2zvhy9m7qg)


## MatchZy可以做什么?
MatchZy 可以解决很多比赛管理需求. 它提供了`!ready`, `!unready`, `!pause`, `!unpause`, `!tac`, `!stop`等基本命令，还提供比赛统计数据等等!

**功能亮点:**

* 操纵简单易上手
* 支持[Get5 Panel!](./get5.md)
* 使用 Match 模式或使用Get5面板时支持 BO1/BO3/BO5 和 Veto!
* [设置比赛](./match_setup) 并将玩家锁定到他们的队伍中
* 练习模式,使用 `.bot`, `.spawn`, `.ctspawn`, `.tspawn`, `.nobots`, `.rethrow`, `.last`, `.timer`, `.clear`, `.exitprac` 和更多命令！
* 拼刀选边! (按预期逻辑，即玩家最多的队伍获胜。如果玩家数量相同，则 HP 优势队伍获胜。如果 HP 相同，则随机决定获胜者)
* 自动录制Demo (确保 tv_enable 1)
* 比赛结束时自动上传Demo录像到指定的URL.
* 玩家白名单 (感谢 [DEAFPS](https://github.com/DEAFPS)!)
* 教练系统
* 每回合的伤害统计报告
* 支持比赛回滚 (目前使用 vanilla valve 的备份系统)
* 能够创建管理员并允许他们访问管理员命令
* 数据统计和CSV统计! MatchZy 将所有比赛的数据和统计信息存储在本地 SQLite 数据库中（也支持 MySQL 数据库！），还会创建一个 CSV 文件来记录该比赛中每个玩家的详细统计信息！
* 提供简单的配置
* 还有更多！