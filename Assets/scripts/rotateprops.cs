using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateprops : MonoBehaviour
{
   
    // Public variable to control rotation speed
    public Vector3 rotationSpeed = new Vector3(0, 100, 0);

    void Update()
    {
        // Rotate the object around its local axes based on the rotationSpeed
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}
