# FFXIV Meteor Launcher

Open source launcher for FFXIV 1.23b clients that allows for specifying custom server connection endpoints.

This supports being used with the [Project Meteor Server](http://ffxivclassic.fragmenterworks.com/wiki/index.php/Main_Page) project.

## Requirements

- Microsoft [.NET Runtime 6.0](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) or higher

## Installation

1. Download the latest version from [Releases](https://github.com/ThiconZ/FFXIV-Meteor-Launcher/releases).
2. Extract the archive to anywhere on your computer.
3. Run the `FFXIV Meteor Launcher.exe`.

## How To Use

Once you start the launcher, you may be greeted with a dialog asking you to select your FFXIV 1.0 installation location. This occurs when the game is installed through abnormal means such as copying your installation to another directory instead of using the original game installer. Following the instructions for selecting your correct installation location to progress past this step. This is required and if you do not select a location the launcher will exit.

For most launcher starts, you will be brought directly to the main interface. From here you can select what server you want to access, enter your login information, patch the game client if necessary, and most importantly, launch the game.

![](https://github.com/ThiconZ/FFXIV-Meteor-Launcher/blob/master/Screenshots/MeteorLauncher_Screenshot.png)

The bottom left of the launcher window will display three relevant version fields:
- `MLVersion`
  - Version number of FFXIV Meteor Launcher.
- `Boot`
  - Version of the "Boot" component of your FFXIV 1.0 installation.
- `Game`
  - Version of the "Game" component of your FFXIV 1.0 installation.

## Adding Custom Servers

Arbitrary servers can be added to the launcher's supported server list by editing the `Servers.xml` file. This file exists along side the core launcher binary.

1. Open `Servers.xml` in a text editor.
2. Copy one of the lines starting with `<Server>`.
3. Paste the copied line onto a new line that is still within the `<Servers>` element of the file.
4. Update the fields with the desired server connection information.

### Servers.xml Format

The server list file supports a number of attribute fields to allow for a customized experience when interacting with each individual server.

Within each `<Server>` element are the `Name`, `Address`, and `LoginUrl` attributes. All three of these should be included in each server entry. Additionally, there is support for applying server-specific themes in the launcher via the `ThemeUrl` attribute. This can be either a local system file path or a remote server endpoint that points to a valid [theme json file](#themejson-format).

The `LoginUrl` can only be left blank if performing debug testing with default values (requires code edits in the launcher to test properly).

**Schema**
```xml
<xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema">
  <xs:element name="Servers">
    <xs:complexType>
      <xs:sequence>
        <xs:element name="Server" maxOccurs="unbounded" minOccurs="0">
          <xs:complexType>
            <xs:attribute type="xs:string" name="Name" use="required"/>
            <xs:attribute type="xs:string" name="Address" use="required"/>
            <xs:attribute type="xs:string" name="LoginUrl" use="required"/>
            <xs:attribute type="xs:string" name="ThemeUrl" use="optional"/>
          </xs:complexType>
        </xs:element>
      </xs:sequence>
    </xs:complexType>
  </xs:element>
</xs:schema>
```

Example of a completed `Servers.xml` file:
```xml
<Servers>
    <Server Name = "Default" Address = "http://127.0.0.1" LoginUrl = "http://127.0.0.1/login.php" />
    <Server Name = "Second Server" Address = "http://127.0.0.1" LoginUrl = "http://127.0.0.1/login2.php" ThemeUrl = "http://127.0.0.1/Theme.json" />
    <Server Name = "Another Server" Address = "https://example.com" LoginUrl = "https://example.com/login.php" />
</Servers>
```

## Theme.json Format

FFXIV Meteor Launcher supports changing the location and appearance of the interface to allow servers to have a more unique feel. These settings are controlled through a JSON file (referenced as Theme.json here but any name is valid).

This file is specified in the Servers.xml on a per-server basis as noted in the prior [Servers.xml Format](#serversxml-format) section.

Table of UI elements able to be modified:

| Name | Margin\* | Width | Height | Foreground\*\* | Background\*\* | Source\*\*\* |
| ------ | ------ | ------ | ------ | ------ | ------ | ------ |
| `UsernameLabel` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `UsernameTextBox` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `PasswordLabel` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `PasswordTextBox` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `LoginBtn` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `ServerListComboBox` | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `VersionLabel` | :x: | :x: | :x: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `BootVersionLabel` | :x: | :x: | :x: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `GameVersionLabel` | :x: | :x: | :x: | :heavy_check_mark: | :heavy_check_mark: | :x: |
| `BackgroundImage` | :x: | :x: | :x: | :x: | :x: | :heavy_check_mark: |

**Notes:**
- Names should be used as-is with their capitalization while attributes should all be lower-case-only.
- \* Margin is actually a list of `left`, `top`, `right`, `bottom` fields which all need to be individually listed (only the ones you wish to modify need to exist in the file)
- \*\* Foreground and Background are Hex Coded RGB or ARGB colors.
- \*\*\* Source is a file path, either local file system or remote online url to a direct image file.

Example Theme.json file:
```json
{
  "UsernameLabel":
  {
    "left": "0",
    "top": "55",
    "foreground": "#FFFFFF"
  },
  "UsernameTextBox":
  {
    "left": "67",
    "top": "59",
    "right": "710",
    "foreground": "#000000",
    "background": "#FFFFFF"
  },
  "PasswordLabel":
  {
    "left": "0",
    "top": "86",
    "foreground": "#FFFFFF"
  },
  "PasswordTextBox":
  {
    "left": "67",
    "top": "90",
    "right": "710",
    "foreground": "#000000",
    "background": "#FFFFFF"
  },
  "LoginBtn":
  {
    "left": "67",
    "top": "113",
    "right": "710",
    "foreground": "#000000",
    "background": "#FFFFFF"
  },
  "ServerListComboBox":
  {
    "left": "710",
    "top": "10",
    "right": "10",
    "foreground": "#000000",
    "background": "#FFFFFF"
  },
  "VersionLabel":
  {
    "foreground": "#FFFFFF"
  },
  "BootVersionLabel":
  {
    "foreground": "#000000"
  },
  "GameVersionLabel":
  {
    "foreground": "#000000"
  },
  "BackgroundImage":
  {
    "source": "https://i.imgur.com/EWS6V3B.png"
  }
}
```

