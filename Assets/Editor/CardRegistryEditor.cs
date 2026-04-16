using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Underdark
{
    public static class CardRegistryEditor
    {
        private const string ASSET_PATH    = "Assets/Data/CardRegistry.asset";
        private const string RESOURCE_PATH = "Assets/Resources/CardRegistry.asset";
        private const string CARDS_FOLDER  = "Assets/Data/Cards";

        [MenuItem("Underdark/Card Registry/Scan Cards (Auto)")]
        public static CardRegistry ScanAndSave()
        {
            var registry = GetOrCreateRegistry();

            var guids = AssetDatabase.FindAssets("t:CardData", new[] { CARDS_FOLDER });
            var list  = new List<CardData>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card != null) list.Add(card);
            }

            // cardName 기준 정렬
            list.Sort((a, b) => string.Compare(a.cardName, b.cardName, System.StringComparison.Ordinal));

            registry.cards = list;
            EditorUtility.SetDirty(registry);
            SyncToResources(registry);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[CardRegistry] {list.Count}개 카드 스캔 완료.");
            return registry;
        }

        [MenuItem("Underdark/Card Registry/Validate")]
        public static void Validate()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CardRegistry>(ASSET_PATH);
            if (registry == null)
            {
                Debug.LogWarning("[CardRegistry] CardRegistry.asset 없음. 먼저 Scan을 실행하세요.");
                return;
            }

            int nullCount = 0;
            int giveTurretCount = 0;
            int buffCount = 0;
            for (int i = 0; i < registry.cards.Count; i++)
            {
                var c = registry.cards[i];
                if (c == null) { nullCount++; continue; }
                if (c.cardType == CardType.GiveTurret) giveTurretCount++;
                else buffCount++;
            }

            if (nullCount == 0)
                Debug.Log($"[CardRegistry] OK - 총 {registry.cards.Count}개 (설치:{giveTurretCount} / 버프:{buffCount}), null 없음.");
            else
                Debug.LogWarning($"[CardRegistry] null 항목 {nullCount}개 발견!");
        }

        public static CardRegistry GetOrCreateRegistry()
        {
            var registry = AssetDatabase.LoadAssetAtPath<CardRegistry>(ASSET_PATH);
            if (registry != null) return registry;

            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            registry = ScriptableObject.CreateInstance<CardRegistry>();
            AssetDatabase.CreateAsset(registry, ASSET_PATH);
            Debug.Log($"[CardRegistry] {ASSET_PATH} 생성됨.");
            return registry;
        }

        private static void SyncToResources(CardRegistry registry)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var existing = AssetDatabase.LoadAssetAtPath<CardRegistry>(RESOURCE_PATH);
            if (existing == null)
                AssetDatabase.CopyAsset(ASSET_PATH, RESOURCE_PATH);
            else
            {
                existing.cards = registry.cards;
                EditorUtility.SetDirty(existing);
            }
        }
    }

    [CustomEditor(typeof(CardRegistry))]
    public class CardRegistryInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Tools", EditorStyles.boldLabel);
            if (GUILayout.Button("Scan Cards (Assets/Data/Cards/)"))
                CardRegistryEditor.ScanAndSave();
            if (GUILayout.Button("Validate"))
                CardRegistryEditor.Validate();
        }
    }
}
