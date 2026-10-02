using System.Numerics;

public class GraphUtils
{
    public static Vector2 MirrorNodeAlongAxis(Graph graph, Vector2 vec, int selectedEdge)
    {
        if (selectedEdge == -1)
            return vec;

        Vector2 edgeStart = graph.nodes[graph.links[selectedEdge].from];
        Vector2 edgeEnd = graph.nodes[graph.links[selectedEdge].to];

        float dx = edgeEnd.X - edgeStart.X;
        float dy = edgeEnd.Y - edgeStart.Y;

        float theta = MathF.Atan2(dy, dx);

        float xo = edgeStart.X;
        float yo = edgeStart.Y;

        return MathUtils.DoAxisFlip(vec, theta * 180 / MathF.PI, xo, yo);
    }

    public static Graph MirrorGraphAlongAxis(Graph graph, int selectedEdge)
    {
        if (selectedEdge == -1)
            return graph;

        Vector2 edgeStart = graph.nodes[graph.links[selectedEdge].from];
        Vector2 edgeEnd = graph.nodes[graph.links[selectedEdge].to];

        float dx = edgeEnd.X - edgeStart.X;
        float dy = edgeEnd.Y - edgeStart.Y;

        float theta = MathF.Atan2(dy, dx);

        float xo = edgeStart.X;
        float yo = edgeStart.Y;

        return new()
        {
            nodes = graph
                .nodes.Select(n => MathUtils.DoAxisFlip(n, theta * 180 / MathF.PI, xo, yo))
                .ToList(),
            links = graph.links,
        };
    }

    public static List<int> GeneratePath(Graph graph, int count, float theta)
    {
        Graph currGraph = graph;
        List<int> edges = [currGraph.GetCollisionEdge(theta, -1)];

        for (int i = 0; i < count - 1; i++)
        {
            currGraph = GraphUtils.MirrorGraphAlongAxis(currGraph, edges[i]);
            edges.Add(currGraph.GetCollisionEdge(theta, edges.Count > 0 ? edges[i] : -1));

            if (edges[edges.Count - 1] == -1)
            {
                System.Console.WriteLine("ERR");
                currGraph.GetCollisionEdge(theta, edges[edges.Count - 2]);
            }
        }

        return edges;
    }
}
