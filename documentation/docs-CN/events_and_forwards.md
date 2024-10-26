#  事件和转发

MatchZy 包含一个事件日志系统(深受 Get5 启发),可记录游戏中发生的许多详细信息。

## HTTP

要在 Web 服务器上接收 MatchZy 事件, 请定义 [URL for event logging](../configuration#matchzy_remote_log_url). MatchZy 将通过 HTTP 将所有事件以 JSON 格式发送到 URL. 您可以添加 [custom HTTP header](../configuration#matchzy_remote_log_header_key) 来验证您的请求.

!!! 警告 "Simple HTTP"

    对于失败的请求，没有重复数据删除或重试逻辑。假设您的游戏服务器和 URL 之间始终可以建立稳定的连接。

## 事件

MatchZy 发送的事件的 OpenAPI 文档可在此处找到 [here](events.html).
