using UnityEngine;


public class GameProcessor : MonoBehaviour
{
    LevelGenerator levelGenerator;


    public void Setup(LevelGenerator lg)
    {
        if (lg == null)
        {
            Debug.LogError("GameProcessor: LevelGenerator is null.");
        }

        levelGenerator = lg;
    }

    private void Awake()
    {
        StartGame();
    }

    void StartGame()
    {
        levelGenerator.GenerateLevel();
    }
}
