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
    public ChipColor color;
    private GameField gameField;

    private float minDragThreshold;
    private float moveDuration;
    private Vector3 initialPosition;
    private Vector3 startDragPosition;
    private bool isDragging = false;
    private bool isMoving = false;


    private void Start()
    {
        gameField = GameMode.Instance.gameField;

        minDragThreshold = gameField.minChipDragThreshold;
        moveDuration = gameField.chipMoveAnimDuration;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // ���������� ��������� ������� ��� �������
        initialPosition = transform.position;
        startDragPosition = eventData.position;
        isDragging = true;
        Debug.Log("Pointer DOWN on " + this.color);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        Debug.Log("Pointer UP on " + this.color);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        Debug.Log("Pointer DRAG on " + this.color);

        // ������������ �������� ������� (��� ������)
        Vector3 currentDragPosition = eventData.position;
        Vector3 dragDelta = currentDragPosition - startDragPosition;

        // ����������, � ����� ����������� ������ ������� ��������
        if (Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y)) {
            if (dragDelta.x > minDragThreshold) {
                // �������� ������
                Move(Vector3.right);
            }
            else if (dragDelta.x < -minDragThreshold) {
                // �������� �����
                Move(Vector3.left);
            }
        }
        else {
            if (dragDelta.y > minDragThreshold) {
                // �������� �����
                Move(Vector3.up);
            }
            else if (dragDelta.y < -minDragThreshold) {
                // �������� ����
                Move(Vector3.down);
            }
        }
    }

    private void Move(Vector3 direction)
    {
        if (isMoving) return;  // ������� ������������� �����������

        // ������� �������� �����
        Vector3Int targetCell = gameField.GetCellPosition(transform.position + direction);
        Chip otherChip = gameField.GetChipInCell(targetCell);

        if (otherChip != null) {
            // ��������� �������� ��� ����������� ���� �����
            StartCoroutine(SwapChips(otherChip, direction));
        }
    }

    private IEnumerator SwapChips(Chip otherChip, Vector3 direction)
    {
        isMoving = true;

        // ��������� �������
        Vector3 startPos = transform.position;
        Vector3 otherStartPos = otherChip.transform.position;

        // ������� �������
        Vector3 targetPos = otherStartPos;
        Vector3 otherTargetPos = startPos;

        float elapsedTime = 0;

        // ������� ����������� ���� ����� �� moveDuration �������
        while (elapsedTime < moveDuration) {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            otherChip.transform.position = Vector3.Lerp(otherStartPos, otherTargetPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // ������������ ������������� ������� �������
        transform.position = targetPos;
        otherChip.transform.position = otherTargetPos;

        isMoving = false;

        // ��������� ����� (����� ������� ������)
        gameField.SwapChipsPositions(transform.position, otherChip.transform.position);
    }
}
