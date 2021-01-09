using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Discord;
using Discord.WebSocket;

namespace Conbot.TagPlugin
{
    public static class TagContextExtensions
    {
        public static IAsyncEnumerable<Tag> GetTagsAsync(this TagContext context,
            ulong? guildId = null, ulong? ownerId = null)
        {
            if (guildId == null && ownerId == null)
                return context.Tags.AsNoTracking().AsAsyncEnumerable();

            if (guildId == null)
                return context.Tags.AsNoTracking().Where(x => x.OwnerId == ownerId).AsAsyncEnumerable();

            if (ownerId == null)
                return context.Tags.AsNoTracking().Where(x => x.GuildId == guildId).AsAsyncEnumerable();

            return context.Tags.AsNoTracking().Where(x => x.GuildId == guildId && x.OwnerId == ownerId)
                .AsAsyncEnumerable();
        }

        public static IAsyncEnumerable<Tag> GetTagsAsync(this TagContext context,
            IGuild? guild = null, IUser? owner = null)
            => GetTagsAsync(context, guild?.Id, owner?.Id);

        public static async Task<Tag?> GetTagAsync(this TagContext context, int id)
            => await context.Tags.AsQueryable().FirstOrDefaultAsync(x => x.Id == id);

        public static async Task<Tag?> GetTagAsync(this TagContext context, IGuild guild, string name)
            => await context.Tags.AsAsyncEnumerable().FirstOrDefaultAsync(x => x.GuildId == guild.Id &&
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        public static async Task<Tag> CreateTagAsync(this TagContext context, ulong guildId, ulong channelId,
            ulong? messageId, ulong? interactionId, ulong userId, string name, string content)
        {
            var tag = new Tag(guildId, name, content, userId, userId, DateTime.UtcNow, guildId, channelId, messageId,
                interactionId);

            await context.Tags.AddAsync(tag);
            return tag;
        }

        public static Task<Tag> CreateTagAsync(this TagContext context, ITextChannel channel,
            IMessage message, string name,
            string content)
        => CreateTagAsync(context, channel.Guild.Id, message.Channel.Id, message.Id, null, message.Author.Id, name,
            content);

        public static Task<Tag> CreateTagAsync(this TagContext context, SocketInteraction interaction, string name,
            string content)
        => CreateTagAsync(context, interaction.Guild.Id, interaction.Channel.Id, null, interaction.Id,
            interaction.Member.Id, name, content);

        public static void RemoveTag(this TagContext context, Tag tag)
            => context.Tags.Remove(tag);

        public static async Task<TagUse> AddTagUseAsync(this TagContext context, Tag tag, ulong guildId,
            ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, TagAlias? usedAlias = null)
        {
            var use = new TagUse(tag, usedAlias, userId, DateTime.UtcNow, guildId, channelId, messageId, interactionId);

            await context.TagUses.AddAsync(use);
            return use;
        }

        public static Task<TagUse> AddTagUseAsync(this TagContext context, Tag tag, ITextChannel channel,
            IMessage message, TagAlias? usedAlias = null)
            => AddTagUseAsync(
                context, tag, channel.Guild.Id, message.Channel.Id, message.Id, null, message.Author.Id, usedAlias);

        public static Task<TagUse> AddTagUseAsync(this TagContext context, Tag tag, SocketInteraction interaction,
            TagAlias? usedAlias = null)
            => AddTagUseAsync(context, tag, interaction.Guild.Id, interaction.Channel.Id, null, interaction.Id,
                interaction.Member.Id, usedAlias);

        public static async Task<TagModification> ModifyTagAsync(this TagContext context, Tag tag, ulong guildId,
            ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, string newContent)
        {
            var modifcation = new TagModification(tag, newContent, tag.Content, userId, DateTime.UtcNow, guildId,
                channelId, messageId, interactionId);

            tag.Content = newContent;

            await context.TagModifications.AddAsync(modifcation);
            return modifcation;
        }

        public static Task<TagModification> ModifyTagAsync(this TagContext context, Tag tag,
            ITextChannel channel, IMessage message,
            string newContent)
            => ModifyTagAsync(context, tag, channel.Guild.Id, message.Channel.Id, message.Id, null, message.Author.Id,
                newContent);

        public static Task<TagModification> ModifyTagAsync(this TagContext context, Tag tag,
            SocketInteraction interaction, string newContent)
            => ModifyTagAsync(context, tag, interaction.Guild.Id, interaction.Channel.Id, null, interaction.Id,
                interaction.Member.Id, newContent);

        public static async Task<TagOwnerChange> ChangeTagOwnerAsync(this TagContext context, Tag tag,
            ulong guildId, ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, ulong newOwnerId,
            OwnerChangeType type)
        {
            var ownerChange = new TagOwnerChange(tag, newOwnerId, tag.OwnerId, type, userId, DateTime.UtcNow, guildId,
                channelId, messageId, interactionId);

            tag.OwnerId = newOwnerId;

            await context.TagOwnerChanges.AddAsync(ownerChange);
            return ownerChange;
        }

        public static Task<TagOwnerChange> ChangeTagOwnerAsync(this TagContext context, Tag tag,
            ITextChannel channel, IMessage message, ulong newOwnerId, OwnerChangeType type)
            => ChangeTagOwnerAsync(context, tag, channel.Guild.Id, message.Channel.Id, message.Id, null,
                message.Author.Id, newOwnerId, type);

        public static Task<TagOwnerChange> ChangeTagOwnerAsync(this TagContext context, Tag tag,
            SocketInteraction interaction, ulong newOwnerId, OwnerChangeType type)
            => ChangeTagOwnerAsync(context, tag, interaction.Guild.Id, interaction.Channel.Id, null, interaction.Id,
            interaction.Member.Id, newOwnerId, type);

        public static IAsyncEnumerable<TagAlias> GetTagAliasesAsync(this TagContext context, Tag tag)
            => context.TagAliases.AsNoTracking().Where(x => x.TagId == tag.Id).AsAsyncEnumerable();

        public static async Task<TagAlias?> GetTagAliasAsync(this TagContext context, IGuild guild, string name)
            => await context.TagAliases.AsAsyncEnumerable().FirstOrDefaultAsync(
                x => x.GuildId == guild.Id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        public static Task<TagAlias> CreateTagAliasAsync(this TagContext context, Tag tag, ITextChannel channel,
            IMessage message, string name)
        => CreateTagAliasAsync(
            context, tag, channel.Guild.Id, message.Channel.Id, message.Id, null, message.Author.Id, name);

        public static Task<TagAlias> CreateTagAliasAsync(this TagContext context, Tag tag,
            SocketInteraction interaction, string name)
        => CreateTagAliasAsync(context, tag, interaction.Guild.Id, interaction.Channel.Id, null, interaction.Id,
            interaction.Member.Id, name);

        public static async Task<TagAlias> CreateTagAliasAsync(this TagContext context, Tag tag, ulong guildId,
            ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, string name)
        {
            var tagAlias = new TagAlias(guildId, name, tag, userId, userId, DateTime.UtcNow, guildId, channelId,
                messageId, interactionId);

            await context.TagAliases.AddAsync(tagAlias);

            return tagAlias;
        }

        public static void RemoveTagAlias(this TagContext context, TagAlias alias)
            => context.TagAliases.Remove(alias);

        public static async Task<TagAliasOwnerChange> ChangeTagAliasOwnerAsync(this TagContext context,
            TagAlias alias, ulong guildId, ulong channelId, ulong? messageId, ulong? interactionId, ulong userId,
            ulong newOwnerId, OwnerChangeType type)
        {
            var ownerChange = new TagAliasOwnerChange(alias, alias.OwnerId, newOwnerId, type, userId, DateTime.UtcNow,
                guildId, channelId, messageId, interactionId);

            alias.OwnerId = newOwnerId;

            await context.TagAliasOwnerChanges.AddAsync(ownerChange);
            return ownerChange;
        }

        public static Task<TagAliasOwnerChange> ChangeTagAliasOwnerAsync(this TagContext context, TagAlias alias,
            ITextChannel channel, IMessage message, ulong newOwnerId, OwnerChangeType type)
            => ChangeTagAliasOwnerAsync(context, alias, channel.Guild.Id, message.Channel.Id, message.Id, null,
                message.Author.Id, newOwnerId, type);

        public static Task<TagAliasOwnerChange> ChangeTagAliasOwnerAsync(this TagContext context, TagAlias alias,
            SocketInteraction interaction, ulong newOwnerId, OwnerChangeType type)
            => ChangeTagAliasOwnerAsync(context, alias, interaction.Guild.Id, interaction.Channel.Id, null,
                interaction.Id, interaction.Member.Id, newOwnerId, type);
    }
}