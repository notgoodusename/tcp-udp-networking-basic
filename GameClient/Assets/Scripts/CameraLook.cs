using UnityEngine;

public class CameraLook : MonoBehaviour
{
    static Convar rotationBounds = new Convar("sv_maxrotation", 89f, "Maximum rotation around the x axis", Flags.NETWORK);
    static Convar rotationSensitivity = new Convar("sensitivity", 2.5f, "Camera rotation sensitivity", Flags.CLIENT);
    
    public Camera playerCamera;

    private ConsoleUI consoleUI;

    private float rotationX;
    private float rotationY;

    private void Start()
    {
        rotationY = playerCamera.transform.localEulerAngles.x;
        rotationX = transform.eulerAngles.y;
        consoleUI = FindObjectOfType<ConsoleUI>();
    }

    private void Update()
    {
        if (consoleUI.isActive())
            return;

        Rotation();
    }

    private void Rotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            Cursor.lockState = CursorLockMode.Locked;
        
        float _mouseVertical = -Input.GetAxis("Mouse Y");
        float _mouseHorizontal = Input.GetAxis("Mouse X");

        rotationY += _mouseVertical * rotationSensitivity.GetValue();
        rotationX += _mouseHorizontal * rotationSensitivity.GetValue();

        rotationY = Mathf.Clamp(rotationY, -rotationBounds.GetValue(), rotationBounds.GetValue());

        playerCamera.transform.localRotation = Quaternion.Euler(rotationY, rotationX, 0f);
        transform.rotation = Quaternion.Euler(0f, rotationX, 0f);
    }
}
