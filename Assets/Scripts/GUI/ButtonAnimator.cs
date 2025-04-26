using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    Image buttonImage;
    Color normalColor = Color.white;
    Color glowColor = new Color(1f, 1f, 0.5f); // Жёлтоватый
    Color highlightColor = new Color(1f, 1f, 0.2f); // Яркий при наведении
    Tween glowTween;

    void Start()
    {
        buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            Debug.LogError("Image не найден на объекте!");
            return;
        }
        transform.localScale = Vector3.zero;

        // Анимация масштабирования до обычного размера
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);

        StartGlow();
    }

    //void StartGlow()
    //{
    //    // Защита от повторного запуска
    //    glowTween?.Kill();

    //    // Запуск бесконечного мягкого свечения
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
        // Останавливаем текущее свечение
        glowTween?.Kill();

        // Быстрая подсветка
        buttonImage.DOColor(highlightColor, 0.2f);
    }

    //public void OnPointerExit(PointerEventData eventData)
    //{
    //    // Возврат к обычному свечению
    //    buttonImage.DOColor(glowColor, 0.01f).OnComplete(StartGlow);
    //}

    public void OnPointerExit(PointerEventData eventData)
    {
        glowTween?.Kill(); // на всякий случай
        // Возврат цвета и запуск свечения
        Sequence seq = DOTween.Sequence();
        //seq.Append(buttonImage.DOColor(glowColor, 0.2f));
        seq.AppendCallback(() => StartGlow());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Анимация "нажатия"
        transform.DOKill(); // Убираем другие анимации на transform
        Sequence pressSeq = DOTween.Sequence();
        pressSeq.Append(transform.DOScale(0.9f, 0.05f));   // Сжимаем
        pressSeq.Append(transform.DOScale(1f, 0.1f));      // Возврат
        pressSeq.Play();
    }
}
