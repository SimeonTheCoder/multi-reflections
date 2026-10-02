public class DataUtils
{
    public static int PackPath(List<int> edges)
    {
        int result = 0;

        for (int i = 0; i < 8; i++)
        {
            int curr = i < edges.Count ? edges[i] : 0xF;
            result += curr << (4 * (7 - i));
        }

        return result;
    }

    public static List<int> UnpackPath(int path)
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
}
