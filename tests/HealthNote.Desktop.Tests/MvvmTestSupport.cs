using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HealthNote.Application;
using HealthNote.Desktop.Threading;

namespace HealthNote.Desktop.Tests
{
    internal sealed class ControlledQuery : IClientReadinessQuery
    {
        public List<Request> Requests { get; } = new List<Request>();

        public Task<bool> IsReadyAsync(CancellationToken cancellationToken)
        {
            Request request = new Request(cancellationToken);
            Requests.Add(request);
            return request.Completion.Task;
        }

        internal sealed class Request
        {
            public Request(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
            }

            public CancellationToken CancellationToken { get; }
            public TaskCompletionSource<bool> Completion { get; } =
                new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        }
    }

    internal sealed class ReadyQuery : IClientReadinessQuery
    {
        public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
    }

    internal sealed class InlineDispatcher : IUiDispatcher
    {
        public Task ApplyAsync(Action action)
        {
            action();
            return Task.CompletedTask;
        }
    }

    internal sealed class PausedDispatcher : IUiDispatcher
    {
        public TaskCompletionSource<Action> Pending { get; } =
            new TaskCompletionSource<Action>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> applied =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task ApplyAsync(Action action)
        {
            Pending.SetResult(action);
            return applied.Task;
        }

        public void Apply()
        {
            Pending.Task.GetAwaiter().GetResult()();
            applied.SetResult(true);
        }
    }

    internal static class TestCompletion
    {
        public static async Task WaitAsync(Task task)
        {
            if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5))) != task)
            {
                throw new TimeoutException("Controlled test operation did not complete.");
            }

            await task;
        }
    }
}
