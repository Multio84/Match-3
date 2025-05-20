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

public enum ChipState
{ 
    Idle,       // is not in any action, available for interaction
    Blocked,    // unavailable for interaction
    Falling,
    Dragging,   // being dragged by player
    Swapping,   // in automatic swap process
    Swapped,    // had been swapped, but swap hadn't been stopped yet
    Destroying
}

[RequireComponent(typeof(SpriteRenderer))]
public abstract class Chip : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    protected GameSettings settings;
    protected GameField gameField;
    protected SwapHandler swapHandler;
    protected Camera renderCamera;
    protected SpriteRenderer sr;
    public ChipDebugger debugger;

    public ChipColor Color;
    private ChipState state;
    private ChipState State
    {
        get => state;
        set
        {
            state = value;
            if (debugger is null)
            {
                Debug.LogError("Debugger is null.");
                return;
            }
            debugger.UpdateState(value);
        }
    }
    protected Vector3 startDragPos;
    public Vector2Int Cell { get; set; }

    public bool IsSwapping; // if chip is in process of swap
    public bool IsInAction; // if the chip is in any action, it can't be deleted in match
    public bool IsMatched;  // if was marked as a part of some match
    protected bool isDragging = false;
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

    protected float dragThreshold;  // min sidtance for a chip to move, after which the chip starts swapping with it's neighbour
    protected float deathDuration;
    protected float startFallSpeed;
    protected float fallGravity;
    protected float distanceToAppear;

    public event Action<Chip> OnChipLanded;
    public event Action<Chip> OnDeathCompleted;
    //public event Action<Chip, Vector2Int, bool> SwapRequested;

    //float fallDuration = 0.3f;

    public virtual void Init(GameSettings gs, GameField gf, SwapHandler sh, Vector2Int cellPos)
    {
        settings = gs;
        gameField = gf;
        swapHandler = sh;

        renderCamera = Camera.main;
        sr = GetComponent<SpriteRenderer>();

        Cell = cellPos;
        SetIdle();
        IsVisible = false;
        IsSwapping = false;

#if UNITY_EDITOR
        gs.OnSettingsChanged += ApplyChipSettings;
#endif
        ApplyChipSettings();

        UseDebugger();
    }

    void UseDebugger()
    {
        if (debugger is null)
        {
            Debug.LogError("Debugger is null.");
            return;
        }
        debugger.UpdateState(State);
    }

    void ApplyChipSettings()
    {
        dragThreshold = settings.chipDragThreshold;
        deathDuration = settings.chipDeathDuration;
        distanceToAppear = settings.cellSize;
        startFallSpeed = settings.chipFallStartSpeed;
        fallGravity = settings.chipFallGravity;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsIdle() && !IsMatched)
            SetState(ChipState.Dragging);
        else
            return;

        startDragPos = ScreenToWorldPos(eventData.position);

        //Debug.Log("Pointer DOWN, State is " + State);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        //if (!IsBlocked() &&
        //    !HasState(ChipState.Dragging) &&
        //    !HasState(ChipState.Swapping) &&
        //    !HasState(ChipState.Swapped))
        //    SetIdle();

        //Debug.Log("Pointer UP, State is " + State);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!HasState(ChipState.Dragging))
            return;

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

            if(!swapHandler.Swap(this, direction, false))
            {
                Debug.LogWarning("Swap is impossible: " +
                    "some of the swapping chips are not in swappable state!");
                SetIdle();
            }
            //HandleDrag(direction);
        }
    }

    //void HandleDrag(Vector2Int direction)
    //{
    //    SwapRequested += swapHandler.Swap;
    //    SwapRequested?.Invoke(this, direction, false);
    //}

    public void Fall(Vector3 targetPos)
    {
        if (!IsVisible) StartCoroutine(AnimateAppearance());

        StartCoroutine(AnimateFall(targetPos));
    }

    private IEnumerator AnimateFall(Vector3 targetPos)
    {
        SetState(ChipState.Falling);

        float currentY = transform.position.y;
        float verticalVelocity = startFallSpeed;

        // Пока текущая позиция выше целевой
        while (currentY > targetPos.y)
        {
            float deltaTime = Time.deltaTime;
            // Увеличиваем скорость за счёт гравитации
            verticalVelocity += fallGravity * deltaTime;
            // Вычитаем пройденное расстояние из позиции по Y
            currentY -= verticalVelocity * deltaTime;

            // Проверяем, не опустились ли уже ниже целевой позиции
            if (currentY < targetPos.y)
                currentY = targetPos.y;

            // Обновляем позицию объекта (оставляем X и Z без изменений)
            transform.position = new Vector3(transform.position.x, currentY, transform.position.z);

            yield return null;
        }

        SetIdle();
        // Объект достиг целевой позиции – можно вызвать событие
        Debug.Log($"Chip {Cell} has fallen.");
        OnChipLanded?.Invoke(this);
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

    public void Die()
    {
        //Debug.Log($"Chip {Cell} sent to die.");
        StartCoroutine(AnimateDeath());
    }

    protected abstract IEnumerator AnimateDeath();

    protected void NotifyDeathCompleted()
    {
#if UNITY_EDITOR
        settings.OnSettingsChanged -= ApplyChipSettings;
#endif
        //SwapRequested = null;
        OnDeathCompleted?.Invoke(this);
    }

    public ChipState GetState()
    {
        return State;
    }

    public bool HasState(ChipState state)
    {
        return State == state;
    }

    public bool IsIdle()
    {
        return HasState(ChipState.Idle);
    }

    public void SetState(ChipState newState)
    {
        if (State != newState)
        {
            State = newState;
        }
    }

    public void SetIdle()
    {
        SetState(ChipState.Idle);
    }

    Vector3 ScreenToWorldPos(Vector3 screenPosition)
    {
        return renderCamera.ScreenToWorldPoint(screenPosition);
    }
}
