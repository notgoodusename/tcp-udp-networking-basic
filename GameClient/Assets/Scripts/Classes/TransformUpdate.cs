using UnityEngine;

class TransformUpdate
{
    public static TransformUpdate zero = new TransformUpdate(0, 0, 0, Vector3.zero, Vector3.zero, Quaternion.identity, Quaternion.identity);

    public int tick;

    public float time;
    public float lastTime;
    public Vector3 position;
    public Vector3 lastPosition;
    public Quaternion rotation;
    public Quaternion lastRotation;

    internal TransformUpdate(int _tick, float _time, float _lastTime, Vector3 _position, Vector3 _lastPosition)
    {
        tick = _tick;
        time = _time;
        lastTime = _lastTime;
        position = _position;
        rotation = Quaternion.identity;
        lastPosition = _lastPosition;
    }

    internal TransformUpdate(int _tick, float _time, float _lastTime, Quaternion _rotation, Quaternion _lastRotation)
    {
        tick = _tick;
        time = _time;
        lastTime = _lastTime;
        position = Vector3.zero;
        rotation = _rotation;
        lastRotation = _lastRotation;
    }

    internal TransformUpdate(int _tick, float _time, float _lastTime, Vector3 _position, Vector3 _lastPosition, Quaternion _rotation, Quaternion _lastRotation)
    {
        tick = _tick;
        time = _time;
        lastTime = _lastTime;
        position = _position;
        rotation = _rotation;
        lastPosition = _lastPosition;
        lastRotation = _lastRotation;
    }
}
