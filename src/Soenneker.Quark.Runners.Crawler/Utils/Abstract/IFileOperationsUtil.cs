using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Quark.Runners.Crawler.Utils.Abstract;

/// <summary>
/// Coordinates the Quark component-preview refresh.
/// </summary>
public interface IFileOperationsUtil
{
    /// <summary>
    /// Refreshes and publishes the generated component-preview repositories.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A task that completes when the full processing workflow has finished.</returns>
    ValueTask Process(CancellationToken cancellationToken);
}
