using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleSkull : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        bool found = other.TryGetComponent(out SignComponent sign);
        if (found && sign.HasSign("Player"))
            Collect();
    }

    //suppesed to be a coroutine due to animation,
    //but because there is no animation yet, it is just a method
    private void Collect ()
    {
        Registry.ins.skullManager.AddSkull();
        //yield animation
        Destroy(gameObject);
    }
}