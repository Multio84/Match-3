using TMPro;
using UnityEngine;


public class ChipDebugger : MonoBehaviour
{
    bool isOn = true;
    public TextMeshPro debugText;
    string text;

    public void UpdateState(ChipState state)
    {
        if (!isOn) return;

        if (state == ChipState.Idle) { text = "Id"; }
        if (state == ChipState.Blocked) { text = "Bl"; }
        if (state == ChipState.Dragging) { text = "Dr"; }
        if (state == ChipState.Swapping) { text = "Sg"; }
        if (state == ChipState.Swapped) { text = "Sd"; }
        if (state == ChipState.Destroying) { text = "De"; }

        debugText.text = text;
    }
}
