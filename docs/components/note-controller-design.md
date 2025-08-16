# NoteController 詳細設計書

## 概要

`NoteController`は、奥行き型リズムゲーム「Jirou」における個別ノーツの動作を制御するコンポーネントです。このコンポーネントは、ノーツの3D空間での移動、距離に応じた動的スケーリング、タイミング判定、ヒット処理など、ノーツに関する全ての振る舞いを管理します。

## 設計方針

### 基本原則
- **単一責任の原則**: ノーツの視覚的表現と移動制御に特化
- **データ駆動設計**: NoteDataクラスから全ての設定を取得
- **高精度タイミング**: Conductorクラスと連携した正確なタイミング管理
- **パフォーマンス重視**: オブジェクトプーリングを考慮したリセット機能

### 主要機能
1. **3D空間移動**: Z軸に沿った奥から手前への移動
2. **動的スケーリング**: 距離に応じた視覚的サイズ変更
3. **タイミング判定**: Perfect/Great/Good/Missの4段階判定
4. **視覚フィードバック**: ヒット時のエフェクトとサウンド再生
5. **Holdノーツ対応**: Trail表示による長押しノーツの視覚化

## クラス構造

### パブリックフィールド

```csharp
// ノーツデータの参照
public NoteData noteData { get; private set; }

// 移動速度（Z軸距離/秒）
public float moveSpeed = 10.0f;
```

### プライベートフィールド

```csharp
// タイミング管理
private float targetBeat;           // ノーツがヒットすべきビート
private bool hasBeenHit;           // ヒット済みフラグ

// 視覚的要素
private Vector3 initialScale;       // 元のスケール値
private MeshRenderer renderer;      // レンダラーコンポーネント
private TrailRenderer trailRenderer; // Holdノーツ用Trail

// コンポーネント参照
private Conductor conductor;         // Conductorシングルトン参照
private bool isCompleted;          // 処理完了フラグ
```

## メソッド詳細

### 初期化メソッド

#### Initialize(NoteData data, Conductor conductorRef)
```csharp
public void Initialize(NoteData data, Conductor conductorRef)
{
    // データの設定
    noteData = data;
    conductor = conductorRef;
    targetBeat = data.TimeToHit;
    
    // 初期状態の記録
    initialScale = transform.localScale;
    hasBeenHit = false;
    isCompleted = false;
    
    // レンダラーの取得とキャッシュ
    renderer = GetComponent<MeshRenderer>();
    
    // Holdノーツの場合、Trailの設定
    if (data.NoteType == NoteType.Hold)
    {
        SetupHoldTrail(data.HoldDuration);
    }
}
```

#### Start()
```csharp
void Start()
{
    // Conductorの参照を取得（Initialize未呼び出しの場合の保険）
    if (conductor == null)
    {
        conductor = Conductor.Instance;
    }
    
    // NoteDataからtargetBeatを設定
    if (noteData != null)
    {
        targetBeat = noteData.TimeToHit;
    }
    
    // 初期スケールを記録
    initialScale = transform.localScale;
    
    // レンダラーをキャッシュ
    renderer = GetComponent<MeshRenderer>();
    
    // Holdノーツの場合、Trailの長さを設定
    if (noteData != null && noteData.NoteType == NoteType.Hold)
    {
        SetupHoldTrail(noteData.HoldDuration);
    }
}
```

### 更新メソッド

#### Update()
```csharp
void Update()
{
    if (conductor == null || isCompleted) return;
    
    // 1. Z座標の更新（奥から手前へ移動）
    float newZ = conductor.GetNoteZPosition(targetBeat);
    transform.position = new Vector3(
        transform.position.x,
        transform.position.y,
        newZ
    );
    
    // 2. 距離に応じたスケール変更
    ApplyDistanceScaling(newZ);
    
    // 3. 判定ラインを通過したかチェック
    if (newZ < conductor.hitZ - 2.0f)
    {
        // ミス処理
        if (!hasBeenHit)
        {
            OnMiss();
        }
        isCompleted = true;
    }
}
```

### 判定メソッド

#### CheckHitTiming()
```csharp
public JudgmentType CheckHitTiming()
{
    // 現在のZ座標を取得
    float currentZ = transform.position.z;
    
    // 判定ラインとの距離を計算
    float distance = Mathf.Abs(currentZ - conductor.hitZ);
    
    // 距離に応じた判定を返す
    if (distance <= 0.5f)
    {
        return JudgmentType.Perfect;  // ±0.5単位以内
    }
    else if (distance <= 1.0f)
    {
        return JudgmentType.Great;    // ±1.0単位以内
    }
    else if (distance <= 1.5f)
    {
        return JudgmentType.Good;     // ±1.5単位以内
    }
    else
    {
        return JudgmentType.Miss;     // それ以外
    }
}
```

### ヒット処理メソッド

#### OnHit()
```csharp
public void OnHit()
{
    if (hasBeenHit) return;  // 二重ヒット防止
    
    hasBeenHit = true;
    isCompleted = true;
    
    // タイミング判定を取得
    JudgmentType judgment = CheckHitTiming();
    
    // スコア加算（ScoreManagerが実装されたら連携）
    // ScoreManager.Instance.AddScore(judgment, noteData);
    
    // エフェクト生成
    if (noteData.CustomHitEffect != null)
    {
        Instantiate(noteData.CustomHitEffect, transform.position, Quaternion.identity);
    }
    
    // サウンド再生
    if (noteData.CustomHitSound != null)
    {
        AudioSource.PlayClipAtPoint(noteData.CustomHitSound, transform.position);
    }
    
    // ノーツを非表示にする（プーリング対応）
    gameObject.SetActive(false);
}
```

### プライベートメソッド

#### ApplyDistanceScaling(float currentZ)
```csharp
private void ApplyDistanceScaling(float currentZ)
{
    // 距離の比率を計算（0=手前、1=奥）
    float distanceRatio = Mathf.Clamp01(currentZ / conductor.spawnZ);
    
    // スケール係数を計算（手前1.5倍、奥0.5倍）
    float scaleFactor = Mathf.Lerp(1.5f, 0.5f, distanceRatio);
    
    // NoteDataのVisualScaleも考慮
    float finalScale = scaleFactor;
    if (noteData != null)
    {
        finalScale *= noteData.VisualScale;
    }
    
    // スケールを適用
    transform.localScale = initialScale * finalScale;
}
```

#### SetupHoldTrail(float holdDuration)
```csharp
private void SetupHoldTrail(float holdDuration)
{
    trailRenderer = GetComponent<TrailRenderer>();
    if (trailRenderer == null)
    {
        trailRenderer = gameObject.AddComponent<TrailRenderer>();
    }
    
    // Holdの長さに応じてTrailの時間を設定
    // ビート数を秒数に変換
    float holdSeconds = holdDuration * (60.0f / conductor.songBpm);
    trailRenderer.time = holdSeconds;
    
    // Trail視覚設定
    trailRenderer.startWidth = 0.5f;
    trailRenderer.endWidth = 0.1f;
    trailRenderer.material = renderer.material;
}
```

#### OnMiss()
```csharp
private void OnMiss()
{
    // ミス処理
    Debug.Log($"ノーツミス: Lane {noteData.LaneIndex}, Beat {targetBeat}");
    
    // ミスエフェクト（実装予定）
    // EffectManager.Instance.PlayMissEffect(transform.position);
    
    // スコア減点（ScoreManager実装後）
    // ScoreManager.Instance.OnMiss();
}
```

### リセットメソッド

#### Reset()
```csharp
public void Reset()
{
    // フラグのリセット
    hasBeenHit = false;
    isCompleted = false;
    
    // データのクリア
    noteData = null;
    targetBeat = 0f;
    
    // 位置とスケールのリセット
    transform.position = Vector3.zero;
    transform.localScale = Vector3.one;
    
    // Trailのリセット
    if (trailRenderer != null)
    {
        trailRenderer.Clear();
    }
    
    // 表示を有効化
    gameObject.SetActive(true);
}
```

## 判定タイプ定義

```csharp
public enum JudgmentType
{
    Perfect,  // 完璧なタイミング
    Great,    // 良いタイミング
    Good,     // 普通のタイミング
    Miss      // ミス
}
```

## パフォーマンス最適化

### オブジェクトプーリング対応
- `Reset()`メソッドによる状態の完全リセット
- `SetActive()`による有効/無効切り替え
- Destroy()の代わりにプールへの返却を想定

### キャッシュ戦略
- コンポーネント参照のキャッシュ（renderer、trailRenderer）
- Conductorシングルトンの参照保持
- 初期スケール値の記録

### 計算の最適化
- Update内での不要な計算を避ける
- 距離計算は必要時のみ実行
- スケーリング計算の簡略化

## 他コンポーネントとの連携

### Conductor
- タイミング同期とZ座標計算
- BPM情報の取得
- 判定ライン位置の参照

### NoteSpawner
- Initialize()メソッドによる初期化
- オブジェクトプーリングからの取得と返却

### ScoreManager（実装予定）
- 判定結果の通知
- スコア加算処理
- コンボ管理

### EffectManager（実装予定）
- ヒットエフェクトの生成
- ミスエフェクトの表示

## テスト要件

### 単体テスト
1. 初期化処理の検証
2. Z座標計算の正確性
3. スケーリング計算の妥当性
4. タイミング判定の精度
5. リセット処理の完全性

### 統合テスト
1. Conductorとの連携動作
2. NoteSpawnerからの生成と初期化
3. 判定タイミングの実機検証
4. Holdノーツの視覚表現

## 今後の拡張予定

1. **フリックノーツ対応**: 横方向の入力を必要とするノーツ
2. **同時押し対応**: 複数レーンの同時判定
3. **カスタムアニメーション**: ノーツ種別ごとの動きの差別化
4. **難易度調整**: 判定幅の動的変更
5. **視覚エフェクト強化**: パーティクルシステムとの連携