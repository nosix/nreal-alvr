# ALVR client for Nreal Light

# Target server version

- [ALVR v17.0.0](https://github.com/alvr-org/ALVR/releases/tag/v17.0.0)

# Requirements for build

- NRSDK 1.8.0
  - [NRSDKForUnityAndroid_1.8.0.unitypackage](https://developer.nreal.ai/download)
  - [Quickstart for Android](https://nreal.gitbook.io/nrsdk-documentation/discover/quickstart-for-android)
- UniRx 7.1.0
  - dependency is written in Packages/manifest.json
- ALVR Android library (Experimental)
  - [ALVR in my repository](https://github.com/nosix/ALVR/tree/android-lib-no-ovr)
    - Change client version to `17.0.0`
      - Edit package version in `alvr/common/Cargo.toml`
      - It may be possible to specify it at build time. ([cargo #6583](https://github.com/rust-lang/cargo/issues/6583))
    - It can be used by publishing to local maven repository in advance.
      ```
      cd alvr/experiments/android-lib-no-ovr
      ./gradlew publishToMavenLocal
      ```
  - dependency is written in Assets/Plugins/Android/mainTemplate.gradle
  - Please do not contact the official ALVR for this library. Currently, I am adding it personally.

# External Resource

- Icon
  - [ICOOON MONO](https://icooon-mono.com/?lang=en)
    - [No redistribution](https://icooon-mono.com/license/)