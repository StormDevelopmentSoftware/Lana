using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;

public static class DSharpPlusExtensions
{
    public static bool TryGetFailedCheck<TFailedCheck>(this ChecksFailedException exception, out TFailedCheck obj)
            where TFailedCheck : CheckBaseAttribute
    {
        obj = exception.FailedChecks.FirstOrDefault(x => x is TFailedCheck) as TFailedCheck;
        return obj != null;
    }

    public static bool HasFailedCheck<TFailedCheck>(this ChecksFailedException exception)
            where TFailedCheck : CheckBaseAttribute
    {
        return TryGetFailedCheck<TFailedCheck>(exception, obj: out var _);
    }
}
