using Microsoft.PowerShell.Commands;
using System.Diagnostics.CodeAnalysis;

namespace PSSharp.ModuleFactory
{
    public sealed class ModuleSpecificationComparer : IComparer<ModuleSpecification>, IEqualityComparer<ModuleSpecification>
    {
        public static ModuleSpecificationComparer Comparer { get; } = new();

        public int Compare(ModuleSpecification? x, ModuleSpecification? y)
            => StringComparer.OrdinalIgnoreCase.Compare(x?.ToString(), y?.ToString());

        public bool Equals(ModuleSpecification? x, ModuleSpecification? y)
            => StringComparer.OrdinalIgnoreCase.Equals(x?.ToString(), y?.ToString());

        public int GetHashCode([DisallowNull] ModuleSpecification obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(obj?.ToString() ?? throw new ArgumentNullException(nameof(obj)));
    }
    public sealed class PSSnapInSpecificationComparer : IComparer<PSSnapInSpecification>, IEqualityComparer<PSSnapInSpecification>
    {
        public static PSSnapInSpecificationComparer Comparer { get; } = new();

        [return: NotNullIfNotNull("obj")]
        private static string? GetStringRepresentation(PSSnapInSpecification? obj)
            => obj is null ? null : obj.Version is null ? obj.Name : $"{obj.Name} {obj.Version}";

        public int Compare(PSSnapInSpecification? x, PSSnapInSpecification? y)
            => StringComparer.OrdinalIgnoreCase.Compare(
                GetStringRepresentation(x),
                GetStringRepresentation(y));

        public bool Equals(PSSnapInSpecification? x, PSSnapInSpecification? y)
            => StringComparer.OrdinalIgnoreCase.Equals(GetStringRepresentation(x), GetStringRepresentation(y));

        public int GetHashCode([DisallowNull] PSSnapInSpecification obj)
            => StringComparer.OrdinalIgnoreCase.GetHashCode(
                GetStringRepresentation(obj) ?? throw new ArgumentNullException(nameof(obj)));
    }
}
