
using Archipelago.Core.Models;
using Archipelago.Core.Util;
using DSAP;
using DSAP.Models;
using Serilog;
using static DSAP.Enums;
namespace DSAP
{
    public class DarkSoulsMemory
    {
        public static void AddItem(int category, int id, int quantity)
        {
            var command = Helpers.GetItemCommand();
            //Set item category
            Array.Copy(BitConverter.GetBytes(category), 0, command, 0x1, 4);
            //Set item quantity
            Array.Copy(BitConverter.GetBytes(quantity), 0, command, 0x7, 4);
            //set item id
            Array.Copy(BitConverter.GetBytes(id), 0, command, 0xD, 4);

            var result = Memory.ExecuteCommand(command);
        }
        public static void AddItemWithMessage(int category, int id, int quantity)
        {
            var command = Helpers.GetItemWithMessage();

            // Set item category (at offset 0x3F)
            Array.Copy(BitConverter.GetBytes(category), 0, command, 0x3F, 4);

            // Set item quantity (at offset 0x43)
            Array.Copy(BitConverter.GetBytes(quantity), 0, command, 0x43, 4);

            // Set item id (at offset 0x47)
            Array.Copy(BitConverter.GetBytes(id), 0, command, 0x47, 4);

            var result = Memory.ExecuteCommand(command);
        }

        public static bool ItemPickupDialogWithoutPickup(int category, int id, int quantity)
        {
            // Tested this method of displaying messages with a 100 back to back triggers and it does not crash the game
            ulong itemPickupDialogManImpl = Helpers.GetItemPickupDialogManImplOffset();
            ItemPickupDialogLinkedList itemPickupLL = Memory.ReadStruct<ItemPickupDialogLinkedList>(itemPickupDialogManImpl);
            ulong currIdxOfLastElement = (itemPickupLL.NextAllocationInLL - itemPickupLL.StartOfLL) / 0x18;

            if (currIdxOfLastElement >= (ulong) ItemQueue.MAX_DISPLAYED_ITEMS)
            {
                return false;
            }

            LinkedListItemData itemData = itemPickupLL.Items[currIdxOfLastElement];
            itemData.ItemCategory = (uint)category;
            itemData.ItemCode = (uint)id;
            itemData.ItemCount = (uint)quantity;
            itemData.PreviousItemInLL = itemPickupLL.StartOfLL + ((currIdxOfLastElement - 1) * 0x18);
            if (currIdxOfLastElement == 0)
            {
                itemData.PreviousItemInLL = 0;
            }
            itemPickupLL.Items[currIdxOfLastElement] = itemData;
            itemPickupLL.NextAllocationInLL += 0x18;
            itemPickupLL.LastElementLinkedList = itemPickupLL.NextAllocationInLL - 0x18;

            Memory.WriteStruct<ItemPickupDialogLinkedList>(itemPickupDialogManImpl, itemPickupLL);
            return true;
        }

        /// <summary>
        /// This command triggers the anti debugger occasionally use ItemPickupDialogWithoutPickup instead
        /// <summary>
        public static void ItemPickupDialogWithoutPickupCommand(int category, int id, int quantity)
        {
            var command = Helpers.ItemPickupDialogWithoutPickup();

            // Set item category (at offset 0x3F)
            Array.Copy(BitConverter.GetBytes(category), 0, command, 0x38, 4);

            // Set item quantity (at offset 0x43)
            Array.Copy(BitConverter.GetBytes(quantity), 0, command, 0x3C, 4);

            // Set item id (at offset 0x47)
            Array.Copy(BitConverter.GetBytes(id), 0, command, 0x40, 4);

            var result = Memory.ExecuteCommand(command);
        }
        public static void FirelinkCommand(LastBonfire lastBonfire)
        {
            Helpers.SetLastBonfire(lastBonfire);
            HomewardBoneCommand();
        }

        public static void HomewardBoneCommand()
        {
            var command = Helpers.HomewardBone();

            Array.Copy(BitConverter.GetBytes(Helpers.GetBaseBOffset()), 0, command, 0x3, 4);

            var result = Memory.ExecuteCommand(command);
        }

        public static void RemoveItemPickupDialogSetupFunction()
        {
            long itemPickupDialogSetupFunction = 0x140728c90;
            var command = Helpers.InjectItemPickupDialogSwitch();
            long address = 0x1400003F0;
            int destinationIndex = 0x12;
            long offsetToItemPickupSetupFunction = itemPickupDialogSetupFunction - (address + destinationIndex);
            byte[] offsetInjectedFunctionBytes = BitConverter.GetBytes((int)offsetToItemPickupSetupFunction);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(offsetInjectedFunctionBytes);
            }
            Array.Copy(offsetInjectedFunctionBytes, 0, command, destinationIndex - 0x4, 4);
            Memory.WriteByteArray((ulong)address, command);

            long itemPickupDialogSetupFunctionCall = 0x1403fe4fa;
            long offset = address - (itemPickupDialogSetupFunctionCall + 0x5);
            byte[] injectedFuncitonCall = new byte[5];
            injectedFuncitonCall[0] = 0xE8;
            byte[] offsetBytes = BitConverter.GetBytes((int)offset);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(offsetBytes);
            }
            Array.Copy(offsetBytes, 0, injectedFuncitonCall, 1, 4);
            Memory.WriteByteArray((ulong)itemPickupDialogSetupFunctionCall, injectedFuncitonCall);

        }
        public static void RemoveItems()
        {
            var lotDictionary = Helpers.GetItemLots();
            var lotFlags = Helpers.GetItemLotFlags();

            //Helpers.WriteToFile("itemLots.json", lots);

            var replacementLot = new ItemLotParamStruct
            {
                LotRarity = 1,
                LotOverallGetItemFlagId = -1,
                LotCumulateNumFlagId = -1,
                LotCumulateNumMax = 0,
            };
            replacementLot.CumulateResetBits = 0;
            replacementLot.EnableLuckBits = 0;
            replacementLot.CumulateLotPoints[0] = 0;
            replacementLot.GetItemFlagIds[0] = -1;
            replacementLot.LotItemBasePoints[0] = 100;
            replacementLot.LotItemCategories[0] = (int)DSItemCategory.Consumables;
            replacementLot.LotItemNums[0] = 1;
            replacementLot.LotItemIds[0] = 370;

            for (int i = 0; i < lotFlags.Count; i++)
            {
                if (lotFlags[i].IsEnabled)
                {
                    List<ItemLot> lots = lotDictionary.GetValueOrDefault(lotFlags[i].Flag);
                    foreach (ItemLot lot in lots)
                    {
                        Helpers.OverwriteItemLot(lot, replacementLot);
                    }
                }
            }
            Log.Logger.Information("Finished overwriting items");
        }
        public static void ReplaceShopItems()
        {
            var lotDictionary = Helpers.GetShopLineUpItems();
            var lotFlags = Helpers.GetShopLineUpFlags();

            var replacementLot = new ShopLineUpItemParam
            {
                EquipId = 0x172,
                SoulValue = 0x100,
                MaterialId = 0x0,
                EventFlag = 0x0,
                qwcId = 0x0,
                SellQuantity = 0x100,
                ShopType = 0x0,
                EquipType = 0x0,
                padding = 0x0
            };

            for (int i = 0; i < lotFlags.Count; i++)
            {
                if (lotFlags[i].IsEnabled)
                {
                    List<ShopLineUpItem> lots = lotDictionary.GetValueOrDefault(lotFlags[i].Flag);
                    foreach (ShopLineUpItem lot in lots)
                    {
                        Helpers.OverwriteShopLineUpItem(lot, replacementLot);
                    }
                }
            }
            Log.Logger.Information("Finished overwriting items");
        }
    }
}