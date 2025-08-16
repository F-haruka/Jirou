using UnityEngine;
using UnityEditor;
using Jirou.Visual;

namespace Jirou.Editor
{
    /// <summary>
    /// 判定ラインプレハブを作成するエディタ拡張
    /// </summary>
    public static class CreateJudgmentLinePrefab
    {
        [MenuItem("Jirou/Create Prefabs/Judgment Line")]
        public static void CreatePrefab()
        {
            // 判定ラインオブジェクトを作成
            GameObject judgmentLineObject = new GameObject("JudgmentLine");
            
            // JudgmentLineコンポーネントを追加
            JudgmentLine judgmentLine = judgmentLineObject.AddComponent<JudgmentLine>();
            
            // Transformを初期化
            judgmentLineObject.transform.position = Vector3.zero;
            judgmentLineObject.transform.rotation = Quaternion.identity;
            judgmentLineObject.transform.localScale = Vector3.one;
            
            // プレハブとして保存
            string prefabPath = "Assets/_Jirou/Prefabs/Stage/JudgmentLine.prefab";
            
            // 既存のプレハブがある場合は確認
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                if (!EditorUtility.DisplayDialog("確認", 
                    "JudgmentLine.prefabは既に存在します。上書きしますか？", 
                    "はい", "いいえ"))
                {
                    // オブジェクトを削除してキャンセル
                    GameObject.DestroyImmediate(judgmentLineObject);
                    return;
                }
            }
            
            // プレハブを作成または更新
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(judgmentLineObject, prefabPath);
            
            // 元のオブジェクトを削除
            GameObject.DestroyImmediate(judgmentLineObject);
            
            // プレハブを選択
            Selection.activeObject = prefab;
            
            // ログ出力
            Debug.Log($"判定ラインプレハブを作成しました: {prefabPath}");
        }
    }
}