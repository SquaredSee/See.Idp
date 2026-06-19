using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace See.Idp.Tests.Support;

internal static class AsyncEnumerableTestFactory
{
    public static async IAsyncEnumerable<T> Create<T>(
        IEnumerable<T> items,
        [EnumeratorCancellation] CancellationToken ct = default
    )
    {
        foreach (var item in items)
        {
            ct.ThrowIfCancellationRequested();
            yield return item;
            await Task.Yield();
        }
    }

    public static IAsyncEnumerable<T> Create<T>(params T[] items)
    {
        return Create((IEnumerable<T>)items);
    }
}
