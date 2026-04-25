using Soenneker.TestHosts.Unit;
using Soenneker.Tests.HostedUnit;

namespace Soenneker.Quark.Runners.Crawler.Tests;

[ClassDataSource<UnitTestHost>(Shared = SharedType.PerTestSession)]
public sealed class QuarkRunnerTests : HostedUnitTest
{
    public QuarkRunnerTests(UnitTestHost host) : base(host)
    {
    }

    [Test]
    public void Default()
    {

    }
}
