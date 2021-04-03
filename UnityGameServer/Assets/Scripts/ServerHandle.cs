using UnityEngine;

public class ServerHandle
{
    public static void WelcomeReceived(int _fromClient, Packet _packet)
    {
        int _clientIdCheck = _packet.ReadInt();
        string _username = _packet.ReadString();

        Debug.Log($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully and is now player {_fromClient}.");
        if (_fromClient != _clientIdCheck)
        {
            Debug.Log($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
        }
        Server.clients[_fromClient].SendIntoGame(_username);
    }

    public static void PlayerInput(int _fromClient, Packet _packet)
    {
        ClientInputState inputState = new ClientInputState();

        inputState.tick = _packet.ReadInt();
        inputState.lerpAmount = _packet.ReadFloat();
        inputState.simulationFrame = _packet.ReadInt();

        inputState.buttons = _packet.ReadInt();

        inputState.HorizontalAxis = _packet.ReadFloat();
        inputState.VerticalAxis = _packet.ReadFloat();
        inputState.rotation = _packet.ReadQuaternion();

        if (!Server.clients[_fromClient].player)
            return;

        Server.clients[_fromClient].player.AddInput(inputState);
    }

    public static void PlayerConvar(int _fromClient, Packet _packet)
    {
        if (!Server.clients[_fromClient].player)
            return;

        string name = _packet.ReadString();
        float requestedValue = _packet.ReadFloat();

        //Check if admin

        foreach (Convar i in Convars.list)
        {
            if (i.name == name)
            {
                i.SetValue(requestedValue);
                return;
            }
        }
    }
}
