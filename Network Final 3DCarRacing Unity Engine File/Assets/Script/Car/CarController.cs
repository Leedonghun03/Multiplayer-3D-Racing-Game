using TMPro;
using UnityEngine;

public class CarController : MonoBehaviour
{
    public UIManager uiManager;
    
    private PlayerInputHandler playerInputHandler;
    private Rigidbody carRigidbody;
    
    private enum DriveType { RWD, FWD, AWD }
    [Header("차량 구동 방식")]
    [SerializeField] private DriveType driveType;
    
    [Header("가속 정보")]
    [SerializeField] private float maxAcceleration = 1500;  // 가속 강도
    [SerializeField] private float maxSpeedKmh = 250;       // 최고 가속
    [SerializeField] private float speedKmh = 0;            // 현재 속도
    [SerializeField] private TextMeshProUGUI dashBoardText;  // 계기판 UI
    
    [Header("브레이크 정보")]
    [SerializeField] private float brakeDecel = 80000;      // 브레이크 강도
    
    [Header("조향 정보")]
    [SerializeField] private float maxSteerAngle = 30;      // 최대 조향값
    
    [Header("타이어 정보")]
    [SerializeField] public WheelCollider[] wheelColliders;
    [SerializeField] public Transform[] wheelTransforms;
    
    private void Start()
    {
        if (!playerInputHandler)
        {
            playerInputHandler = GetComponent<PlayerInputHandler>();
        }

        if (!carRigidbody)
        {
            carRigidbody = GetComponent<Rigidbody>();
        }

        uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        if (uiManager && !dashBoardText)
        {
            this.dashBoardText = uiManager.dashBoardText;
        }
    }

    private void FixedUpdate()
    {
        speedKmh = carRigidbody.linearVelocity.magnitude * 3.6f;
        if (speedKmh > maxSpeedKmh)
        {
            carRigidbody.linearVelocity = carRigidbody.linearVelocity.normalized * (maxSpeedKmh / 3.6f);
        }
        CarMotorAcceling(driveType);
        dashBoardText.text = $"{speedKmh : 0} km/h";

        CarBrake(playerInputHandler.brake);
        CarSteer(playerInputHandler.steer.x);

        for (int i = 0; i < wheelColliders.Length; i++)
        {
            UpdateWheelPose(wheelColliders[i],  wheelTransforms[i]);
        }
    }

    private void CarMotorAcceling(DriveType type)
    {
        switch (type)
        {
            case DriveType.RWD:
                wheelColliders[2].motorTorque = maxAcceleration * playerInputHandler.throttle.y;
                wheelColliders[3].motorTorque = maxAcceleration * playerInputHandler.throttle.y;
                break;
            
            case DriveType.FWD:
                wheelColliders[0].motorTorque = maxAcceleration * playerInputHandler.throttle.y;
                wheelColliders[1].motorTorque = maxAcceleration * playerInputHandler.throttle.y;
                break;
            
            case DriveType.AWD:
                wheelColliders[0].motorTorque = maxAcceleration * playerInputHandler.throttle.y * 0.5f;
                wheelColliders[1].motorTorque = maxAcceleration * playerInputHandler.throttle.y * 0.5f;
                wheelColliders[2].motorTorque = maxAcceleration * playerInputHandler.throttle.y * 0.5f;
                wheelColliders[3].motorTorque = maxAcceleration * playerInputHandler.throttle.y * 0.5f;
                break;
        }
    }

    private void CarBrake(bool brakeInput)
    {
        float braking =  brakeInput ? brakeDecel : 0;
        wheelColliders[0].brakeTorque = braking;
        wheelColliders[1].brakeTorque = braking;
        wheelColliders[2].brakeTorque = braking;
        wheelColliders[3].brakeTorque = braking;
    }

    private void CarSteer(float steerInput)
    {
        float steerAngle = maxSteerAngle * steerInput;
        wheelColliders[0].steerAngle = steerAngle;
        wheelColliders[1].steerAngle = steerAngle;
    }

    private static void UpdateWheelPose(WheelCollider wheelCollider, Transform mesh)
    {
        if (!mesh)
        {
            return;
        }

        wheelCollider.GetWorldPose(out var pos, out var rot);

        mesh.position = pos;
        mesh.rotation = rot;
    }
}
