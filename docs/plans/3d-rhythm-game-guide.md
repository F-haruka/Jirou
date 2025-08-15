# Claude Codeを活用したVibe Codingによる奥行き型リズムゲーム「Jirou」デモ開発手順書

## はじめに

本レポートは、Unity開発環境のセットアップから、AIコーディングアシスタント「Claude」を活用した「Vibe Coding」ワークフローを用いて、WindowsPCで簡単にプレイできる奥行き型リズムゲーム「Jirou」のデモを制作するための、実践的なステップバイステップの手順書である。

### プロジェクト「Jirou」の概要

「Jirou」は、ノーツが画面奥から手前の判定ラインに向かって流れてくる、視覚的に奥行きのあるリズムゲームデモである。本プロジェクトの最重要目標は、**WindowsPC環境で誰でも簡単にデモプレイができること**である。

### 実装する基本機能

- **4レーンシステム**: シンプルで分かりやすい4つのレーン
- **奥行き表現**: ノーツが奥から手前に流れる3D風の視覚効果
- **2種類のノーツ**: Tap（単押し）とHold（長押し）のみ
- **キーボード入力**: D、F、J、Kキーによる簡単操作
- **基本的な判定システム**: Perfect、Great、Good、Missの4段階

### ビジュアルの特徴

- **台形状のレーン**: 奥が狭く、手前が広がる遠近感のあるレーン表示
- **ノーツの拡大縮小**: 奥から手前に来るにつれて大きくなるノーツ
- **斜めのレーン区切り線**: 3D的な奥行き感を強調する視覚効果

### 開発アプローチ：Vibe Codingの活用

「Vibe Coding」は、開発者とAIアシスタントが協調して、アイデアを迅速にプロトタイプ化する開発手法である。厳密な設計書を事前に作成するのではなく、大まかな目標を定めてAIと対話しながら実装を進める。

---

# Part I: プロジェクトの基盤構築

## Chapter 1: Unity開発環境のセットアップ

### 1.1. Unity Hubのインストールとバージョン選定

**推奨環境:**
- Unity 2022.3 LTS（長期サポート版）
- 2D/3D両方の要素を扱うため、安定性を重視

**インストール手順:**
1. Unity公式サイトからUnity Hubをダウンロード
2. Unity Hubをインストール後、「Installs」タブから「Install Editor」を選択
3. 2022.3 LTSの最新版を選択し、以下のモジュールを含めてインストール：
   - Visual Studio（スクリプトエディタ用）
   - Windows Build Support（ビルド用）

### 1.2. Unity IDの作成

1. Unity Hub右上の人型アイコンからサインイン画面へ
2. 「Create account」を選択
3. 必要情報を入力してアカウント作成
4. メール認証を完了
5. Unity Personalライセンスを有効化

### 1.3. バージョン管理の準備（Git）

```bash
# プロジェクトフォルダの作成
mkdir JirouRhythmGame
cd JirouRhythmGame
git init
```

### 1.4. .gitignoreファイルの作成

**.gitignore:**
```
# Unity generated
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]serSettings/

# Visual Studio cache
.vs/
*.csproj
*.sln
*.suo
*.user
*.userprefs

# OS generated
.DS_Store
Thumbs.db
```

---

## Chapter 2: Jirouプロジェクトの設計

### 2.1. プロジェクトフォルダ構造

```
Assets/
├── _Jirou/
│   ├── Art/
│   │   ├── Sprites/
│   │   │   ├── Notes/
│   │   │   ├── Lanes/
│   │   │   └── UI/
│   │   ├── Materials/
│   │   └── Shaders/
│   ├── Audio/
│   │   ├── Music/
│   │   └── SFX/
│   ├── Data/
│   │   └── Charts/
│   ├── Prefabs/
│   │   ├── Notes/
│   │   ├── Effects/
│   │   └── Stage/
│   ├── Scenes/
│   └── Scripts/
│       ├── Core/
│       ├── Gameplay/
│       ├── Visual/
│       └── UI/
└── Settings/
```

### 2.2. Unityプロジェクトの作成

**Unityエディタタスク:**

1. Unity Hubで「New project」をクリック
2. テンプレート「**3D (URP)**」を選択
   - 3D要素と2D要素を組み合わせるため
   - URPによる高品質な視覚効果
3. Project Name: 「JirouRhythmGame」
4. 作成したGitフォルダを保存先に指定
5. 「Create project」をクリック

### 2.3. 初期シーンとカメラ設定

**Unityエディタタスク:**

1. **シーンの作成**：
   - Assets/_Jirou/Scenes/に新規シーンを作成
   - 「GameScene」と命名して保存

2. **カメラ設定（奥行き表現用）**：
   - Main Cameraを選択
   - Position: (0, 5, -5)
   - Rotation: (30, 0, 0) - 下向きの角度で奥行きを強調
   - Field of View: 60
   - Projection: Perspective（透視投影で奥行き表現）

3. **背景設定**：
   - CameraのBackground: 濃い紫や黒
   - Clear Flags: Solid Color

---

# Part II: Vibe Codingによる奥行き型ゲームループの実装

## Chapter 3: タイミング管理とZ軸移動システム

### 3.1. AIプロンプティング：奥行き対応Conductorスクリプト

**Vibe Codingタスク:**

#### プロンプト 1: タイミング管理の基本

```
Unity用のC#スクリプト「Conductor」を作成してください。
リズムゲーム「Jirou」の奥行き型ノーツ移動を管理するシングルトンクラスです。

必要な機能：
1. シングルトンパターンの実装
2. 曲のBPMとタイミング管理
3. AudioSettings.dspTimeを使用した高精度タイミング
4. Z軸でのノーツ位置計算

publicフィールド：
- songBpm: float（BPM値）
- firstBeatOffset: float（最初のビートまでのオフセット秒数）
- songSource: AudioSource（音楽再生用）
- noteSpeed: float = 10.0f（ノーツの移動速度）
- spawnZ: float = 20.0f（ノーツ生成位置のZ座標）
- hitZ: float = 0.0f（判定ラインのZ座標）

privateフィールド：
- dspSongTime: double（曲開始時のdspTime）
- instance: static Conductor（シングルトンインスタンス）

publicプロパティ（getterのみ）：
- songPositionInSeconds: 現在の曲位置（秒）
- songPositionInBeats: 現在の曲位置（ビート）

publicメソッド：
- GetNoteZPosition(float noteBeat): 
  指定ビートのノーツのZ座標を計算
  float beatsPassed = songPositionInBeats - noteBeat;
  return spawnZ + (beatsPassed * noteSpeed);
  
- ShouldSpawnNote(float noteBeat, float beatsInAdvance):
  ノーツを生成すべきタイミングかチェック
```

### 3.2. 奥行き表現のための座標系

Jirouでは、以下の座標系を使用：
- **Z軸**: 奥（正の値）から手前（0）への移動
- **X軸**: 左右のレーン位置
- **Y軸**: 高さ（基本的に固定）

ノーツは画面奥（Z=20）から判定ライン（Z=0）に向かって移動する。

---

## Chapter 4: 奥行き対応の譜面データ構造

### 4.1. AIプロンプティング：譜面データ構造

**Vibe Codingタスク:**

#### プロンプト 1: ノーツデータクラス

```
Unity用のC#スクリプトを作成してください。
「Jirou」の奥行き型ゲーム用ノーツデータ構造を定義します。

public enum NoteType {
    Tap,    // 単押しノーツ
    Hold    // 長押しノーツ
}

[System.Serializable]
public class NoteData {
    public NoteType noteType;
    public int laneIndex;        // レーン番号（0-3）
    public float timeToHit;      // ヒットタイミング（ビート単位）
    public float holdDuration;   // Holdノーツの長さ（ビート単位）
    
    // 視覚的な調整用
    public float visualScale = 1.0f;  // ノーツの大きさ倍率
}
```

#### プロンプト 2: 譜面データScriptableObject

```
ScriptableObjectを継承するC#スクリプト「ChartData」を作成してください。

必要なフィールド：
- songClip: AudioClip（楽曲ファイル）
- bpm: float（BPM）
- songName: string（曲名）
- artist: string（アーティスト名）
- previewTime: float（プレビュー開始時間）
- notes: List<NoteData>（ノーツリスト）

[CreateAssetMenu(fileName = "NewChart", menuName = "Jirou/Chart Data", order = 1)]

メソッド：
- SortNotesByTime(): ノーツをtimeToHitでソート
- GetNotesInTimeRange(float startBeat, float endBeat): 指定範囲のノーツを取得
```

### 4.2. Unityエディタタスク：テスト譜面の作成

1. Assets/_Jirou/Data/Charts/で右クリック
2. Create > Jirou > Chart Dataを選択
3. 「TestChart」と命名
4. Inspectorで設定：
   - Song Clip: テスト用音楽ファイル
   - BPM: 120
   - テストノーツを数個追加

---

## Chapter 5: 奥行き型レーンとノーツの実装

### 5.1. Unityエディタタスク：台形レーンの作成

**レーンビジュアルの作成:**

1. **レーンコンテナ**：
   - 空のGameObjectを作成し、「LaneContainer」と命名
   - Position: (0, 0, 0)

2. **個別レーンの作成**：
   各レーンに対して：
   - 3D Object > Quadを作成（または Line Rendererを使用）
   - 台形状に変形（奥が狭く、手前が広い）
   - 半透明のマテリアルを適用

3. **レーン配置**：
   ```
   Lane 0: X = -3
   Lane 1: X = -1
   Lane 2: X = 1
   Lane 3: X = 3
   ```

### 5.2. AIプロンプティング：レーンビジュアライザー

**Vibe Codingタスク:**

```
Unity用のC#スクリプト「LaneVisualizer」を作成してください。
奥行きのあるレーンを表示します。

using UnityEngine;

publicフィールド：
- laneCount: int = 4
- laneWidth: float = 2.0f
- nearWidth: float = 2.0f（手前の幅）
- farWidth: float = 0.5f（奥の幅）
- laneLength: float = 20.0f
- laneMaterial: Material

privateフィールド：
- laneRenderers: LineRenderer[]

Startメソッド：
1. 各レーンに対してLineRendererを作成
2. 台形状のポイントを設定：
   - 奥の点: (x * farWidth, 0, laneLength)
   - 手前の点: (x * nearWidth, 0, 0)
3. マテリアルと幅を設定

視覚的な特徴：
- 斜めの線で奥行き感を表現
- 半透明で他の要素を邪魔しない
```

### 5.3. Unityエディタタスク：ノーツプレハブの作成

**Tapノーツプレハブ:**
1. 3D Object > Cubeを作成
2. 「TapNote」と命名
3. Scale: (1.5, 0.2, 0.3)（横長の板状）
4. マテリアル作成：
   - 赤色のEmissiveマテリアル
   - 少し発光させる
5. Box Collider（Is Trigger: ON）
6. Rigidbody（Is Kinematic: ON）
7. プレハブ化

**Holdノーツプレハブ:**
1. TapNoteを複製
2. 「HoldNote」に改名
3. 色を黄色に変更
4. 子オブジェクトとして「Trail」を追加（長押し部分の視覚表現）

---

## Chapter 6: 奥行き型ノーツ生成と移動

### 6.1. AIプロンプティング：3D空間でのノーツスポナー

**Vibe Codingタスク:**

#### プロンプト 1: NoteSpawner

```
Unity用のC#スクリプト「NoteSpawner」を作成してください。
「Jirou」の奥行き型ノーツ生成を管理します。

publicフィールド：
- chartData: ChartData（譜面データ）
- tapNotePrefab: GameObject（Tapノーツプレハブ）
- holdNotePrefab: GameObject（Holdノーツプレハブ）
- laneXPositions: float[] = {-3, -1, 1, 3}（各レーンのX座標）
- noteY: float = 0.5f（ノーツのY座標）
- beatsShownInAdvance: float = 3.0f（先読みビート数）

privateフィールド：
- nextNoteIndex: int
- activeNotes: List<GameObject>

Startメソッド：
- Conductor.instance.PlaySong()で曲を開始

Updateメソッド：
1. まだ生成していないノーツがあるかチェック
2. Conductor.instance.ShouldSpawnNote()で生成タイミング判定
3. 生成する場合：
   - 適切なプレハブをInstantiate
   - 位置を設定: 
     Vector3 spawnPos = new Vector3(
       laneXPositions[noteData.laneIndex], 
       noteY, 
       Conductor.instance.spawnZ
     );
   - NoteControllerコンポーネントにデータを設定
```

#### プロンプト 2: NoteController（奥行き移動版）

```
Unity用のC#スクリプト「NoteController」を作成してください。
奥から手前に移動するノーツを制御します。

publicフィールド：
- noteData: NoteData
- moveSpeed: float = 10.0f

privateフィールド：
- targetBeat: float
- hasBeenHit: bool
- initialScale: Vector3
- renderer: MeshRenderer

Startメソッド：
- targetBeatを設定
- initialScaleを記録
- Holdノーツの場合、Trailの長さを設定

Updateメソッド：
1. Z座標の更新：
   float newZ = Conductor.instance.GetNoteZPosition(targetBeat);
   transform.position = new Vector3(
     transform.position.x, 
     transform.position.y, 
     newZ
   );
   
2. 距離に応じたスケール変更（奥で小さく、手前で大きく）：
   float distanceRatio = newZ / Conductor.instance.spawnZ;
   float scale = Mathf.Lerp(1.5f, 0.5f, distanceRatio);
   transform.localScale = initialScale * scale;
   
3. 判定ラインを通過（Z < -2）したら：
   - ミス処理
   - Destroy(gameObject)

publicメソッド：
- OnHit(): ヒット時の処理
- CheckHitTiming(): タイミング判定を返す
```

---

## Chapter 7: 入力システムと奥行き判定

### 7.1. Unityエディタタスク：判定エリアの設定

1. **判定ライン視覚表示**：
   - 3D Object > Quadを作成
   - 「JudgmentLine」と命名
   - Position: (0, 0, 0)
   - Rotation: (90, 0, 0) - 水平に配置
   - Scale: (10, 0.1, 1)
   - 半透明の白いマテリアル

2. **判定ゾーン**：
   各レーンに判定用コライダーを配置：
   - 空のGameObject「JudgmentZone_0〜3」
   - 各レーンのX位置、Z=0に配置
   - Box Collider追加（Is Trigger: ON）
   - Size: (1.5, 1, 2) - Z方向に判定の余裕を持たせる

### 7.2. AIプロンプティング：奥行き対応入力システム

**Vibe Codingタスク:**

#### プロンプト 1: InputManager

```
Unity用のC#スクリプト「InputManager」を作成してください。
「Jirou」の4レーン入力を管理します。

publicフィールド：
- inputKeys: KeyCode[] = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K }
- judgmentZones: JudgmentZone[]（4つの判定ゾーン）
- hitEffectPrefab: GameObject

privateフィールド：
- holdStates: bool[]（各レーンの長押し状態）
- heldNotes: NoteController[]（長押し中のノーツ）

Updateメソッド：
各レーンについて：
1. Input.GetKeyDown(inputKeys[i]):
   - judgmentZones[i].GetClosestNote()で最も近いノーツを取得
   - タイミング判定
   - ヒットエフェクト生成
   - Holdノーツなら保持開始

2. Input.GetKey(inputKeys[i]):
   - Holdノーツの継続チェック

3. Input.GetKeyUp(inputKeys[i]):
   - Holdノーツの終了処理
```

#### プロンプト 2: JudgmentZone（Z軸判定版）

```
Unity用のC#スクリプト「JudgmentZone」を作成してください。
Z軸での判定を行います。

publicフィールド：
- perfectRange: float = 0.5f（Perfect判定のZ範囲）
- greatRange: float = 1.0f
- goodRange: float = 1.5f

privateフィールド：
- notesInZone: List<NoteController>

OnTriggerEnter/Exit:
- ゾーン内のノーツを管理

publicメソッド：
- NoteController GetClosestNote():
  Z座標が0に最も近いノーツを返す
  
- string JudgeHit(NoteController note):
  ノーツのZ座標から判定を返す：
  float distance = Mathf.Abs(note.transform.position.z);
  if (distance <= perfectRange) return "Perfect";
  else if (distance <= greatRange) return "Great";
  else if (distance <= goodRange) return "Good";
  else return "Miss";
```

---

## Chapter 8: 視覚効果とフィードバック

### 8.1. Unityエディタタスク：奥行き強調エフェクト

**パーティクルエフェクトの作成:**

1. **判定エフェクト**：
   - 各判定用のパーティクルシステム作成
   - Z=0の判定ライン上で発生
   - 3D空間で広がるエフェクト

2. **ライティング設定**：
   - Directional Light追加
   - 角度調整で奥行き感を強調
   - 影の設定でノーツの立体感を演出

### 8.2. AIプロンプティング：ビジュアルエフェクトマネージャー

**Vibe Codingタスク:**

```
Unity用のC#スクリプト「VisualEffectManager」を作成してください。
奥行き型ゲームの視覚効果を管理します。

[System.Serializable]
public class JudgmentEffect {
    public string judgmentName;
    public GameObject effectPrefab;
    public Color color;
}

publicフィールド：
- judgmentEffects: JudgmentEffect[]
- comboMilestoneEffects: GameObject[]（10、50、100コンボ用）
- cameraShakeIntensity: float = 0.1f

publicメソッド：
- ShowJudgmentEffect(string judgment, Vector3 position):
  判定位置（Z=0）にエフェクトを生成
  
- ShowLaneHighlight(int laneIndex):
  ヒット時にレーンを一瞬光らせる
  
- TriggerCameraShake(float intensity):
  カメラを揺らす演出（ミス時など）

privateメソッド：
- IEnumerator ShakeCamera():
  カメラ揺れのコルーチン
```

---

## Chapter 9: UIと奥行き表現の統合

### 9.1. Unityエディタタスク：ゲームUI配置

**Canvas設定:**
1. UI > Canvasを作成
2. Canvas設定：
   - Render Mode: Screen Space - Overlay
   - Canvas Scaler: Scale With Screen Size
   - Reference Resolution: 1920 x 1080

**UI要素:**
1. **スコア表示**（右上）
2. **コンボ表示**（中央上）
3. **判定表示**（判定ライン付近）
4. **曲情報**（左上）

### 9.2. AIプロンプティング：UIマネージャー

**Vibe Codingタスク:**

```
Unity用のC#スクリプト「UIManager」を作成してください。

using TMPro;
using UnityEngine;

publicフィールド：
- scoreText: TextMeshProUGUI
- comboText: TextMeshProUGUI
- judgmentText: TextMeshProUGUI
- songInfoText: TextMeshProUGUI

publicメソッド：
- UpdateScore(int score):
  スコア表示更新
  
- UpdateCombo(int combo):
  コンボ表示更新（0の時は非表示）
  
- ShowJudgment(string judgment, Color color):
  判定テキストを表示（フェードアウトアニメーション付き）
  
- SetSongInfo(string songName, string artist):
  曲情報を表示

privateメソッド：
- IEnumerator FadeOutText(TextMeshProUGUI text):
  テキストのフェードアウト
```

---

## Chapter 10: メニューとビルド

### 10.1. シンプルなメニューシーン

**Unityエディタタスク:**

1. 新規シーン「MenuScene」作成
2. UI要素：
   - タイトル「Jirou」
   - Play ボタン
   - Quit ボタン
   - 操作説明テキスト

### 10.2. AIプロンプティング：メニューマネージャー

**Vibe Codingタスク:**

```
Unity用のC#スクリプト「MenuManager」を作成してください。

using UnityEngine.SceneManagement;

publicフィールド：
- playButton: Button
- quitButton: Button
- selectedChart: ChartData（デフォルト譜面）

Startメソッド：
- ボタンイベントの登録

publicメソッド：
- PlayGame():
  GameManagerに譜面データを渡してGameSceneをロード
  
- QuitGame():
  Application.Quit()
  
- ShowInstructions():
  操作説明の表示/非表示切り替え
```

### 10.3. WindowsPC向けビルド設定

**Project Settings:**

1. **Player設定**：
   - Company Name: 開発者名
   - Product Name: Jirou
   - Resolution:
     - Fullscreen Mode: Windowed
     - Default Width: 1280
     - Default Height: 720

2. **Graphics設定**：
   - 低スペックPCでも動作するよう最適化

3. **Quality設定**：
   - デフォルトをMediumに設定

**ビルド手順:**
1. File > Build Settings
2. Scenes In Build:
   - MenuScene（index 0）
   - GameScene（index 1）
3. Platform: PC, Mac & Linux Standalone
4. Target Platform: Windows
5. Build実行

### 10.4. 配布用パッケージ

**READMEファイル:**
```markdown
# Jirou - 奥行き型リズムゲームデモ

## 操作方法
- D, F, J, K キー: 各レーンのノーツを叩く
- ESC: メニューに戻る

## システム要件
- Windows 10以降
- DirectX 11対応
- 推奨: 4GB RAM以上

## 起動方法
Jirou.exeをダブルクリックして起動

## トラブルシューティング
- 音が出ない場合: Windowsの音量設定を確認
- 動作が重い場合: 他のアプリケーションを終了
```

---

## 結論

本手順書では、Unity環境で奥行きのある視覚表現を持つリズムゲームデモ「Jirou」を、AI支援開発手法「Vibe Coding」を活用して効率的に開発する方法を解説した。

### 実装した主要機能

1. **奥行き表現**: ノーツが奥から手前に流れる3D風の視覚効果
2. **台形レーン**: 遠近感のあるレーン表示
3. **動的スケーリング**: 距離に応じたノーツサイズの変化
4. **Z軸判定**: 奥行き方向での正確なタイミング判定
5. **シンプルな操作**: 4キーのみで楽しめる設計

### 技術的な達成事項

- **3D座標系の活用**: Z軸を使った奥行き表現
- **透視投影**: カメラ設定による自然な遠近感
- **パフォーマンス最適化**: WindowsPCで快適に動作

### Vibe Codingの効果

- AIとの対話により、複雑な3D計算も効率的に実装
- 視覚効果の実装を段階的に進められる
- プロトタイプから完成まで迅速に開発

### 今後の拡張可能性

- より複雑なレーン配置（カーブや分岐）
- 背景の3Dオブジェクト追加
- カメラアングルの変化
- ノーツの回転や特殊な動き

本手順書により、奥行きのある魅力的なリズムゲームデモを、誰でも簡単に開発できることを示した。「Jirou」は、シンプルさと視覚的インパクトを両立させた、学習に最適なプロジェクトである。