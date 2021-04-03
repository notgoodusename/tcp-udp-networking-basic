using UnityEngine;

public class SimulationState
{
    public Vector3 position;
    public Vector3 velocity;
    public int simulationFrame;
    public static SimulationState CurrentSimulationState(ClientInputState inputState, PlayerInput player)
    {
        return new SimulationState
        {
            position = player.transform.position,
            velocity = player.velocity,
            simulationFrame = inputState.simulationFrame,
        };
    }
}