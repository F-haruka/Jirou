# スコアシステムのセットアップガイド

## 問題の解決方法

現在、`ScoreManager`のイベントにサブスクライバーがいないという警告が表示されています。これを解決するには、以下のいずれかの方法を実行してください。

## 方法1: GameInitializerを使用する（推奨）

1. Unityエディタでシーンを開く
2. 空のGameObjectを作成（`GameObject > Create Empty`）
3. GameObjectの名前を「GameInitializer」に変更
4. `GameInitializer`コンポーネントを追加
   - Inspectorで「Add Component」をクリック
   - 「GameInitializer」を検索して選択
5. 以下の設定を確認：
   - `Auto Create Score Manager` ✓
   - `Auto Create Score UI Events` ✓
6. シーンを保存して実行

## 方法2: 手動でScoreUIEventsを追加

1. Unityエディタでシーンを開く
2. 空のGameObjectを作成（`GameObject > Create Empty`）
3. GameObjectの名前を「ScoreUIEvents」に変更
4. `ScoreUIEvents`コンポーネントを追加
   - Inspectorで「Add Component」をクリック
   - 「ScoreUIEvents」を検索して選択
5. シーンを保存して実行

## 方法3: ScoreManagerと同じGameObjectに追加

1. シーン内の「ScoreManager」GameObjectを選択
2. Inspectorで「Add Component」をクリック
3. 「ScoreUIEvents」を検索して選択
4. シーンを保存して実行

## 動作確認

正しく設定されると、コンソールに以下のログが表示されます：
```
[ScoreManager] ScoreUIEvents found in scene - UI events will work correctly
[ScoreUIEvents] Subscribing to ScoreManager events...
[ScoreUIEvents] Successfully subscribed to all ScoreManager events
```

## トラブルシューティング

### 警告が消えない場合
- ScoreUIEventsコンポーネントがシーンに存在することを確認
- ScoreUIManagerコンポーネントもシーンに存在することを確認
- Unityを再起動してみる

### UIが更新されない場合
- ScoreDisplay、ComboDisplay、JudgmentCountDisplayコンポーネントがシーンに存在することを確認
- これらのコンポーネントがScoreUIEventsのイベントに購読していることを確認