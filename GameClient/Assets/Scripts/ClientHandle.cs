using System.Net;
using UnityEngine;

public class ClientHandle : MonoBehaviour
{
    public static void Welcome(Packet _packet)
    {
        string _msg = _packet.ReadString();
        int _myId = _packet.ReadInt();
        int _serverTick = _packet.ReadInt();

        GlobalVariables.clientTick = _serverTick;
        GlobalVariables.serverTick = _serverTick;

        Debug.Log($"Message from server: {_msg}");
        Client.instance.myId = _myId;
        ClientSend.WelcomeReceived();

        // Now that we have the client's id, connect UDP
        Client.instance.udp.Connect(((IPEndPoint)Client.instance.tcp.socket.Client.LocalEndPoint).Port);
    }

    public static void SpawnPlayer(Packet _packet)
    {
        int _id = _packet.ReadInt();
        string _username = _packet.ReadString();
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();

        GameManager.instance.SpawnPlayer(_id, _username, _position, _rotation);
    }

    public static void PlayerPosition(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();
        int _serverTick = _packet.ReadInt();

        if (GameManager.players.TryGetValue(_id, out PlayerManager _player))
        {
            if (_serverTick > GlobalVariables.serverTick)
                GlobalVariables.serverTick = _serverTick;

            _player.interpolation.NewUpdate(_serverTick, _position);
        }
    }

    public static void PlayerRotation(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Quaternion _rotation = _packet.ReadQuaternion();
        int _serverTick = _packet.ReadInt();

        if (GameManager.players.TryGetValue(_id, out PlayerManager _player))
        {
            if (_serverTick > GlobalVariables.serverTick)
                GlobalVariables.serverTick = _serverTick;

            _player.interpolation.NewUpdate(_serverTick, _rotation);
        }
    }

    public static void PlayerTransform(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Vector3 _position = _packet.ReadVector3();
        Quaternion _rotation = _packet.ReadQuaternion();
        int _serverTick = _packet.ReadInt();

        if (GameManager.players.TryGetValue(_id, out PlayerManager _player))
        {
            if (_serverTick > GlobalVariables.serverTick)
                GlobalVariables.serverTick = _serverTick;

            _player.interpolation.NewUpdate(_serverTick, _position, _rotation);
        }
    }

    public static void PlayerDisconnected(Packet _packet)
    {
        int _id = _packet.ReadInt();
        Destroy(GameManager.players[_id].gameObject);
        GameManager.players.Remove(_id);
    }

    public static void SimulationState(Packet _packet)
    {
        SimulationState simulationState = new SimulationState();

        simulationState.position = _packet.ReadVector3();
        simulationState.velocity = _packet.ReadVector3();
        simulationState.simulationFrame = _packet.ReadInt();

        if (!GameManager.players[Client.instance.myId].gameObject)
            return;

        GameManager.players[Client.instance.myId].gameObject.GetComponentInChildren<PlayerInput>().OnServerSimulationStateReceived(simulationState);
    }

    public static void ServerConvar(Packet _packet)
    {
        string name = _packet.ReadString();
        float value = _packet.ReadFloat();
        string helpString = _packet.ReadString();
        foreach(Convar i in Convars.list)
        {
            if(i.name == name)
            {
                i.ReceiveResponse(value);
                return;
            }
        }

        // We should have returned, but since the convar doesnt exist in the client
        // we need to create it although the client cant know what it is used for
        // Defaultvalue might be wrong, but it doesnt matter to much
        Convar newConvar = new Convar(name, value, helpString, Flags.NETWORK);
    }

    public static void ServerTick(Packet _packet)
    {
        int _serverTick = _packet.ReadInt();
        if(_serverTick > GlobalVariables.serverTick)
            GlobalVariables.serverTick = _serverTick;
    }
}
