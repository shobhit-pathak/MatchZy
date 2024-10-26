# 使用命令
大部分命令可以使用 . 或 ! 作为前缀 (例如：`.ready` 等效于 `!ready`)

- `.ready` 表示玩家准备就绪 (别名: `.r`)
- `.unready` 表示玩家未准备就绪 (别名: `.ur`, `.notready`)
- `.forceready` 强制准备 (仅在使用 JSON/Get5 设置比赛时有效)
- `.pause` 暂停比赛 (战术或普通暂停，取决于`matchzy_use_pause_command_for_tactical_pause`).
- `.tech` 技术暂停.
- `.tac` 战术暂停
- `.unpause` 申请解除暂停。双方队伍都需输入 `.unpause` 才能解除暂停
- `.stay` 保持在当前阵营 (刀战获胜者在刀战结束后使用)
- `.switch`/`.swap` 切换阵营 (刀战获胜者在刀战结束后使用)
- `.stop` 恢复当前回合的备份 (双方队伍都需输入 `.stop` 才能恢复)
- `.coach <side>` 教练模式,并指定执教阵营,例如: `.coach t`开启 T 阵营的教练模式
- `.uncoach` 离开教练位置

# 练习模式

- `.spawn <number>` 选择当前所在阵营的出生点位置
- `.ctspawn <number>` 选择 CT 阵营出生点位置 (别名: `.cts`)
- `.tspawn <number>` 选择 T 阵营出生点位置 (别名: `.ts`)
- `.bestspawn` 传送到距离当前位置最近的所在阵营出生点
- `.worstspawn` 传送到距离当前位置最远的所在阵营出生点
- `.bestctspawn` 传送到距离当前位置最近的 CT 阵营出生点
- `.worstctspawn` 传送到距离当前位置最远的 CT 阵营出生点
- `.besttspawn` 传送到距离当前位置最近的 T 阵营出生点
- `.worsttspawn` 传送到距离当前位置最远的 T 阵营出生点
- `.showspawns` 高亮显示所有出生点
- `.hidespawns` 关闭高亮显示所有出生点
- `.bot` 在当前位置添加一个bot
- `.crouchbot` 在当前位置添加一个蹲下的bot (别名: `.cbot`)
- `.boost` 在当前位置添加一个bot并让玩家站在其上
- `.crouchboost` 在当前位置添加一个蹲下的bot并让玩家站在其上
- `.ct`, `.t`, `.spec` 切换玩家至指定的队伍
- `.fas` / `.watchme` 强制所有玩家进入观察者模式，只有调用此命令的玩家除外
- `.nobots` 移除所有bots
- `.clear` 清除地图上所有的烟雾、燃烧瓶
- `.fastforward` 将服务器时间快进 20 秒 (别名: `.ff`)
- `.noflash`/`.noblind` 开启闪光弹免疫模式 (但是仍会闪瞎他人)
- `.dryrun` 开始干跑模式 (别名: `.dry`)
- `.god` 开启上帝模式
- `.savenade <name> <optional description>` 保存道具点 (别名: `.sn`)
- `.loadnade <name>` 加载道具点 (别名: `.ln`)
- `.deletenade <name>` 删除指定的道具点 (别名: `.dn`)
- `.importnade <code>` 导入道具点代码 (别名: `.in`)
- `.listnades <optional filter>` 列出所有的道具点,也可添加过滤参数进行过滤 (别名: `.lin`)
- `.break` 破坏所有可以破坏的实体 (玻璃窗、木门、通风口等)
- `.timer` 启动计时器,并在您再次输入`.timer`后停止计时,并告诉您所用时间. 
- `.last` 传送回扔出最后扔出道具的位置
- `.back <number>` 传送回历史扔出道具的位置
- `.delay <delay_in_seconds>` 在使用`.rethrow`或`.throwindex`时,设置投掷道具的延迟.
- `.rethrow` 重新投掷您最后丢出的道具 (别名: `.rt`)
- `.throwindex <index> <optional index> <optional index>` 根据道具投掷历史,重新投掷道具(练习爆弹可用). 例子: `.throwindex 1 2` 将投掷第1个和第2个道具. `.throwindex 4 5 8 9` 投掷第4个、第5个、第8个和第9个道具(如果您在手榴弹中添加了延迟，它们将按照特定的延迟投掷).
- `.lastindex` 获取最后丢出道具的索引号.
- `.rethrowsmoke` 重投您丢出的最后1个烟雾弹.
- `.rethrownade` 重投您丢出的最后1个手雷.
- `.rethrowflash`重投您丢出的最后1个闪光.
- `.rethrowmolotov` 重投您丢出的最后1个燃烧瓶.
- `.rethrowdecoy` 重投您丢出的最后1个诱饵弹.
- `.solid` 切换 mp_solid_teammates
- `.impacts` 切换 sv_showimpacts
- `.traj` 切换 sv_grenade_trajectory_prac_pipreview

# Admin Commands

- `.start` 强制开始比赛。
- `.restart` 强制重新开始/重置比赛. (别名: `.endmatch`, `.forceend`)
- `.forcepause` 以管理员身份暂停比赛(玩家无法取消管理员暂停的比赛). (别名: `.fp`)
- `.forceunpause` 强制取消比赛暂停. (别名: `.fup`)
- `.restore <round>` 回滚到指定的回合.
- `.skipveto` / `.sv` 跳过当前否决阶段.
- `.roundknife` / `.rk` 选择是否开启拼刀选边.如果禁用，将直接从热身阶段进入比赛阶段.
- `.playout` 切换播放（如果启用播放，则所有回合都会进行，无论获胜者是谁。在训练赛中很有用！）
- `.whitelist` 切换玩家白名单。要将玩家列入白名单，请在 `cfg/MatchZy/whitelist.cfg`设置
- `.readyrequired <number>` 设置开始比赛所需的准备就绪玩家数量。如果设置为 0，则所有连接的玩家都必须准备好开始比赛。
- `.settings` 显示当前设置，例如是否启用刀、readyrequired 玩家的值等。
- `.map <mapname>` 改变地图
- `.asay <message>` 在所有聊天中以管理员身份发言
- `.reload_admins` 热加载管理员配置文件`admins.json`
- `.team1 <name>` 设置队伍 1 的名称（默认为 CT）
- `.team2 <name>` 设置第二队的名称（默认为 T）
- `.prac` 启动练习模式(别名: `.tactics`)
- `.exitprac` 退出练习模式并加载比赛模式.
- `.rcon <command>` 发送命令到服务器
