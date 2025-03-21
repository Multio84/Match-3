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
public abstract class Chip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    protected Camera renderCamera;
    protected Vector3 startDragPos;
    protected SpriteRenderer sr;
    protected GameField gameField;

    public ChipColor Color;
    public Vector2Int CellPos { get; set; }

    // the chip is invisible when it's created while playing, until it has finished appearing
    bool isVisible = false;
    public bool IsVisible
    {
        get => isVisible;
        set
        {
            isVisible = value;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            Color color = sr.color;
            sr.color = isVisible ?
                new Color(color.r, color.g, color.b, 1) :
                new Color(color.r, color.g, color.b, 0);
        }
    }
    public bool IsSwapping { get; set; }
    public bool IsInAction { get; set; } // if the chip is in any action, it can't be deleted in match
    public bool IsMatched { get; set; }  // if was marked as a part of some match

    protected bool isDragging = false;
    protected float dragThreshold;  // min sidtance for a chip to move, after which the chip starts swap with it's neighbour
    protected float deathDuration;
    protected float fallDuration;
    protected float fallGravity;
    protected float distanceToAppear;

    public event Action OnChipLanded;


    public virtual void Init(GameField gf, Vector2Int cellPos)
    {
        gameField = gf;
        renderCamera = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        dragThreshold = gameField.chipDragThreshold;
        deathDuration = gameField.chipDeathDuration;
        fallDuration = gameField.chipFallDuration;
        fallGravity = gameField.chipFallGravity;
        distanceToAppear = gameField.cellSize;
        CellPos = cellPos;
        IsVisible = false;
        IsSwapping = false;
    }

    public event Action<Chip> OnDeathCompleted;

    public void Die()
    {
        StartCoroutine(AnimateDeath());
    }

    protected abstract IEnumerator AnimateDeath();

    protected void NotifyDeathCompleted()
    { 
        OnDeathCompleted?.Invoke(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsSwapping) return;
        IsInAction = true;
        startDragPos = ScreenToWorldPos(eventData.position);
        isDragging = true;
        //Debug.Log("Pointer DOWN on " + Color);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        //Debug.Log("Pointer UP on " + Color);
    }

    Vector3 ScreenToWorldPos(Vector3 screenPosition)
    {
        return renderCamera.ScreenToWorldPoint(screenPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        if (IsSwapping) return;

        Vector3 currentDragPosition = ScreenToWorldPos(eventData.position);
        Vector3 dragDelta = currentDragPosition - startDragPos;
        Vector2Int direction = Vector2Int.zero;

        if (Mathf.Abs(dragDelta.x) > dragThreshold || Mathf.Abs(dragDelta.y) > dragThreshold)
        {
            if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y))
            {
                if (dragDelta.x > dragThreshold)
                {
                    direction = Vector2Int.right;
                }
                else if (dragDelta.x < -dragThreshold)
                {
                    direction = Vector2Int.left;
                }
            }
            else
            {
                if (dragDelta.y > dragThreshold)
                {
                    direction = Vector2Int.up;
                }
                else if (dragDelta.y < -dragThreshold)
                {
                    direction = Vector2Int.down;
                }
            }

            gameField.swapManager.Swap(this, direction, false);
        }
    }

    public void Fall(Vector3 targetPos)
    {
        if (!IsVisible) StartCoroutine(AnimateAppearance());

        StartCoroutine(AnimateFall(targetPos));
    }

    IEnumerator AnimateFall(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;

        float elapsedTime = 0;

        while (elapsedTime < fallDuration)
        {
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

        while (distanceTraveled < distanceToAppear)
        {
            distanceTraveled = Mathf.Abs(transform.position.y - startPos.y);
            float alpha = Mathf.Clamp01(distanceTraveled / distanceToAppear);

            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);

            yield return null;
        }

        IsVisible = true;
    }
}
