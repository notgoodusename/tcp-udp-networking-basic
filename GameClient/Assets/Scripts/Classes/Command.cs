using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

public class Commands
{
    static public List<Command> list = new List<Command>();
}
public class Command
{
    private static int counter = 0;
    public int index;

    public string name;
    public string helpString;

    public Flags flags;

    public Action command;
    public Action<string[]> commandEntry;

    public Command(string _name, string _helpString, Flags _flags, Action _command)
    {
        name = _name;
        helpString = _helpString;

        flags = _flags;

        command = _command;

        index = counter;
        counter++;

        Commands.list.Add(this);
    }
    public Command(string _name, string _helpString, Flags _flags, Action<string[]> _commandEntry)
    {
        name = _name;
        helpString = _helpString;

        flags = _flags;

        commandEntry = _commandEntry;

        index = counter;
        counter++;

        Commands.list.Add(this);
    }

    public void ExecuteCommand()
    {
        if (command != null)
            command.Invoke();
    }

    public void ExecuteEntryCommand(string[] strings)
    {
        if (commandEntry != null)
            commandEntry.Invoke(strings);
    }

    public static bool IsNumber(string[] strings)
    {
        foreach (string i in strings)
        {
            if (!(float.TryParse(i, out _)))
                return false;
        }
        return true;
    }

    public static bool IsNumber(string strings)
    {
        if (strings == String.Empty)
            return false;
        if (!(float.TryParse(strings, out _)))
            return false;
        return true;
    }
}