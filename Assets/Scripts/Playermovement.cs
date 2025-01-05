using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class Playermovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;
    private PlayerControllers playerControls;
    private bool isCrouching = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerControls = new PlayerControllers();

        // Subscribe to input events
        playerControls.Player.Jump.performed += ctx => Jump();
        playerControls.Player.Crouch.performed += ctx => ToggleCrouch();
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        Vector2 input = playerControls.Player.Move.ReadValue<Vector2>();
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        float curSpeedX = walkSpeed * input.y;
        float curSpeedY = walkSpeed * input.x;
        float movementDirectionY = moveDirection.y;

        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // Handle look
        // Vector2 lookInput = playerControls.Look.Look.ReadValue<Vector2>();
        // rotationX += -lookInput.y * lookSpeed;
        // rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        // playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        // transform.rotation *= Quaternion.Euler(0, lookInput.x * lookSpeed, 0);
        Vector2 mouseDelta = playerControls.Look.Look.ReadValue<Vector2>();
    
        float mouseX = mouseDelta.x * lookSpeed * Time.deltaTime * 2f;
        float mouseY = mouseDelta.y * lookSpeed * Time.deltaTime * 2f;

        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void Jump()
    {
        if (characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
    }

    private void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        characterController.height = isCrouching ? crouchHeight : defaultHeight;
        walkSpeed = isCrouching ? crouchSpeed : 6f;
    }
}
