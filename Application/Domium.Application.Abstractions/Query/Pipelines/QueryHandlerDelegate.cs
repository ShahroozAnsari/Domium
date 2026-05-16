using System.Threading.Tasks;

namespace Domium.Application.Abstractions.Query.Pipelines;

public delegate Task<TResult> QueryHandlerDelegate<TResult>();