using UnityEngine;
using System;


[CreateAssetMenu(fileName = "GameSettings", menuName = "settings/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("Field")]

    [Range(5, 7)] public int fieldWidth = 7;
    [Range(5, 14)] public int fieldHeight = 14;
    public float cellSize = 140;


    [Header("Chip")]

    [Tooltip("Duration of falling chip animation in seconds.")]
    public float chipFallDuration = 0.4f;

    [Tooltip("Duration of falling chip animation in seconds.")]
    public float chipFallStartSpeed = 100f;

    [Tooltip("Gravity for falling chip: falling speed factor.")]
    public float chipFallGravity = 2;

    [Tooltip("Delay in seconds before next chip in a column starts falling.")]
    public float chipsFallDelay = 0.01f;

    [Tooltip("Dragged chip distance after which chip moves by itself.")]
    public float chipDragThreshold { get { return cellSize / 5f; } }

    [Tooltip("Duration in seconds of chip's death animation.")]
    public float chipDeathDuration = 2f;

    [Tooltip("Duration in seconds of chips' swap animation.")]
    public float chipSwapDuration = 0.2f;

    [Tooltip("Duration in seconds before automatic reverse swap, when manual swap didn't lead to match.")]
    public float reverseSwapDelay = 0.15f;


    [Header("Rules")]

    [Tooltip("Number of cells, minimum for match in line.")]
    public int minMatchSize = 3;

    [Tooltip("Number of cells, maximum for match in line.")]
    public int maxMatchSize = 5;


    public event Action OnSettingsChanged;

    private void OnValidate()
    {
        OnSettingsChanged?.Invoke();
    }
}
