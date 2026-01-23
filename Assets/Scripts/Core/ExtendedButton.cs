using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ExtendedButton : Button
{
    [SerializeField] TextMeshProUGUI text;
    [SerializeField] Color defaultColor = Color.white;
    [SerializeField] Color highlightColor = Color.black;
    [SerializeField] Color pressedColor = Color.red;
    [SerializeField] Color selectedColor = Color.red;
    [SerializeField] Color disabledColor = Color.red;

    protected override void Awake()
    {
        base.Awake();
        text = GetComponentInChildren<TextMeshProUGUI>();
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        Debug.Log("DOING STATE TRANSITION OF EXTNEDE BUTTON");
        base.DoStateTransition(state, instant);
        if (text == null)
            return;
 

        switch (state)
        {
            case SelectionState.Normal:
                text.color = defaultColor;
                break;
            case SelectionState.Highlighted:
                text.color = highlightColor;
                break;
            case SelectionState.Pressed:
                text.color = pressedColor;
                break;
            case SelectionState.Selected:
                text.color = selectedColor;
                break;
            case SelectionState.Disabled:
                text.color = disabledColor;
                break;
            default:
                text.color = Color.black;
                break;
        }

        
    }

}
