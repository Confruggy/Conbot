using System.Linq;
using System.Threading.Tasks;

using Qmmands;

namespace Conbot.Commands;

public class UnsignedLongTypeParser : TypeParser<ulong>
{
    public override ValueTask<TypeParserResult<ulong>> ParseAsync(Parameter parameter, string value,
        CommandContext context)
    {
        if (parameter.Attributes.FirstOrDefault(x => x is SnowflakeAttribute)
            is SnowflakeAttribute attribute)
        {
            if (value.Length >= 15 && value.Length <= 21 && ulong.TryParse(value, out ulong id))
                return TypeParserResult<ulong>.Successful(id);

            string type = attribute.Type switch
            {
                SnowflakeType.Guild => "server ID",
                SnowflakeType.Channel => "channel ID",
                SnowflakeType.Message => "message ID",
                SnowflakeType.User => "user ID",
                _ => "ID"
            };

            return TypeParserResult<ulong>.Failed(
                $"Parameter **{parameter.Name}** must be a valid {type}.");
        }

        if (ulong.TryParse(value, out ulong output))
            return TypeParserResult<ulong>.Successful(output);

        return TypeParserResult<ulong>.Failed($"Parameter **{parameter.Name}** must be a valid integer.");
    }
}