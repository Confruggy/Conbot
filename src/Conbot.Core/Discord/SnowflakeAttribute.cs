using System;

namespace Conbot;

[AttributeUsage(AttributeTargets.Parameter)]
public class SnowflakeAttribute : Attribute
{
    public SnowflakeType Type { get; set; }

    public SnowflakeAttribute(SnowflakeType type)
        => Type = type;
}