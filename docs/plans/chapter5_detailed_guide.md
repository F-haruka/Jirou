# Chapter 5 詳細操作ガイド - 奥行き型レーンとノーツの実装

## 5.1. 台形レーンの作成（詳細版）

### ステップ1: レーンコンテナの準備

1. **Hierarchyウィンドウでの操作**：
   - Hierarchyウィンドウで右クリック
   - 「Create Empty」を選択
   - 作成されたGameObjectを「LaneContainer」に改名
   - Inspectorで Position を (0, 0, 0) に設定

2. **Scene Viewでの確認**：
   - Scene Viewで F キーを押してLaneContainerにフォーカス
   - Scene Viewの右上のGizmosボタンがONになっていることを確認

### ステップ2: Line Rendererを使った台形レーンの作成

#### 4つのレーン境界線を作成

**レーン境界線1（左端）の作成：**

1. **GameObjectの作成**：
   - LaneContainerを右クリック → 「Create Empty」
   - 「LaneBorder_Left」と命名

2. **Line Rendererの追加**：
   - Inspector → 「Add Component」をクリック
   - 検索窓に「Line Renderer」と入力
   - 「Line Renderer」を選択して追加

3. **Line Rendererの設定**：
   ```
   Material: Default-Line（または新規作成）
   Color: 白色（R:1, G:1, B:1, A:0.8）
   Width: 0.1
   Use World Space: ON
   Positions: 2個
   ```

4. **ポジション設定**：
   - Element 0（奥の点）: (-1, 0, 20)
   - Element 1（手前の点）: (-4, 0, 0)

**残りの境界線を同様に作成：**

- **LaneBorder_Center1**:
  - Element 0: (0, 0, 20)
  - Element 1: (-1.5, 0, 0)

- **LaneBorder_Center2**:
  - Element 0: (0, 0, 20)
  - Element 1: (1.5, 0, 0)

- **LaneBorder_Right**:
  - Element 0: (1, 0, 20)
  - Element 1: (4, 0, 0)

### ステップ3: レーンのマテリアル作成

1. **マテリアルフォルダの準備**：
   - Project → Assets/_Jirou/Art/Materials を右クリック
   - 「Create → Material」を選択

2. **レーン境界線用マテリアル**：
   - 名前: 「LaneBorderMaterial」
   - Rendering Mode: Transparent
   - Albedo: 白色
   - Emission: チェックON、色は薄い青（R:0.2, G:0.5, B:1, A:1）
   - Metallic: 0
   - Smoothness: 0.8

3. **マテリアルの適用**：
   - 各Line RendererのMaterialに作成したマテリアルをドラッグ&ドロップ

### ステップ4: 背景レーンエリアの作成

1. **レーンエリア用Quadの作成**：
   - LaneContainerを右クリック → 「3D Object → Quad」
   - 「LaneBackground」と命名

2. **Quadの設定**：
   ```
   Position: (0, -0.1, 10)
   Rotation: (90, 0, 0)
   Scale: (8, 20, 1)
   ```

3. **背景マテリアルの作成**：
   - 新規Material「LaneBackgroundMaterial」
   - Rendering Mode: Transparent
   - Albedo: 暗い紫（R:0.1, G:0.05, B:0.2, A:0.3）

## 5.2. ノーツプレハブの詳細作成手順

### Tapノーツプレハブの作成

#### ステップ1: 基本オブジェクトの作成

1. **Cubeオブジェクトの作成**：
   - Hierarchy → 右クリック → 「3D Object → Cube」
   - 「TapNote」に改名

2. **Transform設定**：
   ```
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1.5, 0.2, 0.3)
   ```

#### ステップ2: ビジュアル設定

1. **マテリアル作成**：
   - Assets/_Jirou/Art/Materials で右クリック
   - 「Create → Material」
   - 「TapNoteMaterial」と命名

2. **マテリアル設定**：
   ```
   Rendering Mode: Standard
   Albedo: 赤色（R:1, G:0.2, B:0.2）
   Emission: チェックON
   Emission Color: 薄い赤（R:0.8, G:0.1, B:0.1）
   Metallic: 0.2
   Smoothness: 0.8
   ```

3. **マテリアル適用**：
   - TapNoteのMesh RendererのMaterialにドラッグ&ドロップ

#### ステップ3: 物理コンポーネントの設定

1. **Colliderの設定**：
   - Inspector → Box Collider → 「Is Trigger」にチェック
   - Center: (0, 0, 0)
   - Size: (1, 1, 1)

2. **Rigidbodyの追加**：
   - Inspector → 「Add Component」
   - 「Rigidbody」を検索して追加
   - 「Is Kinematic」にチェック
   - 「Use Gravity」のチェックを外す

#### ステップ4: プレハブ化

1. **プレハブフォルダの準備**：
   - Assets/_Jirou/Prefabs/Notes フォルダを確認

2. **プレハブ作成**：
   - HierarchyのTapNoteを Assets/_Jirou/Prefabs/Notes にドラッグ
   - プレハブ作成完了
   - HierarchyからTapNoteを削除

### Holdノーツプレハブの作成

#### ステップ1: TapNoteベースの複製

1. **プレハブの複製**：
   - Project → TapNoteプレハブを右クリック
   - 「Duplicate」を選択
   - 「HoldNote」に改名

#### ステップ2: Holdノーツ用カスタマイズ

1. **プレハブ編集モード**：
   - HoldNoteプレハブをダブルクリック
   - プレハブ編集モードに入る

2. **色の変更**：
   - 新規Material「HoldNoteMaterial」を作成
   - Albedo: 黄色（R:1, G:1, B:0.2）
   - Emission Color: 薄い黄色（R:0.8, G:0.8, B:0.1）
   - マテリアルを適用

#### ステップ3: Holdトレイルの追加

1. **子オブジェクトの作成**：
   - HoldNote（ルート）を右クリック → 「Create Empty」
   - 「HoldTrail」と命名

2. **Line Rendererの設定**：
   - HoldTrailに「Line Renderer」コンポーネントを追加
   - 設定値：
   ```
   Material: HoldNoteMaterial
   Width: 1.2
   Use World Space: OFF
   Positions: 2
   Element 0: (0, 0, 0)
   Element 1: (0, 0, -2)  // 初期長さ
   ```

3. **プレハブ保存**：
   - Ctrl+S でプレハブを保存
   - Scene Viewの「◀」ボタンでプレハブ編集モードを終了

## 5.3. 判定ラインとエフェクトの設定

### 判定ライン表示の作成

#### ステップ1: 判定ライン本体

1. **Quadオブジェクトの作成**：
   - Hierarchy → 右クリック → 「3D Object → Quad」
   - 「JudgmentLine」に改名

2. **Transform設定**：
   ```
   Position: (0, 0, 0)
   Rotation: (90, 0, 0)
   Scale: (8, 0.1, 1)
   ```

3. **マテリアル設定**：
   - 新規Material「JudgmentLineMaterial」
   - Rendering Mode: Transparent
   - Albedo: 白色（R:1, G:1, B:1, A:0.6）
   - Emission: チェックON、白色

#### ステップ2: 判定ゾーンの作成

**各レーン用判定ゾーン（4個）の作成：**

1. **ゾーンオブジェクトの作成**：
   - Hierarchy → 右クリック → 「Create Empty」
   - 「JudgmentZone_0」と命名（レーン0用）

2. **Transform設定**：
   ```
   Position: (-3, 0.5, 0)  // レーン0のX座標
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   ```

3. **Box Colliderの追加**：
   - Inspector → 「Add Component」→「Box Collider」
   - 「Is Trigger」にチェック
   - Center: (0, 0, 0)
   - Size: (1.5, 1, 3)  // Z方向に余裕を持たせる

4. **残りのゾーン作成**：
   同様に以下の座標で作成：
   - JudgmentZone_1: Position (-1, 0.5, 0)
   - JudgmentZone_2: Position (1, 0.5, 0)
   - JudgmentZone_3: Position (3, 0.5, 0)

### ヒットエフェクト用パーティクルシステム

#### ステップ1: パーティクルシステムの作成

1. **パーティクルオブジェクトの作成**：
   - Hierarchy → 右クリック → 「Effects → Particle System」
   - 「HitEffect」と命名

2. **基本設定**：
   ```
   Duration: 0.2
   Start Lifetime: 0.5
   Start Speed: 3
   Start Size: 0.3
   Start Color: 白色
   Max Particles: 20
   ```

3. **Emission設定**：
   ```
   Rate over Time: 0
   Bursts: 1個追加
   Time: 0
   Count: 15
   ```

4. **Shape設定**：
   ```
   Shape: Circle
   Radius: 0.5
   ```

5. **プレハブ化**：
   - Assets/_Jirou/Prefabs/Effects にドラッグしてプレハブ化

#### ステップ2: 判定別エフェクトの作成

1. **Perfect用エフェクト**：
   - HitEffectを複製
   - 「PerfectEffect」と命名
   - Start Color: 黄色（R:1, G:1, B:0.2）
   - Count: 20

2. **Great用エフェクト**：
   - 「GreatEffect」と命名
   - Start Color: 緑色（R:0.2, G:1, B:0.2）
   - Count: 15

3. **Good用エフェクト**：
   - 「GoodEffect」と命名
   - Start Color: 青色（R:0.2, G:0.5, B:1）
   - Count: 10

## 5.4. シーン全体の確認とテスト

### 最終確認チェックリスト

1. **レーン表示の確認**：
   - [ ] 4本の境界線が台形状に配置されている
   - [ ] レーン背景が半透明で表示されている
   - [ ] 奥行き感が正しく表現されている

2. **プレハブの確認**：
   - [ ] TapNoteプレハブが正しく作成されている
   - [ ] HoldNoteプレハブにトレイルが含まれている
   - [ ] 両プレハブがPrefabsフォルダに保存されている

3. **判定システムの確認**：
   - [ ] 判定ラインが適切な位置に表示されている
   - [ ] 4つの判定ゾーンがレーンに対応している
   - [ ] エフェクトプレハブが作成されている

### シーンの保存

1. **シーン保存**：
   - File → Save Scene
   - Assets/_Jirou/Scenes/GameScene として保存

2. **Project設定の確認**：
   - File → Build Settings
   - GameSceneがScenes In Buildに含まれているか確認

これで Chapter 5 の詳細な実装が完了しました。次は Claude Code を使ったスクリプト実装に進むことができます。