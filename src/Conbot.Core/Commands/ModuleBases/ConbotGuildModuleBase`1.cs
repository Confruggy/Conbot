namespace Conbot.Commands
{
    [RequireGuild]
    public abstract class ConbotGuildModuleBase<TContext> : ConbotModuleBase<TContext>
        where TContext : ConbotGuildCommandContext
    {
    }
}
