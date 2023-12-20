## Get5 Panel

MatchZy can work with Get5 Web panel ([G5V](https://github.com/PhlexPlexico/G5V) and [G5API](https://github.com/PhlexPlexico/G5API)) to setup and manage matches!

### Features

1. Create teams and setup matches from web panel
2. Support for BO1, BO3, BO5, etc with Veto and Knife Round
2. Get veto, scores and player stats live on the panel
3. Get demo uploaded automatically on the panel (which can be downloaded from its match page)
4. Pause and unpause game from the panel
5. Add players in a live game
6. And much more!!!

### How to use Get5 Panel with MatchZy?

It's pretty simple, just install Get5 panel, add your server in it and you will be able to create and manage matches just like Get5 CSGO :D

### Installing Get5 Panel

To use Get5 panel, [G5V](https://github.com/PhlexPlexico/G5V) and [G5API](https://github.com/PhlexPlexico/G5API) are required

## Without Docker

### Install G5V

You can refer to the installation steps given here: https://github.com/PhlexPlexico/G5V/wiki/Installation

### Install G5API

You can refer to the installation steps given here: https://github.com/PhlexPlexico/G5API/wiki


## Using Docker

docker-compose.yml file:

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

In this file, following changes will be needed:

1. Change `your-domain.com` to your DNS or Domain
2. Change MySQL and Redis password if needed
3. Add `ADMINS` and `SUPERADMINS` as per your need (Steam64ID, comma sepearated if you want to add multiple admins)

Commands to run to download and install this yml file:

```
sudo apt-get update
apt install docker.io
apt install docker-compose

docker network create -d bridge get5
docker-compose -f /path/to/your/docker-compose-file.yml up -d
```

## Current Limitations with Get5 Integration

1. Stats like KAST, Teammates Flashed, Flashbang Assists, Knife Kills, Bomb plants and defuses are missing and will be shown as 0
2. Coaches cannot be added from the panel (player can type `.coach <side>` to start coaching)
3. Backups cannot be listed and restored from the panel (ingame commands for restoring like `.stop` and `.restore <roundnumber>` will work as expected)
