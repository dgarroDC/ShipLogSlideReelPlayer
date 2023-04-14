using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShipLogSlideReelPlayer;

public interface ICustomShipLogModesAPI
{
    public void AddMode(ShipLogMode mode, Func<bool> isEnabledSupplier, Func<string> nameSupplier);

    public void ItemListMake(bool usePhotoAndDescField, Action<GameObject> callback);
    public void ItemListOpen(GameObject itemList);
    public void ItemListClose(GameObject itemList);
    public int ItemListUpdateList(GameObject itemList);
    public void ItemListSetName(GameObject itemList, string nameValue);
    public void ItemListSetItems(GameObject itemList, List<Tuple<string, bool, bool, bool>> items);
    public int ItemListGetSelectedIndex(GameObject itemList);
    public void ItemListSetSelectedIndex(GameObject itemList, int index);
    public Image ItemListGetPhoto(GameObject itemList);
    public Text ItemListGetQuestionMark(GameObject itemList);
    public void ItemListDescriptionFieldClear(GameObject itemList);
    public ShipLogFactListItem ItemListDescriptionFieldGetNextItem(GameObject itemList);
}
