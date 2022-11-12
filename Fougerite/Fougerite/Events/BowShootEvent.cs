namespace Fougerite.Events
{
    /// <summary>
    /// This class is used on BowShoot Hook.
    /// </summary>
    public class BowShootEvent
    {
        private readonly BowWeaponDataBlock _bw;
        private readonly Player _player;
        private readonly ItemRepresentation _ir;
        private readonly uLink.NetworkMessageInfo _unmi;
        private readonly IBowWeaponItem _ibw;

        public BowShootEvent(BowWeaponDataBlock bw, ItemRepresentation ir, uLink.NetworkMessageInfo ui, IBowWeaponItem ibw)
        {
            TakeDamage local = ibw.inventory.GetLocal<TakeDamage>();
            _player = Server.GetServer().FindPlayer(local.GetComponent<Character>().netUser.userID);
            _bw = bw;
            _ibw = ibw;
            _ir = ir;
            _unmi = ui;
        }

        /// <summary>
        /// Removes the arrow that is flying.
        /// </summary>
        public void RemoveArrow()
        {
            IBowWeaponItem.RemoveArrowInFlight();
        }

        /// <summary>
        /// Gets the IBowWeaponItem class
        /// </summary>
        public IBowWeaponItem IBowWeaponItem
        {
            get { return _ibw; }
        }

        /// <summary>
        /// Gets the player who is shooting.
        /// </summary>
        public Player Player
        {
            get { return _player; }
        }

        /// <summary>
        /// Gets the datablock of the bow.
        /// </summary>
        public BowWeaponDataBlock BowWeaponDataBlock
        {
            get { return _bw; }
        }

        /// <summary>
        /// Item representation class.
        /// </summary>
        public ItemRepresentation ItemRepresentation
        {
            get { return _ir; }
        }

        /// <summary>
        /// Gets the uLink.NetworkMessageInfo of the event.
        /// </summary>
        public uLink.NetworkMessageInfo NetworkMessageInfo
        {
            get { return _unmi; }
        }
    }
}
