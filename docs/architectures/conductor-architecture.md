# Conductor アーキテクチャ設計書

## 概要

Conductorクラスは、リズムゲーム「Jirou」の音楽同期とタイミング管理の中核となるシングルトンクラスです。奥行き型リズムゲームにおいて、ノーツのZ軸移動計算と音楽の正確な同期を実現します。

## 設計原則

### 1. シングルトンパターン
- アプリケーション全体で唯一のインスタンスを保証
- グローバルアクセスポイントを提供
- シーン遷移時の永続性を確保

### 2. 高精度タイミング管理
- `AudioSettings.dspTime`を使用した音声ハードウェアレベルの精度
- `Time.deltaTime`や`Time.time`を使用しない（フレームレート依存を避ける）
- ダブル精度浮動小数点数による時間管理

### 3. Z軸座標計算
- 音楽のビート位置に基づくノーツのZ座標計算
- 奥行き表現のための動的位置更新
- スポーン位置から判定ラインまでの滑らかな移動

## クラス構造

### クラス定義
```csharp
public class Conductor : MonoBehaviour
{
    // シングルトンインスタンス
    private static Conductor instance;
    public static Conductor Instance { get; private set; }
}
```

### パブリックフィールド

| フィールド名 | 型 | デフォルト値 | 説明 |
|------------|---|------------|------|
| songBpm | float | - | 楽曲のBPM（Beats Per Minute） |
| firstBeatOffset | float | 0f | 最初のビートまでのオフセット時間（秒） |
| songSource | AudioSource | - | 楽曲再生用のAudioSourceコンポーネント |
| noteSpeed | float | 10.0f | ノーツの移動速度（単位：Z軸距離/ビート） |
| spawnZ | float | 20.0f | ノーツ生成位置のZ座標 |
| hitZ | float | 0.0f | 判定ラインのZ座標 |

### プライベートフィールド

| フィールド名 | 型 | 説明 |
|------------|---|------|
| dspSongTime | double | 楽曲開始時の`AudioSettings.dspTime` |
| secPerBeat | float | 1ビートあたりの秒数（60.0f / songBpm） |

### パブリックプロパティ

#### songPositionInSeconds（読み取り専用）
```csharp
public float songPositionInSeconds
{
    get => (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);
}
```
- 現在の楽曲再生位置を秒単位で取得
- 音声ハードウェアタイムから算出
- firstBeatOffsetを考慮

#### songPositionInBeats（読み取り専用）
```csharp
public float songPositionInBeats
{
    get => songPositionInSeconds / secPerBeat;
}
```
- 現在の楽曲再生位置をビート単位で取得
- BPMに基づく変換

## 主要メソッド

### GetNoteZPosition
```csharp
public float GetNoteZPosition(float noteBeat)
```

**機能**: 指定されたビートタイミングのノーツの現在Z座標を計算

**パラメータ**:
- `noteBeat`: ノーツのビートタイミング

**戻り値**: ノーツのZ座標

**計算ロジック**:
1. 現在のビート位置とノーツビートの差分を計算
2. 経過ビート数に移動速度を乗算
3. スポーン位置から移動距離を減算

```csharp
float beatsPassed = songPositionInBeats - noteBeat;
return spawnZ - (beatsPassed * noteSpeed);
```

### ShouldSpawnNote
```csharp
public bool ShouldSpawnNote(float noteBeat, float beatsInAdvance)
```

**機能**: ノーツを生成すべきタイミングかを判定

**パラメータ**:
- `noteBeat`: ノーツのビートタイミング
- `beatsInAdvance`: 事前生成するビート数

**戻り値**: 生成すべきなら`true`

**判定ロジック**:
- 現在のビート位置 + 先読みビート数 >= ノーツビート
- まだ生成されていないことが前提

### StartSong
```csharp
public void StartSong()
```

**機能**: 楽曲の再生を開始し、タイミング管理を初期化

**処理内容**:
1. `dspSongTime`を現在の`AudioSettings.dspTime`に設定
2. `secPerBeat`を計算（60.0f / songBpm）
3. `songSource.Play()`を実行

### StopSong
```csharp
public void StopSong()
```

**機能**: 楽曲の再生を停止

**処理内容**:
1. `songSource.Stop()`を実行
2. タイミング関連の変数をリセット

## ライフサイクル管理

### Awake
```csharp
void Awake()
{
    // シングルトンパターンの実装
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else
    {
        Destroy(gameObject);
    }
}
```

### Start
```csharp
void Start()
{
    // secPerBeatの初期計算
    secPerBeat = 60.0f / songBpm;
}
```

## Z軸移動の数学的モデル

### 位置計算式
```
Z(t) = Z_spawn - (Beat_current - Beat_note) × Speed
```

- `Z(t)`: 時刻tにおけるノーツのZ座標
- `Z_spawn`: ノーツ生成位置（20.0f）
- `Beat_current`: 現在のビート位置
- `Beat_note`: ノーツの譜面上のビート位置
- `Speed`: 移動速度（10.0f）

### タイミング精度
- **誤差**: ±1ms以内（`AudioSettings.dspTime`の精度）
- **フレームレート非依存**: 60fps、30fps、144fpsで同一の動作
- **音楽同期**: 音声ハードウェアクロックに完全同期

## パフォーマンス考慮事項

### 計算効率
- ビート計算は必要時のみ実行（プロパティゲッター）
- Z座標計算は単純な算術演算のみ
- キャッシュ可能な値は事前計算（`secPerBeat`）

### メモリ使用
- シングルトンのため、インスタンスは1つのみ
- 軽量なデータ構造（基本型のみ使用）

## 拡張性

### 将来的な機能追加
1. **BPM変化対応**: 楽曲中のBPM変化をサポート
2. **オフセット調整**: プレイヤーごとの遅延補正
3. **プレビュー機能**: 楽曲の任意位置からの再生
4. **メトロノーム**: 開発用のビート音再生

### インターフェース設計
他のシステムとの連携を考慮した設計：
- `NoteSpawner`: ノーツ生成タイミングの取得
- `NoteController`: 各ノーツのZ座標更新
- `JudgmentSystem`: 判定タイミングの基準時刻取得
- `ScoreManager`: コンボタイミングの同期

## エラーハンドリング

### 想定されるエラー
1. **AudioSource未設定**: Inspectorで警告表示
2. **BPM未設定または0**: デフォルト値（120）を使用
3. **楽曲未ロード**: 再生前にチェック

### デバッグ機能
```csharp
#if UNITY_EDITOR
    // エディタ上でのビート位置表示
    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 200, 30), 
                  $"Beat: {songPositionInBeats:F2}");
        GUI.Label(new Rect(10, 40, 200, 30), 
                  $"Time: {songPositionInSeconds:F2}s");
    }
#endif
```

## テスト計画

### ユニットテスト
1. **タイミング精度テスト**: 様々なBPMでの精度検証
2. **Z座標計算テスト**: 各ビートでの位置計算検証
3. **スポーン判定テスト**: 生成タイミングの正確性

### 統合テスト
1. **音楽同期テスト**: 実際の楽曲での同期確認
2. **ノーツ移動テスト**: 視覚的な滑らかさの確認
3. **判定連携テスト**: JudgmentSystemとの連携確認

## まとめ

Conductorクラスは、Jirouの音楽同期システムの基盤として、高精度なタイミング管理と奥行き型ノーツ移動の計算を提供します。`AudioSettings.dspTime`を使用することで、フレームレートに依存しない安定した音楽同期を実現し、プレイヤーに快適なリズムゲーム体験を提供します。