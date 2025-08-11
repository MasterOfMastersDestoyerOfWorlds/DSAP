using Archipelago.Core.Models;
using DSAP.Models;
using Serilog;
using static DSAP.Enums;

namespace DSAP
{
    public class ItemQueue : Queue<DarkSoulsItem>
    {

        public List<InjectedString> injectedStrings;
        public static int MAX_DISPLAYED_ITEMS = 5;

        public ItemQueue()
        {
            injectedStrings = new List<InjectedString>();
        }
        
        public void CleanUpItemPickupText()
        {
            foreach (InjectedString injString in injectedStrings)
            {
                Helpers.FreeItemPickupText(injString);
            }
            injectedStrings = new List<InjectedString>();
        }

        public void TryDisplayItem()
        {
            if (!CanDisplayItem())
            {
                return;
            }
            while (injectedStrings.Count() == 0 && Helpers.GetDisplayedItemCount() < MAX_DISPLAYED_ITEMS && this.Count() > 0)
            {
                DarkSoulsItem item = Dequeue();
                if (item.otherWorldItem)
                {
                    DisplayOtherWorldItemPickupText(item);
                }
                else
                {
                    DisplayItemPickupText(item);
                }
            }
        }

        public bool CanDisplayItem()
        {
            return Helpers.GetIsPlayerInGame();
        }

        public bool DisplayItemPickupText(DarkSoulsItem item)
        {
            int displayedItemCount = Helpers.GetDisplayedItemCount();
            if (displayedItemCount >= ItemQueue.MAX_DISPLAYED_ITEMS)
            {
                this.Enqueue(item);
                return false;
            }
            int itemCount = item.StackSize == 0 ? 1 : item.StackSize;

            Log.Logger.Verbose($"Received {item.Name} ({item.ApId})");
            if (item.ApId == 11120000)
            {
                App.RunLagTrap();
            }
            else
            {
                DarkSoulsMemory.AddItem((int)item.Category, item.Id, itemCount);
                DarkSoulsMemory.ItemPickupDialogWithoutPickup(((int)item.Category), item.Id, itemCount);
            }
            return true;
        }

        public bool DisplayOtherWorldItemPickupText(DarkSoulsItem item)
        {
            if (injectedStrings.Count > 0)
            {
                this.Enqueue(item);
                return false;
            }
            InjectedString injString = Helpers.SetItemPickupText(item);
            injectedStrings.Add(injString);
            DarkSoulsMemory.ItemPickupDialogWithoutPickup(((int)item.Category), item.Id, 1);
            return true;
        }
    }
}