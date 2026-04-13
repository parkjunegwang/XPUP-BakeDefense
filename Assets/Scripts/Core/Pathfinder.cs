using System.Collections.Generic;
using UnityEngine;

namespace Underdark
{
    /// <summary>
    /// A* 경로탐색. 몬스터가 스폰→끝점까지 경로를 계산.
    /// </summary>
    public static class Pathfinder
    {
        private class Node
        {
            public int x, y;
            public float g, h;
            public float F => g + h;
            public Node parent;
            public Node(int x, int y) { this.x = x; this.y = y; }
        }

        // 8방향 (대각선 포함) - 비용: 직선=1.0, 대각선=1.414
        private static readonly (Vector2Int dir, float cost)[] Dirs = {
            (Vector2Int.up,                          1.0f),
            (Vector2Int.down,                        1.0f),
            (Vector2Int.left,                        1.0f),
            (Vector2Int.right,                       1.0f),
            (new Vector2Int( 1,  1),  1.414f),
            (new Vector2Int(-1,  1),  1.414f),
            (new Vector2Int( 1, -1),  1.414f),
            (new Vector2Int(-1, -1),  1.414f),
        };

        /// <summary>
        /// 경로 반환 (그리드 좌표 리스트). 경로 없으면 null.
        /// </summary>
public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, MapManager map)
        {
            var open   = new List<Node>();
            var closed = new HashSet<long>();

            Node startNode = new Node(start.x, start.y);
            startNode.g = 0;
            startNode.h = Heuristic(start.x, start.y, end.x, end.y);
            open.Add(startNode);

            while (open.Count > 0)
            {
                open.Sort((a, b) => a.F.CompareTo(b.F));
                Node current = open[0];
                open.RemoveAt(0);

                if (current.x == end.x && current.y == end.y)
                    return BuildPath(current);

                long key = Key(current.x, current.y);
                if (closed.Contains(key)) continue;
                closed.Add(key);

                foreach (var (dir, cost) in Dirs)
                {
                    int  nx   = current.x + dir.x;
                    int  ny   = current.y + dir.y;
                    long nkey = Key(nx, ny);

                    if (closed.Contains(nkey)) continue;
                    if (map.IsBlocked(nx, ny))  continue;

                    bool isDiag = dir.x != 0 && dir.y != 0;
                    if (isDiag)
                    {
                        // 인접한 두 직선 방향 타일 중 하나라도 막혀있으면 대각선 이동 금지.
                        // (&&이면 "둘 다 막힌" 경우만 차단 → 좁은 벽 틈새를 대각선으로 뚫는 버그 발생)
                        bool sideA = map.IsBlocked(current.x + dir.x, current.y);
                        bool sideB = map.IsBlocked(current.x,         current.y + dir.y);
                        if (sideA || sideB) continue;
                    }

                    float ng   = current.g + cost;
                    Node  next = new Node(nx, ny);
                    next.g      = ng;
                    next.h      = Heuristic(nx, ny, end.x, end.y);
                    next.parent = current;

                    bool skip = false;
                    foreach (var o in open)
                        if (o.x == nx && o.y == ny && o.g <= ng) { skip = true; break; }
                    if (!skip) open.Add(next);
                }
            }
            return null;
        }

        private static List<Vector2Int> BuildPath(Node end)
        {
            var path = new List<Vector2Int>();
            for (Node n = end; n != null; n = n.parent)
                path.Insert(0, new Vector2Int(n.x, n.y));
            return path;
        }

private static float Heuristic(int ax, int ay, int bx, int by) { int dx = Mathf.Abs(ax - bx); int dy = Mathf.Abs(ay - by); return 1.0f * (dx + dy) + (1.414f - 2.0f) * Mathf.Min(dx, dy); }

        private static long Key(int x, int y) => ((long)x << 16) | (uint)y;
    }
}
