using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Drive Controls")]
    [Tooltip("The maximum speed of the kart.")]
    public float maxSpeed = 20;
    [Tooltip("The amount of forward force given when driving forwards.")]
    public float driveForce = 20;
    [Tooltip("The amount of backward force given when driving backwards.")]
    public float reverseForce = 10;
    [Tooltip("The power of the brakes when braking to a halt or when driving in the other direction.")]
    public float deaccelerationAmount = 3;

    [Header("Steering Controls")]
    [Tooltip("The rotation speed when steering without drifting.")]
    public float steeringPower = 2;
    [Tooltip("The amount of smoothing to achieve the desired rotation speed.")]
    public float steerSmoothingAmount = 6;
    [Tooltip("The decrease of steering power when stopping to a halt.")]
    public float deacceleratonSteerFactor = 0.25f;
    [Tooltip("The maximum speed where the sterring speed is at maximum. When the kart goes above this speed, the steering power is decreased.")]
    public float steerPowerDecreasePoint = 2;
    [Tooltip("The rotation speed will be multiplied by this value such that it decreases when halting to a stop. When the kart goes above a certain speed, the steering power is decreased by this amount.")]
    public float steerPowerDecreaseFactor = 0.05f;
    // The targeted rotation based on the steering and drifting controls.
    private float targetRotationDelta = 0;
    // The current rotation of the kart.
    [HideInInspector] public float currentRotation;

    [Header("Drifting Controls")]
    [Tooltip("The rotation speed when steering while drifting")]
    public float driftingPower = 2.5f;
    // The direction of the drift. Similar to Mario Kart 8, the drift direction is locked when the player starts drifting.
    [HideInInspector] public float driftDirection;
    // When this is true, the kart is currently drifting.
    [HideInInspector] public bool isDrifting;

    [Header("Jump Controls")]
    [Tooltip("The amount of impulse given when the kart is jumping")]
    public float jumpStrength = 5;

    [Header("Physics Attributes")]
    [Tooltip("This value is multiplied by the local sideways velocity of the kart. A force with an amount based on the result will be given to the kart to prevent sliding sideways. Because the kart is physically a ball, this value is crucial to prevent the ball from rolling to the side when turning.")]
    public float sideDragFactor = 200;
    [Tooltip("The amount of downwards force given to the kart to simulate gravity.")]
    public float gravityForce = 20;
    [Tooltip("The layer mask that is registered as a 'road' (or ground) for the kart. The kart can deaccelerate and jump on any surface recognized by this value.")]
    public LayerMask roadLayers;
    [Tooltip("The max distance a ray will be casted down to check whether the player has touched the ground.")]
    public float maxDownCastDistance = 1;

    [Header("External References")]
    [Tooltip("The kart yaw rotation helper transform. This should generally be a child of Model Normal Rotator.")]
    public Transform modelRotator;
    [Tooltip("The kart pitch and roll helper transform. This Game Object's local up direction will follow the normal of the ground the kart is touching. This should generally be seperated and not be a child of the actual Kart Game Object.")]
    public Transform modelNormalRotator;
    // A reference to the Kart's Rigidbody.
    [HideInInspector] public Rigidbody rb;

    [Header("Debug Controls")]
    [Tooltip("When this is set to true, a sphere gizmo will appear in place of the connected sphere collider")]
    [SerializeField] private bool displayCollider;

    [Header("Input Control")]
    // This value is true when the jump button has been pressed down, and will be reset after the next fixed frame. This value is used to properly pass user input from the Update function to the FixedUpdate function.
    private bool isJumpDown;

    [Header("Optimizing Variables")]
    // This value stores the current local forward speed. Useful for several parts of the code.
    [HideInInspector] public float forwardSpeed;
    // This value stores the velocity of the kart in the horizontal plane. Useful for several parts of the code.
    [HideInInspector] public Vector3 horizontalVelocity;

    // The Start function is run when the script is first enabled - at the start of the scene, when instantiated, or when this component is first enabled. This is run after the Awake function and before any other messages.
    private void Start()
    {
        // Gets a reference to the Rigidbody component.
        rb = GetComponent<Rigidbody>();

        // Initialize values.
        currentRotation = modelRotator.eulerAngles.y;
    }

    // The Update function is run every visual frame. This is best used when updating anything related to rendering/visuals and user input.
    private void Update()
    {
        // Passing the "GetButtonDown" function from the update function to the fixed update function.
        if (Input.GetButtonDown("Jump"))
        {
            isJumpDown = true;
        }
    }

    // The FixedUpdate function is run every fixed frame. This is best used when updating anything related to physics and core game logic.
    private void FixedUpdate() 
    {
        // Pre update.
        forwardSpeed = modelRotator.InverseTransformDirection(rb.velocity).z;
        horizontalVelocity = rb.velocity; horizontalVelocity.y = 0;

        // Car movement.
        DriveForward();
        DriveBackward();
        Deaccelerate();
        Drift();
        Steer();
        Jump();

        // Other physics simulations.
        ApplySideDrag();
        ApplyGravity();

        // Post update.
        ResetInput();
    }

    /// <summary>
    /// This function handles the forward movement of the kart.
    /// </summary>
    private void DriveForward () {
        // If press forward & not over speed limit....
        if (Input.GetAxisRaw("Vertical") > 0 && Mathf.Pow(maxSpeed, 2) > rb.velocity.sqrMagnitude)
        {
            // Add forward force.
            rb.AddForce(driveForce * modelRotator.forward);
        }
    }

    /// <summary>
    /// This function handles the backward movement of the kart.
    /// </summary>
    private void DriveBackward()
    {
        // If press backward & not over speed limit
        if (Input.GetAxisRaw("Vertical") < 0 && Mathf.Pow(maxSpeed, 2) > rb.velocity.sqrMagnitude)
        {
            // Add reverse force.
            rb.AddForce(-reverseForce * modelRotator.forward);
        }
    }

    /// <summary>
    /// This function handles the deacceleration that occurs when trying to move on a different direction / stop to a halt.
    /// </summary>
    private void Deaccelerate()
    {
        // When pressing the other direction (or not pressing), and is touching the road...
        if (Sign(Input.GetAxisRaw("Vertical")) != Mathf.Sign(forwardSpeed)
            && (Physics.Raycast(transform.position, Vector3.down, maxDownCastDistance, roadLayers)))
        {
            // Add deacceleration force.
            rb.AddForce(-deaccelerationAmount * horizontalVelocity);
        }
    }

    /// <summary>
    /// This function handles values for the drifting mechanic.
    /// </summary>
    private void Drift ()
    {
        // Checks if drifting can start (drift button is pressed and is moving forwards).
        bool driftCheck = Input.GetButton("Fire3") && forwardSpeed > 0 && Input.GetAxisRaw("Vertical") > 0;

        // Start drifting...
        if (!isDrifting && driftCheck)
        {
            // When the kart steers left/right...
            if (Input.GetAxisRaw("Horizontal") != 0)
            {
                // Set up drift values.
                driftDirection = Input.GetAxisRaw("Horizontal");
                isDrifting = true;
            }
        } 
        // Stop drifting...
        else if (!driftCheck)
        {
            // Reset drift values.
            driftDirection = 0;
            isDrifting = false;
        }
    }

    /// <summary>
    /// This function handles the steering of the cart.
    /// It calculates the rotations of the kart, whether it is drifting or not.
    /// </summary>
    private void Steer ()
    {
        // The desired rotation change.
        float rotationDelta;
        // The direction of the steering (left or right).
        float steerDirection = Input.GetAxisRaw("Horizontal") * Mathf.Sign(forwardSpeed);

        // Decrease power when driving at a speed above the steerPowerDecreasePoint threshold.
        if (rb.velocity.sqrMagnitude > Mathf.Pow(steerPowerDecreasePoint, 2))
        {
            // Steering power factor based on the speed of the kart and the decrease factor.
            float newSteerPower = Mathf.Lerp(steerPowerDecreasePoint, rb.velocity.magnitude, steerPowerDecreaseFactor);

            // If the kart is drifting...
            if (isDrifting)
            {
                // Calculate the desired rotation change based on the steering power, the decrease factor, and the steer direction.
                rotationDelta = newSteerPower * driftingPower * (steerDirection + driftDirection);
            }
            else
            {
                // Calculate the desired rotation change based on the steering power, the decrease factor, and the steer direction.
                rotationDelta = newSteerPower * steeringPower * steerDirection;
            }
        } 
        else
        {
            // Calculate the desired rotation change based on the steering power, speed, and the steer direction.
            rotationDelta = rb.velocity.magnitude * steeringPower * steerDirection;
        }

        // Reduce the rotation when moving to a halt
        if (Input.GetAxisRaw("Vertical") == 0)
        {
            rotationDelta *= deacceleratonSteerFactor;
        }

        // Set the target rotation-delta to rotation delta, but apply smoothing to it
        targetRotationDelta = Mathf.Lerp(targetRotationDelta, rotationDelta, Time.deltaTime * steerSmoothingAmount);

        // Add the current rotation by target rotation-delta, but also apply smoothing to it (simulating a squared ease-out function)
        currentRotation = Mathf.Lerp(currentRotation, currentRotation + targetRotationDelta, Time.deltaTime * steerSmoothingAmount);
    }

    /// <summary>
    /// This function handles the jumping mechanic.
    /// </summary>
    private void Jump ()
    {
        // When the jump key has just been pressed.
        if (isJumpDown)
        {
            // Cast a ray downwards to check for the ground/road.
            if (Physics.Raycast(transform.position, Vector3.down, maxDownCastDistance, roadLayers))
            {
                // Adds an upwards force to simulate jumping.
                rb.AddForce(modelNormalRotator.up * jumpStrength, ForceMode.Impulse);
            }
        }
    }

    /// <summary>
    /// This function prevents the collider from rolling to the side when turning or hitting objects by adding a counteracting force when moving sideways.
    /// </summary>
    private void ApplySideDrag ()
    {
        // The amount of drag multiplier to give to the kart based on the drag factor and the angle difference between the velocity vector and the forwards vector.
        float dragMultiplier = sideDragFactor * Vector3.Cross(rb.velocity, modelRotator.forward).magnitude / 180f;

        // The direction of the drag force.
        Vector3 dragForceDirection = (Vector3.SignedAngle(rb.velocity, modelRotator.forward, Vector3.up) > 0) ? modelRotator.right : -modelRotator.right;
        dragForceDirection.y = 0;

        // Adds the drag force based on the current velocity and drag multiplier.
        rb.AddForce(horizontalVelocity.magnitude * dragMultiplier * dragForceDirection);
    }

    /// <summary>
    /// This function applies gravity to the kart. Nuff said.
    /// </summary>
    private void ApplyGravity ()
    {
        rb.AddForce(Vector3.down * gravityForce);
    }

    /// <summary>
    /// Resets single frame inputs.
    /// </summary>
    private void ResetInput ()
    {
        isJumpDown = false;
    }

    // The OnDrawGizmos function draws gizmos without requiring the Game Object to be selected. This is useful for debugging and indicating stuff invisible to the player.
    private void OnDrawGizmos()
    {
        if (displayCollider)
        {
            SphereCollider col = GetComponent<SphereCollider>();
            Color clr = Gizmos.color;
            Matrix4x4 mtx = Gizmos.matrix;
            Gizmos.color = Color.green;

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireSphere(Vector3.zero, col.radius);

            Gizmos.matrix = Matrix4x4.TRS(transform.position, Camera.main.transform.rotation, transform.lossyScale);
            Gizmos.DrawWireSphere(Vector3.zero, col.radius);

            Gizmos.matrix = mtx;
            Gizmos.color = clr;
        }
    }

    private int Sign(float f)
    {
        return (f == 0) ? 0 : (f > 0) ? 1 : -1;
    }
}
