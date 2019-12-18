using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Conbot.Data.Entities;
using Discord;
using Microsoft.EntityFrameworkCore;

namespace Conbot.Data.Extensions
{
    public static class TagExtensions
    {
        public static Task<List<Tag>> GetTagsAsync(this ConbotContext context,
            ulong? guildId = null, ulong? ownerId = null)
        {
            if (guildId == null && ownerId == null)
                return context.Tags.ToListAsync();
            if (guildId == null)
                return context.Tags.Where(x => x.OwnerId == ownerId).ToListAsync();
            if (ownerId == null)
                return context.Tags.Where(x => x.GuildId == guildId).ToListAsync();
            return context.Tags.Where(x => x.GuildId == guildId && x.OwnerId == ownerId).ToListAsync();
        }

        public static Task<List<Tag>> GetTagsAsync(this ConbotContext context,
            IGuild guild = null, IUser owner = null)
            => GetTagsAsync(context, guild?.Id, owner?.Id);

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

        public static void RemoveTag(this ConbotContext context, Tag tag)
            => context.Tags.Remove(tag);

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

        public static async Task<TagModification> ModifyTagAsync(this ConbotContext context, Tag tag, ulong guildId,
            ulong channelId, ulong messageId, ulong userId, string newContent)
        {
            var modifcation = new TagModification
            {
                Tag = tag,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                UserId = userId,
                OldContent = tag.Content,
                NewContent = newContent,
                ModifiedAt = DateTime.UtcNow
            };

            tag.Content = newContent;

            await context.TagModifications.AddAsync(modifcation);
            return modifcation;
        }

        public static Task<TagModification> ModifyTagAsync(this ConbotContext context, Tag tag, IMessage message,
            string newContent)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return ModifyTagAsync(context, tag, guild.Id, message.Channel.Id, message.Id, message.Author.Id,
                newContent);
        }

        public static Task<List<TagAlias>> GetTagAliasesAsync(this ConbotContext context, Tag tag)
            => context.TagAliases.Where(x => x.TagId == tag.Id).ToListAsync();

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

        public static void RemoveTagAlias(this ConbotContext context, TagAlias alias)
            => context.TagAliases.Remove(alias);
    }
}