using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

public class DevDiceRollCmd : ConsoleCmdAbstract
{

    private static string info = "DevDiceRoll";

    public override string[] GetCommands()
    {
        return new string[2] { info, "dr" };
    }

    public override string GetDescription() => "Dice Roller";

    public override string GetHelp() =>
        "Dice Roller examples:\n" +
        "dr list # list all loot groups\n" +
        "dr once garbage 2 # roll dice once for garbage at stage 2\n" +
        "dr roll 20 junk 1 # roll 20 dices for junk at stage 1\n" +
        "dr search drinkYuccaJuiceSmoothie # search what yields an item (takes long!)\n" +
        "dr avg iceMachine drinkYuccaJuiceSmoothie 20000 1 # get average of 20k runs at stage 1\n";

    static readonly FieldInfo FieldLootContainers = AccessTools
        .Field(typeof(LootContainer), "lootContainers");

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {

        if (_params.Count == 0)
        {
            Log.Warning("No argument for dice roller");
        }
        else if (_params.Count == 1)
        {
            switch (_params[0])
            {
                case "list":
                    int i = 0;
                    if (FieldLootContainers.GetValue(null) is Dictionary<string, LootContainer> dict)
                    {
                        Log.Out("Listing all container groups:");
                        foreach (var kv in dict)
                        {
                            Log.Out("{0}: {1}", ++i, kv.Key);
                        }
                    }
                    break;
                default:
                    Log.Warning("Unknown command " + _params[0]);
                    break;
            }
        }
        else if (_params.Count == 2)
        {
            switch (_params[0])
            {
                case "list":
                    int i = 0;
                    if (FieldLootContainers.GetValue(null) is Dictionary<string, LootContainer> dict)
                    {
                        Log.Out("Listing all container groups:");
                        foreach (var kv in dict)
                        {
                            i += 1;
                            if (kv.Key.IndexOf(_params[1]) == -1) continue;
                            Log.Out("{0}: {1}", i, kv.Key);
                        }
                    }
                    break;
                case "search":
                    SearchFor(_params[1]);
                    break;
                default:
                    Log.Warning("Unknown command " + _params[0]);
                    break;
            }
        }
        else if (_params.Count == 3)
        {
            switch (_params[0])
            {
                case "once":
                    Log.Out("Rolling dice once results in:");
                    RollDiceOnce(_params[1], float.Parse(_params[2]));
                    break;
                default:
                    Log.Warning("Unknown command " + _params[0]);
                    break;
            }

        }
        else if (_params.Count == 4)
        {
            switch (_params[0])
            {
                case "roll":
                    Log.Out("Rolling dices {0} times:", _params[1]);
                    RollDices(int.Parse(_params[1]),
                        _params[2], float.Parse(_params[3]));
                    break;
                default:
                    Log.Warning("Unknown command " + _params[0]);
                    break;
            }

        }
        else if (_params.Count == 5)
        {
            switch (_params[0])
            {
                case "avg":
                    AverageItem(
                        _params[1],
                        _params[2],
                        int.Parse(_params[3]),
                        int.Parse(_params[4]));
                    break;
                default:
                    Log.Warning("Unknown command " + _params[0]);
                    break;
            }

        }
        else
        {
            Log.Warning("Too many arguments");
        }

    }

    static GameRandom Random = null;

    private static void AddItems(IList<ItemStack> src, IList<ItemStack> others)
    {
        foreach (var other in others)
        {
            bool added = false;
            foreach (var org in src)
            {
                if (other.itemValue.type == org.itemValue.type)
                {
                    // Added
                    org.count += other.count;
                    added = true;
                    break;
                }
            }
            if (added == false)
            {
                src.Add(other);
            }
        }
    }

    private void RollDiceOnce(string container, float stage)
    {

        if (Random == null) Random = GameRandomManager.Instance.CreateGameRandom();
        LootContainer loot = LootContainer.GetLootContainer(container);
        var player = GameManager.Instance.World.GetPrimaryPlayer();

        FastTags tags = new FastTags();

        IList<ItemStack> list = loot.Spawn(Random, 80, stage, 0.0f, player, tags);

        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
            int type = item.itemValue.type;
            if (type < Block.ItemsStartHere)
            {
                Block block = Block.list[type];
                Log.Out("{0}: {1} Blocks {2}", i,
                    item.count, block.GetBlockName());
            }
            else
            {
                int itemId = type - Block.ItemsStartHere;
                Log.Out("{0}: {1} Items {2}", i, item.count,
                    item.itemValue.ItemClass.GetItemName());
            }
        }

    }


    private void RollDices(int rolls, string container, float stage)
    {

        if (Random == null) Random = GameRandomManager.Instance.CreateGameRandom();
        LootContainer loot = LootContainer.GetLootContainer(container);
        var player = GameManager.Instance.World.GetPrimaryPlayer();

        FastTags tags = new FastTags();

        List<ItemStack> summed = new List<ItemStack>();
        for (int i = 0; i < rolls; i++)
        {
            AddItems(summed, loot.Spawn(Random, 80, stage, 0.0f, player, tags));
        }

        summed.Sort(delegate (ItemStack x, ItemStack y)
        {
            return y.count.CompareTo(x.count);
        });

        for (int i = 0; i < summed.Count; i++)
        {
            var item = summed[i];
            int type = item.itemValue.type;
            if (type < Block.ItemsStartHere)
            {
                Block block = Block.list[type];
                Log.Out("{0}: {1} Blocks {2}", i,
                    item.count, block.GetBlockName());
            }
            else
            {
                int itemId = type - Block.ItemsStartHere;
                Log.Out("{0}: {1} Items {2}", i, item.count,
                    item.itemValue.ItemClass.GetItemName());
            }
        }

    }

    static string GetLootName(ItemValue item)
    {
        int type = item.type;
        if (type < Block.ItemsStartHere)
        {
            Block block = Block.list[type];
            return block.GetBlockName();
        }
        return item.ItemClass.GetItemName();
    }


    private static void FindInLoot(LootContainer loot, int runs,
        EntityPlayer player, string search, float stage)
    {
        FastTags tags = new FastTags();
        for (int i = 0; i < runs; i++)
        {
            IList<ItemStack> list = loot.Spawn(
                Random, 80, stage, 0.0f, player, tags);
            foreach (ItemStack stack in list)
            {
                string name = GetLootName(stack.itemValue);
                if (name.ToLower().IndexOf(search) == -1) continue;
                Log.Out("Found {0} in {1} after {2} rolls at stage {3}",
                    name, loot.Name, i, stage);
                return;
            }
        }
    }

    private void SearchFor(string item)
    {
        if (Random == null) Random = GameRandomManager.Instance.CreateGameRandom();
        var player = GameManager.Instance.World.GetPrimaryPlayer();

        string search = item.ToLower();

        Log.Out("Searching for {0} in all loot containers", item);
        if (FieldLootContainers.GetValue(null) is Dictionary<string, LootContainer> dict)
        {
            foreach (var kv in dict)
            {
                for (float stage = 0; stage < 5; stage += 1)
                {
                    FindInLoot(kv.Value, 750,
                        player, search, stage);
                }
            }
        }
    }

    private void AverageItem(string container, string item, int runs, int stage)
    {
        if (Random == null) Random = GameRandomManager.Instance.CreateGameRandom();
        var player = GameManager.Instance.World.GetPrimaryPlayer();

        string search = item.ToLower();

        LootContainer loot = LootContainer.GetLootContainer(container);
        if (loot == null) throw new ArgumentException("Container invalid");

        int count = 0;
        int found = 0;

        FastTags tags = new FastTags();
        for (int i = 0; i < runs; i++)
        {
            IList<ItemStack> list = loot.Spawn(
                Random, 80, stage, 0.0f, player, tags);
            foreach (ItemStack stack in list)
            {
                string name = GetLootName(stack.itemValue);
                if (name.ToLower().IndexOf(search) == -1) continue;
                found += 1; count += stack.count;
                break;
            }
        }

        Log.Out("Averaging {0} runs", runs);
        Log.Out(" Found: {0} avg {1}%", found, 100f * found / runs);
        Log.Out(" Count: {0} avg {1}%", count, 100f * count / runs);

    }

}
