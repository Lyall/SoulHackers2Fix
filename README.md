# Soul Hackers 2 Fix
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/W7W01UAI9)</br>
[![Github All Releases](https://img.shields.io/github/downloads/Lyall/SoulHackers2Fix/total.svg)](https://github.com/Lyall/SoulHackers2Fix/releases)

This WIP BepInEx plugin for the game Soul Hackers 2 features:
- Proper ultrawide and non-16:9 aspect ratio support with pillarbox removal.
- Custom resolution support.
- Graphical tweaks to increase fidelity.
  - Disable Chromatic Aberration
  - Anisotropic Filtering
  - LOD Bias
  - Render Scale (Resolution Scale)
- Fix for slidey character movement above 60fps.

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
- Fullscreen exclusive for custom resolutions does not seem to work properly at the moment.

## Screenshots
| ![ezgif-5-8c9e6fffc1](https://user-images.githubusercontent.com/695941/187015446-42c19f43-c3e6-48f6-811f-f0fb120deedb.gif) |
|:--:|
| Ultrawide pillarbox removal. | 

![CA On](https://user-images.githubusercontent.com/695941/187334877-2768b152-7f49-4787-b5a6-f8e4b4befb4a.jpg)|  ![CA Off](https://user-images.githubusercontent.com/695941/187334873-36430911-4eae-432b-9aa5-72708a212cb2.jpg)  
:-------------------------:|:-------------------------:
Chromatic Aberration On |  Chromatic Aberration Off


  
## Credits
- [BepinEx](https://github.com/BepInEx/BepInEx) is licensed under the GNU Lesser General Public License v2.1.
