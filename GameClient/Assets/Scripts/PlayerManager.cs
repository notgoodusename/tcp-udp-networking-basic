using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public int id;
    public string username;
    public Interpolation interpolation;

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
    }
}
