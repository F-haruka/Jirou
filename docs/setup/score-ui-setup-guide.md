# スコアUIシステム セットアップガイド

## 概要

本ドキュメントでは、Jirouゲームにスコア表示UIシステムを実装するための詳細な手順を説明します。この手順に従うことで、スコア、判定カウント、コンボ表示を含む完全なUIシステムをセットアップできます。

## 前提条件

- Unity 6.0 LTS または Unity 2022.3 LTS
- TextMeshPro パッケージ（Unityに組み込み済み）
- Jirouプロジェクトの基本構造が構築済み

## セットアップ手順

### ステップ1: UI Canvas の作成

#### 1.1 メインCanvasの作成

1. **Hierarchyウィンドウ**で右クリック
2. **UI > Canvas** を選択
3. 作成されたCanvasの名前を `ScoreUICanvas` に変更

#### 1.2 Canvas設定

1. `ScoreUICanvas` を選択
2. **Inspectorウィンドウ**で以下を設定：
   - **Canvas Scaler コンポーネント**
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: X=`1920`, Y=`1080`
     - Screen Match Mode: `0.5`
   - **Canvas コンポーネント**
     - Render Mode: `Screen Space - Overlay`
     - Sort Order: `100`

#### 1.3 EventSystemの確認

1. HierarchyにEventSystemが自動生成されていることを確認
2. なければ、右クリック > **UI > Event System** で作成

### ステップ2: UI階層構造の構築

#### 2.1 トップパネルの作成

1. `ScoreUICanvas` を右クリック
2. **UI > Panel** を選択し、名前を `TopPanel` に変更
3. **RectTransform設定**：
   - Anchor Preset: `Top Stretch` (Shift+Altを押しながら選択)
   - Height: `100`
   - Top: `10`
   - Left: `20`
   - Right: `20`

#### 2.2 スコア表示の作成

1. `TopPanel` を右クリック
2. **Create Empty** を選択し、名前を `ScoreContainer` に変更
3. **RectTransform設定**：
   - Anchor Preset: `Top Left`
   - Width: `300`
   - Height: `80`
   - Pos X: `10`
   - Pos Y: `-40`

4. `ScoreContainer` を右クリック
5. **UI > Text - TextMeshPro** を選択
6. 初回の場合、TMP Essentialsのインポートダイアログが表示されるので `Import TMP Essentials` をクリック
7. 作成されたTextの名前を `ScoreLabel` に変更
8. **TextMeshPro設定**：
   - Text: `SCORE`
   - Font Size: `24`
   - Alignment: `Left` `Middle`
   - Color: `#FFD700` (ゴールド)

9. `ScoreContainer` を右クリック > **UI > Text - TextMeshPro**
10. 名前を `ScoreValue` に変更
11. **TextMeshPro設定**：
    - Text: `0000000`
    - Font Size: `48`
    - Alignment: `Left` `Middle`
    - Color: `#FFFFFF` (白)
    - Position: X=`100`, Y=`0`

### ステップ3: 判定カウント表示の作成

#### 3.1 判定パネルの作成

1. `ScoreUICanvas` を右クリック > **Create Empty**
2. 名前を `JudgmentPanel` に変更
3. **RectTransform設定**：
   - Anchor Preset: `Top Left`
   - Width: `400`
   - Height: `100`
   - Pos X: `30`
   - Pos Y: `-150`

#### 3.2 判定カウント要素の作成

各判定タイプ（Perfect, Great, Good, Miss）について以下を繰り返す：

1. `JudgmentPanel` を右クリック > **Create Empty**
2. 名前を `PerfectContainer` に変更（他: GreatContainer, GoodContainer, MissContainer）
3. **Horizontal Layout Group** コンポーネントを追加
   - Spacing: `10`
   - Child Alignment: `Middle Left`

4. 各Containerに2つのTextMeshProを追加：
   - **Label Text**:
     - 名前: `PerfectLabel`（他も同様）
     - Text: `Perfect:`
     - Font Size: `20`
     - Color: 
       - Perfect: `#FFD700` (ゴールド)
       - Great: `#00FF00` (緑)
       - Good: `#00BFFF` (水色)
       - Miss: `#FF0000` (赤)
   
   - **Count Text**:
     - 名前: `PerfectCount`（他も同様）
     - Text: `000`
     - Font Size: `24`
     - 同じ色設定

5. 各Containerの位置を調整：
   - PerfectContainer: Pos Y=`0`
   - GreatContainer: Pos Y=`-30`
   - GoodContainer: Pos Y=`0`, Pos X=`200`
   - MissContainer: Pos Y=`-30`, Pos X=`200`

### ステップ4: コンボ表示の作成

#### 4.1 コンボパネルの作成

1. `ScoreUICanvas` を右クリック > **Create Empty**
2. 名前を `ComboPanel` に変更
3. **RectTransform設定**：
   - Anchor Preset: `Top Center`
   - Width: `300`
   - Height: `150`
   - Pos Y: `-250`

4. **Canvas Group** コンポーネントを追加（フェード制御用）

#### 4.2 コンボテキストの作成

1. `ComboPanel` に **TextMeshPro** を2つ追加：
   
   - **ComboLabel**:
     - Text: `COMBO`
     - Font Size: `30`
     - Alignment: `Center` `Middle`
     - Color: `#FFFF00` (黄色)
     - Pos Y: `30`
   
   - **ComboNumber**:
     - Text: `0`
     - Font Size: `60`
     - Alignment: `Center` `Middle`
     - Color: `#FFFF00`
     - Pos Y: `-20`

### ステップ5: エフェクト用オブジェクトの準備

#### 5.1 UIEffectPoolの作成

1. Hierarchyで **Create Empty**
2. 名前を `UIEffectPool` に変更
3. **UIEffectPool** スクリプトをアタッチ

#### 5.2 判定ポップアッププレハブの作成

1. `ScoreUICanvas` の下に **Create Empty**
2. 名前を `JudgmentPopup` に変更
3. **Canvas Group** コンポーネントを追加
4. **TextMeshPro** を子として追加
5. **JudgmentPopup** スクリプトをアタッチ
6. プレハブ化：
   - `JudgmentPopup` をProjectウィンドウの `Assets/_Jirou/Prefabs/UI/` にドラッグ
   - Hierarchyから元のオブジェクトを削除

### ステップ6: スクリプトコンポーネントの設定

#### 6.1 ScoreManagerの設定

1. Hierarchyで **Create Empty**
2. 名前を `ScoreManager` に変更
3. **ScoreManager** スクリプトをアタッチ
4. Inspectorで以下を設定：
   - Max Score: `9999999`
   - Base Score Per Note: `1000`
   - Perfect Multiplier: `1.0`
   - Great Multiplier: `0.5`
   - Good Multiplier: `0.1`
   - Miss Multiplier: `0`
   - Combo Score Bonus: `10`

#### 6.2 ScoreUIManagerの設定

1. `ScoreUICanvas` に **ScoreUIManager** スクリプトをアタッチ
2. Inspectorでの参照設定：
   
   **Score Display セクション**:
   - Score Text: `ScoreValue` をドラッグ&ドロップ
   - Score Display: 後で設定（ScoreDisplayコンポーネント追加後）
   
   **Judgment Counts セクション**:
   - Perfect Count Text: `PerfectCount` をドラッグ
   - Great Count Text: `GreatCount` をドラッグ
   - Good Count Text: `GoodCount` をドラッグ
   - Miss Count Text: `MissCount` をドラッグ
   
   **Combo Display セクション**:
   - Combo Text: `ComboNumber` をドラッグ
   - Combo Container: `ComboPanel` をドラッグ

#### 6.3 追加コンポーネントの設定

1. **ScoreDisplay** を `ScoreValue` にアタッチ
   - Score Text: 自身の TextMeshProUGUI を自動取得
   - Animation Duration: `0.3`
   - Enable Scale Animation: `✓`

2. **JudgmentCountDisplay** を `JudgmentPanel` にアタッチ
   - 各Count Textを設定

3. **ComboDisplay** を `ComboPanel` にアタッチ
   - Combo Number Text: `ComboNumber`
   - Combo Label Text: `ComboLabel`
   - Canvas Group: 自動取得

4. **UIPerformanceOptimizer** を `ScoreUICanvas` にアタッチ
   - Target Frame Rate: `60`
   - Use Cached Strings: `✓`

### ステップ7: デバッグ機能の設定

#### 7.1 ScoreUIDebuggerの追加

1. `ScoreUICanvas` に **ScoreUIDebugger** スクリプトをアタッチ
2. Inspectorで設定：
   - Enable Debug Keys: `✓`
   - Show Debug Info: `✓`
   - UI Manager: 自動取得（同じGameObject）

### ステップ8: テストと動作確認

#### 8.1 Playモードでのテスト

1. **Play** ボタンをクリックしてPlayモードに入る
2. 以下のキーでテスト：

   **判定テスト**:
   - `1` キー: Perfect判定
   - `2` キー: Great判定
   - `3` キー: Good判定
   - `4` キー: Miss判定

   **機能テスト**:
   - `Space` キー: スコア+1000
   - `C` キー: コンボ+10
   - `B` キー: コンボブレイク
   - `R` キー: 全リセット
   - `T` キー: ストレステスト開始/停止
   - `F1` キー: デバッグ表示切り替え

#### 8.2 動作確認項目

- [ ] スコアが正しく加算される
- [ ] 判定カウントが増加する
- [ ] コンボが継続/ブレイクする
- [ ] アニメーションが滑らかに動作する
- [ ] 60FPSを維持している

### ステップ9: パフォーマンス最適化

#### 9.1 Canvas最適化

1. `ScoreUICanvas` を選択
2. **Canvas** コンポーネント:
   - Pixel Perfect: `☐` (無効化)

#### 9.2 TextMeshPro最適化

すべてのTextMeshProオブジェクトで：
1. **Raycast Target**: `☐` (無効化)
2. **Extra Settings** > **Margins** を最小限に

### ステップ10: プレハブ化

#### 10.1 ScoreUICanvasのプレハブ化

1. `ScoreUICanvas` を選択
2. Projectウィンドウの `Assets/_Jirou/Prefabs/UI/` にドラッグ
3. プレハブとして保存

#### 10.2 ScoreManagerのプレハブ化

1. `ScoreManager` を選択
2. 同様にプレハブ化

## トラブルシューティング

### よくある問題と解決方法

#### テキストが表示されない

**原因**: TextMeshProのマテリアルが正しく設定されていない
**解決**: 
1. TextMeshProコンポーネントの Material Preset を確認
2. デフォルトマテリアルを使用

#### スコアアニメーションがカクつく

**原因**: 更新頻度が高すぎる
**解決**:
1. UIPerformanceOptimizerの設定を確認
2. Limit Update Rate を有効化

#### コンボが表示されない

**原因**: ComboContainerが非アクティブ
**解決**:
1. ComboPanel の初期状態を確認
2. ScoreUIManagerの Auto Hide Threshold を調整

#### メモリ使用量が増加し続ける

**原因**: オブジェクトプールが機能していない
**解決**:
1. UIEffectPoolが正しく設定されているか確認
2. プレハブ参照が正しいか確認

## パーティクルエフェクトの追加（オプション）

### コンボエフェクトの作成

1. **GameObject > Effects > Particle System** でパーティクルを作成
2. 以下の設定を調整：
   - Duration: `1`
   - Start Lifetime: `0.5`
   - Start Speed: `5`
   - Start Size: `0.5`
   - Emission > Rate over Time: `0`
   - Emission > Bursts: Count `20`
   - Shape: Circle
   - Renderer > Render Mode: `Billboard`

3. 作成したパーティクルを `ComboPanel` の子として配置
4. ComboEffectControllerの該当スロットに設定

## 次のステップ

スコアUIシステムのセットアップが完了したら、以下の機能を追加できます：

1. **楽曲終了時のリザルト画面**
2. **ハイスコアの保存と表示**
3. **オンラインランキング連携**
4. **カスタムスキン機能**
5. **マルチプレイヤー対応**

## まとめ

このガイドに従うことで、完全に機能するスコアUIシステムをUnityエディタ上で構築できます。デバッグ機能を活用してテストを行い、パフォーマンスを確認しながら調整を進めてください。