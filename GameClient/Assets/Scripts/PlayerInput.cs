using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    static Convar moveSpeed = new Convar("sv_movespeed", 6.35f, "Movement speed for the player", Flags.NETWORK);
    static Convar runAcceleration = new Convar("sv_accelerate", 14f, "Acceleration for the player when moving", Flags.NETWORK);
    static Convar airAcceleration = new Convar("sv_airaccelerate", 12f, "Air acceleration for the player", Flags.NETWORK);
    static Convar jumpForce = new Convar("sv_jumpforce", 4f, "Jump force for the player", Flags.NETWORK);
    static Convar friction = new Convar("sv_friction", 5.5f, "Player friction", Flags.NETWORK);
    static Convar gravity = new Convar("sv_gravity", 9.81f, "Player gravity", Flags.NETWORK);

    static ConvarRef interp = new ConvarRef("interpolation");

    public PlayerManager playerManager;
    public Camera playerCamera;
    public CharacterController controller;

    public GameObject groundCheck;
    public LayerMask whatIsGround;
    public float checkRadius;

    [HideInInspector]
    public Vector3 velocity = Vector3.zero;
    private bool isGrounded;

    private const int STATE_CACHE_SIZE = 1024;

    private int simulationFrame;
    private int lastCorrectedFrame;

    private SimulationState serverSimulationState;
    private SimulationState[] simulationStateCache;
    private ClientInputState[] inputStateCache;
    private ClientInputState inputState;

    private ConsoleUI consoleUI;

    private void Awake()
    {
        lastCorrectedFrame = 0;
        simulationFrame = 0;

        serverSimulationState = new SimulationState();
        simulationStateCache = new SimulationState[STATE_CACHE_SIZE];
        inputStateCache = new ClientInputState[STATE_CACHE_SIZE];
        inputState = new ClientInputState();
    }
    void Start()
    {
        consoleUI = FindObjectOfType<ConsoleUI>();
    }

    private void FixedUpdate()
    {
        // Process inputs
        ProcessInput(inputState);
         
        // Send inputs so the server can process them
        SendInputToServer();

        // Reconciliate
        if (serverSimulationState != null) Reconciliate();

        // Get current simulationState
        SimulationState simulationState =
            SimulationState.CurrentSimulationState(inputState, this);

        // Determine the cache index based on on modulus operator.
        int cacheIndex = simulationFrame % STATE_CACHE_SIZE;

        // Store the SimulationState into the simulationStateCache 
        simulationStateCache[cacheIndex] = simulationState;

        // Store the ClientInputState into the inputStateCache
        inputStateCache[cacheIndex] = inputState;

        // Move next frame
        ++simulationFrame;

        // Add position to interpolate
        playerManager.interpolation.PlayerUpdate(simulationFrame, transform.position);
    }

    private void Update()
    {
        // Console is open, dont move
        if (consoleUI.isActive())
        {
            inputState = new ClientInputState
            {
                tick = GlobalVariables.clientTick - Utils.timeToTicks(interp.GetValue()),
                lerpAmount = GlobalVariables.lerpAmount,
                simulationFrame = simulationFrame,
                buttons = 0,
                HorizontalAxis = 0f,
                VerticalAxis = 0f,
                rotation = playerCamera.transform.rotation,
            };
            return;
        }

        // Set correspoding buttons
        int buttons = 0;
        if (Input.GetButton("Jump"))
            buttons |= Button.Jump;
        if (Input.GetButton("Fire1"))
            buttons |= Button.Fire1;

        // Set new input
        inputState = new ClientInputState
        {
            tick = GlobalVariables.clientTick - Utils.timeToTicks(interp.GetValue()),
            lerpAmount = GlobalVariables.lerpAmount,
            simulationFrame = simulationFrame,
            buttons = buttons,
            HorizontalAxis = Input.GetAxisRaw("Horizontal"),
            VerticalAxis = Input.GetAxisRaw("Vertical"),
            rotation = playerCamera.transform.rotation,
        };
    }
    
    private void ProcessInput(ClientInputState inputs)
    {
        RotationCheck(inputs);

        CalculateVelocity(inputs);
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    // Normalizes rotation
    private void RotationCheck(ClientInputState inputs)
    {
        inputs.rotation.Normalize();
    }

    // Calculates player velocity with the given inputs
    private void CalculateVelocity(ClientInputState inputs)
    {
        GroundCheck();

        if (isGrounded)
            WalkMove(inputs);
        else
            AirMove(inputs);
    }

    #region Movement
    void GroundCheck()
    {
        // Are we touching something?
        isGrounded = Physics.CheckSphere(groundCheck.transform.position, checkRadius, whatIsGround);

        // We are touching the ground check if it is a slope
        if (isGrounded &&
            Physics.SphereCast(transform.position, checkRadius, Vector3.down, out RaycastHit hit, 100f, whatIsGround))
        {
            isGrounded = Vector3.Angle(Vector3.up, hit.normal) <= controller.slopeLimit;
        }
    }

    void AirMove(ClientInputState inputs)
    {
        Vector2 input = new Vector2(inputs.HorizontalAxis, inputs.VerticalAxis).normalized;

        Vector3 forward = (inputs.rotation * Vector3.forward);
        Vector3 right = (inputs.rotation * Vector3.right);

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 wishdir = right * input.x + forward * input.y;

        float wishspeed = wishdir.magnitude;

        AirAccelerate(wishdir, wishspeed, airAcceleration.GetValue());

        velocity.y -= gravity.GetValue() * Time.fixedDeltaTime;
    }

    void WalkMove(ClientInputState inputs)
    {
        if ((inputs.buttons & Button.Jump) == Button.Jump)
        {
            Friction(0f);
            velocity.y = jumpForce.GetValue();
            AirMove(inputs);
            return;
        }
        else
            Friction(1f);

        Vector2 input = new Vector2(inputs.HorizontalAxis, inputs.VerticalAxis).normalized;

        var forward = (inputs.rotation * Vector3.forward);
        var right = (inputs.rotation * Vector3.right);

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 wishdir = right * input.x + forward * input.y;

        float wishspeed = wishdir.magnitude;
        wishspeed *= moveSpeed.GetValue();

        Accelerate(wishdir, wishspeed, runAcceleration.GetValue());

        velocity.y = -gravity.GetValue() * Time.fixedDeltaTime;

        if ((inputs.buttons & Button.Jump) == Button.Jump)
        {
            velocity.y = jumpForce.GetValue();
        }
    }

    private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;

        currentspeed = Vector3.Dot(velocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;
        accelspeed = accel * Time.fixedDeltaTime * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;
    }

    void AirAccelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed, accelspeed, currentspeed;

        currentspeed = Vector3.Dot(velocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;

        accelspeed = accel * wishspeed * Time.fixedDeltaTime;

        if (accelspeed > addspeed)
            accelspeed = addspeed;

        velocity.x += accelspeed * wishdir.x;
        velocity.z += accelspeed * wishdir.z;
    }

    void Friction(float t)
    {
        float speed = velocity.magnitude, newspeed, control, drop;

        if (speed < 0.1f)
            return;

        drop = 0;

        if (isGrounded)
        {
            control = speed < runAcceleration.GetValue() ? runAcceleration.GetValue() : speed;
            drop += control * friction.GetValue() * Time.fixedDeltaTime * t;
        }

        newspeed = speed - drop;
        if (newspeed < 0)
            newspeed = 0;

        newspeed /= speed;

        velocity.x *= newspeed;
        velocity.z *= newspeed;
    }
    #endregion

    private void SendInputToServer()
    {
        ClientSend.PlayerInput(inputState);
    }

    private void setPlayerToSimulationState(SimulationState state)
    {
        transform.position = state.position;
        velocity = state.velocity;
        Physics.SyncTransforms();
    }

    public void Reconciliate()
    {
        // Sanity check, don't reconciliate for old states.
        if (serverSimulationState.simulationFrame <= lastCorrectedFrame) return;

        // Determine the cache index 
        int cacheIndex = serverSimulationState.simulationFrame % STATE_CACHE_SIZE;

        // Obtain the cached input and simulation states.
        ClientInputState cachedInputState = inputStateCache[cacheIndex];
        SimulationState cachedSimulationState = simulationStateCache[cacheIndex];

        // If there's missing cache data for either input or simulation 
        // snap the player's position to match the server.
        if (cachedInputState == null || cachedSimulationState == null)
        {
            setPlayerToSimulationState(serverSimulationState);

            // Set the last corrected frame to equal the server's frame.
            lastCorrectedFrame = serverSimulationState.simulationFrame;
            return;
        }

        // If the simulation time isnt equal to the serve time then return
        // this should never happen
        if (cachedInputState.simulationFrame != serverSimulationState.simulationFrame || cachedSimulationState.simulationFrame != serverSimulationState.simulationFrame)
            return;

        // Find the difference between the vector's values. 
        Vector3 difference = cachedSimulationState.position - serverSimulationState.position;

        //  The amount of distance in units that we will allow the client's
        //  prediction to drift from it's position on the server, before a
        //  correction is necessary. 
        float tolerance = 0.0000001f;

        // A correction is necessary.
        if (difference.sqrMagnitude > tolerance)
        {
            // Show warning about misprediction
            Debug.LogWarning("Client misprediction with a difference of " + difference + " at frame " + serverSimulationState.simulationFrame + ".");

            // Set the player's position to match the server's state. 
            setPlayerToSimulationState(serverSimulationState);

            // Declare the rewindFrame as we're about to resimulate our cached inputs. 
            int rewindFrame = serverSimulationState.simulationFrame;

            // Loop through and apply cached inputs until we're 
            // caught up to our current simulation frame. 
            while (rewindFrame < simulationFrame)
            {
                // Determine the cache index 
                int rewindCacheIndex = rewindFrame % STATE_CACHE_SIZE;

                // Obtain the cached input and simulation states.
                ClientInputState rewindCachedInputState = inputStateCache[rewindCacheIndex];
                SimulationState rewindCachedSimulationState = simulationStateCache[rewindCacheIndex];

                // If there's no state to simulate, for whatever reason, 
                // increment the rewindFrame and continue.
                if (rewindCachedInputState == null || rewindCachedSimulationState == null)
                {
                    ++rewindFrame;
                    continue;
                }

                // Process the cached inputs. 
                ProcessInput(rewindCachedInputState);

                // Replace the simulationStateCache index with the new value.
                SimulationState rewoundSimulationState = SimulationState.CurrentSimulationState(rewindCachedInputState, this);
                rewoundSimulationState.simulationFrame = rewindFrame;
                simulationStateCache[rewindCacheIndex] = rewoundSimulationState;

                // Increase the amount of frames that we've rewound.
                ++rewindFrame;
            }
        }

        // Once we're complete, update the lastCorrectedFrame to match.
        // NOTE: Set this even if there's no correction to be made. 
        lastCorrectedFrame = serverSimulationState.simulationFrame;
    }

    // We received a new simualtion state, overwrite it
    public void OnServerSimulationStateReceived(SimulationState simulationState)
    {
        if (serverSimulationState?.simulationFrame < simulationState.simulationFrame)
            serverSimulationState = simulationState;
    }
}