using UnityEngine;

namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a weapon is shot.
    /// </summary>
    public class ShootEvent
    {
        private readonly BulletWeaponDataBlock _bw;
        private readonly Fougerite.Player _player;
        private readonly UnityEngine.GameObject _go;
        private readonly ItemRepresentation _ir;
        private readonly uLink.NetworkMessageInfo _unmi;
        private readonly IBulletWeaponItem _ibw;
        private readonly IDRemoteBodyPart _part;
        private readonly bool _hitnetworkobj;
        private readonly bool _hitbodypart;
        private readonly bool _isheadshot;
        private readonly BodyPart _bodypart;
        private readonly Vector3 _endpos;
        private readonly Vector3 _offset;  

        public ShootEvent(BulletWeaponDataBlock bw, UnityEngine.GameObject go, ItemRepresentation ir, 
            uLink.NetworkMessageInfo ui, IBulletWeaponItem ibw, 
            IDRemoteBodyPart part, bool flag, bool flag2, bool flag3, BodyPart part2, Vector3 vector, Vector3 vector2)
        {
            TakeDamage local = ibw.inventory.GetLocal<TakeDamage>();
            _player = Fougerite.Server.GetServer().FindPlayer(local.GetComponent<Character>().netUser.userID);
            _bw = bw;
            _go = go;
            _ir = ir;
            _ibw = ibw;
            _unmi = ui;
            _part = part;
            _hitnetworkobj = flag;
            _hitbodypart = flag2;
            _isheadshot = flag3;
            _bodypart = part2;
            _endpos = vector;
            _offset = vector2;
        }

        /// <summary>
        /// The weapon's item. (IBulletWeaponItem class)
        /// </summary>
        public IBulletWeaponItem IBulletWeaponItem
        {
            get { return this._ibw; }
        }

        /// <summary>
        /// The player who shoots the gun.
        /// </summary>
        public Fougerite.Player Player
        {
            get { return this._player; }
        }

        /// <summary>
        /// The datablock of the item.
        /// </summary>
        public BulletWeaponDataBlock BulletWeaponDataBlock
        {
            get { return this._bw; }
        }

        /// <summary>
        /// The gameobject of the item.
        /// </summary>
        public UnityEngine.GameObject GameObject
        {
            get { return this._go; }
        }

        /// <summary>
        /// Returns the ItemRepresentation class.
        /// </summary>
        public ItemRepresentation ItemRepresentation
        {
            get { return this._ir; }
        }

        /// <summary>
        /// Gets the uLink.NetworkMessageInfo data.
        /// </summary>
        public uLink.NetworkMessageInfo NetworkMessageInfo
        {
            get { return this._unmi; }
        }

        /// <summary>
        /// Returns the IDRemotePart if exists.
        /// </summary>
        public IDRemoteBodyPart Part
        {
            get { return _part; }
        }

        /// <summary>
        /// Determines if the player hit a network object.
        /// </summary>
        public bool HitNetworkObject
        {
            get { return _hitnetworkobj; }
        }

        /// <summary>
        /// Determines if the player hit bodypart.
        /// </summary>
        public bool HitBodyPart
        {
            get { return _hitbodypart; }
        }

        /// <summary>
        /// Determines if the shot was a headshot.
        /// </summary>
        public bool IsHeadShot
        {
            get { return _isheadshot; }
        }

        /// <summary>
        /// Determines if the shot was a bodyshot.
        /// </summary>
        public BodyPart Bodypart
        {
            get { return _bodypart; }
        }

        /// <summary>
        /// Gets the end position vector.
        /// </summary>
        public Vector3 EndPos
        {
            get { return _endpos; }
        }

        /// <summary>
        /// Gets the offset vector.
        /// </summary>
        public Vector3 Offset
        {
            get { return _offset; }
        }
    }
}
