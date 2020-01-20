using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStatsManager : MonoBehaviour
{
    public static PlayerStatsManager _PlayerStats;

    void Awake()
    {
        if (FindObjectsOfType(GetType()).Length > 1)
        {
            Destroy(gameObject);
        }

        if (PlayerStatsManager._PlayerStats == null)
        {
            PlayerStatsManager._PlayerStats = this;
        }
        else if (PlayerStatsManager._PlayerStats == this)
        {
            Destroy(PlayerStatsManager._PlayerStats.gameObject);
            PlayerStatsManager._PlayerStats = this;
        }

        DontDestroyOnLoad(this.gameObject);

        InitializeStats();
    }

    private void OnLevelWasLoaded(int level)
    {
        InitializeStats();
    }

    public void InitializeStats()
    {

    }
}
