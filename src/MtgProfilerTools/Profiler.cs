using System;
using System.Collections.Generic;

public class Profiler
{
    public static void Enter(string name)
    {
        Console.WriteLine(name);
    }

    public static void Exit()
    {
        Console.WriteLine("Exit");
    }
}
