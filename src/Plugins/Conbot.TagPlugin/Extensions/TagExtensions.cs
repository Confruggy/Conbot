using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Conbot.TagPlugin
{
    public static class TagContextExtensions
    {
        public static ValueTask<List<Tag>> GetTagsAsync(this TagContext context,
            ulong? guildId = null, ulong? ownerId = null)
        {
            if (guildId == null && ownerId == null)
                return context.Tags.ToListAsync();
            if (guildId == null)
                return context.Tags.AsAsyncEnumerable().Where(x => x.OwnerId == ownerId).ToListAsync();
            if (ownerId == null)
                return context.Tags.AsAsyncEnumerable().Where(x => x.GuildId == guildId).ToListAsync();
            return context.Tags.AsAsyncEnumerable().Where(x => x.GuildId == guildId && x.OwnerId == ownerId)
                .ToListAsync();
        }

        public static ValueTask<List<Tag>> GetTagsAsync(this TagContext context,
            IGuild guild = null, IUser owner = null)
            => GetTagsAsync(context, guild?.Id, owner?.Id);

        public static ValueTask<Tag> GetTagAsync(this TagContext context, int id)
            => context.Tags.FirstOrDefaultAsync(x => x.Id == id);

        public static ValueTask<Tag> GetTagAsync(this TagContext context, IGuild guild, string name)
            => context.Tags.FirstOrDefaultAsync(x => x.GuildId == guild.Id &&
                string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        public static async ValueTask<Tag> CreateTagAsync(this TagContext context, ulong guildId, ulong channelId,
            ulong? messageId, ulong? interactionId, ulong userId, string name, string content)
        {
            var tag = new Tag
            {
                GuildId = guildId,
                Name = name,
                Content = content,
                OwnerId = userId,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                CreationGuildId = guildId,
                CreationChannelId = channelId,
                CreationMessageId = messageId,
                CreationInteractionId = interactionId
            };

            await context.Tags.AddAsync(tag);
            return tag;
        }

        public static ValueTask<Tag> CreateTagAsync(this TagContext context, IMessage message, string name,
            string content)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return CreateTagAsync(
                context, guild.Id, message.Channel.Id, message.Id, null, message.Author.Id, name, content);
        }

        public static ValueTask<Tag> CreateTagAsync(this TagContext context, SocketInteraction interaction, string name,
            string content)
        {
            var guild = (interaction.Channel as ITextChannel)?.Guild;
            return CreateTagAsync(
                context, guild.Id, interaction.Channel.Id, null, interaction.Id, interaction.Member.Id, name, content);
        }

        public static void RemoveTag(this TagContext context, Tag tag)
            => context.Tags.Remove(tag);

        public static async ValueTask<TagUse> AddTagUseAsync(this TagContext context, Tag tag, ulong guildId,
            ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, TagAlias usedAlias = null)
        {
            var use = new TagUse
            {
                Tag = tag,
                UsedAlias = usedAlias,
                UserId = userId,
                UsedAt = DateTime.UtcNow,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                InteractionId = interactionId
            };

            await context.TagUses.AddAsync(use);
            return use;
        }

        public static ValueTask<TagUse> AddTagUseAsync(this TagContext context, Tag tag, IMessage message,
            TagAlias usedAlias = null)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return AddTagUseAsync(
                context, tag, guild.Id, message.Channel.Id, message.Id, null, message.Author.Id, usedAlias);
        }

        public static ValueTask<TagUse> AddTagUseAsync(this TagContext context, Tag tag, SocketInteraction interaction,
            TagAlias usedAlias = null)
        {
            var guild = (interaction.Channel as ITextChannel)?.Guild;
            return AddTagUseAsync(
                context, tag, guild.Id, interaction.Channel.Id, null, interaction.Id, interaction.Member.Id, usedAlias);
        }

        public static async ValueTask<TagModification> ModifyTagAsync(this TagContext context, Tag tag, ulong guildId,
            ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, string newContent)
        {
            var modifcation = new TagModification
            {
                Tag = tag,
                OldContent = tag.Content,
                NewContent = newContent,
                UserId = userId,
                ModifiedAt = DateTime.UtcNow,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                InteractionId = interactionId
            };

            tag.Content = newContent;

            await context.TagModifications.AddAsync(modifcation);
            return modifcation;
        }

        public static ValueTask<TagModification> ModifyTagAsync(this TagContext context, Tag tag, IMessage message,
            string newContent)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return ModifyTagAsync(context, tag, guild.Id, message.Channel.Id, message.Id, null, message.Author.Id,
                newContent);
        }

        public static ValueTask<TagModification> ModifyTagAsync(this TagContext context, Tag tag,
            SocketInteraction interaction, string newContent)
        {
            var guild = (interaction.Channel as ITextChannel)?.Guild;
            return ModifyTagAsync(context, tag, guild.Id, interaction.Channel.Id, null, interaction.Id,
                interaction.Member.Id, newContent);
        }

        public static async ValueTask<TagOwnerChange> ChangeTagOwnerAsync(this TagContext context, Tag tag, ulong guildId,
            ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, ulong newOwnerId,
            OwnerChangeType type)
        {
            var ownerChange = new TagOwnerChange
            {
                Tag = tag,
                OldOwnerId = tag.OwnerId,
                NewOwnerId = newOwnerId,
                UserId = userId,
                Type = type,
                ChangedAt = DateTime.UtcNow,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                InteractionId = interactionId
            };

            tag.OwnerId = newOwnerId;

            await context.TagOwnerChanges.AddAsync(ownerChange);
            return ownerChange;
        }

        public static ValueTask<TagOwnerChange> ChangeTagOwnerAsync(this TagContext context, Tag tag, IMessage message,
            ulong newOwnerId, OwnerChangeType type)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return ChangeTagOwnerAsync(context, tag, guild.Id, message.Channel.Id, message.Id, null, message.Author.Id,
                newOwnerId, type);
        }

        public static ValueTask<TagOwnerChange> ChangeTagOwnerAsync(this TagContext context, Tag tag,
            SocketInteraction interaction, ulong newOwnerId, OwnerChangeType type)
        {
            var guild = (interaction.Channel as ITextChannel)?.Guild;
            return ChangeTagOwnerAsync(context, tag, guild.Id, interaction.Channel.Id, null, interaction.Id,
                interaction.Member.Id, newOwnerId, type);
        }

        public static ValueTask<List<TagAlias>> GetTagAliasesAsync(this TagContext context, Tag tag)
            => context.TagAliases.AsAsyncEnumerable().Where(x => x.TagId == tag.Id).ToListAsync();

        public static ValueTask<TagAlias> GetTagAliasAsync(this TagContext context, IGuild guild, string name)
            => context.TagAliases.FirstOrDefaultAsync(
                x => x.GuildId == guild.Id && string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        public static ValueTask<TagAlias> CreateTagAliasAsync(this TagContext context, Tag tag, IMessage message,
            string name)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return CreateTagAliasAsync(
                context, tag, guild.Id, message.Channel.Id, message.Id, null, message.Author.Id, name);
        }

        public static ValueTask<TagAlias> CreateTagAliasAsync(this TagContext context, Tag tag,
            SocketInteraction interaction, string name)
        {
            var guild = (interaction.Channel as ITextChannel)?.Guild;
            return CreateTagAliasAsync(
                context, tag, guild.Id, interaction.Channel.Id, null, interaction.Id, interaction.Member.Id, name);
        }

        public static async ValueTask<TagAlias> CreateTagAliasAsync(this TagContext context, Tag tag, ulong guildId,
            ulong channelId, ulong? messageId, ulong? interactionId, ulong userId, string name)
        {
            var tagAlias = new TagAlias
            {
                Tag = tag,
                GuildId = tag.GuildId,
                Name = name,
                OwnerId = userId,
                CreatorId = userId,
                CreatedAt = DateTime.UtcNow,
                CreationGuildId = guildId,
                CreationChannelId = channelId,
                CreationMessageId = messageId,
                CreationInteractionId = interactionId
            };

            await context.TagAliases.AddAsync(tagAlias);

            return tagAlias;
        }

        public static void RemoveTagAlias(this TagContext context, TagAlias alias)
            => context.TagAliases.Remove(alias);

        public static async ValueTask<TagAliasOwnerChange> ChangeTagAliasOwnerAsync(this TagContext context,
            TagAlias alias, ulong guildId, ulong channelId, ulong? messageId, ulong? interactionId, ulong userId,
            ulong newOwnerId, OwnerChangeType type)
        {
            var ownerChange = new TagAliasOwnerChange
            {
                TagAlias = alias,
                OldOwnerId = alias.OwnerId,
                NewOwnerId = newOwnerId,
                UserId = userId,
                Type = type,
                ChangedAt = DateTime.UtcNow,
                GuildId = guildId,
                ChannelId = channelId,
                MessageId = messageId,
                InteractionId = interactionId
            };

            alias.OwnerId = newOwnerId;

            await context.TagAliasOwnerChanges.AddAsync(ownerChange);
            return ownerChange;
        }

        public static ValueTask<TagAliasOwnerChange> ChangeTagAliasOwnerAsync(this TagContext context, TagAlias alias,
            IMessage message, ulong newOwnerId, OwnerChangeType type)
        {
            var guild = (message.Channel as ITextChannel)?.Guild;
            return ChangeTagAliasOwnerAsync(context, alias, guild.Id, message.Channel.Id, message.Id, null,
                message.Author.Id, newOwnerId, type);
        }

        public static ValueTask<TagAliasOwnerChange> ChangeTagAliasOwnerAsync(this TagContext context, TagAlias alias,
            SocketInteraction interaction, ulong newOwnerId, OwnerChangeType type)
        {
            var guild = (interaction.Channel as ITextChannel)?.Guild;
            return ChangeTagAliasOwnerAsync(context, alias, guild.Id, interaction.Channel.Id, null, interaction.Id,
                interaction.Member.Id, newOwnerId, type);
        }
    }
}