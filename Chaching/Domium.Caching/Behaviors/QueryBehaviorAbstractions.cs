using System.Threading;
using System.Threading.Tasks;

namespace Domium.Caching.Behaviors
{
    /// <summary>
    /// Represents the delegate that executes the next query handler in the pipeline.
    /// </summary>
    /// <typeparam name="TResult">
    /// The query result type.
    /// </typeparam>
    /// <returns>
    /// A task that returns the query result.
    /// </returns>
    public delegate Task<TResult> DomiumQueryHandlerDelegate<TResult>();

    /// <summary>
    /// Defines a query behavior in the Domium query pipeline.
    /// </summary>
    /// <typeparam name="TQuery">
    /// The query type.
    /// </typeparam>
    /// <typeparam name="TResult">
    /// The query result type.
    /// </typeparam>
    public interface IDomiumQueryBehavior<in TQuery, TResult>
        where TQuery : class
    {
        /// <summary>
        /// Handles the query and optionally invokes the next behavior or handler.
        /// </summary>
        /// <param name="query">
        /// The query instance.
        /// </param>
        /// <param name="next">
        /// The delegate that executes the next pipeline component.
        /// </param>
        /// <param name="cancellationToken">
        /// A cancellation token that can cancel the operation.
        /// </param>
        /// <returns>
        /// The query result.
        /// </returns>
        Task<TResult> HandleAsync(
            TQuery query,
            DomiumQueryHandlerDelegate<TResult> next,
            CancellationToken cancellationToken);
    }
}