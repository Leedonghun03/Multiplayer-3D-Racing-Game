using System;
using UnityEngine;

public class Suspension : MonoBehaviour
{
    [SerializeField] private Rigidbody carRigidbody;
    [SerializeField] private Transform[] tireTransform;
    [SerializeField] private Transform[] rayPoints;

    [Header("서스펜션 세팅")] 
    [SerializeField] private float springStiffness;     // 스프링 강성
    [SerializeField] private float restLenght;          // 서스펜션 무하중 길이
    [SerializeField] private float springTravel;        // 스프링 길이
    [SerializeField] private float tireRadius;          // 타이어 반지름

    [SerializeField] private float damperC;     // 감쇠 계수(댐퍼 점성)
    
    private void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        SuspensionSystem();
    }

    private void SuspensionSystem()
    {
        foreach (var rayPoint in rayPoints)
        {
            RaycastHit hit;
            float maxLength = restLenght + springTravel;

            if (Physics.Raycast(rayPoint.position, -rayPoint.up, out hit, maxLength + tireRadius))
            {
                float currentSpringLenth = hit.distance - tireRadius;
                float springCompression = (restLenght - currentSpringLenth) / springTravel;
                
                float springVelocity = Vector3.Dot(carRigidbody.GetPointVelocity(rayPoint.position), rayPoint.up);
                float dampForce = damperC * springCompression;

                float springForce = springStiffness * springCompression;

                float netForce = springForce - dampForce;

                carRigidbody.AddForceAtPosition(springForce * rayPoint.up, rayPoint.position);
                
                Debug.DrawLine(rayPoint.position, hit.point, Color.red);
            }
            else
            {
                Debug.DrawLine(rayPoint.position, rayPoint.position + (tireRadius + maxLength) * -rayPoint.up, Color.green);
            }
        }
    }
}