using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Adventure Movement")]
    public bool adventureMode = true;
    [Min(0.5f)] public float walkSpeed = 4.5f;
    [Min(1f)] public float sprintSpeed = 7.5f;
    [Min(1f)] public float turnSmoothSpeed = 12f;
    public Transform cameraTransform;

    [Header("Jump & Physics")]
    [Min(0f)] public float jumpHeight = 2.0f;
    public float gravity = -20f;
    [Min(0f)] public float groundCheckRadius = 0.22f;
    public LayerMask groundLayers = ~0;

    [Header("Slide & Crouch")]
    [Min(0.1f)] public float slideDuration = 0.85f;
    [Min(0.5f)] public float slideHeight = 1.0f;
    public LayerMask ceilingLayers = ~0;

    [Header("Runner Legacy Compatibility")]
    [FormerlySerializedAs("forwardSpeed")]
    [Min(0f)] public float autoRunSpeed = 8f;
    [Min(0.5f)] public float laneWidth = 3.5f;
    [Min(0.01f)] public float laneChangeSmoothTime = 0.12f;
    [Min(0.1f)] public float laneChangeMaxSpeed = 20f;

    private CharacterController controller;
    private Transform playerModel;
    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 modelStandingPosition;
    private Vector3 modelStandingScale;
    private int laneIndex;
    private float lateralVelocity;
    private float verticalVelocity;
    private float slideTimer;
    private bool jumpRequested;
    private bool slideRequested;
    private Vector2 inputDirection;
    private bool isSprinting;
    private bool runnerStopped;

    public int CurrentLane => laneIndex;
    public bool IsSliding => slideTimer > 0f;
    public bool IsGrounded => controller != null && (controller.isGrounded || CheckGrounded());

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        standingHeight = controller.height;
        standingCenter = controller.center;
        groundLayers &= ~(1 << gameObject.layer);
        ceilingLayers &= ~(1 << gameObject.layer);
        laneWidth = Mathf.Max(0.5f, laneWidth);
        // Force centre lane on spawn/restart.
        Vector3 pos = transform.position;
        pos.x = 0f;
        transform.position = pos;
        laneIndex = 0;

        playerModel = transform.Find("PlayerModel");
        if (playerModel != null)
        {
            modelStandingPosition = playerModel.localPosition;
            modelStandingScale = playerModel.localScale;
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        SetSliding(false);
    }

    private void Update()
    {
        if (controller == null || !controller.enabled || Time.timeScale <= 0f)
            return;
        if (GameManager.Instance != null && GameManager.Instance.IsFinished)
            return;
        if (GameManager.Instance != null && GameManager.Instance.IsInputLocked)
            return;
        if (runnerStopped)
            return;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        ReadKeyboard();

        bool grounded = IsGrounded;
        if (grounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (grounded && jumpRequested && !IsSliding)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        jumpRequested = false;

        verticalVelocity += gravity * Time.deltaTime;

        if (slideRequested)
        {
            RequestSlide();
            slideRequested = false;
        }
        UpdateSlide();

        if (adventureMode)
        {
            UpdateAdventureMovement();
        }
        else
        {
            UpdateRunnerMovement();
        }
    }

    private void UpdateAdventureMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        if (inputDirection.sqrMagnitude > 0.001f)
        {
            Vector3 camForward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 camRight = cameraTransform != null ? cameraTransform.right : Vector3.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // W moves along camera forward, S moves directly backward, A moves left, D moves right
            moveDirection = (camForward * inputDirection.y + camRight * inputDirection.x).normalized;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSmoothSpeed * Time.deltaTime);
            }
        }

        float currentSpeed = walkSpeed;
        if (IsSliding)
        {
            currentSpeed = isSprinting ? sprintSpeed * 0.95f : walkSpeed * 0.85f;
        }
        else if (isSprinting && inputDirection.sqrMagnitude > 0.001f)
        {
            currentSpeed = sprintSpeed;
        }

        Vector3 horizontalVelocity = moveDirection * currentSpeed;
        Vector3 finalVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        controller.Move(finalVelocity * Time.deltaTime);

        // Keep laneIndex updated for legacy inspector/verifier queries
        laneIndex = Mathf.Clamp(Mathf.RoundToInt(transform.position.x / laneWidth), -1, 1);
    }

    private void UpdateRunnerMovement()
    {
        float targetX = laneIndex * laneWidth;
        float nextX = Mathf.SmoothDamp(
            transform.position.x,
            targetX,
            ref lateralVelocity,
            laneChangeSmoothTime,
            laneChangeMaxSpeed,
            Time.deltaTime);
        float lateralSpeed = Time.deltaTime > 0f
            ? (nextX - transform.position.x) / Time.deltaTime
            : 0f;

        transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        float speedMultiplier = GameManager.Instance != null ? GameManager.Instance.SpeedBoostMultiplier : 1f;
        controller.Move(new Vector3(lateralSpeed, verticalVelocity, autoRunSpeed * speedMultiplier) * Time.deltaTime);
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Obstacle obstacle = hit.collider.GetComponentInParent<Obstacle>();
        if (obstacle != null)
            obstacle.ApplyDamageTo(this);
    }

    private void ReadKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!adventureMode)
        {
            if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
                RequestLaneChange(-1);
            if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
                RequestLaneChange(1);
            if (keyboard.spaceKey.wasPressedThisFrame)
                RequestJump();
            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                keyboard.leftCtrlKey.wasPressedThisFrame || keyboard.rightCtrlKey.wasPressedThisFrame)
                RequestSlide();
            return;
        }

        float moveX = 0f;
        float moveY = 0f;

        // W / A / S / D - 360 degree movement
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) moveX -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;
        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) moveY += 1f;
        // S always moves backwards; it does NOT trigger sliding
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) moveY -= 1f;

        inputDirection = new Vector2(moveX, moveY).normalized;
        isSprinting = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;

        // Space: Jump
        if (keyboard.spaceKey.wasPressedThisFrame)
            RequestJump();

        // C or Left Ctrl: Slide / Crouch ONLY (S will never trigger this)
        if (keyboard.cKey.wasPressedThisFrame || keyboard.leftCtrlKey.wasPressedThisFrame)
            RequestSlide();

    }

    public void ResetToCentreLane()
    {
        Vector3 p = transform.position; p.x = 0f; transform.position = p;
        laneIndex = 0;
        lateralVelocity = 0f;
    }

    public void StopRunnerAtChest()
    {
        if (!adventureMode)
            runnerStopped = true;
    }

    public void RequestLaneChange(int direction)
    {
        if (direction != 0 && Time.timeScale > 0f)
            laneIndex = Mathf.Clamp(laneIndex + (direction < 0 ? -1 : 1), -1, 1);
    }

    public void RequestJump()
    {
        if (Time.timeScale > 0f)
        {
            jumpRequested = true;
            AudioManager.Play("sfx_jump");
        }
    }

    public void RequestSlide()
    {
        if (Time.timeScale <= 0f || IsSliding || !IsGrounded)
            return;
        slideTimer = slideDuration;
        AudioManager.Play("sfx_slide");
        SetSliding(true);
    }

    private void UpdateSlide()
    {
        if (slideTimer <= 0f)
            return;

        slideTimer -= Time.deltaTime;
        if (slideTimer <= 0f)
        {
            // Head clearance check before standing up
            if (HasHeadObstruction())
            {
                slideTimer = 0.1f; // Retain crouch until clear
            }
            else
            {
                slideTimer = 0f;
                SetSliding(false);
            }
        }
    }

    private bool HasHeadObstruction()
    {
        if (controller == null)
            return false;

        float bottomY = transform.position.y + controller.center.y - (controller.height * 0.5f);
        float checkStart = bottomY + slideHeight;
        float checkEnd = bottomY + standingHeight;
        float castDistance = Mathf.Max(0.1f, checkEnd - checkStart);

        Vector3 rayStart = transform.position + Vector3.up * checkStart;
        float radius = controller.radius * 0.85f;

        return Physics.SphereCast(rayStart, radius, Vector3.up, out _, castDistance, ceilingLayers, QueryTriggerInteraction.Ignore);
    }

    private void SetSliding(bool sliding)
    {
        if (controller == null)
            return;

        if (sliding)
        {
            float height = Mathf.Clamp(slideHeight, 0.5f, standingHeight);
            controller.height = height;
            controller.center = standingCenter + Vector3.down * ((standingHeight - height) * 0.5f);
            if (playerModel != null)
            {
                playerModel.localScale = new Vector3(modelStandingScale.x, modelStandingScale.y * 0.58f, modelStandingScale.z);
                playerModel.localPosition = modelStandingPosition + Vector3.down * 0.35f;
            }
        }
        else
        {
            controller.height = standingHeight;
            controller.center = standingCenter;
            if (playerModel != null)
            {
                playerModel.localScale = modelStandingScale;
                playerModel.localPosition = modelStandingPosition;
            }
        }
    }

    private bool CheckGrounded()
    {
        Vector3 bottom = transform.position + controller.center + Vector3.down * (controller.height * 0.5f);
        Vector3 probeCenter = bottom + Vector3.up * groundCheckRadius;
        return Physics.CheckSphere(probeCenter, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            slideTimer = 0f;
            SetSliding(false);
        }
    }
}
