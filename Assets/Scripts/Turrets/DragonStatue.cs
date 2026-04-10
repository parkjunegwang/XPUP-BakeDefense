using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// 좌우로 화염방사기처럼 브레스를 지속 발사.
    /// breathDuration 동안 계속 불을 뿜고, cooldown 후 다시 발사.
    /// 브레스 범위 안에 있는 몬스터는 매 tick마다 데미지 + 화상 도트.
    /// </summary>
    public class DragonStatue : TurretBase
    {
        [Header("Dragon Breath")]
        public float breathDuration  = 2.0f;  // 브레스 지속 시간
        public float breathCooldown  = 3.0f;  // 브레스 사이 쿨다운
        public float breathRange     = 2.2f;  // 좌우 사정거리
        public float breathWidth     = 0.9f;  // 브레스 폭 (세로)
        public float tickInterval    = 0.2f;  // 데미지 tick 간격
        public float tickDamage      = 4f;    // tick 당 데미지
        public float burnDuration    = 2f;    // 화상 지속 시간
        public float burnDamage      = 3f;    // 화상 tick 당 데미지
        public float burnTickRate    = 0.5f;  // 화상 tick 간격

        private enum DragonState { Idle, Breathing }
        private DragonState _state = DragonState.Idle;
        private float _stateTimer;

        // 화염 이펙트 오브젝트 (오른쪽만)
        [Tooltip("공격 스프라이트 (없으면 흰 사각형)")]
        public Sprite attackSprite;

        private GameObject _flameRight;
        private SpriteRenderer _srRight;

        // 화상 추적 (중복 화상 방지)
        private HashSet<Monster> _burning = new HashSet<Monster>();

        protected override void Awake()
        {
            base.Awake();
            _stateTimer = breathCooldown * 0.5f; // 처음엔 절반 쿨다운 후 시작

            // 좌우 화염 이펙트 오브젝트 미리 생성
            // 스프라이트 로드 (없으면 런타임에 기본값 사용)
            if (attackSprite == null)
                attackSprite = Resources.Load<Sprite>("Image/DragonStatue_attack");

            _flameRight = CreateFlame("FlameR");
            _srRight    = _flameRight.GetComponent<SpriteRenderer>();
            SetFlamesActive(false);
        }

        private GameObject CreateFlame(string goName)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = attackSprite != null ? attackSprite : GameSetup.WhiteSquareStatic();
            sr.color        = Color.white; // 색/알파 처리 없음
            sr.sortingOrder = SLayer.Effect;
            return go;
        }

        // OnTick은 fireRate 기반 — 여기선 자체 타이머로 Update override
        protected override void OnTick() { }

        protected override void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.WaveInProgress)
            {
                if (_state == DragonState.Breathing)
                {
                    _state = DragonState.Idle;
                    SetFlamesActive(false);
                }
                return;
            }

            _stateTimer -= Time.deltaTime;

            if (_state == DragonState.Idle)
            {
                if (_stateTimer <= 0f)
                {
                    StartBreathing();
                }
            }
            else // Breathing
            {
                UpdateFlameVisual();

                if (_stateTimer <= 0f)
                {
                    StopBreathing();
                }
            }
        }

        private void StartBreathing()
        {
            _state      = DragonState.Breathing;
            _stateTimer = breathDuration;
            SetFlamesActive(true);
            StartCoroutine(BreathTickRoutine());
        }

        private void StopBreathing()
        {
            _state      = DragonState.Idle;
            _stateTimer = breathCooldown;
            SetFlamesActive(false);
            _burning.Clear();
        }

        // 브레스 동안 매 tick마다 범위 내 몬스터에 데미지
        private IEnumerator BreathTickRoutine()
        {
            while (_state == DragonState.Breathing)
            {
                DamageInBreathRange();
                yield return new WaitForSeconds(tickInterval);
            }
        }

private void DamageInBreathRange()
        {
            var monsters = new List<Monster>(MonsterManager.Instance?.ActiveMonsters ?? new List<Monster>());
            var pos      = (Vector2)transform.position;
            var dir      = Vector2.right; // 오른쪽만
            var perp     = new Vector2(-dir.y, dir.x);

            foreach (var m in monsters)
            {
                if (m == null || !m.IsAlive) continue;
                var rel      = (Vector2)m.transform.position - pos;
                float dot    = Vector2.Dot(rel.normalized, dir);
                float distFw = dot > 0f ? rel.magnitude : -1f;
                float distSd = Mathf.Abs(Vector2.Dot(rel, perp));

                if (dot <= 0.1f || distFw > breathRange || distSd > breathWidth) continue;

                m.TakeDamage(tickDamage, false);

                if (!_burning.Contains(m))
                {
                    _burning.Add(m);
                    StartCoroutine(BurnRoutine(m));
                }
            }
        }

        private IEnumerator BurnRoutine(Monster m)
        {
            float elapsed = 0f;
            while (elapsed < burnDuration)
            {
                yield return new WaitForSeconds(burnTickRate);
                elapsed += burnTickRate;
                if (m == null || !m.IsAlive)
                {
                    _burning.Remove(m);
                    yield break;
                }
                m.TakeDamage(burnDamage, false);
            }
            _burning.Remove(m);
        }

        // 화염 이펙트 애니메이션 (깜빡이며 살아있는 불꽃 느낌)
        private void UpdateFlameVisual()
        {
            float t        = Time.time;
            float flicker  = 0.6f + Mathf.PerlinNoise(t * 8f, 0f) * 0.4f;
            float progress = 1f - (_stateTimer / breathDuration);
            float warmUp   = Mathf.Clamp01(progress * 2f);

            float scaleX = breathRange * warmUp;// * flicker;

            // 스프라이트 비율 기준으로 높이 계산
            float scaleY = breathWidth;
            if (attackSprite != null)
            {
                float ppu  = attackSprite.pixelsPerUnit;
                float sprW = attackSprite.rect.width  / ppu;
                float sprH = attackSprite.rect.height / ppu;
                if (sprW > 0f) scaleY = scaleX * (sprH / sprW);
            }

            // 입 위치: firePoint가 있으면 로컬 x 사용, 없으면 0.5f
            float mouthLocalX = (firePoint != null)
                ? transform.InverseTransformPoint(firePoint.position).x
                : 0.5f;

            // 입에서 시작해서 오른쪽으로 뻗음 (pivot=center이므로 +scaleX*0.5)
            _flameRight.transform.position = firePoint.position;
            _flameRight.transform.localScale    = new Vector3(1f, 1f, 1f);

            float alpha = warmUp * flicker;
            //var c = _srRight.color;
            _srRight.color = new Color(1f, 1f, 1f, alpha);

            // 터렛 스프라이트 위에 확실히 렌더링 (Y소팅 기준으로 터렛보다 +50)
            int turretOrder = Mathf.RoundToInt(500f - transform.position.y * 10f) + SLayer.Turret;
            _srRight.sortingOrder = turretOrder + 50;
        }

private void SetFlamesActive(bool active)
        {
            if (_flameRight != null) _flameRight.SetActive(active);
        }
    }
}
