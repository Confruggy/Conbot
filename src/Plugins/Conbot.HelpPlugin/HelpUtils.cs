using System.Linq;
using System.Text;

using Conbot.Commands;

using Disqord;

using Qmmands;

namespace Conbot.HelpPlugin;

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
                .Append(Markdown.Bold(name))
                .Append(" : ")
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

        if (parameter.IsMultiple)
            return $"[{name}] […]";

        if (parameter.IsRemainder && !parameter.IsOptional && !name.Contains('|'))
            return $"<{name}…>";

        if (parameter.IsRemainder && parameter.IsOptional && !name.Contains('|'))
            return $"[{name}…]";

        if (parameter.IsOptional)
            return $"[{name}]";

        return $"<{name}>";
    }

    public static string FormatParameters(Command command)
        => string.Join(" ", command.Parameters.Select(x => FormatParameter(x)));

    public static string GetShortModule(Module module)
        => $"{Markdown.Bold(module.Parent is not null ? $"{module.FullAliases[0]}*" : module.Name)}\n" +
           $"> {module.Description ?? "No Description."}";

    public static string GetShortCommand(Command command)
        => $"{Markdown.Bold(command.FullAliases[0])} {FormatParameters(command)}\n" +
           $"> {command.Description ?? "No Description."}";

    public static string GetPath(Module module)
    {
        string? alias = module.FullAliases.FirstOrDefault();

        if (string.IsNullOrEmpty(alias))
            return module.Name;

        var parent = module.Parent;

        while (parent?.Parent is not null)
            parent = module.Parent;

        return $"{parent?.Name ?? module.Name} › {string.Join(" › ", alias.Split(' '))}";
    }

    public static string GetPath(Command command)
    {
        var module = command.Module;

        while (module.Parent is not null)
            module = module.Parent;

        return $"{module.Name} › {command.FullAliases[0].Replace(" ", " › ")}";
    }
}