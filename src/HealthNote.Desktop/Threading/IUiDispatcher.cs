using System;
using System.Threading.Tasks;

namespace HealthNote.Desktop.Threading
{
    public interface IUiDispatcher
    {
        Task ApplyAsync(Action action);
    }
}
