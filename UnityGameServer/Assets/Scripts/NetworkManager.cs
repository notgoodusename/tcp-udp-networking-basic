using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager instance;

    public int tick = 0;

    public GameObject playerPrefab;

    static LogicTimer logicTimer;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Debug.Log("Instance already exists, destroying object!");
            Destroy(this);
        }
    }

    private void Start()
    {
        logicTimer = new LogicTimer(() => FixedTime());
        logicTimer.Start();

        Application.targetFrameRate = Server.tickrate.GetIntValue();
        QualitySettings.vSyncCount = 0;

        Server.Start(50, 26950);
    }

    void Update()
    {
        logicTimer.Update();
    }

    private void FixedTime()
    {
        Application.targetFrameRate = Server.tickrate.GetIntValue();
        if (!Server.isActive)
        {
            tick = 0;
            return;
        }

        ServerTime();
        LagCompensation.UpdatePlayerRecords();
        tick++;
    }

    private void ServerTime()
    {
        for (int i = 1; i <= Server.MaxPlayers; i++)
        {
            if (Server.clients[i] == null || Server.clients[i].player == null)
                continue;

            Server.clients[i].player.tick = tick;
        }
        ServerSend.ServerTick();
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
        logicTimer.Stop();
    }

    public Player InstantiatePlayer()
    {
        return Instantiate(playerPrefab, Vector3.zero, Quaternion.identity).GetComponent<Player>();
    }
}
