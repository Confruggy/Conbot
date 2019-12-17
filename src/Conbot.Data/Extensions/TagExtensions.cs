using System;
using System.Threading.Tasks;
using Conbot.Data.Entities;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace Conbot.Data.Extensions
{
    public static class TagExtensions
    {
        public static Task<Tag> GetTagAsync(this ConbotContext context, int id)
            => context.Tags.FirstOrDefaultAsync(x => x.Id == id);

        public static Task<Tag> GetTagAsync(this ConbotContext context, IGuild guild, string name)
            => context.Tags.FirstOrDefaultAsync(x => x.GuildId == guild.Id &&
                x.Name.ToLower() == name.ToLower());

        public static async Task<Tag> CreateTagAsync(this ConbotContext context, ulong guildId, ulong channelId,
            ulong messageId, ulong userId, string name, string content)
        {
            var tagCreation = new TagCreation
            {
                GuildId = guildId,
                CreatedAt = DateTimeOffset.Now,
                ChannelId = channelId,
                MessageId = messageId,
                UserId = userId
            };

            var tag = new Tag
            {
                GuildId = guildId,
                Name = name,
                Content = content,
                OwnerId = userId,
                Creation = tagCreation
            };

            await context.Tags.AddAsync(tag);
            return tag;
        }

        public static Task<Tag> CreateTagAsync(this ConbotContext context, IMessage message, string name,
            string content)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return CreateTagAsync(context, guild.Id, message.Channel.Id, message.Id, message.Author.Id, name, content);
        }

        public static async Task<TagUse> AddTagUseAsync(this ConbotContext context, Tag tag, ulong guildId,
            ulong channelId, ulong messageId, ulong userId, TagAlias usedAlias = null)
        {
            var use = new TagUse
            {
                Tag = tag,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                UsedAlias = usedAlias,
                UserId = userId,
                UsedAt = DateTimeOffset.UtcNow
            };

            await context.TagUses.AddAsync(use);
            return use;
        }

        public static Task<TagUse> AddTagUseAsync(this ConbotContext context, Tag tag, IMessage message,
            TagAlias usedAlias = null)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return AddTagUseAsync(context, tag, guild.Id, message.Channel.Id, message.Id, message.Author.Id, usedAlias);
        }

        public static Task<TagAlias> GetTagAliasAsync(this ConbotContext context, IGuild guild, string name)
            => context.TagAliases.FirstOrDefaultAsync(x => x.GuildId == guild.Id && x.Name.ToLower() == name.ToLower());

        public static Task<TagAlias> CreateTagAliasAsync(this ConbotContext context, Tag tag, IMessage message,
            string name)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return CreateTagAliasAsync(context, tag, guild.Id, message.Channel.Id, message.Id, message.Author.Id, name);
        }

        public static async Task<TagAlias> CreateTagAliasAsync(this ConbotContext context, Tag tag, ulong guildId,
            ulong channelId, ulong messageId, ulong userId, string name)
        {
            var tagAliasCreation = new TagAliasCreation
            {
                CreatedAt = DateTimeOffset.Now,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                UserId = userId
            };

            var tagAlias = new TagAlias
            {
                Tag = tag,
                GuildId = tag.GuildId,
                Name = name,
                OwnerId = userId,
                Creation = tagAliasCreation
            };

            await context.TagAliases.AddAsync(tagAlias);

            return tagAlias;
        }
    }
}