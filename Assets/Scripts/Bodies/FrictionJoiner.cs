using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrictionJoiner : MonoBehaviour
{
    public bool active;
    [SerializeField] StayChecker stayChecker;

    private FrictionJoint2D joint;

    private void Start()
    {
        joint = GetComponent<FrictionJoint2D>();
        joint.enabled = false;


        stayChecker.EnterEvent += HandleBodyEnter;
        stayChecker.ExitEvent += HandleBodyExit;
    }

    private void OnDestroy()
    {
        stayChecker.EnterEvent -= HandleBodyEnter;
        stayChecker.ExitEvent -= HandleBodyExit;
    }

    private void HandleBodyEnter (Collider2D other)
    {
        if (!active)
            return;

        bool found = other.TryGetComponent(out Rigidbody2D rb);
        if (!found)
            return;

        joint.enabled = true;
        joint.connectedBody = rb;
    }

    private void HandleBodyExit (Collider2D other)
    {
        joint.connectedBody = null;
        joint.enabled = false;
    }
}
