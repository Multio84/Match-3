
public static class GameFieldProcessing
{
    private static int processingCount;

    public static bool IsProcessing => processingCount > 0;

    public static void StartProcessing()
    {
        processingCount++;
    }

    public static void EndProcessing()
    {
        processingCount--;
        if (processingCount < 0)
            processingCount = 0;
    }
}
