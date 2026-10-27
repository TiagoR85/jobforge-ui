using JobForge.Ui.Services;

namespace JobForge.Ui.Tests;

public sealed class EchoStoreTests
{
    [Fact]
    public void Keeps_only_the_last_20_requests_in_order()
    {
        var store = new EchoStore();
        for (var i = 0; i < 25; i++)
        {
            store.Add(new EchoEntry(DateTimeOffset.UnixEpoch.AddSeconds(i), "text/plain", "ua", $"body{i}"));
        }

        var entries = store.Snapshot();

        Assert.Equal(20, entries.Count);
        Assert.Equal("body5", entries[0].Body);
        Assert.Equal("body24", entries[^1].Body);
    }
}
