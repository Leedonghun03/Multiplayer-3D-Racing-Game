using UnityEngine;

public class CarNetInfo : MonoBehaviour
{
    [SerializeField] private CameraController cameraController;
    
    [SerializeField] private PlayerInputHandler inputHandler;
    [SerializeField] private CarController carController;
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private WheelCollider[] carWheelColliders;
    [SerializeField] private MeshCollider[] carWheelMeshColliders;

    private void Awake()
    {
        if(!cameraController)
        {
            cameraController = GameObject.Find("Camera").GetComponent<CameraController>();
        }

        if (!inputHandler)
        {
            inputHandler = gameObject.GetComponent<PlayerInputHandler>();
        }

        if (!carController)
        {
            carController = gameObject.GetComponent<CarController>();
        }

        if (!carRigidbody)
        {
            carRigidbody = gameObject.GetComponent<Rigidbody>();
        }
    }

    public void CarSetting(bool isLocal)
    {
        if (isLocal)
        {
            // === 로컬 플레이어 설정 ===
            cameraController.SetTarget(gameObject.transform);
        }
        else
        {
            // === 원격 플레이어 설정 ===
            // 입력 핸들러 비활성화
            if (inputHandler)
            {
                inputHandler.enabled = false;
            }
            
            //CarController 비활성화
            if (carController)
            {
                carController.enabled = false;
            }
            
            // 물리값 비활성화
            if (carRigidbody)
            {
                carRigidbody.isKinematic = true;
                carRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            }

            // 바퀴 컨트롤러 비활성화
            if (carWheelColliders.Length > 0)
            {
                foreach (var wheelCollider in carWheelColliders)
                {
                    wheelCollider.enabled = false;
                }
            }
            
            // 바퀴 매쉬 활성화
            if (carWheelMeshColliders.Length > 0)
            {
                foreach (var wheelMeshColliders in carWheelMeshColliders)
                {
                    wheelMeshColliders.gameObject.SetActive(true);
                }
            }
        }
    }
}
