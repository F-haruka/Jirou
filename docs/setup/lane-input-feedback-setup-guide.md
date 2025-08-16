# レーン入力フィードバックシステム Unityエディタ設定ガイド

## 前提条件

以下のコンポーネントが既に実装・設定されていることを確認してください：

- ✅ Conductor（シングルトンとして実装済み）
- ✅ LaneVisualizer（レーン表示設定済み - オプション）
- ✅ InputManager（キー入力設定済み - オプション）
- ✅ Universal Render Pipeline（URP）設定済み

## 実装ファイル

実装済みの2つのバージョンから選択できます：

- **LaneInputFeedback.cs** - 基本版（シンプルなCubeベース）
- **LaneInputFeedbackAdvanced.cs** - 高度版（遠近感対応の台形Mesh）

## 設定手順

### Step 1: スクリプトファイルの確認

1. **実装済みファイルの確認**
   - `Assets/_Jirou/Scripts/Visual/LaneInputFeedback.cs` (基本版)
   - `Assets/_Jirou/Scripts/Visual/LaneInputFeedbackAdvanced.cs` (高度版)

2. **Unityでコンパイル確認**
   - Consoleウィンドウでエラーがないことを確認

### Step 2: GameObjectの作成と配置

1. **Hierarchy上での作業**
   ```
   1. Stageオブジェクトを選択（なければ作成）
   2. 右クリック → Create Empty
   3. 新しいGameObjectの名前を「LaneInputFeedback」に変更
   4. Transform設定:
      - Position: (0, 0, 0)
      - Rotation: (0, 0, 0)
      - Scale: (1, 1, 1)
   ```

2. **階層構造の確認**
   ```
   Scene
   ├── GameManager
   │   ├── Conductor
   │   └── InputManager
   ├── Stage
   │   ├── LaneVisualizer
   │   ├── LaneInputFeedback ← 新規追加
   │   └── JudgmentLine
   └── Main Camera
   ```

### Step 3: コンポーネントの追加

1. **LaneInputFeedbackオブジェクトを選択**
2. **Inspectorウィンドウで「Add Component」ボタンをクリック**
3. **使用するバージョンを選択：**
   - **基本版**: 検索欄に「LaneInputFeedback」と入力して選択
   - **高度版**: 検索欄に「LaneInputFeedbackAdvanced」と入力して選択

### Step 4: Inspector設定

#### 4.1 Feedback Visual Settings

| パラメータ | 推奨値 | 説明 |
|-----------|--------|------|
| Feedback Intensity | 2.0 | 発光の強さ（1.0〜5.0） |
| Feedback Duration | 0.1 | 光る時間（秒） |
| Feedback Curve | (下図参照) | アニメーションカーブ |

**Feedback Curveの設定:**
1. Feedback Curveフィールドをクリック
2. Curve Editorが開く
3. プリセットから「Ease In-Out」を選択
4. または手動で以下の設定:
   - 始点: Time=0, Value=0
   - 中間: Time=0.5, Value=1
   - 終点: Time=1, Value=0

#### 4.2 Cube Settings

| パラメータ | 推奨値 | 説明 |
|-----------|--------|------|
| Cube Height | 0.5 | フィードバックCubeの高さ |
| Normal Alpha | 0.05 | 通常時の透明度（ほぼ透明） |
| Use Trapezoid Mesh | true | 台形Meshを使用（高度版のみ） |

#### 4.3 Color Settings

Lane Colorsの配列を展開して各レーンの色を設定:

1. **Size: 4** を確認
2. 各要素の色を設定:
   ```
   Element 0 (D): R=255, G=51, B=51, A=255  (赤)
   Element 1 (F): R=255, G=204, B=51, A=255 (黄)
   Element 2 (J): R=51, G=255, B=51, A=255  (緑)
   Element 3 (K): R=51, G=51, B=255, A=255  (青)
   ```

#### 4.4 Input Keys

Lane Keysの配列を確認:
```
Element 0: D
Element 1: F
Element 2: J
Element 3: K
```

#### 4.5 Debug Settings (オプション)

| パラメータ | 推奨値 | 説明 |
|-----------|--------|------|
| Show Debug Info | false | デバッグ情報の表示（開発時はtrue）|

### Step 5: Material設定（オプション但し推奨）

1. **Projectウィンドウで作業**
   ```
   1. Assets/_Jirou/Art/Materials フォルダを開く
   2. 右クリック → Create → Material
   3. 名前を「LaneFeedbackBaseMaterial」に変更
   ```

2. **Material設定**
   ```
   Inspector設定:
   - Shader: Universal Render Pipeline/Lit
   - Surface Options:
     * Surface Type: Transparent
     * Blending Mode: Alpha
     * Render Face: Both
   - Surface Inputs:
     * Base Map: 白色
     * Alpha: 0.1
   ```

3. **各レーン用Materialの作成（オプション）**
   ```
   1. LaneFeedbackBaseMaterialを選択
   2. Ctrl+D で複製を4つ作成
   3. 名前を変更:
      - LaneFeedbackMaterial_Red
      - LaneFeedbackMaterial_Yellow
      - LaneFeedbackMaterial_Green
      - LaneFeedbackMaterial_Blue
   4. 各Materialの Base Mapの色を設定
   ```

### Step 6: URP設定の確認

1. **Edit → Project Settings → Graphics**
   - Scriptable Render Pipeline Settings: 設定されていることを確認

2. **URP Asset設定の確認**
   ```
   1. Project → Assets/Settings
   2. URP-HighFidelity を選択
   3. Inspector確認:
      - Lighting → HDR: ✅ チェック
      - Quality → Anti Aliasing: FXAA または SMAA
   ```

3. **Color Space設定**
   ```
   Edit → Project Settings → Player
   - Other Settings → Rendering
   - Color Space: Linear（推奨）
   ```

### Step 7: 実行テスト

1. **Playモードでテスト**
   ```
   1. Playボタンを押す
   2. D、F、J、Kキーを押す
   3. 各レーンが光ることを確認
   ```

2. **確認項目チェックリスト**
   - [ ] 各キーで対応するレーンが光る
   - [ ] 光り方がスムーズ（カクつきなし）
   - [ ] 色が正しく表示される
   - [ ] 複数キー同時押しが機能する
   - [ ] キー長押しでパルスアニメーションが表示される
   - [ ] エラーがConsoleに表示されない
   - [ ] 高度版の場合：台形Meshが正しく表示される

### Step 8: 微調整

#### 8.1 光りが弱い場合
- Feedback Intensity を 3.0〜4.0 に上げる
- Normal Alpha を 0.1 に上げる

#### 8.2 光りが強すぎる場合
- Feedback Intensity を 1.0〜1.5 に下げる
- Color Settings の各色の明度を下げる

#### 8.3 反応が遅い場合
- Feedback Duration を 0.05 に短縮
- Feedback Curve を Linear に変更

### Step 9: Post Processing設定（オプション）

より美しい発光効果のために：

1. **Volume作成**
   ```
   1. Hierarchy → 右クリック → Volume → Global Volume
   2. Profile → New → 名前「GamePostProcess」
   ```

2. **Bloom効果追加**
   ```
   1. Add Override → Post-processing → Bloom
   2. 設定:
      - Intensity: 0.5
      - Threshold: 1.0
      - Scatter: 0.7
   ```

## トラブルシューティング

### 問題: Cubeが表示されない

**解決方法:**
1. Hierarchyで LaneInputFeedback → FeedbackCubes が生成されているか確認
2. 各 LaneFeedback_0〜3 のScaleが (0,0,0) になっていないか確認
3. Materialの Alpha値を確認

### 問題: 光らない・発光しない

**解決方法:**
1. URP設定で HDR が有効になっているか確認
2. MaterialのShaderが正しく設定されているか確認
3. Console にエラーが出ていないか確認

### 問題: 位置がおかしい

**解決方法:**
1. Conductorが正しく初期化されているか確認
2. LaneVisualizerと同じ親オブジェクトに配置されているか確認
3. Transform の Position が (0,0,0) になっているか確認

### 問題: パフォーマンスが低下する

**解決方法:**
1. Feedback Duration を短く（0.05秒程度）に設定
2. Quality Settings を調整
3. 不要なPost Processing効果を無効化

## デバッグモード

### デバッグ情報の表示

実装済みのスクリプトには既にデバッグ機能が含まれています：

1. **InspectorでDebug設定を有効化**
   - Show Debug Info: ✅ チェック

2. **Playモード中の表示内容**
   - 各レーンのステータス（READY/HOLDING）
   - アクティブなコルーチンの状態
   - Conductorの接続状態
   - SpawnZと遠近感スケール値（高度版）

3. **Gizmosによるプレビュー**
   - Scene ビューでレーン位置のワイヤーフレーム表示
   - 台形Meshの形状確認（高度版）

### 実装済み機能の詳細

**基本版（LaneInputFeedback）の機能：**
- シンプルなCubeベースのフィードバック
- キー入力検出と発光エフェクト
- ホールド入力のパルスアニメーション
- 判定タイプ別のフィードバック色

**高度版（LaneInputFeedbackAdvanced）の追加機能：**
- 遠近感対応の台形Mesh生成
- 動的なレーン設定更新（RefreshLaneConfiguration）
- より詳細なデバッグ表示
- 両面描画対応のMaterial設定

## パフォーマンス最適化Tips

1. **Batchingの活用**
   - 同じMaterialを使用するCubeはDynamic Batchingされる
   - Material数を最小限に抑える

2. **LOD設定**
   - カメラから遠い場合はフィードバックを簡略化
   - Quality Settingsでレベル別に調整

3. **オブジェクトプール**
   - フィードバックエフェクトを再利用
   - 生成・破棄のオーバーヘッドを削減

## 完成確認チェックリスト

- [ ] スクリプトファイルが正しい場所に存在している
- [ ] GameObjectが適切な階層に配置されている
- [ ] 使用するバージョン（基本版/高度版）を選択している
- [ ] Inspectorの全設定が完了している
- [ ] D、F、J、Kキーで各レーンが光る
- [ ] キー長押しでパルスアニメーションが動作する
- [ ] 同時押しが正しく動作する
- [ ] 判定連動フィードバック機能が動作する（InputManager連携時）
- [ ] エラーやワーニングが出ていない
- [ ] パフォーマンスが安定している（60FPS維持）
- [ ] 視覚的に満足できる効果が得られている

## 次のステップ

フィードバックシステムが正常に動作したら：

1. **エフェクトの強化**
   - パーティクルシステムの追加
   - より複雑なアニメーション

2. **判定連動**
   - Perfect/Great/Good/Missで異なる演出
   - コンボ数に応じた演出変化

3. **カスタマイズ機能**
   - プレイヤーが色を選択できる機能
   - エフェクト強度の調整オプション