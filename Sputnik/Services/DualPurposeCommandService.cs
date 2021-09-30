﻿using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Sputnik.Context;
using Sputnik.Modules;
using Sputnik.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Sputnik.Services
{
    public class DualPurposeCommandService
    {
        public event Func<LogMessage, Task> Log
        {
            add => underlyingService.Log += value;
            remove => underlyingService.Log -= value;
        }
        public event Func<Optional<CommandInfo>, ICommandContext, IResult, Task> CommandExecuted
        {
            add => underlyingService.CommandExecuted += value;
            remove => underlyingService.CommandExecuted -= value;
        }

        private List<ModuleInfo> CustomModules = new List<ModuleInfo>();

        private CommandService underlyingService;
        private static readonly TypeInfo ModuleTypeInfo = typeof(DualPurposeModuleBase).GetTypeInfo();
        //private static const TypeInfo BaseModuleTypeInfo = typeof(ModuleBase<SocketCommandContext>).GetTypeInfo();
        private readonly SemaphoreSlim _moduleLock;
        private CommandServiceConfig Config;

        public DualPurposeCommandService()
            : this(new CommandServiceConfig())
        {
            _moduleLock = new SemaphoreSlim(1, 1);
        }

        public void AddTypeReader<T>(TypeReader reader)
            => AddTypeReader(typeof(T), reader);

        public void AddTypeReader(Type type, TypeReader reader)
            => underlyingService.AddTypeReader(type, reader);

        public DualPurposeCommandService(CommandServiceConfig conf)
        {
            conf.IgnoreExtraArgs = true;
            underlyingService = new CommandService(conf);
            this.Config = conf;
        }

        public Task<IResult> ExecuteAsync(ICommandContext context, int argPos, IServiceProvider services, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => ExecuteAsync(context, context.Message.Content.Substring(argPos), services, multiMatchHandling);

        public Task<IResult> ExecuteAsync(ICommandContext context, string input, IServiceProvider services, MultiMatchHandling multiMatchHandling = MultiMatchHandling.Exception)
            => underlyingService.ExecuteAsync(context, input, services, multiMatchHandling);

        public async Task RegisterModulesAsync(Assembly assembly, IServiceProvider services)
        {
            await _moduleLock.WaitAsync().ConfigureAwait(false);

            var types = Search(assembly);
            await BuildAsync(types, services).ConfigureAwait(false);
        }

        private async Task<Dictionary<Type, ModuleInfo>> BuildAsync(IEnumerable<TypeInfo> validTypes, IServiceProvider services)
        {
            var topLevelGroups = validTypes.Where(x => x.DeclaringType == null || !IsValidModuleDefinition(x.DeclaringType.GetTypeInfo()));

            var builtTypes = new List<TypeInfo>();

            var result = new Dictionary<Type, ModuleInfo>();

            foreach (var typeInfo in topLevelGroups)
            {
                // TODO: This shouldn't be the case; may be safe to remove?
                if (result.ContainsKey(typeInfo.AsType()))
                    continue;

                ModuleInfo module = null;

                if (ModuleTypeInfo.IsAssignableFrom(typeInfo))
                {
                    module = await underlyingService.CreateModuleAsync("", (x) => BuildModule(x, typeInfo, services));
                    CustomModules.Add(module);
                }
                else
                {
                    module = await underlyingService.AddModuleAsync(typeInfo, services).ConfigureAwait(false);
                }

                result.TryAdd(typeInfo.AsType(), module);
            }

            Logger.Debug($"Successfully built {builtTypes.Count} modules.", Severity.CommandService);

            return result;
        }

        public async Task<ICommandContext> ResolveContextAsync(DiscordSocketClient client, string commandName, SocketUserMessage message)
        {
            if (CustomModules.Any(x => x.Commands.Any(y => y.Name == commandName || y.Aliases.Contains(commandName))))
                return new DualPurposeContext(client, message);
            else
                return new SocketCommandContext(client, message);
        }

        private IReadOnlyList<TypeInfo> Search(Assembly assembly)
        {
            bool IsLoadableModule(TypeInfo info)
            {
                return info.DeclaredMethods.Any(x => x.GetCustomAttribute<CommandAttribute>() != null) &&
                    info.GetCustomAttribute<DontAutoLoadAttribute>() == null;
            }

            var result = new List<TypeInfo>();

            foreach (var typeInfo in assembly.DefinedTypes)
            {
                if (typeInfo.IsPublic || typeInfo.IsNestedPublic)
                {
                    if (IsValidModuleDefinition(typeInfo) &&
                        !typeInfo.IsDefined(typeof(DontAutoLoadAttribute)))
                    {
                        result.Add(typeInfo);
                    }
                }
                else if (IsLoadableModule(typeInfo))
                {
                    Logger.Warn($"Class {typeInfo.FullName} is not public and cannot be loaded. To suppress this message, mark the class with {nameof(DontAutoLoadAttribute)}.");
                }
            }

            return result;
        }

        private static bool IsValidModuleDefinition(TypeInfo typeInfo)
        {
            return (ModuleTypeInfo.IsAssignableFrom(typeInfo) || IsSubclassOfRawGeneric(typeof(ModuleBase<>), typeInfo.AsType())) &&
                   !typeInfo.IsAbstract &&
                   !typeInfo.ContainsGenericParameters;
        }
        private static bool IsValidCommandDefinition(MethodInfo methodInfo)
        {
            return methodInfo.IsDefined(typeof(CommandAttribute)) &&
                   (methodInfo.ReturnType == typeof(Task) || methodInfo.ReturnType == typeof(Task<RuntimeResult>)) &&
                   !methodInfo.IsStatic &&
                   !methodInfo.IsGenericMethod;
        }

        private static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        private void BuildModule(ModuleBuilder builder, TypeInfo typeInfo, IServiceProvider services)
        {
            var attributes = typeInfo.GetCustomAttributes();

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case NameAttribute name:
                        builder.Name = name.Text;
                        break;
                    case SummaryAttribute summary:
                        builder.Summary = summary.Text;
                        break;
                    case RemarksAttribute remarks:
                        builder.Remarks = remarks.Text;
                        break;
                    case AliasAttribute alias:
                        builder.AddAliases(alias.Aliases);
                        break;
                    case GroupAttribute group:
                        builder.Name = builder.Name ?? group.Prefix;
                        builder.Group = group.Prefix;
                        builder.AddAliases(group.Prefix);
                        break;
                    case PreconditionAttribute precondition:
                        builder.AddPrecondition(precondition);
                        break;
                    default:
                        builder.AddAttributes(attribute);
                        break;
                }
            }

            //Check for unspecified info
            if (builder.Aliases.Count == 0)
                builder.AddAliases("");
            if (builder.Name == null)
                builder.Name = typeInfo.Name;

            // Get all methods (including from inherited members), that are valid commands
            var validCommands = typeInfo.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(IsValidCommandDefinition);

            foreach (var method in validCommands)
            {
                var name = method.GetCustomAttribute<CommandAttribute>();

                var createInstance = ReflectionUtils.CreateBuilder<DualPurposeModuleBase>(typeInfo);

                async Task<IResult> ExecuteCallback(ICommandContext context, object[] args, IServiceProvider services, CommandInfo cmd)
                {
                    var instance = createInstance(services);
                    instance.SetContext(context);
                    args = instance.CreateInteractionArgs(cmd, this, args);

                    try
                    {
                        var task = method.Invoke(instance, args) as Task ?? Task.Delay(0);
                        if (task is Task<RuntimeResult> resultTask)
                        {
                            return await resultTask.ConfigureAwait(false);
                        }
                        else
                        {
                            await task.ConfigureAwait(false);
                            return ExecuteResult.FromSuccess();
                        }
                    }
                    finally
                    {
                        (instance as IDisposable)?.Dispose();
                    }
                }

                builder.AddCommand(name.Text, ExecuteCallback, (command) =>
                {
                    BuildCommand(command, method, services);
                });
            }
        }

        private void BuildCommand(CommandBuilder builder, MethodInfo method, IServiceProvider serviceprovider)
        {
            var attributes = method.GetCustomAttributes();

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case CommandAttribute command:
                        builder.AddAliases(command.Text);
                        builder.RunMode = command.RunMode;
                        builder.Name = builder.Name ?? command.Text;
                        builder.IgnoreExtraArgs = command.IgnoreExtraArgs ?? Config.IgnoreExtraArgs;
                        break;
                    case NameAttribute name:
                        builder.Name = name.Text;
                        break;
                    case PriorityAttribute priority:
                        builder.Priority = priority.Priority;
                        break;
                    case SummaryAttribute summary:
                        builder.Summary = summary.Text;
                        break;
                    case RemarksAttribute remarks:
                        builder.Remarks = remarks.Text;
                        break;
                    case AliasAttribute alias:
                        builder.AddAliases(alias.Aliases);
                        break;
                    case PreconditionAttribute precondition:
                        builder.AddPrecondition(precondition);
                        break;
                    default:
                        builder.AddAttributes(attribute);
                        break;
                }
            }

            if (builder.Name == null)
                builder.Name = method.Name;

            var parameters = method.GetParameters();
            int pos = 0, count = parameters.Length;
            foreach (var paramInfo in parameters)
            {
                builder.AddParameter(paramInfo.Name, paramInfo.ParameterType, (parameter) =>
                {
                    BuildParameter(parameter, paramInfo, pos++, count, serviceprovider);
                });
            }
        }

        private static void BuildParameter(ParameterBuilder builder, System.Reflection.ParameterInfo paramInfo, int position, int count, IServiceProvider services)
        {
            var attributes = paramInfo.GetCustomAttributes();
            var paramType = paramInfo.ParameterType;

            builder.IsOptional = true;
            builder.DefaultValue = paramInfo.HasDefaultValue ? paramInfo.DefaultValue : null;

            foreach (var attribute in attributes)
            {
                switch (attribute)
                {
                    case SummaryAttribute summary:
                        builder.Summary = summary.Text;
                        break;
                    case ParamArrayAttribute _:
                        builder.IsMultiple = true;
                        paramType = paramType.GetElementType();
                        break;
                    case ParameterPreconditionAttribute precon:
                        builder.AddPrecondition(precon);
                        break;
                    case RemainderAttribute _:
                        if (position != count - 1)
                            throw new InvalidOperationException($"Remainder parameters must be the last parameter in a command. Parameter: {paramInfo.Name} in {paramInfo.Member.DeclaringType.Name}.{paramInfo.Member.Name}");

                        builder.IsRemainder = true;
                        break;
                    default:
                        builder.AddAttributes(attribute);
                        break;
                }
            }
        }
    }
}
