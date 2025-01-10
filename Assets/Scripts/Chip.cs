using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;


public enum ChipColor
{
    Green,
    Blue,
    Yellow,
    Red,
    Purple,
    White
}


public class Chip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    Camera renderCamera;

    public ChipColor color;
    [HideInInspector] public Vector2Int cellPos;
    Vector3 initialPos;
    Vector3 startDragPos;

    GameField gameField;

    float dragThreshold;  // min sidtance for a chip to move, after which the chip starts swap with it's neighbour
    float deathDuration;
    float fallDuration;
    float fallGravity;
    bool isDragging = false;
    public bool isMoving = false;

    public bool isInAction = false; // if the chip is in action, it can't be deleted by 3 in row match
    public bool isDead = false;

    public event Action OnChipLanded;
    

    void Start()
    {
        gameField = GameMode.Instance.gameField;
        renderCamera = Camera.main;

        dragThreshold = gameField.chipDragThreshold;
        deathDuration = gameField.chipDeathDuration;
        fallDuration = gameField.chipFallDuration;
        fallGravity = gameField.chipFallGravity;
    }


    // ========= POINTER ===========
    public void OnPointerDown(PointerEventData eventData)
    {
        if (isMoving) return;
        isInAction = true;
        // Запоминаем начальную позицию при нажатии
        initialPos = transform.position;
        startDragPos = ScreenToWorldPos(eventData.position);
        isDragging = true;
        //Debug.Log("Pointer DOWN on " + color);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;

        //Debug.Log("Pointer UP on " + color);
    }

    Vector3 ScreenToWorldPos(Vector3 screenPosition)
    {
        // Преобразуем экранные координаты в мировые
        return renderCamera.ScreenToWorldPoint(screenPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        Vector3 currentDragPosition = ScreenToWorldPos(eventData.position);
        Vector3 dragDelta = currentDragPosition - startDragPos;

        //Debug.Log("DragDelta = " + dragDelta.x + ", " + dragDelta.y + "; dragThreshold = " + dragThreshold);

        if (Mathf.Abs(dragDelta.x) > dragThreshold || Mathf.Abs(dragDelta.y) > dragThreshold) {
            if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y)) {
                if (dragDelta.x > dragThreshold) {
                    Move(Vector2Int.right);
                }
                else if (dragDelta.x < -dragThreshold) {
                    Move(Vector2Int.left);
                }
            }
            else {
                if (dragDelta.y > dragThreshold) {
                    Move(Vector2Int.up);
                }
                else if (dragDelta.y < -dragThreshold) {
                    Move(Vector2Int.down);
                }
            }
        }
    }

    // ========= MOVE ===========

    void Move(Vector2Int direction)
    {
        if (isMoving) return;  // Избежим одновременных перемещений

        // Находим соседнюю фишку
        Vector2Int targetCell = gameField.GetCellPosition(transform.position) + direction;
        if (!gameField.IsCellInGrid(targetCell)) return;

        Chip otherChip = gameField.GetChip(targetCell);

        if (otherChip != null) {
            // write swapping positions of chips
            gameField.swappingCells = new Vector2Int[] { cellPos, otherChip.cellPos };

            // Запускаем корутину для перемещения двух фишек
            StartCoroutine(gameField.SwapChips(this, otherChip));
        }
    }


    // ========= DEATH ===========

    public void Kill()
    {
        StartCoroutine(AnimateDeath());
    }

    private IEnumerator AnimateDeath()
    {
        Vector3 startScale = transform.localScale;
        float elapsedTime = 0;

        while (elapsedTime < deathDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsedTime / deathDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        isDead = true;
    }



    // ========= FALL ===========

    public void Fall(Vector3 targetPos)
    {
        StartCoroutine(AnimateFall(targetPos));
    }

    IEnumerator AnimateFall(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;

        float elapsedTime = 0; 

        while (elapsedTime < fallDuration) {
            float t = Mathf.Pow(elapsedTime / fallDuration, fallGravity);
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

        OnChipLanded?.Invoke(); // GameField is counting landed chips
        OnChipLanded = null;    // unsubscrive from landing event
    }

}
