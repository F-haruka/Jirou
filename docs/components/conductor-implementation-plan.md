# Conductor 実装計画書

## 実装概要

本書は、Jirouプロジェクトの中核コンポーネントであるConductorクラスの実装計画を定義します。段階的な実装アプローチを採用し、各フェーズで動作確認を行いながら開発を進めます。

## 実装状況

**最終更新日**: 2025-08-15

### 実装完了項目
- ✅ シングルトンパターン
- ✅ 基本フィールドとプロパティ
- ✅ 楽曲制御メソッド（StartSong, StopSong, PauseSong, ResumeSong）
- ✅ ノーツ位置計算メソッド（GetNoteZPosition, ShouldSpawnNote, IsNoteInHitZone）
- ✅ デバッグ機能（OnGUI, OnDrawGizmos）
- ✅ エラーハンドリング（ValidateAudioSource）
- ✅ 追加機能（GetTimeUntilBeat, ChangeBPM, IsPlayingプロパティ）
- ✅ 4レーン可視化

### 残作業
- ⏳ 統合テスト（実際の楽曲ファイルでのテスト）
- ⏳ パフォーマンス検証

## 実装フェーズ

### フェーズ1: 基本構造の実装（必須）

#### 1.1 シングルトンパターンの実装
```csharp
public class Conductor : MonoBehaviour
{
    private static Conductor instance;
    public static Conductor Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<Conductor>();
                if (instance == null)
                {
                    Debug.LogError("Conductorが見つかりません！");
                }
            }
            return instance;
        }
    }
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}
```

**検証項目**:
- [ ] シーン内に1つのみ存在することを確認
- [ ] シーン遷移時に破棄されないことを確認
- [ ] 他のスクリプトからアクセス可能なことを確認

#### 1.2 基本フィールドとプロパティ
```csharp
// パブリックフィールド
[Header("楽曲設定")]
public float songBpm = 120f;
public float firstBeatOffset = 0f;
public AudioSource songSource;

[Header("ノーツ移動設定")]
public float noteSpeed = 10.0f;
public float spawnZ = 20.0f;
public float hitZ = 0.0f;

// プライベートフィールド
private double dspSongTime;
private float secPerBeat;

// プロパティ
public float songPositionInSeconds
{
    get
    {
        return (float)(AudioSettings.dspTime - dspSongTime - firstBeatOffset);
    }
}

public float songPositionInBeats
{
    get
    {
        return songPositionInSeconds / secPerBeat;
    }
}
```

**検証項目**:
- [ ] Inspectorで値が設定可能
- [ ] プロパティが正しい値を返す
- [ ] AudioSourceコンポーネントの参照が機能

### フェーズ2: コア機能の実装

#### 2.1 楽曲制御メソッド
```csharp
public void StartSong()
{
    // BPMから1ビートあたりの秒数を計算
    secPerBeat = 60.0f / songBpm;
    
    // 開始時刻を記録
    dspSongTime = AudioSettings.dspTime;
    
    // 楽曲を再生
    if (songSource != null && songSource.clip != null)
    {
        songSource.Play();
        Debug.Log($"楽曲開始: BPM={songBpm}, オフセット={firstBeatOffset}秒");
    }
    else
    {
        Debug.LogError("AudioSourceまたはAudioClipが設定されていません！");
    }
}

public void StopSong()
{
    if (songSource != null && songSource.isPlaying)
    {
        songSource.Stop();
        Debug.Log("楽曲停止");
    }
}

public void PauseSong()
{
    if (songSource != null && songSource.isPlaying)
    {
        songSource.Pause();
        Debug.Log("楽曲一時停止");
    }
}

public void ResumeSong()
{
    if (songSource != null && !songSource.isPlaying)
    {
        songSource.UnPause();
        Debug.Log("楽曲再開");
    }
}
```

**検証項目**:
- [ ] 楽曲の再生/停止が正常に動作
- [ ] タイミング計算が正確
- [ ] エラーハンドリングが機能

#### 2.2 ノーツ位置計算メソッド
```csharp
public float GetNoteZPosition(float noteBeat)
{
    // 現在のビート位置からノーツビートまでの差分
    float beatsPassed = songPositionInBeats - noteBeat;
    
    // Z座標を計算（奥から手前へ移動）
    float zPosition = spawnZ - (beatsPassed * noteSpeed);
    
    return zPosition;
}

public bool ShouldSpawnNote(float noteBeat, float beatsInAdvance)
{
    // 先読みビート数を考慮した生成判定
    float spawnBeat = noteBeat - beatsInAdvance;
    
    // 現在のビート位置が生成タイミングを超えたか
    return songPositionInBeats >= spawnBeat;
}

public bool IsNoteInHitZone(float noteZ, float tolerance = 1.0f)
{
    // 判定ラインとの距離を計算
    float distance = Mathf.Abs(noteZ - hitZ);
    
    // 許容範囲内かチェック
    return distance <= tolerance;
}
```

**検証項目**:
- [ ] Z座標計算が正確
- [ ] スポーン判定が適切なタイミング
- [ ] 判定ゾーンの検出が正常

### フェーズ3: デバッグ機能の実装

#### 3.1 ビジュアルデバッグ
```csharp
#if UNITY_EDITOR
void OnGUI()
{
    if (!Application.isPlaying) return;
    
    GUIStyle style = new GUIStyle(GUI.skin.label);
    style.fontSize = 14;
    style.normal.textColor = Color.white;
    
    // 背景ボックス
    GUI.Box(new Rect(10, 10, 250, 120), "Conductor Debug Info");
    
    // デバッグ情報表示
    GUI.Label(new Rect(20, 35, 230, 20), 
              $"BPM: {songBpm}", style);
    GUI.Label(new Rect(20, 55, 230, 20), 
              $"Time: {songPositionInSeconds:F2}s", style);
    GUI.Label(new Rect(20, 75, 230, 20), 
              $"Beat: {songPositionInBeats:F2}", style);
    GUI.Label(new Rect(20, 95, 230, 20), 
              $"Playing: {(songSource != null && songSource.isPlaying)}", style);
}

void OnDrawGizmos()
{
    // スポーンラインの可視化
    Gizmos.color = Color.green;
    Gizmos.DrawWireCube(new Vector3(0, 0, spawnZ), 
                        new Vector3(10, 0.1f, 0.1f));
    
    // 判定ラインの可視化
    Gizmos.color = Color.red;
    Gizmos.DrawWireCube(new Vector3(0, 0, hitZ), 
                        new Vector3(10, 0.1f, 0.1f));
    
    // 移動経路の可視化
    Gizmos.color = Color.yellow;
    Gizmos.DrawLine(new Vector3(-5, 0, spawnZ), 
                    new Vector3(-5, 0, hitZ));
    Gizmos.DrawLine(new Vector3(5, 0, spawnZ), 
                    new Vector3(5, 0, hitZ));
}
#endif
```

#### 3.2 コンソールログ機能
```csharp
[Header("デバッグ設定")]
public bool enableDebugLog = false;

private void LogDebug(string message)
{
    if (enableDebugLog)
    {
        Debug.Log($"[Conductor] {message}");
    }
}
```

### フェーズ4: エラーハンドリングと検証

#### 4.1 初期化時の検証
```csharp
void Start()
{
    // 必須コンポーネントのチェック
    if (songSource == null)
    {
        songSource = GetComponent<AudioSource>();
        if (songSource == null)
        {
            Debug.LogError("AudioSourceが見つかりません！Conductorと同じGameObjectに追加してください。");
        }
    }
    
    // BPMの妥当性チェック
    if (songBpm <= 0)
    {
        Debug.LogWarning($"不正なBPM値: {songBpm}。デフォルト値120に設定します。");
        songBpm = 120f;
    }
    
    // 初期計算
    secPerBeat = 60.0f / songBpm;
}
```

#### 4.2 実行時の検証
```csharp
private bool ValidateAudioSource()
{
    if (songSource == null)
    {
        Debug.LogError("AudioSourceが設定されていません！");
        return false;
    }
    
    if (songSource.clip == null)
    {
        Debug.LogError("AudioClipが設定されていません！");
        return false;
    }
    
    return true;
}
```

## 実装スケジュール

### 第1週: 基礎実装
- [x] Day 1: プロジェクトセットアップとConductor.cs作成
- [x] Day 2: シングルトンパターン実装
- [x] Day 3: 基本フィールドとプロパティ実装
- [x] Day 4: 楽曲制御メソッド実装
- [x] Day 5: テストシーン作成と基本動作確認

### 第2週: 機能拡張
- [x] Day 6: ノーツ位置計算メソッド実装
- [x] Day 7: デバッグ機能実装
- [x] Day 8: エラーハンドリング実装
- [ ] Day 9: 統合テスト
- [ ] Day 10: 最終調整とドキュメント更新

## テスト計画

### ユニットテスト項目

#### タイミング精度テスト
```csharp
[Test]
public void TestBeatCalculation()
{
    // 120 BPMで1秒後は2ビート
    conductor.songBpm = 120f;
    conductor.StartSong();
    yield return new WaitForSeconds(1.0f);
    
    Assert.AreEqual(2.0f, conductor.songPositionInBeats, 0.1f);
}
```

#### Z座標計算テスト
```csharp
[Test]
public void TestNoteZPosition()
{
    conductor.spawnZ = 20f;
    conductor.noteSpeed = 10f;
    
    // 2ビート経過後
    float z = conductor.GetNoteZPosition(0f);
    // songPositionInBeats = 2の場合
    // z = 20 - (2 - 0) * 10 = 0
    
    Assert.AreEqual(0f, z, 0.1f);
}
```

### 統合テスト項目

1. **楽曲同期テスト**
   - 実際の楽曲ファイルを使用
   - ビート位置と音楽の同期確認
   - 様々なBPMでの動作確認

2. **ノーツ移動テスト**
   - ダミーノーツを生成
   - Z軸移動の視覚的確認
   - 判定ラインへの到達タイミング確認

3. **パフォーマンステスト**
   - 高BPM（200以上）での動作
   - 低BPM（60以下）での動作
   - 長時間再生での精度維持

## 実装上の注意事項

### 必須要件
1. **AudioSettings.dspTimeの使用**
   - Time.timeやTime.deltaTimeは使用禁止
   - 音声ハードウェアクロックに同期

2. **シングルトンの適切な実装**
   - シーン遷移での永続性
   - null参照の防止

3. **エラーハンドリング**
   - nullチェックの徹底
   - 適切なログ出力

### パフォーマンス最適化
1. **計算の最小化**
   - プロパティでの遅延評価
   - 不要な計算の削減

2. **メモリ効率**
   - 静的インスタンスの適切な管理
   - イベントリスナーの解除

### コーディング規約
1. **命名規則**
   - publicフィールド: camelCase
   - privateフィールド: camelCase
   - メソッド: PascalCase

2. **コメント**
   - すべて日本語で記述
   - 複雑なロジックには説明を追加

3. **領域分割**
   - `#region`を使用した論理的なグループ化
   - 関連する機能をまとめる

## 成果物

### 必須成果物
1. **Conductor.cs**: 完全実装されたスクリプト
2. **ConductorTest.cs**: ユニットテストスクリプト
3. **TestScene.unity**: 動作確認用シーン

### オプション成果物
1. **ConductorEditor.cs**: カスタムインスペクター
2. **使用例ドキュメント**: 実装例とベストプラクティス

## リスクと対策

### 技術的リスク
| リスク | 影響度 | 対策 |
|-------|--------|------|
| AudioSettings.dspTimeの精度不足 | 高 | 代替タイミング手法の調査 |
| 高レイテンシ環境での同期ずれ | 中 | オフセット調整機能の実装 |
| メモリリーク | 中 | 適切なリソース管理 |

### スケジュールリスク
| リスク | 影響度 | 対策 |
|-------|--------|------|
| テスト時間不足 | 中 | 自動テストの活用 |
| 仕様変更 | 低 | 拡張性を考慮した設計 |

## まとめ

Conductorクラスの実装は、Jirouプロジェクトの基盤となる重要な要素です。段階的な実装アプローチにより、各フェーズで動作を確認しながら、確実に機能を構築していきます。特に音楽同期の精度が重要であるため、`AudioSettings.dspTime`を使用した高精度タイミング管理を徹底します。