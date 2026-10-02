using System.Numerics;
using ImGuiNET;
using Raylib_cs;
using rlImGui_cs;
using Utils;
using static Raylib_cs.Raylib;
using static Utils.RenderingUtils;

public class Program
{
    static int Steps = 720;
    static float RenderSize = 0.1f;
    static bool DoRotation = false;
    static int Count = 8;

    static Vector2 MicPos = new(0f, 0f);

    static Vector2 TransformNodesMic(Vector2 mic, Graph graph, List<int> transforms, int count)
    {
        Vector2 copy = mic;
        Graph graphCopy = graph;

        for (int i = 0; i < count; i++)
        {
            graphCopy = GraphUtils.MirrorGraphAlongAxis(graphCopy, transforms[i]);
            copy = GraphUtils.MirrorNodeAlongAxis(graphCopy, copy, transforms[i]);
        }

        return copy;
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

        Graph graph = new();

        List<(int path, float start, float end)> data = new();

        while (!WindowShouldClose())
        {
            if (!placementMode && graph.nodes.Count > 0)
                selectedEdges = GraphUtils.GeneratePath(graph, Count, Theta);

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

                for (int i = 0; i < graph.nodes.Count; i++)
                {
                    if (Vector2.Distance(graph.nodes[i], mousePos) >= 0.1f)
                        continue;

                    snapNode = i;
                    break;
                }

                if (graph.nodes.Count > 0)
                    graph.links.Add(
                        (graph.nodes.Count - 1, (snapNode != -1) ? snapNode : graph.nodes.Count)
                    );
                if (snapNode == -1)
                    graph.nodes.Add(mousePos);
            }

            BeginDrawing();

            ClearBackground(Color.Black);

            if (!placementMode && graph.nodes.Count > 0)
            {
                data.Clear();

                int lastPath = 0;
                float start = 0;

                for (int i = 0; i < Steps; i++)
                {
                    float theta = 360f / Steps * i + 90;
                    if (theta > 360f)
                        theta -= 360f;

                    List<int> steps = GraphUtils.GeneratePath(graph, Count, theta);

                    int path = DataUtils.PackPath(steps);

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
                            graph,
                            DataUtils.UnpackPath(data[i].path),
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

            DrawGraph(graph, selectedEdges.Count > 0 ? selectedEdges[selectedEdges.Count - 1] : -1);

            if (selectedEdges.Count > 0)
            {
                for (int i = 0; i < selectedEdges.Count; i++)
                {
                    DrawGraph(graph, selectedEdges[i]);

                    Rect(
                        MathUtils.RotateVec(
                            TransformNodesMic(MicPos, graph, selectedEdges, i + 1),
                            Theta,
                            DoRotation
                        ),
                        new(0.2f, 0.2f),
                        Color.Red
                    );
                }
            }

            if (placementMode && graph.nodes.Count > 0)
                Line(graph.nodes[graph.nodes.Count - 1], mousePos, Color.Yellow);

            foreach (Vector2 node in graph.nodes)
            {
                if (Vector2.Distance(node, mousePos) < 0.1f)
                    Rect(node, new(0.2f, 0.2f), Color.Green);
            }

            Rect(MicPos, new(0.2f, 0.2f), Color.Red);

            Line(
                MathUtils.RotateVec(new(0, -100), -Theta, !DoRotation),
                MathUtils.RotateVec(new(0, 100), -Theta, !DoRotation),
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

            for (int i = 0; i < graph.links.Count; i++)
            {
                if (ImGui.Selectable($"{i}: {graph.links[i].from} --> {graph.links[i].to}"))
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
