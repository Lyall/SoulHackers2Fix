# Soul Hackers 2 Fix
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/W7W01UAI9)</br>
[![Github All Releases](https://img.shields.io/github/downloads/Lyall/SoulHackers2Fix/total.svg)](https://github.com/Lyall/SoulHackers2Fix/releases)

This WIP BepInEx plugin for the game Soul Hackers 2 features:
- Proper ultrawide and non-16:9 aspect ratio support with pillarbox removal.
- Graphical tweaks to increase fidelity.
- Adjusting field of view.
- Vert+ FOV at narrower than 16:9 aspect ratios.

## Installation
- Grab the latest release of SoulHackers2Fix from [here.](https://github.com/Lyall/SoulHackers2Fix/releases)
- Extract the contents of the release zip in to the game directory.<br />(e.g. "**steamapps\common\SOUL HACKERS2**" for Steam).
- Run the game once to generate a config file located at **<GameDirectory>\BepInEx\config\SoulHackers2Fix.cfg**
- The first launch may take a little while as BepInEx does its magic.

### Linux
- If you are running Linux (for example with the Steam Deck) then the game needs to have it's launch option changed to load BepInEx.
- You can do this by going to the game properties in Steam and finding "LAUNCH OPTIONS".
- Make sure the launch option is set to: ```WINEDLLOVERRIDES="winhttp=n,b" %command%```

| ![steam launch options](https://user-images.githubusercontent.com/695941/179568974-6697bfcf-b67d-441c-9707-88cd3c72a104.jpeg) |
|:--:|
| Steam launch options. |

## Configuration
- See the generated config file to adjust various aspects of the plugin.

## Known Issues
Please report any issues you see.

- Run into issues after updating the mod? Try deleting your config file, then booting the game to generate a new one.

## Screenshots
|  |
|:--:|
| Ultrawide pillarbox removal. | 

## Credits
- [BepinEx](https://github.com/BepInEx/BepInEx) is licensed under the GNU Lesser General Public License v2.1.
