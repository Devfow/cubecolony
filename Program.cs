using System;
using System.Collections.Generic;
using System.Linq;
using CheapNeuroSim;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => webBuilder.UseStartup<Startup>())
    .Build()
    .Run();

public sealed class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<CubeColony>();
    }

    public void Configure(IApplicationBuilder app)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/api/state", async context =>
            {
                var colony = context.RequestServices.GetRequiredService<CubeColony>();
                await context.Response.WriteAsJsonAsync(colony.TickAndSnapshot());
            });

            endpoints.MapPost("/api/command", async context =>
            {
                var colony = context.RequestServices.GetRequiredService<CubeColony>();
                var command = await System.Text.Json.JsonSerializer.DeserializeAsync<WorldCommand>(
                    context.Request.Body,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (command == null)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                colony.ApplyCommand(command);
                await context.Response.WriteAsJsonAsync(colony.TickAndSnapshot());
            });
        });
    }
}

public sealed class CubeColony
{
    private readonly object _gate = new();
    private readonly List<BotBody> _bots = new();
    private readonly List<WorldItem> _items = new();
    private readonly List<NewsLine> _news = new();
    private readonly bool[,] _open;
    private readonly StableRandom _random = new(20260503);
    private int _tick;
    private int _nextId = 1;

    public CubeColony()
    {
        Width = 6;
        Height = 5;
        _open = new bool[Width, Height];
        for (var x = 1; x < 5; x++)
        {
            for (var y = 1; y < 4; y++)
            {
                _open[x, y] = true;
            }
        }

        AddBot(2, 2, "Vend-0", 0);
        AddBot(3, 2, "Lift-7", 1);
        AddBot(2, 1, "Chime-3", 2);
        AddNews("The first cube lights blink awake.");
    }

    public int Width { get; }
    public int Height { get; }

    public WorldSnapshot TickAndSnapshot()
    {
        lock (_gate)
        {
            Tick();
            return Snapshot();
        }
    }

    public void ApplyCommand(WorldCommand command)
    {
        lock (_gate)
        {
            var bot = _bots.FirstOrDefault(b => b.Id == command.BotId && b.Alive);
            switch ((command.Kind ?? string.Empty).ToLowerInvariant())
            {
                case "feed":
                    if (bot != null)
                    {
                        bot.Feed();
                        AddNews($"{bot.Name} receives nutrient gel.");
                    }
                    break;
                case "pet":
                    if (bot != null)
                    {
                        bot.Comfort();
                        AddNews($"{bot.Name} is comforted through the glass.");
                    }
                    break;
                case "caffeine":
                    if (bot != null)
                    {
                        bot.Dose(AddictiveStimulus.MildReward(0.7f), "caffeine fizz");
                        AddNews($"{bot.Name} fizzes with caffeine.");
                    }
                    break;
                case "poppy":
                    if (bot != null)
                    {
                        bot.Dose(new AddictiveStimulus(0.25f, 0.85f, 0.08f, 0.35f), "poppy tea");
                        AddNews($"{bot.Name} softens after poppy tea.");
                    }
                    break;
                case "teach":
                    if (bot != null)
                    {
                        bot.Teach(command.TopicId == 0 ? 12 : command.TopicId);
                        AddNews($"{bot.Name} learns a vending proverb.");
                    }
                    break;
                case "add":
                    TryAddBot(command.X, command.Y);
                    break;
                case "toggle":
                    ToggleCube(command.X, command.Y);
                    break;
            }
        }
    }

    private void Tick()
    {
        _tick++;
        MaybeSpawnItem();
        foreach (var bot in _bots)
        {
            if (!bot.Alive)
            {
                continue;
            }

            var neighbors = LivingNeighbors(bot).ToArray();
            var social = Math.Min(1f, neighbors.Length / 3f);
            var threat = neighbors.Count(n => n.GroupId != bot.GroupId && n.LastDebug.SocialBias.ThreatByGroup.Length > bot.GroupId) * 0.12f;
            var novelty = bot.LastX == bot.X && bot.LastY == bot.Y ? 0.15f : 0.65f;
            var nearbyItem = ClosestItem(bot);
            if (nearbyItem != null)
            {
                novelty = Math.Min(1f, novelty + 0.25f);
                if (nearbyItem.Kind == ItemKind.Threat)
                {
                    threat = Math.Min(1f, threat + 0.22f);
                }
            }

            var hunger = 1f - bot.LastDebug.Needs.Nutrition;
            var pain = 1f - bot.LastDebug.Needs.Integrity;
            var energy = bot.LastDebug.Needs.Energy;
            var reward = bot.PendingReward + social * 0.05f - threat * 0.05f - pain * 0.2f;

            var output = bot.Brain.Tick(
                BrainInput.FromArray(new[] { hunger, threat, pain, social, novelty, energy, 0.2f, 0f }),
                reward,
                1f);

            bot.PendingReward = 0f;
            bot.LastDebug = bot.Brain.GetDebugSnapshot();
            bot.Age++;

            Act(bot, output, neighbors);
            ConsumeItemIfPresent(bot);
            if (bot.LastDebug.Needs.Integrity <= 0.02f || bot.LastDebug.Needs.Nutrition <= 0.01f)
            {
                bot.Alive = false;
                bot.LastEvent = "went dark";
                AddNews(DescribeDeath(bot));
            }
            else
            {
                MaybeReportNeed(bot);
            }
        }

        foreach (var bot in _bots.Where(b => b.Alive).ToArray())
        {
            foreach (var other in LivingNeighbors(bot))
            {
                bot.Brain.ReceiveSignal(other.Brain.EmitSignal(), new SocialIdentity(other.GroupId), 0.10f);
                bot.Brain.ObserveIndividualInteraction(new IndividualIdentity(other.Id, new SocialIdentity(other.GroupId)), 0.05f, 0f, 0.03f);

                if ((_tick + bot.Id + other.Id) % 17 == 0)
                {
                    var meme = other.Brain.ExpressMeme(12, 0.55f);
                    bot.Brain.ReceiveMeme(meme, new SocialIdentity(other.GroupId), 0.55f, (uint)(_tick + bot.Id));
                    bot.LastEvent = "heard a meme";
                    AddNews($"{bot.Name} hears a meme from {other.Name}.");
                }
            }
        }

        if (_tick % 40 == 0)
        {
            ReproduceBestPair();
        }
    }

    private void Act(BotBody bot, BrainOutput output, BotBody[] neighbors)
    {
        bot.LastX = bot.X;
        bot.LastY = bot.Y;
        if (output[BrainChannels.Explore] > 0.62f || bot.LastDebug.Goal.Primary == GoalKind.Explore)
        {
            var target = BestItemTarget(bot);
            if (target != null && TryMoveToward(bot, target.X, target.Y))
            {
                return;
            }

            TryMove(bot);
        }

        if (output[BrainChannels.Bond] > 0.68f && neighbors.Length > 0)
        {
            bot.LastEvent = "chirped hello";
            bot.PendingReward += 0.03f;
            AddNews($"{bot.Name} chirps hello.");
        }

        if (bot.LastDebug.Goal.Primary == GoalKind.SeekAddictiveStimulus)
        {
            bot.LastEvent = "wants a dose";
            MaybeAddGoalNews(bot, "is searching for relief; craving is steering its brain.");
        }

        if (bot.LastDebug.Goal.Primary == GoalKind.Rest)
        {
            bot.Brain.OfflineConsolidate(0.7f, 1f);
            MaybeAddGoalNews(bot, "is resting so its organoid can settle and consolidate memories.");
        }
    }

    private void MaybeReportNeed(BotBody bot)
    {
        if (_tick - bot.LastNeedNewsTick < 10)
        {
            return;
        }

        var debug = bot.LastDebug;
        var message = ComposeNeedMessage(bot, debug);
        if (message.Length == 0)
        {
            return;
        }

        bot.LastNeedNewsTick = _tick;
        AddNews(message);
    }

    private string ComposeNeedMessage(BotBody bot, BrainDebugSnapshot debug)
    {
        var needs = debug.Needs;
        if (needs.Nutrition < 0.20f)
        {
            return $"{bot.Name} is starving; it needs food soon.";
        }

        if (needs.Integrity < 0.30f)
        {
            return $"{bot.Name} is damaged and wants repair or rest.";
        }

        if (needs.Energy < 0.22f)
        {
            return $"{bot.Name} is running low on charge.";
        }

        if (debug.Addiction.Craving > 0.55f)
        {
            return $"{bot.Name} is craving a dose; its reward system is pulling hard.";
        }

        if (debug.Trauma.Load > 0.45f || debug.Emotion.Primary == EmotionKind.Afraid)
        {
            return $"{bot.Name} is afraid; cortisol and threat memories are high.";
        }

        if (needs.Social < 0.25f)
        {
            return $"{bot.Name} is lonely and wants a neighbor.";
        }

        var target = BestItemTarget(bot);
        if (target != null && debug.Goal.Primary == GoalKind.Explore)
        {
            return $"{bot.Name} noticed {target.Icon} {target.Name} and is rolling toward it.";
        }

        if (debug.Goal.Primary == GoalKind.Bond && LivingNeighbors(bot).Any())
        {
            return $"{bot.Name} wants social contact with a neighboring brain.";
        }

        return string.Empty;
    }

    private void MaybeAddGoalNews(BotBody bot, string text)
    {
        if (_tick - bot.LastGoalNewsTick < 12)
        {
            return;
        }

        bot.LastGoalNewsTick = _tick;
        AddNews($"{bot.Name} {text}");
    }

    private static string DescribeDeath(BotBody bot)
    {
        var needs = bot.LastDebug.Needs;
        if (needs.Nutrition <= 0.01f && needs.Integrity <= 0.02f)
        {
            return $"{bot.Name} goes dark in cube {bot.X},{bot.Y}; starvation and damage overwhelmed the organoid.";
        }

        if (needs.Nutrition <= 0.01f)
        {
            return $"{bot.Name} goes dark in cube {bot.X},{bot.Y}; nutrition reached zero.";
        }

        return $"{bot.Name} goes dark in cube {bot.X},{bot.Y}; its body integrity collapsed.";
    }

    private void TryMove(BotBody bot)
    {
        var directions = new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
        var start = (int)(_random.NextUInt() % 4);
        for (var i = 0; i < directions.Length; i++)
        {
            var d = directions[(start + i) % directions.Length];
            var nx = bot.X + d.Item1;
            var ny = bot.Y + d.Item2;
            if (IsOpen(nx, ny) && _bots.All(b => !b.Alive || b.X != nx || b.Y != ny))
            {
                bot.X = nx;
                bot.Y = ny;
                bot.LastEvent = "rolled to a cube";
                return;
            }
        }
    }

    private void ReproduceBestPair()
    {
        var parents = _bots.Where(b => b.Alive).OrderByDescending(b => b.LastDebug.Needs.Energy + b.LastDebug.Needs.Nutrition + b.LastDebug.Needs.Social).Take(2).ToArray();
        if (parents.Length < 2 || _bots.Count(b => b.Alive) >= 10)
        {
            return;
        }

        foreach (var parent in parents)
        {
            foreach (var spot in Adjacent(parent.X, parent.Y))
            {
                if (IsOpen(spot.x, spot.y) && _bots.All(b => !b.Alive || b.X != spot.x || b.Y != spot.y))
                {
                    var child = new BotBody(_nextId++, $"Bud-{_nextId}", spot.x, spot.y, parent.GroupId, parent.Genome.Mutated((uint)(_tick + _nextId)));
                    child.LastEvent = "grew from a copied organoid";
                    _bots.Add(child);
                    AddNews($"{child.Name} buds into cube {spot.x},{spot.y}.");
                    return;
                }
            }
        }
    }

    private void MaybeSpawnItem()
    {
        if (_items.Count >= 10 || _tick % 9 != 0)
        {
            return;
        }

        if (!_random.Chance(0.62f))
        {
            return;
        }

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var x = _random.NextInt(0, Width);
            var y = _random.NextInt(0, Height);
            if (!IsOpen(x, y) || _bots.Any(b => b.Alive && b.X == x && b.Y == y) || _items.Any(i => i.X == x && i.Y == y))
            {
                continue;
            }

            var kind = PickItemKind();
            var item = WorldItem.Create(kind, x, y, _tick);
            _items.Add(item);
            AddNews($"{item.Icon} {item.Name} appears in cube {x},{y}.");
            return;
        }
    }

    private ItemKind PickItemKind()
    {
        var roll = _random.NextFloat(0f, 1f);
        if (roll < 0.28f) return ItemKind.Food;
        if (roll < 0.46f) return ItemKind.Charger;
        if (roll < 0.62f) return ItemKind.Toy;
        if (roll < 0.76f) return ItemKind.MemeTablet;
        if (roll < 0.88f) return ItemKind.Caffeine;
        if (roll < 0.96f) return ItemKind.PoppyTea;
        return ItemKind.Threat;
    }

    private WorldItem? ClosestItem(BotBody bot)
    {
        WorldItem? best = null;
        var bestDistance = int.MaxValue;
        foreach (var item in _items)
        {
            var distance = Math.Abs(item.X - bot.X) + Math.Abs(item.Y - bot.Y);
            if (distance < bestDistance)
            {
                best = item;
                bestDistance = distance;
            }
        }

        return best;
    }

    private WorldItem? BestItemTarget(BotBody bot)
    {
        WorldItem? best = null;
        var bestScore = 0f;
        foreach (var item in _items)
        {
            var distance = Math.Abs(item.X - bot.X) + Math.Abs(item.Y - bot.Y);
            var score = item.DesireScore(bot.LastDebug) - distance * 0.10f;
            if (score > bestScore)
            {
                best = item;
                bestScore = score;
            }
        }

        return bestScore > 0.15f ? best : null;
    }

    private bool TryMoveToward(BotBody bot, int targetX, int targetY)
    {
        var candidates = Adjacent(bot.X, bot.Y)
            .OrderBy(p => Math.Abs(p.x - targetX) + Math.Abs(p.y - targetY))
            .ToArray();

        foreach (var spot in candidates)
        {
            if (IsOpen(spot.x, spot.y) && _bots.All(b => !b.Alive || b.X != spot.x || b.Y != spot.y))
            {
                bot.X = spot.x;
                bot.Y = spot.y;
                bot.LastEvent = "rolled toward something";
                return true;
            }
        }

        return false;
    }

    private void ConsumeItemIfPresent(BotBody bot)
    {
        var item = _items.FirstOrDefault(i => i.X == bot.X && i.Y == bot.Y);
        if (item == null)
        {
            return;
        }

        _items.Remove(item);
        item.Apply(bot);
        AddNews($"{bot.Name} uses {item.Icon} {item.Name}: {bot.LastEvent}.");
    }

    private IEnumerable<BotBody> LivingNeighbors(BotBody bot)
    {
        return _bots.Where(other => other.Alive && other.Id != bot.Id && Math.Abs(other.X - bot.X) + Math.Abs(other.Y - bot.Y) == 1);
    }

    private IEnumerable<(int x, int y)> Adjacent(int x, int y)
    {
        yield return (x + 1, y);
        yield return (x - 1, y);
        yield return (x, y + 1);
        yield return (x, y - 1);
    }

    private bool IsOpen(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Width && y < Height && _open[x, y];
    }

    private void ToggleCube(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        if (_bots.Any(b => b.Alive && b.X == x && b.Y == y))
        {
            return;
        }

        _open[x, y] = !_open[x, y];
    }

    private void TryAddBot(int x, int y)
    {
        if (!IsOpen(x, y) || _bots.Any(b => b.Alive && b.X == x && b.Y == y))
        {
            return;
        }

        AddBot(x, y, $"Unit-{_nextId}", _nextId % 3);
        AddNews($"A new organoid unit boots in cube {x},{y}.");
    }

    private void AddBot(int x, int y, string name, int groupId)
    {
        if (!IsOpen(x, y))
        {
            _open[x, y] = true;
        }

        var bot = new BotBody(_nextId++, name, x, y, groupId, BrainGenome.CreateFirstGeneration((uint)(100 + _nextId)));
        bot.Brain.ConfigureSocialIdentity(groupId, 3, groupId == 0 ? SocialBiasSettings.LowBias() : SocialBiasSettings.PrejudicedCulture());
        bot.Brain.TeachCulture(12, groupId == 0 ? 0.4f : -0.25f, 0.5f);
        _bots.Add(bot);
    }

    private void AddNews(string text)
    {
        _news.Add(new NewsLine { Tick = _tick, Text = text });
        if (_news.Count > 80)
        {
            _news.RemoveAt(0);
        }
    }

    private WorldSnapshot Snapshot()
    {
        return new WorldSnapshot
        {
            Tick = _tick,
            Width = Width,
            Height = Height,
            Open = Enumerable.Range(0, Width)
                .SelectMany(x => Enumerable.Range(0, Height).Select(y => new CubeSnapshot { X = x, Y = y, Open = _open[x, y] }))
                .ToArray(),
            Bots = _bots.Select(BotSnapshot.From).ToArray(),
            Items = _items.Select(ItemSnapshot.From).ToArray(),
            News = _news.ToArray()
        };
    }
}

public enum ItemKind
{
    Food,
    Charger,
    Toy,
    MemeTablet,
    Caffeine,
    PoppyTea,
    Threat
}

public sealed class WorldItem
{
    public ItemKind Kind { get; private set; }
    public string Icon { get; private set; } = "";
    public string Name { get; private set; } = "";
    public int X { get; private set; }
    public int Y { get; private set; }
    public int BornTick { get; private set; }

    public static WorldItem Create(ItemKind kind, int x, int y, int tick)
    {
        var item = new WorldItem { Kind = kind, X = x, Y = y, BornTick = tick };
        switch (kind)
        {
            case ItemKind.Food:
                item.Icon = "🍜";
                item.Name = "nutrient noodles";
                break;
            case ItemKind.Charger:
                item.Icon = "🔋";
                item.Name = "warm battery";
                break;
            case ItemKind.Toy:
                item.Icon = "🧸";
                item.Name = "soft mascot";
                break;
            case ItemKind.MemeTablet:
                item.Icon = "📜";
                item.Name = "meme tablet";
                break;
            case ItemKind.Caffeine:
                item.Icon = "☕";
                item.Name = "caffeine cup";
                break;
            case ItemKind.PoppyTea:
                item.Icon = "🍵";
                item.Name = "poppy tea";
                break;
            default:
                item.Icon = "⚠️";
                item.Name = "sparking hazard";
                break;
        }

        return item;
    }

    public float DesireScore(BrainDebugSnapshot debug)
    {
        switch (Kind)
        {
            case ItemKind.Food:
                return 1f - debug.Needs.Nutrition;
            case ItemKind.Charger:
                return 1f - debug.Needs.Energy;
            case ItemKind.Toy:
                return Math.Max(1f - debug.Needs.Social, 1f - debug.Needs.Stimulation);
            case ItemKind.MemeTablet:
                return debug.Attention.Novelty * 0.55f + debug.Temperament.Sociability * 0.25f;
            case ItemKind.Caffeine:
                return debug.Addiction.Craving * 0.45f + debug.Attention.Novelty * 0.25f + (1f - debug.Needs.Energy) * 0.25f;
            case ItemKind.PoppyTea:
                return debug.Addiction.Withdrawal * 0.55f + debug.Chemicals.Cortisol * 0.25f + debug.Trauma.Load * 0.25f;
            case ItemKind.Threat:
                return debug.Temperament.NoveltySeeking * 0.08f - debug.Temperament.HarmAvoidance * 0.4f;
            default:
                return 0f;
        }
    }

    public void Apply(BotBody bot)
    {
        switch (Kind)
        {
            case ItemKind.Food:
                bot.PendingReward += 0.45f;
                bot.LastEvent = "slurped nutrient noodles";
                break;
            case ItemKind.Charger:
                bot.PendingReward += 0.22f;
                bot.LastEvent = "warmed on a battery";
                break;
            case ItemKind.Toy:
                bot.PendingReward += 0.16f;
                bot.Brain.OfflineConsolidate(0.55f, 1f);
                bot.LastEvent = "played with a mascot";
                break;
            case ItemKind.MemeTablet:
                bot.Brain.TeachCulture(12, 0.65f, 0.75f);
                bot.PendingReward += 0.10f;
                bot.LastEvent = "read a meme tablet";
                break;
            case ItemKind.Caffeine:
                bot.Dose(AddictiveStimulus.MildReward(0.8f), "found caffeine");
                break;
            case ItemKind.PoppyTea:
                bot.Dose(new AddictiveStimulus(0.25f, 0.85f, 0.08f, 0.35f), "found poppy tea");
                break;
            case ItemKind.Threat:
                bot.PendingReward -= 0.35f;
                bot.LastEvent = "was shocked by a hazard";
                break;
        }
    }
}

public sealed class BotBody
{
    public BotBody(int id, string name, int x, int y, int groupId, BrainGenome genome)
    {
        Id = id;
        Name = name;
        X = x;
        Y = y;
        LastX = x;
        LastY = y;
        GroupId = groupId;
        Genome = genome;
        Brain = genome.CreateBrain();
        Brain.ConfigureSocialIdentity(groupId, 3, SocialBiasSettings.LowBias());
        LastDebug = Brain.GetDebugSnapshot();
    }

    public int Id { get; }
    public string Name { get; }
    public int X { get; set; }
    public int Y { get; set; }
    public int LastX { get; set; }
    public int LastY { get; set; }
    public int GroupId { get; }
    public int Age { get; set; }
    public bool Alive { get; set; } = true;
    public float PendingReward { get; set; }
    public int LastNeedNewsTick { get; set; } = -100;
    public int LastGoalNewsTick { get; set; } = -100;
    public string LastEvent { get; set; } = "booted";
    public BrainGenome Genome { get; }
    public Brain Brain { get; }
    public BrainDebugSnapshot LastDebug { get; set; }

    public void Feed()
    {
        PendingReward += 0.35f;
        LastEvent = "received nutrient gel";
    }

    public void Comfort()
    {
        PendingReward += 0.12f;
        Brain.OfflineConsolidate(0.9f, 1f);
        LastEvent = "was comforted";
    }

    public void Dose(AddictiveStimulus stimulus, string label)
    {
        Brain.ApplyAddictiveStimulus(stimulus);
        PendingReward += stimulus.RewardSpike * 0.2f;
        LastEvent = $"sipped {label}";
    }

    public void Teach(int topicId)
    {
        Brain.TeachCulture(topicId, 0.75f, 0.8f);
        LastEvent = "learned a vending proverb";
    }
}

public sealed class WorldCommand
{
    public int BotId { get; set; }
    public string? Kind { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int TopicId { get; set; }
}

public sealed class WorldSnapshot
{
    public int Tick { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public CubeSnapshot[] Open { get; set; } = Array.Empty<CubeSnapshot>();
    public BotSnapshot[] Bots { get; set; } = Array.Empty<BotSnapshot>();
    public ItemSnapshot[] Items { get; set; } = Array.Empty<ItemSnapshot>();
    public NewsLine[] News { get; set; } = Array.Empty<NewsLine>();
}

public sealed class ItemSnapshot
{
    public int X { get; set; }
    public int Y { get; set; }
    public string Icon { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";

    public static ItemSnapshot From(WorldItem item)
    {
        return new ItemSnapshot { X = item.X, Y = item.Y, Icon = item.Icon, Name = item.Name, Kind = item.Kind.ToString() };
    }
}

public sealed class NewsLine
{
    public int Tick { get; set; }
    public string Text { get; set; } = "";
}

public sealed class CubeSnapshot
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool Open { get; set; }
}

public sealed class BotSnapshot
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int GroupId { get; set; }
    public int Age { get; set; }
    public bool Alive { get; set; }
    public string Event { get; set; } = "";
    public string Goal { get; set; } = "";
    public string Emotion { get; set; } = "";
    public string Personality { get; set; } = "";
    public float Energy { get; set; }
    public float Nutrition { get; set; }
    public float Integrity { get; set; }
    public float Social { get; set; }
    public float Craving { get; set; }
    public float Trauma { get; set; }
    public float Dopamine { get; set; }
    public float Cortisol { get; set; }
    public float Signal { get; set; }

    public static BotSnapshot From(BotBody bot)
    {
        var debug = bot.LastDebug;
        return new BotSnapshot
        {
            Id = bot.Id,
            Name = bot.Name,
            X = bot.X,
            Y = bot.Y,
            GroupId = bot.GroupId,
            Age = bot.Age,
            Alive = bot.Alive,
            Event = bot.LastEvent,
            Goal = debug.Goal.Primary.ToString(),
            Emotion = debug.Emotion.Primary.ToString(),
            Personality = debug.Personality.Style.ToString(),
            Energy = debug.Needs.Energy,
            Nutrition = debug.Needs.Nutrition,
            Integrity = debug.Needs.Integrity,
            Social = debug.Needs.Social,
            Craving = debug.Addiction.Craving,
            Trauma = debug.Trauma.Load,
            Dopamine = debug.Chemicals.Dopamine,
            Cortisol = debug.Chemicals.Cortisol,
            Signal = debug.Actions.Length > BrainChannels.Signal ? debug.Actions[BrainChannels.Signal] : 0f
        };
    }
}
