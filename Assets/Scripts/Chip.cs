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

[RequireComponent(typeof(SpriteRenderer))]
public class Chip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    Camera renderCamera;

    public ChipColor Color;
    public Vector2Int CellPos;
    Vector3 startDragPos;
    SpriteRenderer sr;
    [HideInInspector]
    // the chip is invisible when it's created while playing, until it has finished appearing
    bool isVisible = false;
    public bool IsVisible 
    { 
        get => isVisible;
        set {
            isVisible = value;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            Color color = sr.color;
            sr.color = isVisible ?
                new Color(color.r, color.g, color.b, 1) :
                new Color(color.r, color.g, color.b, 0);
        }
    }

    GameField gameField;

    float dragThreshold;  // min sidtance for a chip to move, after which the chip starts swap with it's neighbour
    float deathDuration;
    float fallDuration;
    float fallGravity;
    float distanceToAppear;
    bool isDragging = false;

    public bool IsMoving = false;
    public bool IsInAction = false; // if the chip is in any action, it can't be deleted in match
    public bool IsDead = false;
    public bool IsMatched = false;  // if was marked as a part of some match

    public event Action OnChipLanded;
    public event Action<Chip> OnDeathCompleted;



    void Awake()
    {
        gameField = GameMode.Instance.gameField;
        renderCamera = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        dragThreshold = gameField.chipDragThreshold;
        deathDuration = gameField.chipDeathDuration;
        fallDuration = gameField.chipFallDuration;
        fallGravity = gameField.chipFallGravity;
        distanceToAppear = gameField.cellSize;
        IsVisible = false;
    }


    // ========= POINTER ===========
    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsMoving) return;
        IsInAction = true;
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
                    Swap(Vector2Int.right);
                }
                else if (dragDelta.x < -dragThreshold) {
                    Swap(Vector2Int.left);
                }
            }
            else {
                if (dragDelta.y > dragThreshold) {
                    Swap(Vector2Int.up);
                }
                else if (dragDelta.y < -dragThreshold) {
                    Swap(Vector2Int.down);
                }
            }
        }
    }


    // ========= SWAP ===========

    void Swap(Vector2Int direction)
    {
        if (IsMoving) return;

        // find adjacent chip to swap with this
        Vector2Int targetCell = gameField.GetCellGridPosition(transform.position) + direction;
        if (!gameField.IsCellInField(targetCell.x, targetCell.y)) return;
        Chip swappedChip = gameField.GetChip(targetCell);

        if (swappedChip is not null) {
            gameField.draggedChip = this;
            gameField.swappedChip = swappedChip;
            gameField.SwapChips(false);
        }
    }


    // ========= DEATH ===========

    public void Die()
    {
        StartCoroutine(AnimateDeath());
    }

    private IEnumerator AnimateDeath()
    {
        if (this is null || gameObject is null) {
            Debug.LogWarning("Trying to animate dead chip. Step 1.");
            yield break;
        }

        Vector3 startScale = transform.localScale;
        float elapsedTime = 0;

        while (elapsedTime < deathDuration) {
            if (this is null || gameObject is null) {
                Debug.LogWarning("Trying to animate dead chip. Step 2.");
                yield break;
            }

            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsedTime / deathDuration);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        IsDead = true;
        OnDeathCompleted?.Invoke(this);
    }


    // ========= FALL ===========

    public void Fall(Vector3 targetPos)
    {
        if (!IsVisible) StartCoroutine(AnimateAppearance());

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

    IEnumerator AnimateAppearance()
    {
        Vector3 startPos = transform.position;
        float distanceTraveled = 0f;
        Color originalColor = sr.color;

        while (distanceTraveled < distanceToAppear) {
            distanceTraveled = Mathf.Abs(transform.position.y - startPos.y);
            float alpha = Mathf.Clamp01(distanceTraveled / distanceToAppear);

            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null;
        }

        IsVisible = true;
    }

}
