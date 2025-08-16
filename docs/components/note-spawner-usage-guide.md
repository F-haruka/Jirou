# NoteSpawner 使用ガイド

## 📖 概要

NoteSpawnerは、Jirouプロジェクトの奥行き型リズムゲームにおいて、譜面データ（ChartData）に基づいてノーツを3D空間に生成・管理するコアコンポーネントです。本ガイドでは、NoteSpawnerシステムの使用方法について詳しく説明します。

## 🚀 クイックスタート

### 1. 自動セットアップ（推奨）

最も簡単な方法は、`NoteSpawnerTestSetup`コンポーネントを使用することです。

```
1. Hierarchyビューで右クリック → Create Empty
2. 作成したGameObjectを「GameManager」などの名前に変更
3. InspectorでAdd Component → Jirou.Debug → NoteSpawnerTestSetup
4. 以下の設定を確認：
   - Auto Setup: ✓（オン）
   - Generate Test Chart: ✓（オン）
   - Test BPM: 120
   - Note Count: 20
5. Playボタンを押してテスト開始
```

これで自動的に以下がセットアップされます：
- Conductor（音楽同期管理）
- NotePoolManager（オブジェクトプール）
- NoteSpawner（ノーツ生成）
- テスト用の譜面データ
- ノーツプレハブ

### 2. 手動セットアップ

より細かい制御が必要な場合は、手動でセットアップします。

#### Step 1: Conductorの設定

```
1. 空のGameObjectを作成し「Conductor」と命名
2. Conductorコンポーネントを追加
3. AudioSourceコンポーネントを追加
4. Inspectorで以下を設定：
   - Song BPM: 120（楽曲のBPM）
   - Note Speed: 10（ノーツ移動速度）
   - Spawn Z: 20（生成位置）
   - Hit Z: 0（判定ライン位置）
```

#### Step 2: NoteSpawnerの設定

```
1. 空のGameObjectを作成し「NoteSpawner」と命名
2. NoteSpawnerコンポーネントを追加
3. Inspectorで以下を設定：
   - Chart Data: 譜面データアセットを設定
   - Tap Note Prefab: Tapノーツプレハブ
   - Hold Note Prefab: Holdノーツプレハブ
   - Lane X Positions: -3, -1, 1, 3（4レーンのX座標）
   - Note Y: 0.5（ノーツのY座標）
   - Beats Shown In Advance: 3（先読みビート数）
```

## 📊 譜面データ（ChartData）の作成

### ScriptableObjectとして作成

```csharp
// Projectビューで右クリック
// Create → Jirou → Chart Data

// または、コードから動的に作成
ChartData chartData = ScriptableObject.CreateInstance<ChartData>();
chartData.songName = "楽曲名";
chartData.artist = "アーティスト名";
chartData.bpm = 120f;
chartData.songClip = audioClip; // 楽曲のAudioClip
chartData.notes = new List<NoteData>();
```

### ノーツデータの追加

```csharp
// Tapノーツの追加
chartData.notes.Add(new NoteData
{
    noteType = NoteType.Tap,
    laneIndex = 0,  // 0-3のレーン番号
    timeToHit = 1.0f,  // ビート単位のタイミング
    noteColor = Color.cyan,
    visualScale = 1.0f
});

// Holdノーツの追加
chartData.notes.Add(new NoteData
{
    noteType = NoteType.Hold,
    laneIndex = 2,
    timeToHit = 2.0f,
    holdDuration = 1.5f,  // ホールド時間（ビート）
    noteColor = Color.yellow
});
```

## 🎮 ノーツプレハブの作成

### 基本的なノーツプレハブ

```
1. 3Dオブジェクト（Cube等）を作成
2. スケールを調整（例: 0.8, 0.3, 0.5）
3. NoteControllerコンポーネントを追加
4. Colliderをトリガーに設定（Is Trigger: ✓）
5. マテリアルで見た目を調整
6. ProjectビューにドラッグしてPrefab化
7. シーンから元のオブジェクトを削除
```

### プレハブの要件

- **必須コンポーネント**
  - `NoteController`: ノーツの動作制御
  - `Collider` (Trigger): 判定用
  - `Renderer`: 表示用

- **推奨設定**
  - Transform.scale: (0.8, 0.3, 0.5)
  - Layer: "Notes"または専用レイヤー
  - Tag: "Note"または"TapNote"/"HoldNote"

## 🔧 実行時の操作

### コンポーネントのメソッド

```csharp
// NoteSpawnerの取得
NoteSpawner spawner = GameObject.Find("NoteSpawner").GetComponent<NoteSpawner>();

// 一時停止
spawner.PauseSpawning();

// 再開
spawner.ResumeSpawning();

// 停止とリセット
spawner.StopAndReset();

// 統計情報の取得
int total, spawned, active, remaining;
spawner.GetStatistics(out total, out spawned, out active, out remaining);
Debug.Log($"総ノーツ: {total}, 生成済み: {spawned}, アクティブ: {active}");
```

### Conductorの制御

```csharp
Conductor conductor = Conductor.Instance;

// 楽曲の開始
conductor.StartSong();

// 一時停止
conductor.PauseSong();

// 再開
conductor.ResumeSong();

// BPM変更（楽曲中のBPM変化対応）
conductor.ChangeBPM(140f);

// 現在位置の取得
float currentBeat = conductor.songPositionInBeats;
float currentTime = conductor.SongPositionInSeconds;
```

## 🐛 デバッグ機能

### Inspector設定

- **Enable Debug Log**: コンソールへのログ出力
- **Show Note Path Gizmo**: シーンビューでレーン可視化

### ランタイムデバッグ表示

Playモード中、Game画面に以下の情報が表示されます：

```
NoteSpawner Debug Info
- 総ノーツ数: 100
- 生成済み: 45 / アクティブ: 8
- 残り: 55

NotePool Debug Info
- Tapプール: 10/30
- Holdプール: 5/30
- ヒット率 - Tap: 85.3% Hold: 90.1%
```

### Gizmos表示

Scene ビューで以下が可視化されます：
- 緑線: スポーンライン（Z=20）
- 赤線: 判定ライン（Z=0）
- 青線: レーン境界
- 黄色: アクティブノーツ

## 🎯 パフォーマンス最適化

### オブジェクトプール設定

```csharp
NotePoolManager poolManager = NotePoolManager.Instance;

// プールサイズの調整
poolManager.initialPoolSize = 20;  // 初期プールサイズ
poolManager.maxPoolSize = 50;      // 最大プールサイズ

// プールのリサイズ
poolManager.ResizePool(30);

// プールのクリア
poolManager.ClearPool();
```

### 最適化のヒント

1. **プールサイズ**: 同時に表示される最大ノーツ数の1.5倍程度に設定
2. **先読みビート数**: 3-4ビートが標準的（見やすさとパフォーマンスのバランス）
3. **スポーン間隔チェック**: `SPAWN_CHECK_INTERVAL`を調整（デフォルト: 0.25ビート）

## 📝 トラブルシューティング

### よくある問題と解決方法

| 問題 | 原因 | 解決方法 |
|------|------|----------|
| ノーツが生成されない | ChartDataが未設定 | InspectorでChartDataを設定 |
| ノーツが見えない | プレハブにRendererがない | プレハブにMeshRendererを追加 |
| タイミングがずれる | Time.deltaTime使用 | AudioSettings.dspTimeを使用 |
| メモリリーク | プール未使用 | NotePoolManagerを有効化 |
| ノーツが動かない | Conductor未設定 | Conductorをシーンに配置 |

### エラーコード

```
NS001: ChartData未設定 → ChartDataアセットを作成・設定
NS002: プレハブ未設定 → ノーツプレハブを設定
NS003: Conductor未検出 → Conductorをシーンに配置
NS004: レーン数不正 → 4レーンのX座標を設定
NS005: 無効なノーツデータ → ChartDataを検証
```

## 🔄 カスタマイズ

### カスタムノーツタイプの追加

```csharp
// NoteType列挙体を拡張
public enum NoteType
{
    Tap,
    Hold,
    Slide,  // 新規追加
    Flick   // 新規追加
}

// NoteSpawnerで対応プレハブを追加
public GameObject slideNotePrefab;
public GameObject flickNotePrefab;

// SpawnNoteメソッドで分岐追加
GameObject notePrefab = noteData.noteType switch
{
    NoteType.Tap => tapNotePrefab,
    NoteType.Hold => holdNotePrefab,
    NoteType.Slide => slideNotePrefab,
    NoteType.Flick => flickNotePrefab,
    _ => tapNotePrefab
};
```

### エフェクトのカスタマイズ

```csharp
// ノーツデータにエフェクト設定
noteData.customHitEffect = hitEffectPrefab;
noteData.customHitSound = hitSoundClip;

// NoteControllerで処理
public void OnHit()
{
    if (customHitEffect != null)
        Instantiate(customHitEffect, transform.position, Quaternion.identity);
    
    if (customHitSound != null)
        AudioSource.PlayClipAtPoint(customHitSound, transform.position);
}
```

## 🎵 楽曲との同期

### AudioClipの準備

1. 楽曲ファイル（.mp3, .wav等）をProjectにインポート
2. ChartDataのSong Clipフィールドに設定
3. BPMを正確に設定（重要！）
4. First Beat Offsetで最初のビートまでの時間を調整

### タイミング調整

```csharp
// Conductorでオフセット調整
conductor.firstBeatOffset = 0.5f;  // 最初のビートまで0.5秒

// ノーツ生成タイミングの調整
spawner.beatsShownInAdvance = 3.0f;  // 3ビート先行して生成

// 判定タイミングの微調整
float timingWindow = 0.1f;  // ±0.1秒の判定窓
```

## 📊 テスト方法

### Unity Test Runnerでのテスト

```
1. Window → General → Test Runner
2. EditModeタブを選択
3. NoteSpawnerTestsを展開
4. 各テストを実行または「Run All」
```

### 実機テスト

```csharp
// デバッグキー設定例
void Update()
{
    // Spaceキーでテスト譜面を再生
    if (Input.GetKeyDown(KeyCode.Space))
    {
        noteSpawner.StopAndReset();
        conductor.StartSong();
    }
    
    // Pキーで一時停止/再開
    if (Input.GetKeyDown(KeyCode.P))
    {
        if (conductor.IsPlaying)
            noteSpawner.PauseSpawning();
        else
            noteSpawner.ResumeSpawning();
    }
    
    // Rキーでリセット
    if (Input.GetKeyDown(KeyCode.R))
    {
        noteSpawner.StopAndReset();
    }
}
```

## 🚀 次のステップ

NoteSpawnerシステムのセットアップが完了したら、次は以下の実装を進めることができます：

1. **判定システムの実装**: ノーツのヒット判定とスコアリング
2. **ビジュアルエフェクト**: ヒット時のパーティクルエフェクト
3. **UIシステム**: スコア表示、コンボカウンター
4. **入力システム**: キーボード入力の処理
5. **レーンビジュアライザー**: 台形状レーンの表示

## 📚 関連ドキュメント

- [NoteSpawner実装計画書](note-spawner-implementation-plan.md)
- [ChartDataシステム設計](../architectures/chart-data-system.md)
- [Conductorシステム設計](../architectures/conductor-system.md)
- [3Dリズムゲーム実装ガイド](../plans/3d-rhythm-game-guide.md)

---

*最終更新: 2025年1月*