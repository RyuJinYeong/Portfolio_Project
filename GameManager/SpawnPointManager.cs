using UnityEngine;
using System.Collections.Generic;

public class SpawnPointManager : MonoBehaviour
{
    public static SpawnPointManager Instance { get; private set; }

    public Transform[] allyFrontRowSpawnPoints;
    public Transform[] allyBackRowSpawnPoints;
    public Transform[] enemyFrontRowSpawnPoints;
    public Transform[] enemyBackRowSpawnPoints;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Transform[] GetAllySpawnPoints(int frontCount, int backCount)
    {
        List<Transform> spawnPoints = new List<Transform>();
        List<int> frontPositions = GetPositions(frontCount);
        List<int> backPositions = GetPositions(backCount);

        foreach (int pos in frontPositions)
        {
            spawnPoints.Add(allyFrontRowSpawnPoints[pos]);
        }
        foreach (int pos in backPositions)
        {
            spawnPoints.Add(allyBackRowSpawnPoints[pos]);
        }
        return spawnPoints.ToArray();
    }

    public Transform[] GetEnemySpawnPoints(int frontCount, int backCount)
    {
        List<Transform> spawnPoints = new List<Transform>();
        List<int> frontPositions = GetPositions(frontCount);
        List<int> backPositions = GetPositions(backCount);

        foreach (int pos in frontPositions)
        {
            spawnPoints.Add(enemyFrontRowSpawnPoints[pos]);
        }
        foreach (int pos in backPositions)
        {
            spawnPoints.Add(enemyBackRowSpawnPoints[pos]);
        }
        return spawnPoints.ToArray();
    }

    private List<int> GetPositions(int count)
    {
        switch (count)
        {
            case 1: return new List<int> { 2 };
            case 2: return new List<int> { 1, 3 };
            case 3: return new List<int> { 0, 2, 4 };
            case 4: return new List<int> { 0, 1, 3, 4 };
            case 5: return new List<int> { 0, 1, 2, 3, 4 };
            default: return new List<int>();
        }
    }
}
