using System;
using System.Reflection;

namespace Domium.Extensions.DependencyInjection.Internal;

internal static class AssemblyFilter
{
    public static bool IsCandidateAssembly(Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return false;
        }

        var name = assembly.GetName().Name;

        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return !IsExcludedAssemblyName(name);
    }

    private static bool IsExcludedAssemblyName(string name)
    {
        return name.Equals("System", StringComparison.OrdinalIgnoreCase)
               || name.Equals("Microsoft", StringComparison.OrdinalIgnoreCase)
               || name.Equals("netstandard", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("runtime.", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("Scrutor", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("Newtonsoft.", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("Serilog", StringComparison.OrdinalIgnoreCase)
               || name.StartsWith("Swashbuckle", StringComparison.OrdinalIgnoreCase);
    }
}