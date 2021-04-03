using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Convars
{
    static public List<Convar> list = new List<Convar>();
}
public class ConvarRef
{
    public int index;
    public string name;

    public ConvarRef(string _name)
    {
        name = _name;
        foreach (Convar i in Convars.list)
        {
            if (i.name == name)
            {
                index = i.index;
                break;
            }
        }
    }
    public float GetValue()
    {
        return Convars.list.ElementAt(index).value;
    }
}

public class Convar
{
    private static int counter = 0;
    public int index;

    public string name;
    public float defaultValue;
    public string helpString;

    public Flags flags;

    public float value;

    public float minValue;
    public float maxValue;

    private bool wasDeclaredAsInt;

    public Convar(string _name, float _defaultValue, string _helpString, Flags _flags, float _minValue, float _maxValue = float.MaxValue)
    {
        name = _name;
        defaultValue = _defaultValue;
        flags = _flags;
        helpString = _helpString;

        value = _defaultValue;
        minValue = _minValue;
        maxValue = _maxValue;

        value = Mathf.Clamp(value, minValue, maxValue);

        index = counter;
        counter++;

        wasDeclaredAsInt = false;

        Convars.list.Add(this);
    }

    public Convar(string _name, float _defaultValue, string _helpString, Flags _flags)
    {
        name = _name;
        defaultValue = _defaultValue;
        flags = _flags;
        helpString = _helpString;

        value = _defaultValue;
        minValue = float.MinValue;
        maxValue = float.MaxValue;

        value = Mathf.Clamp(value, minValue, maxValue);

        index = counter;
        counter++;

        wasDeclaredAsInt = false;

        Convars.list.Add(this);
    }

    public Convar(string _name, int _defaultValue, string _helpString, Flags _flags, int _minValue, int _maxValue = int.MaxValue)
    {
        name = _name;
        defaultValue = _defaultValue;
        flags = _flags;
        helpString = _helpString;

        value = _defaultValue;
        minValue = _minValue;
        maxValue = _maxValue;

        value = Mathf.Clamp(value, minValue, maxValue);

        index = counter;
        counter++;

        wasDeclaredAsInt = true;

        Convars.list.Add(this);
    }

    public Convar(string _name, int _defaultValue, string _helpString, Flags _flags)
    {
        name = _name;
        defaultValue = _defaultValue;
        flags = _flags;
        helpString = _helpString;

        value = _defaultValue;
        minValue = int.MinValue;
        maxValue = int.MaxValue;

        value = Mathf.Clamp(value, minValue, maxValue);

        index = counter;
        counter++;

        wasDeclaredAsInt = true;

        Convars.list.Add(this);
    }

    public void SetValue(float _value)
    {
        if (!wasDeclaredAsInt)
        {
            if (value == _value)
                return;

            if ((flags & Flags.NETWORK) == Flags.NETWORK)
            {
                SendRequest(_value);
                return;
            }

            value = _value;
            if (minValue == float.MinValue && maxValue != float.MaxValue)
                value = Mathf.Min(value, maxValue);

            if (maxValue == float.MaxValue && minValue != float.MinValue)
                value = Mathf.Max(value, minValue);

            if (maxValue != float.MaxValue && minValue != float.MinValue)
                value = Mathf.Clamp(value, minValue, maxValue);

            Convars.list.ElementAt(index).value = value;
        }
        else
        {
            if (value == (int)_value)
                return;

            if ((flags & Flags.NETWORK) == Flags.NETWORK)
            {
                SendRequest((int)_value);
                return;
            }

            value = (int)_value;
            if (minValue == int.MinValue && maxValue != int.MaxValue)
                value = Mathf.Min(value, maxValue);

            if (maxValue == int.MaxValue && minValue != int.MinValue)
                value = Mathf.Max(value, minValue);

            if (maxValue != int.MaxValue && minValue != int.MinValue)
                value = Mathf.Clamp(value, minValue, maxValue);

            Convars.list.ElementAt(index).value = (int)value;
        }
    }
    public int GetIntValue()
    {
        return (int)Convars.list.ElementAt(index).value;
    }

    public float GetValue()
    {
        return Convars.list.ElementAt(index).value;
    }

    public void SetFlags(Flags _flags)
    {
        flags = _flags;

        Convars.list.ElementAt(Convars.list.IndexOf(this)).flags = flags;
    }

    public void SendRequest(float _value)
    {
        if (!Client.instance.isConnected)
            return;

        ClientSend.PlayerConvar(this, _value);
    }

    public void ReceiveResponse(float _value)
    {
        if (value == _value)
            return;

        value = _value;

        Convars.list.ElementAt(index).value = value;
    }

}

public enum Flags
{
    NONE = 1 << 0,
    CLIENT = 1 << 1,
    NETWORK = 1 << 2,
};
