#  Events & Forwards

MatchZy contains an event-logging system (heavily inspired by Get5) that logs many details about what is happening in the game.

## HTTP

To receive MatchZy events on a web server, define a [URL for event logging](../configuration#matchzy_remote_log_url). MatchZy
will send all events to the URL as JSON over HTTP. You may add
a [custom HTTP header](../configuration#matchzy_remote_log_header_key) to authenticate your request.

!!! warning "Simple HTTP"

    There is no deduplication or retry-logic for failed requests. It is assumed that a stable connection can be made
    between your game server and the URL at all times.

## Events

OpenAPI documentation of the events sent by MatchZy is available [here](events.html).
