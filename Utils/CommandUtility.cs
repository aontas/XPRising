using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx.Logging;
using VampireCommandFramework;
using XPRising.Systems;
using LogSystem = XPRising.Plugin.LogSystem;

namespace XPRising.Utils;

public static class CommandUtility
{
    private static readonly List<Type> LoadedCommandTypes = new();
    
    public class PermissionMiddleware : CommandMiddleware
    {
        public override bool CanExecute(
            ICommandContext ctx,
            CommandAttribute command,
            MethodInfo method)
        {
            var type = method.DeclaringType;
            var groupName = type?.GetCustomAttribute<CommandGroupAttribute>()?.Name ?? "";
            var permissionKey = CommandAttributesToPermissionKey(groupName, command.Name, RequiredArgumentCount(method.GetParameters()));

            if (!Database.CommandPermission.TryGetValue(permissionKey, out var requiredPrivilege))
            {
                // If it doesn't exist it may be a command belonging to a different mod.
                // As far as we know, it should have permission.
                return true;
            }
                
            var steamId = PlayerCache.GetSteamIDFromName(ctx.Name);
            var userPrivilege = Database.UserPermission.GetValueOrDefault(steamId, PermissionSystem.LowestPrivilege);

            // If the user privilege is equal or greater to the required privilege, then they have permission
            if (userPrivilege >= requiredPrivilege) return true;
            
            ctx.Reply($"<color={Color.Red}>[permission denied]</color> {permissionKey}");
            return false;
        }
    }

    public struct Command(
        string permissionKey,
        string name,
        string shortHand,
        string usage,
        string description,
        bool isAdmin,
        int privilegeLevel)
    {
        public string PermissionKey = permissionKey;
        public string Name = name;
        public string ShortHand = shortHand;
        public string Usage = usage;
        public string Description = description;
        public bool IsAdmin = isAdmin;
        public int PrivilegeLevel = privilegeLevel;
    }

    private static string CommandAttributesToPermissionKey(string groupName, string commandName, int argCount)
    {
        if (string.IsNullOrEmpty(commandName)) return "";
        var argCountString = argCount == 0 ? "" : $"[{argCount}]";
        return string.Join(" ", new[] { groupName, commandName, argCountString }.Where(s => !string.IsNullOrEmpty(s)));
    }

    private static int RequiredArgumentCount(ParameterInfo[] args)
    {
        return args.Skip(1).Count(p => !p.IsOptional);
    }
    
    private static int DefaultPrivilege(bool isAdmin)
    {
        return isAdmin ? PermissionSystem.HighestPrivilege : PermissionSystem.LowestPrivilege;
    }

    public static void AddCommandType(Type type, bool register = true)
    {
        LoadedCommandTypes.Add(type);
        if (register) CommandRegistry.RegisterCommandType(type);
    }
    
    public static IOrderedEnumerable<Command> GetAllCommands(bool fullAssembly = false)
    {
        var commandTypes = fullAssembly ? Assembly.GetCallingAssembly().GetTypes() : LoadedCommandTypes.ToArray();
        
        var defaultPermissions = PermissionSystem.DefaultCommandPermissions();
        var commands = commandTypes.Select(t =>
            {
                var groupAttribute = t.GetCustomAttribute<CommandGroupAttribute>();
                var groupName = groupAttribute?.Name ?? "";
                var groupShortHand = groupAttribute?.ShortHand ?? "";
                var methods = t.GetMethods()
                    .Select(m => new Tuple<CommandAttribute, ParameterInfo[]>(m.GetCustomAttribute<CommandAttribute>(), m.GetParameters()))
                    .Where(m => m.Item1 != null)
                    .Select(m =>
                    {
                        var shortGroupName = string.IsNullOrEmpty(groupShortHand) ? groupName : groupShortHand;
                        var command = m.Item1;
                        var argCount = RequiredArgumentCount(m.Item2);
                        var permissionKey = CommandAttributesToPermissionKey(groupName, command.Name, argCount);
                        return new Command(
                            permissionKey,
                            CommandAttributesToPermissionKey(groupName, command.Name, 0),
                            CommandAttributesToPermissionKey(shortGroupName, command.ShortHand, 0),
                            command.Usage?.Replace("|", "\\|") ?? "", // This is quoted, so replace with escape
                            command.Description?.Replace("|", "&#124;") ??
                            "", // This is displayed, so replace with HTML value
                            command.AdminOnly,
                            defaultPermissions.GetValueOrDefault(permissionKey, DefaultPrivilege(command.AdminOnly)));
                    });
                return methods;
            }).SelectMany(s => s)
            .OrderBy(c => c.PermissionKey);

        return commands;
    }

    public static void ValidatedCommandPermissions(IEnumerable<Command> commands)
    {
        var commandsDictionary = commands.ToDictionary(command => command.PermissionKey, command => command.IsAdmin);
        var currentPermissions = Database.CommandPermission.Keys;
        foreach (var permission in currentPermissions.Where(permission => !commandsDictionary.ContainsKey(permission)))
        {
            Plugin.Log(LogSystem.Core, LogLevel.Message, $"Removing old permission: {permission}");
            Database.CommandPermission.Remove(permission);
        }

        var defaultCommandPermissions = PermissionSystem.DefaultCommandPermissions();
        foreach (var command in commandsDictionary)
        {
            // Add the permission if it doesn't already exist there
            var added = Database.CommandPermission.TryAdd(command.Key, DefaultPrivilege(command.Value));
            if (added) Plugin.Log(LogSystem.Core, LogLevel.Message, $"Added new permission: {command.Key}");

            // Warn if the default permissions does not include this command
            if (!defaultCommandPermissions.ContainsKey(command.Key))
            {
                Plugin.Log(LogSystem.Core, LogLevel.Warning, $"Default permissions do not include: {command.Key}\nRegenerate the default command permissions (and maybe Command.md).", true);
            }
        }
            
        Plugin.Log(LogSystem.Core, LogLevel.Info, "Permissions have been validated");
    }

    private static string PadCommandString(int index, string command, int width)
    {
        if (string.IsNullOrEmpty(command)) return "".PadRight(width);
        switch (index)
        {
            // Command
            case 0:
            // Shorthand
            case 1:
                return $"`.{command}`".PadRight(width);
            // Usage
            case 2:
            // Level
            case 5:
                return $"`{command}`".PadRight(width);
            // Admin
            case 4:
                // Center the check character
                var padLeft = (width - 1)/2 + 1;
                return (command.Equals("True") ? "\u2611" : "\u2610").PadLeft(padLeft).PadRight(width);
            // Description (& other)
            default:
                return command.PadRight(width);
        }
    }
    
    public static void GenerateCommandMd(IEnumerable<Command> commands)
    {
        // We want to generate something like this:
        // | Command | Short hand | Usage | Description | Admin | Level |
        var headers = new[] { "Command", "Short hand", "Usage", "Description", "Admin", "Level" };
        // Calculate the width of each column
        var defaultWidths = headers.Select(s => s.Length).ToArray();
        var columnWidths = commands.Aggregate(defaultWidths, (acc, command) =>
        {
            acc[0] = Math.Max(acc[0], command.Name.Length + 3); // Add length for quotes and "."
            acc[1] = Math.Max(acc[1], command.ShortHand.Length + 3); // Add length for quotes and "."
            acc[2] = Math.Max(acc[2], command.Usage.Length + 2); // Add length for quotes
            acc[3] = Math.Max(acc[3], command.Description.Length);
            acc[4] = Math.Max(acc[4], command.IsAdmin.ToString().Length);
            acc[5] = Math.Max(acc[5], command.PrivilegeLevel.ToString().Length + 2); // Add length for quotes
            return acc;
        });
        Func<Command, int, string> getColumnData = (command, i) =>
        {
            switch (i)
            {
                case 0:
                    return command.Name;
                case 1:
                    return command.ShortHand;
                case 2:
                    return command.Usage;
                case 3:
                    return command.Description;
                case 4:
                    return command.IsAdmin.ToString();
                case 5:
                    return command.PrivilegeLevel.ToString();
            }

            return "";
        };
        // Generate the table
        var commandTableOutput =
            "To regenerate this table, uncomment the `GenerateCommandMd` function in `Plugin.ValidateCommandPermissions`. Then check the LogOutput.log in the server after starting.\n" +
            "Usage arguments: <> are required, [] are optional\n\n" +
            $"| {string.Join(" | ", headers.Select((s, i) => s.PadRight(columnWidths[i])))} |\n" +
            $"|-{string.Join("-|-", columnWidths.Select(width => "-".PadRight(width, '-')))}-|\n" +
            string.Join("\n", commands.Select(command => "| " + string.Join(" | ", columnWidths.Select((width, i) => PadCommandString(i, getColumnData(command, i), width))) + " |"));
        
        File.WriteAllText(Path.Combine(AutoSaveSystem.ConfigPath, "Command.md"), commandTableOutput);
    }
    
    public static void GenerateDefaultCommandPermissions(IEnumerable<Command> commands)
    {
        var defaultPermissionsFormat = commands.Select(command => $"{{\"{command.PermissionKey}\", {command.PrivilegeLevel}}}");
        File.WriteAllText(Path.Combine(AutoSaveSystem.ConfigPath, "PermissionSystem.DefaultCommandPermissions.txt"), $"{{\n\t{string.Join(",\n\t", defaultPermissionsFormat)}\n}}");
    }
}