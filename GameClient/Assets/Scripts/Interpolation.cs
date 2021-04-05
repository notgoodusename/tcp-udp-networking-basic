using System.Collections.Generic;
using UnityEngine;

public class Interpolation : MonoBehaviour
{
    [SerializeField] public InterpolationMode mode;

    static public Convar interpolation = new Convar("interp", 0.1f, "Visual delay for received updates", Flags.CLIENT, 0f, 0.5f);

    private List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();


    private Vector3 lastPosition;
    private Quaternion lastRotation;
    private float lastTime;

    private int lastTick;
    private float lastLerpAmount;

    private TransformUpdate current;


    [SerializeField] private float timeElapsed = 0f;
    [SerializeField] private float timeToReachTarget = 0.1f;


    public bool isLocalPlayer = false;
    public bool Sync = false;
    public bool Delay = false;
    public bool WaitForLerp = false;

    private void Start()
    {
        if (isLocalPlayer)
        {
            Sync = false;
            Delay = false;
            WaitForLerp = false;
        }

        // The localPlayer uses a different tick
        int currentTick = isLocalPlayer ? 0 : GlobalVariables.clientTick - Utils.timeToTicks(interpolation.GetValue());
        if (currentTick < 0)
            currentTick = 0;

        lastPosition = transform.position;
        lastRotation = transform.rotation;
        lastTime = Time.time;

        lastTick = 0;
        lastLerpAmount = 0f;

        current = new TransformUpdate(currentTick, Time.time, Time.time, transform.position, transform.position, transform.rotation, transform.rotation);
    }

    private void Update()
    {
        if (futureTransformUpdates == null)
        {
            futureTransformUpdates = new List<TransformUpdate>();
            return;
        }

        if (Sync)
        {
            // There is no updates to lerp from, return
            if (futureTransformUpdates.Count <= 0 || futureTransformUpdates[0] == null)
                return;

            TransformUpdate last = TransformUpdate.zero;
            foreach (TransformUpdate update in futureTransformUpdates.ToArray())
            {
                if (update == null || update == TransformUpdate.zero)
                    continue;

                // if tick < current client tick - interpolation amount, then remove
                if (update.tick < GlobalVariables.clientTick - Utils.timeToTicks(interpolation.GetValue()))
                {
                    futureTransformUpdates.Remove(update);
                    continue;
                }

                if (update.tick <= last?.tick)
                    continue;

                // Purpose: Add fake packets in between real ones, to account for packet loss
                if (last != TransformUpdate.zero)
                {
                    // Get tick difference
                    int tickDifference = update.tick - last.tick;
                    if (tickDifference > 1 && !(tickDifference < 0))
                    {
                        // Loop through every tick till getting to the last tick,
                        // which we dont use since it is the current tick
                        TransformUpdate lastInForLoop = last;
                        for (int j = 1; j < tickDifference; j++)
                        {
                            // Create new update
                            TransformUpdate inBetween = TransformUpdate.zero;

                            // Calculate the fraction in between the ticks
                            float fraction = (float)j / (float)tickDifference;

                            // Set corresponding last values
                            inBetween.lastPosition = lastInForLoop.position;
                            inBetween.lastRotation = lastInForLoop.rotation;
                            inBetween.lastTime = lastInForLoop.time;

                            // Lerp with the given fraction
                            inBetween.position = Vector3.Lerp(inBetween.lastPosition, update.position, fraction);
                            inBetween.rotation = Quaternion.Slerp(inBetween.lastRotation, update.rotation, fraction);
                            inBetween.time = Mathf.Lerp(lastInForLoop.time, update.time, fraction);

                            // Set new tick
                            inBetween.tick = lastInForLoop.tick + 1;

                            // Insert new update
                            futureTransformUpdates.Insert(futureTransformUpdates.IndexOf(lastInForLoop), inBetween);

                            // Last tick is now the inserted tick
                            lastInForLoop = inBetween;

                            // Set current tick proper last positions
                            update.lastPosition = lastInForLoop.lastPosition;
                            update.lastRotation = lastInForLoop.rotation;
                            update.lastTime = lastInForLoop.time;
                        }
                    }
                }
                last = update;
            }
        }

        // There is no updates to lerp from, return
        if (futureTransformUpdates.Count <= 0 || futureTransformUpdates[0] == null)
            return;

        // Set current tick
        current = futureTransformUpdates[0];

        // If (time - time tick) <= interpolation amount, return
        if (Time.time - current.time <= Utils.roundTimeToTimeStep(interpolation.GetValue()) && Delay)
            return;

        if (!Sync)
        {
            if (isLocalPlayer)
            {
                timeElapsed = (timeElapsed * Utils.TickInterval() + Time.deltaTime) / Utils.TickInterval();

                Interpolate(timeElapsed);

                // While we have reached the target, move to the next and repeat
                while (ReachedTarget(timeElapsed))
                {
                    timeElapsed = (timeElapsed * Utils.TickInterval() - Utils.TickInterval()) / Utils.TickInterval();
                    timeElapsed = Mathf.Max(0f, timeElapsed);

                    if (futureTransformUpdates.Count <= 0)
                        break;

                    futureTransformUpdates.RemoveAt(0);
                    if (futureTransformUpdates.Count <= 0)
                        break;

                    // Set current tick
                    current = futureTransformUpdates[0];
                }
            }
            else
            {
                timeElapsed += Time.deltaTime;

                Interpolate(timeElapsed / timeToReachTarget);

                // While we have reached the target, move to the next and repeat
                while (ReachedTarget(timeElapsed / timeToReachTarget))
                {
                    timeElapsed -= timeToReachTarget;
                    timeToReachTarget = Mathf.Abs(current.time - current.lastTime);

                    if (futureTransformUpdates.Count <= 0)
                        break;

                    futureTransformUpdates.RemoveAt(0);
                    if (futureTransformUpdates.Count <= 0)
                        break;

                    // Set current tick
                    current = futureTransformUpdates[0];
                }
            }
        }
        else
        {
            // Lerp amount moved to the next loop but the current target didnt move to the next tick, so dont interpolate
            if (lastTick == current.tick && GlobalVariables.lerpAmount < lastLerpAmount)
                return;

            Interpolate(GlobalVariables.lerpAmount);
            lastTick = current.tick;
            lastLerpAmount = GlobalVariables.lerpAmount;
        }
    }


    // Returns if it has reached the targe when interpolating
    // WaitForLerp waits for _lerpAmount to reach 1
    // If it is false it will return true if the target tick
    // is equal to the current interpolated tick
    private bool ReachedTarget(float _lerpAmount)
    {
        if (_lerpAmount <= 0)
            return false;
        switch (mode)
        {
            case InterpolationMode.both:
                if (WaitForLerp)
                    return _lerpAmount >= 1f;
                else
                    return (transform.position == current.position && transform.rotation == current.rotation) || _lerpAmount >= 1f;
            case InterpolationMode.position:
                if (WaitForLerp)
                    return _lerpAmount >= 1f;
                else
                    return transform.position == current.position || _lerpAmount >= 1f;
            case InterpolationMode.rotation:
                if (WaitForLerp)
                    return _lerpAmount >= 1f;
                else
                    return transform.rotation == current.rotation || _lerpAmount >= 1f;
        }
        return false;
    }

    // Interpolates depending on the requested mode
    #region Interpolate
    private void Interpolate(float _lerpAmount)
    {
        switch (mode)
        {
            case InterpolationMode.both:
                InterpolatePosition(_lerpAmount);
                InterpolateRotation(_lerpAmount);
                break;
            case InterpolationMode.position:
                InterpolatePosition(_lerpAmount);
                break;
            case InterpolationMode.rotation:
                InterpolateRotation(_lerpAmount);
                break;
        }
    }

    private void InterpolatePosition(float _lerpAmount)
    {
        transform.position = Vector3.Lerp(current.lastPosition, current.position, _lerpAmount);
    }

    private void InterpolateRotation(float _lerpAmount)
    {
        transform.rotation = Quaternion.Slerp(current.lastRotation, current.rotation, _lerpAmount);
    }
    #endregion

    // Updates are used to add a new tick to the list
    // the list is sorted and then set the last tick info to the respective variables
    #region Updates

    internal void NewUpdate(int _tick, Vector3 _position, Quaternion _rotation)
    {
        if (futureTransformUpdates == null)
        {
            futureTransformUpdates = new List<TransformUpdate>();
            futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, Time.time, _position, _position, _rotation, _rotation));
            
            lastPosition = _position;
            lastRotation = _rotation;
            lastTime = Time.time;
            return;
        }

        futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, lastTime, _position, lastPosition, _rotation, lastRotation));
        futureTransformUpdates.Sort(delegate (TransformUpdate x, TransformUpdate y) {
            return x.tick.CompareTo(y.tick);
        });

        // Purpose: after sorting the updates, we set the last positions/rotations
        // This accounts for packets being out of order
        
        TransformUpdate last = TransformUpdate.zero;
        foreach (TransformUpdate transformUpdate in futureTransformUpdates)
        {
            if (transformUpdate == null)
                continue;

            if (last != TransformUpdate.zero)
            {
                transformUpdate.lastPosition = last.position;
                transformUpdate.lastRotation = last.rotation;
                transformUpdate.lastTime = last.time;

                lastPosition = last.position;
                lastRotation = last.rotation;
                lastTime = last.time;
            }

            last = transformUpdate;
        }
        if(futureTransformUpdates[futureTransformUpdates.Count - 1] != null)
        {
            lastPosition = futureTransformUpdates[futureTransformUpdates.Count - 1].position;
            lastRotation = futureTransformUpdates[futureTransformUpdates.Count - 1].rotation;
            lastTime = futureTransformUpdates[futureTransformUpdates.Count - 1].time;
        }
    }
    internal void NewUpdate(int _tick, Vector3 _position)
    {
        if (futureTransformUpdates == null)
        {
            futureTransformUpdates = new List<TransformUpdate>();
            futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, Time.time, _position, _position));
            lastPosition = _position;
            lastTime = Time.time;
            return;
        }

        futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, lastTime, _position, lastPosition));

        futureTransformUpdates.Sort(delegate (TransformUpdate x, TransformUpdate y) {
            return x.tick.CompareTo(y.tick);
        });

        // Purpose: after sorting the updates, we set the last positions/rotations
        // This accounts for packets being out of order

        TransformUpdate last = TransformUpdate.zero;
        foreach (TransformUpdate transformUpdate in futureTransformUpdates)
        {
            if (transformUpdate == null)
                continue;

            if (last != TransformUpdate.zero)
            {
                transformUpdate.lastPosition = last.position;
                transformUpdate.lastRotation = last.rotation;
                transformUpdate.lastTime = last.time;

                lastPosition = last.position;
                lastRotation = last.rotation;
                lastTime = last.time;
            }

            last = transformUpdate;
        }
        if (futureTransformUpdates[futureTransformUpdates.Count - 1] != null)
        {
            lastPosition = futureTransformUpdates[futureTransformUpdates.Count - 1].position;
            lastTime = futureTransformUpdates[futureTransformUpdates.Count - 1].time;
        }
    }
    internal void NewUpdate(int _tick, Quaternion _rotation)
    {
        if (futureTransformUpdates == null)
        {
            futureTransformUpdates = new List<TransformUpdate>();
            futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, Time.time, _rotation, _rotation));
            lastRotation = _rotation;
            lastTime = Time.time;
            return;
        }

        futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, lastTime, _rotation, lastRotation));

        futureTransformUpdates.Sort(delegate (TransformUpdate x, TransformUpdate y) {
            return x.tick.CompareTo(y.tick);
        });

        // Purpose: after sorting the updates, we set the last positions/rotations
        // This accounts for packets being out of order

        TransformUpdate last = TransformUpdate.zero;
        foreach (TransformUpdate transformUpdate in futureTransformUpdates)
        {
            if (transformUpdate == null)
                continue;

            if (last != TransformUpdate.zero)
            {
                transformUpdate.lastPosition = last.position;
                transformUpdate.lastRotation = last.rotation;
                transformUpdate.lastTime = last.time;

                lastPosition = last.position;
                lastRotation = last.rotation;
                lastTime = last.time;
            }

            last = transformUpdate;
        }
        if (futureTransformUpdates[futureTransformUpdates.Count - 1] != null)
        {
            lastRotation = futureTransformUpdates[futureTransformUpdates.Count - 1].rotation;
            lastTime = futureTransformUpdates[futureTransformUpdates.Count - 1].time;
        }
    }
    #endregion

    // It is used for localPlayer interpolation, for smooth camera gameplay
    // the reason it is a separete function is to skip some unecessary calls
    internal void PlayerUpdate(int _tick, Vector3 _position)
    {
        if (futureTransformUpdates == null)
        {
            futureTransformUpdates = new List<TransformUpdate>();
            futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, Time.time, _position, _position));
            lastPosition = _position;
            lastTime = Time.time;
            return;
        }

        futureTransformUpdates.Add(new TransformUpdate(_tick, Time.time, lastTime, _position, lastPosition));

        lastPosition = _position;
        lastTime = Time.time;
    }

    public enum InterpolationMode
    {
        both,
        position,
        rotation
    }
}