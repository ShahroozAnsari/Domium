using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Command.PipeLines;

public delegate Task CommandHandlerDelegate();

public delegate Task<TResult> CommandHandlerDelegate<TResult>();
