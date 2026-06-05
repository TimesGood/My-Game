using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Íæ¼Ò×´Ì¬»ú
public class PlayerStateMachine
{
    public PlayerState currentState { get; private set; }

    //³õÊ¼»¯×´Ì¬»ú
    public void Initialize(PlayerState _startState)
    {
        currentState = _startState;
        currentState.Enter();
    }

    //ÇÐ»»×´Ì¬»ú
    public void ChangeState(PlayerState _newState)
    {
        currentState.Exit();
        currentState = _newState;
        currentState.Enter();
    }
}
