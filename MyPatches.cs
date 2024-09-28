using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;


namespace AutoDisplayCards;

[HarmonyPatch]
public class MyPatches
{
    private static List<int> _sortedIndexList = new List<int>();
    private static List<int> _sortedPriceList = new List<int>();
    private static bool _isRunning;
    private static Dictionary<EItemType, WarehouseEntry> _warehouseCache = new Dictionary<EItemType, WarehouseEntry>();
    
    private static void FillCardShelves()
    {
        ECardExpansionType fillType = Plugin.FillCardExpansionType.Value;
        SortAlbumByPrice(fillType);
        int index = 0;
        List<CardShelf> cardShelfList = CSingleton<ShelfManager>.Instance.m_CardShelfList;
        for (int i = 0; i < cardShelfList.Count; i++)
        {
            Plugin.LogDebugMessage("Filling shelf " + i);
            List<InteractableCardCompartment> cardCompartmentList = cardShelfList[i].GetCardCompartmentList();
            for (int j = 0; j < cardCompartmentList.Count; j++)
            {
                Plugin.LogDebugMessage("Filling compartment " + j);
                bool filled = MyPatches.FillCardSlot(cardCompartmentList[j], index, fillType);
                if (filled)
                {
                    index++;
                }
            }
        }
    }
    
    private static bool FillCardSlot(InteractableCardCompartment comp, int index, ECardExpansionType expansionType)
    {
        if (comp.m_StoredCardList.Count == 0)
        {
            Plugin.LogDebugMessage("Filling card slot with index " + index);
            InteractableCard3d card3d = CreateInteractableCard3d(_sortedIndexList[index], expansionType);
            comp.SetCardOnShelf(card3d);
            CPlayerData.ReduceCardUsingIndex(_sortedIndexList[index], expansionType, false, 1);
            Plugin.LogDebugMessage("Finished reducing card and setting on shelf");
            return true;
        }
        else
        {
            InteractableCard3d debug = comp.m_StoredCardList.First();
            string monsterName = debug.m_Card3dUI.m_CardUI.GetCardData().monsterType.ToString();
            Plugin.LogDebugMessage("Card detected: " + monsterName);
            return false;
        }
    }

    private static void SortAlbumByPrice(ECardExpansionType expansionType)
    {
        Plugin.LogDebugMessage("Started sorting.");
        _sortedIndexList.Clear();
        _sortedPriceList.Clear();
        int num = 0;
        for (int i = 0; i < InventoryBase.GetShownMonsterList(expansionType).Count; i++)
        {
            Plugin.LogDebugMessage("Sorting monster list " + i);
            for (int j = 0; j < CPlayerData.GetCardAmountPerMonsterType(expansionType, true); j++)
            {
                Plugin.LogDebugMessage("Checking monster id " + j);
                CardData cardData = new CardData();
                cardData.monsterType = CPlayerData.GetMonsterTypeFromCardSaveIndex(num, expansionType);
                cardData.borderType = (ECardBorderType)(num % CPlayerData.GetCardAmountPerMonsterType(expansionType, false));
                cardData.isFoil = num % CPlayerData.GetCardAmountPerMonsterType(expansionType, true) >= CPlayerData.GetCardAmountPerMonsterType(expansionType, false);
                cardData.isDestiny = false;
                cardData.expansionType = expansionType;
                int cardAmount = CPlayerData.GetCardAmount(cardData);
                int num2 = 0;
                if (cardAmount > 0)
                {
                    num2 = Mathf.RoundToInt(CPlayerData.GetCardMarketPrice(cardData) * 100f);
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
                _sortedPriceList.Insert(num3, num2);
                _sortedIndexList.Insert(num3, num);
                num++;
            }
        }
        Plugin.LogDebugMessage("Sorted " + num + " cards.");
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
        cardUI.m_CardUI.SetCardUI(CPlayerData.GetCardData(cardIndex, expansionType, false));
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
        List<Shelf> shelfList = CSingleton<ShelfManager>.Instance.m_ShelfList;
        for (int i = 0; i < shelfList.Count; i++)
        {
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
                    ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(toFillType);
                    Item item = ItemSpawnManager.GetItem(shelfCompartment.m_StoredItemListGrp);
                    item.SetMesh(itemMeshData.mesh, itemMeshData.material, toFillType, itemMeshData.meshSecondary, itemMeshData.materialSecondary);
                    item.transform.position = shelfCompartment.GetEmptySlotTransform().position;
                    item.transform.localScale = shelfCompartment.GetEmptySlotTransform().localScale;
                    item.transform.rotation = shelfCompartment.m_StartLoc.rotation;
                    item.gameObject.SetActive(true);
                    shelfCompartment.AddItem(item, false);
                }
                Plugin.LogDebugMessage("Added " + entry.Amount + " items");
                while (wareComp.GetInteractablePackagingBoxList().Count >0)
                {
                    InteractablePackagingBox_Item box = wareComp.GetLastInteractablePackagingBox();
                    Plugin.LogDebugMessage("Starting box cleanup");
                    RestockManager.RemoveItemPackageBox(box);
                    Plugin.LogDebugMessage("Box removed from restock manager");
                    wareComp.RemoveBox(box);
                    Plugin.LogDebugMessage("Box removed from shelf");
                    WorkerManager.Instance.m_TrashBin.DiscardBox(box, false);
                    Plugin.LogDebugMessage("Box discarded to trash");
                }
                Plugin.LogDebugMessage("Deleted empty boxes");
            }
            else
            {
                List<InteractablePackagingBox_Item> toDelete = new List<InteractablePackagingBox_Item>();
                for (int b = wareComp.GetInteractablePackagingBoxList().Count - 1; b >= 0; b--)
                {
                    InteractablePackagingBox_Item box = wareComp.GetInteractablePackagingBoxList()[b];
                    if (toFillAmount == 0) break;
                    int inBox = box.m_ItemCompartment.GetItemCount();
                    if (inBox <= toFillAmount)
                    {
                        for (int a = 0; a < inBox; a++)
                        {
                            ItemMeshData itemMeshData = InventoryBase.GetItemMeshData(toFillType);
                            Item item = ItemSpawnManager.GetItem(shelfCompartment.m_StoredItemListGrp);
                            item.SetMesh(itemMeshData.mesh, itemMeshData.material, toFillType, itemMeshData.meshSecondary, itemMeshData.materialSecondary);
                            item.transform.position = shelfCompartment.GetEmptySlotTransform().position;
                            item.transform.localScale = shelfCompartment.GetEmptySlotTransform().localScale;
                            item.transform.rotation = shelfCompartment.m_StartLoc.rotation;
                            item.gameObject.SetActive(true);
                            //shelfCompartment.SpawnItem(entry.Amount, false);
                            shelfCompartment.AddItem(item, false);
                        }
                        Plugin.LogDebugMessage("Filled compartment with " + inBox + " items");
                        toDelete.Add(box);
                        if (inBox == toFillAmount)
                        {
                            toFillAmount = 0;
                            break;
                        }
                        Plugin.LogDebugMessage("to fill amount before partial box: " + toFillAmount);
                        toFillAmount -= inBox;
                        Plugin.LogDebugMessage("to fill amount after partial box: " + toFillAmount);
                    }
                    else
                    {
                        Plugin.LogDebugMessage("To fill amount before half box: " + toFillAmount);
                        for (int a = 0; a < toFillAmount; a++)
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
                        Plugin.LogDebugMessage("Filled compartment with " + toFillAmount + " items");
                        for (int l = 0; l < toFillAmount; l++)
                        {
                            box.m_ItemCompartment.RemoveItem(box.m_ItemCompartment.GetFirstItem());
                            box.m_ItemCompartment.CalculatePositionList();
                            box.m_ItemCompartment.RefreshItemPosition(false);
                            wareComp.SetPriceTagItemAmountText();
                        }
                        Plugin.LogDebugMessage("Removed " + toFillAmount + " items from box");
                        toFillAmount = 0;
                    }
                }
                foreach (InteractablePackagingBox_Item box in toDelete)
                {
                    Plugin.LogDebugMessage("Starting box cleanup");
                    RestockManager.RemoveItemPackageBox(box);
                    Plugin.LogDebugMessage("Box removed from restock manager");
                    wareComp.RemoveBox(box);
                    Plugin.LogDebugMessage("Box removed from shelf");
                    WorkerManager.Instance.m_TrashBin.DiscardBox(box, false);
                    Plugin.LogDebugMessage("Box discarded to trash");
                }
            }
        }
        else
        {
            Plugin.LogDebugMessage("Item not found in warehouse or shelf empty");
        }
    }
    
    [HarmonyPatch(typeof(CGameManager), "Update")]
    [HarmonyPrefix]
    public static void GameManagerUpdatePrefix()
    {
        if (Plugin.FillCardTableKey.Value.IsDown() && !_isRunning)
        {
            Plugin.LogDebugMessage("Fill card table key detected");
            _isRunning = true;
            FillCardShelves();
            _isRunning = false;
        }
        if (Plugin.FillItemShelfKey.Value.IsDown() && !_isRunning)
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
}