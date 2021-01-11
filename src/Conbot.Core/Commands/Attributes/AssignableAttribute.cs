using System.Threading.Tasks;

using Discord;

using Qmmands;

namespace Conbot.Commands
{
    public class AssignableAttribute : ParameterCheckAttribute
    {
        private readonly RequireContextAttribute _requireContext;

        public AssignableAttribute() => _requireContext = new RequireContextAttribute(ContextType.Guild);

        public override async ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var role = (IRole)argument;

            var requireContextResult = await _requireContext.CheckAsync(context);
            if (!requireContextResult.IsSuccessful)
                return requireContextResult;

            if (role.IsManaged)
            {
                return CheckResult.Unsuccessful(
                    "Role must be assignable. You can't enter a role which is managed by an application.");
            }

            return CheckResult.Successful;
        }
    }
}
