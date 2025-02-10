using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public enum ViewMode { FirstPerson, ThirdPerson }

    [Header("References")]
    public Transform playerModel;
    public Camera fpvCamera;
    public Camera tpvCamera;
    public Transform cameraPivot;

    [Header("3PV Camera Zoom Settings")]
    public float cameraDistance = 5f;
    public float minCameraDistance = 2f;
    public float maxCameraDistance = 10f;
    public float cameraZoomSpeed = 5f;

    [Header("3PV Camera Rotation Settings")]
    public float cameraRotationSpeed = 300f;
    public float modelRotationLag = 10f;
    public float modelAngleLimit = 10f;
    public float catchUpSpeedMultiplier = 5f;
    public float tpvMaxPitch = 80f;
    public float tpvMinPitch = -30f;

    [Header("3PV Camera Clipping")]
    public LayerMask cameraCollisionMask;
    public float cameraCollisionRadius = 0.3f;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float jumpForce = 8f;
    public float flightSpeed = 10f;
    public float gravityMultiplier = 2f; // Adjust fall speed

    [Header("1PV Camera Settings")]
    public float mouseSensitivity = 2f;
    public float fpvMaxPitch = 80f;

    private CharacterController controller;
    private ViewMode currentView = ViewMode.FirstPerson;
    private bool isFlying = false;
    private Vector3 velocity;
    private float verticalLookRotation;
    private float currentPitch;
    private float currentYaw;
    private float targetCameraDistance;
    private float modelCurrentYaw;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        targetCameraDistance = cameraDistance;
        InitializeView();
    }

    void InitializeView()
    {
        cameraPivot.localRotation = Quaternion.identity;
        playerModel.localRotation = Quaternion.identity;
        Cursor.visible = false;
        EnableFPV();
    }

    void Update()
    {
        HandleInput();
        HandleMovement();
        HandleCamera();
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            if (currentView == ViewMode.FirstPerson) {
                Enable3PV();
            } else {
                EnableFPV();
            }
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            isFlying = !isFlying;
            velocity.y = 0;
        }
    }

    void EnableFPV()
    {
        currentView = ViewMode.FirstPerson;
        tpvCamera.gameObject.SetActive(false);
        fpvCamera.gameObject.SetActive(true);
    }

    void Enable3PV()
    {
        currentView = ViewMode.ThirdPerson;
        
        // Reset camera pivot to match model rotation
        currentYaw = playerModel.eulerAngles.y;
        currentPitch = 30f; // Default pitch angle
        cameraPivot.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        
        fpvCamera.gameObject.SetActive(false);
        tpvCamera.gameObject.SetActive(true);
    }

    void HandleCamera()
    {
        if (currentView == ViewMode.FirstPerson)
        {
            HandleFirstPersonCamera();
        }
        else
        {
            HandleThirdPersonCamera();
        }
    }

    void HandleFirstPersonCamera()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate model instead of root transform
        playerModel.Rotate(Vector3.up * mouseX);
        verticalLookRotation = Mathf.Clamp(verticalLookRotation - mouseY, -fpvMaxPitch, fpvMaxPitch);
        
        // Keep camera aligned with model
        fpvCamera.transform.localEulerAngles = Vector3.right * verticalLookRotation;
    }

    void HandleThirdPersonCamera()
    {
        HandleCameraRotation();
        HandleCameraZoom();
        HandleCameraPosition();
        HandleModelRotation();
    }

    void HandleCameraRotation()
    {
        // Rotate camera pivot based on mouse input
        currentYaw += Input.GetAxis("Mouse X") * cameraRotationSpeed * Time.deltaTime;
        currentPitch -= Input.GetAxis("Mouse Y") * cameraRotationSpeed * Time.deltaTime;
        currentPitch = Mathf.Clamp(currentPitch, tpvMinPitch, tpvMaxPitch);
        
        cameraPivot.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
    }

    void HandleModelRotation()
    {
        // Get current camera yaw from pivot
        float cameraYaw = cameraPivot.eulerAngles.y;
        float modelYaw = playerModel.eulerAngles.y;
        
        // Calculate angle difference
        float angleDifference = Mathf.DeltaAngle(modelYaw, cameraYaw);
        
        // Apply base rotation speed
        float rotationSpeed = cameraRotationSpeed - modelRotationLag;
        
        // Apply catchup speed if over limit
        if (Mathf.Abs(angleDifference) > modelAngleLimit)
        {
            rotationSpeed *= catchUpSpeedMultiplier;
        }

        // Smoothly rotate model towards camera yaw
        modelCurrentYaw = Mathf.MoveTowardsAngle(
            modelYaw, 
            cameraYaw, 
            rotationSpeed * Time.deltaTime
        );

        playerModel.localRotation = Quaternion.Euler(0, modelCurrentYaw, 0);
    }

    void HandleCameraZoom()
    {
        targetCameraDistance = Mathf.Clamp(
            targetCameraDistance - Input.GetAxis("Mouse ScrollWheel") * cameraZoomSpeed,
            minCameraDistance,
            maxCameraDistance
        );
    }

    void HandleCameraPosition()
    {
        // Calculate desired camera position
        Vector3 desiredPosition = cameraPivot.position - 
                                cameraPivot.forward * targetCameraDistance;

        // Camera collision detection
        if (Physics.SphereCast(cameraPivot.position, cameraCollisionRadius, 
            (desiredPosition - cameraPivot.position).normalized, 
            out RaycastHit hit, targetCameraDistance, cameraCollisionMask))
        {
            tpvCamera.transform.position = hit.point + hit.normal * cameraCollisionRadius;
        }
        else
        {
            tpvCamera.transform.position = desiredPosition;
        }

        tpvCamera.transform.LookAt(cameraPivot.position);
    }

    void HandleMovement()
    {
        Vector3 move = currentView switch
        {
            ViewMode.FirstPerson => fpvCamera.transform.TransformDirection(
                new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"))
            ),
            ViewMode.ThirdPerson => playerModel.TransformDirection(
                new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"))
            ),
            _ => Vector3.zero
        };

        float speedMultiplier = isFlying ? flightSpeed : 1f;

        move.y = 0;
        move = move.normalized * walkSpeed * speedMultiplier;

        if (isFlying)
        {
            HandleFlightMovement(ref move);
        }
        else
        {
            HandleGroundMovement(ref move);
        }

        controller.Move(velocity * Time.deltaTime);
    }

    void HandleFlightMovement(ref Vector3 move)
    {
        float verticalInput = Input.GetKey(KeyCode.Space) ? 1 : 
                            Input.GetKey(KeyCode.LeftControl) ? -1 : 0;
        velocity = move + Vector3.up * verticalInput * flightSpeed;
    }

    void HandleGroundMovement(ref Vector3 move)
    {
        velocity.x = move.x;
        velocity.z = move.z;

        if (controller.isGrounded)
        {
            velocity.y = -0.5f;
            if (Input.GetButtonDown("Jump"))
            {
                velocity.y = jumpForce;
            }
        }
        else
        {
            velocity.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }
}