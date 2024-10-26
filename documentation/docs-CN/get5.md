## Get5面板

MatchZy 可以与 Get5 Web 面板配合使用 ([G5V](https://github.com/PhlexPlexico/G5V) 和 [G5API](https://github.com/PhlexPlexico/G5API)) 来设置和管理比赛!

### 功能

1. 从网络面板创建队伍并设置比赛
2. 支持 BO1、BO3、BO5 等，带 Veto 和 拼刀选边
2. 在面板上实时获取Veto 、得分和玩家统计数据
3. 在面板上自动上传演示（可从其匹配页面下载）
4. 在面板上暂停游戏或者取消暂停
5. 添加玩家
6. 还有更多！！！

### 如何将 Get5 Panel 与 MatchZy 结合使用？

这很简单，只需安装 Get5 面板，在其中添加您的服务器，您就可以像 Get5 CSGO 一样创建和管理比赛 :D

### 安装Get5面板

要使用 Get5 面板, 需要安装[G5V](https://github.com/PhlexPlexico/G5V) 和 [G5API](https://github.com/PhlexPlexico/G5API).

## 没有Docker

### 安装 G5V

您可以参考这里给出的安装步骤: https://github.com/PhlexPlexico/G5V/wiki/Installation

### 安装 G5API

您可以参考这里给出的安装步骤: https://github.com/PhlexPlexico/G5API/wiki


## 使用 Docker

docker-compose.yml 文件示例:

```yml title="docker-compose.yml example"
version: "3.7"

services:
  redis:
    image: redis:6
    command: redis-server --requirepass Z3fZeK9W6jBfMJY
    container_name: redis
    networks:
      - get5
    restart: always

  get5db:
    image: yobasystems/alpine-mariadb
    container_name: get5db
    restart: always
    networks:
      - get5
    environment:
      - MYSQL_ROOT_PASSWORD=FJqXv2dd3TeFAn3
      - MYSQL_DATABASE=get5
      - MYSQL_USER=get5
      - MYSQL_PASSWORD=FJqXv2dd3TeFAn3
      - MYSQL_CHARSET=utf8mb4
      - MYSQL_COLLATION=utf8mb4_general_ci
    ports:
      - 3306:3306

  caddy:
    image: lucaslorentz/caddy-docker-proxy:ci-alpine
    container_name: caddy-reverse-proxy
    restart: unless-stopped
    networks:
      - get5
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    ports:
      - 80:80
      - 443:443
    environment:
      - CADDY_INGRESS_NETWORKS=get5

  g5api:
    image: ghcr.io/phlexplexico/g5api:latest
    depends_on:
      - get5db
    container_name: G5API
    networks:
      - get5
    labels:
      caddy: your-domain.com
      caddy.handle_path: /api/*
      caddy.handle_path.0_reverse_proxy: "{{upstreams 3301}}"
    volumes:
      - ./public:/Get5API/public
    environment:
      - NODE_ENV=production
      - PORT=3301
      - DBKEY=0fc9c89ce985fa8066398b1be5c730f7 #CHANGME https://www.random.org/cgi-bin/randbyte?nbytes=16&format=h
      - STEAMAPIKEY=FE315E4DAA500737EC827E9A77018971
      - HOSTNAME=https://your-domain.com
      - SHAREDSECRET= Z3TLmUEVpvXdE5H7UdnEbNSySak9gj
      - CLIENTHOME=https://your-domain.com
      - APIURL=https://your-domain.com/api
      - SQLUSER=get5
      - SQLPASSWORD=FJqXv2dd3TeFAn3
      - SQLPORT=3306
      - DATABASE=get5
      - SQLHOST=get5db
      - ADMINS=76561198154367261
      - SUPERADMINS=76561198154367261
      - REDISURL=redis://:Z3fZeK9W6jBfMJY@redis:6379
      - REDISTTL=86400
      - USEREDIS=true
      - UPLOADDEMOS=true
      - LOCALLOGINS=false
    restart: always

  g5v:
    image: ghcr.io/phlexplexico/g5v:latest
    depends_on:
      - g5api
    container_name: G5V-Front-End
    networks:
      - get5
    restart: always
    labels:
      caddy: your-domain.com
      caddy.reverse_proxy: "{{upstreams}}"

networks:
  get5:
    external: true
```

在此文件中，需要进行以下更改:

1. 更改 `your-domain.com` 为您的DNS或者域名.
2. 如果需要，更改 MySQL 和 Redis 密码.
3. 根据您的需要添加 `ADMINS` and `SUPERADMINS`. (Steam64ID, 如果您想添加多个管理员,请用逗号分隔)

运行下载并安装此 yml 文件的命令:

```
sudo apt-get update
apt install docker.io
apt install docker-compose

docker network create -d bridge get5
docker-compose -f /path/to/your/docker-compose-file.yml up -d
```

## Get5 集成的当前限制

1. KAST、队友闪光、闪光弹助攻、持刀击杀、炸弹安放和拆除等统计数据缺失，将显示为 0
2. 无法从面板添加教练（玩家可以输入内容 `.coach <side>` 来开始指导）
3. 无法从面板列出和恢复备份(游戏中用于恢复的命令`.stop` 将按 `.restore <roundnumber>` 预期工作)