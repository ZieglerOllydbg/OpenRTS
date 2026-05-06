using System;

/// <summary>
/// Attribute used to mark methods as GM commands
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class GMCommandAttribute : Attribute
{
    public string CommandName { get; }

    public GMCommandAttribute(string commandName)
    {
        CommandName = commandName;
    }
}