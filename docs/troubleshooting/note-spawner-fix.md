# NoteSpawner ノーツ生成問題の修正

## 問題の概要
NoteSpawnerTestSetupスクリプトを使用してAuto Setupを実行し、Unityエディタでplayボタンを押しても、ノーツが生成されて流れる様子が確認できない問題。

## 原因

### 1. プレハブの非アクティブ状態
`NoteSpawnerTestSetup.cs`でテスト用プレハブを作成する際、316行目で以下のようにプレハブを非アクティブにしていた：
```csharp
// プレハブを非アクティブ化（プール用）
prefab.SetActive(false);
```

この状態でInstantiateすると、生成されたノーツオブジェクトも非アクティブ状態になり、Update()が呼ばれないため動作しない。

### 2. ノーツ生成後のアクティブ化不足
`NoteSpawner.cs`でノーツを生成した後、明示的にアクティブ化する処理が不足していた。

## 解決策

### 1. プレハブをアクティブ状態のままにする
**NoteSpawnerTestSetup.cs (316行目付近)**
```csharp
// 修正前
prefab.SetActive(false);

// 修正後
// プレハブはアクティブ状態のままにする（Instantiate時にアクティブなオブジェクトが生成される）
// プール管理は各インスタンスで行う
```

### 2. ノーツ生成後に確実にアクティブ化
**NoteSpawner.cs (SpawnNoteメソッド)**
```csharp
// プールから取得できない場合は直接生成
if (noteObject == null)
{
    noteObject = Instantiate(notePrefab);
    // 生成されたノーツを確実にアクティブ化
    noteObject.SetActive(true);
}

// ... 中略 ...

// カスタマイズの適用後も再度アクティブ化を確認
noteObject.SetActive(true);
```

### 3. デバッグログの追加
問題の診断を容易にするため、以下の箇所にデバッグログを追加：

- **NoteSpawnerTestSetup.cs**: セットアップ完了時の状態確認ログ
- **NoteSpawner.cs**: ノーツ生成時の詳細ログ、Update内での状態ログ
- **NoteController.cs**: 初期化時とUpdate時の動作確認ログ
- **NotePoolManager.cs**: プールの初期化時の状態確認

## 動作確認方法

1. **Unityエディタでの確認**
   - Hierarchyビューでノーツオブジェクトがアクティブになっているか確認
   - Consoleウィンドウでデバッグログを確認
   - Scene ビューでノーツが実際に移動しているか確認

2. **デバッグログの確認ポイント**
   - `[NoteSpawnerTestSetup]` のログでセットアップが正常に完了しているか
   - `[NoteSpawner]` のログでノーツが生成されているか、Activeがtrueになっているか
   - `[NoteController]` のログでノーツが移動しているか

3. **Gizmoの活用**
   - NoteSpawnerのshowNotePathGizmoをtrueにして、レーンの経路を可視化
   - アクティブなノーツの位置がGizmoで表示される

## 今後の改善点

1. **プレハブ管理の改善**
   - プレハブは常にアクティブ状態で管理し、インスタンスレベルでアクティブ/非アクティブを制御

2. **エラーハンドリングの強化**
   - ノーツが非アクティブな場合の警告ログ追加
   - プレハブが非アクティブな場合の自動修正機能

3. **テスト環境の整備**
   - 自動テストでノーツの生成と移動を検証
   - パフォーマンスプロファイリングの追加