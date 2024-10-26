## 这是什么?

比赛配置文件包含 MatchZy 和您的服务器举办系列赛所需的一切。这包括将玩家锁定到正确的队伍和阵营、设置地图和配置游戏规则。

**Note:** MatchZy仍可用于 pug/scrim/practice 模式,无需进行match的设置(如果将 `matchzy_kick_when_no_match_loaded` 设置为 `false`). 仅当您想要举办比赛的时候并将玩家设置在正确的队伍和阵营中时，才需要进行比赛设置。

在本文档中，我们将了解如何使用 JSON 文件在 MatchZy 中设置比赛（JSON 文件的结构如下所示）.
有 2 个命令可用于加载比赛:

1. `matchzy_loadmatch <filepath>`: 加载相对于 `csgo` 目录的 JSON 比赛配置文件.
2. `matchzy_loadmatch_url <url> [header name] [header value]`: 通过向给定的 URL 发送 HTTP(S) `GET` 来加载远程（JSON 格式）匹配配置。您可以选择使用 `header name` 和 `header value` 参数提供 HTTP 标头和值对。您应该将所有参数放在引号内 (`""`). (`""`).

## Example

!!! tip "示例仅供参考"
    
    必填字段: `"maplist"`, `"team1"`, `"team2"`和 `"num_maps"`. 如果 `"matchid"` 留空, 则由服务器自动生成.

```json title="csgo/astralis_vs_navi_27.json"
{
  "matchid": 27,
  "team1": {
    "name": "Astralis",
    "players": {
      "76561197990682262": "Xyp9x",
      "76561198010511021": "gla1ve",
      "76561197979669175": "K0nfig",
      "76561198028458803": "BlameF",
      "76561198024248129": "farlig"
    }
  },
  "team2": {
    "name": "NaVi",
    "players": {
      "76561198034202275": "s1mple",
      "76561198044045107": "electronic",
      "76561198246607476": "b1t",
      "76561198121220486": "Perfecto",
      "76561198040577200": "sdy"
    }
  },
  "num_maps": 3,
  "maplist": [
    "de_mirage",
    "de_overpass",
    "de_inferno"
  ],
  "map_sides": [
    "team1_ct",
    "team2_ct",
    "knife"
  ],
  "spectators": {
    "players": {
      "76561198264582285": "Anders Blume"
    }
  },
  "clinch_series": true,
  "players_per_team": 5,
  "cvars": {
    "hostname": "MatchZy: Astralis vs NaVi #27",
    "mp_friendlyfire": "0"
  }
}
```

可以使用以下方式加载此文件:

1. `matchzy_loadmatch astralis_vs_navi_27.json` (在你的 `csgo`目录下有 `astralis_vs_navi_27.json`这个文件)
2. `matchzy_loadmatch_url "https://<url>/astralis_vs_navi_27.json"`


## 目前的不足?

1. 无法通过此配置直接添加教练, 因此要添加教练，请将其添加到队伍的 `"players"`键中,然后在服务器中使用`.coach <teamside>` 命令将玩家置于教练位置.
2. 目前仅支持 Steam64id.

这些限制将在下次更新中尽快解决！:D
