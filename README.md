# Soul Hackers 2 Fix
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/W7W01UAI9)</br>
[![Github All Releases](https://img.shields.io/github/downloads/Lyall/SoulHackers2Fix/total.svg)](https://github.com/Lyall/SoulHackers2Fix/releases)

This BepInEx plugin for the game Soul Hackers 2 features:
- Proper ultrawide and non-16:9 aspect ratio support with pillarbox/letterbox removal.
<!-- - Smoother camera movement with a higher update rate. -->
<!-- - Intro/logos skip. -->
<!-- - Graphical tweaks to increase fidelity. -->
<!-- - Adjusting field of view. -->
<!-- - Vert+ FOV at narrower than 16:9 aspect ratios. -->

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
Please report any issues you see and I'll do my best to address them.

## Screenshots
| ![ezgif-5-8c9e6fffc1](https://user-images.githubusercontent.com/695941/186994307-31006ada-6571-4b27-adf8-f87d838d4d60.gif) |
|:--:|
| Ultrawide pillarbox removal. | 

## Credits
- [BepinEx](https://github.com/BepInEx/BepInEx) is licensed under the GNU Lesser General Public License v2.1.
