using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;

using Disqord;
using Disqord.Bot;

using Humanizer;

using Qmmands;

using Disqord.Extensions.Interactivity.Menus.Paged;

namespace Conbot.SplatoonPlugin;

[Name("Splatoon 2")]
[Group("splatoon", "splat", "sploon")]
[Description("Shows the Splatoon 2 rotation.")]
[Remarks("Powered by [Splatoon2.ink](https://splatoon2.ink/).")]
public class SplatoonModule : ConbotModuleBase
{
    private readonly SplatoonService _splatoonService;
    private readonly Random _random;
    private readonly IConfiguration _config;

    public SplatoonModule(SplatoonService service, Random random, IConfiguration configuration)
    {
        _splatoonService = service;
        _random = random;
        _config = configuration.GetSection("SplatoonPlugin");
    }

    [Command("schedule", "stages", "rotations")]
    [Description("Shows the current map schedule.")]
    public async Task<DiscordCommandResult> ScheduleAsync(
        [Description("The mode to show the schedule of.")]
        [Choices("regular", "ranked", "league", "all")]
        string mode = "all",
        [Description("The page to start with.")]
        int page = 1)
    {
        var schedules = await _splatoonService.GetSchedulesAsync();

        if (schedules is null)
            return Reply("Currently there is no schedule available.");

        return mode == "all"
            ? ShowAllSchedules(schedules, page - 1)
            : ShowSchedule(schedules, mode);
    }

    public DiscordCommandResult ShowAllSchedules(Schedules schedules, int startIndex = 0)
    {
        var now = DateTimeOffset.Now;

        var regularRotations = schedules.RegularRotations.Where(x => x.EndTime > now).ToArray();
        var rankedRotations = schedules.RankedRotations.Where(x => x.EndTime > now).ToArray();
        var leagueRotations = schedules.LeagueRotations.Where(x => x.EndTime > now).ToArray();

        int count = regularRotations.Length;

        if (startIndex >= count || startIndex < 0)
            return Fail("This page doesn't exist.");

        List<Page> pages = new();

        for (int i = 0; i < count; i++)
        {
            var regularRotation = regularRotations[i];
            var rankedRotation = rankedRotations[i];
            var leagueRotation = leagueRotations[i];

            var embed = CreateSchedulesEmbed(regularRotation, rankedRotation, leagueRotation, i + 1, count);
            pages.Add(new Page().WithEmbeds(embed));
        }

        return Paginate(pages, startIndex: startIndex);
    }

    public LocalEmbed CreateSchedulesEmbed(Rotation regularRotation, Rotation rankedRotation,
        Rotation leagueRotation, int page, int total)
    {
        var titleText = new StringBuilder()
            .Append(Markdown.Timestamp(regularRotation.StartTime))
            .Append(" – ")
            .Append(Markdown.Timestamp(regularRotation.EndTime));

        var description = new StringBuilder();

        if (page == 1)
            description.Append("ends");
        else
            description.Append("starts");

        description
            .Append(' ')
            .Append(Markdown.Timestamp(
                page == 1 ? regularRotation.EndTime : regularRotation.StartTime,
                Markdown.TimestampFormat.RelativeTime));

        return new LocalEmbed()
            .WithColor(new Color(0x19d719))
            .WithTitle(titleText.ToString())
            .WithDescription(description.ToString())
            .AddField(
                $"<:{_config["Emotes:Modes:Regular"]}> {regularRotation.Mode.Name}: {regularRotation.Rule.Name}",
                $"**{regularRotation.MapA.Name}** and **{regularRotation.MapB.Name}**")
            .AddField(
                $"<:{_config["Emotes:Modes:Ranked"]}> {rankedRotation.Mode.Name}: {rankedRotation.Rule.Name}",
                $"**{rankedRotation.MapA.Name}** and **{rankedRotation.MapB.Name}**")
            .AddField(
                $"<:{_config["Emotes:Modes:League"]}> {leagueRotation.Mode.Name}: {leagueRotation.Rule.Name}",
                $"**{leagueRotation.MapA.Name}** and **{leagueRotation.MapB.Name}**")
            .WithFooter(
                $"Splatoon 2 • Page {page}/{total} ({"entry".ToQuantity(total)})",
                _config["EmbedFooterIconUrl"]);
    }

    public DiscordCommandResult ShowSchedule(Schedules schedules, string mode = "regular", int startIndex = 0)
    {
        Rotation[] schedule;
        string title;
        string thumbnailUrl;
        string url;
        Color color;

        switch (mode)
        {
            case "ranked":
                schedule = schedules.RankedRotations.ToArray();
                title = "Ranked Battle";
                url = $"{_config["Splatoon2InkBaseUrl"]}/schedules/ranked";
                thumbnailUrl = "https://splatoon2.ink/assets/img/battle-ranked.78832c.png";
                color = new Color(0xf54910);
                break;
            case "league":
                schedule = schedules.LeagueRotations.ToArray();
                title = "League Battle";
                url = $"{_config["Splatoon2InkBaseUrl"]}/schedules/league";
                thumbnailUrl = "https://splatoon2.ink/assets/img/battle-league.95f523.png";
                color = new Color(0xf02d7d);
                break;
            default:
                schedule = schedules.RegularRotations.ToArray();
                title = "Regular Battle";
                url = $"{_config["Splatoon2InkBaseUrl"]}/schedules/regular";
                thumbnailUrl = "https://splatoon2.ink/assets/img/battle-regular.01b5ef.png";
                color = new Color(0x19d719);
                break;
        }

        schedule = schedule.Where(x => x.EndTime > DateTimeOffset.UtcNow).ToArray();

        int count = schedule.Length;
        int currentPage = 1;
        int totalPages = (count / 4) + (count % 4 != 0 ? 1 : 0);

        if (startIndex >= totalPages || startIndex < 0)
            return Fail("This page doesn't exist.");

        List<Page> pages = new();
        var embed = new LocalEmbed();

        for (int i = 0; i < count; i++)
        {
            var rotation = schedule[i];

            string name = $"{rotation.Rule.Name}";

            var valueText = new StringBuilder()
                .Append("**")
                .Append(rotation.MapA.Name)
                .Append("** and **")
                .Append(rotation.MapB.Name)
                .AppendLine("**");

            valueText.Append(i == 0 ? "ends" : "starts");

            valueText
                .Append(' ')
                .AppendLine(Markdown.Timestamp(
                    i == 0 ? rotation.EndTime : rotation.StartTime,
                    Markdown.TimestampFormat.RelativeTime));

            valueText
                .Append(Markdown.Timestamp(rotation.StartTime))
                .Append(" – ")
                .Append(Markdown.Timestamp(rotation.EndTime));

            embed.AddField(name, valueText.ToString());

            if ((i + 1) % 4 != 0 && i != count - 1)
                continue;

            embed
                .WithTitle(title)
                .WithUrl(url)
                .WithThumbnailUrl(thumbnailUrl)
                .WithColor(color)
                .WithFooter(
                    $"Splatoon 2 • Page {currentPage}/{totalPages} ({"entry".ToQuantity(count)})",
                    _config["EmbedFooterIconUrl"]);

            currentPage++;
            pages.Add(new Page().WithEmbeds(embed));
            embed = new LocalEmbed();
        }

        return Paginate(pages, startIndex: startIndex);
    }

    [Command("salmonrun", "sr")]
    [Description("Shows the current Salmon Run schedule.")]
    public async Task<DiscordCommandResult> SalmonRunAsync()
    {
        var schedules = await _splatoonService.GetSalmonRunSchedulesAsync();

        if (schedules is null)
            return Reply("Currently there is no schedule available.");

        var now = DateTimeOffset.UtcNow;

        var detailedRotations = schedules.DetailedRotations.Where(x => x.EndTime > now).ToArray();
        var rotations =
            detailedRotations
                .Concat(schedules.Rotations.Where(x => x.EndTime > now)
                    .Skip(detailedRotations.Length))
                .ToArray();

        var embed = new LocalEmbed()
            .WithColor(new Color(0xff5600))
            .WithThumbnailUrl($"{_config["Splatoon2InkBaseUrl"]}/assets/img/mr-grizz.a87af8.png")
            .WithTitle("Salmon Run")
            .WithFooter("Splatoon 2", _config["EmbedFooterIconUrl"]);

        for (int i = 0; i < rotations.Length; i++)
        {
            var rotation = rotations[i];

            var nameText = new StringBuilder();

            if (i == 0)
            {
                nameText
                    .Append(rotation.StartTime > now ? "Next" : "Open!")
                    .Append(' ');
            }
            else if (i == 1 && rotations[0].StartTime <= now)
            {
                nameText.Append("Next ");
            }

            nameText
                .Append("<:")
                .Append(_config["Emotes:SalmonRun"])
                .Append("> ")
                .Append(Markdown.Timestamp(rotation.StartTime))
                .Append(" – ")
                .Append(Markdown.Timestamp(rotation.EndTime));

            var valueText = new StringBuilder();

            if (rotation.Map is not null)
            {
                valueText
                    .Append("**")
                    .Append(rotation.Map.Name)
                    .Append("**");
            }

            if (rotation.Weapons.Any())
            {
                valueText
                    .Append(" (")
                    .AppendJoin(", ", rotation.Weapons.Select(x => x.Name ?? "Random"))
                    .Append(')');
            }

            valueText.AppendLine();

            valueText.Append(i == 0 ? "ends" : "starts");

            valueText
                .Append(' ')
                .AppendLine(Markdown.Timestamp(
                    i == 0 ? rotation.EndTime : rotation.StartTime,
                    Markdown.TimestampFormat.RelativeTime));

            embed.AddField(nameText.ToString(), valueText.ToString());
        }

        return Reply(embed);
    }

    [Command("shop", "gear")]
    [Description("Shows the currently available gear on the SplatNet Gear Shop.")]
    public async Task<DiscordCommandResult> SplatShopAsync()
    {
        var merchandises = (await _splatoonService.GetMerchandisesAsync())?.ToArray();

        if (merchandises?.Any() != true)
            return Reply("Currently there is no Splat Shop information available.");

        int count = merchandises.Length;
        List<Page> pages = new();

        int gear = 1;

        foreach (var merchandise in merchandises)
        {
            string description =
                $"ends {Markdown.Timestamp(merchandise.EndTime, Markdown.TimestampFormat.RelativeTime)}";
            string priceText = $"<:{_config["Emotes:SplatShop:Money"]}> {merchandise.Price}";

            var slotsText = new StringBuilder()
                .Append("<:")
                .Append(_config[$"Emotes:Skills:{merchandise.Skill.EmoteName}"])
                .Append(">｜");

            for (int i = 0; i < merchandise.Gear.Rarity + 1; i++)
            {
                slotsText
                    .Append("<:")
                    .Append(_config["Emotes:Skills:Locked"])
                    .Append('>');
            }

            string brandText = new StringBuilder()
                .Append("<:")
                .Append(_config[$"Emotes:Brands:{merchandise.Gear.Brand.EmoteName}"])
                .Append("> ")
                .Append(merchandise.Gear.Brand.Name)
                .ToString();

            string originalPriceText =
                $"<:{_config["Emotes:SplatShop:Money"]}> {merchandise.OriginalGear.Price?.ToString() ?? "n/a"}";

            var originalSlotsText = new StringBuilder()
                .Append("<:")
                .Append(_config[$"Emotes:Skills:{merchandise.OriginalGear.Skill.EmoteName}"])
                .Append(">｜");

            for (int i = 0; i < merchandise.OriginalGear.Rarity + 1; i++)
            {
                originalSlotsText
                    .Append("<:")
                    .Append(_config["Emotes:Skills:Locked"])
                    .Append('>');
            }

            var skill = merchandise.Gear.Brand.FrequentSkill;
            string commonAbilityText = $"<:{_config[$"Emotes:Skills:{skill.EmoteName}"]}> {skill.Name}";

            var embed = new LocalEmbed()
                .WithTitle(merchandise.Gear.Name)
                .WithUrl($"https://splatoonwiki.org/wiki/{merchandise.Gear.Name.Replace(' ', '_')}")
                .WithDescription(description)
                .WithThumbnailUrl(merchandise.Gear.ImageUrl)
                .AddField("Price", priceText, true)
                .AddField("Slots", slotsText.ToString(), true)
                .AddField("Brand", brandText, true)
                .AddField("Original Price", originalPriceText, true)
                .AddField("Original Slots", originalSlotsText.ToString(), true)
                .AddField("Common Ability", commonAbilityText, true)
                .WithFooter($"Splatoon 2 • Gear {gear}/{count}", _config["EmbedFooterIconUrl"]);

            switch (merchandise.Kind)
            {
                case GearKind.Shoes:
                    embed.WithColor(0xe66900);
                    break;
                case GearKind.Clothes:
                    embed.WithColor(0x1dd6ad);
                    break;
                case GearKind.Head:
                default:
                    embed.WithColor(new Color(0xee156e));
                    break;
            }

            pages.Add(new Page().WithEmbeds(embed));
            gear++;
        }

        return Paginate(pages);
    }

    [Command("pickweapon")]
    [Description("Picks a random weapon.")]
    public DiscordCommandResult PickWeapon(
        [Description("The category of the weapons to pick from.")]
        [Choices("shooters", "rollers", "chargers", "sloshers", "splatlings", "dualies", "brellas", "all")]
        string category = "all",
        [Name("include reskins")]
        [Description("Whether to include reskinned weapons.")]
        [Choices("yes", "no")]
        string includeReskins = "no")
    {
        var weapons = _splatoonService.Weapons
            .Where(x =>
                (includeReskins != "no" || x.ReskinOf is null) &&
                (category == "all" || string.Equals(x.Type.Category.Name,
                    category,
                    StringComparison.InvariantCultureIgnoreCase)))
            .ToArray();

        var weapon = weapons[_random.Next(0, weapons.Length)];
        var subWeapon = weapon.SubWeapon;
        var special = weapon.Special;

        var embed = new LocalEmbed()
            .WithColor(new Color(0xf02d7d))
            .WithTitle(weapon.Name)
            .WithUrl($"https://splatoonwiki.org/wiki/{weapon.Name.Replace(' ', '_')}")
            .AddField(
                "Sub",
                $"<:{_config[$"Emotes:SubWeapons:{subWeapon.EmoteName}"]}> {subWeapon.Name}",
                true)
            .AddField(
                "Special",
                $"<:{_config[$"Emotes:Specials:{special.EmoteName}"]}> {special.Name}",
                true)
            .WithFooter("Splatoon 2", _config["EmbedFooterIconUrl"]);

        return Reply($"You picked **{weapon.Name}**!", embed);
    }
}
