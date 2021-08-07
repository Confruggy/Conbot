using System.Linq;
using System.Text;

using Conbot.Commands;

using Disqord;

using Qmmands;

namespace Conbot.HelpPlugin
{
    public static class HelpUtils
    {
        public static string FormatParameter(Parameter parameter, bool literal = false)
        {
            string name;
            if (parameter.Checks.FirstOrDefault(x => x is ChoicesAttribute)
                is ChoicesAttribute optionsAttribute)
            {
                name = string.Join('|', optionsAttribute.Choices);
            }
            else
            {
                name = parameter.Name;
            }

            if (literal)
            {
                string type;

                if (parameter.Type.IsAssignableFrom(typeof(bool)))
                {
                    type = "Boolean";
                }
                else if (parameter.Type.IsAssignableFrom(typeof(int)))
                {
                    type = "Integer";
                }
                else if (parameter.Type.IsAssignableFrom(typeof(ulong)))
                {
                    if (parameter.Attributes.FirstOrDefault(x => x is SnowflakeAttribute)
                        is SnowflakeAttribute attribute)
                    {
                        type = attribute.Type switch
                        {
                            SnowflakeType.Guild => "Server ID",
                            SnowflakeType.Channel => "Channel ID",
                            SnowflakeType.Message => "Message ID",
                            SnowflakeType.User => "User ID",
                            _ => "ID"
                        };
                    }
                    else
                    {
                        type = "Integer";
                    }
                }
                else if (typeof(IUser).IsAssignableFrom(parameter.Type))
                {
                    type = "User";
                }
                else if (typeof(IChannel).IsAssignableFrom(parameter.Type))
                {
                    type = "Channel";
                }
                else if (typeof(IRole).IsAssignableFrom(parameter.Type))
                {
                    type = "Role";
                }
                else
                {
                    type = "Text";
                }

                var text = new StringBuilder()
                    .Append("**")
                    .Append(name)
                    .Append("** : ")
                    .Append(type)
                    .Append(" (*");

                if (parameter.IsMultiple)
                    text.Append("multiple");
                else if (parameter.IsOptional)
                    text.Append("optional");
                else
                    text.Append("required");

                if (parameter.IsRemainder)
                    text.Append(", remainder");

                text.Append("*)");
                return text.ToString();
            }
            else
            {
                if (parameter.IsMultiple)
                    return $"[{name}] […]";
                else if (parameter.IsRemainder && !parameter.IsOptional && !name.Contains('|'))
                    return $"<{name}…>";
                else if (parameter.IsRemainder && parameter.IsOptional && !name.Contains('|'))
                    return $"[{name}…]";
                else if (parameter.IsOptional)
                    return $"[{name}]";
                else
                    return $"<{name}>";
            }
        }

        public static string FormatParameters(Command command)
            => string.Join(" ", command.Parameters.Select(x => FormatParameter(x)));
    }
}
