using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] GameObject playerPrefab;

    public event System.Action<Player> PlayerSpawnEvent = delegate { };

    public Player player { get; private set; }
    private Vector2 spawnPoint = Vector2.zero;

    private void OnEnable() => Registry.ins.playerManager = this;
    private void Start()
    {
        Registry.ins.inputSystem.KillPlayerKeyPressEvent += KillPlayer;
    }
    private void OnDestroy()
    {
        Registry.ins.inputSystem.KillPlayerKeyPressEvent -= KillPlayer;
    }

    public void SetSpawnPoint (Vector2 pos)
    {
        spawnPoint = pos;
    }

    public void SpawnPlayer ()
    {
        if (player != null)
        {
            #if UNITY_EDITOR 
            Debug.LogError("There is already one instance of player.");
            #endif
            
            return;
        }

        player = Instantiate(playerPrefab, new Vector3(spawnPoint.x, spawnPoint.y, -1f), Quaternion.identity).GetComponent<Player>();
        PlayerSpawnEvent(player);
    }

    public void KillPlayer ()
    {
        player.PlayDeathAnimation();

        if (Registry.ins.skullManager.GetSkullsAmount() == 0)
        {
            Registry.ins.lm.ReloadLevel();
        }
        else
        {
            StartCoroutine(KillPlayerRoutine());
        }
    }

    private IEnumerator KillPlayerRoutine()
    {
        yield return Registry.ins.tc.TransiteIn();

        Registry.ins.skullManager.DestroySkull();
        Vector3 pos = player.transform.position;
        DestroyPlayer();
        SpawnPlayer();
        Registry.ins.corpseManager.SpawnCorpse(pos, Vector2.zero);

        yield return Registry.ins.tc.TransiteOut();
    }

    public void DestroyPlayer ()
    {
        Destroy(player.gameObject);
        player = null;
    }
}