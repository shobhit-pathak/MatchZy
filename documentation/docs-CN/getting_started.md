默认情况下，插件将以 `pug` 模式启动, 这意味着团队不会被锁定. 玩家和管理员可以使用 [可用命令](../commands)
以下是插件的可用模式:

1. **Pug mode** (默认模式)
2. **Practice mode** - 管理员可以使用 `.prac` 命令启动该模式
3. **Scrim mode** - 管理员可以使用命令切换`.playout`.它将启用所有回合的播放。
4. **Match mode** - 可以通过提供 [match json](../match_setup)设置

以下是一些可用于初始配置的基本管理命令:

- `.start` 强制开始比赛. 
- `.restart` 强制重新开始/重置比赛.
- `.readyrequired <number>` 置开始比赛所需的准备就绪玩家数量.如果设置为 0,则所有连接的玩家都必须准备好开始比赛.
- `.roundknife` 是否开启拼刀选边,如果禁用,比赛将直接从热身阶段进入正赛阶段.
- `.map <mapname>` 改变地图
