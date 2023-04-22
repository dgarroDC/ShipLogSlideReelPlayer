using UnityEngine;

namespace ShipLogSlideReelPlayer;

public class ScreenPromptListSwitcher
{
    private const float SwitchTime = 3f;
    private double _lastSwitchTime;

    private ScreenPromptList _originalPromptList;
    private ScreenPromptList _newPromptList;
    private PromptManager _promptManager;


    public ScreenPromptListSwitcher(ScreenPromptList originalPromptList)
    {
        _originalPromptList = originalPromptList;
        Transform promptListTransform = originalPromptList.transform;

        // Copy prompt list
        _newPromptList = Object.Instantiate(originalPromptList, promptListTransform.position, promptListTransform.rotation, promptListTransform.parent);
        _newPromptList.transform.DestroyAllChildrenImmediate();
        _newPromptList.SetReversePromptOrder(originalPromptList._reverse);
        _newPromptList.SetMinElementHeightAndWidth(originalPromptList._promptElementMinHeight, originalPromptList._promptElementMinWidth);
        
        _promptManager = Locator.GetPromptManager();

        Hide(_newPromptList);
    }

    public void Reset()
    {
        Show(_originalPromptList);
        Hide(_newPromptList);
    }

    public void AddScreenPrompt(ScreenPrompt buttonPrompt)
    {
        _promptManager.AddScreenPrompt(buttonPrompt, _newPromptList, TextAnchor.MiddleRight);
    }

    public void RemoveScreenPrompt(ScreenPrompt buttonPrompt)
    {
        _promptManager.RemoveScreenPrompt(buttonPrompt);
    }

    public void Update()
    {
        if (Time.unscaledTime >= _lastSwitchTime + SwitchTime)
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
