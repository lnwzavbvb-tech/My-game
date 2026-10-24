using System;

internal static class Program
{
    static int Main()
    {
        try { ArenaValidation.Run(); Console.WriteLine("ARENA_VALIDATION_PASSED"); return 0; }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }
}
