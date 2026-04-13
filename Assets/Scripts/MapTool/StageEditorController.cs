using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Underdark
{
    /// <summary>
    /// StageData 에셋을 툴씬에서 시각적으로 편집하는 컨트롤러.
    /// StageEditorUI 와 분리: 이 클래스는 데이터 관리, UI는 StageEditorUI가 담당.
    /// </summary>
    public class StageEditorController : MonoBehaviour
    {
        // ── 현재 편집 중인 StageData ─────────────────────────────────
        public StageData currentStage { get; private set; }
        public bool      isDirty      { get; private set; }

        // ── 선택 상태 ─────────────────────────────────────────────────
        public int selectedWaveIdx  { get; private set; } = -1;
        public int selectedGroupIdx { get; private set; } = -1;

        // ── 이벤트 ────────────────────────────────────────────────────
        public System.Action onDataChanged;   // 스프레드시트 전체 갱신 필요
        public System.Action onSelectionChanged; // 상세 패널만 갱신

        // ── 에셋 경로 ─────────────────────────────────────────────────
        private string _assetPath = "Assets/Data/Stages/StageData.asset";

        // ─────────────────────────────────────────────────────────────
        #region Load / Save
        // ─────────────────────────────────────────────────────────────

        public void NewStage()
        {
            currentStage = ScriptableObject.CreateInstance<StageData>();
            currentStage.stageName = "New Stage";
            currentStage.waves     = new List<WaveData>();
            // 웨이브 3개 초기 추가
            for (int i = 0; i < 3; i++) AddWave();
            isDirty = false;
            selectedWaveIdx  = -1;
            selectedGroupIdx = -1;
            onDataChanged?.Invoke();
        }

        public void LoadStage(StageData asset)
        {
            if (asset == null) return;
            currentStage = asset;
            isDirty = false;
            selectedWaveIdx  = currentStage.waves.Count > 0 ? 0 : -1;
            selectedGroupIdx = -1;
            onDataChanged?.Invoke();
        }

        /// <summary>
        /// 현재 편집 내용을 연결된 에셋에 저장 (Unity Editor 전용).
        /// 런타임에서는 경고만 출력.
        /// </summary>
        public void SaveStage()
        {
#if UNITY_EDITOR
            if (currentStage == null) { Debug.LogWarning("[StageEditor] 저장할 StageData 없음"); return; }

            string path = AssetDatabase.GetAssetPath(currentStage);
            if (string.IsNullOrEmpty(path))
            {
                // 새 에셋이면 경로 생성
                path = _assetPath;
                EnsureFolder(System.IO.Path.GetDirectoryName(path));
                AssetDatabase.CreateAsset(currentStage, path);
            }
            EditorUtility.SetDirty(currentStage);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            isDirty = false;
            Debug.Log($"[StageEditor] 저장 완료: {path}");
#else
            Debug.LogWarning("[StageEditor] Save는 Unity Editor 전용입니다.");
#endif
        }

        private void EnsureFolder(string folderPath)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(folderPath)) return;
            folderPath = folderPath.Replace('\\', '/');
            var parts  = folderPath.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Wave CRUD
        // ─────────────────────────────────────────────────────────────

        public void AddWave()
        {
            if (currentStage == null) return;
            var w = new WaveData();
            w.waveName = $"Wave {currentStage.waves.Count + 1}";
            // 기본 그룹 1개 포함
            w.groups.Add(MakeDefaultGroup(currentStage.waves.Count + 1));
            currentStage.waves.Add(w);
            MarkDirty();
        }

        public void DuplicateWave(int waveIdx)
        {
            if (!ValidWave(waveIdx)) return;
            var src  = currentStage.waves[waveIdx];
            var copy = JsonUtility.FromJson<WaveData>(JsonUtility.ToJson(src));
            copy.waveName += " (copy)";
            currentStage.waves.Insert(waveIdx + 1, copy);
            MarkDirty();
        }

        public void DeleteWave(int waveIdx)
        {
            if (!ValidWave(waveIdx)) return;
            currentStage.waves.RemoveAt(waveIdx);
            selectedWaveIdx  = Mathf.Clamp(selectedWaveIdx,  -1, currentStage.waves.Count - 1);
            selectedGroupIdx = -1;
            MarkDirty();
        }

        public void MoveWaveUp(int waveIdx)
        {
            if (!ValidWave(waveIdx) || waveIdx == 0) return;
            var tmp = currentStage.waves[waveIdx];
            currentStage.waves[waveIdx]     = currentStage.waves[waveIdx - 1];
            currentStage.waves[waveIdx - 1] = tmp;
            if (selectedWaveIdx == waveIdx) selectedWaveIdx--;
            MarkDirty();
        }

        public void MoveWaveDown(int waveIdx)
        {
            if (!ValidWave(waveIdx) || waveIdx >= currentStage.waves.Count - 1) return;
            var tmp = currentStage.waves[waveIdx];
            currentStage.waves[waveIdx]     = currentStage.waves[waveIdx + 1];
            currentStage.waves[waveIdx + 1] = tmp;
            if (selectedWaveIdx == waveIdx) selectedWaveIdx++;
            MarkDirty();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Group CRUD
        // ─────────────────────────────────────────────────────────────

        public void AddGroup(int waveIdx)
        {
            if (!ValidWave(waveIdx)) return;
            currentStage.waves[waveIdx].groups.Add(MakeDefaultGroup(waveIdx + 1));
            MarkDirty();
        }

        public void DeleteGroup(int waveIdx, int groupIdx)
        {
            if (!ValidGroup(waveIdx, groupIdx)) return;
            currentStage.waves[waveIdx].groups.RemoveAt(groupIdx);
            if (selectedWaveIdx == waveIdx && selectedGroupIdx == groupIdx)
                selectedGroupIdx = -1;
            MarkDirty();
        }

        public void DuplicateGroup(int waveIdx, int groupIdx)
        {
            if (!ValidGroup(waveIdx, groupIdx)) return;
            var src  = currentStage.waves[waveIdx].groups[groupIdx];
            var copy = JsonUtility.FromJson<MonsterSpawnGroup>(JsonUtility.ToJson(src));
            currentStage.waves[waveIdx].groups.Insert(groupIdx + 1, copy);
            MarkDirty();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Selection
        // ─────────────────────────────────────────────────────────────

        public void Select(int waveIdx, int groupIdx)
        {
            selectedWaveIdx  = waveIdx;
            selectedGroupIdx = groupIdx;
            onSelectionChanged?.Invoke();
        }

        public WaveData SelectedWave =>
            ValidWave(selectedWaveIdx) ? currentStage.waves[selectedWaveIdx] : null;

        public MonsterSpawnGroup SelectedGroup =>
            ValidGroup(selectedWaveIdx, selectedGroupIdx)
                ? currentStage.waves[selectedWaveIdx].groups[selectedGroupIdx]
                : null;

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Auto-Scale Helpers
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 모든 웨이브의 HP/Speed를 선형으로 스케일링.
        /// waveIdx=0 → startHp/startSpeed, 마지막 → endHp/endSpeed
        /// </summary>
        public void AutoScaleAll(float startHp, float endHp,
                                 float startSpeed, float endSpeed)
        {
            if (currentStage == null) return;
            int total = currentStage.waves.Count;
            if (total == 0) return;

            for (int wi = 0; wi < total; wi++)
            {
                float t = total > 1 ? (float)wi / (total - 1) : 0f;
                float hp    = Mathf.Lerp(startHp,    endHp,    t);
                float speed = Mathf.Lerp(startSpeed, endSpeed, t);

                foreach (var g in currentStage.waves[wi].groups)
                {
                    g.hp    = Mathf.Round(hp);
                    g.speed = Mathf.Round(speed * 10f) / 10f;
                }
            }
            MarkDirty();
        }

        /// <summary>
        /// 모든 웨이브의 Count를 선형 스케일링
        /// </summary>
        public void AutoScaleCount(int startCount, int endCount)
        {
            if (currentStage == null) return;
            int total = currentStage.waves.Count;
            for (int wi = 0; wi < total; wi++)
            {
                float t = total > 1 ? (float)wi / (total - 1) : 0f;
                int cnt = Mathf.RoundToInt(Mathf.Lerp(startCount, endCount, t));
                foreach (var g in currentStage.waves[wi].groups)
                    g.count = Mathf.Max(1, cnt);
            }
            MarkDirty();
        }

        /// <summary>
        /// 지정 웨이브의 이름을 "Wave N" 형식으로 자동 갱신
        /// </summary>
        public void RenameWavesAuto()
        {
            if (currentStage == null) return;
            for (int i = 0; i < currentStage.waves.Count; i++)
                currentStage.waves[i].waveName = $"Wave {i + 1}";
            MarkDirty();
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Field Setters (UI → Data)
        // ─────────────────────────────────────────────────────────────

        public void SetWaveName(int waveIdx, string val)
        {
            if (!ValidWave(waveIdx)) return;
            currentStage.waves[waveIdx].waveName = val;
            MarkDirty(false);
        }

        public void SetWaveXp(int waveIdx, int val)
        {
            if (!ValidWave(waveIdx)) return;
            currentStage.waves[waveIdx].waveCompleteXp = val;
            MarkDirty(false);
        }

        public void SetGroupField(int wi, int gi, string field, float val)
        {
            if (!ValidGroup(wi, gi)) return;
            var g = currentStage.waves[wi].groups[gi];
            switch (field)
            {
                case "hp":            g.hp            = val;             break;
                case "speed":         g.speed         = val;             break;
                case "count":         g.count         = Mathf.Max(1, (int)val); break;
                case "reward":        g.reward        = Mathf.Max(0, (int)val); break;
                case "interval":      g.spawnInterval = Mathf.Max(0f, val); break;
                case "bossHpMult":    g.bossHpMult    = Mathf.Max(0.1f, val); break;
            }
            MarkDirty(false);
        }

        public void SetGroupBoss(int wi, int gi, bool val)
        {
            if (!ValidGroup(wi, gi)) return;
            currentStage.waves[wi].groups[gi].isBoss = val;
            MarkDirty(false);
        }

        public void SetStageName(string val)
        {
            if (currentStage == null) return;
            currentStage.stageName = val;
            MarkDirty(false);
        }

        #endregion

        // ─────────────────────────────────────────────────────────────
        #region Internal Helpers
        // ─────────────────────────────────────────────────────────────

        private bool ValidWave(int wi) =>
            currentStage != null && currentStage.waves != null &&
            wi >= 0 && wi < currentStage.waves.Count;

        private bool ValidGroup(int wi, int gi) =>
            ValidWave(wi) && currentStage.waves[wi].groups != null &&
            gi >= 0 && gi < currentStage.waves[wi].groups.Count;

        private void MarkDirty(bool fireEvent = true)
        {
            isDirty = true;
            if (fireEvent) onDataChanged?.Invoke();
        }

        private MonsterSpawnGroup MakeDefaultGroup(int waveNumber)
        {
            return new MonsterSpawnGroup
            {
                count         = 5 + waveNumber,
                hp            = 30f + waveNumber * 15f,
                speed         = 1.8f + waveNumber * 0.05f,
                reward        = 5,
                spawnInterval = 0.8f,
                isBoss        = false,
                bossHpMult    = 1f,
            };
        }

        #endregion
    }
}
