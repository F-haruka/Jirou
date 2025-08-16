# InputManager 実装計画書

## 実装前提条件の確認

### 必要な依存コンポーネント

以下のコンポーネントが実装済みであることが前提となります：

| コンポーネント | ステータス | 必要な機能 |
|--------------|-----------|-----------|
| NoteController | ✅ 実装済み | Judge()メソッド、noteType判定 |
| JudgmentZone | ❌ 未実装 | GetClosestNote()メソッド、JudgeHit()メソッド |
| JudgmentType | ✅ 実装済み | Perfect/Great/Good/Miss列挙型 |
| Conductor | ❌ 未実装 | 音楽時間管理（オプション） |
| ScoreManager | ❌ 未実装 | スコア計算（オプション） |

### 実装優先順位

1. **必須実装（Phase 1）**
   - JudgmentZone.cs の作成
   - InputManager.cs の基本構造

2. **コア機能（Phase 2）**
   - Tapノーツ判定
   - 基本的な入力検出

3. **拡張機能（Phase 3）**
   - Holdノーツ処理
   - エフェクト生成

## 段階的実装計画

### Step 1: JudgmentZone 実装（前提条件）

```csharp
// JudgmentZone.cs の実装
namespace Jirou.Gameplay
{
    public class JudgmentZone : MonoBehaviour
    {
        [Header("Judgment Ranges")]
        [SerializeField] public float perfectRange = 0.5f;  // Perfect判定のZ範囲
        [SerializeField] public float greatRange = 1.0f;    // Great判定のZ範囲  
        [SerializeField] public float goodRange = 1.5f;     // Good判定のZ範囲
        
        [Header("Lane Settings")]
        [SerializeField] private int laneIndex;
        
        // Private fields
        private List<NoteController> notesInZone = new List<NoteController>();
        
        /// <summary>
        /// Z座標が0に最も近いノーツを返す
        /// </summary>
        public NoteController GetClosestNote()
        {
            NoteController closest = null;
            float minDistance = float.MaxValue;
            
            foreach (var note in notesInZone)
            {
                if (note == null || note.IsJudged) continue;
                
                float distance = Mathf.Abs(note.transform.position.z);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = note;
                }
            }
            
            return closest;
        }
        
        /// <summary>
        /// ノーツのZ座標から判定を返す
        /// </summary>
        public string JudgeHit(NoteController note)
        {
            float distance = Mathf.Abs(note.transform.position.z);
            
            if (distance <= perfectRange) return "Perfect";
            else if (distance <= greatRange) return "Great";
            else if (distance <= goodRange) return "Good";
            else return "Miss";
        }
        
        private void OnTriggerEnter(Collider other)
        {
            var note = other.GetComponent<NoteController>();
            if (note != null && note.LaneIndex == laneIndex)
            {
                notesInZone.Add(note);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            var note = other.GetComponent<NoteController>();
            if (note != null)
            {
                notesInZone.Remove(note);
            }
        }
    }
}
```

### Step 2: InputManager 基本実装

```csharp
// InputManager.cs - Phase 1 基本構造
namespace Jirou.Gameplay
{
    public class InputManager : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private KeyCode[] inputKeys = 
        { 
            KeyCode.D, 
            KeyCode.F, 
            KeyCode.J, 
            KeyCode.K 
        };
        
        [Header("Judgment Zones")]
        [SerializeField] private JudgmentZone[] judgmentZones = new JudgmentZone[4];
        
        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        
        // Private fields
        private bool[] holdStates;
        private NoteController[] heldNotes;
        
        // Constants
        private const int LANE_COUNT = 4;
        
        private void Awake()
        {
            holdStates = new bool[LANE_COUNT];
            heldNotes = new NoteController[LANE_COUNT];
            ValidateComponents();
        }
        
        private void Update()
        {
            for (int i = 0; i < LANE_COUNT; i++)
            {
                ProcessLaneInput(i);
            }
        }
        
        private void ProcessLaneInput(int laneIndex)
        {
            // Phase 1: 基本的な入力検出のみ
            if (Input.GetKeyDown(inputKeys[laneIndex]))
            {
                HandleKeyDown(laneIndex);
            }
        }
        
        private void HandleKeyDown(int laneIndex)
        {
            Debug.Log($"Lane {laneIndex} pressed (Key: {inputKeys[laneIndex]})");
            // Phase 2で判定処理を追加
        }
        
        private void ValidateComponents()
        {
            for (int i = 0; i < LANE_COUNT; i++)
            {
                if (judgmentZones[i] == null)
                {
                    Debug.LogError($"JudgmentZone[{i}] is not assigned!");
                }
            }
        }
    }
}
```

### Step 3: Tap判定の実装

```csharp
// Phase 2 追加実装
private void HandleKeyDown(int laneIndex)
{
    if (judgmentZones[laneIndex] == null) return;
    
    NoteController closestNote = judgmentZones[laneIndex].GetClosestNote();
    
    if (closestNote == null)
    {
        Debug.Log($"Lane {laneIndex}: No note to hit");
        return;
    }
    
    // JudgmentZoneで判定を計算
    string judgmentString = judgmentZones[laneIndex].JudgeHit(closestNote);
    JudgmentType judgment = ConvertToJudgmentType(judgmentString);
    
    // ノーツに判定を通知
    closestNote.Judge(judgment);
    
    // エフェクト生成
    if (hitEffectPrefab != null)
    {
        SpawnHitEffect(laneIndex, judgment);
    }
    
    Debug.Log($"Lane {laneIndex}: {judgment}");
}

private JudgmentType ConvertToJudgmentType(string judgmentString)
{
    switch (judgmentString)
    {
        case "Perfect": return JudgmentType.Perfect;
        case "Great": return JudgmentType.Great;
        case "Good": return JudgmentType.Good;
        case "Miss": 
        default: return JudgmentType.Miss;
    }
}

private void SpawnHitEffect(int laneIndex, JudgmentType judgment)
{
    // レーンのX座標を計算（-3, -1, 1, 3）
    float xPosition = -3f + (laneIndex * 2f);
    Vector3 effectPosition = new Vector3(xPosition, 0.5f, 0f);
    
    GameObject effect = Instantiate(hitEffectPrefab, effectPosition, Quaternion.identity);
    
    // 判定タイプに応じて色を変更（オプション）
    // TODO: エフェクトコンポーネントで判定タイプを表示
    
    Destroy(effect, 0.5f);
}
```

### Step 4: Hold処理の実装

```csharp
// Phase 3 Hold処理追加
private void ProcessLaneInput(int laneIndex)
{
    // KeyDown処理
    if (Input.GetKeyDown(inputKeys[laneIndex]))
    {
        HandleKeyDown(laneIndex);
    }
    // KeyHold処理
    else if (Input.GetKey(inputKeys[laneIndex]) && holdStates[laneIndex])
    {
        HandleKeyHold(laneIndex);
    }
    // KeyUp処理
    else if (Input.GetKeyUp(inputKeys[laneIndex]) && holdStates[laneIndex])
    {
        HandleKeyUp(laneIndex);
    }
}

// HandleKeyDownを修正
private void HandleKeyDown(int laneIndex)
{
    // ... 既存のTap処理 ...
    
    // Holdノーツの場合
    if (closestNote.IsHoldNote())
    {
        holdStates[laneIndex] = true;
        heldNotes[laneIndex] = closestNote;
        closestNote.StartHold();
        Debug.Log($"Lane {laneIndex}: Hold started");
    }
}

private void HandleKeyHold(int laneIndex)
{
    if (heldNotes[laneIndex] == null) 
    {
        holdStates[laneIndex] = false;
        return;
    }
    
    // Hold継続処理
    heldNotes[laneIndex].UpdateHold();
}

private void HandleKeyUp(int laneIndex)
{
    if (heldNotes[laneIndex] == null) 
    {
        holdStates[laneIndex] = false;
        return;
    }
    
    // Hold終了処理
    heldNotes[laneIndex].EndHold();
    
    // 状態をリセット
    holdStates[laneIndex] = false;
    heldNotes[laneIndex] = null;
    
    Debug.Log($"Lane {laneIndex}: Hold ended");
}
```

## テスト用シーン構成

### TestScene セットアップ手順

1. **シーン作成**
   ```
   Assets/_Jirou/Scenes/TestScenes/InputManagerTest.unity
   ```

2. **GameObjectの配置**
   ```
   InputManagerTest (Scene)
   ├── Main Camera (Position: 0, 5, -5 | Rotation: 30, 0, 0)
   ├── InputManager (GameObject)
   │   └── InputManager.cs
   ├── Lanes (Empty GameObject)
   │   ├── Lane0 (Position: -3, 0, 0)
   │   │   └── JudgmentZone.cs (laneIndex: 0)
   │   ├── Lane1 (Position: -1, 0, 0)
   │   │   └── JudgmentZone.cs (laneIndex: 1)
   │   ├── Lane2 (Position: 1, 0, 0)
   │   │   └── JudgmentZone.cs (laneIndex: 2)
   │   └── Lane3 (Position: 3, 0, 0)
   │       └── JudgmentZone.cs (laneIndex: 3)
   └── TestNoteSpawner (デバッグ用)
   ```

3. **JudgmentZone Collider設定**
   ```csharp
   // BoxCollider設定
   Size: (1, 2, 4)  // 幅1、高さ2、奥行き4
   Center: (0, 0, 0)
   IsTrigger: true
   ```

## デバッグ機能

### デバッグ表示の実装

```csharp
// InputManager追加コード
#if UNITY_EDITOR
[Header("Debug")]
[SerializeField] private bool showDebugInfo = true;
[SerializeField] private Color[] laneDebugColors = 
{
    Color.red,
    Color.yellow,
    Color.green,
    Color.blue
};

private void OnGUI()
{
    if (!showDebugInfo) return;
    
    GUIStyle style = new GUIStyle(GUI.skin.label);
    style.fontSize = 20;
    
    for (int i = 0; i < LANE_COUNT; i++)
    {
        string status = holdStates[i] ? "HOLD" : "READY";
        style.normal.textColor = laneDebugColors[i];
        
        GUI.Label(
            new Rect(100 + i * 150, 50, 140, 30),
            $"Lane {i} [{inputKeys[i]}]: {status}",
            style
        );
    }
}
#endif
```

## 実装チェックリスト

### Phase 1: 基本構造（Day 1）
- [ ] JudgmentZone.cs作成
- [ ] InputManager.cs基本構造作成
- [ ] テストシーンのセットアップ
- [ ] 入力検出の動作確認

### Phase 2: Tap判定（Day 2）
- [ ] GetClosestNote()実装
- [ ] JudgeHit()実装
- [ ] ConvertToJudgmentType()実装
- [ ] Judge()呼び出し連携
- [ ] デバッグログ確認

### Phase 3: Hold処理（Day 3）
- [ ] Hold状態管理追加
- [ ] NoteControllerのHoldメソッド連携
- [ ] Hold開始/継続/終了処理
- [ ] Hold状態のデバッグ表示

### Phase 4: エフェクト（Day 4）
- [ ] ヒットエフェクトプレハブ作成
- [ ] SpawnHitEffect()実装
- [ ] 判定別エフェクト表示
- [ ] エフェクトの最適化

### Phase 5: 統合（Day 5）
- [ ] ScoreManager連携（作成後）
- [ ] イベント通知実装
- [ ] パフォーマンステスト
- [ ] 最終調整

## 注意事項

1. **NoteControllerの拡張が必要**
   - IsHoldNote()メソッド
   - StartHold(), UpdateHold(), EndHold()メソッド
   - IsJudgedプロパティ

2. **座標系の確認**
   - Z軸: 奥（20）から手前（0）へ
   - X軸: レーン位置（-3, -1, 1, 3）
   - Y軸: 高さ（通常0.5f）

3. **タイミング精度**
   - 将来的にはAudioSettings.dspTimeを使用
   - 現段階ではZ座標での簡易判定
   - JudgmentZoneで判定範囲を調整可能（perfectRange、greatRange、goodRange）

4. **パフォーマンス**
   - Update()での4レーンループは許容範囲
   - エフェクトはオブジェクトプール化を検討

## 実装開始の準備完了確認

実装を開始する前に、以下を確認してください：

1. ✅ NoteControllerが実装済み
2. ✅ JudgmentType列挙型が定義済み
3. ❌ JudgmentZone実装（最初のステップ - GetClosestNote()とJudgeHit()メソッド含む）
4. ❌ テストシーンの準備

実装準備が整い次第、Step 1のJudgmentZone実装から開始します。