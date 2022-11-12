namespace Fougerite.Events
{
    /// <summary>
    /// This class is created when a player throws a grenade.
    /// </summary>
    public class GrenadeThrowEvent
    {
        private readonly HandGrenadeDataBlock _bw;
        private readonly Player _player;
        private readonly UnityEngine.GameObject _go;
        private readonly ItemRepresentation _ir;
        private readonly uLink.NetworkMessageInfo _unmi;
        private readonly IHandGrenadeItem _ibw;

        public GrenadeThrowEvent(HandGrenadeDataBlock bw, UnityEngine.GameObject go, ItemRepresentation ir, uLink.NetworkMessageInfo ui, IHandGrenadeItem ibw)
        {
            TakeDamage local = ibw.inventory.GetLocal<TakeDamage>();
            _player = Server.GetServer().FindPlayer(local.GetComponent<Character>().netUser.userID);
            _bw = bw;
            _go = go;
            _ir = ir;
            _ibw = ibw;
            _unmi = ui;
        }

        /// <summary>
        /// Returns the original IHandGrenadeItem class
        /// </summary>
        public IHandGrenadeItem IHandGrenadeItem
        {
            get { return _ibw; }
        }

        /// <summary>
        /// Gets the player that is throwing the grenade.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// Gets the original HandGrenadeDataBlock class
        /// </summary>
        public HandGrenadeDataBlock HandGrenadeDataBlock
        {
            get { return _bw; }
        }

        /// <summary>
        /// Gets the gameobject of the grenade.
        /// </summary>
        public UnityEngine.GameObject GameObject
        {
            get { return _go; }
        }

        /// <summary>
        /// Gets the item representation.
        /// </summary>
        public ItemRepresentation ItemRepresentation
        {
            get { return _ir; }
        }

        /// <summary>
        /// Gets the uLink.NetowkrMessageInfo.
        /// </summary>
        public uLink.NetworkMessageInfo NetworkMessageInfo
        {
            get { return _unmi; }
        }
    }
}
