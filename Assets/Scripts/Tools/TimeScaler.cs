using UnityEngine;


public class TimeScaler : MonoBehaviour
{
    [Range(0.03f, 1.0f)] public float timeCoef;


    private void Start()
    {
        Time.timeScale = timeCoef;
    }
}