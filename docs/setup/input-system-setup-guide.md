# Jirou 入力システム・判定セットアップガイド

## 概要

このガイドは、Jirouプロジェクトでキーボード入力（D、F、J、K）を使用してノーツを判定させるための詳細なセットアップ手順を説明します。Chapter 7の奥行き対応入力システムの実装を前提としています。

## 前提条件

以下のコンポーネントが既に実装済みであることを確認してください：
- ✅ Conductor（タイミング管理）
- ✅ NoteSpawner（ノーツ生成）
- ✅ NotePoolManager（オブジェクトプール）
- ✅ LaneVisualizer（レーン表示）
- ✅ NoteController（ノーツ制御）
- ✅ InputManager（入力管理）
- ✅ JudgmentZone（判定ゾーン）
- ✅ InputManagerTestSetup（テストセットアップ）

---

## パート1: 判定システムの基本セットアップ

### ステップ1: 判定ゾーンの作成

#### 1.1 判定ゾーンコンテナの作成

1. **Hierarchyウィンドウ**で右クリック → **Create Empty**
2. 名前を **"JudgmentZones"** に変更
3. **Transform**を設定：
   - **Position**: X=0, Y=0, Z=0
   - **Rotation**: X=0, Y=0, Z=0
   - **Scale**: X=1, Y=1, Z=1

#### 1.2 各レーンの判定ゾーン作成（4つ）

各レーン（0〜3）に対して以下を実行：

**レーン0（左端）の判定ゾーン：**
1. JudgmentZonesを右クリック → **Create Empty**
2. 名前を **"JudgmentZone_Lane0"** に変更
3. **Transform**を設定：
   - **Position**: X=-3, Y=0.5, Z=0
   - **Rotation**: X=0, Y=0, Z=0
   - **Scale**: X=1, Y=1, Z=1

**レーン1の判定ゾーン：**
1. 同様に作成、名前を **"JudgmentZone_Lane1"** に変更
2. **Position**: X=-1, Y=0.5, Z=0

**レーン2の判定ゾーン：**
1. 同様に作成、名前を **"JudgmentZone_Lane2"** に変更
2. **Position**: X=1, Y=0.5, Z=0

**レーン3（右端）の判定ゾーン：**
1. 同様に作成、名前を **"JudgmentZone_Lane3"** に変更
2. **Position**: X=3, Y=0.5, Z=0

#### 1.3 JudgmentZoneコンポーネントの追加

各判定ゾーンオブジェクト（JudgmentZone_Lane0〜3）に対して：

1. オブジェクトを選択
2. **Add Component** → **Jirou** → **Gameplay** → **JudgmentZone**
3. **Add Component** → **Physics** → **Box Collider**

#### 1.4 Box Colliderの設定

各判定ゾーンのBox Colliderを以下のように設定：

- **Is Trigger**: ✅ オン（必須）
- **Center**: X=0, Y=0, Z=0
- **Size**: X=1.5, Y=2, Z=3
  - X: レーンの幅（ノーツが通過する範囲）
  - Y: 高さ（ノーツの上下の判定範囲）
  - Z: 奥行き（判定タイミングの前後の余裕）

#### 1.5 JudgmentZoneコンポーネントの設定

各JudgmentZoneコンポーネントで以下を設定：

**判定範囲設定：**
- **Perfect Range**: 0.5（Z軸での完璧判定範囲）
- **Great Range**: 1.0（Z軸での良判定範囲）
- **Good Range**: 1.5（Z軸でのグッド判定範囲）

**レーン設定：**
- **Lane Index**: 対応するレーン番号（0〜3）を設定
  - JudgmentZone_Lane0 → 0
  - JudgmentZone_Lane1 → 1
  - JudgmentZone_Lane2 → 2
  - JudgmentZone_Lane3 → 3

**デバッグ設定：**
- **Debug Mode**: ✅ オン（開発中は推奨）
- **Show Gizmos**: ✅ オン（判定範囲の可視化）

---

### ステップ2: InputManagerのセットアップ

#### 2.1 InputManagerオブジェクトの作成

1. **Hierarchyウィンドウ**で右クリック → **Create Empty**
2. 名前を **"InputManager"** に変更
3. **Transform**を設定：
   - **Position**: X=0, Y=0, Z=0

#### 2.2 InputManagerコンポーネントの追加

1. InputManagerオブジェクトを選択
2. **Add Component** → **Jirou** → **Gameplay** → **InputManager**

#### 2.3 InputManagerの設定

**入力キー設定（自動設定済み）：**
- Element 0: **D** (KeyCode.D)
- Element 1: **F** (KeyCode.F)
- Element 2: **J** (KeyCode.J)
- Element 3: **K** (KeyCode.K)

**判定ゾーン配列の設定：**
1. **Judgment Zones** のサイズを **4** に設定
2. 各要素にHierarchyから対応する判定ゾーンをドラッグ&ドロップ：
   - Element 0: JudgmentZone_Lane0
   - Element 1: JudgmentZone_Lane1
   - Element 2: JudgmentZone_Lane2
   - Element 3: JudgmentZone_Lane3

**判定設定：**
- **Min Time Between Hits**: 0.1（連打防止のクールダウン時間）
- **Enable Debug GUI**: ✅ オン（デバッグ表示）

**エフェクト設定（オプション）：**
- **Hit Effect Prefab**: 後で作成するヒットエフェクトプレハブを設定
- **Effect Spawn Offset**: Y=0.2（エフェクトの表示位置調整）

---

### ステップ3: ヒットエフェクトの作成（オプション）

#### 3.1 Perfectエフェクトプレハブの作成

1. **Hierarchyウィンドウ**で右クリック → **Effects** → **Particle System**
2. 名前を **"PerfectHitEffect"** に変更
3. **Particle System**の設定：
   - **Duration**: 0.5
   - **Looping**: オフ
   - **Start Lifetime**: 0.5
   - **Start Speed**: 5
   - **Start Size**: 0.3
   - **Start Color**: 黄色（#FFFF00）
   - **Emission** → **Rate over Time**: 0
   - **Emission** → **Bursts**: 
     - Count: 20
     - Time: 0
   - **Shape** → **Shape**: Sphere
   - **Shape** → **Radius**: 0.1

4. `Assets/_Jirou/Prefabs/Effects`フォルダにドラッグしてプレハブ化
5. Hierarchyから削除

#### 3.2 その他の判定エフェクト

同様の手順で以下を作成：
- **GreatHitEffect**（緑色 #00FF00）
- **GoodHitEffect**（青色 #0080FF）
- **MissEffect**（赤色 #FF0000）

---

## パート2: テスト環境のセットアップ

### ステップ4: InputManagerTestSetupの設定

#### 4.1 テストセットアップオブジェクトの作成

1. **Hierarchyウィンドウ**で右クリック → **Create Empty**
2. 名前を **"InputTestSetup"** に変更
3. **Position**: X=0, Y=0, Z=0

#### 4.2 InputManagerTestSetupコンポーネントの追加

1. InputTestSetupオブジェクトを選択
2. **Add Component** → **Jirou** → **Gameplay** → **InputManagerTestSetup**

#### 4.3 テストセットアップの設定

**自動セットアップ設定：**
- **Auto Setup On Start**: ✅ オン
- **Create Default Prefabs**: ✅ オン（プレハブが未作成の場合）

**生成パターン設定：**
- **Note Generation Pattern**: Sequential（順番に生成）
  - Sequential: レーンを順番に
  - Random: ランダムなレーンに
  - All Lanes: 全レーン同時に

**レーン設定：**
- **Lane Count**: 4
- **Lane X Positions**: -3, -1, 1, 3（自動設定）
- **Lane Width**: 1.5
- **Lane Height**: 2.0

**判定ゾーン設定：**
- **Judgment Z**: 0（判定ライン位置）
- **Perfect Range**: 0.5
- **Great Range**: 1.0
- **Good Range**: 1.5

**ノーツ生成設定：**
- **Spawn Z**: 20（ノーツ生成位置）
- **Note Y**: 0.5（ノーツの高さ）
- **Note Speed**: 10（ノーツの移動速度）

---

## パート3: 実行とテスト

### ステップ5: 統合テストの実行

#### 5.1 実行前チェックリスト

以下を確認してください：

**必須コンポーネント：**
- [x] Conductorが存在し、AudioClipが設定されている
- [x] NoteSpawnerが存在し、ChartDataまたはテスト譜面が設定されている
- [x] NotePoolManagerが存在し、プレハブが設定されている
- [x] InputManagerが存在する
- [x] 4つのJudgmentZoneが作成され、正しい位置にある
- [x] InputManagerのJudgment Zones配列に4つの判定ゾーンが設定されている

**オプション：**
- [x] LaneVisualizerでレーンが表示されている
- [x] ヒットエフェクトプレハブが作成・設定されている

#### 5.2 Playモードでのテスト

1. **Playボタン**をクリックしてゲームを開始
2. **コンソールウィンドウ**で以下を確認：
   ```
   [InputManager] 初期化完了 - 4レーン
   [InputManagerTestSetup] テスト環境構築完了
   ```

#### 5.3 キーボード操作テスト

**基本操作：**
- **D キー**: レーン0（左端）のノーツを判定
- **F キー**: レーン1のノーツを判定
- **J キー**: レーン2のノーツを判定
- **K キー**: レーン3（右端）のノーツを判定

**テスト用操作（InputManagerTestSetup使用時）：**
- **1〜4 キー**: 対応レーンにTapノーツを生成
- **F1〜F4 キー**: 対応レーンにHoldノーツを生成
- **R キー**: シーンをリセット
- **Space キー**: 音楽の再生/停止
- **P キー**: ポーズ/再開

#### 5.4 判定の確認

1. **Gameビュー**または**Sceneビュー**で以下を確認：
   - ノーツが奥（Z=20）から手前（Z=0）に流れてくる
   - 判定ライン（Z=0）付近でキーを押すとノーツが消える
   - 判定タイミングによって異なるエフェクトが表示される

2. **デバッグGUI**（画面左上）で以下を確認：
   ```
   === Input Manager Debug ===
   Lane 0 (D): [状態表示]
   Lane 1 (F): [状態表示]
   Lane 2 (J): [状態表示]
   Lane 3 (K): [状態表示]
   Notes in zones: [ゾーン内のノーツ数]
   ```

---

## パート4: 判定精度の調整

### ステップ6: タイミング調整

#### 6.1 判定範囲の微調整

各JudgmentZoneコンポーネントで調整可能：

**厳しい判定（音ゲー上級者向け）：**
- Perfect Range: 0.3
- Great Range: 0.6
- Good Range: 1.0

**標準判定（デフォルト）：**
- Perfect Range: 0.5
- Great Range: 1.0
- Good Range: 1.5

**緩い判定（初心者向け）：**
- Perfect Range: 0.8
- Great Range: 1.5
- Good Range: 2.5

#### 6.2 入力遅延の補正

Conductorコンポーネントで調整：
- **Input Offset**: 音楽と入力のずれを補正（ミリ秒単位）
  - プラス値: 判定を遅らせる
  - マイナス値: 判定を早める

#### 6.3 視覚的フィードバックの調整

InputManagerで調整：
- **Visual Feedback Delay**: エフェクト表示のタイミング調整
- **Effect Duration**: エフェクトの表示時間

---

## パート5: Hold ノーツの設定

### ステップ7: Holdノーツ判定の設定

#### 7.1 Holdノーツ用の設定

NoteControllerコンポーネント（各ノーツプレハブ）で：
- **Hold Required Accuracy**: 0.8（80%以上の時間押し続ける必要）
- **Hold Check Interval**: 0.1（チェック間隔）

#### 7.2 InputManagerのHold設定

- **Hold Grace Period**: 0.1（Hold開始の猶予時間）
- **Hold Release Grace**: 0.05（Hold終了の猶予時間）

---

## トラブルシューティング

### 問題1: キーを押してもノーツが判定されない

**確認事項：**
1. InputManagerのJudgment Zones配列が正しく設定されているか
2. 各JudgmentZoneのBox ColliderのIs Triggerがオンか
3. ノーツプレハブにNoteControllerコンポーネントがあるか
4. ノーツプレハブのBox ColliderのIs Triggerがオンか

**解決策：**
- JudgmentZoneのサイズを大きくする（Size.Z を 5 程度に）
- Debug ModeをオンにしてSceneビューでギズモを確認

### 問題2: 判定タイミングがずれる

**確認事項：**
1. Conductorの設定でInput Offsetを調整
2. 判定範囲（Perfect/Great/Good Range）を調整

**解決策：**
```
// Conductorで調整
Input Offset = 実測遅延時間（ms）

// 例：音が遅れて聞こえる場合
Input Offset = -50 // 50ms早く判定
```

### 問題3: Holdノーツが正しく判定されない

**確認事項：**
1. NoteControllerのIsHoldNoteメソッドが正しく動作しているか
2. InputManagerのHandleKeyHoldメソッドが呼ばれているか

**解決策：**
- Hold Required Accuracyを下げる（0.6程度に）
- Hold Grace Periodを増やす（0.2程度に）

### 問題4: 複数のノーツが同時に判定される

**確認事項：**
1. Min Time Between Hitsの値が適切か
2. JudgmentZoneのGetClosestNoteが正しく動作しているか

**解決策：**
- Min Time Between Hitsを0.2に増やす
- 判定範囲を狭める

---

## デバッグツール

### コンソールコマンド（開発中）

```csharp
// InputManagerのpublicメソッド（スクリプトから呼び出し可能）
InputManager.Instance.ForceJudge(laneIndex, judgmentType);
InputManager.Instance.ShowDebugInfo();
InputManager.Instance.ResetStatistics();
```

### ギズモ表示

**Sceneビューで確認できる要素：**
- 判定ゾーンの範囲（緑の箱）
- Perfect判定範囲（黄色の線）
- Great判定範囲（青の線）
- Good判定範囲（赤の線）
- ノーツの軌道（白い線）

### 統計情報の表示

InputManagerTestSetupで右クリック → **Show Statistics**：
- 総判定数
- 判定別の内訳（Perfect/Great/Good/Miss）
- 最大コンボ数
- 判定精度（％）

---

## パフォーマンス最適化

### 判定処理の最適化

1. **判定ゾーンの最適化：**
   - 不要な物理演算を無効化
   - Rigidbodyは使用しない（トリガーのみ）

2. **エフェクトの最適化：**
   - パーティクル数を抑える（20〜30個）
   - エフェクトの寿命を短くする（0.5秒以内）

3. **入力処理の最適化：**
   - Update内での処理を最小限に
   - 判定済みノーツの即座の削除

---

## 完成チェックリスト

### 必須項目
- [ ] 4つの判定ゾーンが正しい位置に配置されている
- [ ] InputManagerが判定ゾーンを認識している
- [ ] D、F、J、Kキーで各レーンの判定ができる
- [ ] Perfect、Great、Good、Missの判定が正しく行われる
- [ ] ノーツが判定後に消える

### 推奨項目
- [ ] 判定エフェクトが表示される
- [ ] デバッグGUIで状態が確認できる
- [ ] Holdノーツの判定が機能する
- [ ] 判定音が再生される（実装時）

### オプション項目
- [ ] コンボシステムが動作する（ScoreManager実装後）
- [ ] スコア表示が更新される（UIManager実装後）
- [ ] 判定テキストが表示される（UIManager実装後）

---

## まとめ

このガイドに従うことで、Jirouプロジェクトの入力システムと判定システムが完全に動作するようになります。キーボードのD、F、J、Kキーを使用して、奥から流れてくるノーツを正確なタイミングで判定できるシステムが構築されます。

セットアップ完了後は、InputManagerTestSetupを使用して様々なパターンでテストを行い、判定精度や視覚的フィードバックを調整してください。

問題が発生した場合は、トラブルシューティングセクションを参照するか、デバッグモードを有効にしてコンソールログを確認してください。