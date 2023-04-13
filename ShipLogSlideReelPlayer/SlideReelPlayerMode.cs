using System;
using System.Collections.Generic;
using System.Linq;
using CustomShipLogModes;

namespace ShipLogSlideReelPlayer;

public class SlideReelPlayerMode : ShipLogMode
{
    // TODO: Translation
    public const string Name = "Slide Reel Player";

    private ShipLogSlideProjectorPlus _reelProjector;
    private ShipLogEntry[] _reels;
    private ItemsList _itemsList;

    private OWAudioSource _oneShotSource;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;

        _itemsList = GetComponent<ItemsList>();
        _itemsList.SetName(Name);

        // There are no guarantees of the initial state of question mark and photo
        _itemsList.questionMark.gameObject.SetActive(false);
        _itemsList.photo.gameObject.SetActive(true);
        _reelProjector = new ShipLogSlideProjectorPlus(_itemsList.photo, upperRightPromptList);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        _itemsList.Open();

        _oneShotSource.PlayOneShot(AudioType.Artifact_Insert);

        // TODO: Get rid of ShipLogEntry extension
        _reels = ShipLogSlideReelPlayer.Instance.ReelEntries.Values
            .Where(re => re.GetState() == ShipLogEntry.State.Explored)
            .ToArray();
        
        // TODO: Why is the mark on hud visible in the last reels?

        List<Tuple<string, bool, bool, bool>> items = new();
        for (int i = 0; i < _reels.Length; i++)
        {
            ShipLogEntry reel = _reels[i];
            items.Add(new Tuple<string, bool, bool, bool>(reel.GetName(false), false, false, reel.HasMoreToExplore())); 
            // TODO: Also more to explore TEXT (another one? "something missing")
        }

        _itemsList.contentsItems = items;
        _itemsList.selectedIndex = 0; // TODO: Remember selection? Take into consideration that new reels could be discovered
        OnItemSelected();
    }

    private void OnItemSelected()
    {
        _itemsList.DescriptionFieldClear();
        if (_reels[_itemsList.selectedIndex].HasMoreToExplore())
        {
            // TODO: Translation
            _itemsList.DescriptionFieldGetNextItem().DisplayText("<color=orange>There's something missing here.</color>");
        }
        _reelProjector.OnEntrySelected(_reels, _itemsList.selectedIndex, _reels.Length);
    }

    public override void UpdateMode()
    {
        if (_itemsList.UpdateList() != 0)
        {
            OnItemSelected();
        }
        _reelProjector.Update();
    }

    public override void ExitMode()
    {
        _itemsList.Close();
        // TODO: Probably more, remove reel ( Or wait until fully closed animator???) or something, also prompts
    }

    public override void OnEnterComputer()
    {
        // No-op
    }

    public override void OnExitComputer()
    {
        // No-op
    }
    
    public override bool AllowModeSwap()
    {
        return true;
    }

    public override bool AllowCancelInput()
    {
        return true;
    }

    public override string GetFocusedEntryID()
    {
        return "";
    }
}