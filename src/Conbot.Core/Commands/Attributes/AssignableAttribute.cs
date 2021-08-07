using System.Threading.Tasks;

using Disqord;

using Qmmands;

namespace Conbot.Commands
{
    public class AssignableAttribute : ParameterCheckAttribute
    {
        public override ValueTask<CheckResult> CheckAsync(object argument, CommandContext context)
        {
            var role = (IRole)argument;

            if (role.IsManaged)
            {
                return CheckResult.Failed(
                    "Role must be assignable. You can't enter a role which is managed by an application.");
            }

            return CheckResult.Successful;
        }
    }
}
