using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Fougerite.Permissions
{
    /// <summary>
    /// Represents a group of permissions used in the permission management system.
    /// </summary>
    /// <remarks>
    /// A permission group is identified by a unique ID and includes a name, a nickname,
    /// and a collection of specific permissions. Permission groups help in organizing
    /// and assigning permissions to users or roles within the system.
    /// The group name is automatically transformed into a unique identifier when set.
    /// </remarks>
    public class PermissionGroup
    {
        /// <summary>
        /// Stores the internal name of the permission group used as the backing field
        /// for the public <c>GroupName</c> property. This field is modified and accessed
        /// indirectly through the property to enforce specific validation and transformation logic.
        /// </summary>
        private string _groupname;
        /// <summary>
        /// Represents the unique identifier assigned to a permission group.
        /// This property is automatically derived from the group name and ensures
        /// that each permission group can be uniquely identified within the system.
        /// </summary>
        [JsonProperty]
        public uint UniqueID
        {
            get;
            set;
        }

        /// <summary>
        /// Defines the name of the permission group. The <c>GroupName</c> property
        /// allows getting or setting the group's name, ensuring proper formatting by trimming
        /// whitespace and converting the name to a unique identifier internally.
        /// Modifying this property automatically updates the <c>UniqueID</c> associated with the group.
        /// </summary>
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

        /// <summary>
        /// Represents the nickname associated with a permission group.
        /// This property allows for a user-friendly or alternative name
        /// to be set for the group, which can be accessed and modified as needed.
        /// </summary>
        [JsonProperty]
        public string NickName
        {
            get;
            set;
        }

        /// <summary>
        /// Represents the list of permissions assigned to a specific group.
        /// This property holds a collection of permission strings that define
        /// the access rights or capabilities of the associated group within the system.
        /// </summary>
        [JsonProperty]
        public List<string> GroupPermissions
        {
            get;
            set;
        } = new List<string>();
        
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