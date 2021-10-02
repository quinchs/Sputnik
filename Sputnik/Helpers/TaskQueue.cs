using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sputnik
{
    public class TaskQueue<TResult>
    {
        private Queue<Func<Task<TResult>>> _tasks;

        private int _runCount;
        private TimeSpan _delay;

        public TaskQueue(int runCount, TimeSpan delay, IEnumerable<Task<TResult>> tasks)
            : this(runCount, delay)
        {
            foreach (var t in tasks)
                _tasks.Enqueue(() => t);
        }
        public TaskQueue(int runCount, TimeSpan delay)
        {
            _runCount = runCount;
            _delay = delay;
            _tasks = new Queue<Func<Task<TResult>>>();
        }

        public void Add(Func<Task<TResult>> task)
        {
            _tasks.Enqueue(task);
        }

        public async Task<IEnumerable<TResult>> RunAsync()
        {
            List<TResult> results = new();

            while (_tasks.Count != 0)
            {
                var q = new List<Task<TResult>>();

                for (int i = 0; i != _runCount; i++)
                {
                    if (_tasks.TryDequeue(out var t))
                    {
                        q.Add(Task.Run(t));
                    }
                    else
                        break;
                }

                results.AddRange(await Task.WhenAll(q).ConfigureAwait(false));

                await Task.Delay(_delay);
            }

            return results;
        }
    }
}
