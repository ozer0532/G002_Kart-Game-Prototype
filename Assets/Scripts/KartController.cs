using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [Header("Drive Controls")]
    [SerializeField] private float maxSpeed = 20;
    [SerializeField] private float driveForce = 20;
    [SerializeField] private float reverseForce = 10;
    [SerializeField] private float deaccelerationAmount = 3;

    [Header("Steering Controls")]
    [SerializeField] private float steeringPower = 3;
    [SerializeField] private float steerSmoothingAmount = 6;
    [SerializeField] private float steerSmoothingDeaccelerationFactor = 0.1f;
    [SerializeField] private float steerPowerDecreasePoint = 2;
    [SerializeField] private float steerPowerDecreaseFactor = 0.3f;
    private float targetRotationDelta = 0;

    [Header("Drifting Controls")]
    [SerializeField] private float driftingPower = 6;
    private float driftDirection;
    private bool isDrifting;

    [Header("Jump Controls")]
    [SerializeField] private float jumpStrength = 10;

    [Header("Physics Attributes")]
    [SerializeField] private float sideDragFactor = 200;
    [SerializeField] private float gravityForce = 15;
    [SerializeField] private LayerMask roadLayers;
    [SerializeField] private float maxDownCastDistance = 1;
    private float currentRotation;

    [Header("Model Manipulation")]
    [SerializeField] private float normalRotateSmoothAmount = 5;
    [SerializeField] private float normalOnAirSmoothAmount = 1;
    [SerializeField] private float driftRotationAmount = 20;
    [SerializeField] private float driftRotateSmoothAmount = 4;
    private Vector3 upDirection;

    [Header("External References")]
    [SerializeField] private Transform model;
    [SerializeField] private Transform modelRotator;
    [SerializeField] private Transform modelNormalRotator;
    private Vector3 modelOffset;
    private Rigidbody rb;

    [Header("Debug Controls")]
    [SerializeField] private bool displayCollider;

    [Header("Input Control")]
    private bool isJumpDown;

    [Header("Optimizing Variables")]
    private float forwardSpeed;
    private Vector3 horizontalVelocity;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        modelOffset = modelRotator.position - transform.position;
        currentRotation = modelRotator.eulerAngles.y;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            isJumpDown = true;
        }
    }

    private void FixedUpdate() 
    {
        forwardSpeed = modelRotator.InverseTransformDirection(rb.velocity).z;
        horizontalVelocity = rb.velocity; horizontalVelocity.y = 0;

        DriveForward();
        DriveBackward();
        Deaccelerate();
        Drift();
        Steer();
        Jump();

        ApplySideDrag();
        ApplyGravity();

        MoveModel();
        RotateModel();

        ResetInput();
    }

    private void DriveForward () {
        // If press forward & not over speed limit
        if (Input.GetAxisRaw("Vertical") > 0 && Mathf.Pow(maxSpeed, 2) > rb.velocity.sqrMagnitude)
        {
            rb.AddForce(driveForce * modelRotator.forward);
        }
    }

    private void DriveBackward()
    {
        // If press backward & not over speed limit
        if (Input.GetAxisRaw("Vertical") < 0 && Mathf.Pow(maxSpeed, 2) > rb.velocity.sqrMagnitude)
        {
            rb.AddForce(-reverseForce * modelRotator.forward);
        }
    }

    private void Deaccelerate()
    {
        if (Input.GetAxisRaw("Vertical") != Mathf.Sign(forwardSpeed))
        {
            rb.AddForce(-deaccelerationAmount * horizontalVelocity);
        }
    }

    private void Drift ()
    {
        bool driftCheck = Input.GetButton("Fire3") && forwardSpeed > 0 && Input.GetAxisRaw("Vertical") > 0;
        if (!isDrifting && driftCheck)
        {
            if (Input.GetAxisRaw("Horizontal") != 0)
            {
                driftDirection = Input.GetAxisRaw("Horizontal");
                isDrifting = true;
            }
        } 
        else if (!driftCheck)
        {
            driftDirection = 0;
            isDrifting = false;
        }
    }

    private void Steer ()
    {
        float rotationDelta;
        float steerDirection = Input.GetAxisRaw("Horizontal") * Mathf.Sign(forwardSpeed);

        // Increase decrease power when driving fast
        if (rb.velocity.sqrMagnitude > Mathf.Pow(steerPowerDecreasePoint, 2))
        {
            float newSteerPower = Mathf.Lerp(steerPowerDecreasePoint, rb.velocity.magnitude, steerPowerDecreaseFactor);
            // Drifting Control
            if (isDrifting)
            {
                rotationDelta = newSteerPower * driftingPower * (steerDirection + driftDirection);
            }
            else
            {
                rotationDelta = newSteerPower * steeringPower * steerDirection;
            }
        } 
        else
        {
            rotationDelta = rb.velocity.magnitude * steeringPower * steerDirection;
        }

        // Apply steering deacceleration
        if (Input.GetAxisRaw("Vertical") == 0)
        {
            rotationDelta *= steerSmoothingDeaccelerationFactor;
        }

        // Calculate rotation
        targetRotationDelta = Mathf.Lerp(targetRotationDelta, rotationDelta, Time.deltaTime * steerSmoothingAmount);
        currentRotation = Mathf.Lerp(currentRotation, currentRotation + targetRotationDelta, Time.deltaTime * steerSmoothingAmount);
    }

    private void Jump ()
    {
        if (isJumpDown)
        {
            if (Physics.Raycast(transform.position, Vector3.down, maxDownCastDistance, roadLayers))
            {
                rb.AddForce(modelNormalRotator.up * jumpStrength, ForceMode.Impulse);
            }
        }
    }

    private void ApplySideDrag ()
    {
        float dragMultiplier = sideDragFactor * Vector3.Cross(rb.velocity, modelRotator.forward).magnitude / 180f;
        Vector3 dragForceDirection = (Vector3.SignedAngle(rb.velocity, modelRotator.forward, Vector3.up) > 0) ? modelRotator.right : -modelRotator.right;
        dragForceDirection.y = 0;
        rb.AddForce(horizontalVelocity.magnitude * dragMultiplier * dragForceDirection);
    }

    private void ApplyGravity ()
    {
        rb.AddForce(Vector3.down * gravityForce);
    }

    private void MoveModel ()
    {
        modelNormalRotator.position = transform.position + modelOffset;
    }

    private void RotateModel ()
    {
        RaycastHit hitInfo;

        // Get kart up direction
        if (Physics.Raycast(transform.position, Vector3.down, out hitInfo, maxDownCastDistance, roadLayers))
        {
            upDirection = Vector3.Lerp(upDirection, hitInfo.normal, normalRotateSmoothAmount * Time.deltaTime);
        } 
        else
        {
            upDirection = Vector3.Lerp(upDirection, Vector3.up, normalOnAirSmoothAmount * Time.deltaTime);
        }
        Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, upDirection);

        float modelTargetRotation = (isDrifting) ? driftRotationAmount * (Input.GetAxisRaw("Horizontal") + driftDirection) : 0;
        float modelCurrentRotation = Mathf.LerpAngle(model.localEulerAngles.y, modelTargetRotation, Time.deltaTime * driftRotateSmoothAmount);
        model.localEulerAngles = new Vector3(model.localEulerAngles.x, modelCurrentRotation, model.localEulerAngles.z);
        modelRotator.localEulerAngles = new Vector3(0, currentRotation, 0);
        modelNormalRotator.rotation = normalRotation;
    }

    private void ResetInput ()
    {
        isJumpDown = false;
    }

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
}
