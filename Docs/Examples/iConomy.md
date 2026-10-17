### Example plugin
`iConomy` - player balance economy

### Original
[`iConomy.py`](https://github.com/dretax/Python-Plugins/blob/master/FougeritePlugins/iConomy/iConomy.py) by
DreTaX.

### Concept
A per-player money balance stored in `DataStore` (table `"iConomy"`, keyed by SteamID), with:
- `/money` - shows your balance (or, for admins/moderators, another player's: `/money <name>`).
- `/pay <name> <amount>` - transfers money between two online players, with sanity checks (can't pay negative/
  zero, can't pay yourself, can't pay more than you have).
- PvP kill rewards: on `On_PlayerKilled`, the attacker gets a cut of the victim's balance (either a percentage
  of both balances, or a flat amount, depending on a configured mode), and the victim loses the matching amount
  - "killing a rich player is more profitable than killing a poor one".
- A buy/sell shop (`/buy "item" "qty"`, `/sell "item" "qty"`) against a second ini (`ShopData.ini`) mapping a
  friendly shop name to a real item name and a price - omitted below to keep the example focused on the balance
  mechanic itself; it's a straightforward `IniParser` lookup + `Inventory.AddItem`/`RemoveItem` once you have
  `GiveMoney`/`TakeMoney`.

Every mutation goes through `GetMoney`/`GiveMoney`/`TakeMoney`/`SetMoney` helpers so the "lazily default a new
player to `DefaultMoney`" logic and the balance-change notifications only need to exist in one place.

### C#

```csharp
using System;
using Fougerite;

namespace iConomy
{
    public class iConomy : Module
    {
        public override string Name { get { return "iConomy"; } }
        public override string Author { get { return "DreTaX"; } }
        public override string Description { get { return "A simple player-balance economy."; } }
        public override Version Version { get { return new Version(1, 3); } }

        private const string Table = "iConomy";
        private const double DefaultMoney = 100.0;
        private const double KillPortion = 1.25;   // attacker keeps 125% of their current balance
        private const double DeathPortion = 0.75;  // victim keeps 75% of their current balance

        public override void Initialize()
        {
            Hooks.OnPlayerConnected += OnPlayerConnected;
            Hooks.OnPlayerKilled += OnPlayerKilled;
            Hooks.OnCommand += OnCommand;
        }

        public override void DeInitialize()
        {
            Hooks.OnPlayerConnected -= OnPlayerConnected;
            Hooks.OnPlayerKilled -= OnPlayerKilled;
            Hooks.OnCommand -= OnCommand;
        }

        private double GetMoney(ulong uid)
        {
            object raw = DataStore.GetInstance().Get(Table, uid);
            if (raw == null)
            {
                DataStore.GetInstance().Add(Table, uid, DefaultMoney);
                return DefaultMoney;
            }
            return Convert.ToDouble(raw);
        }

        private void SetMoney(ulong uid, double amount)
        {
            DataStore.GetInstance().Add(Table, uid, Math.Max(0.0, amount));
        }

        private void OnPlayerConnected(Player player)
        {
            player.MessageFrom(Name, "You have $" + GetMoney(player.UID));
        }

        private void OnPlayerKilled(DeathEvent de)
        {
            if (!de.AttackerIsPlayer || !de.VictimIsPlayer || de.Attacker == null) return;
            if (de.Attacker.UID == de.Victim.UID) return;

            double aMoney = GetMoney(de.Attacker.UID);
            double vMoney = GetMoney(de.Victim.UID);
            double newAMoney = Math.Round((aMoney == 0 ? 20.0 : aMoney) * KillPortion, 2);
            double newVMoney = Math.Round(vMoney * DeathPortion, 2);

            SetMoney(de.Attacker.UID, newAMoney);
            SetMoney(de.Victim.UID, newVMoney);
            de.Attacker.MessageFrom(Name, "You found $" + (newAMoney - aMoney));
            de.Victim.MessageFrom(Name, "You lost $" + (vMoney - newVMoney));
        }

        private void OnCommand(Player player, string cmd, string[] args)
        {
            if (cmd == "money" && args.Length == 0)
            {
                player.MessageFrom(Name, "You have $" + GetMoney(player.UID));
            }
            else if (cmd == "pay" && args.Length == 2)
            {
                Player target = Server.GetServer().FindPlayer(args[0]);
                if (target == null) { player.MessageFrom(Name, "Player not found."); return; }
                double amount = double.Parse(args[1]);
                if (amount <= 0) { player.MessageFrom(Name, "Amount must be positive."); return; }
                if (target.UID == player.UID) { player.MessageFrom(Name, "You can't pay yourself."); return; }
                double balance = GetMoney(player.UID);
                if (balance < amount) { player.MessageFrom(Name, "You don't have that much money."); return; }
                SetMoney(player.UID, balance - amount);
                SetMoney(target.UID, GetMoney(target.UID) + amount);
                player.MessageFrom(Name, "You paid $" + amount + " to " + target.Name);
                target.MessageFrom(Name, "You received $" + amount + " from " + player.Name);
            }
        }
    }
}
```

### JavaScript

```javascript
var Table = "iConomy";
var DefaultMoney = 100.0;
var KillPortion = 1.25;
var DeathPortion = 0.75;

function GetMoney(uid)
{
    var raw = DataStore.Get(Table, uid);
    if (raw === null)
    {
        DataStore.Add(Table, uid, DefaultMoney);
        return DefaultMoney;
    }
    return raw;
}

function SetMoney(uid, amount)
{
    DataStore.Add(Table, uid, Math.max(0.0, amount));
}

function On_PlayerConnected(Player)
{
    Player.MessageFrom("iConomy", "You have $" + GetMoney(Player.UID));
}

function On_PlayerKilled(DeathEvent)
{
    if (!DeathEvent.AttackerIsPlayer || !DeathEvent.VictimIsPlayer || DeathEvent.Attacker === null) return;
    if (DeathEvent.Attacker.UID === DeathEvent.Victim.UID) return;

    var aMoney = GetMoney(DeathEvent.Attacker.UID);
    var vMoney = GetMoney(DeathEvent.Victim.UID);
    var newAMoney = Math.round(((aMoney === 0 ? 20.0 : aMoney) * KillPortion) * 100) / 100;
    var newVMoney = Math.round((vMoney * DeathPortion) * 100) / 100;

    SetMoney(DeathEvent.Attacker.UID, newAMoney);
    SetMoney(DeathEvent.Victim.UID, newVMoney);
    DeathEvent.Attacker.MessageFrom("iConomy", "You found $" + (newAMoney - aMoney));
    DeathEvent.Victim.MessageFrom("iConomy", "You lost $" + (vMoney - newVMoney));
}

function On_Command(Player, cmd, args)
{
    if (cmd === "money" && args.length === 0)
    {
        Player.MessageFrom("iConomy", "You have $" + GetMoney(Player.UID));
    }
    else if (cmd === "pay" && args.length === 2)
    {
        var target = Server.FindPlayer(args[0]);
        if (target === null) { Player.MessageFrom("iConomy", "Player not found."); return; }
        var amount = parseFloat(args[1]);
        if (amount <= 0) { Player.MessageFrom("iConomy", "Amount must be positive."); return; }
        if (target.UID === Player.UID) { Player.MessageFrom("iConomy", "You can't pay yourself."); return; }
        var balance = GetMoney(Player.UID);
        if (balance < amount) { Player.MessageFrom("iConomy", "You don't have that much money."); return; }
        SetMoney(Player.UID, balance - amount);
        SetMoney(target.UID, GetMoney(target.UID) + amount);
        Player.MessageFrom("iConomy", "You paid $" + amount + " to " + target.Name);
        target.MessageFrom("iConomy", "You received $" + amount + " from " + Player.Name);
    }
}
```

### Python

```python
Table = "iConomy"
DefaultMoney = 100.0
KillPortion = 1.25
DeathPortion = 0.75


class iConomy:

    def GetMoney(self, uid):
        raw = DataStore.Get(Table, uid)
        if raw is None:
            DataStore.Add(Table, uid, DefaultMoney)
            return DefaultMoney
        return float(raw)

    def SetMoney(self, uid, amount):
        DataStore.Add(Table, uid, max(0.0, amount))

    def On_PlayerConnected(self, Player):
        Player.MessageFrom("iConomy", "You have $" + str(self.GetMoney(Player.UID)))

    def On_PlayerKilled(self, DeathEvent):
        if not DeathEvent.AttackerIsPlayer or not DeathEvent.VictimIsPlayer or DeathEvent.Attacker is None:
            return
        if DeathEvent.Attacker.UID == DeathEvent.Victim.UID:
            return

        aMoney = self.GetMoney(DeathEvent.Attacker.UID)
        vMoney = self.GetMoney(DeathEvent.Victim.UID)
        newAMoney = round((aMoney if aMoney != 0 else 20.0) * KillPortion, 2)
        newVMoney = round(vMoney * DeathPortion, 2)

        self.SetMoney(DeathEvent.Attacker.UID, newAMoney)
        self.SetMoney(DeathEvent.Victim.UID, newVMoney)
        DeathEvent.Attacker.MessageFrom("iConomy", "You found $" + str(newAMoney - aMoney))
        DeathEvent.Victim.MessageFrom("iConomy", "You lost $" + str(vMoney - newVMoney))

    def On_Command(self, Player, cmd, args):
        if cmd == "money" and len(args) == 0:
            Player.MessageFrom("iConomy", "You have $" + str(self.GetMoney(Player.UID)))
        elif cmd == "pay" and len(args) == 2:
            target = Server.FindPlayer(args[0])
            if target is None:
                Player.MessageFrom("iConomy", "Player not found.")
                return
            amount = float(args[1])
            if amount <= 0:
                Player.MessageFrom("iConomy", "Amount must be positive.")
                return
            if target.UID == Player.UID:
                Player.MessageFrom("iConomy", "You can't pay yourself.")
                return
            balance = self.GetMoney(Player.UID)
            if balance < amount:
                Player.MessageFrom("iConomy", "You don't have that much money.")
                return
            self.SetMoney(Player.UID, balance - amount)
            self.SetMoney(target.UID, self.GetMoney(target.UID) + amount)
            Player.MessageFrom("iConomy", "You paid $" + str(amount) + " to " + target.Name)
            target.MessageFrom("iConomy", "You received $" + str(amount) + " from " + Player.Name)
```

### Lua

```lua
local Table = "iConomy"
local DefaultMoney = 100.0
local KillPortion = 1.25
local DeathPortion = 0.75

local function GetMoney(uid)
    local raw = DataStore:Get(Table, uid)
    if raw == nil then
        DataStore:Add(Table, uid, DefaultMoney)
        return DefaultMoney
    end
    return raw
end

local function SetMoney(uid, amount)
    DataStore:Add(Table, uid, math.max(0.0, amount))
end

function On_PlayerConnected(player)
    player:MessageFrom("iConomy", "You have $" .. GetMoney(player.UID))
end

function On_PlayerKilled(deathEvent)
    if not deathEvent.AttackerIsPlayer or not deathEvent.VictimIsPlayer or deathEvent.Attacker == nil then return end
    if deathEvent.Attacker.UID == deathEvent.Victim.UID then return end

    local aMoney = GetMoney(deathEvent.Attacker.UID)
    local vMoney = GetMoney(deathEvent.Victim.UID)
    local base = aMoney ~= 0 and aMoney or 20.0
    local newAMoney = math.floor(base * KillPortion * 100 + 0.5) / 100
    local newVMoney = math.floor(vMoney * DeathPortion * 100 + 0.5) / 100

    SetMoney(deathEvent.Attacker.UID, newAMoney)
    SetMoney(deathEvent.Victim.UID, newVMoney)
    deathEvent.Attacker:MessageFrom("iConomy", "You found $" .. (newAMoney - aMoney))
    deathEvent.Victim:MessageFrom("iConomy", "You lost $" .. (vMoney - newVMoney))
end

function On_Command(player, cmd, args)
    if cmd == "money" and #args == 0 then
        player:MessageFrom("iConomy", "You have $" .. GetMoney(player.UID))
    elseif cmd == "pay" and #args == 2 then
        local target = Server.FindPlayer(args[1])
        if target == nil then player:MessageFrom("iConomy", "Player not found."); return end
        local amount = tonumber(args[2])
        if amount <= 0 then player:MessageFrom("iConomy", "Amount must be positive."); return end
        if target.UID == player.UID then player:MessageFrom("iConomy", "You can't pay yourself."); return end
        local balance = GetMoney(player.UID)
        if balance < amount then player:MessageFrom("iConomy", "You don't have that much money."); return end
        SetMoney(player.UID, balance - amount)
        SetMoney(target.UID, GetMoney(target.UID) + amount)
        player:MessageFrom("iConomy", "You paid $" .. amount .. " to " .. target.Name)
        target:MessageFrom("iConomy", "You received $" .. amount .. " from " .. player.Name)
    end
end
```

### Notes / what was trimmed
- The original supports two reward modes (percentage-based, shown above, or a flat extra/flat penalty amount),
  separate settings per NPC species for `On_NPCKilled` rewards, admin `/givemoney`/`/takemoney`/`/setmoney`/
  `/flushiconomy`, and a full ini-driven buy/sell shop (`/buy`/`/sell`/`/price`) with quoted-argument parsing -
  all layered on top of the same `GetMoney`/`SetMoney` pair shown here.

See also: [`Classes/Data.md`](../Classes/Data.md) · [`Hooks/Player/On_PlayerKilled.md`](../Hooks/Player/On_PlayerKilled.md)
