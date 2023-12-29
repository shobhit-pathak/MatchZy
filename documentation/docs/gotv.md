## GOTV Broadcast

MatchZy makes no changes to the broadcasting part of the GOTV, but will automatically adjust the
[`mp_match_restart_delay`](https://totalcsgo.com/command/mpmatchrestartdelay) when a map ends if GOTV is enabled to
ensure that it won't be shorter than what is required for the GOTV broadcast to finish.

!!! warning "Don't mess too much with the TV!"

    Changing `tv_delay` or `tv_enable` in `warmup.cfg`, `live.cfg` etc. is going to cause problems with your demos.
    We recommend you set `tv_delay` either on your server in general.


## Recording Demos

MatchZy records the demos automatically. It recording starts once all teams have readied up and ends following a map result.

Path of demos can be configured using `matchzy_demo_path <directory>/`. If defined, it must not start with a slash and must end with a slash. Set to empty string to use the csgo root.

Demo files will be named according to `matchzy_demo_name_format`. The default format is: `"{TIME}_{MATCH_ID}_{MAP}_{TEAM1}_vs_{TEAM2}"`

!!! info "Broadcast delay on GOTV recording"

    When the GOTV recording stops, the server will flush its framebuffer to disk. This may cause a lag spike or a
    complete freeze of the GOTV broadcast if you have a substantial `tv_delay`, so MatchZy will wait until the entire match
    has been broadcast before it stops recording the demo.

## Automatic Upload

In addition to recording demos, MatchZy can also upload them to a URL when the recording stops. You can define the upload URL with
`matchzy_demo_upload_url <upload_url>`. The HTTP body will be the zipped demo file, and you can
read the [headers](#headers) for file metadata.

Example: `matchzy_demo_upload_url "https://your-website.com/upload-endpoint"`

### Headers

MatchZy will add these HTTP headers to its demo upload request:

1. `MatchZy-FileName` is the name of the demo file
2. `MatchZy-MapNumber` is the zero-indexed map number in the series.
3. `MatchZy-MatchId` Unique ID of the match.


### Example

This is an example of how a [Node.js](https://nodejs.org/en/) web server using [Express](https://expressjs.com/) might
read the demo upload request sent by MatchZy.

!!! warning "Proof of concept only"
 
    This is a simple proof-of-concept and should not be blindly copied to a production system. It has no HTTPS support
    and is only meant to demonstrate the key aspects of reading a potentially large POST request.

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
