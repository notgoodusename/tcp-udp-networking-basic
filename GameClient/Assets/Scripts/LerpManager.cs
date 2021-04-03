using UnityEngine;

public class LerpManager : MonoBehaviour
{
    void Update()
    {
        // We dont want to lag behind the real tick by too much,
        // so just teleport to the next tick
        // The cases where this can happen are high ping/low fps
        GlobalVariables.clientTick = Mathf.Clamp(GlobalVariables.clientTick, GlobalVariables.serverTick - 2, GlobalVariables.serverTick);

        // Client (simulated) tick >= Server (real) tick, return
        if (GlobalVariables.clientTick >= GlobalVariables.serverTick)
            return;

        // While lerp amount is or more than 1, we move to the next clientTick and reset the lerp amount
        GlobalVariables.lerpAmount = (GlobalVariables.lerpAmount * Utils.TickInterval() + Time.deltaTime) / Utils.TickInterval();
        while (GlobalVariables.lerpAmount >= 1f)
        {
            // Client (simulated) tick >= Server (real) tick, break
            if (GlobalVariables.clientTick >= GlobalVariables.serverTick)
                break;

            GlobalVariables.lerpAmount = (GlobalVariables.lerpAmount * Utils.TickInterval() - Utils.TickInterval()) / Utils.TickInterval();
            GlobalVariables.lerpAmount = Mathf.Max(0f, GlobalVariables.lerpAmount);
            GlobalVariables.clientTick++;
        }
    }
}
