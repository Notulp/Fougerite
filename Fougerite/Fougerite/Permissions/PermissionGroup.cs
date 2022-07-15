using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    public class PermissionGroup
    {
        private string _groupname;
        
        [JsonProperty]
        public uint UniqueID
        {
            get;
            set;
        }
        
        [JsonProperty]
        public string GroupName
        {
            get
            {
                return _groupname;
            }
            set
            {
                _groupname = value.Trim();
                UniqueID = GetUniqueID(_groupname.ToLower());
            }
        }

        [JsonProperty]
        public string NickName
        {
            get;
            set;
        }

        [JsonProperty]
        public List<string> GroupPermissions
        {
            get;
            set;
        }
        
        /// <summary>
        /// Gets the unique identifier of a string.
        /// This is used for group names.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private uint GetUniqueID(string value)
        {
            return SuperFastHashUInt16Hack.Hash(Encoding.UTF8.GetBytes(value));
        }
    }
}