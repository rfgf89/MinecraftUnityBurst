
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    
    
    private Vector3 velocity;

    [SerializeField]private WorldController world;
    
    public float gravity = 9.807f;
    public float speed;
    public float jumpPower = 3f;
    public bool goodmode = false;
    
    public CharacterController controller;
    public Transform groundCheck;
    public float groundDistance = 0.5f;
    public LayerMask groundMask;

    public bool isGrounded;
    
    [SerializeField]private InputManager inputManager;
    
    
    [Header("Move Button")]
    private InputAction movementButton;
    [SerializeField]
    [LabelOverride("Move Composite Button")]
    private TypeButton typeButtonMove;
    
 
    private InputAction sprintButton;
    [SerializeField]
    [LabelOverride("Sprint Button")]
    private TypeButton typeButtonSprint;
    

    private InputAction jumpButton;
    [SerializeField]
    [LabelOverride("Jump Button")]
    private TypeButton typeButtonJump;

   
    private InputAction slinkButton;
    [SerializeField]
    [LabelOverride("Slink Button")]
    private TypeButton typeButtonSlink;
    
  
    private InputAction godButton;
    [SerializeField]
    [LabelOverride("God Button")]
    private TypeButton typeButtonGod;
   
    private void Start()
    {
        movementButton = inputManager.GetButton(typeButtonMove);
        sprintButton = inputManager.GetButton(typeButtonSprint);
        jumpButton = inputManager.GetButton(typeButtonJump);
        godButton = inputManager.GetButton(typeButtonGod);
        slinkButton = inputManager.GetButton(typeButtonSlink);
    }

    void Update()
    {
       //float3 pos = new float3(
        //    gameObject.transform.position.x/ WorldController.chunksSize.x,0, gameObject.transform.position.z/ WorldController.chunksSize.z) ;
       // Chunk chunk = world.GiveClaster(pos).ClasterGetCell(pos);

        //if (chunk == null || (chunk.handler != null && (chunk.handler.chunk == chunk &&  chunk.handler.computing)))
        //return;
        
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2.0f;
        
        
        float x = movementButton.ReadValue<Vector2>().x;
        float z = movementButton.ReadValue<Vector2>().y;
        Vector3 move = Vector3.zero;
        
        if (godButton.IsPressed())
            goodmode = !goodmode;
        
        
        if (!goodmode)
        {
            
        if (sprintButton.IsPressed())
            move = transform.right * x + transform.forward * z * 5.0f;
        else
            move = transform.right * x + transform.forward * z;
        
        
            controller.Move(move * speed * Time.deltaTime);
            
            if (jumpButton.IsPressed() && isGrounded)
                velocity.y = Mathf.Sqrt(jumpPower * +2f * gravity)*3f;
            
           
                velocity.y -= ((gravity) * Time.deltaTime)*3f;
                controller.Move(velocity * Time.deltaTime);
            }
        else
        {
            
            controller.transform.Translate( Vector3.forward*z+ Vector3.right*x+ Vector3.up*((jumpButton.ReadValue<float>()-slinkButton.ReadValue<float>())*5f)* speed * Time.deltaTime*5f);
        }
      
       

        
    }
}
