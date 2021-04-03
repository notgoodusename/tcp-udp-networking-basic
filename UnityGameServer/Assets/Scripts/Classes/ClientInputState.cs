using UnityEngine;

public class ClientInputState
{
    public int tick;
    public float lerpAmount;
    public int simulationFrame;

    public int buttons;

    public float HorizontalAxis;
    public float VerticalAxis;
    public Quaternion rotation;
}
public class Button
{
    public static int Jump = 1 << 0;
    public static int Fire1 = 1 << 2;
};