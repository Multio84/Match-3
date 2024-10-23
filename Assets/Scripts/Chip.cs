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
    private GameField gameField;

    private float dragThreshold;  // min sidtance for a chip to move, after which the chip starts swap with it's neighbour
    private float swapDuration;
    private float deathDuration;
    private Vector3 initialPosition;
    private Vector3 startDragPos;
    private bool isDragging = false;
    private bool isMoving = false;
    public bool isInAction = false; // if the chip is in action, it can't be deleted by 3 in row match
    public bool isDead = false;

    void Start()
    {
        gameField = GameMode.Instance.gameField;
        renderCamera = Camera.main;

        swapDuration = gameField.chipSwapDuration;
        dragThreshold = gameField.chipDragThreshold;
        deathDuration = gameField.chipDeathDuration;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (isMoving) return;
        isInAction = true;
        // Запоминаем начальную позицию при нажатии
        initialPosition = transform.position;
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

    void Move(Vector2Int direction)
    {
        if (isMoving) return;  // Избежим одновременных перемещений

        // Находим соседнюю фишку
        Vector2Int targetCell = gameField.GetCellPosition(transform.position) + direction;
        if (!gameField.IsCellInGrid(targetCell)) return;

        Chip otherChip = gameField.GetChip(targetCell);

        if (otherChip != null) {
            // Запускаем корутину для перемещения двух фишек
            StartCoroutine(SwapChips(otherChip));
        }
    }

    public void AnimateDeath()
    {
        StartCoroutine(Death());
    }

    private IEnumerator Death()
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

    IEnumerator SwapChips(Chip otherChip)
    {
        isMoving = true;

        // set positions
        Vector3 startPos = transform.position;
        Vector3 otherStartPos = otherChip.transform.position;

        Vector3 targetPos = otherStartPos;
        Vector3 otherTargetPos = startPos;

        float elapsedTime = 0;

        // animate chips swap
        while (elapsedTime < swapDuration) {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / swapDuration);
            otherChip.transform.position = Vector3.Lerp(otherStartPos, otherTargetPos, elapsedTime / swapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;
        otherChip.transform.position = otherTargetPos;
        
        // update chips array
        gameField.SwapChipsPositions(transform.position, otherChip.transform.position);

        isMoving = false;
        isInAction = false;
    }

}
