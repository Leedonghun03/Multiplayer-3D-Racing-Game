using UnityEngine;

public class CarMotor : MonoBehaviour
{
    private PlayerInputHandler playerInputHandler;
    
    // 최대 가속
    public float maxAcceleration;
    // 최대 속도
    public float maxSpeed;
    // 브레이크 감속
    public float breakDecel;

    private Rigidbody carRigidbody;
    
    // 스로틀(엑셀)
    float throttle;
    
    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        playerInputHandler = GetComponent<PlayerInputHandler>();
    }

    void Update()
    {
        throttle = playerInputHandler.throttle.y;
    }

    private void FixedUpdate()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);

        // 직진
        if (throttle > 0)
        {
            float carSpeed = throttle * maxAcceleration;
            
            // 최대 속도를 넘었는지 확인
            if (maxSpeed > carSpeed)
            {
                carRigidbody.AddRelativeForce(forward * carSpeed);
            }
        }
        
        // 후진
        if (throttle < 0)
        {
            float carSpeed = throttle * maxAcceleration;
            
            carRigidbody.AddRelativeForce(forward * carSpeed);
        }
    }
}
