using System.Reflection;
using JetBrains.Annotations;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace Painter;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.emilanderss0n.painter";
    public string Name { get; init; } = "Painter";
    public string Author { get; init; } = "MoxoPixel";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("2.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; }
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
    {
        {"com.wtt.commonlib", new SemanticVersioning.Range("~3.0.0")}
    };
    public string? Url { get; init; } = "";
    public string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 2), UsedImplicitly]
public class Painter(
    ISptLogger<Painter> logger,
    ModHelper modHelper,
    ImageRouter imageRouter,
    TraderConfig traderConfig,
    RagfairConfig ragfairConfig,
    TimeUtil timeUtil,
    EpicTraderHelper traderHelper) : IOnLoad
{

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        
        // A path to the mods files we use below
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        // A relative path to the trader icon to show
        var traderImagePath = Path.Combine(pathToMod, "res/painter.jpg");

        // The base json containing trader settings we will add to the server
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");

        // Create a helper class and use it to register our traders image/icon + set its stock refresh time
        imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), traderImagePath);
        EpicTraderHelper.SetTraderUpdateTime(traderConfig, traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));

        // Add our trader to the config file, this lets it be seen by the flea market
        ragfairConfig.Traders.TryAdd(traderBase.Id, true);

        // Add our trader (with no items yet) to the server database
        // An 'assort' is the term used to describe the offers a trader sells, it has 3 parts to an assort
        // 1: The item
        // 2: The barter scheme, cost of the item (money or barter)
        // 3: The Loyalty level, what rep level is required to buy the item from trader
        traderHelper.AddTraderWithEmptyAssortToDb(traderBase);

        // Add localisation text for our trader to the database so it shows to people playing in different languages
        traderHelper.AddTraderToLocales(traderBase, "Painter", "Painter is a trader who sells unique weapon mods and gear. Also painted weapon parts (incl. whole weapons).");

        // Get the assort data from JSON
        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "db/assort.json");

        // Save the data we loaded above into the trader we've made
        traderHelper.OverwriteTraderAssort(traderBase.Id, assort);
        
        logger.Success("[Painter] Mod loaded successfully.");
        
        return Task.CompletedTask;
    }
}

[Injectable(TypePriority = OnLoadOrder.TraderRegistration + 3), UsedImplicitly]
public class PainterModQuest(WTTServerCommonLib.WTTServerCommonLib wttServerCommonLib) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var assembly = Assembly.GetExecutingAssembly();
        await wttServerCommonLib.CustomQuestService.CreateCustomQuests(assembly); 
        await Task.CompletedTask;
    }
}