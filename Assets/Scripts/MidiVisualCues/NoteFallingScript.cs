//<summary>
// Handles individual note behaviour
//<summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteFallingScript : MonoBehaviour
{
    public float velocity = 1.0f;
    public float maxY = -1f;

    //<summary>
    // Handles falling velocity of note and its destruction
    //<summary>
    void Update()
    {
        transform.Translate(Vector3.down * velocity * Time.deltaTime); // note falls 1 unit length per unit delta time
        float topEdgeY = transform.position.y + (transform.localScale.y * 0.5f);
        if (topEdgeY < maxY)
        {
            Destroy(gameObject);
        }
    }
}
