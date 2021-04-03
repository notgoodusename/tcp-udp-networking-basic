using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Binds
{
    static public List<Bind> list = new List<Bind>();
}

public class Bind
{
    private static int counter = 0;
    public int index;

    public KeyCode key;
    public Action command;
    public string commandString;

    public Bind(string _commandString, KeyCode _key, Action _command)
    {
        key = _key;
        command = _command;
        commandString = _commandString;

        index = counter;
        counter++;

        Binds.list.Add(this);
    }

    public Bind(Command _command, KeyCode _key)
    {
        key = _key;
        command = _command.command;
        commandString = _command.name;

        index = counter;
        counter++;

        Binds.list.Add(this);
    }

    public void ExecuteCommand()
    {
        if (command != null)
            command.Invoke();
    }
}