using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

public class FingertipManager : MonoBehaviour
{
    public GameObject fingertipPrefab;

    private XRHandSubsystem handSubsystem;
    private bool initialized = false;

    private Dictionary<XRHandJointID, GameObject> leftHandFingertips = new();
    private Dictionary<XRHandJointID, GameObject> rightHandFingertips = new();

    private readonly XRHandJointID[] fingerTips = new XRHandJointID[]
    {
        XRHandJointID.ThumbTip,
        XRHandJointID.IndexTip,
        XRHandJointID.MiddleTip,
        XRHandJointID.RingTip,
        XRHandJointID.LittleTip
    };

    void Update()
    {
        if (!initialized)
        {
            TryInitializeHandSubsystem();
            return;
        }

        if (handSubsystem.leftHand.isTracked)
            UpdateFingerPositions(handSubsystem.leftHand, leftHandFingertips);

        if (handSubsystem.rightHand.isTracked)
            UpdateFingerPositions(handSubsystem.rightHand, rightHandFingertips);
    }

    void TryInitializeHandSubsystem()
    {
        List<XRHandSubsystem> subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetInstances(subsystems);

        if (subsystems.Count > 0)
        {
            handSubsystem = subsystems[0];
            handSubsystem.Start();

            foreach (var tip in fingerTips)
            {
                leftHandFingertips[tip] = Instantiate(fingertipPrefab);
                rightHandFingertips[tip] = Instantiate(fingertipPrefab);
            }

            initialized = true;
        }
    }

    void UpdateFingerPositions(XRHand hand, Dictionary<XRHandJointID, GameObject> fingertipDict)
    {
        foreach (var tip in fingerTips)
        {
            XRHandJoint joint = hand.GetJoint(tip);
            if (joint.TryGetPose(out Pose pose))
            {
                fingertipDict[tip].transform.SetPositionAndRotation(pose.position, pose.rotation);
            }
        }
    }
}
