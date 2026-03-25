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

        private static readonly Vector2Int[] Dirs = {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
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
                // 가장 F가 낮은 노드 선택
                open.Sort((a, b) => a.F.CompareTo(b.F));
                Node current = open[0];
                open.RemoveAt(0);

                if (current.x == end.x && current.y == end.y)
                    return BuildPath(current);

                long key = Key(current.x, current.y);
                if (closed.Contains(key)) continue;
                closed.Add(key);

                foreach (var dir in Dirs)
                {
                    int nx = current.x + dir.x;
                    int ny = current.y + dir.y;
                    long nkey = Key(nx, ny);
                    if (closed.Contains(nkey)) continue;
                    if (map.IsBlocked(nx, ny)) continue;

                    float ng = current.g + 1f;
                    Node next = new Node(nx, ny);
                    next.g = ng;
                    next.h = Heuristic(nx, ny, end.x, end.y);
                    next.parent = current;

                    // 이미 열린 리스트에 있고 더 나쁘면 스킵
                    bool skip = false;
                    foreach (var o in open)
                        if (o.x == nx && o.y == ny && o.g <= ng) { skip = true; break; }
                    if (!skip) open.Add(next);
                }
            }
            return null; // 경로 없음
        }

        private static List<Vector2Int> BuildPath(Node end)
        {
            var path = new List<Vector2Int>();
            for (Node n = end; n != null; n = n.parent)
                path.Insert(0, new Vector2Int(n.x, n.y));
            return path;
        }

        private static float Heuristic(int ax, int ay, int bx, int by)
            => Mathf.Abs(ax - bx) + Mathf.Abs(ay - by);

        private static long Key(int x, int y) => ((long)x << 16) | (uint)y;
    }
}
