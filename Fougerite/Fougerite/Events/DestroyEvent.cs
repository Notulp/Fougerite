namespace Fougerite.Events
{
	/// <summary>
	/// This class runs when an entity is destroyed.
	/// </summary>
	public class DestroyEvent
	{
		private object _attacker;
		private DamageEvent _de;
		private bool _decay;
		private Entity _ent;
		private string _weapon;
		private WeaponImpact _wi;

		public DestroyEvent(ref DamageEvent d, Entity ent, bool isdecay)
		{
			Player player = Server.GetServer().FindPlayer(d.attacker.client.userID);
			if (player != null) 
			{
				Attacker = player;
			}

			WeaponData = null;
			IsDecay = isdecay;
			DamageEvent = d;
			Entity = ent;

			string weaponName = "Unknown";
			if (d.extraData is WeaponImpact weaponImpact)
			{
				WeaponData = weaponImpact;
				if (weaponImpact.dataBlock != null)
				{
					weaponName = weaponImpact.dataBlock.name;
				}
			}
			else
			{
				string strType = d.attacker.id.ToString();
				if (d.attacker.id is TimedExplosive)
					weaponName = "Explosive Charge";
				else if (d.attacker.id is TimedGrenade)
					weaponName = "F1 Grenade";
				else if (strType.Contains("MutantBear"))
					weaponName = "Mutant Bear Claw";
				else if (strType.Contains("Bear"))
					weaponName = "Bear Claw";
				else if (strType.Contains("MutantWolf"))
					weaponName = "Mutant Wolf Claw";
				else if (strType.Contains("Wolf"))
					weaponName = "Wolf Claw";
				else if (d.attacker.id.Equals(d.victim.id))
					weaponName = string.Format("Self ({0})", DamageType);
				else
					weaponName = "Hunting Bow";
			}
			WeaponName = weaponName;
		}

		/// <summary>
		/// Returns the Attacker's object. Can be anything from decay to player.
		/// </summary>
		public object Attacker
		{
			get
			{
				return _attacker;
			}
			set
			{
				_attacker = value;
			}
		}

		/// <summary>
		/// Gets the last damage of the event.
		/// </summary>
		public float DamageAmount
		{
			get
			{
				return _de.amount;
			}
			set
			{
				_de.amount = value;
			}
		}

		/// <summary>
		/// Gets the DamageEvent class
		/// </summary>
		public DamageEvent DamageEvent
		{
			get
			{
				return _de;
			}
			set
			{
				_de = value;
			}
		}

		/// <summary>
		/// This Getter tries to find the cause of the damage if possible.
		/// </summary>
		public string DamageType
		{
			get
			{
				string str = "Unknown";
				switch ((int) DamageEvent.damageTypes)
				{
					case 0:
						return "Bleeding";

					case 1:
						return "Generic";

					case 2:
						return "Bullet";

					case 3:
					case 5:
					case 6:
					case 7:
						return str;

					case 4:
						return "Melee";

					case 8:
						return "Explosion";

					case 0x10:
						return "Radiation";

					case 0x20:
						return "Cold";
				}
				return str;
			}
		}

		/// <summary>
		/// This getter returns the entity that is being destroyed.
		/// </summary>
		public Entity Entity
		{
			get
			{
				return _ent;
			}
			set
			{
				_ent = value;
			}
		}

		/// <summary>
		/// Gets if the destroy was caused by decay.
		/// </summary>
		public bool IsDecay
		{
			get
			{
				return _decay;
			}
			set
			{
				_decay = value;
			}
		}

		/// <summary>
		/// Gets the weaponimpact of the event.
		/// </summary>
		public WeaponImpact WeaponData
		{
			get
			{
				return _wi;
			}
			set
			{
				_wi = value;
			}
		}

		/// <summary>
		/// Gets the weapon's name that caused the damage.
		/// </summary>
		public string WeaponName
		{
			get
			{
				return _weapon;
			}
			set
			{
				_weapon = value;
			}
		}
	}
}

