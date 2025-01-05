using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoteFallingScript : MonoBehaviour
{
    public float velocity = 1.0f;
    public float maxY = -1f;

    void Update()
    {
        transform.Translate(Vector3.down * velocity * Time.deltaTime);
        float topEdgeY = transform.position.y + (transform.localScale.y * 0.5f);
        if (topEdgeY < maxY)
        {
            Destroy(gameObject);
        }
    }
}
