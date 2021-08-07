using System.Threading.Tasks;

using Qmmands;

namespace Conbot.Commands
{
    public class IntegerTypeParser : TypeParser<int>
    {
        public override ValueTask<TypeParserResult<int>> ParseAsync(Parameter parameter, string value,
            CommandContext context)
        {
            if (int.TryParse(value, out int output))
                return TypeParserResult<int>.Successful(output);

            return TypeParserResult<int>.Failed($"Parameter **{parameter.Name}** must be a valid integer.");
        }
    }
}
