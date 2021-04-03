using UnityEngine;

public class ClientSend : MonoBehaviour
{
    /// <summary>Sends a packet to the server via TCP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendTCPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.tcp.SendData(_packet);
    }

    /// <summary>Sends a packet to the server via UDP.</summary>
    /// <param name="_packet">The packet to send to the sever.</param>
    private static void SendUDPData(Packet _packet)
    {
        _packet.WriteLength();
        Client.instance.udp.SendData(_packet);
    }

    #region Packets
    /// <summary>Lets the server know that the welcome message was received.</summary>
    public static void WelcomeReceived()
    {
        using (Packet _packet = new Packet((int)ClientPackets.welcomeReceived))
        {
            _packet.Write(Client.instance.myId);
            _packet.Write(UIManager.instance.usernameField.text);

            SendTCPData(_packet);
        }
    }

    /// <summary>Sends player inputs to the server.</summary>
    /// <param name="_inputState">Inputs of the player.</param>
    public static void PlayerInput(ClientInputState _inputState)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerInput))
        {
            _packet.Write(_inputState.tick);
            _packet.Write(_inputState.lerpAmount);
            _packet.Write(_inputState.simulationFrame);

            _packet.Write(_inputState.buttons);
            
            _packet.Write(_inputState.HorizontalAxis);
            _packet.Write(_inputState.VerticalAxis);
            _packet.Write(_inputState.rotation);

            SendUDPData(_packet);
        }
    }

    /// <summary>Sends request to change convar.</summary>
    public static void PlayerConvar(Convar i, float _value)
    {
        using (Packet _packet = new Packet((int)ClientPackets.playerConvar))
        {
            _packet.Write(i.name);
            _packet.Write(_value);

            SendTCPData(_packet);
        }
    }


    #endregion
}
