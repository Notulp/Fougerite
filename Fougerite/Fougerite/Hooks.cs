﻿namespace Fougerite
{
    using Facepunch.Utility;
    using Fougerite.Events;
    using Rust;
    using RustProto;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Linq;
    using uLink;
    using UnityEngine;

    public class Hooks
    {
        public static System.Collections.Generic.List<object> decayList = new System.Collections.Generic.List<object>();
        public static Hashtable talkerTimers = new Hashtable();

        public static event BlueprintUseHandlerDelegate OnBlueprintUse;
        public static event ChatHandlerDelegate OnChat;
        public static event ChatRawHandlerDelegate OnChatRaw;
        public static event CommandHandlerDelegate OnCommand;
        public static event CommandRawHandlerDelegate OnCommandRaw;
        public static event ConsoleHandlerDelegate OnConsoleReceived;
        public static event DoorOpenHandlerDelegate OnDoorUse;
        public static event EntityDecayDelegate OnEntityDecay;
        public static event EntityDeployedDelegate OnEntityDeployed;
        public static event EntityHurtDelegate OnEntityHurt;
        public static event EntityDestroyedDelegate OnEntityDestroyed;
        public static event ItemsDatablocksLoaded OnItemsLoaded;
        public static event HurtHandlerDelegate OnNPCHurt;
        public static event KillHandlerDelegate OnNPCKilled;
        public static event ConnectionHandlerDelegate OnPlayerConnected;
        public static event DisconnectionHandlerDelegate OnPlayerDisconnected;
        public static event PlayerGatheringHandlerDelegate OnPlayerGathering;
        public static event HurtHandlerDelegate OnPlayerHurt;
        public static event KillHandlerDelegate OnPlayerKilled;
        public static event PlayerSpawnHandlerDelegate OnPlayerSpawned;
        public static event PlayerSpawnHandlerDelegate OnPlayerSpawning;
        public static event PluginInitHandlerDelegate OnPluginInit;
        public static event ServerInitDelegate OnServerInit;
        public static event ServerShutdownDelegate OnServerShutdown;
        public static event ShowTalkerDelegate OnShowTalker;
        public static event LootTablesLoaded OnTablesLoaded;
        public static event ModulesLoadedDelegate OnModulesLoaded;

        public static void BlueprintUse(IBlueprintItem item, BlueprintDataBlock bdb)
        {
            Fougerite.Player player = Fougerite.Player.FindByPlayerClient(item.controllable.playerClient);
            if (player != null)
            {
                BPUseEvent ae = new BPUseEvent(bdb);
                if (OnBlueprintUse != null)
                {
                    OnBlueprintUse(player, ae);
                }
                if (!ae.Cancel)
                {
                    PlayerInventory internalInventory = player.Inventory.InternalInventory as PlayerInventory;
                    if (internalInventory.BindBlueprint(bdb))
                    {
                        int count = 1;
                        if (item.Consume(ref count))
                        {
                            internalInventory.RemoveItem(item.slot);
                        }
                        player.Notice("", "You can now craft: " + bdb.resultItem.name, 4f);
                    }
                    else
                    {
                        player.Notice("", "You already have this blueprint", 4f);
                    }
                }
            }
        }

        public static void ChatReceived(ref ConsoleSystem.Arg arg)
        {
            if (!chat.enabled)
                return;

            if (string.IsNullOrEmpty(arg.ArgsStr))
                return;

            var quotedName = arg.argUser.displayName.QuoteSafe();
            var quotedMessage = arg.GetString(0).QuoteSafe();
            if (arg.GetString(0).StartsWith("/"))
                Logger.LogDebug(string.Format("[CHAT-CMD] {0} executed {1}", quotedName, quotedMessage));

            if (OnChatRaw != null)
                OnChatRaw(ref arg);

            if (string.IsNullOrEmpty(arg.ArgsStr))
                return;

            if (quotedMessage.Trim('"').StartsWith("/")) {
                string[] args = Facepunch.Utility.String.SplitQuotesStrings(quotedMessage.Trim('"'));
                var command = args[0].TrimStart('/');
                var cargs = new string[args.Length - 1];
                Array.Copy(args, 1, cargs, 0, cargs.Length);

                if (OnCommand != null)
                    OnCommand(Fougerite.Player.FindByPlayerClient(arg.argUser.playerClient), command, cargs);

            } else {
                Logger.ChatLog(quotedName, quotedMessage);               
                var chatstr = new ChatString(quotedMessage);
                if(OnChat != null)
                    OnChat(Fougerite.Player.FindByPlayerClient(arg.argUser.playerClient), ref chatstr);

                string newchat = chatstr.NewText.Substring(1, chatstr.NewText.Length - 2).QuoteSafe().Replace("\\\"", "" + '\u0022');

                if (string.IsNullOrEmpty(newchat))
                    return;

                Fougerite.Data.GetData().chat_history.Add(newchat);
                Fougerite.Data.GetData().chat_history_username.Add(quotedName);                                                   
                ConsoleNetworker.Broadcast(string.Format("chat.add {0} {1}", quotedName, newchat));
            }
        }

        public static bool ConsoleReceived(ref ConsoleSystem.Arg a)
        {
            StringComparison ic = StringComparison.InvariantCultureIgnoreCase;
            bool external = a.argUser == null;
            bool adminRights = (a.argUser != null && a.argUser.admin) || external;

            string userid = "[external][external]";
            if (adminRights && !external)
                userid = string.Format("[{0}][{1}]", a.argUser.displayName, a.argUser.userID.ToString());

            string logmsg = string.Format("[ConsoleReceived] userid={0} adminRights={1} command={2}.{3} args={4}", userid, adminRights.ToString(), a.Class, a.Function, (a.HasArgs(1) ? a.ArgsStr : "none"));
            Logger.LogDebug(logmsg);

            if (a.Class.Equals("fougerite", ic) && a.Function.Equals("reload", ic)) {
                if (adminRights) {
                    ModuleManager.ReloadModules();
                    a.ReplyWith("Fougerite: Reloaded!");
                }
            } else if (OnConsoleReceived != null)
                OnConsoleReceived(ref a, external);

            if (string.IsNullOrEmpty(a.Reply))
                a.ReplyWith(string.Format("Fougerite: {0}.{1} was executed!", a.Class, a.Function));

            return true;
        }

        public static bool CheckOwner(DeployableObject obj, Controllable controllable)
        {
            DoorEvent de = new DoorEvent(new Entity(obj));
            if (obj.ownerID == controllable.playerClient.userID)
                de.Open = true;

            if (!(obj is SleepingBag) && OnDoorUse != null)
                OnDoorUse(Fougerite.Player.FindByPlayerClient(controllable.playerClient), de);

            return de.Open;
        }

        public static float EntityDecay(object entity, float dmg)
        {
            if (entity == null)
                return 0f;

            try
            {
                DecayEvent de = new DecayEvent(new Entity(entity), ref dmg);
                if (OnEntityDecay != null)
                    OnEntityDecay(de);

                if (decayList.Contains(entity))
                    decayList.Remove(entity);

                decayList.Add(entity);
                return de.DamageAmount;
            }
            catch  { }
            return 0f;
        }

        public static void EntityDeployed(object entity)
        {
            Entity e = new Entity(entity);
            Fougerite.Player creator = e.Creator;

            if (OnEntityDeployed != null)
                OnEntityDeployed(creator, e);
        }

        public static void EntityHurt(object entity, ref DamageEvent e)
        {
            if (entity == null)
                return;

            try
            {
                HurtEvent he = new HurtEvent(ref e, new Entity(entity));
                if (decayList.Contains(entity))
                    he.IsDecay = true;

                if (he.Entity.IsStructure() && !he.IsDecay)
                {
                    StructureComponent component = entity as StructureComponent;
                    if ((component.IsType(StructureComponent.StructureComponentType.Ceiling) || component.IsType(StructureComponent.StructureComponentType.Foundation)) || component.IsType(StructureComponent.StructureComponentType.Pillar))
                        he.DamageAmount = 0f;
                }
                TakeDamage takeDamage = he.Entity.GetTakeDamage();
                takeDamage.health += he.DamageAmount;

                // when entity is destroyed
                if (e.status != LifeStatus.IsAlive)
                {
                    DestroyEvent de = new DestroyEvent(ref e, new Entity(entity), he.IsDecay);
                    if (OnEntityDestroyed != null)
                        OnEntityDestroyed(de);
                }
                else if (OnEntityHurt != null)
                    OnEntityHurt(he);

                Zone3D zoned = Zone3D.GlobalContains(he.Entity);
                if ((zoned == null) || !zoned.Protected)
                {
                    if ((he.Entity.GetTakeDamage().health - he.DamageAmount) <= 0f)
                        he.Entity.Destroy();
                    else
                    {
                        TakeDamage damage2 = he.Entity.GetTakeDamage();
                        damage2.health -= he.DamageAmount;
                    }
                }
            }
            catch { }
        }

        public static void hijack(string name)
        {
            if ((((name != "!Ng") && (name != ":rabbit_prefab_a")) && ((name != ";res_woodpile") && (name != ";res_ore_1"))) && ((((((((((((((name != ";res_ore_2") & (name != ";res_ore_3")) & (name != ":stag_prefab")) & (name != ":boar_prefab")) & (name != ":chicken_prefab")) & (name != ":bear_prefab")) & (name != ":wolf_prefab")) & (name != ":mutant_bear")) & (name != ":mutant_wolf")) & (name != "AmmoLootBox")) & (name != "MedicalLootBox")) & (name != "BoxLoot")) & (name != "WeaponLootBox")) & (name != "SupplyCrate")))
                Logger.LogDebug("Hijack: " + name);
        }

        public static ItemDataBlock[] ItemsLoaded(System.Collections.Generic.List<ItemDataBlock> items, Dictionary<string, int> stringDB, Dictionary<int, int> idDB)
        {
            ItemsBlocks blocks = new ItemsBlocks(items);
            if (OnItemsLoaded != null)
                OnItemsLoaded(blocks);

            int num = 0;
            foreach (ItemDataBlock block in blocks)
            {
                stringDB.Add(block.name, num);
                idDB.Add(block.uniqueID, num);
                num++;
            }
            Fougerite.Server.GetServer().Items = blocks;
            return blocks.ToArray();
        }

        public static void NPCHurt(ref DamageEvent e)
        {
            HurtEvent he = new HurtEvent(ref e);
            if ((he.Victim as NPC).Health > 0f)
            {
                NPC victim = he.Victim as NPC;
                victim.Health += he.DamageAmount;
                if (OnNPCHurt != null)
                    OnNPCHurt(he);
                if (((he.Victim as NPC).Health - he.DamageAmount) <= 0f)
                    (he.Victim as NPC).Kill();
                else
                {
                    NPC npc2 = he.Victim as NPC;
                    npc2.Health -= he.DamageAmount;
                }
            }
        }

        public static void NPCKilled(ref DamageEvent e)
        {
            try
            {
                DeathEvent de = new DeathEvent(ref e);
                if (OnNPCKilled != null)
                    OnNPCKilled(de);
            }
            catch { }
        }

        public static bool PlayerConnect(NetUser user)
        {
            bool connected = false;

            if (user.playerClient == null) {
                Logger.LogDebug("PlayerConnect user.playerClient is null");
                return connected;
            }

            Fougerite.Server server = Fougerite.Server.GetServer();
            Fougerite.Player player = new Fougerite.Player(user.playerClient);
            if (server.Players.Contains(player))
            {
                Logger.LogError(string.Format("[PlayerConnect] Server.Players already contains {0} {1}", player.Name, player.SteamID));
                connected = user.connected;
                return connected;
            }
            server.Players.Add(player);
            if (!Fougerite.Player.Cache.ContainsKey(user.userID))
            {
                Fougerite.Player.Cache.Add(user.userID, user.displayName);
            }
            else if (user.displayName != Fougerite.Player.Cache[user.userID])
            {
                Fougerite.Player.Cache[user.userID] = user.displayName;
            }
            if (OnPlayerConnected != null)
                OnPlayerConnected(player);

            connected = user.connected;

            if (Fougerite.Config.GetBoolValue("Fougerite", "tellversion"))
                player.Message(string.Format("This server is powered by Fougerite v.{0}!", Bootstrap.Version));

            return connected;
        }

        public static void PlayerDisconnect(NetUser user)
        {
            try
            {
                Fougerite.Player item = Fougerite.Player.FindByPlayerClient(user.playerClient);
                if (item == null)
                    return;

                Fougerite.Server.GetServer().Players.Remove(item);
                Logger.LogDebug("User Disconnected: " + item.Name + " (" + item.SteamID + ")");
                if (OnPlayerDisconnected != null)
                    OnPlayerDisconnected(item);

            }
            catch { }
        }

        public static void PlayerGather(Inventory rec, ResourceTarget rt, ResourceGivePair rg, ref int amount)
        {
            try
            {
                Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.networkView.owner);
                GatherEvent ge = new GatherEvent(rt, rg, amount);
                if (OnPlayerGathering != null)
                {
                    OnPlayerGathering(player, ge);
                }
                amount = ge.Quantity;
                if (!ge.Override)
                {
                    amount = Mathf.Min(amount, rg.AmountLeft());
                }
                rg._resourceItemDatablock = ge.Item;
                rg.ResourceItemName = ge.Item;
            }
            catch { }
        }

        public static void PlayerGatherWood(IMeleeWeaponItem rec, ResourceTarget rt, ref ItemDataBlock db, ref int amount, ref string name)
        {
            try
            {
                Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.inventory.networkView.owner);
                GatherEvent ge = new GatherEvent(rt, db, amount);
                ge.Item = "Wood";
                if (OnPlayerGathering != null)
                {
                    OnPlayerGathering(player, ge);
                }
                db = Fougerite.Server.GetServer().Items.Find(ge.Item);
                amount = ge.Quantity;
                name = ge.Item;
            }
            catch { }
        }

        public static void PlayerHurt(ref DamageEvent e)
        {
            try
            {
                HurtEvent he = new HurtEvent(ref e);
                if (!(he.Attacker is NPC) && !(he.Victim is NPC))
                {
                    Fougerite.Player attacker = he.Attacker as Fougerite.Player;
                    Fougerite.Player victim = he.Victim as Fougerite.Player;
                    Zone3D zoned = Zone3D.GlobalContains(attacker);
                    if ((zoned != null) && !zoned.PVP)
                    {
                        attacker.Message("You are in a PVP restricted area.");
                        he.DamageAmount = 0f;
                        e = he.DamageEvent;
                        return;
                    }
                    zoned = Zone3D.GlobalContains(victim);
                    if ((zoned != null) && !zoned.PVP)
                    {
                        attacker.Message(victim.Name + " is in a PVP restricted area.");
                        he.DamageAmount = 0f;
                        e = he.DamageEvent;
                        return;
                    }
                }
                if (OnPlayerHurt != null)
                    OnPlayerHurt(he);
                e = he.DamageEvent;
            }
            catch { }
        }

        public static bool PlayerKilled(ref DamageEvent de)
        {
            bool flag = false;
            DeathEvent event2 = null;
            try
            {
                event2 = new DeathEvent(ref de);
                flag = event2.DropItems;
            }
            catch
            { 
                return flag;
            }
            if (OnPlayerKilled != null && event2 != null)
            {
                try
                {
                    OnPlayerKilled(event2);
                    flag = event2.DropItems;
                }
                catch { }
            }
            return flag;
        }

        public static void PlayerSpawned(PlayerClient pc, Vector3 pos, bool camp)
        {
            try
            {
                Fougerite.Player player = Fougerite.Player.FindByPlayerClient(pc);
                SpawnEvent se = new SpawnEvent(pos, camp);
                if ((OnPlayerSpawned != null) && (player != null))
                {
                    OnPlayerSpawned(player, se);
                }
            }
            catch { }
        }

        public static Vector3 PlayerSpawning(PlayerClient pc, Vector3 pos, bool camp)
        {
            try
            {
                Fougerite.Player player = Fougerite.Player.FindByPlayerClient(pc);
                SpawnEvent se = new SpawnEvent(pos, camp);
                if ((OnPlayerSpawning != null) && (player != null))
                {
                    OnPlayerSpawning(player, se);
                }
                return new Vector3(se.X, se.Y, se.Z);
            }
            catch { }
            return Vector3.zero;
        }

        public static void PluginInit()
        {
            if (OnPluginInit != null)
            {
                OnPluginInit();
            }
        }

        public static void ResetHooks()
        {
            OnPluginInit = delegate
            {
            };
            OnChat = delegate(Fougerite.Player param0, ref ChatString param1)
            {
            };
            OnChatRaw = delegate(ref ConsoleSystem.Arg param0)
            {
            };
            OnCommand = delegate(Fougerite.Player param0, string param1, string[] param2)
            {
            };
            OnCommandRaw = delegate(ref ConsoleSystem.Arg param0)
            {
            };
            OnPlayerConnected = delegate(Fougerite.Player param0)
            {
            };
            OnPlayerDisconnected = delegate(Fougerite.Player param0)
            {
            };
            OnNPCKilled = delegate(DeathEvent param0)
            {
            };
            OnNPCHurt = delegate(HurtEvent param0)
            {
            };
            OnPlayerKilled = delegate(DeathEvent param0)
            {
            };
            OnPlayerHurt = delegate(HurtEvent param0)
            {
            };
            OnPlayerSpawned = delegate(Fougerite.Player param0, SpawnEvent param1)
            {
            };
            OnPlayerSpawning = delegate(Fougerite.Player param0, SpawnEvent param1)
            {
            };
            OnPlayerGathering = delegate(Fougerite.Player param0, GatherEvent param1)
            {
            };
            OnEntityHurt = delegate(HurtEvent param0)
            {
            };
            OnEntityDestroyed = delegate(DestroyEvent param0)
            {
            };
            OnEntityDecay = delegate(DecayEvent param0)
            {
            };
            OnEntityDeployed = delegate(Fougerite.Player param0, Entity param1)
            {
            };
            OnConsoleReceived = delegate(ref ConsoleSystem.Arg param0, bool param1)
            {
            };
            OnBlueprintUse = delegate(Fougerite.Player param0, BPUseEvent param1)
            {
            };
            OnDoorUse = delegate(Fougerite.Player param0, DoorEvent param1)
            {
            };
            OnTablesLoaded = delegate(Dictionary<string, LootSpawnList> param0)
            {
            };
            OnItemsLoaded = delegate(ItemsBlocks param0)
            {
            };
            OnServerInit = delegate
            {
            };
            OnServerShutdown = delegate
            {
            };
            OnModulesLoaded = delegate
            {
            };

            foreach (Fougerite.Player player in Fougerite.Server.GetServer().Players)
            {
                player.FixInventoryRef();
            }
        }

        public static void ServerShutdown()
        {
            if (OnServerShutdown != null)
                OnServerShutdown();

            DataStore.GetInstance().Save();
        }

        public static void ServerStarted()
        {
            DataStore.GetInstance().Load();
            if (OnServerInit != null)
                OnServerInit();
        }

        public static void ShowTalker(uLink.NetworkPlayer player, PlayerClient p)
        {
            if (OnShowTalker != null)
                OnShowTalker(player, p);
        }

        internal static void ModulesLoaded()
        {
            if (OnModulesLoaded != null)
                OnModulesLoaded();
        }

        public static Dictionary<string, LootSpawnList> TablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            if (OnTablesLoaded != null)
                OnTablesLoaded(lists);
            return lists;
        }

        public delegate void BlueprintUseHandlerDelegate(Fougerite.Player player, BPUseEvent ae);
        public delegate void ChatHandlerDelegate(Fougerite.Player player, ref ChatString text);
        public delegate void ChatRawHandlerDelegate(ref ConsoleSystem.Arg arg);
        public delegate void CommandHandlerDelegate(Fougerite.Player player, string text, string[] args);
        public delegate void CommandRawHandlerDelegate(ref ConsoleSystem.Arg arg);
        public delegate void ConnectionHandlerDelegate(Fougerite.Player player);
        public delegate void ConsoleHandlerDelegate(ref ConsoleSystem.Arg arg, bool external);
        public delegate void DisconnectionHandlerDelegate(Fougerite.Player player);
        public delegate void DoorOpenHandlerDelegate(Fougerite.Player p, DoorEvent de);
        public delegate void EntityDecayDelegate(DecayEvent de);
        public delegate void EntityDeployedDelegate(Fougerite.Player player, Entity e);
        public delegate void EntityHurtDelegate(HurtEvent he);
        public delegate void EntityDestroyedDelegate(DestroyEvent de);
        public delegate void HurtHandlerDelegate(HurtEvent he);
        public delegate void ItemsDatablocksLoaded(ItemsBlocks items);
        public delegate void KillHandlerDelegate(DeathEvent de);
        public delegate void LootTablesLoaded(Dictionary<string, LootSpawnList> lists);
        public delegate void PlayerGatheringHandlerDelegate(Fougerite.Player player, GatherEvent ge);
        public delegate void PlayerSpawnHandlerDelegate(Fougerite.Player player, SpawnEvent se);
        public delegate void ShowTalkerDelegate(uLink.NetworkPlayer player, PlayerClient p);
        public delegate void PluginInitHandlerDelegate();
        public delegate void ServerInitDelegate();
        public delegate void ServerShutdownDelegate();
        public delegate void ModulesLoadedDelegate();
    }
}
