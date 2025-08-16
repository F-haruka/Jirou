# Unityエディタセットアップガイド

このドキュメントは、Jirouプロジェクトの奥行き型リズムゲームシステムをUnityエディタ上で完全にセットアップするための詳細な手順書です。

## 必要な前提条件

- Unity 6000.2.0f1 (Unity 6.0 LTS) または Unity 2022.3 LTS
- Universal Render Pipeline (URP) が有効化されていること
- プロジェクトが正しくインポートされていること

## セットアップ概要

以下のコンポーネントとオブジェクトをセットアップします：

1. **Conductor（音楽同期マネージャー）**
2. **NoteSpawner（ノーツ生成システム）**
3. **NotePoolManager（オブジェクトプール管理）**
4. **LaneVisualizer（レーン表示システム）**
5. **NoteSpawnerTestSetup（テスト環境）**
6. **必要なプレハブとアセット**

---

## ステップ1: 基本シーンの準備

### 1.1 シーンのクリーンアップ

1. Unityエディタで `Assets/_Jirou/Scenes/SampleScene.unity` を開く
2. Hierarchyウィンドウで不要なオブジェクトを削除（デフォルトのCubeやSphereなど）
3. Main CameraとDirectional Lightのみを残す

### 1.2 カメラの設定

1. **Main Camera** を選択
2. Inspectorで以下を設定：
   - **Position**: X=0, Y=5, Z=-5
   - **Rotation**: X=30, Y=0, Z=0
   - **Field of View**: 60
   - **Clipping Planes**:
     - Near: 0.3
     - Far: 100

### 1.3 ライティングの調整

1. **Directional Light** を選択
2. Inspectorで以下を設定：
   - **Rotation**: X=50, Y=-30, Z=0
   - **Intensity**: 1.2
   - **Color**: 白（#FFFFFF）

---

## ステップ2: Conductorのセットアップ

### 2.1 Conductorオブジェクトの作成

1. Hierarchyウィンドウで右クリック → **Create Empty**
2. 新しいGameObjectの名前を **"Conductor"** に変更
3. **Position**: X=0, Y=0, Z=0 に設定

### 2.2 Conductorコンポーネントの追加

1. Conductorオブジェクトを選択
2. **Add Component** → **Jirou** → **Core** → **Conductor** を追加
3. **Add Component** → **Audio** → **Audio Source** を追加

### 2.3 Conductorの設定

Inspectorで以下を設定：

**Conductor コンポーネント:**
- **Song BPM**: 180
- **Spawn Z**: 20
- **Judgment Z**: 0
- **Note Y**: 0.5
- **Lane Width**: 2
- **Note Speed**: 10

**Audio Source コンポーネント:**
- **AudioClip**: `Assets/_Jirou/Audio/Music/End_Time.wav` をドラッグ&ドロップ
- **Play On Awake**: オフ
- **Loop**: オフ
- **Volume**: 1

---

## ステップ3: NotePoolManagerのセットアップ

### 3.1 NotePoolManagerオブジェクトの作成

1. Hierarchyウィンドウで右クリック → **Create Empty**
2. 名前を **"NotePoolManager"** に変更
3. **Position**: X=0, Y=0, Z=0

### 3.2 NotePoolManagerコンポーネントの追加

1. NotePoolManagerオブジェクトを選択
2. **Add Component** → **Jirou** → **Gameplay** → **NotePoolManager**

### 3.3 NotePoolManagerの設定

Inspectorで以下を設定：
- **Initial Pool Size**: 10
- **Max Pool Size**: 30
- **Tap Note Prefab**: 後でプレハブを作成して設定
- **Hold Note Prefab**: 後でプレハブを作成して設定

---

## ステップ4: NoteSpawnerのセットアップ

### 4.1 NoteSpawnerオブジェクトの作成

1. Hierarchyウィンドウで右クリック → **Create Empty**
2. 名前を **"NoteSpawner"** に変更
3. **Position**: X=0, Y=0, Z=0

### 4.2 NoteSpawnerコンポーネントの追加

1. NoteSpawnerオブジェクトを選択
2. **Add Component** → **Jirou** → **Gameplay** → **NoteSpawner**

### 4.3 NoteSpawnerの設定

Inspectorで以下を設定：
- **Beats Shown In Advance**: 3
- **Enable Debug Log**: オン（デバッグ用）
- **Show Note Path Gizmo**: オン（ギズモ表示）
- **Lane X Positions**: 自動的にConductorから取得される
- **Note Y**: 0.5

---

## ステップ5: LaneVisualizerのセットアップ

### 5.1 LaneVisualizerオブジェクトの作成

1. Hierarchyウィンドウで右クリック → **Create Empty**
2. 名前を **"LaneVisualizer"** に変更
3. **Position**: X=0, Y=0, Z=0

### 5.2 LaneVisualizerコンポーネントの追加

1. LaneVisualizerオブジェクトを選択
2. **Add Component** → **Jirou** → **Visual** → **LaneVisualizer**

### 5.3 LaneVisualizerの設定

Inspectorで以下を設定：

**レーン設定:**
- **Lane Count**: 4
- **Lane Width**: 2.0

**奥行き設定:**
- **Near Width**: 2.0
- **Far Width**: 0.5
- **Lane Length**: 20.0

**ビジュアル設定:**
- **Lane Material**: 新しいマテリアルを作成（後述）
- **Line Width**: 0.05
- **Lane Color**: RGBA(1, 1, 1, 0.3)

**オプション設定:**
- **Show Center Line**: オン
- **Show Outer Borders**: オン

**Conductor連携:**
- **Sync With Conductor**: オン
- **Sync Update Interval**: 1.0

### 5.4 レーンマテリアルの作成

1. Projectウィンドウで `Assets/_Jirou/Art/Materials` フォルダを右クリック
2. **Create** → **Material** を選択
3. 名前を **"LaneMaterial"** に変更
4. Shaderを **Universal Render Pipeline** → **Unlit** → **Transparent** に設定
5. **Base Map** の色を白、アルファを0.3に設定
6. LaneVisualizerの **Lane Material** フィールドにドラッグ&ドロップ

---

## ステップ6: ノーツプレハブの作成

### 6.1 Tapノーツプレハブの作成

1. Hierarchyウィンドウで右クリック → **3D Object** → **Cube**
2. 名前を **"TapNote"** に変更
3. **Transform** を設定：
   - **Scale**: X=0.8, Y=0.3, Z=0.5
4. **Add Component** → **Jirou** → **Gameplay** → **NoteController**
5. **Box Collider** の **Is Trigger** をオンに設定
6. マテリアルを作成して適用：
   - Projectで右クリック → **Create** → **Material**
   - 名前を **"TapNoteMaterial"** に変更
   - **Base Map** の色をシアン（#00FFFF）に設定
   - Cubeにドラッグ&ドロップ
7. プレハブ化：
   - Hierarchyの "TapNote" を `Assets/_Jirou/Prefabs/Notes` フォルダにドラッグ
   - Hierarchyから元のオブジェクトを削除

### 6.2 Holdノーツプレハブの作成

1. 上記と同じ手順でCubeを作成
2. 名前を **"HoldNote"** に変更
3. **Transform** を設定：
   - **Scale**: X=0.8, Y=0.3, Z=2.0（長めに設定）
4. **Add Component** → **Jirou** → **Gameplay** → **NoteController**
5. **Box Collider** の **Is Trigger** をオンに設定
6. マテリアルを作成して適用：
   - 名前を **"HoldNoteMaterial"** に変更
   - **Base Map** の色を黄色（#FFFF00）に設定
7. プレハブ化して保存

### 6.3 プレハブの適用

1. **NotePoolManager** を選択
2. Inspectorで：
   - **Tap Note Prefab**: TapNoteプレハブをドラッグ&ドロップ
   - **Hold Note Prefab**: HoldNoteプレハブをドラッグ&ドロップ
3. **NoteSpawner** を選択
4. 同様に両プレハブを設定

---

## ステップ7: NoteSpawnerTestSetupの追加

### 7.1 テストセットアップオブジェクトの作成

1. Hierarchyウィンドウで右クリック → **Create Empty**
2. 名前を **"TestSetup"** に変更
3. **Position**: X=0, Y=0, Z=0

### 7.2 NoteSpawnerTestSetupコンポーネントの追加

1. TestSetupオブジェクトを選択
2. **Add Component** → **Jirou** → **Testing** → **NoteSpawnerTestSetup**

### 7.3 テストセットアップの設定

Inspectorで以下を設定：

**セットアップ設定:**
- **Auto Setup**: オン
- **Generate Test Chart**: オン

**テスト譜面設定:**
- **Test BPM**: 180（End_Time.wavに合わせる）
- **Note Count**: 20
- **Note Interval**: 1.0

**コンポーネント参照:**
- **Conductor**: Hierarchyから Conductor をドラッグ&ドロップ
- **Note Spawner**: Hierarchyから NoteSpawner をドラッグ&ドロップ
- **Note Pool Manager**: Hierarchyから NotePoolManager をドラッグ&ドロップ

---

## ステップ8: 判定ラインの視覚化（オプション）

### 8.1 判定ラインオブジェクトの作成

1. Hierarchyで右クリック → **3D Object** → **Plane**
2. 名前を **"JudgmentLine"** に変更
3. **Transform** を設定：
   - **Position**: X=0, Y=0, Z=0
   - **Rotation**: X=90, Y=0, Z=0
   - **Scale**: X=0.8, Y=0.01, Z=0.1

### 8.2 判定ラインマテリアルの作成

1. 新しいマテリアルを作成
2. 名前を **"JudgmentLineMaterial"** に変更
3. **Base Map** の色を緑（#00FF00）、アルファを0.5に設定
4. 判定ラインに適用

---

## ステップ9: 動作確認

### 9.1 再生前のチェックリスト

- [ ] Conductor に AudioClip（End_Time.wav）が設定されている
- [ ] NotePoolManager に両プレハブが設定されている
- [ ] NoteSpawner に両プレハブが設定されている
- [ ] LaneVisualizer のレーンが表示されている（Gizmoで確認）
- [ ] TestSetup の Auto Setup がオンになっている

### 9.2 テスト実行

1. Unityエディタで **Play** ボタンをクリック
2. コンソールで以下を確認：
   - "テスト環境のセットアップ完了" メッセージ
   - エラーメッセージがないこと
3. Sceneビューで以下を確認：
   - レーンが正しく表示されている
   - ノーツが奥から手前に流れてくる
   - ノーツがZ=0付近で消える

### 9.3 トラブルシューティング

**ノーツが表示されない場合:**
- プレハブが正しく設定されているか確認
- ChartDataが生成されているか確認（コンソールログを確認）
- AudioClipが正しく設定されているか確認

**レーンが表示されない場合:**
- LaneVisualizerのマテリアルが設定されているか確認
- Sync With Conductorがオンになっているか確認
- Gizmoが有効になっているか確認（Sceneビューの右上）

**音楽が再生されない場合:**
- End_Time.wavが正しいパスに配置されているか確認
- AudioSourceのVolumeが0になっていないか確認
- AudioListenerがシーンに存在するか確認（通常はMain Cameraに付属）

---

## ステップ10: カスタマイズと調整

### 10.1 ノーツ速度の調整

Conductorの **Note Speed** を変更（デフォルト: 10）
- 値を大きくする: ノーツが速く流れる
- 値を小さくする: ノーツがゆっくり流れる

### 10.2 レーン幅の調整

Conductorの **Lane Width** を変更（デフォルト: 2）
- 値を大きくする: レーン間隔が広がる
- 値を小さくする: レーン間隔が狭まる

### 10.3 判定タイミングの調整

Conductorの **Judgment Z** を変更（デフォルト: 0）
- マイナス値: 判定が手前になる
- プラス値: 判定が奥になる

### 10.4 見た目の調整

LaneVisualizerで以下を調整：
- **Line Width**: レーンラインの太さ
- **Lane Color**: レーンの色と透明度
- **Near Width / Far Width**: 遠近感の強さ

---

## 付録A: スクリプト実行順序の設定

プロジェクト設定で以下の実行順序を推奨：

1. **Edit** → **Project Settings** → **Script Execution Order**
2. 以下の順序で設定：
   - Conductor: -100
   - NotePoolManager: -50
   - LaneVisualizer: -40
   - NoteSpawner: -30
   - NoteSpawnerTestSetup: -20
   - NoteController: デフォルト（0）

---

## 付録B: パフォーマンス最適化

### オブジェクトプールの調整

NotePoolManagerで：
- **Initial Pool Size**: 最初に生成するノーツ数（10〜20推奨）
- **Max Pool Size**: 最大プールサイズ（30〜50推奨）

### レンダリング最適化

1. **Edit** → **Project Settings** → **Quality**
2. URPアセットで **URP-Performant** を選択（低スペック環境）

---

## 付録C: デバッグツール

### コンテキストメニューの活用

各コンポーネントを右クリックして利用可能：
- **NoteSpawnerTestSetup**:
  - Start Test: テスト開始
  - Stop Test: テスト停止
  - Show Statistics: 統計情報表示
- **LaneVisualizer**:
  - Force Sync: 手動同期

### デバッグログの活用

NoteSpawnerの **Enable Debug Log** をオンにすると、詳細なログが出力されます。

---

## まとめ

このセットアップガイドに従うことで、Jirouプロジェクトの基本的な奥行き型リズムゲームシステムが動作する環境が構築できます。各コンポーネントは独立して動作するように設計されているため、必要に応じて個別に調整・改良することが可能です。

問題が発生した場合は、まずコンソールログを確認し、エラーメッセージに従って対処してください。それでも解決しない場合は、`docs/troubleshooting/` フォルダ内のトラブルシューティングガイドを参照してください。