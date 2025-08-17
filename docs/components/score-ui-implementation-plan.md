# スコアUIシステム実装計画書

## 概要

本ドキュメントでは、スコアUIシステムの実装手順と詳細な技術仕様を定義します。設計書（`docs/design/score-ui-system-design.md`）に基づき、段階的な実装アプローチを採用します。

## 実装スケジュール

### Phase 1: 基本UI構築（2日）

#### Day 1: Canvas構築とレイアウト

1. **Canvas作成**
   ```
   手順:
   1. HierarchyでCreate > UI > Canvas
   2. 名前を "ScoreUICanvas" に変更
   3. Canvas Scaler設定:
      - UI Scale Mode: Scale With Screen Size
      - Reference Resolution: 1920 x 1080
      - Screen Match Mode: 0.5
   ```

2. **UI階層構造の作成**
   ```
   ScoreUICanvas/
   ├── TopPanel (Horizontal Layout Group)
   │   ├── ScoreContainer
   │   │   ├── ScoreLabel (TextMeshPro)
   │   │   └── ScoreValue (TextMeshPro)
   │   └── Spacer
   ├── JudgmentPanel (Grid Layout Group)
   │   ├── PerfectContainer
   │   │   ├── PerfectLabel
   │   │   └── PerfectCount
   │   ├── GreatContainer
   │   ├── GoodContainer
   │   └── MissContainer
   └── ComboPanel
       ├── ComboLabel
       └── ComboNumber
   ```

#### Day 2: 基本スクリプト実装

1. **ScoreUIManager.cs**
   ```csharp
   using UnityEngine;
   using TMPro;
   
   namespace Jirou.UI
   {
       public class ScoreUIManager : MonoBehaviour
       {
           [Header("Score Display")]
           [SerializeField] private TextMeshProUGUI scoreText;
           
           [Header("Judgment Counts")]
           [SerializeField] private TextMeshProUGUI perfectCountText;
           [SerializeField] private TextMeshProUGUI greatCountText;
           [SerializeField] private TextMeshProUGUI goodCountText;
           [SerializeField] private TextMeshProUGUI missCountText;
           
           [Header("Combo Display")]
           [SerializeField] private TextMeshProUGUI comboText;
           [SerializeField] private GameObject comboContainer;
           
           private ScoreManager scoreManager;
           private int currentScore;
           private int[] judgmentCounts = new int[4];
           private int currentCombo;
           
           void Start()
           {
               InitializeUI();
               SubscribeToEvents();
           }
           
           private void InitializeUI()
           {
               UpdateScoreDisplay(0);
               ResetJudgmentCounts();
               HideCombo();
           }
       }
   }
   ```

### Phase 2: ScoreManager連携（2日）

#### Day 3: イベントシステム実装

1. **ScoreUIEvents.cs**
   ```csharp
   using System;
   using UnityEngine;
   
   namespace Jirou.UI
   {
       public static class ScoreUIEvents
       {
           public static event Action<int> OnScoreChanged;
           public static event Action<JudgmentType> OnJudgmentOccurred;
           public static event Action<int> OnComboChanged;
           public static event Action OnComboBreak;
           
           public static void TriggerScoreChange(int newScore)
           {
               OnScoreChanged?.Invoke(newScore);
           }
           
           public static void TriggerJudgment(JudgmentType type)
           {
               OnJudgmentOccurred?.Invoke(type);
           }
           
           public static void TriggerComboChange(int combo)
           {
               OnComboChanged?.Invoke(combo);
           }
           
           public static void TriggerComboBreak()
           {
               OnComboBreak?.Invoke();
           }
       }
   }
   ```

2. **ScoreManager統合**
   ```csharp
   // ScoreManager.cs への追加
   public class ScoreManager : MonoBehaviour
   {
       // 既存のコード...
       
       private void ProcessJudgment(JudgmentType type)
       {
           int points = CalculatePoints(type);
           currentScore += points;
           
           // UI通知
           ScoreUIEvents.TriggerScoreChange(currentScore);
           ScoreUIEvents.TriggerJudgment(type);
           
           if (type != JudgmentType.Miss)
           {
               currentCombo++;
               ScoreUIEvents.TriggerComboChange(currentCombo);
           }
           else
           {
               currentCombo = 0;
               ScoreUIEvents.TriggerComboBreak();
           }
       }
   }
   ```

#### Day 4: 表示更新ロジック

1. **ScoreDisplay.cs**
   ```csharp
   using UnityEngine;
   using TMPro;
   using System.Collections;
   
   namespace Jirou.UI
   {
       public class ScoreDisplay : MonoBehaviour
       {
           [SerializeField] private TextMeshProUGUI scoreText;
           [SerializeField] private float animationDuration = 0.3f;
           [SerializeField] private AnimationCurve scaleCurve;
           
           private int currentDisplayScore;
           private int targetScore;
           private Coroutine animationCoroutine;
           
           public void UpdateScore(int newScore)
           {
               targetScore = newScore;
               
               if (animationCoroutine != null)
                   StopCoroutine(animationCoroutine);
               
               animationCoroutine = StartCoroutine(AnimateScore());
           }
           
           private IEnumerator AnimateScore()
           {
               float startScore = currentDisplayScore;
               float elapsed = 0f;
               
               while (elapsed < animationDuration)
               {
                   elapsed += Time.deltaTime;
                   float t = elapsed / animationDuration;
                   
                   currentDisplayScore = Mathf.RoundToInt(
                       Mathf.Lerp(startScore, targetScore, t)
                   );
                   
                   scoreText.text = currentDisplayScore.ToString("D7");
                   
                   // スケールアニメーション
                   float scale = scaleCurve.Evaluate(t);
                   scoreText.transform.localScale = Vector3.one * scale;
                   
                   yield return null;
               }
               
               currentDisplayScore = targetScore;
               scoreText.text = currentDisplayScore.ToString("D7");
               scoreText.transform.localScale = Vector3.one;
           }
       }
   }
   ```

### Phase 3: ビジュアルエフェクト（3日）

#### Day 5: 判定エフェクト実装

1. **JudgmentPopup.cs**
   ```csharp
   using UnityEngine;
   using TMPro;
   using DG.Tweening;
   
   namespace Jirou.UI
   {
       public class JudgmentPopup : MonoBehaviour
       {
           [SerializeField] private TextMeshProUGUI judgmentText;
           [SerializeField] private float moveDistance = 50f;
           [SerializeField] private float duration = 0.5f;
           [SerializeField] private float fadeDelay = 0.3f;
           
           private CanvasGroup canvasGroup;
           
           void Awake()
           {
               canvasGroup = GetComponent<CanvasGroup>();
           }
           
           public void Show(JudgmentType type, Vector3 position)
           {
               transform.position = position;
               SetupAppearance(type);
               PlayAnimation();
           }
           
           private void SetupAppearance(JudgmentType type)
           {
               switch(type)
               {
                   case JudgmentType.Perfect:
                       judgmentText.text = "PERFECT!";
                       judgmentText.color = new Color(1f, 0.843f, 0f); // Gold
                       break;
                   case JudgmentType.Great:
                       judgmentText.text = "GREAT!";
                       judgmentText.color = Color.green;
                       break;
                   case JudgmentType.Good:
                       judgmentText.text = "GOOD";
                       judgmentText.color = Color.cyan;
                       break;
                   case JudgmentType.Miss:
                       judgmentText.text = "MISS";
                       judgmentText.color = Color.red;
                       break;
               }
           }
           
           private void PlayAnimation()
           {
               // リセット
               transform.localScale = Vector3.zero;
               canvasGroup.alpha = 1f;
               
               // アニメーションシーケンス
               Sequence sequence = DOTween.Sequence();
               
               // スケールイン
               sequence.Append(transform.DOScale(1f, 0.2f)
                   .SetEase(Ease.OutBack));
               
               // 上方向に移動
               sequence.Join(transform.DOMoveY(
                   transform.position.y + moveDistance, 
                   duration
               ).SetEase(Ease.OutQuad));
               
               // フェードアウト
               sequence.Insert(fadeDelay, 
                   canvasGroup.DOFade(0f, duration - fadeDelay));
               
               // 完了時にプールに返却
               sequence.OnComplete(() => 
                   UIEffectPool.Instance.ReturnPopup(this));
           }
       }
   }
   ```

#### Day 6: コンボエフェクト実装

1. **ComboEffectController.cs**
   ```csharp
   using UnityEngine;
   using UnityEngine.UI;
   using DG.Tweening;
   using System.Collections;
   
   namespace Jirou.UI
   {
       public class ComboEffectController : MonoBehaviour
       {
           [Header("Milestone Effects")]
           [SerializeField] private ParticleSystem combo50Effect;
           [SerializeField] private ParticleSystem combo100Effect;
           [SerializeField] private ParticleSystem combo200Effect;
           
           [Header("Screen Effects")]
           [SerializeField] private Image screenFlash;
           [SerializeField] private float flashDuration = 0.2f;
           
           [Header("Combo Text Effects")]
           [SerializeField] private TextMeshProUGUI comboText;
           [SerializeField] private float pulseDuration = 0.3f;
           [SerializeField] private AnimationCurve pulseCurve;
           
           private int lastMilestone = 0;
           
           public void OnComboUpdate(int combo)
           {
               // パルスアニメーション
               PulseComboText();
               
               // マイルストーンチェック
               CheckMilestone(combo);
           }
           
           private void CheckMilestone(int combo)
           {
               if (combo == 50 && lastMilestone < 50)
               {
                   PlayMilestoneEffect(combo50Effect);
                   lastMilestone = 50;
               }
               else if (combo == 100 && lastMilestone < 100)
               {
                   PlayMilestoneEffect(combo100Effect);
                   StartCoroutine(ScreenFlash());
                   lastMilestone = 100;
               }
               else if (combo == 200 && lastMilestone < 200)
               {
                   PlayMilestoneEffect(combo200Effect);
                   StartCoroutine(ScreenFlash(Color.yellow));
                   lastMilestone = 200;
               }
           }
           
           private void PlayMilestoneEffect(ParticleSystem effect)
           {
               effect.Play();
           }
           
           private IEnumerator ScreenFlash(Color color = default)
           {
               if (color == default) color = Color.white;
               
               screenFlash.color = new Color(color.r, color.g, color.b, 0);
               screenFlash.gameObject.SetActive(true);
               
               // フラッシュイン
               yield return screenFlash.DOFade(0.3f, flashDuration * 0.3f)
                   .WaitForCompletion();
               
               // フラッシュアウト
               yield return screenFlash.DOFade(0f, flashDuration * 0.7f)
                   .WaitForCompletion();
               
               screenFlash.gameObject.SetActive(false);
           }
           
           private void PulseComboText()
           {
               comboText.transform.DOKill();
               comboText.transform.localScale = Vector3.one;
               
               comboText.transform.DOPunchScale(
                   Vector3.one * 0.2f, 
                   pulseDuration, 
                   1, 
                   0.5f
               );
           }
           
           public void OnComboBreak()
           {
               lastMilestone = 0;
               
               // ブレイクアニメーション
               comboText.transform.DOShakePosition(0.3f, 10f);
               comboText.DOColor(Color.red, 0.1f)
                   .OnComplete(() => comboText.DOColor(Color.white, 0.3f));
           }
       }
   }
   ```

#### Day 7: オブジェクトプール実装

1. **UIEffectPool.cs**
   ```csharp
   using UnityEngine;
   using System.Collections.Generic;
   
   namespace Jirou.UI
   {
       public class UIEffectPool : MonoBehaviour
       {
           private static UIEffectPool instance;
           public static UIEffectPool Instance => instance;
           
           [Header("Prefabs")]
           [SerializeField] private GameObject judgmentPopupPrefab;
           [SerializeField] private GameObject comboEffectPrefab;
           
           [Header("Pool Settings")]
           [SerializeField] private int initialPoolSize = 10;
           
           private Queue<JudgmentPopup> popupPool;
           private Queue<GameObject> effectPool;
           private Transform poolContainer;
           
           void Awake()
           {
               if (instance == null)
               {
                   instance = this;
                   InitializePools();
               }
               else
               {
                   Destroy(gameObject);
               }
           }
           
           private void InitializePools()
           {
               poolContainer = new GameObject("UIEffectPool").transform;
               poolContainer.SetParent(transform);
               
               // 判定ポップアッププール
               popupPool = new Queue<JudgmentPopup>();
               for (int i = 0; i < initialPoolSize; i++)
               {
                   CreateNewPopup();
               }
               
               // エフェクトプール
               effectPool = new Queue<GameObject>();
               for (int i = 0; i < initialPoolSize; i++)
               {
                   CreateNewEffect();
               }
           }
           
           private JudgmentPopup CreateNewPopup()
           {
               GameObject obj = Instantiate(judgmentPopupPrefab, poolContainer);
               obj.SetActive(false);
               JudgmentPopup popup = obj.GetComponent<JudgmentPopup>();
               popupPool.Enqueue(popup);
               return popup;
           }
           
           private GameObject CreateNewEffect()
           {
               GameObject obj = Instantiate(comboEffectPrefab, poolContainer);
               obj.SetActive(false);
               effectPool.Enqueue(obj);
               return obj;
           }
           
           public JudgmentPopup GetPopup()
           {
               if (popupPool.Count == 0)
               {
                   CreateNewPopup();
               }
               
               JudgmentPopup popup = popupPool.Dequeue();
               popup.gameObject.SetActive(true);
               return popup;
           }
           
           public void ReturnPopup(JudgmentPopup popup)
           {
               popup.gameObject.SetActive(false);
               popupPool.Enqueue(popup);
           }
           
           public GameObject GetEffect()
           {
               if (effectPool.Count == 0)
               {
                   CreateNewEffect();
               }
               
               GameObject effect = effectPool.Dequeue();
               effect.SetActive(true);
               return effect;
           }
           
           public void ReturnEffect(GameObject effect)
           {
               effect.SetActive(false);
               effectPool.Enqueue(effect);
           }
       }
   }
   ```

### Phase 4: 最適化とポリッシュ（2日）

#### Day 8: パフォーマンス最適化

1. **最適化実装項目**
   - StringBuilderを使用した文字列操作
   - 更新頻度の制御（60FPS固定）
   - CanvasGroupによるバッチング最適化
   - Static/Dynamicバッチングの設定

2. **ProfilerAnalyzer.cs**
   ```csharp
   using UnityEngine;
   using UnityEngine.Profiling;
   using System.Text;
   
   namespace Jirou.UI
   {
       public class UIPerformanceOptimizer : MonoBehaviour
       {
           private StringBuilder stringBuilder = new StringBuilder(16);
           private float updateInterval = 0.016f; // 60FPS
           private float lastUpdateTime;
           
           // キャッシュ
           private readonly string[] cachedNumbers = new string[10000];
           private readonly string scoreFormat = "D7";
           private readonly string comboFormat = "D4";
           
           void Awake()
           {
               // 数値文字列をプリキャッシュ
               for (int i = 0; i < cachedNumbers.Length; i++)
               {
                   cachedNumbers[i] = i.ToString();
               }
           }
           
           public string GetCachedNumber(int value)
           {
               if (value >= 0 && value < cachedNumbers.Length)
                   return cachedNumbers[value];
               
               return value.ToString();
           }
           
           public bool ShouldUpdate()
           {
               if (Time.time - lastUpdateTime >= updateInterval)
               {
                   lastUpdateTime = Time.time;
                   return true;
               }
               return false;
           }
           
           public string FormatScore(int score)
           {
               stringBuilder.Clear();
               stringBuilder.Append(score.ToString(scoreFormat));
               return stringBuilder.ToString();
           }
       }
   }
   ```

#### Day 9: 最終調整とテスト

1. **UIテストシーン作成**
   - テスト用のデバッグコマンド実装
   - 各種判定をシミュレート
   - ストレステスト機能

2. **ScoreUIDebugger.cs**
   ```csharp
   using UnityEngine;
   
   namespace Jirou.UI.Debug
   {
       public class ScoreUIDebugger : MonoBehaviour
       {
           [Header("Debug Controls")]
           [SerializeField] private bool enableDebugKeys = true;
           [SerializeField] private int scoreIncrement = 1000;
           
           private ScoreUIManager uiManager;
           
           void Start()
           {
               uiManager = GetComponent<ScoreUIManager>();
           }
           
           void Update()
           {
               if (!enableDebugKeys) return;
               
               // 判定シミュレート
               if (Input.GetKeyDown(KeyCode.Alpha1))
                   SimulateJudgment(JudgmentType.Perfect);
               if (Input.GetKeyDown(KeyCode.Alpha2))
                   SimulateJudgment(JudgmentType.Great);
               if (Input.GetKeyDown(KeyCode.Alpha3))
                   SimulateJudgment(JudgmentType.Good);
               if (Input.GetKeyDown(KeyCode.Alpha4))
                   SimulateJudgment(JudgmentType.Miss);
               
               // スコア直接追加
               if (Input.GetKeyDown(KeyCode.Space))
                   AddScore(scoreIncrement);
               
               // リセット
               if (Input.GetKeyDown(KeyCode.R))
                   ResetAll();
               
               // ストレステスト
               if (Input.GetKeyDown(KeyCode.T))
                   StartCoroutine(StressTest());
           }
           
           private void SimulateJudgment(JudgmentType type)
           {
               ScoreUIEvents.TriggerJudgment(type);
               
               int points = type switch
               {
                   JudgmentType.Perfect => 1000,
                   JudgmentType.Great => 500,
                   JudgmentType.Good => 100,
                   JudgmentType.Miss => 0,
                   _ => 0
               };
               
               if (points > 0)
                   ScoreUIEvents.TriggerScoreChange(points);
           }
           
           private System.Collections.IEnumerator StressTest()
           {
               Debug.Log("Starting UI Stress Test...");
               
               for (int i = 0; i < 100; i++)
               {
                   SimulateJudgment((JudgmentType)(Random.Range(0, 4)));
                   yield return new WaitForSeconds(0.05f);
               }
               
               Debug.Log("Stress Test Complete!");
           }
       }
   }
   ```

## Prefab構成

### ScoreUICanvas.prefab

```
ScoreUICanvas (Canvas)
├── EventSystem
├── UIEffectPool
├── TopPanel (RectTransform)
│   ├── ScoreContainer
│   │   ├── ScoreLabel (TextMeshPro: "SCORE")
│   │   └── ScoreValue (TextMeshPro: "0000000")
│   └── BGMIndicator
│       └── MusicIcon (Image)
├── JudgmentPanel (RectTransform)
│   ├── PerfectContainer (HorizontalLayoutGroup)
│   │   ├── PerfectLabel (TextMeshPro: "Perfect:")
│   │   └── PerfectCount (TextMeshPro: "000")
│   ├── GreatContainer
│   ├── GoodContainer
│   └── MissContainer
├── ComboPanel (RectTransform)
│   ├── ComboLabel (TextMeshPro: "COMBO")
│   ├── ComboNumber (TextMeshPro: "0")
│   └── ComboEffects
│       ├── Combo50Particles
│       ├── Combo100Particles
│       └── Combo200Particles
└── ScreenEffects
    └── FlashOverlay (Image)
```

## 依存関係

### 必要なパッケージ

1. **TextMeshPro** (組み込み済み)
   - バージョン: 3.0.6以上
   - 用途: 高品質テキストレンダリング

2. **DOTween** (要インストール)
   - バージョン: 1.2.745以上
   - 用途: アニメーション処理
   - インストール: https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676

3. **Unity Input System** (インストール済み)
   - バージョン: 1.4.4以上
   - 用途: デバッグ入力処理

### アセット準備

1. **フォント**
   - デジタルフォント（スコア用）
   - 日本語対応フォント（ラベル用）

2. **スプライト/テクスチャ**
   - 音符アイコン
   - 背景グラデーション
   - エフェクトテクスチャ

3. **パーティクルシステム**
   - コンボエフェクト用パーティクル
   - 判定エフェクト用パーティクル

## テスト手順

### 単体テスト

1. **ScoreUIManagerTest**
   ```csharp
   [Test]
   public void ScoreDisplay_UpdatesCorrectly()
   {
       // Arrange
       var uiManager = new GameObject().AddComponent<ScoreUIManager>();
       
       // Act
       uiManager.UpdateScore(1234567);
       
       // Assert
       Assert.AreEqual("1234567", uiManager.GetDisplayedScore());
   }
   ```

### 統合テスト

1. **実プレイシーンでの確認項目**
   - スコア更新の正確性
   - 判定カウントの精度
   - コンボ表示の動作
   - エフェクトのタイミング
   - パフォーマンス（60FPS維持）

### デバッグモード

```
キー操作:
- 1: Perfect判定
- 2: Great判定  
- 3: Good判定
- 4: Miss判定
- Space: スコア+1000
- R: リセット
- T: ストレステスト
```

## トラブルシューティング

### よくある問題と解決策

1. **テキストが表示されない**
   - TextMeshProのマテリアル設定確認
   - Canvas Sort Order確認
   - カメラのCulling Mask確認

2. **アニメーションがカクつく**
   - DOTweenの設定でRecycle Tweensを有効化
   - Update TypeをFixed Updateに変更
   - Canvas.pixelPerfectを無効化

3. **メモリリーク**
   - オブジェクトプールのサイズ調整
   - DOTweenのKill設定確認
   - イベント購読の解除確認

## まとめ

本実装計画書に従って開発を進めることで、9日間でスコアUIシステムの完全実装が可能です。各フェーズごとに動作確認を行い、問題があれば早期に対処することが重要です。特にパフォーマンス面では、継続的なプロファイリングを行い、60FPSを維持できるよう最適化を心がけてください。