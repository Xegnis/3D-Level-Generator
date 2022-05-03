using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class RingRotator : MonoBehaviour
{
    [FormerlySerializedAs("rotation")] public Vector3 Rotation;
    
    void Update()
    {
        transform.rotation *= Quaternion.Euler(Rotation * Time.deltaTime);
    }
}