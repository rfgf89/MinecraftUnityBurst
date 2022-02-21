using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class HeadController : MonoBehaviour
{
    public float mouseSense;

    public Transform playerBody;
    public Transform mouseCube;
    
    [SerializeField]private InputManager inputManager;
    
    [Header("Mouse composite")]
    private InputAction mouseButton;
    [SerializeField]
    [LabelOverride("Mouse Action")]
    private TypeButton typeButtonMouse;
    
    private float xRotation = 0f;
    private Vector3 hitPoint;
    private RaycastHit hit;
    public LayerMask layerMask;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        mouseButton = inputManager.GetButton(typeButtonMouse);
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = mouseButton.ReadValue<Vector2>().x * mouseSense ;
        float mouseY = mouseButton.ReadValue<Vector2>().y * mouseSense ;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90,90);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);

        if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 128f, layerMask))
        {
            hitPoint = transform.position+transform.TransformDirection(Vector3.forward) * (hit.distance + 0.01f);
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * (hit.distance + 0.5f), Color.red);
        }

        Vector3 posBlock = new Vector3(Mathf.Floor(hit.point.x+hit.normal.x/2.0f), 
            Mathf.Floor(hit.point.y+hit.normal.y/2.0f), Mathf.Floor(hit.point.z+hit.normal.z/2.0f));
        

        mouseCube.transform.position = posBlock+new Vector3(0.5f,0.5f,0.5f);
        
    }
}
