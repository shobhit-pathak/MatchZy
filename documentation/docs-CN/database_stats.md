### 比赛/玩家的数据和统计

MatchZy 默认使用 **SQLite** 数据库, 无需您手动配置. 您也可以使用 **MySQL** 数据库存储MatchZy的数据!
我们当前使用 3 张表, 分别是 `matchzy_stats_matches`, `matchzy_stats_maps`和 `matchzy_stats_players`.
 
顾名思义, `matchzy_stats_matches` 保存每场比赛的数据, 如比赛 ID, 队伍名称, 得分等.
`matchzy_stats_maps` 存储比赛中每张地图的数据.
`matchzy_stats_players` 存储参加该比赛的每个玩家的数据/统计数据.它存储比赛ID、击杀数、死亡数、助攻数和其他重要统计数据！

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
在这里, 将 `DatabaseType` 从 `SQLite` 改为 `MySQL` 然后填写所有其他详细信息，如连接IP、数据库、用户名等.
MySQL 数据库对于那些想要在多台服务器上保持数据一致性的人来说很有用!

### CSV Stats
比赛结束后，数据将从数据库中提取,并将 CSV 文件写入文件夹`csgo/MatchZy_Stats`中. 此文件夹将包含每场比赛的 CSV 文件 (文件格式: `match_data_map{mapNumber}_{matchId}.csv`) 并且它和`matchzy_stats_players`数据相同.

这里有一个改进的空间，比如在 CSV 文件中或至少在文件格式中包含比赛的分数。我很快就会做出这个改变！