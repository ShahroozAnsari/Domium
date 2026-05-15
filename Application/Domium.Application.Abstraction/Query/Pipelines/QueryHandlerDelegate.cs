using System.Threading.Tasks;

namespace Domium.Application.Abstraction.Query.Pipelines;

public delegate Task<TResult> QueryHandlerDelegate<TResult>();