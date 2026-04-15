using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Underdark
{
    /// <summary>
    /// 메뉴: Underdark > Connect TurretRegistry to All Scenes
    /// 모든 씬의 TurretManager에 TurretRegistry를 자동으로 연결합니다.
    /// </summary>
    public static class TurretRegistrySetup
    {
        [MenuItem("Underdark/Connect TurretRegistry to All Scenes")]
        public static void ConnectToAllScenes()
        {
            var reg = AssetDatabase.LoadAssetAtPath<TurretRegistry>("Assets/Data/TurretRegistry.asset");
            if (reg == null)
            {
                EditorUtility.DisplayDialog("오류", "Assets/Data/TurretRegistry.asset 를 찾을 수 없습니다.", "확인");
                return;
            }

            int connected = 0;
            var sceneGuids  = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
            var currentPath = EditorSceneManager.GetActiveScene().path;

            // 현재 열린 씬 처리
            var curMgr = Object.FindObjectOfType<TurretManager>();
            if (curMgr != null && curMgr.registry == null)
            {
                Undo.RecordObject(curMgr, "Connect TurretRegistry");
                curMgr.registry = reg;
                EditorUtility.SetDirty(curMgr);
                EditorSceneManager.MarkSceneDirty(curMgr.gameObject.scene);
                connected++;
                Debug.Log($"[TurretRegistrySetup] '{curMgr.gameObject.scene.name}' 연결 완료");
            }

            // 나머지 씬 순회
            foreach (var guid in sceneGuids)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(guid);
                if (scenePath == currentPath) continue;

                var scene   = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                bool dirty  = false;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var mgr = root.GetComponentInChildren<TurretManager>(true);
                    if (mgr == null || mgr.registry != null) continue;

                    Undo.RecordObject(mgr, "Connect TurretRegistry");
                    mgr.registry = reg;
                    EditorUtility.SetDirty(mgr);
                    dirty = true;
                    connected++;
                    Debug.Log($"[TurretRegistrySetup] '{scene.name}' 연결 완료");
                }

                if (dirty) EditorSceneManager.SaveScene(scene);
                EditorSceneManager.CloseScene(scene, true);
            }

            // 현재 씬 저장
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("완료",
                $"{connected}개 씬의 TurretManager에 TurretRegistry 연결 완료!\n\n" +
                "이미 연결된 씬은 건드리지 않았습니다.", "확인");
        }

        [MenuItem("Underdark/Validate TurretRegistry")]
        public static void Validate()
        {
            var reg = AssetDatabase.LoadAssetAtPath<TurretRegistry>("Assets/Data/TurretRegistry.asset");
            if (reg == null) { EditorUtility.DisplayDialog("오류", "TurretRegistry.asset 없음", "확인"); return; }

            var missing  = new System.Collections.Generic.List<string>();
            var ok       = new System.Collections.Generic.List<string>();

            foreach (var e in reg.entries)
            {
                if (e.prefab == null) missing.Add($"  ✗ {e.type}");
                else                  ok.Add($"  ✓ {e.type} → {e.prefab.name}");
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[TurretRegistry] 총 {reg.entries.Count}개\n");
            if (missing.Count > 0)
            {
                sb.AppendLine($"=== 누락된 Prefab ({missing.Count}개) ===");
                foreach (var m in missing) sb.AppendLine(m);
                sb.AppendLine();
            }
            sb.AppendLine($"=== 연결된 Prefab ({ok.Count}개) ===");
            foreach (var o in ok) sb.AppendLine(o);

            Debug.Log(sb.ToString());

            if (missing.Count == 0)
                EditorUtility.DisplayDialog("검증 완료", $"모든 {reg.entries.Count}개 터렛에 Prefab 연결됨!", "확인");
            else
                EditorUtility.DisplayDialog("누락 발견",
                    $"{missing.Count}개 터렛에 Prefab이 없습니다:\n\n" + string.Join("\n", missing) +
                    "\n\nAssets/Data/TurretRegistry asset에서 직접 연결해주세요.", "확인");
        }
    }
}
