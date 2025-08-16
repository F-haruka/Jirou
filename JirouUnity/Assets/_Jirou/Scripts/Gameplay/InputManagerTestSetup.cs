using UnityEngine;
using Jirou.Core;

namespace Jirou.Gameplay
{
    /// <summary>
    /// InputManagerのテストシーンセットアップ用スクリプト
    /// テスト用のノーツを定期的に生成し、InputManagerの動作を確認する
    /// </summary>
    public class InputManagerTestSetup : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool autoSpawnNotes = true;
        [SerializeField] private float spawnInterval = 2.0f;  // ノーツ生成間隔（秒）
        [SerializeField] private int spawnLanePattern = 0;  // 0: Sequential, 1: Random, 2: All lanes
        
        [Header("Note Prefabs")]
        [SerializeField] private GameObject tapNotePrefab;
        [SerializeField] private GameObject holdNotePrefab;
        
        [Header("References")]
        [SerializeField] private InputManager inputManager;
        [SerializeField] private JudgmentZone[] judgmentZones = new JudgmentZone[4];
        
        [Header("Visual Settings")]
        [SerializeField] private Material[] laneMaterials = new Material[4];
        [SerializeField] private Color[] laneColors = new Color[]
        {
            Color.red,
            Color.yellow,
            Color.green,
            Color.blue
        };
        
        // プライベートフィールド
        private float nextSpawnTime;
        private int currentLane = 0;
        private bool isSetupComplete = false;
        
        void Start()
        {
            SetupTestScene();
            nextSpawnTime = Time.time + 1.0f;  // 1秒後から開始
        }
        
        void Update()
        {
            if (!isSetupComplete) return;
            
            // 自動ノーツ生成
            if (autoSpawnNotes && Time.time >= nextSpawnTime)
            {
                SpawnTestNotes();
                nextSpawnTime = Time.time + spawnInterval;
            }
            
            // テスト用キー入力（数字キー1-4でノーツ生成）
            for (int i = 0; i < 4; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SpawnNoteAtLane(i, false);  // Tapノーツ
                }
                if (Input.GetKeyDown(KeyCode.F1 + i))
                {
                    SpawnNoteAtLane(i, true);   // Holdノーツ
                }
            }
            
            // リセット（Rキー）
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetTestScene();
            }
        }
        
        /// <summary>
        /// テストシーンのセットアップ
        /// </summary>
        private void SetupTestScene()
        {
            Debug.Log("[TestSetup] Starting test scene setup...");
            
            // InputManagerの検索・作成
            if (inputManager == null)
            {
                inputManager = FindObjectOfType<InputManager>();
                if (inputManager == null)
                {
                    GameObject inputManagerObj = new GameObject("InputManager");
                    inputManager = inputManagerObj.AddComponent<InputManager>();
                    Debug.Log("[TestSetup] Created InputManager");
                }
            }
            
            // レーンと判定ゾーンの作成
            GameObject lanesParent = new GameObject("Lanes");
            for (int i = 0; i < 4; i++)
            {
                CreateLaneAndJudgmentZone(i, lanesParent.transform);
            }
            
            // InputManagerに判定ゾーンを設定
            for (int i = 0; i < 4; i++)
            {
                if (judgmentZones[i] != null)
                {
                    inputManager.SetJudgmentZone(i, judgmentZones[i]);
                }
            }
            
            // カメラの設定
            SetupCamera();
            
            // ライティングの設定
            SetupLighting();
            
            // デフォルトプレハブの作成（なければ）
            CreateDefaultPrefabs();
            
            isSetupComplete = true;
            Debug.Log("[TestSetup] Test scene setup completed!");
        }
        
        /// <summary>
        /// レーンと判定ゾーンの作成
        /// </summary>
        private void CreateLaneAndJudgmentZone(int laneIndex, Transform parent)
        {
            // レーンのX座標を計算（-3, -1, 1, 3）
            float xPosition = -3f + (laneIndex * 2f);
            
            // レーンオブジェクトの作成
            GameObject laneObj = GameObject.CreatePrimitive(PrimitiveType.Plane);
            laneObj.name = $"Lane{laneIndex}";
            laneObj.transform.parent = parent;
            laneObj.transform.position = new Vector3(xPosition, 0, 10);
            laneObj.transform.rotation = Quaternion.Euler(90, 0, 0);
            laneObj.transform.localScale = new Vector3(0.18f, 1f, 4f);  // 幅1.8、長さ40
            
            // レーンの色設定
            Renderer laneRenderer = laneObj.GetComponent<Renderer>();
            if (laneMaterials[laneIndex] != null)
            {
                laneRenderer.material = laneMaterials[laneIndex];
            }
            else
            {
                laneRenderer.material.color = laneColors[laneIndex] * 0.3f;  // 暗めの色
            }
            
            // 判定ゾーンの作成
            GameObject zoneObj = new GameObject($"JudgmentZone{laneIndex}");
            zoneObj.transform.parent = laneObj.transform;
            zoneObj.transform.localPosition = new Vector3(0, 0, -1f);  // 判定ライン位置（Z=0付近）
            zoneObj.transform.localRotation = Quaternion.identity;
            zoneObj.transform.localScale = Vector3.one;
            
            // JudgmentZoneコンポーネントを追加
            JudgmentZone zone = zoneObj.AddComponent<JudgmentZone>();
            judgmentZones[laneIndex] = zone;
            
            // BoxColliderを追加（JudgmentZoneのStartで設定されるが念のため）
            BoxCollider collider = zoneObj.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(1f, 2f, 4f);
            
            // 判定ラインの視覚表示
            GameObject judgmentLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            judgmentLine.name = "JudgmentLine";
            judgmentLine.transform.parent = laneObj.transform;
            judgmentLine.transform.localPosition = new Vector3(0, 0.1f, -1f);
            judgmentLine.transform.localScale = new Vector3(1f, 0.02f, 0.05f);
            
            Renderer lineRenderer = judgmentLine.GetComponent<Renderer>();
            lineRenderer.material.color = Color.white;
            
            Debug.Log($"[TestSetup] Created lane {laneIndex} at X={xPosition}");
        }
        
        /// <summary>
        /// カメラのセットアップ
        /// </summary>
        private void SetupCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }
            
            // 奥行き型リズムゲーム用のカメラ設定
            mainCamera.transform.position = new Vector3(0, 5, -5);
            mainCamera.transform.rotation = Quaternion.Euler(30, 0, 0);
            mainCamera.fieldOfView = 60;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = 50f;
            
            Debug.Log("[TestSetup] Camera configured");
        }
        
        /// <summary>
        /// ライティングのセットアップ
        /// </summary>
        private void SetupLighting()
        {
            // 既存のライトを検索
            Light[] lights = FindObjectsOfType<Light>();
            if (lights.Length == 0)
            {
                // ディレクショナルライトの作成
                GameObject lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.0f;
                light.color = Color.white;
                lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);
                
                Debug.Log("[TestSetup] Lighting configured");
            }
        }
        
        /// <summary>
        /// デフォルトプレハブの作成
        /// </summary>
        private void CreateDefaultPrefabs()
        {
            // Tapノーツプレハブがない場合は作成
            if (tapNotePrefab == null)
            {
                GameObject tapNote = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tapNote.name = "TapNote_Default";
                tapNote.transform.localScale = new Vector3(0.8f, 0.3f, 0.3f);
                
                // NoteControllerを追加
                NoteController noteController = tapNote.AddComponent<NoteController>();
                noteController.noteType = NoteType.Tap;
                
                // マテリアル設定
                Renderer renderer = tapNote.GetComponent<Renderer>();
                renderer.material.color = Color.cyan;
                
                // Colliderの設定
                BoxCollider collider = tapNote.GetComponent<BoxCollider>();
                collider.isTrigger = true;
                
                tapNotePrefab = tapNote;
                tapNote.SetActive(false);
                
                Debug.Log("[TestSetup] Created default Tap note prefab");
            }
            
            // Holdノーツプレハブがない場合は作成
            if (holdNotePrefab == null)
            {
                GameObject holdNote = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                holdNote.name = "HoldNote_Default";
                holdNote.transform.localScale = new Vector3(0.8f, 0.3f, 1.0f);
                
                // NoteControllerを追加
                NoteController noteController = holdNote.AddComponent<NoteController>();
                noteController.noteType = NoteType.Hold;
                noteController.holdDuration = 2.0f;
                
                // マテリアル設定
                Renderer renderer = holdNote.GetComponent<Renderer>();
                renderer.material.color = Color.magenta;
                
                // Colliderの設定
                CapsuleCollider collider = holdNote.GetComponent<CapsuleCollider>();
                collider.isTrigger = true;
                
                holdNotePrefab = holdNote;
                holdNote.SetActive(false);
                
                Debug.Log("[TestSetup] Created default Hold note prefab");
            }
        }
        
        /// <summary>
        /// テスト用ノーツの生成
        /// </summary>
        private void SpawnTestNotes()
        {
            switch (spawnLanePattern)
            {
                case 0:  // Sequential
                    SpawnNoteAtLane(currentLane, Random.value > 0.7f);
                    currentLane = (currentLane + 1) % 4;
                    break;
                    
                case 1:  // Random
                    int randomLane = Random.Range(0, 4);
                    SpawnNoteAtLane(randomLane, Random.value > 0.7f);
                    break;
                    
                case 2:  // All lanes
                    for (int i = 0; i < 4; i++)
                    {
                        if (Random.value > 0.5f)  // 50%の確率で生成
                        {
                            SpawnNoteAtLane(i, Random.value > 0.8f);
                        }
                    }
                    break;
            }
        }
        
        /// <summary>
        /// 指定レーンにノーツを生成
        /// </summary>
        private void SpawnNoteAtLane(int laneIndex, bool isHold)
        {
            GameObject prefab = isHold ? holdNotePrefab : tapNotePrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"[TestSetup] {(isHold ? "Hold" : "Tap")} note prefab is not assigned!");
                return;
            }
            
            // レーンのX座標を計算
            float xPosition = -3f + (laneIndex * 2f);
            Vector3 spawnPosition = new Vector3(xPosition, 0.5f, 20f);  // Z=20から開始
            
            // ノーツを生成
            GameObject noteObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            noteObj.SetActive(true);
            noteObj.name = $"{(isHold ? "Hold" : "Tap")}Note_Lane{laneIndex}_{Time.time:F2}";
            
            // NoteControllerの設定
            NoteController noteController = noteObj.GetComponent<NoteController>();
            if (noteController != null)
            {
                noteController.laneIndex = laneIndex;
                noteController.noteType = isHold ? NoteType.Hold : NoteType.Tap;
                noteController.moveSpeed = 5.0f;  // 移動速度
                
                if (isHold)
                {
                    noteController.holdDuration = Random.Range(1.5f, 3.0f);
                }
                
                // 色をレーンに合わせる
                Renderer renderer = noteObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = laneColors[laneIndex];
                }
            }
            
            // 簡易的な移動スクリプトを追加（Conductorがない場合のフォールバック）
            TestNoteMovement movement = noteObj.AddComponent<TestNoteMovement>();
            movement.moveSpeed = 5.0f;
            movement.destroyZ = -5.0f;
            
            Debug.Log($"[TestSetup] Spawned {(isHold ? "Hold" : "Tap")} note at lane {laneIndex}");
        }
        
        /// <summary>
        /// テストシーンのリセット
        /// </summary>
        private void ResetTestScene()
        {
            // 全ノーツを削除
            NoteController[] allNotes = FindObjectsOfType<NoteController>();
            foreach (var note in allNotes)
            {
                if (note.gameObject != tapNotePrefab && note.gameObject != holdNotePrefab)
                {
                    Destroy(note.gameObject);
                }
            }
            
            // InputManagerをリセット
            if (inputManager != null)
            {
                inputManager.ResetInputManager();
            }
            
            Debug.Log("[TestSetup] Test scene reset");
        }
        
        void OnGUI()
        {
            // 操作説明の表示
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.white;
            
            GUI.Box(new Rect(10, 120, 300, 200), "");
            
            GUI.Label(new Rect(20, 125, 280, 30), "=== Test Controls ===", style);
            GUI.Label(new Rect(20, 150, 280, 30), "D,F,J,K: Hit notes", style);
            GUI.Label(new Rect(20, 175, 280, 30), "1-4: Spawn Tap notes", style);
            GUI.Label(new Rect(20, 200, 280, 30), "F1-F4: Spawn Hold notes", style);
            GUI.Label(new Rect(20, 225, 280, 30), "R: Reset scene", style);
            GUI.Label(new Rect(20, 250, 280, 30), $"Auto Spawn: {(autoSpawnNotes ? "ON" : "OFF")}", style);
            GUI.Label(new Rect(20, 275, 280, 30), $"Pattern: {GetPatternName(spawnLanePattern)}", style);
            
            // トグルボタン
            if (GUI.Button(new Rect(20, 300, 100, 25), "Toggle Auto"))
            {
                autoSpawnNotes = !autoSpawnNotes;
            }
            
            if (GUI.Button(new Rect(130, 300, 100, 25), "Change Pattern"))
            {
                spawnLanePattern = (spawnLanePattern + 1) % 3;
            }
        }
        
        private string GetPatternName(int pattern)
        {
            switch (pattern)
            {
                case 0: return "Sequential";
                case 1: return "Random";
                case 2: return "All Lanes";
                default: return "Unknown";
            }
        }
    }
    
    /// <summary>
    /// テスト用の簡易ノーツ移動スクリプト
    /// </summary>
    public class TestNoteMovement : MonoBehaviour
    {
        public float moveSpeed = 5.0f;
        public float destroyZ = -5.0f;
        
        void Update()
        {
            // Z軸方向に移動（奥から手前へ）
            transform.position += Vector3.back * moveSpeed * Time.deltaTime;
            
            // 判定ラインを大きく通過したら削除
            if (transform.position.z < destroyZ)
            {
                Destroy(gameObject);
            }
        }
    }
}