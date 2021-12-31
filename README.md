# ALVR client for Nreal Light

# Target server version

- [ALVR (Nightly Build)](https://github.com/nosix/nreal-alvr/blob/main/alvr_server_windows.zip)
  - It is not a binary distributed by the official ALVR. Please do not contact the official ALVR for this binary.
  - I plan to support the next released version of the official ALVR.

# Requirements for build

- NRSDK 1.7.0
  - [NRSDKForUnityAndroid_1.7.0.unitypackage](https://developer.nreal.ai/download)
  - [Quickstart for Android](https://nrealsdkdoc.readthedocs.io/en/v1.7.0/Docs/Unity_EN/Develop/Quickstart%20for%20Android.html)
- UniRx 7.1.0
  - dependency is written in Packages/manifest.json
- ALVR Android library (Experimental)
  - [ALVR in my repository](https://github.com/nosix/ALVR/tree/android-lib-no-ovr)
  - dependency is written in Assets/Plugins/Android/mainTemplate.gradle
  - Please do not contact the official ALVR for this library. Currently, I am adding it personally.