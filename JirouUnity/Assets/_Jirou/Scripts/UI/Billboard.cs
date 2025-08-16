using UnityEngine;

namespace Jirou.UI
{
    /// <summary>
    /// 3D空間でオブジェクトを常にカメラに向かせるコンポーネント
    /// 判定テキストが常にプレイヤーに見えるようにするために使用
    /// </summary>
    public class Billboard : MonoBehaviour
    {
        [Header("Billboard Settings")]
        [SerializeField] private bool lockYAxis = false;
        [SerializeField] private bool lockXAxis = false;
        [SerializeField] private bool lockZAxis = false;
        [SerializeField] private bool useMainCamera = true;
        [SerializeField] private Camera targetCamera;
        
        private Camera mainCamera;
        
        void Start()
        {
            // カメラの取得
            if (useMainCamera)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogWarning("[Billboard] Main camera not found!");
                }
            }
            else if (targetCamera != null)
            {
                mainCamera = targetCamera;
            }
            else
            {
                mainCamera = Camera.main;
                Debug.LogWarning("[Billboard] Target camera not set, using main camera");
            }
        }
        
        void LateUpdate()
        {
            if (mainCamera == null)
            {
                // カメラが見つからない場合は再取得を試みる
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }
            
            // カメラの方向を取得
            Vector3 lookDirection = transform.position - mainCamera.transform.position;
            
            // 軸のロック処理
            if (lockYAxis) lookDirection.y = 0;
            if (lockXAxis) lookDirection.x = 0;
            if (lockZAxis) lookDirection.z = 0;
            
            // 方向がゼロベクトルでない場合のみ回転を適用
            if (lookDirection != Vector3.zero)
            {
                // カメラに向かって回転
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }
        
        /// <summary>
        /// ターゲットカメラを動的に変更
        /// </summary>
        public void SetTargetCamera(Camera newCamera)
        {
            targetCamera = newCamera;
            mainCamera = newCamera;
            useMainCamera = false;
        }
        
        /// <summary>
        /// メインカメラを使用するように設定
        /// </summary>
        public void UseMainCamera()
        {
            useMainCamera = true;
            mainCamera = Camera.main;
        }
        
        void OnEnable()
        {
            // 有効化時にカメラを再取得
            if (mainCamera == null)
            {
                Start();
            }
        }
    }
}