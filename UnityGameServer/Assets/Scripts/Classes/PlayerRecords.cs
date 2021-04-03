using UnityEngine;

public class PlayerRecord
{
    public Vector3 position;
    public Quaternion rotation;
    public int playerTick;
    public PlayerRecord()
    {
        position = new Vector3();
        rotation = new Quaternion();
        playerTick = new int();
    }
    public PlayerRecord(Vector3 _position, Quaternion _rotation, int _playerTick)
    {
        position = _position;
        rotation = _rotation;
        playerTick = _playerTick;
    }
}