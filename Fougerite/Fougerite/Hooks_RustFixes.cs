using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Fougerite.Events;
using uLink;
using Debug = UnityEngine.Debug;
using Facepunch.MeshBatch;
using UnityEngine;

namespace Fougerite
{
    public partial class Hooks
    {
        private static DateTime _lasTime = DateTime.Now;
        private static DateTime _lasTime2 = DateTime.Now;
        private static DateTime _lasTime3 = DateTime.Now;
        private static DateTime _lasTime4 = DateTime.Now;
        private static DateTime _lasTime5 = DateTime.Now;
        private static DateTime _lasTime6 = DateTime.Now;
        private static DateTime _lasTime7 = DateTime.Now;
        private static DateTime _lasTime8 = DateTime.Now;
        private static DateTime _lasTime9 = DateTime.Now;
        private static DateTime _lasTime10 = DateTime.Now;
        private static DateTime _lasTime11 = DateTime.Now;
        internal static readonly Dictionary<ulong, int> ActionCooldown = new Dictionary<ulong, int>();

        private static double CalculateDiff(ref DateTime then)
        {
            DateTime now = DateTime.Now;
            double diff = (now - then).TotalSeconds;
            return diff;
        }

        public static void RecieveNetwork(Metabolism m, float cal, float water, float rad, float anti, float temp,
            float poison)
        {
            double diff = CalculateDiff(ref _lasTime);
            if (diff > 300)
            {
                _lasTime = DateTime.Now;
                Logger.LogWarning("[RecieveNetwork] A metabolism hack was prevented.");
            }
        }

        public static bool ConfirmVoice(VoiceCom com, byte[] data)
        {
            uLink.NetworkPlayer nplayer = com.networkViewOwner;

            double diff = CalculateDiff(ref _lasTime2);
            if (data == null)
            {
                if (diff > 10)
                {
                    _lasTime2 = DateTime.Now;
                    Player player = Server.GetServer().FindByNetworkPlayer(nplayer);
                    if (player != null)
                    {
                        Logger.LogWarning($"[VoiceByteOverflown] Received null value. Possible Sender: {player.Name} - {player.SteamID} - {player.IP}");
                    }
                    else
                    {
                        Logger.LogWarning("[VoiceByteOverflown] Received null value.");
                    }
                }

                return false;
            }

            if (data.Length > 15000)
            {
                if (diff > 10)
                {
                    _lasTime2 = DateTime.Now;
                    Player player = Server.GetServer().FindByNetworkPlayer(nplayer);
                    if (player != null)
                    {
                        Logger.LogWarning($"[VoiceByteOverflown] Received a huge amount of byte, clearing. {data.Length} {player.Name} - {player.SteamID} - {player.IP}");
                    }
                    else
                    {
                        Logger.LogWarning($"[VoiceByteOverflown] Received a huge amount of byte, clearing. {data.Length}");
                    }
                }

                Array.Clear(data, 0, data.Length);
                return false;
            }

            for (uint i = 0; i < data.Length;)
            {
                try
                {
                    uint conversion = (uint)BitConverter.ToInt32(data, (int)i);
                    if (conversion > 10000)
                    {
                        if (diff > 10)
                        {
                            _lasTime2 = DateTime.Now;
                            Player player = Server.GetServer().FindByNetworkPlayer(nplayer);
                            if (player != null)
                            {
                                Logger.LogWarning(
                                    $"[VoiceByteOverflown] Received a huge amount of byte, clearing. {conversion} {player.Name} - {player.SteamID} - {player.IP}");
                            }
                            else
                            {
                                Logger.LogWarning(
                                    $"[VoiceByteOverflown] Received a huge amount of byte, clearing. {conversion}");
                            }
                        }

                        Array.Clear(data, 0, data.Length);
                        return false;
                    }

                    i += conversion + 6;
                }
                catch
                {
                    if (diff > 10)
                    {
                        _lasTime2 = DateTime.Now;
                        Player player = Server.GetServer().FindByNetworkPlayer(nplayer);
                        if (player != null)
                        {
                            Logger.LogWarning(
                                $"[VoiceByteOverflown] Seems like an error occured while reading the voice bytes. Someone is trying to send false packets? {player.Name} - {player.SteamID} - {player.IP}");
                        }
                        else
                        {
                            Logger.LogWarning(
                                "[VoiceByteOverflown] Seems like an error occured while reading the voice bytes. Someone is trying to send false packets?");
                        }
                    }

                    Array.Clear(data, 0, data.Length);
                    return false;
                }
            }

            return true;
        }

        public static void FallDamageCheck(FallDamage fd, float v)
        {
            double diff = CalculateDiff(ref _lasTime11);
            if (diff > 10)
            {
                Logger.LogWarning($"[Legbreak RPC] Bypassed a legbreak RPC possibly sent by a hacker. Value: {v}");
                _lasTime11 = DateTime.Now;
            }
            //fd.SetLegInjury(v);
        }
        
        public static bool RegisterHook(ServerSave save)
        {
            if (ServerSaveHandler.ServerIsSaving)
            {
                // Return false if the registers already contain our save class. (Tell that we have already added it)
                if (ServerSaveManager.Instances.registers.Contains(save))
                {
                    return false;
                }

                // If we already added It to our temp dictionary, then return false. (Tell that we have already added it)
                if (ServerSaveHandler.UnProcessedSaves.ContainsKey(save))
                {
                    return false;
                }

                ServerSaveHandler.UnProcessedSaves.Add(save, 1);
            }
            else
            {
                if (!ServerSaveManager.Instances.registers.Add(save))
                {
                    return false;
                }

                ServerSaveManager.Instances.ordered.Add(save);
            }

            return true;
        }

        public static bool UnRegisterHook(ServerSave save)
        {
            if (ServerSaveHandler.ServerIsSaving)
            {
                // Return false if the registers doesn't contain our save class. (Tell that It doesn't exist)
                if (!ServerSaveManager.Instances.registers.Contains(save))
                {
                    return false;
                }

                // If we have already added the value for later processing return false (Tell that It doesn't exist)
                if (ServerSaveHandler.UnProcessedSaves.ContainsKey(save))
                {
                    return false;
                }

                ServerSaveHandler.UnProcessedSaves.Add(save, 2);
            }
            else
            {
                if (!ServerSaveManager.Instances.registers.Remove(save))
                {
                    return false;
                }

                ServerSaveManager.Instances.ordered.Remove(save);
            }

            return true;
        }

        public static void TossBypass(InventoryHolder holder, uLink.BitStream stream, uLink.NetworkMessageInfo info)
        {
            if (info == null || info.sender == null)
            {
                return;
            }

            Inventory inventory = holder.inventory;
            Facepunch.NetworkView networkView = holder.networkView;

            if (networkView.owner != info.networkView.owner)
            {
                return;
            }

            if (networkView.owner != info.sender)
            {
                return;
            }

            int data;
            try
            {
                data = (int)Inventory.RPCInteger(stream);
            }
            catch
            {
                return;
            }

            if (float.IsNaN(data) || float.IsInfinity(data) || data > 39)
            {
                return;
            }

            InventoryItem item;
            if (inventory.collection.Get(data, out item))
            {
                DropHelper.DropItem(inventory, data);
            }
        }

        public static void LoggerEvent(LoggerEventType type, string message)
        {
            try
            {
                if (OnLogger != null)
                {
                    LoggerEvent evt = new LoggerEvent(type, message);
                    OnLogger(evt);
                }
            }
            catch (Exception ex)
            {
                Logger.LogErrorIgnore($"LoggerEvent Error: {ex}", null, true);
            }
        }

        public static Dictionary<string, LootSpawnList> TablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            try
            {
                if (OnTablesLoaded != null)
                    OnTablesLoaded(lists);
            }
            catch (Exception ex)
            {
                Logger.LogError($"TablesLoadedEvent Error: {ex}");
            }

            return lists;
        }

        public static void ITSPHook(Inventory instance, byte slotNumber, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime3);
            if (float.IsNaN(slotNumber) || float.IsInfinity(slotNumber) || slotNumber > 39)
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning($"[ITSP InvalidPacket] {slotNumber} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid ITSP Packet.");
                    }

                    _lasTime3 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.IsAnAuthorizedLooter(info.sender) && instance.collection.Get(slotNumber, out item))
            {
                instance.SplitStack(slotNumber);
            }
        }

        public static void IACTHook(Inventory instance, byte itemIndex, byte action, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime4);
            if (float.IsNaN(itemIndex) || float.IsInfinity(itemIndex) || itemIndex > 39
                || float.IsNaN(action) || float.IsInfinity(action) ||
                !Enum.IsDefined(typeof(InventoryItem.MenuItem), action))
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning($"[IACT InvalidPacket] {itemIndex} - {action} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid IACT Packet.");
                    }

                    _lasTime4 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if ((info.sender == instance.networkView.owner) && instance.collection.Get(itemIndex, out item))
            {
                item.OnMenuOption((InventoryItem.MenuItem)action);
            }
        }

        private static bool CheckSenderIsNonOwningClient(Inventory instance, uLink.NetworkPlayer sender)
        {
            if (sender.isClient && (sender != instance.networkView.owner))
            {
                return true;
            }

            return false;
        }

        public static void IASTHook(Inventory instance, byte itemIndex, uLink.NetworkViewID itemRepID,
            uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime5);
            // Just to make sure.
            if (float.IsNaN(itemIndex) || float.IsInfinity(itemIndex) || itemIndex > 39)
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning($"[IAST InvalidPacket] {itemIndex} - {itemRepID} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid IAST Packet.");
                    }

                    _lasTime5 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (!CheckSenderIsNonOwningClient(instance, info.sender) && instance.collection.Get(itemIndex, out item))
            {
                instance.SetActiveItemManually(itemIndex,
                    !(itemRepID != uLink.NetworkViewID.unassigned)
                        ? null
                        : uLink.NetworkView.Find(itemRepID).GetComponent<ItemRepresentation>(),
                    new uLink.NetworkViewID?(itemRepID));
            }
        }

        public static void OC1Hook(Controllable instance, uLink.NetworkViewID rootViewID, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (uLink.NetworkView.Find(rootViewID) != null)
            {
                instance.OverrideControlOfHandleRPC(rootViewID, rootViewID, ref info);
            }
        }

        public static void OC2Hook(Controllable instance, uLink.NetworkViewID rootViewID,
            uLink.NetworkViewID parentViewID, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (uLink.NetworkView.Find(rootViewID) != null)
            {
                instance.OverrideControlOfHandleRPC(rootViewID, parentViewID, ref info);
            }
        }

        public static void CLRHook(Controllable instance, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            Controllable bt = instance.ch.bt;
            Facepunch.NetworkView networkView = bt.networkView;

            if ((networkView != null) && (networkView.viewID != uLink.NetworkViewID.unassigned))
            {
                NetCull.RemoveRPCsByName(networkView, "Controllable:RFH");
                while (bt._sentRootControlCount > instance.ch.id)
                {
                    string view = Controllable.kClientSideRootNumberRPCName[bt._sentRootControlCount--];
                    NetCull.RemoveRPCsByName(networkView, view);
                }
            }

            instance.ch.Delete();
            if (((bt != null) && ((bt.RT & 0xc00) == 0)) &&
                ((networkView != null) && (networkView.viewID != uLink.NetworkViewID.unassigned)))
            {
                networkView.RPC<byte>("Controllable:RFH", uLink.RPCMode.OthersBuffered, (byte)bt._sentRootControlCount);
            }

            instance.SharedPostCLR();
        }

        public static void CLDHook(Controllable instance, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (info.sender != instance.networkViewOwner)
            {
                return;
            }

            Controllable.Disconnect(instance);
        }

        public static void ISMVHook(Inventory instance, byte fromSlot, byte toSlot, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime6);
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning($"[ISMV InvalidPacket] {fromSlot} - {toSlot} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid ISMV Packet.");
                    }

                    _lasTime6 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory.SlotOperationResult message =
                    instance.SlotOperation(fromSlot, toSlot, Inventory.SlotOperationsMove(info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        public static void ITMGHook(Inventory instance, NetEntityID toInvID, byte fromSlot, byte toSlot,
            bool tryCombine, uLink.NetworkMessageInfo info)
        {
            if (info == null || toInvID == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime7);
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning($"[ITMG InvalidPacket] {fromSlot} - {toSlot} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid ITMG Packet.");
                    }

                    _lasTime7 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory component = toInvID.GetComponent<Inventory>();
                Inventory.SlotOperationResult message = instance.SlotOperation(fromSlot, component, toSlot,
                    Inventory.SlotOperationsMerge(tryCombine, info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        public static void ITMVHook(Inventory instance, NetEntityID toInvID, byte fromSlot, byte toSlot,
            uLink.NetworkMessageInfo info)
        {
            if (info == null || toInvID == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime8);
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning($"[ITMV InvalidPacket] {fromSlot} - {toSlot} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid ITMV Packet.");
                    }

                    _lasTime8 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory component = toInvID.GetComponent<Inventory>();
                Inventory.SlotOperationResult message = instance.SlotOperation(fromSlot, component, toSlot,
                    Inventory.SlotOperationsMove(info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        public static void ITSMHook(Inventory instance, byte fromSlot, byte toSlot, bool tryCombine,
            uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime9);
            if (float.IsNaN(fromSlot) || float.IsInfinity(fromSlot) || fromSlot > 39
                || float.IsNaN(toSlot) || float.IsInfinity(toSlot) || toSlot > 39)
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning(
                            $"[ITSM InvalidPacket] {fromSlot} - {toSlot} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid ITSM Packet.");
                    }

                    _lasTime9 = DateTime.Now;
                }

                return;
            }

            InventoryItem item;
            if (instance.collection.Get(fromSlot, out item))
            {
                Inventory.SlotOperationResult message = instance.SlotOperation(fromSlot, toSlot,
                    Inventory.SlotOperationsMerge(tryCombine, info.sender));
                if (((int)message) <= 0)
                {
                    Debug.LogWarning(message);
                }
            }
        }

        public static void SVUCHook(Inventory instance, byte cell, uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            double diff = CalculateDiff(ref _lasTime10);
            if (float.IsNaN(cell) || float.IsInfinity(cell) || cell > 39)
            {
                if (diff > 10)
                {
                    Player player = Server.GetServer().FindByNetworkPlayer(info.sender);
                    if (player != null)
                    {
                        Logger.LogWarning(
                            $"[SVUC InvalidPacket] {cell} - {player.Name} - {player.SteamID} - {player.IP}");
                        Server.GetServer().BanPlayer(player, "Console", "Invalid SVUC Packet.");
                    }

                    _lasTime10 = DateTime.Now;
                }

                return;
            }

            if (instance.IsAnAuthorizedLooter(info.sender, true, "reqinvcellupdate"))
            {
                instance.MarkSlotDirty(cell);
            }
        }


        public static void CRFSHook(CraftingInventory instance, int amount, int blueprintUID,
            uLink.NetworkMessageInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (info.sender != instance.networkViewOwner)
            {
                return;
            }

            if (float.IsNaN(amount) || float.IsNaN(blueprintUID) || float.IsInfinity(amount) ||
                float.IsInfinity(blueprintUID))
            {
                return;
            }

            if (float.IsNaN(info.timestampInMillis) || float.IsInfinity(info.timestampInMillis))
            {
                return;
            }

            BlueprintDataBlock bd = CraftingInventory.FindBlueprint(blueprintUID);
            if (bd != null)
            {
                instance.StartCrafting(bd, amount, info.timestampInMillis);
            }
        }

        private static NGC.Procedure Message(NGC instance, byte[] data, int offset, int length,
            uLink.NetworkMessageInfo info)
        {
            try
            {
                int num4;
                byte[] buffer;
                int startIndex = offset;
                int num2 = BitConverter.ToInt32(data, startIndex);
                startIndex += 4;
                int num3 = offset + length;
                if (startIndex == num3)
                {
                    buffer = null;
                    num4 = 0;
                }
                else
                {
                    num4 = num3 - startIndex;
                    buffer = new byte[num4];
                    int num5 = 0;
                    do
                    {
                        byte val = data[startIndex++];
                        buffer[num5++] = val;
                    } while (startIndex < num3);
                }

                return instance.Message(num2, buffer, num4, info);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Caught an NGC Error: {ex}");
                // ignore
            }

            return null;
        }

        public static void CHook(NGC instance, byte[] data, uLink.NetworkMessageInfo info)
        {
            if (data.Length > 30000)
            {
                Array.Clear(data, 0, data.Length);
                Logger.LogError($"CHook Received: {data.Length}");
                return;
            }

            NGC.Procedure procedure = Message(instance, data, 0, data.Length, info);
            if (procedure != null && !procedure.Call())
            {
                if (procedure.view != null)
                {
                    Logger.LogWarning(
                        $"Did not call rpc {procedure.view.prefab.installation.methods[procedure.message].method.Name} for view {procedure.view.name} (entid:{{procedure.view.id}},msg:{{procedure.message}})", instance);
                }
                else if (NGC.log_nonexistant_ngc_errors)
                {
                    Logger.LogWarning($"Did not call rpc to non existant view# {procedure.target}. ( message id was {procedure.message} )", instance);
                }
            }
        }

        public static bool AHook(NGC instance, byte[] data, uLink.NetworkMessageInfo info)
        {
            if (data.Length > 30000)
            {
                Array.Clear(data, 0, data.Length);
                Logger.LogError($"AHook Received: {data.Length}");
                return false;
            }

            if (info.sender != uLink.NetworkPlayer.server)
            {
                Array.Clear(data, 0, data.Length);
                Logger.LogError($"AHook Received: {data.Length} Not server.");
                return false;
            }

            return true;
        }

        public static bool InternalRPCCheck(Class5 class5)
        {
            string str;
            try
            {
                str = class5.class56_0.ipendPoint_0.Address.ToString();
            }
            catch
            {
                return false;
            }

            if (!Server.GetServer().IsBannedIP(str))
            {
                try
                {
                    Class5.Enum0 num = (Class5.Enum0)class5.enum0_0;
                    Enum8 num2 = class5.enum8_0;
                    if (Enum.IsDefined(typeof(Class5.Enum0), num) && Enum.IsDefined(typeof(Enum8), num2))
                    {
                        return true;
                    }
                }
                catch
                {
                    //ignore
                }

                Server.GetServer().BanPlayerIP(str, "1", "uLink AuthorizationCheck", "Fougerite");
                Logger.LogWarning($"[Fougerite uLinkInternalCheck] Hoax IP automatically banned, and rejected: {str}",
                    null);
            }

            return false;
        }

        public static void uLinkAuthorizationCheck(Class48 class48, Class5 class5_0)
        {
            string str;
            try
            {
                str = class5_0.class56_0.ipendPoint_0.Address.ToString();
            }
            catch
            {
                return;
            }

            if (!Server.GetServer().IsBannedIP(str))
            {
                NetworkLog.Debug<string, Class5>(NetworkLogFlags.RPC, "Server handling ", class5_0);
                NetworkLog.Debug<string, string, string, Struct15>(NetworkLogFlags.Timestamp, "Server got message ",
                    class5_0.string_0, " with timestamp ", class5_0.struct15_0);
                if (class5_0.method_16())
                {
                    NetworkLog.Error<Class5, string>(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, class5_0,
                        " is from another server and will be dropped!");
                }
                else
                {
                    if (!class5_0.method_15())
                    {
                        if (class48.bool_1)
                        {
                            Logger.LogWarning(
                                $"[Fougerite uLinkAuthorizationCheck] Hoax IP automatically banned, and rejected: {str}", null);
                            Server.GetServer().BanPlayerIP(str, "1", "uLink AuthorizationCheck", "Fougerite");
                            return;
                        }

                        class48.vmethod_35(new Class5(class48, class5_0));
                    }
                    else if (class5_0.method_1())
                    {
                        class48.method_281(class5_0);
                        return;
                    }

                    if (class5_0.method_14())
                    {
                        class48.method_73(class5_0);
                    }
                }
            }
        }

        public static void ActivateImmediatelyUncheckedHook(MeshBatchPhysicalOutput meshBatchPhysicalOutput)
        {
            if (meshBatchPhysicalOutput.FlaggedForActivation)
            {
                Facepunch.MeshBatch.Runtime.Sealed.MeshBatchPhysicalIntegration.Cancel(meshBatchPhysicalOutput);
                meshBatchPhysicalOutput.FlaggedForActivation = false;
            }
            if (!meshBatchPhysicalOutput.Activated)
            {
                // This becomes null when the entity is destroyed on deployment before its activated
                // thus causing null reference exception
                if (meshBatchPhysicalOutput.GameObject == null)
                {
                    return;
                }
                meshBatchPhysicalOutput.Activated = true;
                meshBatchPhysicalOutput.GameObject.SetActive(true);
            }
        }
        
        public static void RPCFix(Class48 c48, Class5 class5_0, uLink.NetworkPlayer networkPlayer_1)
        {
            Class56 class2 = c48.method_270(networkPlayer_1);
            if (class2 != null)
            {
                c48.method_277(class5_0, class2);
            }
            else
            {
                if (IsShuttingDown)
                {
                    return;
                }

                object data = networkPlayer_1.GetLocalData();
                if (data is NetUser user)
                {
                    ulong id = user.userID;
                    if (uLinkDCCache.Contains(id))
                    {
                        return;
                    }

                    Logger.LogDebug("===Fougerite uLink===");
                    Player player = Server.GetServer().GetCachePlayer(id);
                    if (player != null)
                    {
                        Logger.LogDebug($"[Fougerite uLink] Detected RPC Failing Player: {player.Name}-{player.SteamID} Trying to kick...");
                        if (player.IsOnline)
                        {
                            player.Disconnect(false);
                            Logger.LogDebug("[Fougerite uLink] Should be kicked!");
                            return; // Return to avoid the RPC Logging
                        }

                        Logger.LogDebug("[Fougerite uLink] Server says It's offline. Not touching.");
                        uLinkDCCache.Add(player.UID);
                    }
                    else
                    {
                        Logger.LogDebug("[Fougerite uLink] Not existing in cache...");
                        uLinkDCCache.Add(id);
                    }
                }
                else
                {
                    Logger.LogDebug("===Fougerite uLink===");
                    Logger.LogDebug("[Fougerite uLink] Not existing in cache... (2x0)");
                }

                Logger.LogDebug(
                    $"[Fougerite uLink] Private RPC (internal RPC {class5_0.enum0_0}) was not sent because a connection to {class5_0.networkPlayer_1} was not found!");
                //NetworkLog.Error<string, string, uLink.NetworkPlayer, string>(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, "Private RPC ", (class5_0.method_11() ? class5_0.string_0 : ("(internal RPC " + class5_0.enum0_0 + ")")) + " was not sent because a connection to ", class5_0.networkPlayer_1, " was not found!");
            }
        }

        public static void RPCCatch(object obj)
        {
            var info = obj as uLink.NetworkMessageInfo;
            if (info == null)
            {
                return;
            }

            if (info.sender == uLink.NetworkPlayer.server)
            {
                return;
            }

            var netuser = info.sender.localData as NetUser;
            if (netuser == null)
            {
                return;
            }

            Logger.LogWarning(
                $"[Fougerite uLink] RPC Message from {netuser.displayName}-{netuser.userID} triggered an exception. Kicking...");
            if (netuser.connected)
            {
                netuser.Kick(NetError.Facepunch_Kick_Violation, true);
            }
        }

        public static void uLinkCatch(Class0 instance)
        {
            string ip = ((IPEndPoint)(instance.endPoint_0)).Address.ToString();
            Logger.Log($"[uLink Ignore] Ignored Socket from: {ip}");
        }
        
        public static void Action1BHook(ItemRepresentation itr, byte[] data, uLink.NetworkMessageInfo info)
        {
            if (data == null)
            {
                return;
            }

            if (itr == null)
            {
                return;
            }

            if (data.Length > 500)
            {
                return;
            }

            try
            {
                uLink.BitStream
                    stream = new uLink.BitStream(data, false); // Can only read once from the BitStream, so we copy It.

                Vector3 v = stream.ReadVector3();
                Vector3 v2 = stream.ReadVector3();
                if (v == null || v2 == null)
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }

                if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y)
                    || float.IsNaN(v.z) || float.IsInfinity(v.z))
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }

                if (float.IsNaN(v2.x) || float.IsInfinity(v2.x) || float.IsNaN(v2.y) || float.IsInfinity(v2.y)
                    || float.IsNaN(v2.z) || float.IsInfinity(v2.z))
                {
                    Array.Clear(data, 0, data.Length);
                    return;
                }
            }
            catch
            {
                // ignore
            }

            try
            {
                uLink.BitStream copy = new uLink.BitStream(data, false);
                itr.RunServerAction(1, copy, ref info);
            }
            catch (Exception ex)
            {
                Logger.LogError("[Action1Error] Failed to call RunServerAction, Check logs.");
                Logger.LogDebug($"Error: {ex}");
            }
        }

        public static void Action1Hook(ItemRepresentation itr, uLink.BitStream stream, uLink.NetworkMessageInfo info)
        {
            if (stream._data == null)
            {
                return;
            }

            if (itr == null)
            {
                return;
            }

            if (ServerSaveHandler.ServerIsSaving)
            {
                if (itr.networkViewOwner != null)
                {
                    if (itr.networkViewOwner.GetLocalData() is NetUser user)
                    {
                        Player player = Server.GetServer().FindPlayer(user.userID.ToString());
                        if (player != null)
                        {
                            if (!ActionCooldown.ContainsKey(player.UID))
                            {
                                ActionCooldown[player.UID] = 1;
                            }
                            else
                            {
                                ActionCooldown[player.UID] += 1;
                            }

                            if (ActionCooldown[player.UID] < 3)
                            {
                                player.Message(Bootstrap.SaveNotification);
                            }
                        }
                    }
                }

                return;
            }

            ActionCooldown.Clear();

            uLink.BitStream
                copy = new uLink.BitStream(stream._data,
                    false); // Can only read once from the BitStream, so we copy It.
            try
            {
                Vector3 v = copy.ReadVector3();
                Vector3 v2 = copy.ReadVector3();
                if (v == null || v2 == null)
                {
                    return;
                }

                if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y)
                    || float.IsNaN(v.z) || float.IsInfinity(v.z))
                {
                    return;
                }

                if (float.IsNaN(v2.x) || float.IsInfinity(v2.x) || float.IsNaN(v2.y) || float.IsInfinity(v2.y)
                    || float.IsNaN(v2.z) || float.IsInfinity(v2.z))
                {
                    return;
                }
            }
            catch
            {
                // Ignore
            }

            try
            {
                itr.RunServerAction(1, stream, ref info);
            }
            catch (Exception ex)
            {
                Logger.LogError("[Action2Error] Failed to call RunServerAction, Check logs.");
                Logger.LogDebug($"Error: {ex}");
            }
        }
        
        public static bool DeployableCheckHook(DeployableItemDataBlock instance, Ray ray, out Vector3 pos,
            out Quaternion rot, out TransCarrier carrier)
        {
            DeployableItemDataBlock.DeployPlaceResults results;
            Vector3 origin = ray.origin;
            Vector3 direction = ray.direction;
            if (float.IsNaN(origin.x) || float.IsInfinity(origin.x)
                                      || float.IsNaN(origin.y) || float.IsInfinity(origin.y)
                                      || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                carrier = null;
                return false;
            }

            if (float.IsNaN(direction.x) || float.IsInfinity(direction.x)
                                         || float.IsNaN(direction.y) || float.IsInfinity(direction.y)
                                         || float.IsNaN(direction.z) || float.IsInfinity(direction.z))
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                carrier = null;
                return false;
            }

            instance.CheckPlacementResults(ray, out pos, out rot, out carrier, out results);
            return results.Valid();
        }
        
        public static void StructureComponentDoAction1(StructureComponentDataBlock instance, uLink.BitStream stream,
            ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            IStructureComponentItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<IStructureComponentItem>(out item) && (item.uses > 0))
            {
                StructureComponent structureToPlacePrefab = instance.structureToPlacePrefab;
                Vector3 origin = stream.ReadVector3();
                Vector3 direction = stream.ReadVector3();
                Vector3 position = stream.ReadVector3();
                Quaternion rotation = stream.ReadQuaternion();
                uLink.NetworkViewID viewID = stream.ReadNetworkViewID();
                if (viewID == null || float.IsNaN(viewID.id) || float.IsInfinity(viewID.id))
                {
                    return;
                }

                if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) || float.IsNaN(origin.y) ||
                    float.IsInfinity(origin.y)
                    || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
                {
                    return;
                }

                if (float.IsNaN(direction.x) || float.IsInfinity(direction.x) || float.IsNaN(direction.y) ||
                    float.IsInfinity(direction.y)
                    || float.IsNaN(direction.z) || float.IsInfinity(direction.z))
                {
                    return;
                }

                if (float.IsNaN(position.x) || float.IsInfinity(position.x) || float.IsNaN(position.y) ||
                    float.IsInfinity(position.y)
                    || float.IsNaN(position.z) || float.IsInfinity(position.z))
                {
                    return;
                }

                if (float.IsNaN(rotation.x) || float.IsInfinity(rotation.x) || float.IsNaN(rotation.y) ||
                    float.IsInfinity(rotation.y)
                    || float.IsNaN(rotation.z) || float.IsInfinity(rotation.z) || float.IsNaN(rotation.w) ||
                    float.IsInfinity(rotation.w))
                {
                    return;
                }

                StructureMaster component = null;
                if (viewID == uLink.NetworkViewID.unassigned)
                {
                    if (instance.MasterFromRay(new Ray(origin, direction)))
                    {
                        return;
                    }

                    /*if (structureToPlacePrefab.type != StructureComponent.StructureComponentType.Foundation)
                    {
                        Debug.Log("ERROR, tried to place non foundation structure on terrain!");
                    }
                    else*/
                    if (structureToPlacePrefab.type == StructureComponent.StructureComponentType.Foundation)
                    {
                        component = NetCull.InstantiateClassic<StructureMaster>(
                            Facepunch.Bundling.Load<StructureMaster>("content/structures/StructureMasterPrefab"), position,
                            rotation, 0);
                        component.SetupCreator(item.controllable);
                    }
                }
                else
                {
                    component = uLink.NetworkView.Find(viewID).gameObject.GetComponent<StructureMaster>();
                }

                if (component == null)
                {
                    return;
                    //Debug.Log("NO master, something seriously wrong");
                }

                if (instance._structureToPlace.CheckLocation(component, position, rotation) &&
                    instance.CheckBlockers(position))
                {
                    StructureComponent component2 = NetCull
                        .InstantiateStatic(instance.structureToPlaceName, position, rotation)
                        .GetComponent<StructureComponent>();
                    if (component2 != null)
                    {
#pragma warning disable 618
                        component.AddStructureComponent(component2);
#pragma warning restore 618
                        int count = 1;
                        EntityDeployed(component2, ref info);
                        if (item.Consume(ref count))
                        {
                            item.inventory.RemoveItem(item.slot);
                        }
                    }
                }
            }
        }

        public static void TorchDoAction1(TorchItemDataBlock instance, uLink.BitStream stream, ItemRepresentation rep,
            ref uLink.NetworkMessageInfo info)
        {
            ITorchItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<ITorchItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
            {
                if (item.isLit)
                {
                    item.Extinguish();
                }

                rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                Vector3 origin = stream.ReadVector3();
                Vector3 forward = stream.ReadVector3();
                if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) || float.IsNaN(origin.y) ||
                    float.IsInfinity(origin.y)
                    || float.IsNaN(origin.z) || float.IsInfinity(origin.z))
                {
                    return;
                }

                if (float.IsNaN(forward.x) || float.IsInfinity(forward.x) || float.IsNaN(forward.y) ||
                    float.IsInfinity(forward.y)
                    || float.IsNaN(forward.z) || float.IsInfinity(forward.z))
                {
                    return;
                }


                instance.ThrowFlare(rep, origin, forward);
                int count = 1;
                if (item.Consume(ref count))
                {
                    item.inventory.RemoveItem(item.slot);
                }
            }
        }
    }
}