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

    // protected override void OnItemSelected() {
    //     _reelProjector.OnEntrySelected(_reels, SelectedIndex, _reels.Length);
    // }

    public override void Initialize(ScreenPromptList centerPromptList, ScreenPromptList upperRightPromptList, OWAudioSource oneShotSource)
    {
        _oneShotSource = oneShotSource;

        _itemsList = GetComponent<ItemsList>();
        _itemsList.Initialize();
        _itemsList.SetName(Name);
        
        _itemsList.Photo.gameObject.SetActive(true); // This will be ALWAYS active, we own this photo
        _reelProjector = new ShipLogSlideProjectorPlus(_itemsList.Photo, upperRightPromptList);
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

        List<string> texts = new List<string>();
        for (int i = 0; i < _reels.Length; i++)
        {
            texts.Add(_reels[i].GetName(false));
          // TODO:  ListItems[i]._moreToExploreIcon.gameObject.SetActive(_reels[i].HasMoreToExplore()); // TODO: Also TEXT
        }

        _itemsList.ContentsItems = texts;
        _itemsList.SelectedIndex = 0; // TODO: Remember selection? Take into consideration that new reels could be discovered
        _reelProjector.OnEntrySelected(_reels, _itemsList.SelectedIndex, _reels.Length);
    }

    // TODO: Entry menu animation too????
    
    public override void UpdateMode()   
    {
        if (_itemsList.UpdateList())
        {
            _reelProjector.OnEntrySelected(_reels, _itemsList.SelectedIndex, _reels.Length);
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