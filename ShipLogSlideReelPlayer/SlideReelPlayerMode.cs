using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace ShipLogSlideReelPlayer;

public class SlideReelPlayerMode : CustomShipLogModes.ItemListMode
{
    public override string GetModeName()
    {
        return "Slide Reel Player";
    }

    private int count = 0;

    protected override void OnItemSelected() {
        if (SelectedIndex == 5)
        {
            UpdateItemCount(0);
        }
        count++;
    }

    public override void UpdateMode()   
    {
        base.UpdateMode();
        for (int i = 0; i < 30; i++)
        {
            if (OWInput.IsNewlyPressed(InputLibrary.left))
            {
                // TODO: why -15f sometimes not needed???
                ListItems[i]._iconRoot.anchoredPosition = new Vector2(ListItems[i]._nameField.rectTransform.sizeDelta.x * ListItems[i]._nameField.rectTransform.localScale.x + 2f, -15f);
            }
            if (OWInput.IsNewlyPressed(InputLibrary.right))
            {
                ListItems[i]._moreToExploreIcon.gameObject.SetActive(!ListItems[i]._moreToExploreIcon.gameObject.activeInHierarchy);
            }           
            if (OWInput.IsNewlyPressed(InputLibrary.markEntryOnHUD))
            {
                ListItems[i]._nameField.text += "+";
            }    

            if (i == 0)
            {
                ShipLogSlideReelPlayer.Instance.ModHelper.Console.WriteLine("ANCHORED 0="+ListItems[i]._iconRoot.anchoredPosition);
            }
        }
        if (OWInput.IsNewlyPressed(InputLibrary.interact))
        {
            ShipLogSlideReelPlayer.Instance.CreateMode("NEW!!!!! 1");
            // ShipLogSlideReelPlayer.Instance.CreateMode("NEW!!!!! 2");
            // ShipLogSlideReelPlayer.Instance.CreateMode("NEW!!!!! 3");
        }
    }
    

    public override void EnterMode(string entryID = "", List<ShipLogFact> revealQueue = null)
    {
        base.EnterMode(entryID, revealQueue);

        OneShotSource.PlayOneShot(AudioType.Artifact_Insert);

        Stopwatch sw = new Stopwatch();
        sw.Start();
        UpdateItemCount(30);
        sw.Stop();
        ShipLogSlideReelPlayer.Instance.ModHelper.Console.WriteLine("Elapsed A: "+sw.ElapsedMilliseconds);
        sw.Reset();
        sw.Start();
        for (int i = 0; i < 30; i++)
        {
            ListItems[i]._nameField.text = "TEST " + i;
            if (i % 3 == 0)
            {
                // ListItems[i]._moreToExploreIcon.gameObject.SetActive(true);   
            }
        }
        sw.Stop();
        ShipLogSlideReelPlayer.Instance.ModHelper.Console.WriteLine("Elapsed B: "+sw.ElapsedMilliseconds);
    }
}