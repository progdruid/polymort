using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cannon : MonoBehaviour
{
    [SerializeField] SignalActivator signal;
    public float timePeriod;
    public GameObject ProjectilePrefab;

    private Vector3 relativeProjSpawnPoint = new Vector3(0f, 0.25f, -1f);
    private Animator animator;
    private float timePassed;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (signal != null && signal.activated)//cuz by activating signal you're disabling cannons
            return;

        if (timePassed < timePeriod)
            timePassed += Time.deltaTime;
        else
        {
            timePassed = 0;
            Shoot();
        }
    }

    private void Shoot ()
    {
        if (animator != null)
            animator.SetTrigger("Shot");
        Transform proj = Instantiate(ProjectilePrefab, transform, false).transform;
        proj.localPosition = relativeProjSpawnPoint;

    }
}
