using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using Utils;
using static Raylib_cs.Raylib;
using static Utils.RenderingUtils;

public class Program
{
    static float Theta = 0f;
    static int Steps = 720;
    static float RenderSize = 0.1f;
    static bool DoRotation = false;
    static int Count = 8;

    static Vector2 MicPos = new(0f, 0f);

    static Vector2 TransformVec(Vector2 vector, Vector2 i, Vector2 j)
    {
        return vector.X * i + vector.Y * j;
    }

    static Vector2 RotateVec(Vector2 vec, bool should = true)
    {
        if (!should)
            return vec;
        return TransformVec(
            vec,
            new(MathF.Cos(Theta / 180f * MathF.PI), MathF.Sin(Theta / 180f * MathF.PI)),
            new(-MathF.Sin(Theta / 180f * MathF.PI), MathF.Cos(Theta / 180f * MathF.PI))
        );
    }

    static Vector2 RotateVec(Vector2 vec, float theta, bool should = true)
    {
        if (!should)
            return vec;
        return TransformVec(
            vec,
            new(MathF.Cos(theta / 180f * MathF.PI), MathF.Sin(theta / 180f * MathF.PI)),
            new(-MathF.Sin(theta / 180f * MathF.PI), MathF.Cos(theta / 180f * MathF.PI))
        );
    }

    static Vector2 DoAxisFlip(Vector2 vector, float theta, float xo, float yo)
    {
        float thetaRad = theta / 180f * MathF.PI;
        Vector2 copy = new(vector.X, vector.Y);

        copy -= new Vector2(xo, yo);
        copy = TransformVec(
            copy,
            new(MathF.Cos(-thetaRad), MathF.Sin(-thetaRad)),
            new(-MathF.Sin(-thetaRad), MathF.Cos(-thetaRad))
        );

        copy = TransformVec(copy, new(1f, 0f), new(0f, -1f));

        copy = TransformVec(
            copy,
            new(MathF.Cos(thetaRad), MathF.Sin(thetaRad)),
            new(-MathF.Sin(thetaRad), MathF.Cos(thetaRad))
        );
        copy += new Vector2(xo, yo);

        return copy;
    }

    static void DrawNodes(List<Vector2> nodes)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            Rect(RotateVec(nodes[i], DoRotation), new(0.1f, 0.1f), Color.White);

            (int x, int y) coords = TransformCoords(RotateVec(nodes[i], DoRotation));
            DrawText($"{i}", coords.x - 10, coords.y - 20, 10, Color.Yellow);
        }
    }

    static void DrawLinks(
        List<(int from, int to)> links,
        List<Vector2> nodes,
        int selectedEdge,
        int lastCollision
    )
    {
        for (int i = 0; i < links.Count; i++)
        {
            Line(
                RotateVec(nodes[links[i].from], DoRotation),
                RotateVec(nodes[links[i].to], DoRotation),
                i == selectedEdge ? Color.Yellow : Color.White
            );
        }

        // int collisionEdge = GetCollisionEdge(nodes, links, lastCollision);
        // if (collisionEdge == -1) return;

        // Line(
        //     RotateVec(nodes[links[collisionEdge].from], DoRotation),
        //     RotateVec(nodes[links[collisionEdge].to], DoRotation),
        //     Color.Red
        // );
    }

    static Vector2 MirrorNodeAlongAxis(
        List<(int from, int to)> links,
        List<Vector2> nodes,
        Vector2 node,
        int selectedEdge
    )
    {
        if (selectedEdge == -1)
            return node;

        Vector2 edgeStart = nodes[links[selectedEdge].from];
        Vector2 edgeEnd = nodes[links[selectedEdge].to];

        float dx = edgeEnd.X - edgeStart.X;
        float dy = edgeEnd.Y - edgeStart.Y;

        float theta = MathF.Atan2(dy, dx);

        float xo = edgeStart.X;
        float yo = edgeStart.Y;

        return DoAxisFlip(node, theta * 180 / MathF.PI, xo, yo);
    }

    static List<Vector2> MirrorGraphAlongAxis(
        List<(int from, int to)> links,
        List<Vector2> nodes,
        int selectedEdge
    )
    {
        if (selectedEdge == -1)
            return nodes;

        Vector2 edgeStart = nodes[links[selectedEdge].from];
        Vector2 edgeEnd = nodes[links[selectedEdge].to];

        float dx = edgeEnd.X - edgeStart.X;
        float dy = edgeEnd.Y - edgeStart.Y;

        float theta = MathF.Atan2(dy, dx);

        float xo = edgeStart.X;
        float yo = edgeStart.Y;

        return nodes.Select(n => DoAxisFlip(n, theta * 180 / MathF.PI, xo, yo)).ToList();
    }

    static List<Vector2> TransformNodes(
        List<Vector2> nodes,
        List<(int from, int to)> links,
        List<int> transforms,
        int count
    )
    {
        List<Vector2> copy = nodes;

        for (int i = 0; i < count; i++)
            copy = MirrorGraphAlongAxis(links, copy, transforms[i]);

        return copy;
    }

    static Vector2 TransformNodesMic(
        Vector2 mic,
        List<Vector2> nodes,
        List<(int from, int to)> links,
        List<int> transforms,
        int count
    )
    {
        Vector2 copy = mic;
        List<Vector2> graphCopy = nodes;

        for (int i = 0; i < count; i++)
        {
            graphCopy = MirrorGraphAlongAxis(links, graphCopy, transforms[i]);
            copy = MirrorNodeAlongAxis(links, graphCopy, copy, transforms[i]);
        }

        return copy;
    }

    static int GetCollisionEdge(
        List<Vector2> nodes,
        List<(int from, int to)> links,
        float theta,
        int lastLink
    )
    {
        int collisionEdge = -1;
        float lastY = -999;

        for (int i = 0; i < links.Count; i++)
        {
            Vector2 a = RotateVec(nodes[links[i].from], theta);
            Vector2 b = RotateVec(nodes[links[i].to], theta);

            if (b.X < a.X)
            {
                Vector2 temp = new(a.X, a.Y);

                a.X = b.X;
                a.Y = b.Y;

                b.X = temp.X;
                b.Y = temp.Y;
            }

            if (i != lastLink && a.X <= 0 && b.X >= 0)
            {
                float t = (0f - a.X) / (b.X - a.X);
                float y = (1 - t) * a.Y + t * b.Y;

                if (y > lastY)
                {
                    collisionEdge = i;
                    lastY = y;
                }
            }
        }

        return collisionEdge;
    }

    static List<int> SelectEdges(
        List<Vector2> nodes,
        List<(int from, int to)> links,
        int count,
        float theta
    )
    {
        List<Vector2> currNodes = nodes;
        List<int> edges = [GetCollisionEdge(currNodes, links, theta, -1)];

        for (int i = 0; i < count - 1; i++)
        {
            currNodes = MirrorGraphAlongAxis(links, currNodes, edges[i]);
            edges.Add(GetCollisionEdge(currNodes, links, theta, edges.Count > 0 ? edges[i] : -1));

            if (edges[edges.Count - 1] == -1)
            {
                System.Console.WriteLine("ERR");
                GetCollisionEdge(currNodes, links, theta, edges[edges.Count - 2]);
            }
        }

        return edges;
    }

    static int PackPath(List<int> edges)
    {
        int result = 0;

        for (int i = 0; i < 8; i++)
        {
            int curr = i < edges.Count ? edges[i] : 0xF;
            result += curr << (4 * (7 - i));
        }

        return result;
    }

    static List<int> UnpackPath(int path)
    {
        List<int> edges = new();

        for (int i = 0; i < 8; i++)
        {
            int curr = (path >> (4 * (7 - i))) & 0xF;
            if (curr == 0xF)
                break;

            edges.Add(curr);
        }

        return edges;
    }

    static void Main(string[] args)
    {
        Color[] colors =
        [
            Color.Red,
            Color.Green,
            Color.Blue,
            Color.Yellow,
            Color.Pink,
            Color.Brown,
        ];

        bool placementMode = false;
        List<int> selectedEdges = new();

        InitWindow(1920, 1080, "Test");
        SetTargetFPS(120);

        rlImGui.Setup(true);

        List<Vector2> nodes = new();
        List<(int from, int to)> links = new();

        List<(int path, float start, float end)> data = new();

        while (!WindowShouldClose())
        {
            if (!placementMode && nodes.Count > 0)
                selectedEdges = SelectEdges(nodes, links, Count, Theta);

            Zoom *= 1 + 0.1f * GetMouseWheelMoveV().Y;

            Vector2 mousePos = GetMousePosition();
            mousePos = ClipToXY(((int)mousePos.X, (int)mousePos.Y));

            mousePos = new(MathF.Round(mousePos.X), MathF.Round(mousePos.Y));
            Rect(mousePos, new(0.1f, 0.1f), Color.Yellow);

            if (IsKeyPressed(KeyboardKey.P))
                placementMode = !placementMode;

            if (placementMode && IsMouseButtonPressed(MouseButton.Left))
            {
                int snapNode = -1;

                for (int i = 0; i < nodes.Count; i++)
                {
                    if (Vector2.Distance(nodes[i], mousePos) >= 0.1f)
                        continue;

                    snapNode = i;
                    break;
                }

                if (nodes.Count > 0)
                    links.Add((nodes.Count - 1, (snapNode != -1) ? snapNode : nodes.Count));
                if (snapNode == -1)
                    nodes.Add(mousePos);
            }

            BeginDrawing();

            ClearBackground(Color.Black);

            if (!placementMode && nodes.Count > 0)
            {
                data.Clear();

                int lastPath = 0;
                float start = 0;

                for (int i = 0; i < Steps; i++)
                {
                    float theta = 360f / Steps * i + 90;
                    if (theta > 360f)
                        theta -= 360f;

                    List<int> steps = SelectEdges(nodes, links, Count, theta);

                    int path = PackPath(steps);

                    if (path != lastPath)
                    {
                        data.Add((path, start, (360f / Steps * (i - 1) + 90f) % 360f));
                        start = theta;
                    }

                    lastPath = path;

                    for (int j = 0; j < Count; j++)
                    {
                        Vector2 xy = new(i / (float)Steps - 0.5f, j / (float)Count - 0.5f);
                        Rect(xy * 10f, new Vector2(1f / Steps, 1f / Count) * 10, colors[steps[j]]);
                    }

                    if (i == Steps - 1)
                    {
                        data.Add((path, start, 90f));
                    }
                }

                int validCount = 0;

                for (int i = 0; i < data.Count; i++)
                {
                    for (int k = 0; k < Count; k++)
                    {
                        Vector2 newMic = TransformNodesMic(
                            MicPos,
                            nodes,
                            links,
                            UnpackPath(data[i].path),
                            k
                        );
                        float angle = MathF.Atan2(newMic.X, newMic.Y) / MathF.PI * 180f;
                        if (angle < 0f)
                            angle = angle + 360f;

                        bool valid = angle >= data[i].start && angle <= data[i].end;
                        if (valid)
                        {
                            validCount++;
                            Line(newMic, new(0, 0), Color.Red);
                        }
                    }
                }

                System.Console.WriteLine($"Valid: {(validCount / (float)data.Count * 100):f2}%");
            }

            DrawNodes(nodes);
            DrawLinks(
                links,
                nodes,
                -1,
                selectedEdges.Count > 0 ? selectedEdges[selectedEdges.Count - 1] : -1
            );

            if (selectedEdges.Count > 0)
            {
                for (int i = 0; i < selectedEdges.Count; i++)
                {
                    DrawNodes(TransformNodes(nodes, links, selectedEdges, i + 1));
                    DrawLinks(
                        links,
                        TransformNodes(nodes, links, selectedEdges, i + 1),
                        selectedEdges[i],
                        selectedEdges[i]
                    );
                    Rect(
                        RotateVec(
                            TransformNodesMic(MicPos, nodes, links, selectedEdges, i + 1),
                            DoRotation
                        ),
                        new(0.2f, 0.2f),
                        Color.Red
                    );
                }
            }

            if (placementMode && nodes.Count > 0)
                Line(nodes[nodes.Count - 1], mousePos, Color.Yellow);

            foreach (Vector2 node in nodes)
            {
                if (Vector2.Distance(node, mousePos) < 0.1f)
                    Rect(node, new(0.2f, 0.2f), Color.Green);
            }

            Rect(MicPos, new(0.2f, 0.2f), Color.Red);

            Line(
                RotateVec(new(0, -100), -Theta, !DoRotation),
                RotateVec(new(0, 100), -Theta, !DoRotation),
                Color.Green
            );

            rlImGui.Begin();

            ImGui.Begin("Controls");

            ImGui.SliderFloat("Theta", ref Theta, 0f, 360f);
            ImGui.SliderInt("Step Size", ref Steps, 0, 360_0);
            ImGui.SliderFloat("Render Size", ref RenderSize, 0f, 0.2f);
            // ImGui.SliderInt("Count", ref Count, 0, 100);
            ImGui.Checkbox("Point up", ref DoRotation);

            ImGui.BeginMultiSelect(ImGuiMultiSelectFlags.SingleSelect);

            for (int i = 0; i < links.Count; i++)
            {
                if (ImGui.Selectable($"{i}: {links[i].from} --> {links[i].to}"))
                    selectedEdges.Add(i);
            }

            ImGui.EndMultiSelect();

            ImGui.End();

            rlImGui.End();
            EndDrawing();
        }

        CloseWindow();
    }
}
