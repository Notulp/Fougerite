using System.Linq;
using System.Security;
using Fougerite.Permissions;

namespace RustPP.Permissions
{
    using Fougerite;
    using RustPP;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    [Serializable]
    public class Administrator
    {
        private List<string> _flags;
        private string _name;
        private ulong _userid;
        [XmlIgnore]
        private static List<Administrator> admins = new List<Administrator>();
        public static string[] PermissionsFlags = new string[] { 
            "CanMute", "CanUnmute", "CanWhiteList", "CanKill", "CanKick", "CanBan", "CanUnban", "CanTeleport",
            "CanLoadout", "CanAnnounce", "CanSpawnItem", "CanGiveItem", "CanReload", "CanSaveAll", "CanAddAdmin",
            "CanDeleteAdmin", "CanGetFlags", "CanAddFlags", "CanUnflag", "CanInstaKO", "CanGodMode", "RCON",
            "Moderator", "CanShutdown", "CanInstaKOAll"
        };

        public Administrator()
        {
        }

        public Administrator(ulong userID, string name)
        {
            this._userid = userID;
            this._name = name;
            this._flags = new List<string>();
            AddFlagsToList(this._flags, Core.config.GetSetting("Settings", "default_admin_flags"));
        }

        public Administrator(ulong userID, string name, string flags)
        {
            this._userid = userID;
            this._name = SecurityElement.Escape(name);
            this._flags = new List<string>();
            AddFlagsToList(this._flags, flags);
        }

        public static void AddAdmin(Administrator admin)
        {
            admins.Add(admin);
        }

        private static void AddFlagsToList(List<string> l, string flagstr)
        {
            foreach (string flag in flagstr.Split(new char[] { '|' }))
            {
                string proper = Administrator.GetProperName(flag);
                if (l.Contains(proper) || proper.Equals(string.Empty))
                    continue;

                l.Add(proper);
            }
        }

        public static void DeleteAdmin(ulong admin)
        {
            admins.Remove(GetAdmin(admin));
        }

        public static void DeleteAdmin(string admin)
        {
            admins.Remove(GetAdmin(admin));
        }

        public static Administrator GetAdmin(ulong userID)
        {
            foreach (Administrator administrator in admins)
            {
                if (userID == administrator.UserID)
                    return administrator;
            }
            return null;
        }

        public static Administrator GetAdmin(string name)
        {
            foreach (Administrator administrator in admins)
            {
                if (name.Equals(administrator.DisplayName, StringComparison.OrdinalIgnoreCase))
                    return administrator;
            }
            return null;
        }

        public static string GetProperName(string name)
        {
            foreach (string flag in PermissionsFlags)
            {
                if (flag.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return flag;
            }
            return string.Empty;
        }

        public bool HasPermission(string perm)
        {
            return Flags.Any(x => x.Equals(perm, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsAdmin(ulong uid)
        {
            foreach (Administrator administrator in admins)
            {
                if (uid == administrator.UserID)
                    return true;
            }
            return false;
        }

        public static bool IsAdmin(string name)
        {
            foreach (Administrator administrator in admins)
            {
                if (name.Equals(administrator.DisplayName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public static bool IsValidFlag(string flag)
        {
            return GetProperName(flag) != string.Empty;
        }

        public static void NotifyAdmins(string msg)
        {
            foreach (Administrator administrator in admins)
            {
                Fougerite.Player user = Fougerite.Server.GetServer().FindPlayer(administrator.UserID.ToString());
                if (user != null)
                {
                    user.MessageFrom(Core.Name + "Admins", msg);
                }
            }
        }

        [XmlIgnore]
        public static List<Administrator> AdminList
        {
            get
            {
                return admins;
            }
            set
            {
                admins = value;
            }
        }

        public string DisplayName
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
            }
        }

        public List<string> Flags
        {
            get
            {
                return this._flags;
            }
            set
            {
                this._flags = value;
            }
        }

        public ulong UserID
        {
            get
            {
                return this._userid;
            }
            set
            {
                this._userid = value;
            }
        }
    }
}