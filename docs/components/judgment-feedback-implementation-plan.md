# 判定フィードバックシステム実装計画書

## 1. 実装概要

### 1.1 目的
判定結果をプレイヤーに視覚的にフィードバックするシステムを段階的に実装する。

### 1.2 スコープ
- 判定テキスト表示システム（JudgmentTextDisplay）
- 判定エフェクト管理システム（JudgmentEffectManager）
- 既存システムとの統合

### 1.3 実装期間目安
- Phase 1: 2-3時間（基本機能）
- Phase 2: 2-3時間（エフェクト統合）
- Phase 3: 1-2時間（最適化・調整）

## 2. 前提条件と依存関係

### 2.1 必要な既存コンポーネント
- ✅ InputManager（実装済み）
- ✅ JudgmentZone（実装済み）
- ✅ JudgmentType（実装済み）
- ✅ Conductor（実装済み）

### 2.2 必要なUnityパッケージ
- ✅ TextMeshPro（インストール済み）
- ✅ Universal Render Pipeline（設定済み）

### 2.3 必要なアセット
- ⚠️ 日本語フォント（NotoSansJP推奨）
- ✅ エフェクトプレハブ（作成済み）

## 3. Phase 1: 判定テキスト表示システム

### 3.1 実装手順

#### Step 1: JudgmentTextDisplayスクリプトの作成
**ファイル**: `Assets/_Jirou/Scripts/UI/JudgmentTextDisplay.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Jirou.Core;
using Jirou.Gameplay;

namespace Jirou.UI
{
    public class JudgmentTextDisplay : MonoBehaviour
    {
        [Header("Text Settings")]
        [SerializeField] private GameObject textPrefab;
        [SerializeField] private float displayDuration = 1.0f;
        [SerializeField] private float moveSpeed = 2.0f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Judgment Settings")]
        [SerializeField] private JudgmentDisplaySettings[] judgmentSettings;
        
        [Header("Pool Settings")]
        [SerializeField] private int poolSize = 20;
        
        private Queue<GameObject> textPool;
        private Dictionary<JudgmentType, JudgmentDisplaySettings> settingsDict;
        
        [System.Serializable]
        public class JudgmentDisplaySettings
        {
            public JudgmentType type;
            public string displayText;
            public Color color;
            public float scale = 1.0f;
        }
        
        void Awake()
        {
            InitializePool();
            InitializeSettings();
        }
        
        void OnEnable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged += OnNoteJudged;
            }
        }
        
        void OnDisable()
        {
            if (InputManager.Instance != null)
            {
                InputManager.OnNoteJudged -= OnNoteJudged;
            }
        }
        
        // 実装詳細は設計書参照
    }
}
```

#### Step 2: テキストプレハブの作成

1. **GameObject作成**
   ```
   Hierarchy > Create Empty > "JudgmentTextPrefab"
   ```

2. **TextMeshProコンポーネント追加**
   ```
   Add Component > TextMeshPro - Text
   設定:
   - Font Size: 48
   - Alignment: Center
   - Font Style: Bold
   ```

3. **Billboardスクリプト追加**
   ```csharp
   // Assets/_Jirou/Scripts/UI/Billboard.cs
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
               transform.LookAt(
                   transform.position + mainCamera.transform.rotation * Vector3.forward,
                   mainCamera.transform.rotation * Vector3.up
               );
           }
       }
   }
   ```

4. **プレハブ化**
   ```
   Prefabs/UI/JudgmentTextPrefab.prefab として保存
   ```

#### Step 3: シーンへの配置

1. **JudgmentTextDisplayオブジェクト作成**
   ```
   Hierarchy > Create Empty > "JudgmentTextDisplay"
   Position: (0, 0, 0)
   ```

2. **スクリプトアタッチと設定**
   ```
   Add Component > JudgmentTextDisplay
   設定:
   - Text Prefab: JudgmentTextPrefab
   - Display Duration: 1.0
   - Move Speed: 2.0
   - Pool Size: 20
   ```

3. **判定設定の入力**
   ```
   Judgment Settings:
   - Perfect: "Perfect", Yellow, Scale 1.2
   - Great: "Great", Green, Scale 1.1
   - Good: "Good", Cyan, Scale 1.0
   - Miss: "Miss", Red, Scale 0.9
   ```

### 3.2 テストと検証

#### テストシナリオ1: 基本動作確認
```
1. PlayModeに入る
2. D,F,J,Kキーを押す
3. 判定テキストが表示されることを確認
4. テキストがフェードアウトすることを確認
```

#### テストシナリオ2: 連続入力
```
1. 複数のキーを素早く連打
2. テキストが重ならずに表示されることを確認
3. パフォーマンスが低下しないことを確認
```

## 4. Phase 2: エフェクトシステム統合

### 4.1 実装手順

#### Step 1: JudgmentEffectManagerスクリプトの作成
**ファイル**: `Assets/_Jirou/Scripts/Visual/JudgmentEffectManager.cs`

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jirou.Core;
using Jirou.Gameplay;

namespace Jirou.Visual
{
    public class JudgmentEffectManager : MonoBehaviour
    {
        [Header("Effect Prefabs")]
        [SerializeField] private GameObject perfectEffectPrefab;
        [SerializeField] private GameObject greatEffectPrefab;
        [SerializeField] private GameObject goodEffectPrefab;
        [SerializeField] private GameObject missEffectPrefab;
        
        [Header("Effect Settings")]
        [SerializeField] private float effectDuration = 1.5f;
        [SerializeField] private bool useObjectPool = true;
        [SerializeField] private int poolSizePerType = 10;
        
        private Dictionary<JudgmentType, GameObject> effectPrefabs;
        private Dictionary<JudgmentType, Queue<GameObject>> effectPools;
        
        void Awake()
        {
            InitializePrefabDictionary();
            if (useObjectPool)
            {
                InitializeObjectPools();
            }
        }
        
        // 実装詳細は設計書参照
    }
}
```

#### Step 2: 既存エフェクトプレハブの検証と調整

1. **プレハブの確認**
   ```
   Prefabs/Effects/
   ├── PerfectHitEffect.prefab
   ├── GreatHitEffect.prefab
   ├── GoodHitEffect.prefab
   └── MissEffect.prefab
   ```

2. **ParticleSystemの調整**
   ```
   各プレハブ:
   - Duration: 1.5秒
   - Start Lifetime: 1.0秒
   - Start Speed: 2-5
   - Emission Rate: 50-100
   ```

#### Step 3: InputManagerとの連携確認

1. **InputManager.csの確認**
   ```csharp
   // Line 145-148の確認
   if (hitEffectPrefab != null && judgment != JudgmentType.Miss)
   {
       SpawnHitEffect(laneIndex, judgment);
   }
   ```

2. **エフェクトプレハブの割り当て**
   ```
   InputManagerのInspector:
   - Hit Effect Prefab: 一旦nullのまま（JudgmentEffectManagerで管理）
   ```

### 4.2 統合テスト

#### テストシナリオ1: エフェクト表示
```
1. 各判定タイプのエフェクトが表示される
2. エフェクトの位置が正しい
3. エフェクトの持続時間が適切
```

#### テストシナリオ2: パフォーマンス
```
1. 連続判定時のFPS測定
2. メモリ使用量の監視
3. オブジェクトプールの動作確認
```

## 5. Phase 3: 最適化と調整

### 5.1 パフォーマンス最適化

#### Step 1: オブジェクトプールの調整
```csharp
// プールサイズの動的調整
private void ExpandPool(JudgmentType type, int additionalCount)
{
    for (int i = 0; i < additionalCount; i++)
    {
        GameObject obj = Instantiate(effectPrefabs[type]);
        obj.SetActive(false);
        effectPools[type].Enqueue(obj);
    }
}
```

#### Step 2: LOD実装（オプション）
```csharp
// カメラ距離に応じた品質調整
private void AdjustEffectQuality(GameObject effect, float distance)
{
    ParticleSystem ps = effect.GetComponent<ParticleSystem>();
    if (distance > 10f)
    {
        var emission = ps.emission;
        emission.rateOverTime = emission.rateOverTime.constant * 0.5f;
    }
}
```

### 5.2 視覚的調整

#### Step 1: アニメーションカーブの調整
```
Inspector上で調整:
- Fade Curve: より滑らかなカーブに
- Scale Animation: バウンス効果の追加
- Position Animation: イージング関数の適用
```

#### Step 2: 色とコントラストの最適化
```
各判定タイプの色調整:
- Perfect: HDR Yellow (Intensity 2.0)
- Great: Bright Green
- Good: Light Blue
- Miss: Dark Red
```

## 6. 実装チェックリスト

### Phase 1 チェックリスト
- [ ] JudgmentTextDisplay.cs作成
- [ ] Billboard.cs作成
- [ ] テキストプレハブ作成
- [ ] シーンへの配置
- [ ] InputManagerとの連携確認
- [ ] 基本動作テスト

### Phase 2 チェックリスト
- [ ] JudgmentEffectManager.cs作成
- [ ] エフェクトプレハブの調整
- [ ] オブジェクトプール実装
- [ ] エフェクト表示テスト
- [ ] パフォーマンステスト

### Phase 3 チェックリスト
- [ ] プール最適化
- [ ] アニメーション調整
- [ ] 色・コントラスト調整
- [ ] 最終統合テスト
- [ ] パフォーマンス測定

## 7. トラブルシューティング

### 7.1 よくある問題と解決方法

#### 問題1: テキストが表示されない
```
原因と解決:
1. TextMeshProがインポートされていない
   → Window > TextMeshPro > Import TMP Essential Resources
2. レイヤー設定の問題
   → テキストオブジェクトのLayerを"UI"に設定
3. カメラのCulling Mask確認
   → UIレイヤーが含まれているか確認
```

#### 問題2: エフェクトが見えない
```
原因と解決:
1. Sorting Layerの問題
   → Particle SystemのRendererでOrder in Layerを調整
2. カメラのClipping Planes
   → Near/Farの値を確認
3. マテリアルの設定
   → Shader設定を確認
```

#### 問題3: パフォーマンス低下
```
原因と解決:
1. オブジェクトプールが機能していない
   → useObjectPoolがtrueになっているか確認
2. パーティクル数が多すぎる
   → Max Particlesを調整
3. テクスチャサイズが大きい
   → テクスチャ圧縮設定を確認
```

### 7.2 デバッグ用コード

```csharp
// デバッグ表示用
#if UNITY_EDITOR
void OnGUI()
{
    if (showDebugInfo)
    {
        GUILayout.Label($"Active Texts: {GetActiveTextCount()}");
        GUILayout.Label($"Pool Size: {textPool.Count}");
        GUILayout.Label($"Last Judgment: {lastJudgmentType}");
    }
}
#endif
```

## 8. 実装後の確認事項

### 8.1 機能確認
- [ ] Perfect判定時: 黄色テキスト+エフェクト表示
- [ ] Great判定時: 緑色テキスト+エフェクト表示
- [ ] Good判定時: 水色テキスト+エフェクト表示
- [ ] Miss判定時: 赤色テキスト+エフェクト表示

### 8.2 品質確認
- [ ] テキストの視認性が良好
- [ ] エフェクトが適切な位置に表示
- [ ] アニメーションが滑らか
- [ ] 音との同期が取れている

### 8.3 パフォーマンス確認
- [ ] 60FPS維持（PC環境）
- [ ] メモリ使用量が安定
- [ ] GC Allocが最小限

## 9. 次のステップ

### 9.1 追加機能の候補
1. コンボ数表示システム
2. スコア加算アニメーション
3. 判定ライン発光エフェクト
4. カメラシェイク機能

### 9.2 改善項目
1. カスタマイズ可能なUI設定
2. アクセシビリティ対応
3. パフォーマンスプロファイリング
4. ユーザーフィードバックの収集

## 10. 参考資料

### 10.1 関連ドキュメント
- `/docs/design/judgment-feedback-system-design.md`
- `/docs/architectures/jirou-implementation-status-report.md`
- `/docs/guidelines/csharp-coding-standards.md`

### 10.2 Unity公式ドキュメント
- [TextMeshPro Documentation](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html)
- [Particle System](https://docs.unity3d.com/Manual/PartSysReference.html)
- [Object Pooling Pattern](https://learn.unity.com/tutorial/object-pooling)