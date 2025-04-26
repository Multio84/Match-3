using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    Image buttonImage;
    Color normalColor = Color.white;
    Color glowColor = new Color(1f, 1f, 0.5f); // Ƹ��������
    Color highlightColor = new Color(1f, 1f, 0.2f); // ����� ��� ���������
    Tween glowTween;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogError("Image �� ������ �� �������!");
            return;
        }
        transform.localScale = Vector3.zero;

        // �������� ��������������� �� �������� �������
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        StartGlow();
    }

    //void StartGlow()
    //{
    //    // ������ �� ���������� �������
    //    glowTween?.Kill();

    //    // ������ ������������ ������� ��������
    //    glowTween = buttonImage.DOColor(glowColor, 1f)
    //        .SetLoops(-1, LoopType.Yoyo)
    //        .SetEase(Ease.InOutSine);
    //}
    void StartGlow()
    {
        glowTween?.Kill();

        glowTween = DOTween.Sequence()
            .Append(buttonImage.DOColor(glowColor, 0.5f))
            .Append(buttonImage.DOColor(normalColor, 0.5f))
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutFlash);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ������������� ������� ��������
        glowTween?.Kill();

        // ������� ���������
        buttonImage.DOColor(highlightColor, 0.2f);
    }

    //public void OnPointerExit(PointerEventData eventData)
    //{
    //    // ������� � �������� ��������
    //    buttonImage.DOColor(glowColor, 0.01f).OnComplete(StartGlow);
    //}

    public void OnPointerExit(PointerEventData eventData)
    {
        glowTween?.Kill(); // �� ������ ������
        // ������� ����� � ������ ��������
        Sequence seq = DOTween.Sequence();
        //seq.Append(buttonImage.DOColor(glowColor, 0.2f));
        seq.AppendCallback(() => StartGlow());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // �������� "�������"
        transform.DOKill(); // ������� ������ �������� �� transform
        Sequence pressSeq = DOTween.Sequence();
        pressSeq.Append(transform.DOScale(0.9f, 0.05f));   // �������
        pressSeq.Append(transform.DOScale(1f, 0.1f));      // �������
        pressSeq.Play();
    }
}
