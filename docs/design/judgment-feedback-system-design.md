# 判定フィードバックシステム詳細設計書

## 1. 概要

### 1.1 目的
奥行き型リズムゲーム「Jirou」において、プレイヤーがノーツを叩いた際の判定結果を視覚的にフィードバックするシステムを設計する。

### 1.2 現状の問題
- 判定は内部的に処理されているが、プレイヤーに視覚的なフィードバックが提供されていない
- InputManagerからOnNoteJudgedイベントが発行されているが、これを受け取って表示するコンポーネントが存在しない
- HitEffectプレハブは作成されているが、活用されていない

### 1.3 期待される動作
- キーを押してノーツを判定した際に、判定結果（Perfect/Great/Good/Miss）がテキストとして表示される
- 判定に応じたエフェクト（パーティクル等）が表示される
- フィードバックは3D空間内で表示され、奥行き感を保つ

## 2. システムアーキテクチャ

### 2.1 コンポーネント構成

```
InputManager (既存)
    ↓ OnNoteJudged イベント
┌──────────────────┬─────────────────┐
↓                  ↓                 ↓
JudgmentTextDisplay  JudgmentEffectManager  ScoreManager(既存)
（新規作成）          （新規作成）            
```

### 2.2 主要コンポーネント

#### 2.2.1 JudgmentTextDisplay
- **役割**: 判定結果のテキストを3D空間に表示
- **機能**:
  - InputManagerのOnNoteJudgedイベントを購読
  - 判定タイプに応じたテキスト表示
  - アニメーション（フェードイン・アウト、上昇移動）
  - オブジェクトプール管理

#### 2.2.2 JudgmentEffectManager
- **役割**: 判定エフェクトの統合管理
- **機能**:
  - InputManagerのOnNoteJudgedイベントを購読
  - 判定タイプに応じたエフェクトプレハブの生成
  - エフェクトの位置・タイミング制御
  - オブジェクトプール管理

## 3. 詳細設計

### 3.1 JudgmentTextDisplay

#### 3.1.1 クラス構造

```csharp
namespace Jirou.UI
{
    public class JudgmentTextDisplay : MonoBehaviour
    {
        // 設定フィールド
        [Header("Text Settings")]
        [SerializeField] private GameObject textPrefab;
        [SerializeField] private float displayDuration = 1.0f;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private AnimationCurve fadeCurve;
        
        [Header("Judgment Colors")]
        [SerializeField] private Color perfectColor = Color.yellow;
        [SerializeField] private Color greatColor = Color.green;
        [SerializeField] private Color goodColor = Color.cyan;
        [SerializeField] private Color missColor = Color.red;
        
        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 20;
        
        // プライベートフィールド
        private Queue<GameObject> textPool;
        private Dictionary<JudgmentType, string> judgmentTexts;
        private Dictionary<JudgmentType, Color> judgmentColors;
        
        // メソッド
        void Awake();
        void OnEnable();
        void OnDisable();
        void InitializePool();
        void InitializeDictionaries();
        void OnNoteJudged(int laneIndex, JudgmentType judgment);
        void DisplayJudgmentText(int laneIndex, JudgmentType judgment);
        GameObject GetPooledText();
        void ReturnToPool(GameObject text);
        IEnumerator AnimateText(GameObject textObj, Vector3 startPos);
    }
}
```

#### 3.1.2 表示位置計算

```csharp
// レーンのX座標: -3, -1, 1, 3
float xPosition = -3f + (laneIndex * 2f);

// Y座標: 判定ラインより少し上
float yPosition = 2.0f;

// Z座標: 判定ライン付近
float zPosition = 0.5f;

Vector3 displayPosition = new Vector3(xPosition, yPosition, zPosition);
```

#### 3.1.3 テキストアニメーション

```csharp
IEnumerator AnimateText(GameObject textObj, Vector3 startPos)
{
    float elapsed = 0f;
    TextMeshPro tmp = textObj.GetComponent<TextMeshPro>();
    Color originalColor = tmp.color;
    
    while (elapsed < displayDuration)
    {
        float t = elapsed / displayDuration;
        
        // 上方向への移動
        float yOffset = moveSpeed * t;
        textObj.transform.position = startPos + Vector3.up * yOffset;
        
        // フェードアウト
        float alpha = fadeCurve.Evaluate(t);
        Color currentColor = originalColor;
        currentColor.a = alpha;
        tmp.color = currentColor;
        
        // スケールアニメーション（オプション）
        float scale = 1.0f + (0.5f * Mathf.Sin(t * Mathf.PI));
        textObj.transform.localScale = Vector3.one * scale;
        
        elapsed += Time.deltaTime;
        yield return null;
    }
    
    ReturnToPool(textObj);
}
```

### 3.2 JudgmentEffectManager

#### 3.2.1 クラス構造

```csharp
namespace Jirou.Visual
{
    public class JudgmentEffectManager : MonoBehaviour
    {
        // 設定フィールド
        [Header("Effect Prefabs")]
        [SerializeField] private GameObject perfectEffectPrefab;
        [SerializeField] private GameObject greatEffectPrefab;
        [SerializeField] private GameObject goodEffectPrefab;
        [SerializeField] private GameObject missEffectPrefab;
        
        [Header("Effect Settings")]
        [SerializeField] private float effectDuration = 1.5f;
        [SerializeField] private bool useObjectPool = true;
        [SerializeField] private int poolSizePerType = 10;
        
        // プライベートフィールド
        private Dictionary<JudgmentType, GameObject> effectPrefabs;
        private Dictionary<JudgmentType, Queue<GameObject>> effectPools;
        
        // メソッド
        void Awake();
        void OnEnable();
        void OnDisable();
        void InitializePrefabDictionary();
        void InitializeObjectPools();
        void OnNoteJudged(int laneIndex, JudgmentType judgment);
        void SpawnEffect(int laneIndex, JudgmentType judgment);
        GameObject GetPooledEffect(JudgmentType judgment);
        void ReturnToPool(GameObject effect, JudgmentType judgment);
        IEnumerator DeactivateAfterDelay(GameObject effect, JudgmentType judgment);
    }
}
```

#### 3.2.2 エフェクト生成位置

```csharp
Vector3 GetEffectPosition(int laneIndex)
{
    // レーンのX座標
    float xPosition = -3f + (laneIndex * 2f);
    
    // Y座標: 判定ライン上
    float yPosition = 0.5f;
    
    // Z座標: 判定ライン
    float zPosition = 0f;
    
    return new Vector3(xPosition, yPosition, zPosition);
}
```

### 3.3 TextMeshPro設定

#### 3.3.1 3Dテキストプレハブ構成

```
JudgmentText (Prefab)
├── TextMeshPro Component
│   ├── Font Asset: NotoSansJP-Bold SDF
│   ├── Font Size: 36
│   ├── Alignment: Center
│   ├── Color: White (動的に変更)
│   └── Sorting Layer: UI
├── RectTransform
│   ├── Width: 200
│   ├── Height: 100
│   └── Pivot: (0.5, 0.5)
└── Billboard Script (オプション)
    └── カメラに向かって常に正面を向く
```

#### 3.3.2 Billboard機能

```csharp
public class Billboard : MonoBehaviour
{
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
    }
    
    void LateUpdate()
    {
        if (mainCamera != null)
        {
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                           mainCamera.transform.rotation * Vector3.up);
        }
    }
}
```

## 4. パフォーマンス最適化

### 4.1 オブジェクトプール

- テキストとエフェクトの両方でオブジェクトプールを使用
- 初期プールサイズ: 各判定タイプごとに10個
- 動的拡張: 必要に応じてプールを拡張

### 4.2 描画最適化

- LOD（Level of Detail）の実装を検討
- カメラから遠い位置のエフェクトは簡略化
- バッチング可能なマテリアル設定

### 4.3 メモリ管理

- 不要なテクスチャの圧縮
- エフェクトのライフタイム管理
- ガベージコレクションの最小化

## 5. 拡張性

### 5.1 カスタマイズ可能な要素

- 判定テキストのフォント、サイズ、色
- エフェクトの種類と強度
- アニメーションパターン
- 表示位置のオフセット

### 5.2 将来の機能追加

- コンボ数の表示
- 連続Perfect時の特別エフェクト
- スコア加算アニメーション
- カメラシェイク効果

## 6. 実装優先順位

1. **Phase 1**: JudgmentTextDisplayの基本実装
   - テキスト表示機能
   - 基本的なフェードアニメーション
   - InputManagerとの連携

2. **Phase 2**: エフェクトシステムの実装
   - JudgmentEffectManagerの実装
   - 既存のエフェクトプレハブとの連携
   - オブジェクトプール実装

3. **Phase 3**: 最適化と改善
   - パフォーマンス最適化
   - 視覚的な調整
   - ユーザー設定の追加

## 7. テスト項目

### 7.1 機能テスト
- [ ] 各判定タイプでテキストが表示される
- [ ] エフェクトが正しい位置に生成される
- [ ] アニメーションが正常に動作する
- [ ] オブジェクトプールが正しく機能する

### 7.2 パフォーマンステスト
- [ ] 連続判定時のフレームレート維持
- [ ] メモリリークがない
- [ ] オブジェクト生成/破棄のオーバーヘッドが最小

### 7.3 統合テスト
- [ ] InputManagerとの連携が正常
- [ ] 他のUIシステムとの競合がない
- [ ] ゲームプレイ中の視認性が良好

## 8. リスクと対策

### 8.1 パフォーマンス低下
- **リスク**: 大量のテキスト/エフェクト表示によるFPS低下
- **対策**: オブジェクトプール、LOD実装、表示数制限

### 8.2 視認性の問題
- **リスク**: 3D空間でのテキスト読みづらさ
- **対策**: Billboard実装、適切なフォントサイズ、コントラスト調整

### 8.3 タイミングのズレ
- **リスク**: 判定とフィードバック表示のタイミングずれ
- **対策**: イベント駆動型実装、フレーム独立のアニメーション