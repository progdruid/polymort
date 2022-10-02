using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CorpseManager : MonoBehaviour
{
    public event System.Action CorpseUpdateEvent = delegate { };

    public GameObject corpsePrefab;

    private List<GameObject> corpses = new List<GameObject>();

    private void Start()
    {
        Registry.ins.corpseManager = this;
        CorpseUpdateEvent();
    }

    public void SpawnCorpse (Vector2 pos, Vector2 startVel)
    {
        GameObject corpse = Instantiate(corpsePrefab, pos, Quaternion.identity);
        corpse.GetComponent<Rigidbody2D>().velocity += startVel;
        corpses.Add(corpse);

        CorpseUpdateEvent();
    }

    public void ClearCorpses ()
    {
        for (int i = corpses.Count - 1; i >= 0; i--)
        {
            Destroy(corpses[i]);
            corpses.RemoveAt(i);
        }

        CorpseUpdateEvent();
    }

    public int GetCorpseCount ()
    {
        return corpses.Count;
    }
}
