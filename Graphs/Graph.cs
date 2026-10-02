using System.Numerics;

public class Graph
{
    public List<Vector2> nodes = new();
    public List<(int from, int to)> links = new();

    public int GetCollisionEdge(float theta, int lastCollision)
    {
        int collisionEdge = -1;
        float lastY = -999;

        for (int i = 0; i < links.Count; i++)
        {
            Vector2 a = MathUtils.RotateVec(nodes[links[i].from], theta);
            Vector2 b = MathUtils.RotateVec(nodes[links[i].to], theta);

            if (b.X < a.X)
            {
                Vector2 temp = new(a.X, a.Y);

                a.X = b.X;
                a.Y = b.Y;

                b.X = temp.X;
                b.Y = temp.Y;
            }

            if (i != lastCollision && a.X <= 0 && b.X >= 0)
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
}
