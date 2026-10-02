using System.Numerics;
using Graphs;
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
    static int Count = 16;

    static Vector2 MicPos = new(0f, 0f);

    static void Main(string[] args)
    {
        bool placementMode = false;
        GraphPath selectedPath = new();

        InitWindow(1920, 1080, "Test");
        SetTargetFPS(120);

        rlImGui.Setup(true);

        Graph graph = new();

        while (!WindowShouldClose())
        {
            if (!placementMode && graph.nodes.Count > 0)
                selectedPath = Graph.GeneratePath(graph, Count, Theta);

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
                    graph.edges.Add(
                        (graph.nodes.Count - 1, (snapNode != -1) ? snapNode : graph.nodes.Count)
                    );
                if (snapNode == -1)
                    graph.nodes.Add(mousePos);
            }

            BeginDrawing();

            ClearBackground(Color.Black);

            if (!placementMode && graph.nodes.Count > 0)
            {
                graph.GenerateData(Steps, Count);
                int validCount = 0;

                for (int i = 0; i < graph.data.Count; i++)
                {
                    for (int k = 0; k < Count; k++)
                    {
                        bool valid = graph.IsValidPath(i, MicPos, k);

                        Vector2 newMic = graph.TransformNodeWithGraph(
                            MicPos,
                            graph.data[i],
                            k
                        );

                        if (valid)
                        {
                            validCount++;
                            Line(newMic, new(0, 0), Color.Red);
                        }
                    }
                }

                System.Console.WriteLine($"Valid: {(validCount / (float) graph.data.Count * 100):f2}%");
            }

            DrawGraph(graph, selectedPath.Length > 0 ? selectedPath.edges[selectedPath.Length - 1] : -1);

            if (selectedPath.Length > 0)
            {
                for (int i = 0; i < selectedPath.Length; i++)
                {
                    DrawGraph(graph, selectedPath.edges[i]);

                    Rect(
                        MathUtils.RotateVec(
                            graph.TransformNodeWithGraph(MicPos, selectedPath, i + 1),
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
            ImGui.SliderInt("Count", ref Count, 0, 100);
            ImGui.Checkbox("Point up", ref DoRotation);

            ImGui.BeginMultiSelect(ImGuiMultiSelectFlags.SingleSelect);

            for (int i = 0; i < graph.edges.Count; i++)
            {
                if (ImGui.Selectable($"{i}: {graph.edges[i].from} --> {graph.edges[i].to}"))
                    selectedPath.edges.Add(i);
            }

            ImGui.EndMultiSelect();

            ImGui.End();

            rlImGui.End();
            EndDrawing();
        }

        CloseWindow();
    }
}
