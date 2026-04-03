using UnityEngine;
using UnityEditor;

namespace Underdark
{
    [CustomEditor(typeof(TurretBase), true)]
    public class TurretRangePreviewEditor : Editor
    {
        // ── 색상 ──────────────────────────────────────────────────────
        static readonly Color ColCircleFill    = new Color(0.3f, 0.9f, 0.4f, 0.12f);
        static readonly Color ColCircleLine    = new Color(0.3f, 0.9f, 0.4f, 0.80f);
        static readonly Color ColTileFill      = new Color(0.4f, 0.7f, 1.0f, 0.18f);
        static readonly Color ColTileLine      = new Color(0.4f, 0.7f, 1.0f, 0.80f);
        static readonly Color ColMeleeFill     = new Color(1.0f, 0.9f, 0.2f, 0.20f);
        static readonly Color ColMeleeLine     = new Color(1.0f, 0.9f, 0.2f, 0.90f);
        static readonly Color ColDragonFill    = new Color(1.0f, 0.5f, 0.1f, 0.15f);
        static readonly Color ColDragonLine    = new Color(1.0f, 0.5f, 0.1f, 0.85f);
        static readonly Color ColBHDetect      = new Color(0.6f, 0.1f, 1.0f, 0.10f);
        static readonly Color ColBHDetectLine  = new Color(0.6f, 0.1f, 1.0f, 0.50f);
        static readonly Color ColBHHole        = new Color(0.8f, 0.2f, 1.0f, 0.22f);
        static readonly Color ColBHHoleLine    = new Color(0.9f, 0.3f, 1.0f, 0.90f);
        static readonly Color ColElecFill      = new Color(0.4f, 0.8f, 1.0f, 0.15f);
        static readonly Color ColElecLine      = new Color(0.4f, 0.8f, 1.0f, 0.85f);
        static readonly Color ColDmgFill       = new Color(0.9f, 0.2f, 0.2f, 0.18f); // 데미지 타일

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                if (target is AreaDamageTurret area && Application.isPlaying)
                    area.RefreshAreaVisual();
                SceneView.RepaintAll();
            }

            // 정보 패널
            var tb = (TurretBase)target;
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("── Attack Preview ──", EditorStyles.centeredGreyMiniLabel);

            if (tb is DragonStatue d)
            {
                EditorGUILayout.LabelField($"Detect range : {tb.range:F2}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Breath W×H  : {d.breathRange:F2} × {d.breathWidth:F2}", EditorStyles.miniLabel);
            }
            else if (tb is BlackHoleTurret bh)
            {
                EditorGUILayout.LabelField($"Detect range : {tb.range:F2}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Hole radius  : {bh.holeRadius:F2}", EditorStyles.miniLabel);
            }
            else if (tb is MeleeTurret mt)
            {
                var dirs = new[]{"Down","Left","Up","Right"};
                EditorGUILayout.LabelField($"Direction    : {dirs[GetDirIndex(mt)]}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Range        : {tb.range:F2}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField($"Range : {tb.range:F2}", EditorStyles.miniLabel);
            }
        }

void OnSceneGUI()
        {
            var tb = (TurretBase)target;
            if (tb == null) return;
            Vector3 pos = tb.transform.position;

            // turretType이 None이면 클래스 타입으로 추론
            var type = tb.turretType;
            if (type == TurretType.None)
            {
                if      (tb is MeleeTurret)    type = TurretType.MeleeTurret;
                else if (tb is SpikeTrap)      type = TurretType.SpikeTrap;
                else if (tb is ElectricGate)   type = TurretType.ElectricGate;
                else if (tb is DragonStatue)   type = TurretType.DragonStatue;
                else if (tb is BlackHoleTurret)type = TurretType.BlackHole;
                else if (tb is AreaDamageTurret)type = TurretType.AreaDamage;
                else if (tb is WallTurret)     type = TurretType.Wall;
            }

            switch (type)
            {
                // ── MeleeTurret: 방향 앞 N칸 하이라이트 ─────────────────
                case TurretType.MeleeTurret:
                    DrawMeleePreview(tb as MeleeTurret, pos);
                    break;

                // ── SpikeTrap: 터렛 위치 타일 + 데미지 반경 ──
                case TurretType.SpikeTrap:
                    DrawSingleTile(pos, ColTileFill, ColTileLine);
                    DrawCircle(pos, tb.range, ColDmgFill, new Color(1f, 0.35f, 0.35f, 0.7f));
                    Handles.color = Color.white;
                    Handles.Label(pos + Vector3.up * 0.55f, $"r={tb.range:F1}", EditorStyles.miniLabel);
                    break;

                // ── ElectricGate: 3x1 타일, 양끝만 데미지 ───────────────
                case TurretType.ElectricGate:
                    DrawElectricPreview(pos, tb.range);
                    break;

                // ── DragonStatue: 탐지 원 + 좌우 브레스 사각형 ──────────
                case TurretType.DragonStatue:
                    var dr = tb as DragonStatue;
                    DrawCircle(pos, tb.range, new Color(1f, 0.5f, 0.1f, 0.06f), new Color(1f, 0.6f, 0.2f, 0.35f));
                    if (dr != null)
                        DrawBreathRects(pos, dr.breathRange, dr.breathWidth * 0.5f);
                    Handles.color = Color.white;
                    Handles.Label(pos + Vector3.right * tb.range + Vector3.up * 0.2f,
                        $"detect={tb.range:F1}", EditorStyles.miniLabel);
                    break;

                // ── BlackHole: 탐지 원(연함) + 홀 반경(진함) ────────────
                case TurretType.BlackHole:
                    var bh = tb as BlackHoleTurret;
                    DrawCircle(pos, tb.range,    ColBHDetect,  ColBHDetectLine);
                    if (bh != null)
                        DrawCircle(pos, bh.holeRadius, ColBHHole, ColBHHoleLine);
                    Handles.color = Color.white;
                    Handles.Label(pos + Vector3.right * tb.range + Vector3.up * 0.2f,
                        $"detect={tb.range:F1}  hole={bh?.holeRadius:F1}", EditorStyles.miniLabel);
                    break;

                // ── AreaDamage: 사각형 범위 ───────────────
                case TurretType.AreaDamage:
                    float ra = tb.range > 0 ? tb.range : 1.8f;
                    DrawBox(pos, ra, ra, new Color(0.85f, 0.2f, 1f, 0.12f), new Color(0.85f, 0.2f, 1f, 0.8f));
                    Handles.color = Color.white;
                    Handles.Label(pos + Vector3.right * ra + Vector3.up * 0.2f,
                        $"r={ra:F1}", EditorStyles.miniLabel);
                    break;

                // ── 벽: 미리보기 없음 ────────────────────────────────────
                case TurretType.Wall:
                case TurretType.Wall2x1:
                case TurretType.Wall1x2:
                case TurretType.Wall2x2:
                    break;

                // ── 기본: 원형 ───────────────────────────────────────────
                default:
                    if (tb.range > 0f)
                    {
                        DrawCircle(pos, tb.range, ColCircleFill, ColCircleLine);
                        Handles.color = Color.white;
                        Handles.Label(pos + Vector3.right * tb.range + Vector3.up * 0.2f,
                            $"r={tb.range:F1}", EditorStyles.miniLabel);
                    }
                    break;
            }
        }

        // ── 유틸 ──────────────────────────────────────────────────────

        void DrawCircle(Vector3 c, float r, Color fill, Color line)
        {
            Handles.color = fill;
            Handles.DrawSolidDisc(c, Vector3.forward, r);
            Handles.color = line;
            Handles.DrawWireDisc(c, Vector3.forward, r);
        }

        void DrawBox(Vector3 c, float hw, float hh, Color fill, Color line)
        {
            var pts = new Vector3[]
            {
                c + new Vector3(-hw, -hh),
                c + new Vector3( hw, -hh),
                c + new Vector3( hw,  hh),
                c + new Vector3(-hw,  hh),
            };
            Handles.DrawSolidRectangleWithOutline(pts, fill, line);
        }

        // 단일 타일 박스
        void DrawSingleTile(Vector3 center, Color fill, Color line)
        {
            float s = GetStep() * 0.48f;
            DrawBox(center, s, s, fill, line);
        }

        // 타일 크기 기반 N x M 박스
        void DrawTilePreview(Vector3 origin, int cols, int rows, Color fill, Color line)
        {
            float step = GetStep();
            float w = cols * step;
            float h = rows * step;
            // origin은 좌하단 타일 기준 → 중심 보정
            Vector3 center = origin + new Vector3((cols - 1) * step * 0.5f, (rows - 1) * step * 0.5f);
            DrawBox(center, w * 0.5f, h * 0.5f, fill, line);
        }

        void DrawMeleePreview(MeleeTurret mt, Vector3 pos)
        {
            if (mt == null) return;
            var dir = mt.FacingDir;
            float step = GetStep();

            // 공격 범위 (방향 앞 N칸) - 칸 단위 박스로 표시
            int tiles = Mathf.Max(1, Mathf.RoundToInt(mt.range / step));
            for (int i = 1; i <= tiles; i++)
            {
                Vector3 tileCenter = pos + new Vector3(dir.x * step * i, dir.y * step * i);
                // 데미지 타일 (진한 노랑)
                float alpha = Mathf.Lerp(0.35f, 0.12f, (float)(i - 1) / tiles);
                Color f = new Color(1f, 0.9f, 0.15f, alpha);
                Color l = new Color(1f, 0.9f, 0.15f, 0.85f);
                DrawBox(tileCenter, step * 0.48f, step * 0.48f, f, l);
            }

            // 방향 화살표
            Vector3 arrowEnd = pos + new Vector3(dir.x * step * tiles, dir.y * step * tiles);
            Handles.color = ColMeleeLine;
            Handles.DrawLine(pos, arrowEnd);
            // 화살촉
            Vector3 right = new Vector3(-dir.y, dir.x) * 0.15f;
            Vector3 tip   = arrowEnd;
            Vector3 back  = arrowEnd - new Vector3(dir.x, dir.y) * 0.2f;
            Handles.DrawLine(tip, back + right);
            Handles.DrawLine(tip, back - right);
        }

        void DrawElectricPreview(Vector3 pos, float range)
        {
            float step = GetStep();
            // 3칸짜리 터렛: 왼쪽, 가운데(통과가능), 오른쪽
            // 위치: origin이 가운데 기둥이라고 가정 (실제론 왼쪽 기둥이 origin)
            // 데미지: 양끝 2칸
            for (int i = 0; i < 3; i++)
            {
                Vector3 tc = pos + new Vector3(step * i, 0f);
                bool isDmg = (i == 0 || i == 2);
                Color f = isDmg ? ColDmgFill : new Color(0.4f, 0.8f, 1f, 0.06f);
                Color l = isDmg ? new Color(1f, 0.4f, 0.4f, 0.8f) : ColElecLine;
                DrawBox(tc, step * 0.48f, step * 0.48f, f, l);
            }
            // 전기 빔 라인
            Handles.color = ColElecLine;
            Handles.DrawLine(pos + new Vector3(0, 0.02f), pos + new Vector3(step * 2, 0.02f));
            Handles.DrawLine(pos - new Vector3(0, 0.02f), pos + new Vector3(step * 2, -0.02f));
        }

        void DrawBreathRects(Vector3 pos, float rangeX, float halfY)
        {
            // 왼쪽
            Vector3 lCenter = pos + new Vector3(-rangeX * 0.5f, 0f);
            DrawBox(lCenter, rangeX * 0.5f, halfY, ColDragonFill, ColDragonLine);
            // 오른쪽
            Vector3 rCenter = pos + new Vector3(rangeX * 0.5f, 0f);
            DrawBox(rCenter, rangeX * 0.5f, halfY, ColDragonFill, ColDragonLine);
        }

        float GetStep()
        {
            if (MapManager.Instance != null)
                return MapManager.Instance.tileSize + MapManager.Instance.tileGap;
            return 1.05f; // 에디터 기본값
        }

        int GetDirIndex(MeleeTurret mt)
        {
            // FacingDir 리플렉션으로 가져오기 (private _dirIndex 직접 접근 불가)
            var dir = mt.FacingDir;
            if (dir == new Vector2Int(0, -1)) return 0;
            if (dir == new Vector2Int(-1, 0)) return 1;
            if (dir == new Vector2Int(0,  1)) return 2;
            return 3;
        }
    }
}
