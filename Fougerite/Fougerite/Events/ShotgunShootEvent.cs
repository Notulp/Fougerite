using UnityEngine;

namespace Fougerite.Events
{
    /// <summary>
    /// Determines the ShotgunEvent's type.
    /// </summary>
    public enum ShotgunEventType
    {
        BeforeShot,
        AfterShot
    }
    
    /// <summary>
    /// This class is created when a shotgun is shot.
    /// </summary>
    public class ShotgunShootEvent
    {
        private readonly ShotgunDataBlock _bw;
        private readonly Player _player;
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
        private readonly ShotgunEventType _type;
        private int _pellets = 0;

        public ShotgunShootEvent(ShotgunDataBlock bw, ItemRepresentation ir, uLink.NetworkMessageInfo ui, IBulletWeaponItem ibw, int pellets, ShotgunEventType type)
        {
            TakeDamage local = ibw.inventory.GetLocal<TakeDamage>();
            _player = Server.GetServer().FindPlayer(local.GetComponent<Character>().netUser.userID);
            _bw = bw;
            _ir = ir;
            _ibw = ibw;
            _unmi = ui;
            _type = type;
            _part = null;
            _pellets = pellets;
            _hitnetworkobj = false;
            _hitbodypart = false;
            _isheadshot = false;
            _bodypart = BodyPart.Spine0;
            _endpos = Vector3.zero;
            _offset = Vector3.zero;
        }
        
        public ShotgunShootEvent(ShotgunDataBlock bw, ItemRepresentation ir, uLink.NetworkMessageInfo ui, IBulletWeaponItem ibw, ShotgunEventType type, 
            IDRemoteBodyPart part, bool flag, bool flag2, bool flag3, BodyPart part2, Vector3 vector, Vector3 vector2)
        {
            TakeDamage local = ibw.inventory.GetLocal<TakeDamage>();
            _player = Server.GetServer().FindPlayer(local.GetComponent<Character>().netUser.userID);
            _bw = bw;
            _ir = ir;
            _ibw = ibw;
            _unmi = ui;
            _type = type;
            _part = part;
            _hitnetworkobj = flag;
            _hitbodypart = flag2;
            _isheadshot = flag3;
            _bodypart = part2;
            _endpos = vector;
            _offset = vector2;
        }

        /// <summary>
        /// Gets or Sets the amount of pellets that will be flying out of the shotgun.
        /// Only works if the Event Type is "BeforeShot".
        /// </summary>
        public int Pellets
        {
            get { return _pellets; }
            set { _pellets = value; }
        }

        /// <summary>
        /// The amount of pellets that are going to fly from the gun.
        /// </summary>
        /// <param name="pellets"></param>
        public void SetPellets(int pellets)
        {
            _pellets = pellets;
        }

        /// <summary>
        /// The IBulletWeaponItem of the gun.
        /// </summary>
        public IBulletWeaponItem IBulletWeaponItem
        {
            get { return _ibw; }
        }

        /// <summary>
        /// The player who shoots the gun.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// The datablock of the shotgun.
        /// </summary>
        public ShotgunDataBlock ShotgunDataBlock
        {
            get { return _bw; }
        }

        /// <summary>
        /// Gets the ItemRepresentation class
        /// </summary>
        public ItemRepresentation ItemRepresentation
        {
            get { return _ir; }
        }

        /// <summary>
        /// Gets the uLink.NetworkMessageInfo data.
        /// </summary>
        public uLink.NetworkMessageInfo NetworkMessageInfo
        {
            get { return _unmi; }
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
