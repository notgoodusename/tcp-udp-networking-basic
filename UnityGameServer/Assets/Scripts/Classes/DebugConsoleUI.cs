using System.Collections.Generic;

public class DebugConsoleUI
{
    public class Info
    {
        public string message;
        public int color;
        public Info(string _message, int _color)
        {
            message = _message;
            color = _color;
        }
    }
    public static List<Info> messages = new List<Info>();
    public static void Log<T>(T log)
    {
        messages.Add(new Info(log.ToString(), 0));
    }

    public static void LogWarning<T>(T log)
    {
        messages.Add(new Info(log.ToString(), 1));
    }

    public static void LogError<T>(T log)
    {
        messages.Add(new Info(log.ToString(), 2));
    }

    public static void Clear()
    {
        messages.Clear();
    }
}