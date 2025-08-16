# 判定フィードバックシステム セットアップガイド

## 概要
このガイドでは、判定フィードバックシステム（判定テキスト表示とエフェクト）をUnityエディタで設定する手順を説明します。

## 必要なコンポーネント
- JudgmentTextDisplay.cs（作成済み）
- Billboard.cs（作成済み）
- JudgmentEffectManager.cs（作成済み）
- TextMeshPro（Unity Package）

## セットアップ手順

### Phase 1: 判定テキスト表示システムの設定

#### Step 1: TextMeshProのインポート確認
1. **メニューから確認**
   ```
   Window > TextMeshPro > Import TMP Essential Resources
   ```
   - すでにインポート済みの場合はこのステップをスキップ

#### Step 2: 判定テキストプレハブの作成

1. **空のGameObjectを作成**
   ```
   Hierarchy右クリック > Create Empty
   名前: "JudgmentTextPrefab"
   ```

2. **TextMeshProコンポーネントを追加**
   ```
   JudgmentTextPrefabを選択
   Inspector > Add Component > "TextMeshPro - Text"
   ```

3. **TextMeshProの設定**
   - **Text Input**: "Perfect"（テスト用）
   - **Font Asset**: NotoSansJP-Bold SDF（または任意の日本語フォント）
   - **Font Size**: 48
   - **Alignment**: Center & Middle
   - **Color**: White（動的に変更されるため）
   - **Font Style**: Bold

4. **RectTransformの調整**
   ```
   Width: 300
   Height: 100
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   ```

5. **Billboardコンポーネントを追加**
   ```
   Add Component > Scripts > Jirou.UI > Billboard
   設定:
   - Use Main Camera: ✓（チェック）
   - Lock Y Axis: □（チェックなし）
   - Lock X Axis: □（チェックなし）
   - Lock Z Axis: □（チェックなし）
   ```

6. **プレハブとして保存**
   ```
   JudgmentTextPrefabをドラッグ
   保存先: Assets/_Jirou/Prefabs/UI/JudgmentTextPrefab.prefab
   ```
   - Prefabs/UIフォルダがない場合は作成

7. **シーンから削除**
   ```
   Hierarchy内のJudgmentTextPrefabを削除
   ```

#### Step 3: JudgmentTextDisplayオブジェクトの作成

1. **空のGameObjectを作成**
   ```
   Hierarchy右クリック > Create Empty
   名前: "JudgmentTextDisplay"
   ```

2. **Transformを設定**
   ```
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   ```

3. **JudgmentTextDisplayスクリプトをアタッチ**
   ```
   Add Component > Scripts > Jirou.UI > JudgmentTextDisplay
   ```

4. **Inspectorで設定**
   
   **Text Settings:**
   - Text Prefab: JudgmentTextPrefab（先ほど作成したプレハブをドラッグ）
   - Display Duration: 1.0
   - Move Speed: 2.0
   - Fade Curve: デフォルトのまま（または調整）
   
   **Judgment Settings:**（4つのエントリが自動的に表示される）
   - Element 0:
     - Type: Perfect
     - Display Text: "Perfect"
     - Color: Yellow (R:1, G:1, B:0, A:1)
     - Scale: 1.2
   
   - Element 1:
     - Type: Great
     - Display Text: "Great"
     - Color: Green (R:0, G:1, B:0, A:1)
     - Scale: 1.1
   
   - Element 2:
     - Type: Good
     - Display Text: "Good"
     - Color: Cyan (R:0, G:1, B:1, A:1)
     - Scale: 1.0
   
   - Element 3:
     - Type: Miss
     - Display Text: "Miss"
     - Color: Red (R:1, G:0, B:0, A:1)
     - Scale: 0.9
   
   **Pool Settings:**
   - Pool Size: 20
   
   **Debug:**
   - Show Debug Info: ✓（テスト時はチェック）

### Phase 2: エフェクトシステムの設定

#### Step 1: エフェクトプレハブの確認

1. **既存のエフェクトプレハブを確認**
   ```
   Assets/_Jirou/Prefabs/Effects/
   以下のファイルが存在することを確認:
   - PerfectHitEffect.prefab
   - GreatHitEffect.prefab
   - GoodHitEffect.prefab
   - MissEffect.prefab
   ```

2. **プレハブが存在しない場合は作成**
   
   各判定タイプ用の簡単なパーティクルエフェクトを作成：
   
   **Perfectエフェクトの例：**
   ```
   Hierarchy右クリック > Effects > Particle System
   名前: "PerfectHitEffect"
   ```
   
   **パーティクル設定：**
   - Duration: 1.5
   - Start Lifetime: 1.0
   - Start Speed: 5
   - Start Size: 0.5
   - Start Color: Yellow
   - Emission > Rate over Time: 100
   - Shape > Shape: Sphere, Radius: 0.5
   
   **プレハブとして保存：**
   ```
   Assets/_Jirou/Prefabs/Effects/PerfectHitEffect.prefab
   ```
   
   同様に他の判定タイプ用エフェクトも作成

#### Step 2: JudgmentEffectManagerオブジェクトの作成

1. **空のGameObjectを作成**
   ```
   Hierarchy右クリック > Create Empty
   名前: "JudgmentEffectManager"
   ```

2. **Transformを設定**
   ```
   Position: (0, 0, 0)
   Rotation: (0, 0, 0)
   Scale: (1, 1, 1)
   ```

3. **JudgmentEffectManagerスクリプトをアタッチ**
   ```
   Add Component > Scripts > Jirou.Visual > JudgmentEffectManager
   ```

4. **Inspectorで設定**
   
   **Effect Prefabs:**
   - Perfect Effect Prefab: PerfectHitEffect.prefab
   - Great Effect Prefab: GreatHitEffect.prefab
   - Good Effect Prefab: GoodHitEffect.prefab
   - Miss Effect Prefab: MissEffect.prefab
   
   **Effect Settings:**
   - Effect Duration: 1.5
   - Use Object Pool: ✓（チェック）
   - Pool Size Per Type: 10
   - Effect Position Offset: (0, 0, 0)
   
   **Quality Settings:**
   - Enable LOD: ✓（チェック）
   - LOD Distance: 10
   - LOD Quality Reduction: 0.5
   
   **Debug:**
   - Show Debug Info: ✓（テスト時はチェック）

### Phase 3: システムの統合と動作確認

#### Step 1: InputManagerの確認

1. **InputManagerオブジェクトを選択**
   ```
   Hierarchyで "InputManager" を検索して選択
   ```

2. **イベントが正しく設定されているか確認**
   - InputManagerスクリプトがアタッチされている
   - JudgmentZonesが4つ設定されている

#### Step 2: 動作テスト

1. **Playモードに入る**
   ```
   Unityエディタ上部の▶ボタンをクリック
   ```

2. **キー入力テスト**
   ```
   D, F, J, Kキーを押す
   ```

3. **確認事項**
   - [ ] 判定テキストが表示される
   - [ ] テキストが上にフェードアウトしながら移動する
   - [ ] 判定タイプごとに異なる色で表示される
   - [ ] エフェクトが判定位置に表示される
   - [ ] 複数回連続で押してもスムーズに動作する

#### Step 3: デバッグ情報の確認

1. **Game画面でデバッグ情報を確認**
   - 左上にInputManager Debug
   - その下にJudgment Text Display Debug
   - さらに下にJudgment Effect Manager Debug

2. **確認項目**
   - Active Texts: アクティブなテキスト数
   - Pool Size: 利用可能なプールオブジェクト数
   - Active Effects: アクティブなエフェクト数

### トラブルシューティング

#### 問題1: テキストが表示されない
**解決方法:**
1. TextMeshProがインポートされているか確認
2. JudgmentTextPrefabにTextMeshProコンポーネントがあるか確認
3. カメラのCulling MaskにDefaultレイヤーが含まれているか確認

#### 問題2: エフェクトが見えない
**解決方法:**
1. エフェクトプレハブが正しく設定されているか確認
2. ParticleSystemのRendererでMaterialが設定されているか確認
3. カメラの位置とエフェクトの位置を確認

#### 問題3: テキストが小さすぎる/大きすぎる
**解決方法:**
1. JudgmentTextDisplayのScaleを調整
2. TextMeshProのFont Sizeを調整
3. カメラの距離を確認

#### 問題4: パフォーマンスが低下する
**解決方法:**
1. Pool Sizeを増やす
2. LODを有効にする
3. エフェクトのパーティクル数を減らす

### 最適化のヒント

1. **プールサイズの調整**
   - 通常のプレイで必要な最大数+αに設定
   - 少なすぎると動的生成でカクつく

2. **エフェクトの最適化**
   - Max Particlesを適切に設定
   - テクスチャサイズを最小限に
   - Emission Rateを調整

3. **テキストの最適化**
   - フォントアトラスサイズを適切に設定
   - 不要な文字を含めない

### 完了チェックリスト

- [ ] TextMeshProインポート完了
- [ ] JudgmentTextPrefab作成完了
- [ ] JudgmentTextDisplayオブジェクト設定完了
- [ ] エフェクトプレハブ準備完了
- [ ] JudgmentEffectManagerオブジェクト設定完了
- [ ] InputManagerとの連携確認完了
- [ ] 動作テスト完了
- [ ] デバッグ情報表示確認完了

## 次のステップ

判定フィードバックシステムの設定が完了したら、以下の追加機能を検討できます：

1. **コンボ表示システム**
   - 連続成功回数の表示
   - コンボボーナスエフェクト

2. **スコア表示システム**
   - リアルタイムスコア更新
   - スコア加算アニメーション

3. **カスタマイズ機能**
   - プレイヤーが色やエフェクトを変更可能に
   - 設定の保存/読み込み

## サポート

問題が発生した場合は、以下を確認してください：
- `/docs/components/judgment-feedback-implementation-plan.md`
- `/docs/design/judgment-feedback-system-design.md`
- コンソールログでエラーメッセージを確認