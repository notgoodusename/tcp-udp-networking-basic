using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    static Convar moveSpeed = new Convar("sv_movespeed", 6.35f, "Movement speed for the player", Flags.NETWORK);
    static Convar runAcceleration = new Convar("sv_accelerate", 14f, "Acceleration for the player when moving", Flags.NETWORK);
    static Convar airAcceleration = new Convar("sv_airaccelerate", 12f, "Air acceleration for the player", Flags.NETWORK);
    static Convar jumpForce = new Convar("sv_jumpforce", 4f, "Jump force for the player", Flags.NETWORK);
    static Convar friction = new Convar("sv_friction", 5.5f, "Player friction", Flags.NETWORK);
    static Convar gravity = new Convar("sv_gravity", 9.81f, "Player gravity", Flags.NETWORK);
    static Convar rotationBounds = new Convar("sv_maxrotation", 89f, "Maximum rotation around the x axis", Flags.NETWORK);

    public GameObject head;
    public CharacterController controller;

    public LayerMask whatIsGround;
    public GameObject groundCheck;
    public float checkRadius;

    [HideInInspector]
    public int id;
    [HideInInspector]
    public string username;
    [HideInInspector]
    public int tick = 0;

    [HideInInspector]
    public Vector3 velocity = Vector3.zero;
    private bool isGrounded;

    private int lastFrame;
    private Queue<ClientInputState> clientInputs = new Queue<ClientInputState>();

    // Set corresponding id and name
    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
    }

    private void Awake()
    {
        lastFrame = 0;
    }

    public void Destroy()
    {
        Destroy(gameObject);
    }

    public void FixedUpdate()
    {
        if(!Server.isActive)
        {
            lastFrame = 0;
            return;
        }

        ProcessInputs();
        ServerSend.PlayerTransform(this);
    }

    public void ProcessInputs()
    {
        // Declare the ClientInputState that we're going to be using.
        ClientInputState inputState = null;

        // Obtain CharacterInputState's from the queue. 
        while (clientInputs.Count > 0 && (inputState = clientInputs.Dequeue()) != null)
        {
            // Player is sending simulation frames that are in the past, dont process them
            if (inputState.simulationFrame <= lastFrame)
                continue;

            lastFrame = inputState.simulationFrame;

            // Process the input.
            ProcessInput(inputState);

            // Obtain the current SimulationState.
            SimulationState state = SimulationState.CurrentSimulationState(inputState, this);

            // Send the state back to the client.
            ServerSend.SendSimulationState(id, state);
        }
    }

    private void ProcessInput(ClientInputState inputs)
    {
        RotationCheck(inputs);

        if ((inputs.buttons & Button.Fire1) == Button.Fire1)
        {
            LagCompensation.Backtrack(id, inputs.tick, inputs.lerpAmount);
        }
        
        CalculateVelocity(inputs);
        controller.Move(velocity * Time.fixedDeltaTime);
    }

    // Clamps and sets rotation
    private void RotationCheck(ClientInputState inputs)
    {
        // Set body y rotation
        inputs.rotation.Normalize();
        transform.rotation = new Quaternion(0f, inputs.rotation.y, 0f, inputs.rotation.w);

        // Set x rotation
        head.transform.localRotation = new Quaternion(inputs.rotation.x, 0f, 0f, inputs.rotation.w);

        // Clamp x rotation
        float angle = head.transform.localEulerAngles.x;
        angle = (angle > 180) ? angle - 360 : angle;
        angle = Mathf.Clamp(angle, -rotationBounds.GetValue(), rotationBounds.GetValue());

        // Set clamped angle and normalize
        head.transform.rotation = Quaternion.Euler(angle, head.transform.eulerAngles.y, 0f);
        head.transform.rotation.Normalize();
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

    public void AddInput(ClientInputState _inputState)
    {
        clientInputs.Enqueue(_inputState);
    }
}
