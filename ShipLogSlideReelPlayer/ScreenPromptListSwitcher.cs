using UnityEngine;

namespace ShipLogSlideReelPlayer;

public class ScreenPromptListSwitcher
{
    private const float SwitchTime = 3f;
    private double _lastSwitchTime;

    private ScreenPromptList _originalPromptList;
    private ScreenPromptList _newPromptList;
    private RectTransform _originalPromptListRect;
    private RectTransform _newPromptListRect;
    private PromptManager _promptManager;
    private readonly bool _switchPrompts;

    public ScreenPromptListSwitcher(ScreenPromptList originalPromptList, bool switchPrompts)
    {
        _promptManager = Locator.GetPromptManager();

        _originalPromptList = originalPromptList;
        _originalPromptListRect = _originalPromptList.GetComponent<RectTransform>();

        _switchPrompts = switchPrompts;
        if (switchPrompts)
        {
            // Copy prompt list
            _newPromptList = Object.Instantiate(originalPromptList, _originalPromptListRect.position, _originalPromptListRect.rotation, _originalPromptListRect.parent);
            _newPromptListRect = _newPromptList.GetComponent<RectTransform>();
            _newPromptListRect.DestroyAllChildrenImmediate();
            _newPromptList.SetReversePromptOrder(originalPromptList._reverse);
            // This is probably not needed, UI size setter is already copied and should handle this, assuming the prompt list from Custom Ship Log Modes has it...
            _newPromptList.SetMinElementDimensionsAndFontSize(originalPromptList._promptElementMinHeight, originalPromptList._promptElementMinWidth, originalPromptList._listFontSize);

            Hide(_newPromptList);
        }
    }

    public void Reset()
    {
        if (_switchPrompts)
        {
            Show(_originalPromptList);
            Hide(_newPromptList);
        }
    }

    public void AddScreenPrompt(ScreenPrompt buttonPrompt)
    {
        _promptManager.AddScreenPrompt(buttonPrompt, _switchPrompts? _newPromptList : _originalPromptList, TextAnchor.MiddleRight);
    }

    public void RemoveScreenPrompt(ScreenPrompt buttonPrompt)
    {
        _promptManager.RemoveScreenPrompt(buttonPrompt);
    }

    public void Update()
    {
        if (_switchPrompts && Time.unscaledTime >= _lastSwitchTime + SwitchTime)
        {
            _lastSwitchTime = Time.unscaledTime;
            // Use scale because active mess up with children somehow, they could remain invisible
            if (_originalPromptList.transform.localScale.Equals(Vector3.one))
            {
                Hide(_originalPromptList);
                Show(_newPromptList);
            }
            else
            {
                Show(_originalPromptList);
                Hide(_newPromptList);
            }
        }
        //     ALSO TEST "?"
    }

    private void Hide(ScreenPromptList promptList)
    {
        promptList.transform.localScale = Vector3.zero;
    }
    
    private void Show(ScreenPromptList promptList)
    {
        promptList.transform.localScale = Vector3.one;
    }
}
