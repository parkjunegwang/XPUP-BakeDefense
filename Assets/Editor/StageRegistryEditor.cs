using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Underdark
{
    /// <summary>
    /// StageRegistry 관련 에디터 툴.
    /// Underdark/Stage Registry > Scan Stages 로 자동 스캔.
    /// </summary>
    public static class StageRegistryEditor
    {
        private const string ASSET_PATH     = "Assets/Data/StageRegistry.asset";
        private const string RESOURCE_PATH  = "Assets/Resources/StageRegistry.asset";
        private const string STAGES_FOLDER  = "Assets/Data/Stages";

        // ── 자동 스캔 & 저장 ─────────────────────────────────────────

        [MenuItem("Underdark/Stage Registry/Scan Stages (Auto)")]
        public static StageRegistry ScanAndSave()
        {
            var registry = GetOrCreateRegistry();

            // Assets/Data/Stages/ 하위 StageData 전부 수집
            var guids = AssetDatabase.FindAssets("t:StageData", new[] { STAGES_FOLDER });
            var list  = new List<StageData>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var sd   = AssetDatabase.LoadAssetAtPath<StageData>(path);
                if (sd != null) list.Add(sd);
            }

            // stageName 기준 정렬
            list.Sort((a, b) => string.Compare(a.stageName, b.stageName,
                System.StringComparison.Ordinal));

            registry.stages = list;
            EditorUtility.SetDirty(registry);

            // Resources 복사 (런타임 로드용)
            SyncToResources(registry);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[StageRegistry] {list.Count}개 스테이지 스캔 완료: {string.Join(", ", list.ConvertAll(s => s.stageName))}");
            return registry;
        }

        [MenuItem("Underdark/Stage Registry/Validate")]
        public static void Validate()
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageRegistry>(ASSET_PATH);
            if (registry == null)
            {
                Debug.LogWarning("[StageRegistry] StageRegistry.asset 없음. 먼저 Scan을 실행하세요.");
                return;
            }

            int nullCount = 0;
            for (int i = 0; i < registry.stages.Count; i++)
            {
                if (registry.stages[i] == null)
                {
                    Debug.LogWarning($"[StageRegistry] index {i} 가 null입니다.");
                    nullCount++;
                }
            }

            if (nullCount == 0)
                Debug.Log($"[StageRegistry] OK - {registry.stages.Count}개 스테이지, null 없음.");
            else
                Debug.LogWarning($"[StageRegistry] null 항목 {nullCount}개 발견!");
        }

        // ── 내부 헬퍼 ────────────────────────────────────────────────

        public static StageRegistry GetOrCreateRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<StageRegistry>(ASSET_PATH);
            if (registry != null) return registry;

            // Data 폴더 확인/생성
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            registry = ScriptableObject.CreateInstance<StageRegistry>();
            AssetDatabase.CreateAsset(registry, ASSET_PATH);
            Debug.Log($"[StageRegistry] {ASSET_PATH} 생성됨.");
            return registry;
        }

        private static void SyncToResources(StageRegistry registry)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var existing = AssetDatabase.LoadAssetAtPath<StageRegistry>(RESOURCE_PATH);
            if (existing == null)
            {
                // 복사
                AssetDatabase.CopyAsset(ASSET_PATH, RESOURCE_PATH);
            }
            else
            {
                // 기존 Resources 파일에 stages 동기화
                existing.stages = registry.stages;
                EditorUtility.SetDirty(existing);
            }
        }
    }

    /// <summary>StageRegistry 인스펙터 커스텀 - 버튼 추가</summary>
    [CustomEditor(typeof(StageRegistry))]
    public class StageRegistryInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Scan Stages (Assets/Data/Stages/)"))
                StageRegistryEditor.ScanAndSave();

            if (GUILayout.Button("Validate"))
                StageRegistryEditor.Validate();
        }
    }
}
