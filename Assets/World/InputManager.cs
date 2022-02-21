using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum TypeButton
{
   PlayerMove,
   PlayerJump,
   PlayerSprint,
   PlayerMouseXY,
   PlayerGodMod,
   PlayerSlink
}

public class InputManager : MonoBehaviour
{
    

    [SerializeField]private List<PlayerInput> playerInputs;
    void Awake()
    {
        foreach (var input in playerInputs)
        {
            input.button.Enable();
        }

    }

    public InputAction GetButton(TypeButton type) => playerInputs.Find((x) => x.type == type).button;
    
}

[Serializable]
public struct PlayerInput
{
    public TypeButton type;
    public InputAction button;
    
}