using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 스테이지 클리어 기록을 PlayerPrefs에 저장/로드.
    /// 씬 전환 시 선택한 스테이지 인덱스도 여기서 관리.
    /// </summary>
    public static class SaveData
    {
        private const string KEY_PREFIX       = "stage_clear_";
        private const string KEY_SELECTED     = "selected_stage";
        private const string KEY_TURRET_POOL  = "selected_turrets";

        /// <summary>스테이지 클리어 여부 저장</summary>
        public static void SetCleared(int stageIndex, bool cleared)
        {
            PlayerPrefs.SetInt(KEY_PREFIX + stageIndex, cleared ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>스테이지 클리어 여부 확인</summary>
        public static bool IsCleared(int stageIndex)
            => PlayerPrefs.GetInt(KEY_PREFIX + stageIndex, 0) == 1;

        /// <summary>해당 스테이지가 플레이 가능한지 (0번은 항상 가능, 이후는 이전 스테이지 클리어 필요)</summary>
        public static bool IsUnlocked(int stageIndex)
            => stageIndex == 0 || IsCleared(stageIndex - 1);

        /// <summary>게임 씬으로 넘길 선택된 스테이지 인덱스</summary>
        public static int SelectedStageIndex
        {
            get => PlayerPrefs.GetInt(KEY_SELECTED, 0);
            set { PlayerPrefs.SetInt(KEY_SELECTED, value); PlayerPrefs.Save(); }
        }

        /// <summary>전체 클리어 기록 초기화 (디버그용)</summary>
        public static TurretType[] SelectedTurrets { get { string raw = PlayerPrefs.GetString(KEY_TURRET_POOL, ""); if (string.IsNullOrEmpty(raw)) return new TurretType[0]; var parts = raw.Split(','); var result = new System.Collections.Generic.List<TurretType>(); foreach (var p in parts) { if (System.Enum.TryParse<TurretType>(p.Trim(), out var t)) result.Add(t); } return result.ToArray(); } set { if (value == null || value.Length == 0) { PlayerPrefs.DeleteKey(KEY_TURRET_POOL); } else { var parts = new string[value.Length]; for (int i = 0; i < value.Length; i++) parts[i] = value[i].ToString(); PlayerPrefs.SetString(KEY_TURRET_POOL, string.Join(",", parts)); } PlayerPrefs.Save(); } }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
