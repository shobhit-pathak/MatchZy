此插件是基于 C# 构建的,如果您打算修改此插件,则需要安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).

安装后，

1. 克隆github的repo

2. 使用 `dotnet restore` 恢复并安装依赖项。

3. 修改代码

4. 使用 `dotnet publish` 命令，您将在插件目录中获得一个名为 `bin` 的文件夹。

5. 导航到 `bin/Release/net8.0/publish/` 并从那里复制所有内容并将其粘贴到 `csgo/addons/counterstrikesharp/plugins/MatchZy` 中（可以跳过 CounterStrikeSharp.API.dll 和 CounterStrikeSharp.API.pdb）

6. 完成了！现在您可以测试您的更改，并且如果您愿意，还可以为插件做出贡献 :p
