using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 스테이지 클리어 기록, 씬 전환 데이터, 터렛 소유 정보를 PlayerPrefs에 저장/로드.
    /// </summary>
    public static class SaveData
    {
        private const string KEY_PREFIX        = "stage_clear_";
        private const string KEY_SELECTED      = "selected_stage";
        private const string KEY_TURRET_POOL   = "selected_turrets";
        private const string KEY_OWNED_PREFIX  = "owned_turret_";

        // ── 스테이지 클리어 ──────────────────────────────────────────

        public static void SetCleared(int stageIndex, bool cleared)
        {
            PlayerPrefs.SetInt(KEY_PREFIX + stageIndex, cleared ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static bool IsCleared(int stageIndex)
            => PlayerPrefs.GetInt(KEY_PREFIX + stageIndex, 0) == 1;

        public static bool IsUnlocked(int stageIndex)
            => stageIndex == 0 || IsCleared(stageIndex - 1);

        // ── 씬 전환 데이터 ───────────────────────────────────────────

        public static int SelectedStageIndex
        {
            get => PlayerPrefs.GetInt(KEY_SELECTED, 0);
            set { PlayerPrefs.SetInt(KEY_SELECTED, value); PlayerPrefs.Save(); }
        }

        public static TurretType[] SelectedTurrets
        {
            get
            {
                string raw = PlayerPrefs.GetString(KEY_TURRET_POOL, "");
                if (string.IsNullOrEmpty(raw)) return new TurretType[0];
                var parts  = raw.Split(',');
                var result = new List<TurretType>();
                foreach (var p in parts)
                    if (System.Enum.TryParse<TurretType>(p.Trim(), out var t))
                        result.Add(t);
                return result.ToArray();
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    PlayerPrefs.DeleteKey(KEY_TURRET_POOL);
                }
                else
                {
                    var parts = new string[value.Length];
                    for (int i = 0; i < value.Length; i++)
                        parts[i] = value[i].ToString();
                    PlayerPrefs.SetString(KEY_TURRET_POOL, string.Join(",", parts));
                }
                PlayerPrefs.Save();
            }
        }

        // ── 터렛 소유 ────────────────────────────────────────────────

        /// <summary>특정 터렛을 소유하고 있는지 확인</summary>
        public static bool IsOwned(TurretType type)
            => PlayerPrefs.GetInt(KEY_OWNED_PREFIX + type.ToString(), 0) == 1;

        /// <summary>특정 터렛의 소유 상태 설정</summary>
        public static void SetOwned(TurretType type, bool owned)
        {
            PlayerPrefs.SetInt(KEY_OWNED_PREFIX + type.ToString(), owned ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// TurretRegistry의 isDefault=true 터렛들을 기본 소유로 초기화.
        /// 아직 한 번도 설정된 적 없는 터렛만 초기화 (기존 데이터 보존).
        /// </summary>
        public static void InitDefaultOwnership(TurretRegistry registry)
        {
            if (registry == null) return;
            foreach (var entry in registry.All)
            {
                if (entry == null || entry.type == TurretType.None) continue;
                string key = KEY_OWNED_PREFIX + entry.type.ToString();
                if (!PlayerPrefs.HasKey(key) && entry.isDefault)
                    PlayerPrefs.SetInt(key, 1);
            }
            PlayerPrefs.Save();
        }

        // ── 시즌패스 ─────────────────────────────────────────────────

        private const string KEY_PASS_LEVEL    = "pass_level";
        private const string KEY_PASS_OWNED    = "pass_owned";
        private const string KEY_PASS_CLAIMED  = "pass_claimed_";   // + "free_N" or "paid_N"

        public const int PASS_MAX_LEVEL = 20;

        /// <summary>현재 패스 레벨 (0~20)</summary>
        public static int PassLevel
        {
            get => PlayerPrefs.GetInt(KEY_PASS_LEVEL, 0);
            set
            {
                PlayerPrefs.SetInt(KEY_PASS_LEVEL, Mathf.Clamp(value, 0, PASS_MAX_LEVEL));
                PlayerPrefs.Save();
            }
        }

        /// <summary>유료 패스 구매 여부</summary>
        public static bool PassOwned
        {
            get => PlayerPrefs.GetInt(KEY_PASS_OWNED, 0) == 1;
            set { PlayerPrefs.SetInt(KEY_PASS_OWNED, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        /// <summary>특정 레벨의 무료/유료 보상 수령 여부</summary>
        public static bool IsPassClaimed(int level, bool paid)
        {
            string key = KEY_PASS_CLAIMED + (paid ? "paid_" : "free_") + level;
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        /// <summary>특정 레벨의 무료/유료 보상 수령 처리</summary>
        public static void SetPassClaimed(int level, bool paid)
        {
            string key = KEY_PASS_CLAIMED + (paid ? "paid_" : "free_") + level;
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>스테이지 클리어 시 패스 레벨 1 증가 (최대치 제한)</summary>
        public static void AddPassLevel()
        {
            if (PassLevel < PASS_MAX_LEVEL) PassLevel = PassLevel + 1;
        }

        // ── 골드 / 보석 ──────────────────────────────────────────────

        private const string KEY_GOLD = "gold";
        private const string KEY_GEM  = "gem";

        public static int Gold
        {
            get => PlayerPrefs.GetInt(KEY_GOLD, 0);
            set { PlayerPrefs.SetInt(KEY_GOLD, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        public static int Gem
        {
            get => PlayerPrefs.GetInt(KEY_GEM, 0);
            set { PlayerPrefs.SetInt(KEY_GEM, Mathf.Max(0, value)); PlayerPrefs.Save(); }
        }

        // ── 전체 초기화 (디버그용) ───────────────────────────────────

        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
