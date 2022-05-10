namespace RustPP.Commands
{
    using Fougerite;
    using RustPP;
    using System;

    public class SaveAllCommand : ChatCommand
    {
        public override void Execute(ref ConsoleSystem.Arg Arguments, ref string[] ChatArguments)
        {
            var pl = Server.GetServer().GetCachePlayer(Arguments.argUser.userID);
            //AvatarSaveProc.SaveAll();
            pl.MessageFrom(Core.Name, "Saved ALL Avatar files!");
            World.GetWorld().ServerSaveHandler.ManualBackGroundSave();
            pl.MessageFrom(Core.Name, "Saved server global state!");
            //Helper.CreateSaves();
            pl.MessageFrom(Core.Name, "Saved " + Core.Name + " data!");
        }
    }
}