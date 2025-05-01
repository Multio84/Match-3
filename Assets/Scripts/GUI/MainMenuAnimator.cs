using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Collections;


public class MainMenuAnimator : SettingsSubscriber
{
    public override GameSettings Settings { get; set; }
    public Button buttonStart;
    public Button buttonOptions;
    public Button buttonQuit;

    Vector3 buttonStartEndPos;
    Vector3 buttonOptionsEndPos;
    Vector3 buttonQuitEndPos;

    float buttonsAnimDelay;
    float buttonsAnimDuration;


    public void Setup(GameSettings gs)
    {
        Settings = gs;
    }

    public override void ApplyGameSettings()
    {
        buttonsAnimDelay = Settings.mainButtonsAnimDelay;
        buttonsAnimDuration = Settings.mainButtonsAnimDuration;
    }

    void Start()
    {
        //StartAnimation();
    }

    void StartAnimation()
    {
        SetEndStates();
        SetStartPositions();

        StartCoroutine(StartDelay());
    }

    IEnumerator StartDelay()
    {
        yield return new WaitForSeconds(0.01f);
        AnimateMainButtons();
    }

    void AnimateMainButtons()
    {
        Transform[] buttons = { buttonStart.transform, buttonOptions.transform, buttonQuit.transform };
        Vector3[] endPositions = { buttonStartEndPos, buttonOptionsEndPos, buttonQuitEndPos };
        Vector3 endScale = Vector3.one;

        Sequence seq = DOTween.Sequence();
        for (int i = 0; i < buttons.Length; i++)
        {
            Transform currentButton = buttons[i];

            Tween moveTween = currentButton
                .DOMove(endPositions[i], buttonsAnimDuration)
                .SetEase(Ease.OutElastic, 0.4f, 0.3f);

            Tween scaleXTween = DOTween.To(
                () => currentButton.localScale.x,
                x =>
                {
                    float newX = (x > 1f) ? 2f - x : x;
                    Vector3 curScale = currentButton.localScale;
                    currentButton.localScale = new Vector3(newX, curScale.y, 1);
                },
                1f, // target X value
                buttonsAnimDuration
            ).SetEase(Ease.OutElastic, 0.4f, 0.3f);

            float rawValue = currentButton.localScale.y;

            // increases button's Y scale on value,
            // proportional to button's X offset from target position (screen X center)
            Tween scaleYTween = DOTween.To(
                () => rawValue,
                x =>
                {
                    rawValue = x;
                    float newY = x;

                    newY = (x < 1f) ? (1f + Mathf.Abs(1f - x)) : x;
                    Vector3 curScale = currentButton.localScale;
                    currentButton.localScale = new Vector3(curScale.x, newY, 1);
                },
                1f, // target Y value
                buttonsAnimDuration
            ).SetEase(Ease.OutElastic, 0.6f, 0.3f);

            float startDelay = buttonsAnimDelay * i;

            seq.Insert(startDelay, moveTween);
            seq.Join(scaleXTween);
            seq.Join(scaleYTween);
        }
    }

    void SetEndStates()
    {
        buttonStartEndPos = buttonStart.transform.position;
        buttonOptionsEndPos = buttonOptions.transform.position;
        buttonQuitEndPos = buttonQuit.transform.position;
    }

    void SetStartPositions()
    {
        buttonStart.transform.position += new Vector3(-1000, 0, 0);
        buttonOptions.transform.position += new Vector3(1000, 0, 0);
        buttonQuit.transform.position += new Vector3(-1000, 0, 0);

        buttonStart.transform.localScale = new Vector3(0.1f, 0.1f, 1);
        buttonOptions.transform.localScale = new Vector3(0.1f, 0.1f, 1);
        buttonQuit.transform.localScale = new Vector3(0.1f, 0.1f, 1);
    }


}
