using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PCInputSet : InputSet
{
    void Update()
    {
        if (!Active)
            return;

        if (CanWalk)
            HorizontalValue = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.Space) && CanJump)
            InvokeJumpKeyPressEvent();
        else if (Input.GetKeyUp(KeyCode.Space))
            InvokeJumpKeyReleaseEvent();

        //player kill key is temporary
        //will not be in the final game
        //exists only for testing
        if (Input.GetKeyDown(KeyCode.E))
            InvokeKillPlayerKeyPressEvent();
        if (Input.GetKeyDown(KeyCode.R))
            InvokeReloadActivationEvent();
        if (Input.GetKeyDown(KeyCode.Q))
            InvokeQuitActivationEvent();

    }
}
