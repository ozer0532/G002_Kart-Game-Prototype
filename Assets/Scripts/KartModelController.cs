using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KartModelController : MonoBehaviour
{
    [Header("Logical Controller Reference")]
    [Tooltip("The Kart Controller component this model is attached to.")]
    public KartController kartController;

    [Header("Body Model Manipulation")]
    [Tooltip("The amount of smoothing given when the kart's roll/yaw changes on the ground.")]
    public float normalRotateSmoothAmount = 5;
    [Tooltip("The amount of smoothing given when the kart's roll/yaw changes in the air.")]
    public float normalOnAirSmoothAmount = 1;
    [Tooltip("The angle of rotation the kart will appear relative to the forward direction visually when drifting.")]
    public float driftRotationAmount = 20;
    [Tooltip("The amount of smoothing towards the rotation the kart will appear relative to the forward direction visually when drifting.")]
    public float driftRotateSmoothAmount = 4;
    // The local up direction based on the normal of the ground the ball is touching.
    private Vector3 upDirection;

    [Header("Wheel Movement")]
    [Tooltip("The maximum angle that the wheels can turn.")]
    public float wheelSteeringAngle = 30;
    [Tooltip("The amount of smoothing given when the wheel turns left/right")]
    public float wheelSteerSmoothing = 20;
    [Tooltip("The speed at which the wheel rotates proportional to the Rigidbody's speed.")]
    public float wheelSpinSpeed = 1;

    [Header("Steering Wheel Movement")]
    [Tooltip("The maximum angle that the wheels can turn.")]
    public float steeringWheelAngle = 30;
    [Tooltip("The amount of smoothing given when the wheel turns left/right")]
    public float steeringWheelSmoothing = 20;

    [Header("External References")]
    [Tooltip("The main model of the kart. This should generally be a child of Model Rotator.")]
    public Transform model;
    [Tooltip("The kart yaw rotation helper transform. This should generally be a child of Model Normal Rotator.")]
    public Transform modelRotator;
    [Tooltip("The kart pitch and roll helper transform. This Game Object's local up direction will follow the normal of the ground the kart is touching. This should generally be seperated and not be a child of the actual Kart Game Object.")]
    public Transform modelNormalRotator;
    [Tooltip("The Transform component of the front left turning axle.")]
    public Transform frontLeftTurningAxle;
    [Tooltip("The Transform component of the front right turning axle.")]
    public Transform frontRightTurningAxle;
    [Tooltip("The Transform component of the front left wheel.")]
    public Transform frontLeftWheel;
    [Tooltip("The Transform component of the front right wheel.")]
    public Transform frontRightWheel;
    [Tooltip("The Transform component of the back left wheel.")]
    public Transform backLeftWheel;
    [Tooltip("The Transform component of the back right wheel.")]
    public Transform backRightWheel;
    [Tooltip("The Transform component of the steering wheel.")]
    public Transform steeringWheel;
    // The position of the model relative to the kart.
    private Vector3 modelOffset;

    // Start is called before the first frame update
    void Start()
    {
        modelOffset = transform.position - kartController.transform.position;
        print(modelOffset);
    }

    // Update is called once per frame
    void Update()
    {
        MoveModel();
        RotateModel();

        SpinWheel();
        TurnWheel();
        TurnSteeringWheel();
    }

    /// <summary>
    /// This function translates the kart model to the physical kart.
    /// </summary>
    private void MoveModel()
    {
        transform.position = kartController.transform.position + modelOffset;
    }

    /// <summary>
    /// This function rotates the kart model with respect to the kart's rotation, and the angle of the ground the kart is on.
    /// </summary>
    private void RotateModel()
    {
        RaycastHit hitInfo;

        // Get kart up direction
        if (Physics.Raycast(kartController.transform.position, Vector3.down, out hitInfo, kartController.maxDownCastDistance, kartController.roadLayers))
        {
            upDirection = Vector3.Lerp(upDirection, hitInfo.normal, normalRotateSmoothAmount * Time.deltaTime);
        }
        else
        {
            upDirection = Vector3.Lerp(upDirection, Vector3.up, normalOnAirSmoothAmount * Time.deltaTime);
        }
        Quaternion normalRotation = Quaternion.FromToRotation(Vector3.up, upDirection);

        float modelTargetRotation = (kartController.isDrifting) ? driftRotationAmount * (Input.GetAxisRaw("Horizontal") + kartController.driftDirection) : 0;
        float modelCurrentRotation = Mathf.LerpAngle(model.localEulerAngles.y, modelTargetRotation, Time.deltaTime * driftRotateSmoothAmount);
        model.localEulerAngles = new Vector3(model.localEulerAngles.x, modelCurrentRotation, model.localEulerAngles.z);
        modelRotator.localEulerAngles = new Vector3(0, kartController.currentRotation, 0);
        modelNormalRotator.rotation = normalRotation;
    }

    private void SpinWheel()
    {
        if (backLeftWheel != null)
        {
            backLeftWheel.Rotate(kartController.forwardSpeed * Time.deltaTime * wheelSpinSpeed, 0, 0, Space.Self);
        }
        if (backRightWheel != null)
        {
            backRightWheel.Rotate(kartController.forwardSpeed * Time.deltaTime * wheelSpinSpeed, 0, 0, Space.Self);
        }
        if (frontLeftWheel != null)
        {
            frontLeftWheel.Rotate(kartController.forwardSpeed * Time.deltaTime * wheelSpinSpeed, 0, 0, Space.Self);
        }
        if (frontRightWheel != null)
        {
            frontRightWheel.Rotate(kartController.forwardSpeed * Time.deltaTime * wheelSpinSpeed, 0, 0, Space.Self);
        }
    }

    private void TurnWheel()
    {
        float targetRotation = Input.GetAxisRaw("Horizontal") * wheelSteeringAngle;
        float currentRotation = frontLeftTurningAxle.localEulerAngles.y;
        float newRotation = Mathf.LerpAngle(currentRotation, targetRotation, wheelSteerSmoothing * Time.deltaTime);

        if (frontLeftTurningAxle != null)
        {
            frontLeftTurningAxle.localEulerAngles = new Vector3(0, newRotation, 0);
        }
        if (frontRightTurningAxle != null)
        {
            frontRightTurningAxle.localEulerAngles = new Vector3(0, newRotation, 0);
        }
    }

    private void TurnSteeringWheel()
    {
        if (steeringWheel != null)
        {
            float targetRotation = -Input.GetAxisRaw("Horizontal") * steeringWheelAngle;
            float currentRotation = steeringWheel.localEulerAngles.y;
            float newRotation = Mathf.LerpAngle(currentRotation, targetRotation, steeringWheelSmoothing * Time.deltaTime);

            steeringWheel.localEulerAngles = new Vector3(0, newRotation, 0);
        }
    }
}
