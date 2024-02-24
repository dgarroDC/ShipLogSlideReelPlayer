using System;
using System.Collections.Generic;
using System.Linq;
using ShipLogSlideReelPlayer.CustomModesAPIs;

namespace ShipLogSlideReelPlayer;

public class SlideReelPlayerMode : ShipLogMode
{
    // TODO: Translation
    public const string Name = "Slide Reel Player";
    
    public ItemListWrapper itemList;

    private ShipLogSlideProjectorPlus _reelProjector;
    private ReelShipLogEntry[] _reels;

    private OWAudioSource _oneShotSource;
    private ScreenPromptList _upperRightPromptList;

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;
        _upperRightPromptList = upperRightPromptList;

        itemList.SetName(Name);

        itemList.GetQuestionMark().text = ShipLogSlideReelPlayer.WithGreenColor("?");
        
        _reelProjector = new ShipLogSlideProjectorPlus(itemList.GetPhoto(), _upperRightPromptList, itemList is ShipLogItemListWrapper);
    }

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        itemList.Open();

        _oneShotSource.PlayOneShot(AudioType.Artifact_Insert);

        _reels = ShipLogSlideReelPlayer.Instance.ReelEntries.Values
            .Where(re => re.ShouldShow())
            .ToArray();

        List<Tuple<string, bool, bool, bool>> items = new();
        for (int i = 0; i < _reels.Length; i++)
        {
            ReelShipLogEntry reel = _reels[i];
            items.Add(new Tuple<string, bool, bool, bool>(reel.GetName(), false, false, reel.HasMoreToExplore())); 
        }

        itemList.SetItems(items);
        if (_reels.Length > 0)
        {
            itemList.GetPhoto().gameObject.SetActive(true);
            itemList.GetQuestionMark().gameObject.SetActive(false);

            itemList.SetSelectedIndex(0); // TODO: Remember selection? Take into consideration that new reels could be discovered
            // Description field already cleared here
            OnItemSelected();
            _reelProjector.AddPrompts();
        }
        else
        {
            itemList.GetPhoto().gameObject.SetActive(false);
            itemList.GetQuestionMark().gameObject.SetActive(true);

            itemList.DescriptionFieldClear();
            // TODO: Translation
            itemList.DescriptionFieldGetNextItem().DisplayText("No slide reels watched.");
        }
    }

    private void OnItemSelected()
    {
        int selectedIndex = itemList.GetSelectedIndex();
        itemList.DescriptionFieldClear();
        _reelProjector.DescriptionFieldItem = itemList.DescriptionFieldGetNextItem(); // This is always the first, could be just be set once?
        if (_reels[selectedIndex].HasMoreToExplore())
        {
            // TODO: Translation
            itemList.DescriptionFieldGetNextItem().DisplayText("<color=orange>There's something missing here.</color>");
        }

        _reelProjector.OnEntrySelected(_reels, selectedIndex);
    }

    public override void UpdateMode()
    {
        // Important to call UpdateList even with empty list!
        if (itemList.UpdateList() != 0)
        {
            OnItemSelected();
        }
        _reelProjector.Update();
    }

    public override void ExitMode()
    {
        itemList.DescriptionFieldClear(); // Just in case...
        itemList.Close();
        if (_reels.Length == 0) return; // Probably works without this...

        _reelProjector.RemoveReel();
        _reelProjector.RemovePrompts();
        ShipLogSlideReelPlayer.Instance.UnloadAllTextures();
        // Note: Texture aren't unloaded while the game is paused (StreamingIteratedTextureAssetBundle.Update())
        // textured unloaded by updating slide index are, meaning that up to 5*#Reels textures could be loaded   
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
