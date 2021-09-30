using Discord.WebSocket;
using Dynmap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik
{
    public class HandlerService
    {
        private DiscordSocketClient client;
        private DynmapClient dynmap;
        private static readonly Dictionary<DiscordHandler, object> Handlers = new Dictionary<DiscordHandler, object>();

        /// <summary>
        /// Gets a handler with the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the handler to get.</typeparam>
        /// <returns>The handler with the type of <typeparamref name="T"/>. If no handler is found then <see langword="null"/>.</returns>
        public static T GetHandlerInstance<T>()
            where T : DiscordHandler => Handlers.FirstOrDefault(x => x.Key.GetType() == typeof(T)).Value as T;

        public HandlerService(DiscordSocketClient client, DynmapClient dynmap)
        {
            this.client = client;
            this.dynmap = dynmap;

            this.client.Ready += Client_Ready;

            List<Type> typs = new List<Type>();
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAssignableTo(typeof(DiscordHandler)) && type != typeof(DiscordHandler))
                    {
                        // add to a cache.
                        typs.Add(type);
                    }
                }
            }

            foreach (var handler in typs)
            {
                var inst = Activator.CreateInstance(handler);
                Handlers.Add(inst as DiscordHandler, inst);
            }

            Logger.Write($"Handler service <Green>Initialized</Green>! {Handlers.Count} handlers created!", Severity.Core);
        }

        private Task Client_Ready()
        {
            _ = Task.Run(async () =>
            {
                var work = new List<Func<Task>>();

                foreach (var item in Handlers)
                {
                    work.Add(async () =>
                    {
                        try
                        {
                            await item.Key.InitializeAsync(this.client, this.dynmap);
                            item.Key.Initialize(this.client, this.dynmap);
                        }
                        catch (Exception x)
                        {
                            Logger.Write($"Exception occured while initializing {item.Key.GetType().Name}: {x}", new[] { Severity.Core, Severity.Critical }, nameof(HandlerService));
                        }
                    });
                }

                await Task.WhenAll(work.Select(x => x()).ToArray());
                Logger.Write($"All {Handlers.Count} handlers <Green>Initialized</Green>", new[] { Severity.Core, Severity.Log }, nameof(HandlerService));
            });

            return Task.CompletedTask;
        }
    }

    /// <summary>
    ///     Marks the current class as a handler.
    /// </summary>
    public abstract class DiscordHandler
    {
        /// <summary>
        ///     Intitialized this handler asynchronously.
        /// </summary>
        /// <param name="client">The <see cref="DiscordSocketClient"/> to inject.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> to inject.</param>
        /// <returns>A task representing the asynchronous operation of initializing this handler.</returns>
        public virtual Task InitializeAsync(DiscordSocketClient client, DynmapClient dynmap)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        ///     Intitialized this handler.
        /// </summary>
        /// <param name="client">The <see cref="DiscordSocketClient"/> to inject.</param>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to inject.</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> to inject.</param>
        public virtual void Initialize(DiscordSocketClient client, DynmapClient dynmap)
        {
        }
    }
}
