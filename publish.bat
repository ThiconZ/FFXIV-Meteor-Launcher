
dotnet publish "FFXIV Meteor Launcher/FFXIV Meteor Launcher.csproj" -r win-x86 -c Release -o ./publish /p:PublishSingleFile=true --self-contained false
move "publish\FFXIV Meteor Launcher.exe" "publish\FFXIV Meteor Launcher.exe"
