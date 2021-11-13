using Microsoft.PowerShell.Commands;
using System.Management.Automation.Language;

namespace PSSharp.ModuleFactory
{
    /// <summary>
    /// Attempts to deserialize the contents of a file path into a module fingerprint.
    /// </summary>
    public sealed class FilePathToModuleFingerprintTransformationAttribute : ArgumentTransformationAttribute
    {
        /// <summary>
        /// Processes <paramref name="inputData"/> as one or more objects. When the object is a string that
        /// references an existing file path and the path successfully deserializes to a
        /// <see cref="ModuleFingerprint"/>, this transformation passes through that fingerprint instead of
        /// the file path.
        /// </summary>
        /// <param name="engineIntrinsics"></param>
        /// <param name="inputData"></param>
        /// <returns><see cref="object"/>[] of the elements of <paramref name="inputData"/>, with paths
        /// that could be deserialized into a <see cref="ModuleFingerprint"/> replaced with the fingerprint.
        /// If only one element would exist in the output array, that element is unwrapped and returned 
        /// instead.</returns>
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            IEnumerable enumerableData;
            enumerableData = LanguagePrimitives.GetEnumerable(inputData);
            if (enumerableData is null) enumerableData = new object[] { inputData };
            var results = new List<object>();

            foreach (var element in enumerableData)
            {
                if (element is string possiblePath)
                {
                    if (engineIntrinsics.SessionState.InvokeProvider.Item.Exists(possiblePath))
                    {
                        var resolvedPaths = engineIntrinsics.SessionState.Path.GetResolvedProviderPathFromPSPath(possiblePath, out var provider);
                        if (provider.ImplementingType != typeof(FileSystemProvider))
                        {
                            results.Add(element);
                            continue;
                        }
                        else
                        {
                            var fingerprints = new List<ModuleFingerprint>(resolvedPaths.Count);
                            try
                            {
                                foreach (var resolvedPath in resolvedPaths)
                                {
                                    using var fs = new FileStream(resolvedPath, FileMode.Open, FileAccess.Read);
                                    using var streamReader = new StreamReader(fs);
                                    var jsonReader = new Newtonsoft.Json.JsonTextReader(streamReader);
                                    var serializer = Newtonsoft.Json.JsonSerializer.CreateDefault();
                                    var fingerprint = serializer.Deserialize<ModuleFingerprint>(jsonReader);
                                    if (fingerprint is not null)
                                    {
                                        fingerprints.Add(fingerprint);
                                    }
                                }
                                results.AddRange(fingerprints);
                            }
                            catch
                            {
                                results.Add(element);
                            }
                        }
                    }
                    else
                    {
                        results.Add(element);
                    }
                }
                else
                {
                    results.Add(element);
                }
            }

            if (results.Count == 1)
            {
                // The engine can convert back to an array if necessary, but cannot convert a single-element
                // array into that element.
                return results[0];
            }
            else
            {
                // The engine is much more strongly inclined to convert from an array than from a List<T>.
                return results.ToArray();
            }
        }
    }

    /// <summary>
    /// Parameter completions for the name of a module.
    /// </summary>
    public sealed class ModuleNameCompletionAttribute : ArgumentCompleterFactoryAttribute, IArgumentCompleter
    {
        /// <summary>
        /// Set to <see langword="true"/> to offer completions from all modules on the machine
        /// instead of just from modules imported into the current PowerShell session.
        /// </summary>
        public bool All { get; set; }
        /// <summary>
        /// Offers completion results based on the names of PowerShell modules loaded into the current PowerShell
        /// runspace (or all modules available if <see cref="All"/> is set to <see langword="true"/> where the
        /// name matches the wildcard expression based on <paramref name="wordToComplete"/>.
        /// </summary>
        /// <param name="commandName"/>
        /// <param name="parameterName"/>
        /// <param name="wordToComplete">Text to complete with a module name match.</param>
        /// <param name="commandAst"/>
        /// <param name="fakeBoundParameters"/>
        /// <returns></returns>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var quote = WithoutQuotations(ref wordToComplete);
            var wc = wordToComplete + "*";

            using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);

            ps.AddCommand("Get-Module");
            if (!string.IsNullOrWhiteSpace(commandName))
            {
                ps.AddParameter("Name", wc);
            }
            if (All)
            {
                ps.AddParameter("All");
            }

            var modules = ps.Invoke<PSModuleInfo>();
            if (modules is null || modules.Count == 0)
            {
                return Array.Empty<CompletionResult>();
            }
            var results = new CompletionResult[modules.Count];
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (!quote.HasValue && module.Name.Contains(' ')) quote = '\'';
                var completionText = quote + module.Name + quote;
                var listItemText = module.Name;
                var toolTip = string.Format("{0}: {1}\n{2}: {3}\n{4}: {5}",
                    nameof(module.Name),
                    module.Name,
                    nameof(module.ModuleType),
                    module.ModuleType,
                    nameof(module.Path), module.Path);
                results[i] = new CompletionResult(
                    completionText,
                    listItemText,
                    CompletionResultType.ParameterValue,
                    toolTip);
            }
            return results;
        }

        public override IArgumentCompleter Create() => this;

        private static char? WithoutQuotations(ref string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (value[0] == '"')
            {
                if (value[^1] == '"')
                {
                    value = value[1..^2];
                    return '"';
                }
                else
                {
                    value = value[1..];
                    return '"';
                }
            }
            else if (value[0] == '\'')
            {
                if (value[^1] == '\'')
                {
                    value = value[1..^2];
                    return '\'';
                }
                else
                {
                    value = value[1..];
                    return '\'';
                }
            }
            else
            {
                return null;
            }
        }
    }
    
    /// <summary>
    /// Offers no parameter completions (used to override the default path-based completion
    /// when that completion makes no sense for a given parameter).
    /// </summary>
    public sealed class NoCompletionAttribute : ArgumentCompleterFactoryAttribute, IArgumentCompleter
    {
        /// <summary>
        /// Offers no completions.
        /// </summary>
        /// <returns>An empty set of <see cref="CompletionResult"/>s.</returns>
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
            => Array.Empty<CompletionResult>();

        /// <summary>
        /// Returns the current instance.
        /// </summary>
        /// <returns></returns>
        public override IArgumentCompleter Create() => this;
    }
}
