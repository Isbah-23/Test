using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteFallingScript : MonoBehaviour
{
    public float velocity = 1.0f;
    void Update()
    {
        transform.Translate(Vector3.down * velocity * Time.deltaTime);
    }
}
