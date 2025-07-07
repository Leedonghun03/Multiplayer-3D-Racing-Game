using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    
    [SerializeField] private Vector3 offset;
    [SerializeField] private float moveSmooth;
    
    void FixedUpdate()
    {
        FollowTarget();
    }

    public void SetTarget(Transform playerCar)
    {
        target = playerCar;
    }
    
    private void FollowTarget()
    {
        if (!target)
        {
            return;
        }
        
        Vector3 targetPoint = target.position + target.rotation * offset;
        
        transform.position = Vector3.Lerp(transform.position, targetPoint, moveSmooth * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, moveSmooth * Time.deltaTime);
    }
}
