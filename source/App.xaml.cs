using Archipelago.Core;
using Archipelago.Core.MauiGUI;
using Archipelago.Core.MauiGUI.Models;
using Archipelago.Core.MauiGUI.ViewModels;
using Archipelago.Core.Models;
using Archipelago.Core.Traps;
using Archipelago.Core.Util;
using Archipelago.Core.Util.Overlay;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using DSAP.Models;
using Newtonsoft.Json;
using Serilog;
using static DSAP.Enums;
using Color = Microsoft.Maui.Graphics.Color;
namespace DSAP
{
    public partial class App : Application
    {
        static MainPageViewModel Context;
        private DeathLinkService _deathlinkService;

        public static ArchipelagoClient Client { get; set; }
        public static List<DarkSoulsItem> AllItems { get; set; }
        private static readonly object _lockObject = new object();
        private bool IsHandlingDeathlink = false;
        private static ItemQueue itemQueue = new ItemQueue();

        public static string Slot;

        public App()
        {
            InitializeComponent();

            Context = new MainPageViewModel();
            Context.ConnectClicked += Context_ConnectClicked;
            Context.UnstuckVisible = true;
            Context.CommandReceived += (e, a) =>
            {
                Client?.SendMessage(a.Command);
            };
            MainPage = new MainPage(Context);
            Context.ConnectButtonEnabled = true;
        }

        private void Context_UnstuckClicked(object? sender, EventArgs e)
        {
            //var bonfireStates = Helpers.GetBonfireStates();
            //Log.Logger.Information(JsonConvert.SerializeObject(bonfireStates));
            //var originalLots = Helpers.GetItemLots();
            //RemoveItems();
            //var overwrittenLots = Helpers.GetItemLots();

            //if (originalLots == overwrittenLots)
            //{
            //    Log.Error("Overwriting itemlots failed.");
            //}

            DarkSoulsMemory.HomewardBoneCommand();
        }

        public static bool IsValidPointer(ulong address)
        {
            try
            {
                Memory.ReadByte(address);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void Context_ConnectClicked(object? sender, ConnectClickedEventArgs e)
        {
            Context.ConnectButtonEnabled = false;
            Log.Logger.Information("Connecting...");
            if (Client != null)
            {
                Client.isReadyToReceiveItems = false;
                Client.Connected -= OnConnected;
                Client.Disconnected -= OnDisconnected;
                Client.ItemReceived -= Client_ItemReceived;
                Client.MessageReceived -= Client_MessageReceived;
                Context.UnstuckClicked -= Context_UnstuckClicked;
                //if (_deathlinkService != null)
                //{
                //    _deathlinkService.OnDeathLinkReceived -= _deathlinkService_OnDeathLinkReceived;
                //    _deathlinkService = null;
                //}
                Client.CancelMonitors();
            }
            DarkSoulsClient client = new DarkSoulsClient();
            var connected = client.Connect();
            if (!connected)
            {
                Log.Logger.Error("Dark Souls not running, open Dark Souls before connecting!");
                Context.ConnectButtonEnabled = true;
                return;
            }

            Client = new ArchipelagoClient(client);

            AllItems = Helpers.GetAllItems();
            Client.Connected += OnConnected;
            Client.Disconnected += OnDisconnected;
            var isOnline = Helpers.GetIsPlayerOnline();
            if (isOnline)
            {
                Log.Logger.Warning("YOU ARE PLAYING ONLINE. THIS APPLICATION WILL NOT PROCEED.");
                Context.ConnectButtonEnabled = true;
                return;
            }

            var isInGame = Helpers.GetIsPlayerInGame();
            if (!isInGame)
            {
                Log.Logger.Warning("Please load into the game with your character to connect.");
                Context.ConnectButtonEnabled = true;
                return;
            }

            await Client.Connect(e.Host, "Dark Souls Remastered");

            Client.ItemReceived += Client_ItemReceived;
            Client.MessageReceived += Client_MessageReceived;
            Context.UnstuckClicked += Context_UnstuckClicked;

            await Client.Login(e.Slot, !string.IsNullOrWhiteSpace(e.Password) ? e.Password : null);

            Slot = e.Slot;

            Client.IntializeOverlayService(new WindowsOverlayService());

            //if (Client.Options.ContainsKey("enable_deathlink") && (bool)Client.Options["enable_deathlink"])
            //{
            //    _deathlinkService = Client.EnableDeathLink();
            //    _deathlinkService.OnDeathLinkReceived += _deathlinkService_OnDeathLinkReceived;
            //    Memory.MonitorAddressForAction<int>(Helpers.GetPlayerHPAddress(), () => SendDeathlink(_deathlinkService), (health) => Helpers.GetPlayerHP() <= 0);
            //}

            itemQueue.CleanUpItemPickupText();
            RemoveItems();
            DarkSoulsMemory.RemoveItemPickupDialogSetupFunction();

            //need to reload the area on connect to ensure that the item lots are updated 
            DarkSoulsMemory.HomewardBoneCommand();

            var bossLocations = Helpers.GetBossFlagLocations();
            var itemLocations = Helpers.GetItemLotLocations();
            var bonfireLocations = Helpers.GetBonfireFlagLocations();
            var doorLocations = Helpers.GetDoorFlagLocations();
            var fogWallLocations = Helpers.GetFogWallFlagLocations();
            var miscLocations = Helpers.GetMiscFlagLocations();

            var goalLocation = bossLocations.First(x => x.Name.Contains("Lord of Cinder"));
            Memory.MonitorAddressBitForAction(goalLocation.Address, goalLocation.AddressBit, () => Client.SendGoalCompletion());
            foreach (var fogWall in fogWallLocations)
            {
                Memory.MonitorAddressBitForAction(fogWall.Address, fogWall.AddressBit, () =>
                {
                    Log.Debug($"Fog Wall Opened: {fogWall.Name}");
                });
            }


            Client.MonitorLocations(bossLocations);
            Client.MonitorLocations(itemLocations);
            Client.MonitorLocations(bonfireLocations);
            Client.MonitorLocations(doorLocations);
            // Client.MonitorLocations(fogWallLocations);
            Client.MonitorLocations(miscLocations);

            //Helpers.MonitorLastBonfire((lastBonfire) =>
            //{
            //    Log.Logger.Debug($"Rested at bonfire: {lastBonfire.id}:{lastBonfire.name}");
            //});

            Memory.MonitorAddressByteChangeForAction(Helpers.GetItemPickupDialog(), 0x1, 0x0, () => itemQueue.CleanUpItemPickupText());
            Task.Run(async delegate
            {
                while (true)
                {
                    await Task.Delay(100);
                    itemQueue.TryDisplayItem();
                }
            });

            Context.ConnectButtonEnabled = true;
            Client.isReadyToReceiveItems = true;
        }

        //private void SendDeathlink(DeathLinkService _deathlinkService)
        //{
        //    if (!IsHandlingDeathlink)
        //    {
        //        Log.Logger.Information("Sending Deathlink. RIP.");
        //        _deathlinkService.SendDeathLink(new DeathLink(Client.CurrentSession.Players.ActivePlayer.Name));
        //    }

        //    //Restart deathlink when player is alive again
        //    Memory.MonitorAddressForAction<int>(Helpers.GetPlayerHPAddress(), 
        //        () => {
        //            IsHandlingDeathlink = false;
        //            Memory.MonitorAddressForAction<int>(Helpers.GetPlayerHPAddress(),
        //                () => SendDeathlink(_deathlinkService),
        //                (health) => Helpers.GetPlayerHP() <= 0);
        //            },
        //        (health) => Helpers.GetPlayerHP() > 0);
        //}
        //private void _deathlinkService_OnDeathLinkReceived(DeathLink deathLink)
        //{
        //    Log.Logger.Information("Deathlink received. RIP.");
        //    IsHandlingDeathlink = true;
        //    Memory.Write(Helpers.GetPlayerHPAddress(), 0);
        //}

        private void Client_MessageReceived(object? sender, Archipelago.Core.Models.MessageReceivedEventArgs e)
        {
            MessagePart[] Parts = e.Message.Parts;
            if (Parts.Any(x => x.Text == "[Hint]: "))
            {
                LogHint(e.Message);
            }
            else if (Parts.Count() >= 5 && Parts[0].Text == Slot && Parts.Any(x => x.Text.Contains("sent")))
            {
                DarkSoulsItem otherWorldItem = new DarkSoulsItem(Parts[4].Text + " " + Parts[2].Text);
                itemQueue.DisplayOtherWorldItemPickupText(otherWorldItem);
            }
            else if (Parts.Length == 1 && Parts[0].Text.StartsWith(Slot + ": /"))
            {
                string[] commandParts = Parts[0].Text.Split(" ");
                if (commandParts.Length >= 2 && commandParts[1] == "/warp")
                {
                    string bonfireName = string.Join(" ", commandParts.Skip(2));
                    if (bonfireName.Length == 0)
                    {
                        bonfireName = "Firelink Shrine [Bonfire]";
                    }
                    List<LastBonfire> bonfires = Helpers.GetLastBonfireList();
                    List<LastBonfire> queryResults = bonfires.FindAll(x => x.name.ToLower().Contains(bonfireName.ToLower()));
                    if (queryResults.Count() == 1)
                    {
                        DarkSoulsMemory.FirelinkCommand(queryResults[0]);
                    }
                    else
                    {
                        Log.Logger.Information("Could not find bonfire try one of: ");
                        foreach(LastBonfire bonfire in queryResults) {
                            Log.Logger.Information(bonfire.name);
                        }
                    }
                }   
            }

            Log.Logger.Information(JsonConvert.SerializeObject(e.Message));
            Client.AddOverlayMessage(e.Message.ToString());
        }

        private static void RemoveItems()
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
        private static void Client_ItemReceived(object? sender, ItemReceivedEventArgs e)
        {
            Item item = e.Item;
            int itemAPId = (int)item.Id;
            DarkSoulsItem fakeItem = new DarkSoulsItem(item.Name);
            DarkSoulsItem itemToReceive = AllItems.FirstOrDefault(x => x.ApId == itemAPId, fakeItem);
            LogItem(e.Item, itemToReceive.StackSize);
            itemQueue.DisplayItemPickupText(itemToReceive);
        }
        public static async void RunLagTrap()
        {
            using (var lagTrap = new LagTrap(TimeSpan.FromSeconds(20)))
            {
                lagTrap.Start();
                await lagTrap.WaitForCompletionAsync();
            }
        }

        private static void LogItem(Item item, int quantity)
        {
            var messageToLog = new LogListItem(new List<TextSpan>()
            {
                new TextSpan(){Text = $"[{item.Id.ToString()}] -", TextColor = Color.FromRgb(255, 255, 255)},
                new TextSpan(){Text = $"{item.Name}", TextColor = Color.FromRgb(200, 255, 200)},
                new TextSpan(){Text = $"x{quantity.ToString()}", TextColor = Color.FromRgb(200, 255, 200)}
            });
            lock (_lockObject)
            {
                Application.Current.Dispatcher.DispatchAsync(() =>
                {
                    Context.ItemList.Add(messageToLog);
                });
            }

            Client.AddOverlayMessage($"Received [{item.Id.ToString()}] - {item.Name}");

        }
        private static void LogHint(LogMessage message)
        {
            var newMessage = message.Parts.Select(x => x.Text);

            if (Context.HintList.Any(x => x.TextSpans.Select(y => y.Text) == newMessage))
            {
                return; //Hint already in list
            }
            List<TextSpan> spans = new List<TextSpan>();
            foreach (var part in message.Parts)
            {
                spans.Add(new TextSpan() { Text = part.Text, TextColor = Color.FromRgb(part.Color.R, part.Color.G, part.Color.B) });
            }
            lock (_lockObject)
            {
                Application.Current.Dispatcher.DispatchAsync(() =>
                {
                    Context.HintList.Add(new LogListItem(spans));
                });
            }
        }
        private static void OnConnected(object sender, EventArgs args)
        {
            Log.Logger.Information("Connected to Archipelago");
            Log.Logger.Information($"Playing {Client.CurrentSession.ConnectionInfo.Game} as {Client.CurrentSession.Players.GetPlayerName(Client.CurrentSession.ConnectionInfo.Slot)}");
        }

        private static void OnDisconnected(object sender, EventArgs args)
        {
            Log.Logger.Information("Disconnected from Archipelago");
        }
        protected override Window CreateWindow(IActivationState activationState)
        {
            var window = base.CreateWindow(activationState);
            if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            {
                window.Title = "DSAP - Dark Souls Archipelago Randomizer";

            }
            window.Width = 600;

            return window;
        }
    }
}
