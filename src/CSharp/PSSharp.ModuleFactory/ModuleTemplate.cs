using PSSharp.ModuleFactory.Commands;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSSharp.ModuleFactory
{
    /// <summary>
    /// Wrapper for files containing the data required for a module template. If the files are temporary
    /// (such as if they were restored from compression or an archive), disposing this instance must
    /// remove the temporary files.
    /// </summary>
    public interface ITemplateContents : IDisposable
    {
        /// <summary>
        /// The module template metadata.
        /// </summary>
        ModuleTemplateMetadata TemplateMetadata { get; }

        /// <summary>
        /// The entire module template, which is not loaded unless requested.
        /// </summary>
        Lazy<ConfigurationModuleTemplate> ModuleTemplate { get; }

        /// <summary>
        /// The directory of all template files.
        /// </summary>
        string TemplateDirectory { get; }
        /// <summary>
        /// Files paths to all files included in the template.
        /// </summary>
        IReadOnlyList<string> TemplateFilePaths { get; }
    }
    /// <summary>
    /// Repository that is used to manage the available module templates.
    /// </summary>
    public interface IModuleTemplateRepository
    {
        /// <summary>
        /// List all registered module templates on the system.
        /// </summary>
        /// <returns></returns>
        List<ModuleTemplateMetadata> ListTemplates();
        /// <summary>
        /// Get data about a single module template by id.
        /// </summary>
        /// <param name="templateId">The <see cref="ModuleTemplateMetadata.TemplateId"/> of the template to
        /// attempt to retreive.</param>
        /// <returns>The module template if it exists, or <see langword="null"/>.</returns>
        ModuleTemplateMetadata? GetTemplate(Guid templateId);

        /// <summary>
        /// Gets files for a module template. As the implementation of file storage is up to the implementation, the
        /// files may be temporarily restored from another data source for the lifetime of this instance, in which case
        /// disposing of this instance should clear out the files. Therefore it is important to retain this object
        /// for the entire duration that the files are needed.
        /// </summary>
        /// <param name="templateId">The <see cref="ModuleTemplateMetadata.TemplateId"/> of the template
        /// to retrieve files related to.</param>
        /// <returns>An instance that indicates the current location for the files that make up the module template.</returns>
        ITemplateContents? GetTemplateContents(Guid templateId);
        /// <summary>
        /// Create a module template from the files within a source directory.
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <returns></returns>
        bool TryCreateModuleTemplate(string sourcePath, [MaybeNullWhen(false)] out ModuleTemplateMetadata metadata, Cmdlet? messageHost = null);
        /// <summary>
        /// Create a module template from the files within a source directory. This overload includes defined
        /// metadata that has already been created such as from a pre-existing template, which may be used to
        /// load a template that was created by another machine so that metadata (such as the 
        /// <see cref="ModuleTemplateMetadata.TemplateId"/> will be the same).
        /// </summary>
        /// <param name="sourcePath">The path where the source files for the template currently exist.</param>
        /// <param name="metadata">Metadata for template that was previously generated for the template.</param>
        /// <returns></returns>
        bool TryCreateModuleTemplate(string sourcePath, ModuleTemplateMetadata metadata, Cmdlet? messageHost = null);
        /// <summary>
        /// Removes a template from the repository.
        /// </summary>
        /// <param name="templateId"></param>
        /// <exception cref="ItemNotFoundException"/>
        void DeleteTemplate(Guid templateId);
    }
    
    /// <summary>
    /// Offers completion results for <see cref="ConfigurationModuleTemplate"/>s.
    /// </summary>
    public sealed class ModuleTemplateCompletionAttribute : ArgumentCompleterAttribute, IArgumentCompleter
    {
        public ModuleTemplateCompletionAttribute()
        : base(typeof(ModuleTemplateCompletionAttribute))
        {

        }
        public IEnumerable<CompletionResult> CompleteArgument(string commandName, string parameterName, string wordToComplete, CommandAst commandAst, IDictionary fakeBoundParameters)
        {
            var templates = ModuleFactoryCmdlet.Repository.ListTemplates();
            if (templates.Count == 0) yield break;
            char? wrapQuotes = null;
            if (wordToComplete.StartsWith("'"))
            {
                wrapQuotes = '\'';
                wordToComplete = wordToComplete.Trim(wrapQuotes.Value);
            }
            else if (wordToComplete.StartsWith("\""))
            {
                wrapQuotes = '"';
                wordToComplete = wordToComplete.Trim(wrapQuotes.Value);
            }
            var wc = WildcardPattern.Get(wordToComplete + "*", WildcardOptions.IgnoreCase);
            foreach (var template in templates)
            {
                switch (parameterName.ToLower())
                {
                    case "id":
                    case "templateid":
                        {
                            if (wc.IsMatch(template.TemplateId.ToString()))
                            {
                                string safeTemplateId;
                                if (wrapQuotes == '\'')
                                {
                                    safeTemplateId = $"'{template.TemplateId}'";
                                }
                                else if (wrapQuotes == '"')
                                {
                                    safeTemplateId = $"\"{template.TemplateId}\"";
                                }
                                else
                                {
                                    safeTemplateId = template.TemplateId.ToString();
                                }
                                var fullTemplateName = $"{template.Name} ({template.Version})";
                                yield return new CompletionResult(safeTemplateId, fullTemplateName, CompletionResultType.ParameterValue, template.TemplateId + ": " + fullTemplateName);
                            }
                        }
                        break;
                    case "name":
                    case "template":
                    case "templatename":
                    default:
                        {
                            if (wc.IsMatch(template.Name))
                            {
                                string safeTemplateName;
                                if (wrapQuotes == '\'')
                                {
                                    safeTemplateName = $"'{CodeGeneration.EscapeSingleQuotedStringContent(template.Name)}'";
                                }
                                else if (wrapQuotes == '"')
                                {
                                    safeTemplateName = $"\"{CodeGeneration.EscapeVariableName(template.Name?.Replace("\"", "`\""))}\"";
                                }
                                else if (!wrapQuotes.HasValue && (template.Name?.Contains(" ") ?? false))
                                {
                                    safeTemplateName = $"'{CodeGeneration.EscapeSingleQuotedStringContent(template.Name)}'";
                                }
                                else
                                {
                                    safeTemplateName = template.Name ?? template.TemplateId.ToString();
                                }

                                var fullTemplateName = $"{template.Name} ({template.Version})";
                                yield return new CompletionResult(safeTemplateName, fullTemplateName, CompletionResultType.ParameterValue, template.TemplateId + ": " + fullTemplateName);
                            }
                        }
                        break;
                    case "version":
                    case "templateversion":
                        {
                            if (template.Version is null) continue;
                            if (!wc.IsMatch(template.Version.ToString())) continue;
                            string? matchTemplateName = null;
                            Guid? matchTemplateId = null;
                            if (fakeBoundParameters.Contains("Name"))
                            {
                                matchTemplateName = LanguagePrimitives.ConvertTo<string>(fakeBoundParameters["Name"]);
                            }
                            if (fakeBoundParameters.Contains("TemplateId")
                                && LanguagePrimitives.TryConvertTo<Guid>(fakeBoundParameters["TemplateId"], out var templateId))
                            {
                                matchTemplateId = templateId;
                            }

                            if (matchTemplateId.HasValue && matchTemplateId.Value != template.TemplateId)
                            {
                                continue;
                            }
                            if(matchTemplateName is not null && !matchTemplateName.Equals(template.Name, StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            string safeTemplateVersion;
                            if (wrapQuotes == '\'')
                            {
                                safeTemplateVersion = $"'{template.Version}'";
                            }
                            else if (wrapQuotes == '"')
                            {
                                safeTemplateVersion = $"\"{template.Version}\"";
                            }
                            else
                            {
                                safeTemplateVersion = template.Version.ToString();
                            }

                            var fullTemplateName = $"{template.Name} ({template.Version})";
                            yield return new CompletionResult(safeTemplateVersion, fullTemplateName, CompletionResultType.ParameterValue, template.TemplateId + ": " + fullTemplateName);
                        }
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Metadata about a <see cref="ConfigurationModuleTemplate"/>.
    /// </summary>
    public class ModuleTemplateMetadata
    {
        [MaybeNull]
        public string Name { get; set; }
        [MaybeNull]
        [JsonConverter(typeof(JsonStringVersionConverter))]
        public Version Version { get; set; }
        public Guid TemplateId { get; set; }
        public string? Description { get; set; }

        public override string ToString()
        {
            if (Version is null)
            {
                return Name ?? "Unnamed Template";
            }
            else
            {
                return $"{Name} ({Version})";
            }
        }
    }

    /// <summary>
    /// Transforms the value of a <see cref="ModuleTemplateMetadata.Name"/> or
    /// <see cref="ModuleTemplateMetadata.TemplateId"/> to the corresponding <see cref="ModuleTemplateMetadata"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class ModuleTemplateMetadataTransformationAttribute : ArgumentTransformationAttribute
    {
        public bool SupportWildcards { get; set; }
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            ModuleTemplateMetadata? match = null;
            if (inputData is ModuleTemplateMetadata)
            {
                return inputData;
            }
            if (inputData is string nameOrId)
            {
                var templates = ModuleFactoryCmdlet.Repository.ListTemplates();
                for (int i = 0; i < templates.Count; i++)
                {
                    var template = templates[i];
                    if (nameOrId.Equals(template.Name, StringComparison.OrdinalIgnoreCase)
                        || nameOrId.Equals(template.TemplateId.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        if (match is null)
                        {
                            throw new ArgumentTransformationMetadataException(Resources.AmbiguousTemplateName);
                        }
                        match = template;
                    }
                }
                return match ?? inputData;
            }
            else if (inputData is Guid id)
            {
                var metadata = ModuleFactoryCmdlet.Repository.GetTemplate(id);
                return metadata ?? inputData;
            }
            else
            {
                return inputData;
            }
        }
    }
    /// <summary>
    /// Default implementation of <see cref="IModuleTemplateRepository"/>.
    /// </summary>
    internal sealed class ModuleTemplateRepository : IModuleTemplateRepository
    {
        public ModuleTemplateRepository()
        {
            var assemVer = Assembly.GetExecutingAssembly().GetName().Version?.Major ?? 1;
            _moduleTemplateDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "PSSharp",
                "ModuleFactory",
                $"{assemVer}.0",
                "Templates");
            _moduleTemplateMetadataPath = Path.Combine(_moduleTemplateDirectory, "templates.json");
        }
        private readonly string _moduleTemplateDirectory;
        private readonly string _moduleTemplateMetadataPath;
        private string GetTemplateDirectory(Guid templateId) => Path.Combine(_moduleTemplateDirectory, templateId.ToString());
        public List<ModuleTemplateMetadata> ListTemplates()
        {
            if (!File.Exists(_moduleTemplateMetadataPath))
            {
                return new List<ModuleTemplateMetadata>();
            }
            var fileContent = File.ReadAllBytes(_moduleTemplateMetadataPath);
            var allMetadata = JsonSerializer.Deserialize<List<ModuleTemplateMetadata>>(fileContent)
                ?? new List<ModuleTemplateMetadata>();
            return allMetadata;
        }

        public ModuleTemplateMetadata? GetTemplate(Guid templateId) => ListTemplates().Find(i => i.TemplateId == templateId);


        /// <summary>
        /// Helper method to add template metadata to the templates list as well as creating the template's metadata
        /// file.
        /// </summary>
        /// <param name="metadata"></param>
        private bool TryRegisterTemplateMetadata(ModuleTemplateMetadata metadata, Cmdlet? cmdlet)
        {
            if (metadata is null) throw new ArgumentNullException(nameof(metadata));
            if (string.IsNullOrEmpty(metadata.Name))
            {
                var ex = new InvalidOperationException(Resources.TemplateNameRequired);
                if (cmdlet is null) throw ex;
                else
                {
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateNameRequired),
                        ErrorCategory.InvalidData,
                        metadata);
                    cmdlet.WriteError(er);
                    return false;
                }
            }
            if (metadata.Version is null) metadata.Version = new Version(1, 0, 0);
            if (metadata.TemplateId == default) metadata.TemplateId = Guid.NewGuid();

            if (!Directory.Exists(_moduleTemplateDirectory)) Directory.CreateDirectory(_moduleTemplateDirectory);
            List<ModuleTemplateMetadata>? allMetadata = null;
            if (!File.Exists(_moduleTemplateMetadataPath)) allMetadata = new List<ModuleTemplateMetadata>();
            using var fs = new FileStream(_moduleTemplateMetadataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            if (allMetadata is null)
            {
                using var ms = new MemoryStream();
                fs.CopyTo(ms);
                allMetadata = JsonSerializer.Deserialize<List<ModuleTemplateMetadata>>(ms.ToArray())
                    ?? new List<ModuleTemplateMetadata>();
            }
            else
            {
                // since we create the file and might fail, we need to make sure that if we do we'll leave the file
                // in a valid json state for a future call
                var bytes = Encoding.UTF8.GetBytes("[]");
                fs.Write(bytes);
            }
            if (allMetadata.Exists(i => i.TemplateId == metadata.TemplateId))
            {
                var ex = new InvalidOperationException(Resources.RepositoryTemplateIdConflict);
                if (cmdlet is null) throw ex;
                else
                {
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.RepositoryTemplateIdConflict),
                        ErrorCategory.InvalidOperation,
                        metadata)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.RepositoryTemplateIdConflictInterpolated, metadata.TemplateId))
                    };
                    cmdlet.WriteError(er);
                    return false;
                }
            }
            var nameVersionConflict = allMetadata.Exists(i =>
                metadata.Name.Equals(i.Name, StringComparison.OrdinalIgnoreCase)
                && metadata.Version.Equals(i.Version));
            if (nameVersionConflict)
            {
                var ex = new InvalidOperationException(Resources.TemplateVersionConflict);
                if (cmdlet is null) throw ex;
                else
                {
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateVersionConflict),
                        ErrorCategory.InvalidOperation,
                        metadata)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateVersionConflictInterpolated, metadata.Name, metadata.Version))
                        {
                            RecommendedAction = Resources.TemplateVersionConflictRecommendedAction
                        }
                    };
                    cmdlet.WriteError(er);
                    return false;
                }
            }
            allMetadata.Add(metadata);
            fs.Position = 0;
            using var writer = new Utf8JsonWriter(fs);
            JsonSerializer.Serialize(writer, allMetadata);
            return true;
        }

        public ITemplateContents? GetTemplateContents(Guid templateId)
        {
            try
            {
                return new TemplateContents(this, templateId);
            }
            catch (ItemNotFoundException)
            {
                return null;
            }
        }

        public bool TryCreateModuleTemplate(string sourcePath, [MaybeNullWhen(false)] out ModuleTemplateMetadata metadata, Cmdlet? messageHost = null)
        {
            metadata = null;
            var templateFile = Path.Combine(sourcePath, "template.psd1");
            if (!File.Exists(templateFile))
            {
                var ex = new ItemNotFoundException(Resources.TemplateDataFileNotFound);
                if (messageHost is null) throw ex;
                else
                {
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.TemplateDataFileNotFound),
                        ErrorCategory.ObjectNotFound,
                        templateFile)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.TemplateDataFileNotFoundInterpolated, templateFile))
                    };
                    messageHost.WriteError(er);
                    return false;
                }
            }
            var data = Parser.ParseFile(templateFile, out _, out var errors);
            if (errors.Length > 0)
            {
                var ex = new ParseException(errors);
                if (messageHost is null) throw ex;
                else
                {
                    var er = new ErrorRecord(ex,
                        nameof(ParseException),
                        ErrorCategory.ParserError,
                        templateFile);
                    messageHost.WriteError(er);
                    return false;
                }
            }
            var hashtableData = data.Find<HashtableAst>(false);
            if (hashtableData is null)
            {
                var ex = new InvalidDataException(Resources.DataFileContentsInvalid);
                if (messageHost is null) throw ex;
                else
                {
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.DataFileContentsInvalid),
                        ErrorCategory.InvalidData,
                        templateFile)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.DataFileContentsInvalidInterpolated, templateFile))
                    };
                    messageHost.WriteError(er);
                    return false;
                }
            }

            object hashtable;
            try
            {
                hashtable = hashtableData.SafeGetValue();
            }
            catch (InvalidOperationException inner)
            {
                if (messageHost is null) throw;
                else
                {
                    var ex = new InvalidDataException(Resources.ValueNotConstant, inner);
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.ValueNotConstant),
                        ErrorCategory.InvalidData,
                        templateFile
                        )
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.ValueNotConstantInterpolated, templateFile))
                    };
                    messageHost.WriteError(er);
                    return false;
                }
            }

            if (!LanguagePrimitives.TryConvertTo<ConfigurationModuleTemplate>(hashtable, out var template))
            {
                var ex = new InvalidCastException(Resources.DataFileCastError);
                if (messageHost is null) throw ex;
                else
                {
                    var er = new ErrorRecord(
                        ex,
                        nameof(Resources.DataFileCastError),
                        ErrorCategory.InvalidData,
                        hashtable)
                    {
                        ErrorDetails = new ErrorDetails(string.Format(Resources.DataFileCastErrorInterpolated, templateFile, typeof(ConfigurationModuleTemplate)))
                    };
                    messageHost.WriteError(er);
                    return false;
                }
            }

            var context = new ValidationContext(template);
            var validationErrors = new List<ValidationResult>();
            if (!Validator.TryValidateObject(template, context, validationErrors))
            {
                var ex = new AggregateException(validationErrors.Select(static v => new ValidationException(v.ErrorMessage)));
                if (messageHost is null) throw ex.GetBaseException();
                else
                {
                    var er = new ErrorRecord(
                        ex.GetBaseException(),
                        nameof(Resources.ModuleTemplateValidationException),
                        ErrorCategory.InvalidData,
                        template)
                    {
                        ErrorDetails = new ErrorDetails(Resources.ModuleTemplateValidationException)
                    };
                    messageHost.WriteError(er);
                    return false;
                }
            }

            metadata = new ModuleTemplateMetadata()
            {
                Description = template.Description,
                Name = template.Name ?? throw new InvalidDataException(),
                TemplateId = Guid.NewGuid(),
                Version = template.Version ?? new Version(1, 0, 0)
            };

            return TryCreateModuleTemplate(sourcePath, metadata, messageHost);
        }
        public bool TryCreateModuleTemplate(string sourcePath, ModuleTemplateMetadata metadata, Cmdlet? messageHost = null)
        {
            if (!TryRegisterTemplateMetadata(metadata, messageHost))
            {
                return false;
            }

            CopyFileSystemData(sourcePath, GetTemplateDirectory(metadata.TemplateId), messageHost);

            return true;
        }

        public void DeleteTemplate(Guid templateId)
        {
            var templateDir = GetTemplateDirectory(templateId);
            var templateExists = Directory.Exists(templateDir);
            if (!templateExists)
            {
                throw new ItemNotFoundException(Resources.TemplateNotFound);
            }
            using var templateMetadataFs = new FileStream(_moduleTemplateMetadataPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Directory.Delete(templateDir, true);
            using var ms = new MemoryStream();
            templateMetadataFs.CopyTo(ms);
            var allMetadata = JsonSerializer.Deserialize<List<ModuleTemplateMetadata>>(ms.ToArray())
                ?? new List<ModuleTemplateMetadata>();
            allMetadata.RemoveAll(i => i.TemplateId == templateId);
            templateMetadataFs.Position = 0;
            using var writer = new Utf8JsonWriter(templateMetadataFs);
            JsonSerializer.Serialize(writer, allMetadata);
            templateMetadataFs.SetLength(templateMetadataFs.Position);
        }

        private static void CopyFileSystemData(string sourceDirectory, string destinationDirectory, Cmdlet? verboseCallback)
        {
            if (!Directory.Exists(sourceDirectory)) throw new DirectoryNotFoundException();
            if (!Directory.Exists(destinationDirectory)) Directory.CreateDirectory(destinationDirectory);

            var sourceDir = new DirectoryInfo(sourceDirectory);

            foreach (var fileInfo in sourceDir.GetFiles())
            {
                verboseCallback?.WriteVerbose(string.Format(Resources.UploadingTemplateFileVerbose, fileInfo.FullName));
                verboseCallback?.WriteDebug($"Placing file at path '{Path.Combine(destinationDirectory, fileInfo.Name)}'.");
                var destination = Path.Combine(destinationDirectory, fileInfo.Name);
                File.Copy(fileInfo.FullName, destination);
            }

            foreach (var dir in sourceDir.GetDirectories())
            {
                var destination = Path.Combine(destinationDirectory, dir.Name);
                Directory.CreateDirectory(destination);
                CopyFileSystemData(dir.FullName, destination, verboseCallback);
            }
        }


        private class TemplateContents : ITemplateContents
        {
            /// <exception cref="ItemNotFoundException"/>
            public TemplateContents(ModuleTemplateRepository repository, Guid templateId)
            {
                TemplateMetadata = repository.GetTemplate(templateId) ?? throw new ItemNotFoundException();
                TemplateDirectory = repository.GetTemplateDirectory(templateId);
                var filePaths = new List<string>(32);
                ListFiles(filePaths, TemplateDirectory);
                TemplateFilePaths = filePaths;

                static void ListFiles(List<string> list, string directory)
                {
                    foreach (var item in Directory.GetFiles(directory))
                    {
                        list.Add(item);
                    }
                    foreach (var dir in Directory.GetDirectories(directory))
                    {
                        ListFiles(list, dir);
                    }
                }

                ModuleTemplate = new Lazy<ConfigurationModuleTemplate>(LoadModuleTemplate);
            }
            private ConfigurationModuleTemplate LoadModuleTemplate()
            {
                var templatePath = Path.Combine(TemplateDirectory, "template.psd1");
                var fileAst = Parser.ParseFile(templatePath, out _, out var errors);
                if (errors.Length > 0) throw new ParseException(errors);
                var hashtableAst = fileAst.Find<HashtableAst>(false);
                if (hashtableAst is null) throw new InvalidDataException(Resources.DataFileContentsInvalid);
                var hashtable = hashtableAst.SafeGetValue();
                var template = LanguagePrimitives.ConvertTo<ConfigurationModuleTemplate>(hashtable);
                return template;
            }


            public ModuleTemplateMetadata TemplateMetadata { get; }

            public Lazy<ConfigurationModuleTemplate> ModuleTemplate { get; }

            public string TemplateDirectory { get; }

            public IReadOnlyList<string> TemplateFilePaths { get; }

            public void Dispose()
            {
            }
        }
    }
    /// <summary>
    /// Extensions for <see cref="IModuleTemplateRepository"/>.
    /// </summary>
    public static class ModuleTemplateRepositoryExtensions
    {
        /// <summary>
        /// Gets data about a single module template by name. If multiple templates exist with the same name,
        /// the newest version will be identified and returned.
        /// </summary>
        /// <param name="name">The <see cref="ModuleTemplateMetadata.Name"/> of the template to attempt to
        /// retreive, or the full string representation of the template (which is the name and version).</param>
        /// <returns>The newest version of the module template if it exists, or <see langword="null"/>.</returns>
        public static List<ModuleTemplateMetadata> ListTemplates(this IModuleTemplateRepository repository, string name, bool expandWildcards, string? versionString)
        {
            var templates = repository.ListTemplates();
            if (expandWildcards)
            {
                var wc = WildcardPattern.Get(name, WildcardOptions.IgnoreCase);
                var versionWc = versionString is null
                    ? WildcardPattern.Get("*", WildcardOptions.None)
                    : WildcardPattern.Get(versionString, WildcardOptions.IgnoreCase);
                return templates.FindAll(i => wc.IsMatch(i.Name) && versionWc.IsMatch(i.Version?.ToString() ?? string.Empty));
            }
            else
            {
                return templates.FindAll(i => name.Equals(i.Name, StringComparison.OrdinalIgnoreCase)
                    && (versionString is null || versionString.Equals(i.Version?.ToString())));
            }
        }
    }
}
