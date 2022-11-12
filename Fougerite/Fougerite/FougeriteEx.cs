using System;
using System.Linq;
using System.Collections.Generic;
using Fougerite;

public static class LevenshteinDistance
{
    /// http://www.dotnetperls.com/levenshtein
    /// <summary>
    /// Compute the distance between two strings.
    /// </summary>
    private static int Compute(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        // Step 1
        if (n == 0)
        {
            return m;
        }

        if (m == 0)
        {
            return n;
        }

        // Step 2
        for (int i = 0; i <= n; d[i, 0] = i++)
        {
        }

        for (int j = 0; j <= m; d[0, j] = j++)
        {
        }

        // Step 3
        for (int i = 1; i <= n; i++)
        {
            //Step 4
            for (int j = 1; j <= m; j++)
            {
                // Step 5
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                // Step 6
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        // Step 7
        return d[n, m];
    }

    public static int Distance(this string self, string test)
    {
        return Compute(self.ToUpperInvariant(), test.ToUpperInvariant());
    }

    public static double Similarity(this string self, string test)
    {
        int distance = Compute(self.ToUpperInvariant(), test.ToUpperInvariant());
        return 1.0 - ((double)distance / (double)Math.Max(self.Length, test.Length));
    }
}

public static class FougeriteEx
{
    public static StringComparison ic = StringComparison.InvariantCultureIgnoreCase;

    public static T[] Slice<T>(this T[] source, int start, int end)
    {
        // Handles negative ends.
        if (end < 0)
        {
            end = source.Length + end;
        }
        int len = end - start;
        // Return new array.
        T[] res = new T[len];
        for (int i = 0; i < len; i++)
        {
            res[i] = source[i + start];
        }
        return res;
    }

    public static string LongestCommonSubstring(this string self, string test)
    {
        int[,] lengths = new int[self.Length, test.Length];
        int greatestLength = 0;
        string lcs = string.Empty;

        // Compare each consecutive character in string a with each consecutive character in string b.
        for (int i = 0; i < self.Length; i++)
        {
            for (int j = 0; j < test.Length; j++)
            {
                //Keep a running count of the number of consecutive characters in each that are the same.
                if (self[i].ToString().Equals(test[j].ToString(), ic))
                {
                    if (i == 0 || j == 0)
                    {
                        lengths[i, j] = 1;
                    }
                    else
                    {
                        lengths[i, j] = lengths[i - 1, j - 1] + 1;
                    }
                    // When we find the greatest count so far, note the substring of which it is. 
                    if (lengths[i, j] > greatestLength)
                    {
                        greatestLength = lengths[i, j];
                        lcs = self.Substring(i - greatestLength + 1, greatestLength);
                    }
                }
                else
                {
                    lengths[i, j] = 0;
                }
            }
        }
        // Output the substring we noted.
        return lcs;
    }

    public static bool HasBPTerm(this string self)
    {
        bool flag = false;
        foreach (string term in self.Split(new char[] { ' ' }))
        {
            flag = BP.Equals(term, ic) ? true : false;
            if (flag)
                break;

            flag = BLUEPRINT.Similarity(term) > 0.666 ? true : false;

            if (flag)
                break;
        }
        return flag;
    }

    public static string BaseItem(this string self)
    {
        if (self.Length == 0)
            return self;

        ICollection<string> baseterms = new List<string>();
        foreach (string term in self.Split(new char[] { ' ' }))
        {
            if (BP.Equals(term, ic))
                continue;

            if (BLUEPRINT.Similarity(term) > 0.666)
                continue;

            baseterms.Add(term);
        }
        return string.Join(" ", baseterms.ToArray<string>());
    }

    public static string Blueprint(this string self)
    {
        if (self.Length == 0)
            return self;

        string key = self.Replace("Research Kit 1", "Research Kit");
        if (BlueprintNames.ContainsKey(key))
            return $"{key} {BlueprintNames[key]}";

        return self;
    }

    public static string MatchItemName(this string self)
    {
        Logger.LogDebug($"[MatchItemName] self={self}");
        if (self.Length == 0)
            return self;

        if (self.Contains("WoodSpike"))
            self = self.Replace("Wood", "");

        if (self.Equals("Barricade_Fence_Deployable"))
            self = "Wood Barricade";

        if (self.Equals("SingleBed"))
            self = "Bed";

        if (self.Contains("WindowFrame"))
            self = self.Replace("Frame", "");

        if (self.Contains("DoorFrame"))
            self = self.Replace("Frame", "way");

        if (self.Equals("MetalBarsWindow"))
            self = "Metal Window Bars";

        if (self.Equals("WoodBoxLarge"))
            self = "Large Wood Storage";

        if (self.Equals("WoodBox"))
            self = "Wood Storage Box";

        var queryName = from name in ItemNames
                        group name by self.BaseItem().Similarity(name) into match
                        orderby match.Key descending
                        select match.FirstOrDefault();

        var enumerable = queryName as IList<string> ?? queryName.ToList();
        if (enumerable.FirstOrDefault() == null || enumerable.Count() == 0)
        {
            Logger.LogError($"[MatchItemName] self={self} matches=NULL");
            return self;
        }

        Logger.LogDebug($"[MatchItemName] matches={string.Join(", ", enumerable.ToArray())}");

        if (self.HasBPTerm())
            return enumerable.First().Blueprint();

        return enumerable.First();
    }

    private const string BP = "BP";
    private const string BLUEPRINT = "Blueprint";

    public static readonly IEnumerable<string> ItemWords = new string[] { "556", "Ammo", "9mm", "Pistol", "Animal", "Fat", "Anti-Radiation", "Pills", "Armor", "Part", "1", "2", "3",
        "4", "5", "6", "7", "Arrow", "Bandage", "Bed", "Blood", "Draw", "Kit", "Bolt", "Action", "Rifle", "Camp", "Fire", "Can", "of", "Beans", "Tuna", "Charcoal", "Chocolate", "Bar",
        "Cloth", "Boots", "Helmet", "Pants", "Vest", "Cooked", "Chicken", "Breast", "Empty", "Casing", "Shotgun", "Shell", "Explosive", "Charge", "Explosives", "F1", "Grenade", "Flare",
        "Flashlight", "Mod", "Furnace", "Granola", "Gunpowder", "HandCannon", "Handmade", "Lockpick", "Holo", "Sight", "sight", "Hunting", "Bow", "Invisible", "Kevlar", "Large", "Medkit",
        "Spike", "Wall", "Wood", "Laser", "Leather", "Low", "Grade", "Fuel", "Quality", "Metal", "M4", "MP5A4",  "Ceiling",  "Doorway", "Foundation", "Fragments", "Ore",  "Pillar",  "Ramp",
        "Window", "Bars",  "P250", "Paper", "Pick", "Axe", "Pipe", "Primed", "Rad", "Suit", "Raw", "Recycle", "Repair", "Bench", "Research", "Revolver", "Rock", "Shells", "Silencer", "Sleeping",
        "Bag", "Small",  "Rations",  "Stash",  "Water", "Bottle", "Stone", "Hatchet", "Stones", "Sulfur",  "Supply", "Signal", "Torch", "Uber", "Weapon", "Barricade", "Gate",  "Gateway",  "Planks",
        "Shelter", "Stairs",  "Storage", "Box", "Wooden", "Door", "Workbench", "Blueprint", "BP" };

    public static readonly IEnumerable<string> ItemNames = new string[] { "556 Ammo", "9mm Ammo", "9mm Pistol", "Animal Fat", "Anti-Radiation Pills", "Armor Part 1", "Armor Part 2", "Armor Part 3", 
        "Armor Part 4", "Armor Part 5", "Armor Part 6", "Armor Part 7", "Arrow", "Bandage", "Bed", "Blood Draw Kit", "Blood", "Bolt Action Rifle", "Camp Fire", "Can of Beans", 
        "Can of Tuna", "Charcoal", "Chocolate Bar", "Cloth Boots", "Cloth Helmet", "Cloth Pants", "Cloth Vest", "Cloth", "Cooked Chicken Breast", "Empty 556 Casing",
        "Empty 9mm Casing", "Empty Shotgun Shell", "Explosive Charge", "Explosives", "F1 Grenade", "Flare", "Flashlight Mod", "Furnace", "Granola Bar", "Gunpowder", "HandCannon",
        "Handmade Lockpick", "Handmade Shell", "Hatchet", "Holo sight", "Hunting Bow", "Invisible Boots", "Invisible Helmet", "Invisible Pants", "Invisible Vest", "Kevlar Boots",
        "Kevlar Helmet", "Kevlar Pants", "Kevlar Vest", "Large Medkit", "Large Spike Wall", "Large Wood Storage", "Laser Sight", "Leather Boots", "Leather Helmet", "Leather Pants",
        "Leather Vest", "Leather", "Low Grade Fuel", "Low Quality Metal", "M4", "MP5A4", "Metal Ceiling", "Metal Door", "Metal Doorway", "Metal Foundation", "Metal Fragments",
        "Metal Ore", "Metal Pillar", "Metal Ramp", "Metal Stairs", "Metal Wall", "Metal Window Bars", "Metal Window", "P250", "Paper", "Pick Axe", "Pipe Shotgun", "Primed 556 Casing",
        "Primed 9mm Casing", "Primed Shotgun Shell", "Rad Suit Boots", "Rad Suit Helmet", "Rad Suit Pants", "Rad Suit Vest", "Raw Chicken Breast", "Recycle Kit 1", "Repair Bench", 
        "Research Kit 1", "Revolver", "Rock", "Shotgun Shells", "Shotgun", "Silencer", "Sleeping Bag", "Small Medkit", "Small Rations", "Small Stash", "Small Water Bottle", "Spike Wall", 
        "Stone Hatchet", "Stones", "Sulfur Ore", "Sulfur", "Supply Signal", "Torch", "Uber Hatchet", "Uber Hunting Bow", "Weapon Part 1", "Weapon Part 2", "Weapon Part 3", "Weapon Part 4",
        "Weapon Part 5", "Weapon Part 6", "Weapon Part 7", "Wood Barricade", "Wood Ceiling", "Wood Doorway", "Wood Foundation", "Wood Gate", "Wood Gateway", "Wood Pillar", "Wood Planks",
        "Wood Ramp", "Wood Shelter", "Wood Stairs", "Wood Storage Box", "Wood Wall", "Wood Window", "Wood", "Wooden Door", "Workbench"
    };

    public static readonly IDictionary<string, string> BlueprintNames = new Dictionary<string, string>()
    {
        { "556 Ammo", "Blueprint" }, { "9mm Ammo", "Blueprint" }, { "9mm Pistol", "Blueprint" }, { "Armor Part 1", "BP" }, { "Armor Part 2", "BP" }, { "Armor Part 3", "BP" }, { "Armor Part 4", "BP" },
        { "Armor Part 5", "BP" }, { "Armor Part 6", "BP" }, { "Armor Part 7", "BP" }, { "Arrow", "Blueprint" }, { "Bandage", "Blueprint" }, { "Bed", "Blueprint" }, { "Blood Draw Kit", "Blueprint" },
        { "Bolt Action Rifle", "Blueprint" }, { "Camp Fire", "Blueprint" }, { "Cloth Boots", "BP" }, { "Cloth Helmet", "BP" }, { "Cloth Pants", "BP" }, { "Cloth Vest", "BP" }, { "Empty Shotgun Shell", "Blueprint" },
        { "Explosive Charge", "Blueprint" }, { "Explosives", "Blueprint" }, { "F1 Grenade", "Blueprint" }, { "Flare", "Blueprint" }, { "Flashlight Mod", "Blueprint" }, { "Furnace", "Blueprint" },
        { "Gunpowder", "Blueprint" }, { "HandCannon", "Blueprint" }, { "Handmade Lockpick", "Blueprint" }, { "Handmade Shell", "Blueprint" }, { "Hatchet", "Blueprint" }, { "Holo sight", "Blueprint" },
        { "Hunting Bow", "Blueprint" }, { "Kevlar Boots", "BP" }, { "Kevlar Helmet", "BP" }, { "Kevlar Pants", "BP" }, { "Kevlar Vest", "BP" }, { "Large Medkit", "Blueprint" }, { "Large Spike Wall", "Blueprint" },
        { "Large Wood Storage", "Blueprint" }, { "Laser Sight", "Blueprint" }, { "Leather Boots", "BP" }, { "Leather Helmet", "BP" }, { "Leather Pants", "BP" }, { "Leather Vest", "BP" }, { "Low Grade Fuel", "Blueprint" },
        { "Low Quality Metal", "Blueprint" }, { "M4", "Blueprint" }, { "MP5A4", "Blueprint" }, { "Metal Ceiling", "" }, { "Metal Door", "Blueprint" }, { "Metal Doorway", "BP" }, { "Metal Foundation", "BP" },
        { "Metal Pillar", "BP" }, { "Metal Ramp", "BP" }, { "Metal Stairs", "BP" }, { "Metal Wall", "BP" }, { "Metal Window Bars", "Blueprint" }, { "Metal Window", "BP" }, { "P250", "Blueprint" },
        { "Paper", "Blueprint" }, { "Pick Axe", "Blueprint" }, { "Pipe Shotgun", "Blueprint" }, { "Primed 556 Casing", "Blueprint" }, { "Primed 9mm Casing", "Blueprint" }, { "Primed Shotgun Shell", "Blueprint" },
        { "Rad Suit Boots", "BP" }, { "Rad Suit Helmet", "BP" }, { "Rad Suit Pants", "BP" }, { "Rad Suit Vest", "BP" }, { "Repair Bench", "Blueprint" }, { "Research Kit", "Blueprint" }, { "Revolver", "Blueprint" },
        { "Shotgun Shells", "Blueprint" }, { "Shotgun", "Blueprint" }, { "Silencer", "Blueprint" }, { "Sleeping Bag", "Blueprint" }, { "Small Medkit", "Blueprint" }, { "Small Stash", "Blueprint" },
        { "Spike Wall", "Blueprint" }, { "Stone Hatchet", "Blueprint" }, { "Torch", "Blueprint" }, { "Weapon Part 1", "BP" }, { "Weapon Part 2", "BP" }, { "Weapon Part 3", "BP" }, { "Weapon Part 4", "BP" },
        { "Weapon Part 5", "BP" }, { "Weapon Part 6" , "BP" }, { "Weapon Part 7", "BP" }, { "Wood Barricade", "Blueprint" }, { "Wood Ceiling", "BP" }, { "Wood Doorway", "BP" }, { "Wood Foundation", "BP" },
        { "Wood Gate", "Blueprint" }, { "Wood Gateway", "Blueprint" }, { "Wood Pillar", "BP" }, { "Wood Planks", "Blueprint" }, { "Wood Ramp", "BP" }, { "Wood Shelter", "BP" }, { "Wood Stairs", "BP" },
        { "Wood Storage Box", "Blueprint" }, { "Wood Wall", "BP" }, { "Wood Window", "BP" }, { "Wooden Door", "Blueprint" }, { "Workbench", "Blueprint" }
    };
}
