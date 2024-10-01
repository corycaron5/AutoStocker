using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;


namespace AutoDisplayCards;

[HarmonyPatch]
public class MyPatches
{
    private static List<int> _sortedIndexList = new List<int>();
    private static List<float> _sortedPriceList = new List<float>();
    private static bool _isRunning;
    private static Dictionary<EItemType, WarehouseEntry> _warehouseCache = new Dictionary<EItemType, WarehouseEntry>();
    
    private static void FillCardShelves()
    {
        ECardExpansionType fillType = Plugin.FillCardExpansionType.Value;
        SortAlbumByPrice(fillType);
        if (_sortedIndexList.Count == 0)
        {
            Plugin.LogDebugMessage("No applicable cards to fill shelves");
            return;
        }
        int index = 0;
        List<CardShelf> cardShelfList = CSingleton<ShelfManager>.Instance.m_CardShelfList;
        for (int i = 0; i < cardShelfList.Count; i++)
        {
            if (cardShelfList[i].m_ItemNotForSale)
            {
                Plugin.LogDebugMessage("Shelf " + i + " is a personal shelf");
                continue;
            }
            Plugin.LogDebugMessage("Filling shelf " + i);
            List<InteractableCardCompartment> cardCompartmentList = cardShelfList[i].GetCardCompartmentList();
            for (int j = 0; j < cardCompartmentList.Count; j++)
            {
                Plugin.LogDebugMessage("Filling compartment " + j);
                EFillResult result = FillCardSlot(cardCompartmentList[j], index, fillType);
                switch (result)
                {
                    case EFillResult.Filled:
                        index++;
                        break;
                    case EFillResult.Full:
                        break;
                    case EFillResult.NoCards:
                        return;
                }
            }
        }
    }
    
    private static EFillResult FillCardSlot(InteractableCardCompartment comp, int index, ECardExpansionType expansionType)
    {
        if (comp.m_StoredCardList.Count == 0)
        {
            if (_sortedIndexList.Count - 1 < index)
            {
                Plugin.LogDebugMessage("No more available cards");
                return EFillResult.NoCards;
            }
            Plugin.LogDebugMessage("Filling card slot with index " + index);
            InteractableCard3d card3d = CreateInteractableCard3d(_sortedIndexList[index], expansionType);
            card3d.transform.position = comp.m_PutCardLocation.position;
            comp.SetCardOnShelf(card3d);
            bool isDestiny = false;
            int fixedIndex = _sortedIndexList[index];
            if (fixedIndex >= InventoryBase.GetShownMonsterList(expansionType).Count * CPlayerData.GetCardAmountPerMonsterType(expansionType, true))
            {
                Plugin.LogDebugMessage("Detected index out of range: " + fixedIndex);
                Plugin.LogDebugMessage("Max index for this expansion: " + (InventoryBase.GetShownMonsterList(expansionType).Count * CPlayerData.GetCardAmountPerMonsterType(expansionType, true) - 1));
                fixedIndex -= InventoryBase.GetShownMonsterList(expansionType).Count *
                              CPlayerData.GetCardAmountPerMonsterType(expansionType, true);
                isDestiny = true;
                Plugin.LogDebugMessage("Updated index to: " + fixedIndex);
            }
            CPlayerData.ReduceCardUsingIndex(fixedIndex, expansionType, isDestiny, 1);
            Plugin.LogDebugMessage("Finished reducing card and setting on shelf");
            return EFillResult.Filled;
        }
        else
        {
            InteractableCard3d debug = comp.m_StoredCardList.First();
            string monsterName = debug.m_Card3dUI.m_CardUI.GetCardData().monsterType.ToString();
            Plugin.LogDebugMessage("Card detected: " + monsterName);
            return EFillResult.Full;
        }
    }

    private static void SortAlbumByPrice(ECardExpansionType expansionType)
    {
        Plugin.LogDebugMessage("Started sorting.");
        _sortedIndexList.Clear();
        _sortedPriceList.Clear();
        int num = 0;
        int num2 = 0;
        int num3 = 0;
        SortCardsByPrice(expansionType,ref num,false, ref num3);
        if (expansionType == ECardExpansionType.Ghost)
        {
            SortCardsByPrice(expansionType,ref num2,true, ref num3);
        }
        Plugin.LogDebugMessage("Sorted " + num3 + " cards.");
    }

    private static void SortCardsByPrice(ECardExpansionType expansionType, ref int saveIndex, bool isDestiny, ref int totalCount)
    {
        for (int i = 0; i < InventoryBase.GetShownMonsterList(expansionType).Count; i++)
        {
            Plugin.LogDebugMessage("Sorting monster list " + i);
            Plugin.LogDebugMessage("Cards for this monster: " + CPlayerData.GetCardAmountPerMonsterType(expansionType, true));
            for (int j = 0; j < CPlayerData.GetCardAmountPerMonsterType(expansionType, true); j++)
            {
                Plugin.LogDebugMessage("Checking card id " + saveIndex);
                CardData cardData = new CardData();
                cardData.monsterType = CPlayerData.GetMonsterTypeFromCardSaveIndex(saveIndex, expansionType);
                cardData.borderType = (ECardBorderType)(saveIndex % CPlayerData.GetCardAmountPerMonsterType(expansionType, false));
                cardData.isFoil = saveIndex % CPlayerData.GetCardAmountPerMonsterType(expansionType, true) >= CPlayerData.GetCardAmountPerMonsterType(expansionType, false);
                cardData.isDestiny = isDestiny;
                cardData.expansionType = expansionType;
                int cardAmount = CPlayerData.GetCardAmount(cardData);
                if (cardAmount <= Plugin.AmountToHold.Value)
                {
                    Plugin.LogDebugMessage("Not enough duplicates");
                    saveIndex++;
                    totalCount++;
                    continue;
                }
                float num2 = 0;
                if (cardAmount > 0)
                {
                    num2 = CPlayerData.GetCardMarketPrice(cardData);
                }
                if ((num2 > Plugin.MaxCardValue.Value) || (num2 < Plugin.MinCardValue.Value))
                {
                    Plugin.LogDebugMessage("Card price not within range");
                    Plugin.LogDebugMessage("Price: " + num2);
                    Plugin.LogDebugMessage("Max: " + Plugin.MaxCardValue.Value);
                    Plugin.LogDebugMessage("Min: " + Plugin.MinCardValue.Value);
                    saveIndex++;
                    totalCount++;
                    continue;
                }
                int num3 = _sortedPriceList.Count;
                for (int k = 0; k < _sortedPriceList.Count; k++)
                {
                    if (num2 > _sortedPriceList[k])
                    {
                        num3 = k;
                        break;
                    }
                }
                for (int l = 0; l < cardAmount - Plugin.AmountToHold.Value; l++)
                {
                    _sortedPriceList.Insert(num3, num2);
                    _sortedIndexList.Insert(num3, totalCount);
                }
                saveIndex++;
                totalCount++;
            }
        }
    }

    private static InteractableCard3d CreateInteractableCard3d(int cardIndex, ECardExpansionType expansionType)
    {
        Plugin.LogDebugMessage("Creating interactable card");
        Card3dUIGroup cardUI = CSingleton<Card3dUISpawner>.Instance.GetCardUI();
        InteractableCard3d card3d = ShelfManager.SpawnInteractableObject(EObjectType.Card3d).GetComponent<InteractableCard3d>();
        cardUI.m_IgnoreCulling = true;
        cardUI.m_CardUIAnimGrp.gameObject.SetActive(true);
        cardUI.m_CardUI.SetFoilCullListVisibility(true);
        cardUI.m_CardUI.ResetFarDistanceCull();
        cardUI.m_CardUI.SetFoilMaterialList(CSingleton<Card3dUISpawner>.Instance.m_FoilMaterialTangentView);
        cardUI.m_CardUI.SetFoilBlendedMaterialList(CSingleton<Card3dUISpawner>.Instance.m_FoilBlendedMaterialTangentView);
        bool isDestiny = false;
        int fixedIndex = cardIndex;
        if (cardIndex >= InventoryBase.GetShownMonsterList(expansionType).Count * CPlayerData.GetCardAmountPerMonsterType(expansionType, true))
        {
            Plugin.LogDebugMessage("Detected index out of range: " + cardIndex);
            Plugin.LogDebugMessage("Max index for this expansion: " + (InventoryBase.GetShownMonsterList(expansionType).Count * CPlayerData.GetCardAmountPerMonsterType(expansionType, true) - 1));
            fixedIndex -= InventoryBase.GetShownMonsterList(expansionType).Count *
                CPlayerData.GetCardAmountPerMonsterType(expansionType, true);
            isDestiny = true;
            Plugin.LogDebugMessage("Updated index to: " + fixedIndex);
        }
        cardUI.m_CardUI.SetCardUI(CPlayerData.GetCardData(fixedIndex, expansionType, isDestiny));
        Plugin.LogDebugMessage("Got card data");
        card3d.SetCardUIFollow(cardUI);
        card3d.SetEnableCollision(false);
        return card3d;
    }

    public static void CacheWarehouse()
    {
        _warehouseCache.Clear();
        List<WarehouseShelf> wareShelfList = CSingleton<ShelfManager>.Instance.m_WarehouseShelfList;
        for (int i = 0; i < wareShelfList.Count; i++)
        {
            Plugin.LogDebugMessage("Warehouse shelf " + i);
            List<InteractableStorageCompartment> compList = wareShelfList[i].GetStorageCompartmentList();
            for (int j = 0; j < compList.Count; j++)
            {
                Plugin.LogDebugMessage("Compartment " + j);
                ShelfCompartment shelfCompartment = compList[j].GetShelfCompartment();
                List<InteractablePackagingBox_Item> boxList = shelfCompartment.GetInteractablePackagingBoxList();
                if (boxList.Count > 0)
                {
                    Plugin.LogDebugMessage("Found  " + boxList.Count + " boxes on shelf");
                    EItemType type = boxList[0].m_ItemCompartment.GetItemType();
                    int count = 0;
                    for (int k = 0; k < boxList.Count; k++)
                    {
                        count += boxList[k].m_ItemCompartment.GetItemCount();
                    }
                    WarehouseEntry entry = new WarehouseEntry(i, j, count);
                    _warehouseCache.TryAdd(type, entry);
                    Plugin.LogDebugMessage("Added entry " + type + ":" + entry);
                }
            }
        }
    }

    public static void FillItemShelves()
    {
        CacheWarehouse();
        if (Plugin.RefillSprayers.Value && Plugin.PrioritizeSprayersWhenRefillingStock.Value)
        {
            FillSprayers();
        }
        List<Shelf> shelfList = CSingleton<ShelfManager>.Instance.m_ShelfList;
        for (int i = 0; i < shelfList.Count; i++)
        {
            if (shelfList[i].m_ItemNotForSale)
            {
                Plugin.LogDebugMessage("Shelf " + i + " is a personal shelf");
                continue;
            }
            Plugin.LogDebugMessage("Filling shelf " + i);
            List<ShelfCompartment> compList = shelfList[i].GetItemCompartmentList();
            for (int j = 0; j < compList.Count; j++)
            {
                Plugin.LogDebugMessage("Filling compartment " + j);
                ShelfCompartment shelfCompartment = compList[j];
                if (shelfCompartment.GetItemCount() < shelfCompartment.GetMaxItemCount())
                {
                    FillItemShelf(shelfCompartment);
                }
                else Plugin.LogDebugMessage("Compartment already full");
            }
        }
        if (Plugin.RefillSprayers.Value && !Plugin.PrioritizeSprayersWhenRefillingStock.Value)
        {
            FillSprayers();
        }
    }

    public static void FillSprayers()
    {
        if (_warehouseCache.ContainsKey(EItemType.Deodorant))
        {
            WarehouseEntry deodorant = _warehouseCache.GetValueSafe(EItemType.Deodorant);
            if (deodorant.Amount > 0)
            {
                List<InteractableAutoCleanser> sprayers = ShelfManager.GetAutoCleanserList();
                ShelfCompartment wareComp = ShelfManager.Instance.m_WarehouseShelfList[deodorant.ShelfIndex].GetStorageCompartmentList()[deodorant.CompartmentIndex].GetShelfCompartment();
                foreach (InteractableAutoCleanser sprayer in sprayers)
                {
                    while (sprayer.HasEnoughSlot())
                    {
                        if (wareComp.GetItemCount() > 0)
                        {
                            InteractablePackagingBox_Item box = wareComp.GetLastInteractablePackagingBox();
                            if (box is not null)
                            {
                                Item firstItem = box.m_ItemCompartment.GetFirstItem();
                                firstItem.transform.position = sprayer.GetEmptySlotTransform().position;
                                firstItem.LerpToTransform(sprayer.GetEmptySlotTransform(),sprayer.GetEmptySlotTransform());
                                sprayer.AddItem(firstItem,true);
                                //firstItem.DisableItem();
                                box.m_ItemCompartment.RemoveItem(firstItem);
                                if (box.m_ItemCompartment.GetItemCount() == 0)
                                {
                                    CleanupBox(box, wareComp);
                                }
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    wareComp.SetPriceTagItemAmountText();
                }
            }
        }
    }

    public static void FillItemShelf(ShelfCompartment shelfCompartment)
    {
        EItemType toFillType = shelfCompartment.GetItemType();
        if (_warehouseCache.ContainsKey(toFillType))
        {
            Plugin.LogDebugMessage("Found item " + toFillType.ToString());
            int toFillAmount = shelfCompartment.GetMaxItemCount() - shelfCompartment.GetItemCount();
            WarehouseEntry entry = _warehouseCache.GetValueSafe(toFillType);
            ShelfCompartment wareComp =
                ShelfManager.Instance.m_WarehouseShelfList[entry.ShelfIndex].GetStorageCompartmentList()[entry.CompartmentIndex].GetShelfCompartment();
            if (entry.Amount <= toFillAmount)
            {
                for (int a = 0; a < entry.Amount; a++)
                {
                    SpawnItemOnShelf(toFillType, shelfCompartment);
                }
                Plugin.LogDebugMessage("Added " + entry.Amount + " items");
                while (wareComp.GetInteractablePackagingBoxList().Count >0)
                {
                    InteractablePackagingBox_Item box = wareComp.GetLastInteractablePackagingBox();
                    CleanupBox(box, wareComp);
                }
                Plugin.LogDebugMessage("Deleted empty boxes");
            }
            else
            {
                List<InteractablePackagingBox_Item> toDelete = new List<InteractablePackagingBox_Item>();
                for (int b = wareComp.GetInteractablePackagingBoxList().Count - 1; b >= 0; b--)
                {
                    InteractablePackagingBox_Item box = wareComp.GetInteractablePackagingBoxList()[b];
                    if (toFillAmount <= 0) break;
                    int inBox = box.m_ItemCompartment.GetItemCount();
                    if (inBox <= toFillAmount)
                    {
                        for (int a = 0; a < inBox; a++)
                        {
                            SpawnItemOnShelf(toFillType, shelfCompartment);
                        }
                        Plugin.LogDebugMessage("Filled compartment with " + inBox + " items");
                        toDelete.Add(box);
                        if (inBox == toFillAmount)
                        {
                            toFillAmount = 0;
                            break;
                        }
                        Plugin.LogDebugMessage("To fill amount before partial box: " + toFillAmount);
                        toFillAmount -= inBox;
                        Plugin.LogDebugMessage("To fill amount after partial box: " + toFillAmount);
                    }
                    else
                    {
                        Plugin.LogDebugMessage("To fill amount before half box: " + toFillAmount);
                        for (int a = 0; a < toFillAmount; a++)
                        {
                            SpawnItemOnShelf(toFillType, shelfCompartment);
                            RemoveItemFromBox(box, wareComp);
                        }
                        Plugin.LogDebugMessage("Filled compartment with " + toFillAmount + " items and removed from box");
                        toFillAmount = 0;
                    }
                }
                foreach (InteractablePackagingBox_Item box in toDelete)
                {
                    CleanupBox(box, wareComp);
                }
            }
        }
        else
        {
            Plugin.LogDebugMessage("Item not found in warehouse or shelf empty");
        }
    }

    public static void SpawnItemOnShelf(EItemType toFillType, ShelfCompartment shelfCompartment)
    {
        ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(toFillType);
        Item item = ItemSpawnManager.GetItem(shelfCompartment.m_StoredItemListGrp);
        item.SetMesh(itemMeshData.mesh, itemMeshData.material, toFillType, itemMeshData.meshSecondary, itemMeshData.materialSecondary);
        item.transform.position = shelfCompartment.GetEmptySlotTransform().position;
        item.transform.localScale = shelfCompartment.GetEmptySlotTransform().localScale;
        item.transform.rotation = shelfCompartment.m_StartLoc.rotation;
        item.gameObject.SetActive(true);
        shelfCompartment.AddItem(item, false);
    }

    public static void RemoveItemFromBox(InteractablePackagingBox_Item box, ShelfCompartment wareComp)
    {
        Item firstItem = box.m_ItemCompartment.GetFirstItem();
        firstItem.DisableItem();
        box.m_ItemCompartment.RemoveItem(firstItem);
        if (wareComp is not null)
        {
            wareComp.SetPriceTagItemAmountText();
        }
    }

    public static void CleanupBox(InteractablePackagingBox_Item box, ShelfCompartment comp)
    {
        Plugin.LogDebugMessage("Starting box cleanup");
        RestockManager.RemoveItemPackageBox(box);
        Plugin.LogDebugMessage("Box removed from restock manager");
        if (comp is not null)
        {
            comp.RemoveBox(box);
            Plugin.LogDebugMessage("Box removed from shelf");
        }
        WorkerManager.Instance.m_TrashBin.DiscardBox(box, false);
        Plugin.LogDebugMessage("Box discarded to trash");
    }
    
    [HarmonyPatch(typeof(CGameManager), "Update")]
    [HarmonyPrefix]
    public static void GameManagerUpdatePrefix()
    {
        if (Plugin.FillCardTableKey.Value.IsDown() && !_isRunning && Plugin.PluginEnabled.Value)
        {
            Plugin.LogDebugMessage("Fill card table key detected");
            _isRunning = true;
            FillCardShelves();
            _isRunning = false;
        }
        if (Plugin.FillItemShelfKey.Value.IsDown() && !_isRunning && Plugin.PluginEnabled.Value)
        {
            Plugin.LogDebugMessage("Fill item shelf key detected");
            _isRunning = true;
            FillItemShelves();
            _isRunning = false;
        }
    }

    public struct WarehouseEntry
    {
        public WarehouseEntry(int shelfIndex, int compartmentIndex, int amount)
        {
            this.ShelfIndex = shelfIndex;
            this.CompartmentIndex = compartmentIndex;
            this.Amount = amount;
        }
        
        public int ShelfIndex = -1;
        public int CompartmentIndex = -1;
        public int Amount = 0;

        public override string ToString() => $"(shelf-{ShelfIndex},compartment-{CompartmentIndex},amount-{Amount})";
    }

    public enum EFillResult
    {
        Filled,
        Full,
        NoCards
    }
}