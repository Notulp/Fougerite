namespace Fougerite.Events
{
    /// <summary>
    /// This class is used on BowShoot Hook.
    /// </summary>
    public class BowShootEvent
    {
        private readonly BowWeaponDataBlock _bw;
        private readonly Fougerite.Player _player;
        private readonly ItemRepresentation _ir;
        private readonly uLink.NetworkMessageInfo _unmi;
        private readonly IBowWeaponItem _ibw;

        public BowShootEvent(BowWeaponDataBlock bw, ItemRepresentation ir, uLink.NetworkMessageInfo ui, IBowWeaponItem ibw)
        {
            TakeDamage local = ibw.inventory.GetLocal<TakeDamage>();
            _player = Fougerite.Server.GetServer().FindPlayer(local.GetComponent<Character>().netUser.userID);
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
            get { return this._ibw; }
        }

        /// <summary>
        /// Gets the player who is shooting.
        /// </summary>
        public Fougerite.Player Player
        {
            get { return this._player; }
        }

        /// <summary>
        /// Gets the datablock of the bow.
        /// </summary>
        public BowWeaponDataBlock BowWeaponDataBlock
        {
            get { return this._bw; }
        }

        /// <summary>
        /// Item representation class.
        /// </summary>
        public ItemRepresentation ItemRepresentation
        {
            get { return this._ir; }
        }

        /// <summary>
        /// Gets the uLink.NetworkMessageInfo of the event.
        /// </summary>
        public uLink.NetworkMessageInfo NetworkMessageInfo
        {
            get { return this._unmi; }
        }
    }
}
