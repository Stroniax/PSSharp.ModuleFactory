using Microsoft.PowerShell.Commands;
using System.Collections.ObjectModel;
using System.Management.Automation.Language;
using System.Reflection;

namespace PSSharp.ModuleFactory.Commands
{
    public abstract class ModuleFactoryCmdlet : PSCmdlet
    {
        internal const string ResourceBaseName = "PSSharp.ModuleFactory.Properties.Resources";

        public const string ModuleProjectNoun = "ModuleProject";
        public const string ModuleTemplateNoun = "ModuleTemplate";
        public const string ModuleTemplateRepositoryNoun = "ModuleTemplateRepository";
        public const string ModuleFingerprintNoun = "ModuleFingerprint";

        private static IModuleTemplateRepository? _repository;

        protected static bool IsRepositoryLoaded => _repository is not null;

        [System.Diagnostics.CodeAnalysis.AllowNull]
        protected internal static IModuleTemplateRepository Repository
        {
            get => _repository ??= new ModuleTemplateRepository();
            set => _repository = value;
        }



        /// <summary>
        /// Gets one or more resolved paths from the path indicated by <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path to expand.</param>
        /// <param name="isLiteralPath"><see langword="true"/> if the path is a LiteralPath (PSPath), which
        /// indicates that wildcards should not be evaluated when identifying the full path.</param>
        /// <param name="hadErrors"><see langword="true"/> if any errors are written to the error stream while
        /// invoking this method.</param>
        /// <param name="allowMultipleFiles"><see langword="true"/> if the path may be evaluated to express multiple files.</param>
        /// <param name="requireFileSystemPath"><see langword="true"/> if the path(s) must reside on the file system.</param>
        /// <returns></returns>
        protected IReadOnlyList<string> ResolvePath(string path, bool isLiteralPath, out bool hadErrors, bool allowMultipleFiles = false, bool requireFileSystemPath = false)
        {
            hadErrors = false;
            Collection<string> resolvedPaths;
            try
            {
                if (isLiteralPath)
                {
                    resolvedPaths = new Collection<string>()
                    {
                        GetUnresolvedProviderPathFromPSPath(path)
                    };
                }
                else
                {
                    resolvedPaths = GetResolvedProviderPathFromPSPath(path, out var provider);
                    if (requireFileSystemPath && provider.ImplementingType != typeof(FileSystemProvider))
                    {
                        hadErrors = true;
                        var ex = new PSArgumentException(Resources.ExpectedFileSystemPath);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.ExpectedFileSystemPath),
                            ErrorCategory.InvalidArgument,
                            path)
                        {
                            ErrorDetails = new ErrorDetails(string.Format(Resources.ExpectedFileSystemPathInterpolated, path))
                        };
                        WriteError(er);
                    }
                }
            }
            catch
            {
                hadErrors = true;
                WriteFileNotFoundError(path);
                return Array.Empty<string>();
            }
            if (!allowMultipleFiles && resolvedPaths.Count > 1)
            {
                hadErrors = true;
                var ex = new AmbiguousMatchException(Resources.AmbiguousPath);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.AmbiguousPath),
                    ErrorCategory.InvalidArgument,
                    path)
                {
                    ErrorDetails = new ErrorDetails(string.Format(Resources.AmbiguousPathInterpolated, path))
                };
                WriteError(er);
            }
            if (resolvedPaths.Count == 0)
            {
                hadErrors = true;
                WriteFileNotFoundError(path);
                return Array.Empty<string>();
            }
            if (requireFileSystemPath)
            {
                foreach (var resolvedPath in resolvedPaths)
                {
                    if (!File.Exists(resolvedPath) && !Directory.Exists(resolvedPath))
                    {
                        hadErrors = true;
                        WriteFileNotFoundError(path);
                    }
                }
            }
            return resolvedPaths;
        }
        /// <summary>
        /// Imports data from a ".psd1" file and attempts to convert the resultant hashtable(s) into <typeparamref name="T"/>.
        /// If the conversion fails, an error will be written to the error stream.
        /// </summary>
        /// <typeparam name="T">The type that the contents of the data file(s) should be converted to.</typeparam>
        /// <param name="path">The path of the data file to import.</param>
        /// <param name="isLiteralPath"><see langword="true"/> if the path is a LiteralPath (PSPath), which means that the path
        /// should be interpreted without evaluating wildcards.</param>
        /// <param name="hadErrors"><see langword="true"/> if any errors were written to the error stream while invoking this method.</param>
        /// <param name="multipleFiles"><see langword="false"/> if the path must resolve to a single file.</param>
        /// <returns></returns>
        protected T[] ImportPowerShellDataFiles<T>(string path, bool isLiteralPath, out bool hadErrors, bool multipleFiles = false)
        {
            var hashtableContents = ImportPowerShellDataFiles(path, isLiteralPath, out hadErrors, multipleFiles);
            if (LanguagePrimitives.TryConvertTo<T[]>(hashtableContents, out var result))
            {
                return result;
            }
            else
            {
                hadErrors = true;
                var ex = new InvalidDataException(Resources.DataFileCastError);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.DataFileCastError),
                    ErrorCategory.InvalidData,
                    path)
                {
                    ErrorDetails = new ErrorDetails(string.Format(Resources.DataFileCastErrorInterpolated, path, typeof(T)))
                };
                WriteError(er);
                return Array.Empty<T>();
            }
        }
        protected Hashtable[] ImportPowerShellDataFiles(string path, bool isLiteralPath, out bool hadErrors, bool multipleFiles = false)
        {
            hadErrors = false;
            var paths = ResolvePath(path, isLiteralPath, out hadErrors, allowMultipleFiles: multipleFiles, requireFileSystemPath: true);

            var results = new List<Hashtable>(16);
            foreach (var filePath in paths)
            {
                if (filePath is null) continue;

                object? SafeGetAstValue(Ast ast, out bool hadErrors)
                {
                    try
                    {
                        hadErrors = false;
                        return ast.SafeGetValue();
                    }
                    catch (InvalidOperationException inner)
                    {
                        hadErrors = true;
                        var ex = new InvalidDataException(Resources.ValueNotConstant, inner);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.ValueNotConstant),
                            ErrorCategory.InvalidData,
                            filePath)
                        {
                            ErrorDetails = new ErrorDetails(string.Format(Resources.ValueNotConstantInterpolated, filePath))
                        };
                        WriteError(er);
                        return null;
                    }
                }
                var ast = Parser.ParseFile(filePath, out _, out var errors);
                if (errors.Length > 0)
                {
                    hadErrors = true;
                    var inner = new ParseException(errors);
                    var ex = new ParseException(Resources.ParseError, inner);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.ParseError),
                        ErrorCategory.ParserError,
                        filePath)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.ParseErrorInterpolated, filePath))
                    };
                    WriteError(er);
                    continue;
                }
                var dataAsArray = ast.Find<ArrayLiteralAst>(false);
                if (dataAsArray is not null)
                {
                    var value = SafeGetAstValue(dataAsArray, out var getAstValueHadErrors);
                    if (getAstValueHadErrors)
                    {
                        hadErrors = true;
                        continue;
                    }

                    if (value is not Array array)
                    {
                        WriteDebug($"Attempt to parse value {value} of type {value?.GetType().FullName ?? "(null)"} as array failed when importing PowerShell data file at path '{filePath}'.");
                        continue;
                    }
                    var hashtables = new Hashtable[array.Length];
                    bool notHashtable = false;
                    for (int i = 0; i < array.Length; i++)
                    {
                        var element = ((dynamic)array)[i];
                        if (element is Hashtable hashtable)
                        {
                            hashtables[i] = hashtable;
                        }
                        else
                        {
                            notHashtable = true;
                            WriteDebug($"Attempt to parse value {element} of type {element?.GetType().FullName ?? "(null)"} as hashtable failed when importing PowerShell data file at path '{filePath}'.");
                            break;
                        }
                    }
                    if (notHashtable)
                    {
                        hadErrors = true;
                        var ex = new InvalidDataException(Resources.DataFileContentsInvalid);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.DataFileContentsInvalid),
                            ErrorCategory.InvalidData,
                            filePath)
                        {
                            ErrorDetails = new ErrorDetails(string.Format(Resources.DataFileContentsInvalidInterpolated, filePath))
                        };
                        WriteError(er);
                    }
                    else
                    {
                        results.AddRange(hashtables);
                    }
                    continue;
                }
                var hashtableAst = ast.Find<HashtableAst>(false);
                if (hashtableAst is not null)
                {
                    var value = SafeGetAstValue(hashtableAst, out var getAstValueHadErrors);
                    if (getAstValueHadErrors)
                    {
                        hadErrors = true;
                        continue;
                    }
                    if (value is Hashtable hashtable)
                    {
                        results.Add(hashtable);
                    }
                    else
                    {
                        WriteDebug($"Attempt to parse value {value} of type {value?.GetType().FullName ?? "(null)"} as hashtable failed when importing PowerShell data file at path '{filePath}'.");
                    }
                    continue;
                }
                else
                {
                    hadErrors = true;
                    var ex = new InvalidDataException(Resources.DataFileContentsInvalid);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.DataFileContentsInvalid),
                        ErrorCategory.InvalidData,
                        filePath)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.DataFileContentsInvalidInterpolated, filePath))
                    };
                    WriteError(er);
                }
            }
            return results.ToArray();
        }
        /// <summary>
        /// Writes an <see cref="ItemNotFoundException"/> with an inner exception of <see cref="FileNotFoundException"/>.
        /// The <see cref="ErrorCategoryInfo.Category"/> is <see cref="ErrorCategory.ObjectNotFound"/>.
        /// The <see cref="ErrorRecord.TargetObject"/> is <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path at which a file was expected but not found.</param>
        protected void WriteFileNotFoundError(string path)
        {
            var inner = new FileNotFoundException(Resources.FileNotFound, path);
            var ex = new ItemNotFoundException(Resources.FileNotFound, inner);
            var er = new ErrorRecord(
                    ex,
                    nameof(Resources.FileNotFound),
                    ErrorCategory.ObjectNotFound,
                    path)
            {
                ErrorDetails = new ErrorDetails(string.Format(Resources.FileNotFoundInterpolated, path))
            };
            WriteError(er);
        }
    }
}
