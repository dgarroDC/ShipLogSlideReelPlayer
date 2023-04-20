using UnityEngine;

namespace ShipLogSlideReelPlayer;

public class ScreenPromptListSwitcher
{
    private ScreenPromptList _originalPromptList;
    private ScreenPromptList _newPromptList;
    private PromptManager _promptManager;

    private double _lastSwitchTime = -100f;

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
        
        Update(); // To force not overlapping first time
        // TODO: Restore original
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
        if (Time.unscaledTime >= _lastSwitchTime + 5f)
        {
            _lastSwitchTime = Time.unscaledTime;
            if (_originalPromptList.gameObject.activeSelf)
            {
                _originalPromptList.gameObject.SetActive(false);
                _newPromptList.gameObject.SetActive(true);
            }
            else
            {
                _originalPromptList.gameObject.SetActive(true);
                _newPromptList.gameObject.SetActive(false);
            }
        }
    }
}