using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleUI : MonoBehaviour
{
    public ScrollRect scrollLog;
    public InputField typeField;
    public GameObject textSample;
    public GameObject holder;

    static Command help;
    static Command showallcommands;
    static Command find;
    static Command clear;

    static Command exec;

    static Command bind;
    static Command unbind;
    static Command unbindall;
    static Command key_listboundkeys;
    static Command key_findbinding;

    static Command toggleconsole;

    static Bind ConsoleKey;

    private bool isOpen = false;
    private int currentInput = 0;
    private static List<string> input = new List<string>();

    public void Awake()
    {
        help = new Command("help", "Shows help about commands/convars", Flags.NONE, (string[] a) =>
        {
            string fullCommand = String.Empty;
            for (int i = 0; i < a.Length; i++)
            {
                fullCommand += a[i] + " ";
            }
            helpCommand(fullCommand);
        });

        showallcommands = new Command("showall", "Shows all commands/convars", Flags.NONE, () =>
        {
            showAllCommands();
        });

        find = new Command("find", "Searches all commands/convars", Flags.NONE, (string[] a) =>
        {
            searchAllConsoleCommandsList(a[0]);
        });

        clear = new Command("clear", "Clears console", Flags.NONE, () =>
        {
            clearConsoleCommands();
        });

        toggleconsole = new Command("toggleconsole", "Toggles console", Flags.NONE, () =>
        {
            if (!Application.isEditor)
                return;

            isOpen = !isOpen;
            // Move to the bottom of the console
            Canvas.ForceUpdateCanvases();
            scrollLog.verticalScrollbar.value = 0f;
            Canvas.ForceUpdateCanvases();
        });

        exec = new Command("exec", "Executes config file", Flags.NONE, (string[] a) =>
        {
            string command = String.Empty;
            for (int i = 0; i < a.Length; i++)
            {
                command += a[i] + " ";
            }
            loadTxtFile(command.TrimEnd());
        });

        bind = new Command("bind", "Binds command to key", Flags.NONE, (string[] a) =>
        {
            string command = String.Empty;
            for(int i = 1; i < a.Length; i++)
            {
                command += a[i] + " ";
            }
            bindKey(a[0], command.TrimEnd());
        });

        unbind = new Command("unbind", "Unbinds key", Flags.NONE, (string[] a) =>
        {
            unbindKey(a[0].ToUpper());
        });

        unbindall = new Command("unbindall", "Unbinds all keys", Flags.NONE, () =>
        {
            unbindAll();
        });

        key_listboundkeys = new Command("key_listboundkeys", "Shows all bound keys", Flags.NONE, () =>
        {
            showAllBinds();
        });

        key_findbinding = new Command("key_findbinding", "Finds command in binds", Flags.NONE, (string[] a) =>
        {
            string fullCommand = String.Empty;
            for (int i = 0; i < a.Length; i++)
            {
                fullCommand += a[i] + " ";
            }
            findBindCommand(fullCommand.TrimEnd());
        });

        ConsoleKey = new Bind(toggleconsole, KeyCode.F12);
    }

    private void Start()
    {
        // Creates a txt file and it reads that file to process it as console commands,
        // if it already exists it wont create a new txt
        createTxtFile("autoexec");
        loadTxtFile("autoexec");
    }

    // Returns current state of the console
    public bool isActive()
    {
        return isOpen;
    }

    // Creates txt file with the corresponding name
    void createTxtFile(string _name)
    {
        string name = "/" + _name;
        if (!name.Contains(".txt"))
            name += ".txt";
        string path = Application.dataPath + name;
        if (!File.Exists(path))
            File.WriteAllText(path, String.Empty);
    }

    // Loads all txt lines and executes them as a console command
    void loadTxtFile(string _name)
    {
        string name = "/" + _name;
        if (!name.Contains(".txt"))
            name += ".txt";
        string path = Application.dataPath + name;
        if (File.Exists(path))
        {
            foreach (string i in File.ReadAllLines(path))
            {
                ProcessText(i);
            }
        }
    }

    // Shows help about the given command
    void helpCommand(string command)
    {
        string finalText = CommandGetHelp(command.TrimEnd());
        if (finalText == String.Empty) // String is empty, no command could be found return
        {
            AddText("Couldnt find any help about \"" + command.TrimEnd() + "\"", 1);
            return;
        }
        AddText(finalText);
    }

    // Shows all convars/commands in an unorganized order
    void showAllCommands()
    {
        foreach (Convar i in Convars.list)
        {
            AddText(i.name);
        }
        foreach (Command i in Commands.list)
        {
            AddText(i.name);
        }
    }

    // Searches for given string in all convars/commands
    // and shows in an unorganized order
    void searchAllConsoleCommandsList(string search)
    {
        foreach (Convar i in Convars.list)
        {
            if (i.name.Contains(search))
            {
                AddText(ConvarGetInfo(i));
            }
        }
        foreach (Command i in Commands.list)
        {
            if (i.name.Contains(search))
            {
                AddText(CommandGetInfo(i));
            }
        }
    }

    // Deletes all current text in console
    void clearConsoleCommands()
    {
        for (int i = 0; i < textSample.transform.parent.childCount; i++)
        {
            GameObject go = textSample.transform.parent.GetChild(i).gameObject;
            if (go == null)
                continue;

            if (go == textSample)
                continue;

            Destroy(go);
        }
        AddText("clear");
    }

    // Binds given key to given command
    void bindKey(string key, string command)
    {
        string _key = key.ToUpper();
        if(float.TryParse(key, out _)) // If it is a number, the we need to use Alpha0-9
            _key = "Alpha" + key;

        if (System.Enum.TryParse(_key, out KeyCode _) == false) // Key doesnt exist, return
        {
            AddText("Key couldnt be found", 1);
            return;
        }

        KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), _key);
        if (keyCode.ToString().TrimStart() == String.Empty) // Key is empty, return
        {
            AddText("Key couldnt be found", 1);
            return;
        }

        int index = -1;
        
        // Search if it already exists
        foreach (Bind i in Binds.list)
        {
            if(i.key == keyCode)
            {
                index = Binds.list.IndexOf(i); // Found existing key, exit the foreach loop
                break;
            }
        }

        if(index != -1) // The given key already exists
        {
            if (command == String.Empty) // If the command is empty, it is asking for what command is bound to
            {
                if (Binds.list.ElementAt(index).commandString == String.Empty) // If the asked command is empty, then it is bound to nothing
                {
                    AddText("Key isnt binded", 1); // Return that it isnt bound
                    return;
                }
                // Else return the command it is bound to
                AddText("bind \"" + key + "\" \"" + Binds.list.ElementAt(index).commandString + "\"");
                return;
            }
            // Replace command with the new given command and return
            Binds.list.ElementAt(index).command = () => ProcessText(command, false);
            Binds.list.ElementAt(index).commandString = command;
            return;
        }

        // If the command is empty, it is asking for what command is bound to
        // but the key doesnt exist, so it cant be bound to anything, return
        if (command == String.Empty && index == -1)
        {
            AddText("Key isnt binded", 1);
            return;
        }
        
        // Create new bind since key doesnt exist
        Bind newBind = new Bind(command, keyCode, () => ProcessText(command, false));
    }

    // Deletes command from given key
    void unbindKey(string key)
    {
        string _key = key;
        if (float.TryParse(key, out _)) // If it is a number, the we need to use Alpha0-9
            _key = "Alpha" + key;

        if (System.Enum.TryParse(_key, out KeyCode _) == false) // Key doesnt exist, return
        {
            AddText("Key couldnt be found", 1);
            return;
        }

        KeyCode keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), _key);
        if (keyCode.ToString().TrimStart() == String.Empty) // Key is empty, return
        {
            AddText("Key couldnt be found", 1);
            return;
        }

        foreach (Bind i in Binds.list)
        {
            if (i.key == keyCode) // We found the key, delete the command and return
            {
                i.command = null;
                i.commandString = String.Empty;
                return;
            }
        }
    }

    // Deletes all binds commands
    void unbindAll()
    {
        foreach (Bind i in Binds.list)
        {
            i.command = null;
            i.commandString = String.Empty;
            continue;
        }
    }

    // Shows all existing binds
    void showAllBinds()
    {
        foreach (Bind i in Binds.list)
        {
            if (i.commandString == String.Empty) // Key is bound, but doesnt contain a usable command
                continue;

            string key = i.key.ToString().ToLower();
            key.Replace("alpha", String.Empty); // If it is a number show it like so
            AddText("\"" + key + "\" = \"" + i.commandString + "\"");
        }
    }

    // Finds binds that are have the given command,
    // or contain part of the given string
    void findBindCommand(string command)
    {
        foreach (Bind i in Binds.list)
        {
            if(i.commandString.Contains(command))
            {
                string key = i.key.ToString().ToLower();
                key.Replace("alpha", String.Empty); // If it is a number show it like so
                AddText("\"" + key + "\" = \"" + i.commandString + "\"");
            }
        }
    }

    private void Update()
    {
        // Check if we the are new messages
        foreach(DebugConsoleUI.Info i in DebugConsoleUI.messages)
        {
            // Add received messages, with the respective color
            AddText(i.message, i.color);
        }
        // Clear messages, since all of them arent needed
        DebugConsoleUI.Clear();


        // Check if we are pressing a binded key
        for (int i = 0; i < Binds.list.Count; i++)
        {
            if (Binds.list.ElementAt(i) == null)
                continue;

            if(Input.GetKeyDown(Binds.list.ElementAt(i).key))
                Binds.list.ElementAt(i).ExecuteCommand();
        }

        if(!Application.isEditor)
        {
            string input = Console.ReadLine();
            if (input != "")
                ProcessText(input);
            return;
        }

        // If the console isnt equal to the state of it
        // then set it
        if (holder.activeSelf != isOpen)
            holder.SetActive(isOpen);

        if (isOpen)
        {
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;

            // Input is a list of all the given inputs to the console, via the typeField
            if (input.Count >= 1) // If inputs exists then
            {
                // if we press arrowup or arrowdown, we should be able to go through them
                // It is useful when repeating commands, instead of typing them over and over

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    currentInput++;
                    if (currentInput >= input.Count)
                        currentInput = 0;
                    typeField.text = input.ElementAt(currentInput);
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    currentInput--;
                    if (currentInput < 0)
                        currentInput = input.Count - 1;
                    typeField.text = input.ElementAt(currentInput);
                }
            }

            if (Input.GetKeyDown(KeyCode.Return)) // We just submitted a command
            {
                // Keep the inputField active, instead of deselectting it
                typeField.ActivateInputField();

                // Save text and rest the typeField
                string text = typeField.text;
                typeField.text = String.Empty;

                if (text.TrimStart() != String.Empty) // Check if it isnt empty
                {
                    // Reset current input, add the given input and process the text
                    currentInput = 0;
                    input.Add(text);
                    ProcessText(text);

                    // Move to the bottom of the console
                    Canvas.ForceUpdateCanvases();
                    scrollLog.verticalScrollbar.value = 0f;
                    Canvas.ForceUpdateCanvases();
                }
            }
        }

        // It isnt open so we dont want to type on it, deactive it
        if(!isOpen)
        {
            typeField.DeactivateInputField();
        }
    }


    void ProcessText(string _message, bool showResponse = true, bool multipleCommands = false)
    {
        if (_message == String.Empty) // Cant process what is empty, return
            return;

        // Remove useless empty spaces at the start, end, remove the quotation marks and replace multiple multiple whitespaces with one space
        string message = _message.TrimStart().TrimEnd().Replace("\"", string.Empty);
        message = Regex.Replace(message, " {2,}", " ");
        if (message == String.Empty) // Cant process what is empty, return
            return;

        // Split the message into different strings
        // the first string of the split message would be the command/convar
        // the rest are the requested parameters for each command/convar
        string[] splitedSpaces = message.Split(' ');
        if (splitedSpaces[0] == String.Empty) // Cant process what is empty, return
            return;

        string finalText = " ";
        string response = "] " + _message.TrimStart().TrimEnd();

        bool isBindingKey = bind.name == splitedSpaces[0];

        // It checks if you want to show a response
        if (showResponse)
            AddText(response);

        // When the string contains a ; the it splits the line to different commands
        // The check for binding a key is because, when binding we want to be able to bind
        // multiple commands to a single key
        if (!multipleCommands && !(isBindingKey && splitedSpaces.Length >= 3))
        {
            // Split text and execute as separte commands
            string[] splitedCommands = message.Split(';');
            if (splitedCommands.Length > 1)
            {
                for (int i = 0; i < splitedCommands.Length; i++)
                {
                    ProcessText(splitedCommands[i], false, true);
                }
                return;
            }
        }

        // Look for the convar name, and check if it equals the given command/convar
        foreach (Convar i in Convars.list)
        {
            if(i.name == splitedSpaces[0]) // Found the convar
            {
                // The message only contains the convar and not the value, it its asking for what it does
                if (splitedSpaces.Length == 1 || splitedSpaces[1] == String.Empty)
                {
                    finalText = ConvarGetInfo(i);
                    break;
                }

                // Check if we can pass the given value to a float
                if (float.TryParse(splitedSpaces[1], out _))
                    i.SetValue(float.Parse(splitedSpaces[1]));
                finalText = String.Empty;
                break;
            }
        }
        
        if (finalText == " ") // Didnt find a convar, now search for the commands
        {
            // Look for the command name, and check if it equals the given command/convar
            foreach (Command i in Commands.list)
            {
                if (i.name == splitedSpaces[0])
                {
                    finalText = String.Empty;
                    switch (splitedSpaces.Length)
                    {
                        case 1:
                            // The message only contains the command, it could be that it is asking for help
                            // or executing a command
                            if (i.command != null)
                                i.ExecuteCommand();
                            else if (i.commandEntry != null)
                                AddText(CommandGetInfo(i));
                            else
                                finalText = CommandGetInfo(i);
                            break;
                        default:
                            if (splitedSpaces.Length < 2)
                                break;

                            // Create an array, which contains every single paramater given
                            string[] stringArray = new string[splitedSpaces.Length];
                            for (int j = 1; j < stringArray.Length; j++)
                            {
                                stringArray[j - 1] = splitedSpaces[j];
                            }

                            i.ExecuteEntryCommand(stringArray);
                            break;
                    }
                    break;
                }
            }
        }
        
        // Couldnt find convar/command, so its unknown
        if (finalText == " ")
            finalText = "Unknown command \"" + splitedSpaces[0] + "\"";

        // Add text to console
        if (finalText != String.Empty)
            AddText(finalText);
    }

    // Searches for all the variables with the given string, and returns the proper info of the command/convar
    string CommandGetHelp(string i)
    {
        string finalText = String.Empty;
        int[] index = new int[2] { -1, -1 };
        foreach (Command j in Commands.list)
        {
            if (j.name == i)
            {
                index[0] = j.index;
                break;
            }
        }
        if (index[0] == -1)
        {
            foreach (Convar j in Convars.list)
            {
                if (j.name == i)
                {
                    index[1] = j.index;
                    break;
                }
            }
        }
        if (index[0] != -1)
            finalText = CommandGetInfo(Commands.list.ElementAt(index[0]));
        else if (index[1] != -1)
            finalText = ConvarGetInfo(Convars.list.ElementAt(index[1]));
        return finalText;
    }

    // Returns a string which contains all the info of the given command in a certain manner
    string CommandGetInfo(Command i)
    {
        string finalText = String.Empty;
        finalText = "\"" + i.name + "\" - " + i.helpString;
        return finalText;
    }

    // Returns a string which contains all the info of the given convar in a certain manner
    string ConvarGetInfo(Convar i)
    {
        string finalText = String.Empty;
        finalText = "\"" + i.name + "\" = \"" + i.value + "\"";

        if (i.value != i.defaultValue)
            finalText += " (def. \"" + i.defaultValue + "\")";

        switch (i.minValue)
        {
            case float.MinValue:
                break;
            case int.MinValue:
                break;
            default:
                finalText += " min. " + i.minValue;
                break;
        }

        switch (i.maxValue)
        {
            case float.MaxValue:
                break;
            case int.MaxValue:
                break;
            default:
                finalText += " max. " + i.maxValue;
                break;
        }

        string flags = "";

        if ((i.flags & Flags.SERVER) == Flags.SERVER)
            flags = " server side";
        if ((i.flags & Flags.NETWORK) == Flags.NETWORK)
            flags = " networked";

        if (flags != String.Empty)
            finalText += flags;

        if (i.helpString != String.Empty)
            finalText += " - " + i.helpString;

        return finalText;
    }

    // Adds text to console, and takes color to modify the text color
    public void AddText(string _string, int color = 0)
    {
        if (Application.isEditor)
        {
            GameObject text = Instantiate(textSample);
            text.transform.SetParent(textSample.transform.parent);
            text.GetComponent<Text>().text = _string;
            switch (color)
            {
                case 0:
                    text.GetComponent<Text>().color = Color.white;
                    break;
                case 1:
                    text.GetComponent<Text>().color = Color.yellow;
                    break;
                case 2:
                    text.GetComponent<Text>().color = Color.red;
                    break;
            }
            text.SetActive(true);
        }
        else
        {
            Console.BackgroundColor = ConsoleColor.Black;
            switch (color)
            {
                case 0:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case 1:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case 2:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
            }
            Console.WriteLine(_string);
            Console.ResetColor();
        }
    }
}