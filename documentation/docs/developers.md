Since this plugin is built on C#, [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) will be required if you intend to make changes in this plugin. Once you have that installed,

1. Clone the repository
2. Use `dotnet restore` to restore and install the dependencies.
3. Make your changes
4. Use `dotnet publish` command and you'll get a folder called `bin` in your plugin directory.
5. Navigate to `bin/Debug/net7.0/publish/`and copy all the content from there and paste it into `csgo/addons/counterstrikesharp/plugins/MatchZy` (CounterStrikeSharp.API.dll and CounterStrikeSharp.API.pdb can be skipped)
6. It's done! Now you can test your changes, and also contribute to the plugin if you want to :p 
