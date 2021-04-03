public class ServerSend
{
    /// <summary>Sends a packet to a client via TCP.</summary>
    /// <param name="_toClient">The client to send the packet the packet to.</param>
    /// <param name="_packet">The packet to send to the client.</param>
    private static void SendTCPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        // Sanity check
        if (Server.clients[_toClient].tcp != null)
            Server.clients[_toClient].tcp.SendData(_packet);
    }

    /// <summary>Sends a packet to a client via UDP.</summary>
    /// <param name="_toClient">The client to send the packet the packet to.</param>
    /// <param name="_packet">The packet to send to the client.</param>
    private static void SendUDPData(int _toClient, Packet _packet)
    {
        _packet.WriteLength();
        // Sanity check
        if(Server.clients[_toClient].udp.endPoint != null)
            Server.clients[_toClient].udp.SendData(_packet);
    }

    /// <summary>Sends a packet to all clients via TCP.</summary>
    /// <param name="_packet">The packet to send.</param>
    private static void SendTCPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].tcp.SendData(_packet);
        }
    }
    /// <summary>Sends a packet to all clients except one via TCP.</summary>
    /// <param name="_exceptClient">The client to NOT send the data to.</param>
    /// <param name="_packet">The packet to send.</param>
    private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }
    }

    /// <summary>Sends a packet to all clients via UDP.</summary>
    /// <param name="_packet">The packet to send.</param>
    private static void SendUDPDataToAll(Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            Server.clients[i].udp.SendData(_packet);
        }
    }
    /// <summary>Sends a packet to all clients except one via UDP.</summary>
    /// <param name="_exceptClient">The client to NOT send the data to.</param>
    /// <param name="_packet">The packet to send.</param>
    private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
    {
        _packet.WriteLength();
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (i != _exceptClient)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }
    }

    #region Packets
    /// <summary>Sends a welcome message to the given client.</summary>
    /// <param name="_toClient">The client to send the packet to.</param>
    /// <param name="_msg">The message to send.</param>
    /// <param name="_serverTick">The tick of the server to initialize the client clock.</param>
    public static void Welcome(int _toClient, string _msg, int _serverTick)
    {
        using (Packet _packet = new Packet((int)ServerPackets.welcome))
        {
            _packet.Write(_msg);
            _packet.Write(_toClient);
            _packet.Write(_serverTick);

            SendTCPData(_toClient, _packet);
        }
    }

    /// <summary>Tells a client to spawn a player.</summary>
    /// <param name="_toClient">The client that should spawn the player.</param>
    /// <param name="_player">The player to spawn.</param>
    public static void SpawnPlayer(int _toClient, Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.username);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);

            SendTCPData(_toClient, _packet);
        }
    }

    /// <summary>Sends a player's updated position to all clients.</summary>
    /// <param name="_player">The player whose position to update.</param>
    public static void PlayerPosition(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.tick);

            SendUDPDataToAll(_player.id , _packet);
        }
    }

    /// <summary>Sends a player's updated rotation to all clients except to himself (to avoid overwriting the local player's rotation).</summary>
    /// <param name="_player">The player whose rotation to update.</param>
    public static void PlayerRotation(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.rotation);
            _packet.Write(_player.tick);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    /// <summary>Sends a player's updated position and rotation to all clients except to the client himself (to avoid overwriting the player's simulation state).</summary>
    /// <param name="_player">The player whose position and rotation to update.</param>
    public static void PlayerTransform(Player _player)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerTransform))
        {
            _packet.Write(_player.id);
            _packet.Write(_player.transform.position);
            _packet.Write(_player.transform.rotation);
            _packet.Write(_player.tick);

            SendUDPDataToAll(_player.id, _packet);
        }
    }

    /// <summary>Sends a player's disconnection.</summary>
    /// <param name="_playerId">The id of the player who disconnects.</param>
    public static void PlayerDisconnected(int _playerId)
    {
        using (Packet _packet = new Packet((int)ServerPackets.playerDisconnected))
        {
            _packet.Write(_playerId);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>Sends a player's simulation state.</summary>
    /// <param name="_toClient">The client that should receive the simulation state.</param>
    /// <param name="_simulationState">The simulation state to send.</param>
    public static void SendSimulationState(int _toClient, SimulationState _simulationState)
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverSimulationState))
        {
            _packet.Write(_simulationState.position);
            _packet.Write(_simulationState.velocity);
            _packet.Write(_simulationState.simulationFrame);

            SendUDPData(_toClient, _packet);
        }
    }

    /// <summary>Sends a convar state.</summary>
    /// <param name="i">The convar to send.</param>
    public static void SendConvar(Convar i)
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverConvar))
        {
            _packet.Write(i.name);
            _packet.Write(i.value);
            _packet.Write(i.helpString);

            SendTCPDataToAll(_packet);
        }
    }

    /// <summary>Sends current server tick.</summary>
    public static void ServerTick()
    {
        using (Packet _packet = new Packet((int)ServerPackets.serverTick))
        {
            _packet.Write(NetworkManager.instance.tick);

            SendTCPDataToAll(_packet);
        }
    }

    #endregion
}
