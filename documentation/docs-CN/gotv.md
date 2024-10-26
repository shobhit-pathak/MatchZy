## GOTV广播

MatchZy 不会对 GOTV 的广播部分进行任何更改，但如果启用了 GOTV，则地图结束时会自动调整[`mp_match_restart_delay`](https://totalcsgo.com/command/mpmatchrestartdelay) 以确保它不会短于 GOTV 广播完成所需的时间。

!!! 警告 "Don't mess too much with the TV!"

    在 `warmup.cfg`, `live.cfg`文件中修改 `tv_delay` 或者 `tv_enable`等参数.将导致demo出现问题.
    我们建议您在服务器上设置"tv_delay".


## 录制Demos

MatchZy 会自动录制Demo. 录制在所有队伍准备就绪后开始,并在地图结果后结束.

可以使用 `matchzy_demo_path <directory>/` 配置Demo保存路径. 如果已定义，则不能以斜杠开头，必须以斜杠结尾。设置为空字符串以使用 csgo 根目录.

Demo 的命名将根据 `matchzy_demo_name_format`字段决定. 默认格式为: `"{TIME}_{MATCH_ID}_{MAP}_{TEAM1}_vs_{TEAM2}"`

!!! INFO "Broadcast delay on GOTV recording"

    When the GOTV recording stops, the server will flush its framebuffer to disk. This may cause a lag spike or a
    complete freeze of the GOTV broadcast if you have a substantial `tv_delay`, so MatchZy will wait until the entire match
    has been broadcast before it stops recording the demo.

## 自动上传

除了录制Demo之外，MatchZy 还可以在录制停止时将Demo上传到 URL.您可以使用 `matchzy_demo_upload_url <upload_url>` 定义上传的URL. HTTP 正文将是压缩的Demo文件, 您可以
阅读 [headers](#headers) 以获取文件元数据。

例子: `matchzy_demo_upload_url "https://your-website.com/upload-endpoint"`

### Headers

MatchZy 将在其Demo上传请求中添加以下 HTTP 标头:

1. `MatchZy-FileName` Demo的名车
2. `MatchZy-MapNumber` 从0开始的地图编号
3. `MatchZy-MatchId` 比赛的唯一ID


### Example

这是一个使用 [Express](https://expressjs.com/) 的 [Node.js](https://nodejs.org/en/) Web 服务器如何读取 MatchZy 发送的演示上传请求的示例.

!!! warning "Proof of concept only"
 
    这是一个简单的概念验证，不应盲目复制到生产系统.它不支持 HTTPS,仅用于演示读取可能很大的 POST 请求的关键方面。

```js title="Node.js example"
const express = require('express');
const path = require('path');
const fs = require('fs');

const app = express();
const port = 3000;

app.post('/upload', function (req, res) {

    // Read the MatchZy headers to know what to do with the file.
    const filename = req.header('MatchZy-FileName');
    const matchId = req.header('MatchZy-MatchId');
    const mapNumber = req.header('MatchZy-MapNumber');
 
    // Put all demos for the same match in a folder.
    const folder = path.join(__dirname, 'demos', matchId);
    if (!fs.existsSync(folder)) {
       fs.mkdirSync(folder, {recursive: true});
    }
    // Create a stream and point it to a file, using the filename from the header.
    let writeStream = fs.createWriteStream(path.join(folder, filename));
 
    // Pipe the request body into the stream.
    req.pipe(writeStream);
 
    // Wait for the request to end and reply with 200.
    req.on('end', () => {
       writeStream.end();
       res.status(200);
       res.end('Success');
    });
 
    // If there is a problem writing the file, reply with 500.
    writeStream.on('error', function (err) {
       res.status(500);
       res.end('Error writing demo file: ' + err.message);
    });
 
 })
 
app.listen(port);
```
