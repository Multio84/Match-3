using UnityEngine;


public class GameProcessor : MonoBehaviour
{
    GameplayConductor gameplayConductor;


    public void Setup(GameplayConductor gc)
    {
        gameplayConductor = gc;
    }

    private void Awake()
    {
        StartApp();
    }

    void StartApp()
    {
        gameplayConductor.StartGame();
    }
}
