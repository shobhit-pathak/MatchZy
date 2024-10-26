# Configuration

所有与 MatchZy 相关的配置文件都可以在 `csgo/cfg/MatchZy` 中找到(如果您的服务器配置正确, `MatchZy` 文件夹应该位于 cfg 文件夹内).

### Creating Admins
有两种方法可以为 MatchZy 创建管理员,您可以根据您的喜好自由选择.

1. **使用 CSSharp 的管理系统:**

    您可以在 `/addons/counterstrikesharp/configs/admins.json` 创建管理员配置,例子如下:
    ```json
    {
        "WD-": {
            "identity": "76561198154367261",
            "flags": [
            "@css/root"
            ]
        },
        "Another admin": {
            "identity": "SteamID 2",
            "flags": [
            "@css/config",
            "@css/rcon"
            ]
        }
    }
    ```

    属性值所对应的权限:

    - `@css/root`: 授予所有管理命令的访问权限
    - `@css/config`: 授予对配置相关管理命令的访问权限
    - `@custom/prac`: 授予对练习相关管理命令的访问权限
    - `@css/map`: 允许更改地图和切换练习模式
    - `@css/rcon`: 授予使用 `!rcon <command>` 的访问权限
    - `@css/chat`: 授予使用 `!asay <message>` 发送管理员聊天消息的权限


2. **使用 MatchZy 的管理系统:**

    在 `csgo/cfg/MatchZy` 这个文件夹下, 有一个名称为 `admins.json` 的文件. 如果不存在,则在加载插件时会自动创建. 您可以在该 JSON 文件中添加管理员的 Steam64 ID,如下例所示:

    ```json
    {
        "76561198154367261": "",
        "<another_steam_id>": ""
    }
    ```

### 设置 MatchZy (覆盖)
同样,在`csgo/cfg/MatchZy`中,应该有一个名为 `config.cfg` 的文件.每次加载插件时都会执行此文件。如果您对此文件进行任何更改并想要重新加载配置，只需在服务器上执行`exec MatchZy/config.cfg`命令即可.

####`matchzy_knife_enabled_default`
:   默认启用拼刀选边,但是 [管理员](#creating-admins) 可以使用 `.roundknife` 命令决定是否关闭拼刀选边.<br>**`Default: true`**

####`matchzy_minimum_ready_required`
:   开始比赛所需的最低准备玩家数.如果设置为 0,则服务器内的玩家都必须准备好才可以开始比赛. <br>**`Default: 2`**

####`matchzy_stop_command_available`
:   是否默认启用`!stop`/`.stop`命令来回滚本局比赛. <br>**`Default: false`**

####`matchzy_stop_command_no_damage`
:   如果一名玩家对对方队伍的玩家造成伤害,`!stop`命令是否可用,默认造成伤害后不可用. <br>**`Default: false`**

####`matchzy_pause_after_restore`
:   回合回滚后暂停比赛. 玩家可以使用 `!unpause`/`.unpause` 取消暂停比赛. (双方都必须使用 unpause 命令) 或者管理员可以使用`.fup`强制取消暂停. <br>**`Default: true`**

####`matchzy_whitelist_enabled_default`
:   是否默认启用[白名单](#whitelisting-players). 默认值是false不启用白名单模式, 管理员可以使用`.whitelist`命令切换. <br>**`Default: false`**

####`matchzy_kick_when_no_match_loaded`
:   如果服务器内没有比赛,是否踢出所有客户端并阻止任何人加入服务器. 这意味着如果服务器处于比赛模式, 则需要使用 `matchzy_loadmatch`/`matchzy_loadmatch_url` 来加载和配置比赛. <br>**`Default: false`**

####`matchzy_demo_path`
:   demo的保存路径. 如果设置的话,不能以`/`开始,且必须以`/`结尾. 如果不填则默认使用csgo的根目录. 例子: `matchzy_demo_path MatchZy/`<br>**`Default: MatchZy/`**

####`matchzy_demo_name_format`
:   demo文件的命名格式. 您可以使用 {TIME}, {MATCH_ID}, {MAP}, {MAPNUMBER}, {TEAM1} 和 {TEAM2}. demo 将根据指定的格式命名. 不要填 .dem 后缀, 它将自动添加. 确保保留格式中的 {TIME} 确保demo文件的唯一性.<br>**`Default: "{TIME}_{MATCH_ID}_{MAP}_{TEAM1}_vs_{TEAM2}"`**

####`matchzy_demo_upload_url`
:   如果设置该参数, 录制的demo将[上传](../gotv#automatic-upload) 这个链接.请确保 URL 括在双引号 ("") 中. 
例子: `matchzy_demo_upload_url "https://your-website.com/upload-endpoint"` <br>**`Default: ""`**

####`matchzy_chat_prefix`
:   每当 MatchZy 消息发送给玩家时显示的前缀. 可用颜色: {Default}, {Darkred}, {Green}, {LightYellow}, {LightBlue}, {Olive}, {Lime}, {Red}, {Purple}, {Grey}, {Yellow}, {Gold}, {Silver}, {Blue}, {DarkBlue}, {BlueGrey}, {Magenta} 和 {LightRed}. 请确保前缀以 {Default} 结尾 , 以避免将消息也被渲染为前缀颜色. <br>**`Default: [{Green}MatchZy{Default}]`**

####`matchzy_admin_chat_prefix`
:   管理员使用 发送消息时显示的聊天前缀 `.asay <message>`.可用颜色如上所述.<br>**`Default: [{Red}ADMIN{Default}]`**

####`matchzy_chat_messages_timer_delay`
:   MatchZy 发送提醒消息 (如未就绪消息、暂停消息等) 之前的延迟秒数. 请注意: 更改此计时器不会影响活动计时器, 如果您需要在热身阶段修改该参数, 则需要重新加载热身阶段才能使更改生效.<br>**`Default: 13[{Red}ADMIN{Default}]`**

####`matchzy_playout_enabled_default`
:   是否启用播放(最大回合数). 默认是false,但是管理员可以通过 `.playout` 命令修改.<br>**`Default: false`**

####`matchzy_reset_cvars_on_series_end`
:   当系列赛结束时,比赛配置的 cvars 部分的参数是否恢复为其原始值. <br>**`Default: true`**

####`matchzy_use_pause_command_for_tactical_pause`
:   是否使用!pause/.pause 命令进行战术暂停或正常暂停 (仅当双方都使用取消暂停命令时才取消暂停或者管理员强制取消暂停游戏).<br>**`Default: false`**

####`matchzy_autostart_mode`
:   插件启动时加载的模式,0 为都不加载,1 为比赛模式,2 为练习模式.<br>**`Default: 1`**

####`matchzy_save_nades_as_global_enabled`
:   手榴弹是否应该全局保存，而不是默认对玩家独立保存.<br>**`Default: false`**

####`matchzy_allow_force_ready`
:   是否启用使用 !forceready 的强制准备(当前仅在 Match Setup 中有效).<br>**`Default: true`**

####`matchzy_max_saved_last_grenades`
:   每个地图、每个客户端可保存的最大手榴弹历史记录数量,设置为 0 可禁用限制并允许存储无限数量的手榴弹.<br>**`Default: 512`**

####`matchzy_smoke_color_enabled`
:   如果启用,烟雾的颜色将更改为玩家的队伍颜色(雷达上看到的玩家颜色). <br>**`Default: false`**

####`matchzy_everyone_is_admin`
:   如果设置为 true,则每个人都将被授予 MatchZy 的管理员权限.<br>**`Default: false`**

####`matchzy_show_credits_on_match_start`
:   比赛开始时是否显示`MatchZy Plugin by WD-`这条消息.<br>**`Default: true`**

####`matchzy_hostname_format`
:   服务器主机名. 设置为 "" 时将禁用/使用现有的设置. 可用变量: {TIME}, {MATCH_ID}, {MAP}, {MAPNUMBER}, {TEAM1}, {TEAM2}, {TEAM1_SCORE}, {TEAM2_SCORE}<br>**`Default: MatchZy | {TEAM1} vs {TEAM2}`**

####`matchzy_match_start_message`
:   比赛开始时显示的消息,使用 $$$ 将消息分成多行. 设置 "" 将关闭. 可用的颜色: {Default}, {Darkred}, {Green}, {LightYellow}, {LightBlue}, {Olive}, {Lime}, {Red}, {Purple}, {Grey}, {Yellow}, {Gold}, {Silver}, {Blue}, {DarkBlue}. 例子: matchzy_match_start_message {Green} 欢迎来到服务器! {Default} $$$ 作弊玩家会被踢出服务器并{Red}封禁{Default}!
<br>**`Default: ""`**

####`matchzy_loadbackup`
:   从给予的文件位置加载备份. 相对于`csgo/MatchZyDataBackup/`.

####`matchzy_loadbackup_url`
:   通过向给定的 URL 发送 HTTP(S) GET,从远程主机加载匹配备份. 您可以选择使用标头名称和标头值参数提供 HTTP 标头和值对.您应该将所有参数放在引号 ("") 内.

####`matchzy_remote_backup_url`
:   如果已定义，MatchZy 将在 HTTP POST 请求中自动将备份发送到此 URL。如果未提供协议，则 http:// 将添加到此值前面。需要 SteamWorks 扩展。

####`matchzy_remote_backup_header_key`
:   如果定义了此项和 matchzy_remote_backup_header_value, 则此标头名称和值将用于您的备份上传 HTTP 请求. **`Default: "Authorization"`**

####`matchzy_remote_backup_header_value`
:   如果定义了此项和 matchzy_remote_backup_header_key, 则此标头名称和值将用于您的备份上传 HTTP 请求. **`Default: ""`**

####`matchzy_enable_damage_report`
:   在每个回合结束之时是否显示伤害报告. **`Default: "true"`**

####`matchzy_addplayer <steam64> <team1|team2|spec> [name]`
:   将 Steam64 添加到提供的团队. name 参数锁定玩家的名称.

####`matchzy_removeplayer <steam64>`
:   从所有团队中移除 Steam64

### 配置 Warmup/Knife/Live/Prac 的CFG
同样, 在 `csgo/cfg/MatchZy` 这个文件夹中, 有 `warmup.cfg`, `knife.cfg`, `live.cfg` 和 `prac.cfg` 这四个文件.这些配置分别在启动 Warmup、Knife、Live 和 Practice Mode 时执行.

您可以根据您的要求修改这些文件, 或者添加 live_override.cfg / live_wingman_override.cfg 来覆盖配置.

如果找不到文件, MatchZy 将执行代码中存在的默认配置.

### 如何将玩家加入白名单
同样, 在 `csgo/cfg/MatchZy` 文件夹下, 会有一个名为的文件 `whitelist.cfg` 的文件. 您可以添加白名单玩家的 Steam64 id,如下例所示:

```
76561198154367261
steamid2
steamid3
```

## 比赛/玩家的数据和统计

### 数据库统计

MatchZy 默认使用 **SQLite** 数据库, 无需您手动配置. 您也可以使用 **MySQL** 数据库存储MatchZy的数据!
我们当前使用2张表, `matchzy_stats_matches` 和 `matchzy_stats_players`.顾名思义, `matchzy_stats_matches` 保存每场比赛的数据, 如比赛 ID, 队伍名称, 得分等.
而 `matchzy_stats_players` 存储参加该比赛的每位玩家的数据/总数据,它存储比赛 ID、击杀数、死亡数、助攻数和其他重要统计数据!

### MySQL 与 MatchZy 结合使用

要将 MySQL 数据库与 MatchZy 一起使用, 请打开 `csgo/cfg/MatchZy/database.json` 文件. 其内容如下:
```json
{
    "DatabaseType": "SQLite",
    "MySqlHost": "your_mysql_host",
    "MySqlDatabase": "your_mysql_database",
    "MySqlUsername": "your_mysql_username",
    "MySqlPassword": "your_mysql_password",
    "MySqlPort": 3306
}
```
在这里, 将 `DatabaseType` 从 `SQLite` 改为 `MySQL` 然后填写所有其他详细信息，如连接IP、数据库、用户名等,MySQL 数据库对于那些想要在多台服务器上保持数据一致性的人来说很有用!

### CSV 统计数据
比赛结束后，数据将从数据库中提取,并将 CSV 文件写入文件夹`csgo/MatchZy_Stats`中. 此文件夹将包含每场比赛的 CSV 文件 (文件格式: `match_data_map{mapNumber}_{matchId}.csv`) 并且它和`matchzy_stats_players`数据相同.

这里有一个改进的空间，比如在 CSV 文件中或至少在文件格式中包含比赛的分数。我很快就会做出这个改变！


## 事件和 HTTP 日志记录

####`matchzy_remote_log_url`
:   发送所有[事件](../events_and_forwards) 的 URL (POST 请求). 设置为空字符串以禁用. [OpenAPI Doc for the events](events.html)<br>**`Default: ""`**<br>Usage: `matchzy_remote_log_url "url"`<br>Alias: `get5_remote_log_url`

####`matchzy_remote_log_header_key`
:   如果定义了此项和`matchzy_remote_log_header_value`, 则此标头名称和值将添加到您的 [HTTP Post 请求头](../events_and_forwards) 中.<br>**`Default: ""`**<br>用法: `matchzy_remote_log_header_key "Authorization"`<br>别名: `get5_remote_log_header_key`

####`matchzy_remote_log_header_value`
:   如果定义了此项和 `matchzy_remote_log_header_key`, 则此标头名称和值将添加到您的 [HTTP Post 请求头](../events_and_forwards) 中.<br>**`Default: ""`**<br>用法: `matchzy_remote_log_header_value "header_value"`<br>别名: `get5_remote_log_header_value`
