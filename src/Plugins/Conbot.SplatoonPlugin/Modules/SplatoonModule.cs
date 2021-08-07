using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Conbot.Commands;
using Conbot.Extensions;
using Conbot.Interactive;
using Conbot.TimeZonePlugin;
using Conbot.TimeZonePlugin.Extensions;

using Disqord;
using Disqord.Bot;

using Humanizer;

using NodaTime;

using Qmmands;

namespace Conbot.SplatoonPlugin
{
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

            if (mode == "all")
                return await ShowAllSchedulesAsync(schedules, page - 1);
            else
                return await ShowScheduleAsync(schedules, mode);
        }

        public async Task<DiscordCommandResult> ShowAllSchedulesAsync(Schedules schedules, int startIndex = 0)
        {
            var now = DateTimeOffset.Now;

            var regularRotations = schedules.RegularRotations.Where(x => x.EndTime > now);
            var rankedRotations = schedules.RankedRotations.Where(x => x.EndTime > now);
            var leagueRotations = schedules.LeagueRotations.Where(x => x.EndTime > now);

            int count = regularRotations.Count();

            if (startIndex >= count || startIndex < 0)
                return Fail("This page doesn't exist.");

            var timezone = await Context.GetUserTimeZoneAsync() ?? DateTimeZone.Utc;
            var instant = SystemClock.Instance.GetCurrentInstant();

            var paginator = new Paginator();

            for (int i = 0; i < count; i++)
            {
                var regularRotation = regularRotations.ElementAt(i);
                var rankedRotation = rankedRotations.ElementAt(i);
                var leagueRotation = leagueRotations.ElementAt(i);

                var embed = CreateSchedulesEmbed(
                    regularRotation, rankedRotation, leagueRotation, timezone, instant, i + 1, count);
                paginator.AddPage(embed);
            }

            return Paginate(paginator, startIndex);
        }

        public LocalEmbed CreateSchedulesEmbed(Rotation regularRotation, Rotation rankedRotation,
            Rotation leagueRotation, DateTimeZone timezone, Instant instant, int page, int total)
        {
            var now = instant.ToDateTimeOffset();
            var zonedInstant = instant.InZone(timezone);

            var startTime = Instant.FromDateTimeOffset(regularRotation.StartTime).InZone(timezone);
            var endTime = Instant.FromDateTimeOffset(regularRotation.EndTime).InZone(timezone);

            var titleText = new StringBuilder();

            if (startTime.Date != zonedInstant.Date)
            {
                titleText
                    .Append(startTime.Date.ToString("dd/MM", CultureInfo.InvariantCulture))
                    .Append(" at ");
            }

            titleText
                .Append(startTime.TimeOfDay.ToString("HH:mm", CultureInfo.InvariantCulture))
                .Append(" – ");

            if (endTime.Date != startTime.Date)
            {
                titleText
                    .Append(endTime.Date.ToString("dd/MM", CultureInfo.InvariantCulture))
                    .Append(" at ");
            }

            titleText.Append(endTime.TimeOfDay.ToString("HH:mm", CultureInfo.InvariantCulture));

            return new LocalEmbed()
                .WithColor(new Color(0x19d719))
                .WithTitle(titleText.ToString())
                .WithDescription($"for {(regularRotation.EndTime - now).ToLongFormattedString()}")
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

        public async Task<DiscordCommandResult> ShowScheduleAsync(Schedules schedules, string mode = "regular",
            int startIndex = 0)
        {
            IEnumerable<Rotation> schedule;
            string title;
            string thumbnailUrl;
            string url;
            Color color;

            if (mode == "ranked")
            {
                schedule = schedules.RankedRotations;
                title = "Ranked Battle";
                url = $"{_config["Splatoon2InkBaseUrl"]}/schedules/ranked";
                thumbnailUrl = "https://splatoon2.ink/assets/img/battle-ranked.78832c.png";
                color = new Color(0xf54910);
            }
            else if (mode == "league")
            {
                schedule = schedules.LeagueRotations;
                title = "League Battle";
                url = $"{_config["Splatoon2InkBaseUrl"]}/schedules/league";
                thumbnailUrl = "https://splatoon2.ink/assets/img/battle-league.95f523.png";
                color = new Color(0xf02d7d);
            }
            else
            {
                schedule = schedules.RegularRotations;
                title = "Regular Battle";
                url = $"{_config["Splatoon2InkBaseUrl"]}/schedules/regular";
                thumbnailUrl = "https://splatoon2.ink/assets/img/battle-regular.01b5ef.png";
                color = new Color(0x19d719);
            }

            var timezone = await Context.GetUserTimeZoneAsync() ?? DateTimeZone.Utc;
            var instant = SystemClock.Instance.GetCurrentInstant();
            var now = instant.ToDateTimeOffset();
            var zonedInstant = instant.InZone(timezone);

            schedule = schedule.Where(x => x.EndTime > now);

            int count = schedule.Count();
            int currentPage = 1;
            int totalPages = (count / 4) + (count % 4 != 0 ? 1 : 0);

            if (startIndex >= totalPages || totalPages < 0)
                return Fail("This page doesn't exist.");

            var paginator = new Paginator();
            var embed = new LocalEmbed();

            for (int i = 0; i < count; i++)
            {
                var rotation = schedule.ElementAt(i);

                var startTime = Instant.FromDateTimeOffset(rotation.StartTime).InZone(timezone);
                var endTime = Instant.FromDateTimeOffset(rotation.EndTime).InZone(timezone);

                string name = $"{rotation.Rule.Name}";

                var valueText = new StringBuilder()
                    .Append("**")
                    .Append(rotation.MapA.Name)
                    .Append("** and **")
                    .Append(rotation.MapB.Name)
                    .AppendLine("**");

                if (rotation.StartTime <= now)
                {
                    valueText
                        .Append("for")
                        .Append(' ')
                        .AppendLine((rotation.EndTime - now).ToLongFormattedString());
                }
                else
                {
                    valueText
                        .Append("in")
                        .Append(' ')
                        .AppendLine((rotation.StartTime - now).ToLongFormattedString());
                }

                if (startTime.Date != zonedInstant.Date)
                {
                    valueText
                        .Append(startTime.Date.ToString("dd/MM", CultureInfo.InvariantCulture))
                        .Append(" at ");
                }

                valueText
                    .Append(startTime.TimeOfDay.ToString("HH:mm", CultureInfo.InvariantCulture))
                    .Append(" – ");

                if (endTime.Date != startTime.Date)
                {
                    valueText
                        .Append(endTime.Date.ToString("dd/MM", CultureInfo.InvariantCulture))
                        .Append(" at ");
                }

                valueText.Append(endTime.TimeOfDay.ToString("HH:mm", CultureInfo.InvariantCulture));

                embed.AddField(name, valueText.ToString());

                if ((i + 1) % 4 == 0 || i == count - 1)
                {
                    embed
                        .WithTitle(title)
                        .WithUrl(url)
                        .WithThumbnailUrl(thumbnailUrl)
                        .WithColor(color)
                        .WithFooter(
                            $"Splatoon 2 • Page {currentPage}/{totalPages} ({"entry".ToQuantity(count)})",
                            _config["EmbedFooterIconUrl"]);

                    currentPage++;
                    paginator.AddPage(embed);
                    embed = new LocalEmbed();
                }
            }

            return Paginate(paginator, startIndex);
        }

        [Command("salmonrun", "sr")]
        [Description("Shows the current Salmon Run schedule.")]
        public async Task<DiscordCommandResult> SalmonRunAsync()
        {
            var schedules = await _splatoonService.GetSalmonRunSchedulesAsync();

            if (schedules is null)
                return Reply("Currently there is no schedule available.");

            var timezone = await Context.GetUserTimeZoneAsync() ?? DateTimeZone.Utc;
            var instant = SystemClock.Instance.GetCurrentInstant();
            var now = instant.ToDateTimeOffset();

            var detailedRotations = schedules.DetailedRotations.Where(x => x.EndTime > now);
            var rotations =
                detailedRotations
                    .Concat(schedules.Rotations.Where(x => x.EndTime > now).Skip(detailedRotations.Count()))
                    .ToList();

            var embed = new LocalEmbed()
                .WithColor(new Color(0xff5600))
                .WithThumbnailUrl($"{_config["Splatoon2InkBaseUrl"]}/assets/img/mr-grizz.a87af8.png")
                .WithTitle("Salmon Run")
                .WithFooter("Splatoon 2", _config["EmbedFooterIconUrl"]);

            for (int i = 0; i < rotations.Count; i++)
            {
                var rotation = rotations[i];

                var startTime = Instant.FromDateTimeOffset(rotation.StartTime).InZone(timezone);
                var endTime = Instant.FromDateTimeOffset(rotation.EndTime).InZone(timezone);

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
                    .Append(startTime.Date.ToString("dd/MM", CultureInfo.InvariantCulture))
                    .Append(" at ")
                    .Append(startTime.TimeOfDay.ToString("HH:mm", CultureInfo.InvariantCulture))
                    .Append(" – ")
                    .Append(endTime.Date.ToString("dd/MM", CultureInfo.InvariantCulture))
                    .Append(" at ")
                    .Append(endTime.TimeOfDay.ToString("HH:mm", CultureInfo.InvariantCulture));

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
                        .AppendJoin(", ", rotation.Weapons.Select(x => x.Name))
                        .Append(')');
                }

                valueText.AppendLine();

                if (rotation.StartTime <= now)
                {
                    valueText
                        .Append("for")
                        .Append(' ')
                        .AppendLine((rotation.EndTime - now).ToLongFormattedString());
                }
                else
                {
                    valueText
                        .Append("in")
                        .Append(' ')
                        .AppendLine((rotation.StartTime - now).ToLongFormattedString());
                }

                embed.AddField(nameText.ToString(), valueText.ToString());
            }

            return Reply(embed);
        }

        [Command("shop", "gear")]
        [Description("Shows the currently available gear on the SplatNet Gear Shop.")]
        public async Task<DiscordCommandResult> SplatShopAsync()
        {
            var merchandises = await _splatoonService.GetMerchandisesAsync();

            if (merchandises?.Any() != true)
                return Reply("Currently there is no Splat Shop information available.");

            var now = DateTimeOffset.UtcNow;

            int count = merchandises.Count();
            var paginator = new Paginator();

            int gear = 1;

            foreach (var merchandise in merchandises)
            {
                string description = $"{(merchandise.EndTime - now).ToLongFormattedString()} left";
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

                if (merchandise.Kind == GearKind.Shoes)
                    embed.WithColor(0xe66900);
                else if (merchandise.Kind == GearKind.Clothes)
                    embed.WithColor(0x1dd6ad);
                else
                    embed.WithColor(new Color(0xee156e));

                paginator.AddPage(embed);
                gear++;
            }

            return Paginate(paginator);
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
                        StringComparison.InvariantCultureIgnoreCase)));

            var weapon = weapons.ElementAt(_random.Next(0, weapons.Count()));
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
}
