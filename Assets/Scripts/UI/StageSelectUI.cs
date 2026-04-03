using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Underdark
{
    public class StageSelectUI : MonoBehaviour
    {
        [Header("Stage Data")]
        public List<StageData> stages = new List<StageData>();
        public string gameSceneName = "GameScene";

        [Header("Container")]
        public Transform buttonContainer;

        [Header("Colors")]
        public Color colorCleared  = new Color(0.3f, 0.9f, 0.4f);
        public Color colorUnlocked = new Color(0.6f, 0.75f, 1f);
        public Color colorLocked   = new Color(0.4f, 0.4f, 0.45f);

        [Header("Debug")]
        public bool resetSaveOnStart = false;

        private void Start()
        {
            if (resetSaveOnStart) SaveData.ResetAll();

            if (stages == null || stages.Count == 0)
            {
                var loaded = Resources.LoadAll<StageData>("Stages");
                stages = new List<StageData>(loaded);
                stages.Sort((a, b) => string.Compare(a.stageName, b.stageName));
            }
            BuildButtons();
        }

        private void BuildButtons()
        {
            if (buttonContainer == null) { Debug.LogWarning("[StageSelectUI] buttonContainer is null!"); return; }

            foreach (Transform c in buttonContainer) Destroy(c.gameObject);

            for (int i = 0; i < stages.Count; i++)
                CreateStageButton(stages[i], i);
        }

        private void CreateStageButton(StageData stage, int idx)
        {
            bool cleared  = SaveData.IsCleared(idx);
            bool unlocked = SaveData.IsUnlocked(idx);

            // ── 루트 버튼 ────────────────────────────────────────────
            var go = new GameObject("StageBtn_" + idx);
            go.transform.SetParent(buttonContainer, false);

            // LayoutElement - 높이 고정
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 120f;
            le.minHeight       = 120f;
            le.flexibleWidth   = 1f;

            var bg = go.AddComponent<Image>();
            bg.color = cleared
                ? new Color(0.12f, 0.38f, 0.18f, 1f)
                : unlocked
                    ? new Color(0.15f, 0.22f, 0.42f, 1f)
                    : new Color(0.12f, 0.12f, 0.18f, 1f);

            var btn = go.AddComponent<Button>();
            btn.interactable = unlocked;

            var outline = go.AddComponent<Outline>();
            outline.effectColor    = unlocked ? new Color(0.5f, 0.6f, 1f, 0.5f) : new Color(0f, 0f, 0f, 0.3f);
            outline.effectDistance = new Vector2(2f, -2f);

            // ── 스테이지 번호 배지 ───────────────────────────────────
            var numGo = new GameObject("NumBadge");
            numGo.transform.SetParent(go.transform, false);
            var numRect = numGo.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0f, 0.5f);
            numRect.anchorMax = new Vector2(0f, 0.5f);
            numRect.pivot     = new Vector2(0f, 0.5f);
            numRect.anchoredPosition = new Vector2(16f, 0f);
            numRect.sizeDelta = new Vector2(60f, 60f);
            var numImg = numGo.AddComponent<Image>();
            numImg.color = unlocked ? new Color(0.3f, 0.4f, 0.8f, 0.8f) : new Color(0.2f, 0.2f, 0.25f, 0.8f);
            var numTmp = new GameObject("Num").AddComponent<TextMeshProUGUI>();
            numTmp.transform.SetParent(numGo.transform, false);
            var numTmpRect = numTmp.GetComponent<RectTransform>();
            numTmpRect.anchorMin = Vector2.zero; numTmpRect.anchorMax = Vector2.one;
            numTmpRect.offsetMin = numTmpRect.offsetMax = Vector2.zero;
            numTmp.text      = (idx + 1).ToString();
            numTmp.fontSize  = 38f;
            numTmp.fontStyle = FontStyles.Bold;
            numTmp.color     = Color.white;
            numTmp.alignment = TextAlignmentOptions.Center;

            // ── 텍스트 영역 ──────────────────────────────────────────
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(go.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = new Vector2(0f, 0f);
            textAreaRect.anchorMax = new Vector2(1f, 1f);
            textAreaRect.offsetMin = new Vector2(92f, 8f);
            textAreaRect.offsetMax = new Vector2(-16f, -8f);

            // 스테이지 이름
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(textArea.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text      = stage != null ? stage.stageName : "Stage";
            titleTmp.fontSize  = 38f;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color     = unlocked ? Color.white : colorLocked;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // 정보 (웨이브 수 + 상태)
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(textArea.transform, false);
            var infoRect = infoGo.AddComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0f, 0f);
            infoRect.anchorMax = new Vector2(1f, 0.5f);
            infoRect.offsetMin = infoRect.offsetMax = Vector2.zero;
            var infoTmp = infoGo.AddComponent<TextMeshProUGUI>();
            string statusStr = cleared ? "✓ CLEAR" : unlocked ? "AVAILABLE" : "🔒 LOCKED";
            int waveCount    = stage != null ? stage.TotalWaves : 0;
            infoTmp.text = $"Waves {waveCount}  ·  {statusStr}";
            infoTmp.fontSize = 38f;
            infoTmp.color    = cleared ? colorCleared : unlocked ? colorUnlocked : colorLocked;
            infoTmp.alignment = TextAlignmentOptions.MidlineLeft;

            // ── 클릭 이벤트 ──────────────────────────────────────────
            if (unlocked)
            {
                int capturedIdx = idx;
                btn.onClick.AddListener(() =>
                {
                    SaveData.SelectedStageIndex = capturedIdx;
                    SceneManager.LoadScene("TowerSelectScene");
                });
            }
        }

        [ContextMenu("Rebuild Buttons")]
        public void DebugRebuild() => BuildButtons();
    }
}
