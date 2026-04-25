using System.Threading;
using System.Threading.Tasks;

namespace Soenneker.Quark.Runners.Crawler.Utils.Abstract;

public interface IFileOperationsUtil
{
    ValueTask Process(CancellationToken cancellationToken);
}
