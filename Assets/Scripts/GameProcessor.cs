using UnityEngine;


[DefaultExecutionOrder(1000)]
public class GameProcessor : MonoBehaviour
{
    LevelGenerator levelGenerator;


    public void Setup(LevelGenerator lg)
    {
        this.levelGenerator = lg;
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
