using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer.CustomShipLogModes;

public interface ICustomShipLogModesAPI
{
    public void AddMode(ShipLogMode mode, Func<bool> isEnabledSupplier, Func<string> nameSupplier);

    public void ItemListMake(bool usePhotoAndDescField, Action<MonoBehaviour> callback);
    public void ItemListOpen(MonoBehaviour itemList);
    public void ItemListClose(MonoBehaviour itemList);
    public int ItemListUpdateList(MonoBehaviour itemList);
    public void ItemListSetName(MonoBehaviour itemList, string nameValue);
    public void ItemListSetItems(MonoBehaviour itemList, List<Tuple<string, bool, bool, bool>> items);
    public int ItemListGetSelectedIndex(MonoBehaviour itemList);
    public void ItemListSetSelectedIndex(MonoBehaviour itemList, int index);
    public Image ItemListGetPhoto(MonoBehaviour itemList);
    public Text ItemListGetQuestionMark(MonoBehaviour itemList);
    public void ItemListDescriptionFieldClear(MonoBehaviour itemList);
    public ShipLogFactListItem ItemListDescriptionFieldGetNextItem(MonoBehaviour itemList);
}
