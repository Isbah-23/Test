using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControllerModelFix : MonoBehaviour
{
    void Start()
    {
        // Ensure consistent rendering for both eyes
        if (Camera.main.stereoTargetEye == StereoTargetEyeMask.Both)
        {
            Debug.Log("Adjusting controller prefab for stereo rendering.");
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}

