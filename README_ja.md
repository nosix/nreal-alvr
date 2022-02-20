# ALVR client for Nreal Light

# 対象とするサーバーバージョン

- [ALVR v17.0.0](https://github.com/alvr-org/ALVR/releases/tag/v17.0.0)

# 対象とするデバイス

- [Computing Unit included in Dev Kit](https://developer.nreal.ai/dev-kit)

# コントローラー

このアプリではコントローラーの入力にハンドトラッキングとスマホコントローラーを使用します。
このアプリはハンドトラッキングとスマホコントローラーの入力を Oculus Touch コントローラーの入力に変換します。

## ハンドトラッキング

Nreal Light のハンドトラッキングは、５本指を識別できないときに、しばしば誤検出します。
そのため、手が５本指を識別しやすい方向を向いているときのみ入力を受け付けます。
MinAnglePalmFacingFront と MaxAnglePalmFacingFront の間の角度に収まっている時のみ入力は有効になります。

アプリは手の動きを以下の様にコントローラー入力に変換します：

| 手                                                              | コントローラー               |
|----------------------------------------------------------------|-----------------------|
| 位置                                                             | 位置                    |
| 方向                                                             | 方向                    |
| 人差し指と掌の方向がつくる角度 [ThresholdAngleForTrigger..MaxAngleForTrigger] | トリガーの値 [0..1]         |
| 上記の角度が ThresholdAngleForTrigger を超えるとき                         | トリガータッチ               |
| 上記の角度が MaxAngleForTrigger に達したとき                               | トリガークリック              |
| 中指と掌の方向がつくる角度  [ThresholdAngleForGrip..MaxAngleForGrip]        | グリップの値 [0..1]         |
| 上記の角度が ThresholdAngleForGrip を超えるとき                            | グリップタッチ               |
| 上記の角度が MaxAngleForGrip に達したとき                                  | グリップクリック              |
| 掌を顔方向に向けて上下左右に移動 [MinDistance2DInput..MaxDistance2DInput]      | 親指スティックの上下左右移動 [0..1] |
| 親指の付け根と先端の方向がつくる角度が ThresholdAngleBendThumb を超えるとき             | 親指スティックタッチ            |

スマホコントローラー上でボタンパネルが有効にされている場合、掌を顔方向に向けると空間にボタンが表示されます。

## スマホコントローラー

アプリはスマホコントローラーの入力を以下の様にコントローラーの入力に変換します：

| スマホコントローラー        | コントローラー                |
|-------------------|------------------------|
| Menu ボタンタッチ       | メニュークリック               |
| System ボタンタッチ     | システム (Oculus ボタン) クリック |
| 左右 Thumb ボタンタッチ   | 左右親指スティッククリック          |
| 左右 Trigger ボタンタッチ | 左右トリガークリック             |
| 左右 Grip ボタンタッチ    | 左右グリップクリック             |
| A ボタンタッチ          | A クリック                 |
| B ボタンタッチ          | B クリック                 |
| X ボタンタッチ          | X クリック                 |
| Y ボタンタッチ          | Y クリック                 |

# 設定

## 手の回転

コントローラーの方向を調整できます。
デフォルトは実際の手の向きに合わせて (90,90,0) に設定されています。
レーザーで指し示すことが難しい場合には、手の向きを (0,0,0) に設定してください。
スライダーの中央が 0 です。

Cache Value を有効にすると、現在の設定がキャッシュされ、デフォルト値が現在の設定に反映されます。
Cache Value を無効にすると、キャッシュされた値が現在の設定に反映されます。

## ギアアイコンボタン

ギアアイコンから設定ダイアログを開けます。
テキストフィールドで値を書き換えて Submit ボタンを押下すると、設定が変更されます。
入力した内容に問題があった場合、テキストフィールドの Message 行に問題の内容が表示されます。

テキストフィールドの全ての値を消去して Submit ボタンを押下すると、デフォルトの設定に変更されます。

設定項目は以下の様になっています。

| 項目名                              | データ型    | デフォルト値                 | 説明                                                                        |
|----------------------------------|---------|------------------------|---------------------------------------------------------------------------|
| Message                          | string  |                        | 設定に問題があったときにメッセージが表示されます。値を設定する必要はありません。                                  |
| SpaceScale                       | float   | 0.5                    | 実空間に対する仮想空間の倍率。                                                           |
| EyeHeight                        | float   | 0.66                   | 地面から HMD までの高さ。単位はメートル。                                                   |
| Ipd                              | float   | 0.068606               | 瞳孔間距離. 単位はメートル。                                                           |
| DiagonalFovAngle                 | float   | 52                     | 対角視野角。単位は度。                                                               |
| ZoomRatio                        | float   | 1                      | 視野の倍率。大きいと拡大される。倍率が大きいほど、視野が狭くなる。　                                        |
| FovRatioInner                    | float   | 45                     | 視野の内側の比率。 　                                                               |
| FovRatioOuter                    | float   | 49                     | 視野の外側の比率。   　                                                             |
| FovRatioUpper                    | float   | 50                     | 視野の上側の比率。     　                                                           |
| FovRatioLower                    | float   | 48                     | 視野の下側の比率。                                                                 |
| HandUpwardMovement               | float   | 0.1                    | 手の位置を上方向に調整する量。単位はメートル。                                                   |
| HandForwardMovement              | float   | 0.1                    | 手の位置を前方向に調整する量。単位はメートル。                                                   |
| MinAnglePalmFacingFront          | Vector3 | (-30.0, -60.0, -100.0) | 掌が前方を向いている状態を基準として、入力を有効とする掌の向き最小値(角度)。単位は度。ハンドトラッキング参照。                  |
| MaxAnglePalmFacingFront          | Vector3 | (10.0, 10.0, 40.0)     | 掌が前方を向いている状態を基準として、入力を有効とする掌の向き最大値(角度)。単位は度。ハンドトラッキング参照。                  |
| ThresholdAnglePalmFacingBack     | float   | 60                     | 掌が顔方向を向いている状態を基準として、入力を有効とする回転角度の最大値(角度)。小さくすると、入力を有効とする手の方向が厳しくなる。単位は度。  |
| ThresholdYDistanceEnableTracking | float   | 0.3                    | 頭と手の高さの差がこの値を超えると入力を無効にする。単位はメートル。                                        |
| MinDistance2DInput               | float   | 0.02                   | 親指スティックの移動を開始する手の移動量。単位はメートル。小さくすると、少しの手の動きで親指スティックが移動し始める。               |
| MaxDistance2DInput               | float   | 0.1                    | 親指スティックの移動を終了する手の移動量。単位はメートル。小さくすると、少しの手の動きで親指スティックが大きく移動する。              |
| ThresholdAngleBendThumb          | float   | 30                     | 親指スティックタッチを検出する角度。単位は度。ハンドトラッキング参照。                                       |
| MaxAngleForTrigger               | float   | 120                    | 最大のトリガー値となる指の角度。単位は度。ハンドトラッキング参照。                                         |
| ThresholdAngleForTrigger         | float   | 80                     | 最小のトリガー値となる指の角度。単位は度。ハンドトラッキング参照。                                         |
| MaxAngleForGrip                  | float   | 120                    | 最大のグリップ値となる指の角度。単位は度。ハンドトラッキング参照。                                         |
| ThresholdAngleForGrip            | float   | 80                     | 最小のグリップ値となる指の角度。単位は度。ハンドトラッキング参照。                                         |
| SigmaWForAngle                   | float   | 1                      | 角度の値を調整するためのカルマンフィルタの sigma V。                                            |
| SigmaVForAngle                   | float   | 10                     | 角度の値を調整するためのカルマンフィルタの sigma W。W を増やすと、センサー値は大きな誤差を含むとして扱い、センサー値を信頼しなくなる。  |
| SigmaWForPosition                | float   | 1E-05                  | 位置の値を調整するためのカルマンフィルタの sigma V。                                            |
| SigmaVForPosition                | float   | 0.0001                 | 位置の値を調整するためのカルマンフィルタの sigma W。W を増やすと、センサー値は大きな誤差を含むとして扱い、センサー値を信頼しなくなる。　 |

# トラブルシューティング

## 頭を動かしたときに映像が追従しない

実行中の全てのアプリを終了し、Nreal Launcher を再起動してください。

# ビルドにおける要求事項

- NRSDK 1.8.0
  - [NRSDKForUnityAndroid_1.8.0.unitypackage](https://developer.nreal.ai/download)
  - [Quickstart for Android](https://nreal.gitbook.io/nrsdk-documentation/discover/quickstart-for-android)
- UniRx 7.1.0
  - Packages/manifest.json に依存を記載
- ALVR Android library (Experimental)
  - [ALVR in my repository](https://github.com/nosix/ALVR/tree/android-lib-no-ovr)
    - クライアントバージョンを `17.0.0` に変更
      - `alvr/common/Cargo.toml` のパッケージバージョンを編集
      - ビルド時に指定できる様になるかもしれません ([cargo #6583](https://github.com/rust-lang/cargo/issues/6583))
    - 以下でローカル Maven リポジトリに配布できます
      ```
      cd alvr/experiments/android-lib-no-ovr
      ./gradlew publishToMavenLocal
      ```
  - Assets/Plugins/Android/mainTemplate.gradle に依存を記載
  - このライブラリについて公式の ALVR に問い合わせないでください。現在、このライブラリは私が個人的に追加しています。

# 外部リソース

- アイコン
  - [ICOOON MONO](https://icooon-mono.com/?lang=en)
    - [No redistribution](https://icooon-mono.com/license/)