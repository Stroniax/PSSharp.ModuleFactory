using Microsoft.PowerShell.Commands;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;

namespace PSSharp.ModuleFactory.Commands
{
    [Cmdlet(VerbsLifecycle.Build, ScriptModuleNoun,
        SupportsShouldProcess = true,
        RemotingCapability = RemotingCapability.PowerShell,
        DefaultParameterSetName = nameof(Path))]
    [OutputType(typeof(BuildScriptModuleResult))]
    public sealed partial class BuildScriptModuleCommand : ModuleFactoryCmdlet
    {
        public const string ScriptModuleNoun = "ScriptModule";

        private readonly HashSet<string> _resolvedPaths = new(StringComparer.OrdinalIgnoreCase);
        private string[] _path = null!;
        private bool _isLiteralPath;

        /// <summary>
        /// Paths to source files to use in the module files.
        /// </summary>
        [Parameter(Position = 0, Mandatory = true, ParameterSetName = nameof(Path),
            HelpMessageBaseName = ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageBuildScriptModulePath))]
        [Alias("FilePath")]
        [SupportsWildcards]
        public string[] Path
        {
            get => _path;
            set => (_path, _isLiteralPath) = (value, false);
        }
        /// <summary>
        /// Paths to source files to use in the module files. Wildcards passed to the LiteralPath parameter
        /// will not be trated as wildcards.
        /// </summary>
        [Parameter(ValueFromPipelineByPropertyName = true, Mandatory = true, ParameterSetName = nameof(LiteralPath),
            HelpMessageBaseName = ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageBuildScriptModuleLiteralPath))]
        [Alias("PSPath")]
        public string[] LiteralPath
        {
            get => _path;
            set => (_path, _isLiteralPath) = (value, true);
        }

        /// <summary>
        /// The path that the resulting script module ('.psm1') will be created at.
        /// </summary>
        [Parameter(Position = 1, Mandatory = true,
            ParameterSetName = nameof(Path),
            HelpMessageBaseName = ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageBuildScriptModuleDestinationPath))]
        [Parameter(Position = 0, Mandatory = true,
            ParameterSetName = nameof(LiteralPath),
            HelpMessageBaseName = ResourceBaseName,
            HelpMessageResourceId = nameof(Resources.HelpMessageBuildScriptModuleDestinationPath))]
        [Alias("DestinationPath")]
        public string OutputPath { get; set; } = null!;

        /// <summary>
        /// Add comments to functions in the resultant file indicating the source file used for the function.
        /// </summary>
        [Parameter]
        [PSDefaultValue(Value = SourceFileTraceLevel.FileNameLineNumber)]
        public SourceFileTraceLevel FunctionSourceTrace { get; set; } = SourceFileTraceLevel.FileNameLineNumber;
        /// <summary>
        /// Add comments to classes in the resultant file indicating the source file used for the class.
        /// </summary>
        [Parameter]
        [PSDefaultValue(Value = SourceFileTraceLevel.FileNameLineNumber)]
        public SourceFileTraceLevel ClassSourceTrace { get; set; } = SourceFileTraceLevel.FileNameLineNumber;
        /// <summary>
        /// Add comments to statements in the resultant file indicating the source file used for the statement.
        /// </summary>
        [Parameter]
        [PSDefaultValue(Value = SourceFileTraceLevel.None)]
        public SourceFileTraceLevel StatementSourceTrace { get; set; } = SourceFileTraceLevel.None;
        /// <summary>
        /// Rebuild the result file even if all source files have not changed since the source file was previously built.
        /// </summary>
        [Parameter]
        public SwitchParameter Force { get; set; }

        /// <summary>
        /// Fail if the destination file exists.
        /// </summary>
        [Parameter]
        public SwitchParameter NoClobber { get; set; }

        private void WriteProgress(string statusDescription, string? currentOperation)
        {
            var progress = new ProgressRecord(1, "Building script module", statusDescription)
            {
                CurrentOperation = currentOperation
            };
            WriteProgress(progress);
        }
        private void WriteProgress(string statusDescription, string? currentOperation, int processedFileCount, int totalFileCount)
        {
            var percentComplete = (int)Math.Min(100, Math.Truncate((double)processedFileCount / totalFileCount));
            var progress = new ProgressRecord(1, "Building script module", statusDescription)
            {
                CurrentOperation = currentOperation,
                PercentComplete = percentComplete,
            };
            WriteProgress(progress);
        }
        private void WriteProgressCompleted()
        {
            WriteProgress(
                new ProgressRecord(
                1,
                "Building script module",
                "Build complete")
                {
                    PercentComplete = 100,
                    RecordType = ProgressRecordType.Completed
                }
                );
        }
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            foreach (var path in Path)
            {
                WriteProgress("Identifying source files", $"Resolving path '{path}'.");
                var resolvedPaths = ResolvePath(path, _isLiteralPath, out _, true, true);
                foreach (var resolvedPath in resolvedPaths) _resolvedPaths.Add(resolvedPath);
            }
        }
        protected override void EndProcessing()
        {
            if (_resolvedPaths.Count == 0)
            {
                WriteProgressCompleted();
                WriteWarning(Resources.BuildScriptModuleWarningNoFiles);
                return;
            }
            var destinationPath = GetUnresolvedProviderPathFromPSPath(OutputPath);

            if (_resolvedPaths.Contains(destinationPath))
            {
                WriteProgressCompleted();
                var ex = new ArgumentException(Resources.IncludeSourceFileInDestination);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.IncludeSourceFileInDestination),
                    ErrorCategory.InvalidArgument,
                    destinationPath)
                {
                    ErrorDetails = new ErrorDetails(this, ResourceBaseName, nameof(Resources.IncludeSourceFileInDestinationInterpolated), destinationPath)
                    {
                        RecommendedAction = string.Format(Resources.IncludeSourceFileInDestionationRecommendedAction, MyInvocation.MyCommand.Name)
                    }
                };
                WriteError(er);
                return;
            }

            if (NoClobber && File.Exists(destinationPath))
            {
                WriteProgressCompleted();
                var ex = new InvalidDataException(Resources.FileExists);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.FileExists),
                    ErrorCategory.InvalidData,
                    destinationPath)
                {
                    ErrorDetails = new ErrorDetails(this, ResourceBaseName, nameof(Resources.FileExistsInterpolated), destinationPath)
                };
                WriteError(er);
                return;
            }

            if (!Force && File.Exists(destinationPath))
            {
                var moduleWriteTime = File.GetLastWriteTime(destinationPath);
                var sourceWriteTime = default(DateTime);
                foreach (var sourceFile in _resolvedPaths)
                {
                    var sourceFileWriteTime = File.GetLastWriteTime(sourceFile);
                    if (sourceWriteTime < sourceFileWriteTime)
                    {
                        sourceWriteTime = sourceFileWriteTime;
                    }
                    if (sourceFileWriteTime > moduleWriteTime) break;
                }
                if (sourceWriteTime < moduleWriteTime)
                {
                    var fileAst = Parser.ParseFile(destinationPath, out _, out var prebuiltErrors);

                    // rebuild if the current file cannot be parsed
                    if (prebuiltErrors.Length == 0)
                    {
                        WriteWarning(string.Format(Resources.BuildScriptModuleWarningUpToDate, destinationPath));
                        WriteProgress("Generating result", "Identifying requirements and functions from the existing script AST.");
                        var pseudoResult = new BuildScriptModuleResult(fileAst);
                        WriteProgressCompleted();
                        WriteObject(pseudoResult);
                        return;
                    }
                }
            }

            bool hadErrors = false;
            ParamBlockAst? paramBlockAst = null;
            NamedBlockAst? dynamicParamBlockAst = null;
            var beginBlockStatements = new StatementAstCollection();
            var processBlockStatements = new StatementAstCollection();
            var endBlockStatements = new StatementAstCollection();

            var usingStatements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var requiredAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var requiredModules = new HashSet<ModuleSpecification>();
            var requiresSnapIns = new HashSet<PSSnapInSpecification>();
            var functionNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var classNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, IList<string>> aliasNames = new(StringComparer.OrdinalIgnoreCase);
            var isElevationRequired = false;
            string? requiredApplicationId = null;
            IEnumerable<string>? requiredPSEditions = null;
            Version? requiredPSVersion = null;

            int index = 0;
            foreach (var scriptFile in _resolvedPaths)
            {
                WriteProgress("Parsing source files", $"Reading file {++index} of {_resolvedPaths.Count}\n{scriptFile}", index, _resolvedPaths.Count);
                WriteDebug(string.Format(Resources.BuildScriptModuleDebugReadingFile, scriptFile));
                var ast = Parser.ParseFile(scriptFile, out _, out var scriptFileErrors);
                if (scriptFileErrors is not null && scriptFileErrors.Length > 0)
                {
                    hadErrors = true;
                    var ex = new ParseException(scriptFileErrors);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.ParseError),
                        ErrorCategory.ParserError,
                        scriptFile)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.ParseErrorInterpolated, scriptFile))
                    };
                    WriteError(er);
                    continue;
                }

                if (ast.ScriptRequirements is not null)
                {
                    WriteDebug(Resources.BuildScriptModuleDebugScriptRequirements);
                    if (ast.ScriptRequirements.IsElevationRequired) isElevationRequired = true;
                    if (!string.IsNullOrWhiteSpace(ast.ScriptRequirements.RequiredApplicationId))
                    {
                        if (!string.IsNullOrWhiteSpace(requiredApplicationId) &&
                            !requiredApplicationId.Equals(ast.ScriptRequirements.RequiredApplicationId, StringComparison.OrdinalIgnoreCase))
                        {
                            hadErrors = true;
                            var ex = new InvalidOperationException(Resources.ConflictingApplicationIdRequirement);
                            var er = new ErrorRecord(
                                ex,
                                nameof(Resources.ConflictingApplicationIdRequirement),
                                ErrorCategory.InvalidOperation,
                                scriptFile)
                            {
                                ErrorDetails = new ErrorDetails(string.Format(Resources.ConflictingApplicationIdRequirementInterpolated, scriptFile))
                                {
                                    RecommendedAction = Resources.ConflictingApplicationIdRequirementRecommendedAction
                                }
                            };
                            WriteError(er);
                        }
                        else
                        {
                            requiredApplicationId = ast.ScriptRequirements.RequiredApplicationId;
                        }
                    }
                    foreach (var requiredAssembly in ast.ScriptRequirements.RequiredAssemblies)
                    {
                        requiredAssemblies.Add(requiredAssembly);
                    }
                    foreach (var requiredModule in ast.ScriptRequirements.RequiredModules)
                    {
                        requiredModules.Add(requiredModule);
                    }
                    if (ast.ScriptRequirements.RequiredPSEditions?.Count > 0)
                    {
                        if (requiredPSEditions is null)
                        {
                            requiredPSEditions = ast.ScriptRequirements.RequiredPSEditions;
                        }
                        else if (!requiredPSEditions.SequenceEqual(ast.ScriptRequirements.RequiredPSEditions, StringComparer.OrdinalIgnoreCase))
                        {
                            hadErrors = true;
                            var ex = new InvalidOperationException(Resources.ConflictingPSEditionRequirement);
                            var er = new ErrorRecord(
                                ex,
                                nameof(Resources.ConflictingPSEditionRequirement),
                                ErrorCategory.InvalidOperation,
                                scriptFile)
                            {
                                ErrorDetails = new ErrorDetails(string.Format(Resources.ConflictingPSEditionRequirementInterpolated, scriptFile))
                                {
                                    RecommendedAction = Resources.ConflictingPSEditionRequirementRecommendedAction
                                }
                            };
                            WriteError(er);
                        }
                    }
                    if (ast.ScriptRequirements.RequiredPSVersion is not null)
                    {
                        if (requiredPSVersion is null || requiredPSVersion < ast.ScriptRequirements.RequiredPSVersion)
                        {
                            requiredPSVersion = ast.ScriptRequirements.RequiredPSVersion;
                        }
                    }
                    foreach (var snapin in ast.ScriptRequirements.RequiresPSSnapIns)
                    {
                        requiresSnapIns.Add(snapin);
                    }
                }

                var functionAsts = ast.FindAll<FunctionDefinitionAst>(false);
                foreach (var functionAst in functionAsts)
                {
                    if (functionAst.Parent is not FunctionMemberAst)
                    {
                        functionNames.Add(functionAst.Name);
                        RegisterAliases(aliasNames, functionAst);
                    }
                }
                var typeAsts = ast.FindAll<TypeDefinitionAst>(true);
                foreach (var typeAst in typeAsts)
                {
                    classNames.Add(typeAst.Name);
                }
                foreach (var usingStatement in ast.UsingStatements)
                {
                    if (usingStatement.UsingStatementKind == UsingStatementKind.Module)
                    {
                        if (MatchesAnyRelativePath(scriptFile, usingStatement.Name.Value, _resolvedPaths))
                        {
                            WriteDebug(string.Format(Resources.BuildScriptModuleDebugIgnoredUsingModule, usingStatement.Name.Extent.Text, scriptFile));
                            continue;
                        }
                    }
                    usingStatements.Add(usingStatement.Extent.Text);
                }

                if (ast.ParamBlock is not null)
                {
                    if (paramBlockAst is null)
                    {
                        paramBlockAst = ast.ParamBlock;
                    }
                    else
                    {
                        hadErrors = true;
                        var ex = new InvalidDataException(Resources.MultipleParamBlocksDefined);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.MultipleParamBlocksDefined),
                            ErrorCategory.InvalidData,
                            scriptFile);
                        WriteError(er);
                    }
                }
                if (ast.DynamicParamBlock is not null)
                {
                    if (dynamicParamBlockAst is null) dynamicParamBlockAst = ast.DynamicParamBlock;
                    else
                    {
                        hadErrors = true;
                        var ex = new InvalidDataException(Resources.MultipleDynamicParamBlocksDefined);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.MultipleDynamicParamBlocksDefined),
                            ErrorCategory.InvalidData,
                            scriptFile);
                        WriteError(er);
                    }
                }
                if (ast.BeginBlock is not null)
                {
                    hadErrors = true;
                    var ex = new InvalidDataException(Resources.ScriptModuleBeginBlock);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.ScriptModuleBeginBlock),
                        ErrorCategory.InvalidData,
                        scriptFile)
                    {
                        ErrorDetails = new ErrorDetails(this, ResourceBaseName, nameof(Resources.ScriptModuleBeginBlockInterpolated), scriptFile)
                        {
                            RecommendedAction = Resources.ScriptModuleSingleClauseRecommendedAction
                        }
                    };
                    WriteError(er);
                    beginBlockStatements.Add(ast.BeginBlock.Statements);
                }
                if (ast.ProcessBlock is not null)
                {
                    if (endBlockStatements.Count > 0)
                    {
                        hadErrors = true;
                        var ex = new InvalidDataException(Resources.ScriptModuleSingleClause);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.ScriptModuleSingleClause),
                            ErrorCategory.InvalidArgument,
                            scriptFile)
                        {
                            ErrorDetails = new ErrorDetails(this, ResourceBaseName, nameof(Resources.ScriptModuleSingleClauseInterpolated), scriptFile, nameof(ScriptBlockAst.EndBlock))
                            {
                                RecommendedAction = Resources.ScriptModuleSingleClauseRecommendedAction
                            }
                        };
                        WriteError(er);
                    }
                    processBlockStatements.Add(ast.ProcessBlock.Statements);
                }
                if (ast.EndBlock is not null)
                {
                    if (processBlockStatements.Count > 0)
                    {
                        hadErrors = true;
                        var ex = new InvalidDataException(Resources.ScriptModuleSingleClause);
                        var er = new ErrorRecord(
                            ex,
                            nameof(Resources.ScriptModuleSingleClause),
                            ErrorCategory.InvalidArgument,
                            scriptFile)
                        {
                            ErrorDetails = new ErrorDetails(this, ResourceBaseName, nameof(Resources.ScriptModuleSingleClauseInterpolated), scriptFile, nameof(ScriptBlockAst.ProcessBlock))
                            {
                                RecommendedAction = Resources.ScriptModuleSingleClauseRecommendedAction
                            }
                        };
                        WriteError(er);
                    }
                    endBlockStatements.Add(ast.EndBlock.Statements);
                }
            }

            if (hadErrors)
            {
                WriteVerbose(string.Format(Resources.BuildScriptModuleVerboseHadErrorsInterpolated, destinationPath));
                WriteProgressCompleted();
                return;
            }

            if (!ShouldProcess(
                string.Format(Resources.ShouldProcessBuildScriptModuleDescription, destinationPath, _resolvedPaths.Count),
                string.Format(Resources.ShouldProcessBuildScriptModuleWarning, destinationPath, _resolvedPaths.Count),
                Resources.ShouldProcessBuildScriptModuleAction))
            {
                WriteProgressCompleted();
                return;
            }

            var outputDirectory = System.IO.Path.GetDirectoryName(destinationPath);
            if (outputDirectory is not null && !Directory.Exists(outputDirectory)) Directory.CreateDirectory(outputDirectory);
            using (var outputFileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                WriteProgress("Writing script module", "Writing script requirements.");
                static string GetModuleSpecificationString(PSSnapInSpecification specification)
                {
                    if (specification.Version is null)
                    {
                        return $"'{specification.Name}'";
                    }
                    else
                    {
                        return $"'{specification.Name}' -Version {specification.Version}";
                    }
                }
                using var writer = new StreamWriter(outputFileStream);

                foreach (var usingStatement in usingStatements) writer.WriteLine(usingStatement);
                foreach (var requirement in requiredModules) writer.WriteLine("#requires -Module {0}", requirement);
                foreach (var requirement in requiredAssemblies) writer.WriteLine("#requires -Assembly '{0}'", requirement);
                foreach (var snapin in requiresSnapIns) writer.WriteLine("#requires -PSSnapIn {0}", GetModuleSpecificationString(snapin));
                if (requiredPSVersion is not null) writer.WriteLine("#requires -Version {0}", requiredPSVersion);
                if (isElevationRequired) writer.WriteLine("#requires -RunAsAdministrator");

                if (beginBlockStatements.Count > 0)
                {
                    WriteProgress("Writing script module", "Writing begin block.");
                    writer.WriteLine("begin {");
                    beginBlockStatements.WriteAllStatements(writer, FunctionSourceTrace, ClassSourceTrace, StatementSourceTrace);
                    writer.WriteLine("}");
                }

                if (processBlockStatements.Count > 0)
                {
                    WriteProgress("Writing script module", "Writing process block.");
                    writer.WriteLine("process {");
                    processBlockStatements.WriteAllStatements(writer, FunctionSourceTrace, ClassSourceTrace, StatementSourceTrace);
                    writer.WriteLine("}");
                }

                if (endBlockStatements.Count > 0)
                {
                    WriteProgress("Writing script module", "Writing end block.");
                    writer.WriteLine("end {");
                    endBlockStatements.WriteAllStatements(writer, FunctionSourceTrace, ClassSourceTrace, StatementSourceTrace);
                    writer.WriteLine("}");
                }
            }

            WriteProgress("Validating script module", null);
            Parser.ParseFile(destinationPath, out _, out var scriptModuleErrors);
            if (scriptModuleErrors?.Length > 0)
            {
                var inner = new ParseException(scriptModuleErrors);
                var ex = new ParseException(Resources.GeneratedInvalidScript, inner);
                var er = new ErrorRecord(
                    ex,
                    nameof(Resources.GeneratedInvalidScript),
                    ErrorCategory.ParserError,
                    scriptModuleErrors)
                {
                    ErrorDetails = new ErrorDetails(Resources.GeneratedInvalidScript)
                    {
                        RecommendedAction = Resources.GeneratedInvalidScriptRecommendedAction
                    }
                };
                WriteError(er);
            }

            var result = new BuildScriptModuleResult(
                destinationPath,
                functionNames.ToArray(),
                aliasNames,
                classNames.ToArray(),
                requiredAssemblies.ToArray(),
                requiredModules.ToArray(),
                requiresSnapIns.ToArray(),
                requiredPSEditions?.ToArray(),
                requiredPSVersion,
                isElevationRequired);

            WriteProgressCompleted();
            WriteObject(result);

            base.EndProcessing();
        }

        private static void RegisterAliases(IDictionary<string, IList<string>> aliases, FunctionDefinitionAst function)
        {
            if (function.Body.ParamBlock?.Attributes is null)
            {
                return;
            }
            foreach (var attributeAst in function.Body.ParamBlock.Attributes)
            {
                if (attributeAst.TypeName.GetReflectionAttributeType() == typeof(AliasAttribute))
                {
                    if (!aliases.ContainsKey(function.Name))
                    {
                        aliases.Add(function.Name, new List<string>());
                    }
                    foreach (var aliasValueAst in attributeAst.PositionalArguments)
                    {
                        // if someone provides an alias like `33` (unquoted), the value will not be a string
                        var aliasName = LanguagePrimitives.ConvertTo<string>(aliasValueAst.SafeGetValue());
                        aliases[function.Name].Add(aliasName);
                    }
                }
            }
        }
        /// <summary>
        /// Determines if <paramref name="targetPath"/> matches the <paramref name="relativePath"/> defined
        /// from within a definition of <paramref name="sourcePath"/>.
        /// </summary>
        /// <param name="sourcePath">The path in which the relative path is defined.</param>
        /// <param name="relativePath">The relative path defined within the source path that may refer to one of the <paramref name="targetPaths"/>.</param>
        /// <param name="targetPaths">The paths to compare to <paramref name="relativePath"/>.</param>
        /// <returns><see langword="true"/> if <paramref name="relativePath"/> is included in <paramref name="targetPaths"/>.</returns>
        private bool MatchesAnyRelativePath(string sourcePath, string relativePath, HashSet<string> targetPaths)
        {
            if (sourcePath is null) throw new ArgumentNullException(nameof(sourcePath));
            if (relativePath is null) throw new ArgumentNullException(nameof(relativePath));
            if (targetPaths is null) throw new ArgumentNullException(nameof(targetPaths));
            if (targetPaths.Comparer != StringComparer.OrdinalIgnoreCase) throw new ArgumentException("Expected hashset comparer to be case insensitive.");

            // set our current context to the source directory
            const string stackname = "Build-ScriptModule RelativePath";
            SessionState.Path.PushCurrentLocation(stackname);
            try
            {
                SessionState.Path.SetLocation(System.IO.Path.GetDirectoryName(sourcePath));

                var resolvedRelativePath = GetUnresolvedProviderPathFromPSPath(relativePath);

                // targetPath is already resolved so we can just determine if the paths match now
                WriteDebug($"Comparing relative path {resolvedRelativePath} from {relativePath} required by {sourcePath}.");
                return targetPaths.Contains(resolvedRelativePath);
            }
            finally
            {
                SessionState.Path.PopLocation(stackname);
            }
        }

        public sealed record BuildScriptModuleResult : IEquatable<BuildScriptModuleResult>
        {
            public BuildScriptModuleResult()
            {
                FilePath = string.Empty;
                Functions = new(Array.Empty<string>());
                Aliases = new(new Dictionary<string, ReadOnlyCollection<string>>());
                Classes = new(Array.Empty<string>());
                RequiredAssemblies = new(Array.Empty<string>());
                RequiredModules = new(Array.Empty<ModuleSpecification>());
                RequiredSnapIns = new(Array.Empty<PSSnapInSpecification>());
                RequiredPSEditions = new(Array.Empty<string>());
                IsElevationRequired = false;
            }
            public BuildScriptModuleResult(
                [NotNull] string path,
                IList<string>? functions,
                IReadOnlyDictionary<string, IList<string>> aliases,
                IList<string>? classes,
                IList<string>? requiredAssemblies,
                IList<ModuleSpecification>? requiredModules,
                IList<PSSnapInSpecification>? requiredSnapIns,
                IList<string>? requiredPSEditions,
                Version? powerShellVersion,
                bool isElevationRequired)
            {
                FilePath = path ?? throw new ArgumentNullException(nameof(path));
                Functions = new(functions ?? Array.Empty<string>());
                var tempAliases = new Dictionary<string, ReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var alias in aliases)
                {
                    tempAliases[alias.Key] = new ReadOnlyCollection<string>(alias.Value);
                }
                Aliases = new ReadOnlyDictionary<string, ReadOnlyCollection<string>>(tempAliases);
                Classes = new(classes ?? Array.Empty<string>());
                RequiredAssemblies = new(requiredAssemblies ?? Array.Empty<string>());
                RequiredModules = new(requiredModules ?? Array.Empty<ModuleSpecification>());
                RequiredSnapIns = new(requiredSnapIns ?? Array.Empty<PSSnapInSpecification>());
                RequiredPSEditions = new(requiredPSEditions ?? Array.Empty<string>());
                PowerShellVersion = powerShellVersion;
                IsElevationRequired = isElevationRequired;
            }
            public BuildScriptModuleResult(ScriptBlockAst fileAst)
            {
                FilePath = fileAst?.Extent?.StartScriptPosition?.File
                    ?? throw new ArgumentException(Resources.ScriptBlockAstNotFromFile);
                var functions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var aliases = new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);
                var classes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var functionAst in fileAst.FindAll<FunctionDefinitionAst>(false))
                {
                    if (functionAst.Parent is not FunctionMemberAst)
                    {
                        functions.Add(functionAst.Name);
                        RegisterAliases(aliases, functionAst);
                    }
                }
                foreach (var typeDefinitionAst in fileAst.FindAll<TypeDefinitionAst>(true))
                {
                    classes.Add(typeDefinitionAst.Name);
                }
                Functions = new(functions.ToArray());
                var tempAliases = new Dictionary<string, ReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var alias in aliases)
                {
                    tempAliases[alias.Key] = new(alias.Value);
                }
                Aliases = new(tempAliases);
                Classes = new(classes.ToArray());
                RequiredAssemblies = new(fileAst.ScriptRequirements?.RequiredAssemblies
                    ?? Array.Empty<string>() as IList<string>);
                RequiredModules = new(fileAst.ScriptRequirements?.RequiredModules
                    ?? Array.Empty<ModuleSpecification>() as IList<ModuleSpecification>);
                RequiredSnapIns = new(fileAst.ScriptRequirements?.RequiresPSSnapIns
                    ?? Array.Empty<PSSnapInSpecification>() as IList<PSSnapInSpecification>);
                RequiredPSEditions = new(fileAst.ScriptRequirements?.RequiredPSEditions
                    ?? Array.Empty<string>() as IList<string>);
                PowerShellVersion = fileAst.ScriptRequirements?.RequiredPSVersion;
                IsElevationRequired = fileAst.ScriptRequirements?.IsElevationRequired ?? false;
            }



            /// <summary>
            /// The full file path of the created .psm1 module.
            /// </summary>
            public string FilePath { get; }
            /// <summary>
            /// Names of all functions defined in the output module, whether or not they are exported.
            /// Nested functions (functions defined inside of another function) are not included in
            /// this list.
            /// </summary>
            public ReadOnlyCollection<string> Functions { get; }
            /// <summary>
            /// Aliases of all functions defined in the output module, whether or not the function
            /// is exported. Aliases of nested functions (functions defined inside of another function)
            /// are not included in this list.
            /// </summary>
            public ReadOnlyDictionary<string, ReadOnlyCollection<string>> Aliases { get; }
            /// <summary>
            /// Names of all classes defined in the output module.
            /// </summary>
            public ReadOnlyCollection<string> Classes { get; }
            /// <summary>
            /// Paths to assemblies required by the output module.
            /// </summary>
            public ReadOnlyCollection<string> RequiredAssemblies { get; }
            /// <summary>
            /// Values specified by `#requires -Module` statements from source files which are required by the
            /// output module.
            /// </summary>
            public ReadOnlyCollection<ModuleSpecification> RequiredModules { get; }
            /// <summary>
            /// Values specified by `#requires -PSSnapIn` statements from source files which are required by the
            /// output module.
            /// </summary>
            public ReadOnlyCollection<PSSnapInSpecification> RequiredSnapIns { get; }
            /// <summary>
            /// Values specified by `#requires -PSEdition` statements from source files which are required by the
            /// output module.
            /// </summary>
            public ReadOnlyCollection<string> RequiredPSEditions { get; }
            /// <summary>
            /// The highest version specified by `#requires -Version` statements from source files. This version
            /// is required by the output module. <see langword="null"/> if the `#requires -version` statement
            /// is not present in any source files.
            /// </summary>
            public Version? PowerShellVersion { get; }
            /// <summary>
            /// <see langword="true"/> if `#requires -RunAsAdministrator` is present in any source files.
            /// </summary>
            public bool IsElevationRequired { get; }

            bool IEquatable<BuildScriptModuleResult>.Equals(BuildScriptModuleResult? other)
            {
                return other is not null
                    && FilePath.Equals(other.FilePath, StringComparison.OrdinalIgnoreCase)
                    && PowerShellVersion == other.PowerShellVersion
                    && Functions.SequenceEqual(other.Functions, StringComparer.OrdinalIgnoreCase)
                    && Classes.SequenceEqual(other.Classes, StringComparer.OrdinalIgnoreCase)
                    && RequiredAssemblies.SequenceEqual(other.RequiredAssemblies, StringComparer.OrdinalIgnoreCase)
                    && RequiredModules.SequenceEqual(other.RequiredModules, ModuleSpecificationComparer.Comparer)
                    && RequiredSnapIns.SequenceEqual(other.RequiredSnapIns, PSSnapInSpecificationComparer.Comparer)
                    && RequiredPSEditions.SequenceEqual(other.RequiredPSEditions, StringComparer.OrdinalIgnoreCase);
            }
            /// <summary>
            /// Returns the <see cref="FilePath"/> of the current instance.
            /// </summary>
            /// <returns><see cref="FilePath"/></returns>
            public override string ToString() => FilePath;
        }
        internal class StatementAstCollection
        {
            private readonly List<FunctionDefinitionAst> _functionDefinitionAsts = new();
            private readonly List<TypeDefinitionAst> _typeDefinitionAsts = new();
            private readonly List<StatementAst> _statementAsts = new();
            public int Count
                => _functionDefinitionAsts.Count
                + _typeDefinitionAsts.Count
                + _statementAsts.Count;

            public void Add(params StatementAst[] statements)
                => Add(statements as IEnumerable<StatementAst>);
            public void Add(IEnumerable<StatementAst> statements)
            {
                foreach (var statement in statements)
                {
                    switch (statement)
                    {
                        case FunctionDefinitionAst f:
                            _functionDefinitionAsts.Add(f);
                            break;
                        case TypeDefinitionAst t:
                            _typeDefinitionAsts.Add(t);
                            break;
                        default:
                            _statementAsts.Add(statement);
                            break;
                    }
                }
            }
            private static void WriteStatement(StreamWriter writer, StatementAst statement, SourceFileTraceLevel traceLevel)
            {
                if (traceLevel == SourceFileTraceLevel.FileNameLineNumber || traceLevel == SourceFileTraceLevel.FileName)
                    writer.WriteLine("### File: {0}", System.IO.Path.GetFileName(statement.Extent.StartScriptPosition.File));
                if (traceLevel == SourceFileTraceLevel.FilePathLineNumber || traceLevel == SourceFileTraceLevel.FilePath)
                    writer.WriteLine("### File: {0}", statement.Extent.StartScriptPosition.File);
                if (traceLevel == SourceFileTraceLevel.FileNameLineNumber || traceLevel == SourceFileTraceLevel.FilePathLineNumber)
                    writer.WriteLine("### Start Line: {0}\n###End Line: {1}", statement.Extent.StartLineNumber, statement.Extent.EndLineNumber);
                writer.WriteLine(statement.Extent.Text);
            }
            public void WriteAllStatements(StreamWriter writer, SourceFileTraceLevel functionTraceLevel, SourceFileTraceLevel classTraceLevel, SourceFileTraceLevel statementTraceLevel)
            {
                if (_functionDefinitionAsts.Count > 0)
                {
                    writer.WriteLine("#region Functions");
                    foreach (var statement in _functionDefinitionAsts.OrderBy(i => i.Name))
                    {
                        WriteStatement(writer, statement, functionTraceLevel);
                    }
                    writer.WriteLine("#endregion");
                }
                if (_typeDefinitionAsts.Count > 0)
                {
                    writer.WriteLine("#region Classes");
                    foreach (var statement in _typeDefinitionAsts.OrderBy(i => i.Name))
                    {
                        WriteStatement(writer, statement, classTraceLevel);
                    }
                    writer.WriteLine("#endregion");
                }
                if (_statementAsts.Count > 0)
                {
                    writer.WriteLine("#region Misc Statements");
                    foreach (var statement in _statementAsts)
                    {
                        WriteStatement(writer, statement, statementTraceLevel);
                    }
                    writer.WriteLine("#endregion Misc Statements");
                }
            }
        }
        public enum SourceFileTraceLevel
        {
            None,
            FileName,
            FilePath,
            FileNameLineNumber,
            FilePathLineNumber,
        }
    }
}
