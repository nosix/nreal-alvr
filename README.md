# ALVR client for Nreal Light

# Target server version

- [ALVR v17.0.0](https://github.com/alvr-org/ALVR/releases/tag/v17.0.0)

# Controller

This app uses hand tracking and phone controller for controller input.
This app converts hand tracking and phone controller input to Oculus Touch controller input.

## Hand Tracking

Nreal Light's hand tracking is often falsely detected when five fingers cannot be identified.
Therefore, the input is valid only when the hand is facing in a direction that makes it easy to identify the five fingers.
Input is valid only when the hand is facing the angle between MinAnglePalmFacingFront and MaxAnglePalmFacingFront.

| Hand                                                                               | Controller                               |
|------------------------------------------------------------------------------------|------------------------------------------|
| position                                                                           | position                                 |
| orientation                                                                        | orientation                              |
| angle between index middle and palm [ThresholdAngleForTrigger..MaxAngleForTrigger] | trigger value [0..1]                     |
| the angle exceeded ThresholdAngleForTrigger                                        | trigger touch                            |
| The angle has reached MaxAngleForTrigger                                           | trigger click                            |
| angle between middle middle and palm [ThresholdAngleForGrip..MaxAngleForGrip]      | grip value [0..1]                        |
| the angle exceeded ThresholdAngleForGrip                                           | grip touch                               |
| the angle has reached MaxAngleForGrip                                              | grip click                               |
| move up / down / left / right with the back of your hand facing forward            | thumbstick move up / down / left / right |
| angle between thumb metacarpal and thumb top exceeded thresholdAngleBendThumb      | thumbstick touch                         |

When the Button Panel on the phone controller is on, the buttons appear in space when you turn the back of your hand to the front.

## Phone Controller

| Phone Controller                   | Controller                    |
|------------------------------------|-------------------------------|
| Menu button touch                  | menu click                    |
| System button touch                | system (Oculus button) click  |
| left / right Thumb buttons touch   | left / right thumbstick click |
| left / right Trigger buttons touch | left / right trigger click    |
| left / right Grip buttons touch    | left / right grip click       |
| A button touch                     | A click                       |
| B button touch                     | B click                       |
| X button touch                     | X click                       |
| Y button touch                     | Y click                       |

# Settings

## Hand Rotation

You can adjust the orientation of the controller.
The default is set to (90,90,0) according to the orientation of the hand.
If it is difficult to point with the laser, set it to (0,0,0). 
The center of the slider is 0.

By turning on Cache Values, the current settings are saved in the cache and the default value is set to the current value.
By turning off Cache Values, the cached values will be the current values.

## Gear Icon Button

You can open the setting screen from the gear icon.
If you rewrite the value of the text field and press the submit button, the setting will be changed.
If there is a problem with the input content, the content of the problem will be displayed in the Message line of the text field.

If you delete all the text fields and then press the submit button, it will be the default setting.

The following are the items that can be set.

| Item Name                        | Data Type | Default Value          | Description                                                                                                                                                                                      |
|----------------------------------|-----------|------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Message                          | string    |                        | A message displayed when there is a problem with the settings. There is no need to set a value.                                                                                                  |
| SpaceScale                       | float     | 0.5                    | The scale of virtual space with respect to real space.                                                                                                                                           |
| EyeHeight                        | float     | 0.66                   | The height of the HMD from the ground. The unit is meters.                                                                                                                                       |
| DiagonalFovAngle                 | float     | 52                     | Diagonal viewing angle. The unit is degrees.                                                                                                                                                     |
| ZoomRatio                        | float     | 1                      | FOV magnification. The larger the zoom, the narrower the field of view.                                                                                                                          |
| FovRatioInner                    | float     | 45                     | The ratio of the inner of the FOV.                                                                                                                                                               |
| FovRatioOuter                    | float     | 49                     | The ratio of the outer of the FOV.                                                                                                                                                               |
| FovRatioUpper                    | float     | 50                     | The ratio of the upper of the FOV.                                                                                                                                                               |
| FovRatioLower                    | float     | 48                     | The ratio of the lower of the FOV.                                                                                                                                                               |
| HandUpwardMovement               | float     | 0.1                    | Amount to adjust the position of the hand upward. The unit is meters.                                                                                                                            |
| HandForwardMovement              | float     | 0.1                    | Amount to adjust the position of the hand forward. The unit is meters.                                                                                                                           |
| MinAnglePalmFacingFront          | Vector3   | (-30.0, -60.0, -100.0) | The minimum value of the range in which the input is valid by identifying that the palm is facing the front. The unit is degrees. See Hand Tracking.                                             |
| MaxAnglePalmFacingFront          | Vector3   | (10.0, 10.0, 40.0)     | The maximum value of the range in which the input is valid by identifying that the palm is facing the front. The unit is degrees. See Hand Tracking.                                             |
| ThresholdAnglePalmFacingBack     | float     | 60                     | Maximum value of rotation angle to identify the palm facing the back and enable input. If it is made smaller, the direction of the hand that allows input becomes stricter. The unit is degrees. |
| ThresholdYDistanceEnableTracking | float     | 0.3                    | Disable input if head and hand heights are farther than this value. The unit is meters.                                                                                                          |
| MinDistance2DInput               | float     | 0.02                   | Amount of hand movement to start thumbstick move. The unit is meters. If it is made smaller, even a small hand movement will be detected as a thumbstick move.                                   |
| MaxDistance2DInput               | float     | 0.1                    | Amount of hand movement to end thumbstick move. The unit is meters. If you make it smaller, the amount of thumbstick move will increase even with a small hand movement.                         |
| ThresholdAngleBendThumb          | float     | 30                     | The angle at which the thumbstick touch is detected. The unit is degrees. See Hand Tracking.                                                                                                     |
| MaxAngleForTrigger               | float     | 120                    | Finger angle at maximum Trigger value. The unit is degrees. See Hand Tracking.                                                                                                                   |
| ThresholdAngleForTrigger         | float     | 80                     | Finger angle at minimum Trigger value. The unit is degrees. See Hand Tracking.                                                                                                                   |
| MaxAngleForGrip                  | float     | 120                    | Finger angle at maximum Trigger value. The unit is degrees. See Hand Tracking.                                                                                                                   |
| ThresholdAngleForGrip            | float     | 80                     | Finger angle at minimum Trigger value. The unit is degrees. See Hand Tracking.                                                                                                                   |
| SigmaWForAngle                   | float     | 1                      | Kalman filter sigma V for adjusting angle values.                                                                                                                                                |
| SigmaVForAngle                   | float     | 10                     | Kalman filter sigma W for adjusting angle values. If W is increased, the sensor value will be treated as having a large error, and the sensor value will not be trusted.                         |
| SigmaWForPosition                | float     | 1E-05                  | Kalman filter sigma V for adjusting position values.                                                                                                                                             |
| SigmaVForPosition                | float     | 0.0001                 | Kalman filter sigma W for adjusting position values. If W is increased, the sensor value will be treated as having a large error, and the sensor value will not be trusted.                      |

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