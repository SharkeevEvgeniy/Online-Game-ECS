# Snipe Unity Package


## Installation guide

* Install [Facebook SDK for Unity](https://developers.facebook.com/docs/unity/) (direct [download link](https://origincache.facebook.com/developers/resources/?id=FacebookSDK-current.zip))
* Install [External Dependency Manager for Unity](https://github.com/googlesamples/unity-jar-resolver). Actually it is already should be installed because it is included in the previous package.
* [Add](https://docs.unity3d.com/Manual/upm-ui-giturl.html) <b>Snipe Client Tools</b> package to Unity Package Manager - https://github.com/Mini-IT/SnipeToolsUnityPackage.git <br />
After package import is done in Unity editor "Snipe" menu should appear.
* Click <b>"Snipe/Install Snipe Package"</b> menu item

## Updating

Unity Package Manager doesn't support auto updates for git-based packages. That is why Snipe Client Tools comes with its own Updater (<b>"Snipe/Updater"</b> menu item).

Alternatively there are some other methods:
* You may use [UPM Git Extension](https://github.com/mob-sakai/UpmGitExtension).
* You may add the same package again using git URL. Package manager will update an existing one.
* Or you may manually edit your project's Packages/packages-lock.json. Just remove "com.miniit.snipe.client" section.

## Third-party libraries used

* [websocket-sharp](https://github.com/sta/websocket-sharp)
* [BetterStreamingAssets](https://github.com/gwiazdorrr/BetterStreamingAssets)
* [fastJSON](https://github.com/mgholam/fastJSON) - modified for IL2CPP compatibility
* KcpClient from [Mirror](https://github.com/vis2k/Mirror)
