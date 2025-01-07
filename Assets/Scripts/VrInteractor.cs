using System.Collections;
using System.Collections.Generic;
using UnityEngine;

interface VrInteractable
{
    public void Interact();
}
public class VrInteractor : MonoBehaviour
{
    public Transform InteractorSource;
    public float InteractorRange;
    private XRIDefaultInputActions controls;
    
    void Start()
    {
        controls = new XRIDefaultInputActions();
        controls.Enable();
    }

    void Update()
    {
        //check for mouse button press
        if(/*Input.GetMouseButtonDown(0)*/ controls.XRIRightHandInteraction.UIPress.WasPressedThisFrame())
        {
            Ray r = new Ray(InteractorSource.position, InteractorSource.forward);
            if(Physics.Raycast(r,out RaycastHit hit, InteractorRange))
            {
                if(hit.collider.TryGetComponent<VrInteractable>(out VrInteractable interactableObject))
                {
                    interactableObject.Interact();
                }
            }
        }
    }
}
