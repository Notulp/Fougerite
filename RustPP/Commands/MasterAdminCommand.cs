namespace RustPP.Commands
{
    using Fougerite;
    using RustPP.Permissions;
    using System;

    public class MasterAdminCommand : ChatCommand
    {
        private static string MasterAdminPreset = "CanKick|CanBan|CanUnban|CanTeleport|CanLoadout|CanAnnounce|CanSpawnItem|CanGiveItem|CanReload|CanSaveAll|CanAddAdmin|CanDeleteAdmin|CanGetFlags|CanInstaKO|CanAddFlags|CanUnflag|CanWhiteList|CanKill|CanMute|CanUnmute|CanGodMode|RCON";

        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            if (!Administrator.IsAdmin(pl.UID))
            {
                Administrator.AddAdmin(new Administrator(pl.UID, pl.Name, MasterAdminPreset));
                pl.MessageFrom(Core.Name, "You are now a Master Administrator!");
            }
            else
            {
                pl.MessageFrom(Core.Name, "You are already an administrator!");
            }
        }
    }
}