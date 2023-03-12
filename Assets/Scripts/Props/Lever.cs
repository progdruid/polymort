using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SignalActivator))]
public class Lever : MonoBehaviour
{
    private SignalActivator signal;
    private Animator animator;

    private bool pulled;

    private void Start ()
    {
        signal = GetComponent<SignalActivator>();
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        bool found = other.TryGetComponent(out SignComponent sign);
        if (!found || !sign.HasSign("Body"))
            return;

        pulled = !pulled;
        animator.SetTrigger("Pulled");
        signal.UpdateActivation(pulled, gameObject);
    }
}
