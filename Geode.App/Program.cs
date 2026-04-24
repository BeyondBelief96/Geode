using Geode.App.Examples;

namespace Geode.App;

public static class Program
{
    public static void Main(string[] args)
    {
        // Swap this line to switch which example runs.
        using var example = new Chapter03Triangle();
        example.Run();
    }
}
