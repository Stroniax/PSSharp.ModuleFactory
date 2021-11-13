using System.Collections.ObjectModel;
using System.Text;

#pragma warning disable CS8618

namespace PSSharp.ModuleFactory
{
    public enum ConfigurationItemType
    {
        Undefined,
        Variable,
        Argument,
        Parameters,
        Command,
        DynamicParameter,
        Document,
    }

    public class ConfigurationValuePSTypeConverter : PSTypeConverter
    {
        public override bool CanConvertFrom(object sourceValue, Type destinationType)
        {
            if (!destinationType.IsAssignableTo(typeof(ConfigurationValue))) return false;
            return true;
        }

        public override bool CanConvertTo(object sourceValue, Type destinationType)
        {
            if (sourceValue is ConfigurationValue value
                && value.IsSafeValue
                && (value.SafeGetValue()?.GetType().IsAssignableTo(destinationType) ?? false))
            {
                return true;
            }
            return false;
        }

        public override object ConvertFrom(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (sourceValue is string str)
            {
                if (str.StartsWith('$'))
                {
                    return new ConfigurationVariable(str);
                }
                else
                {
                    return new ConfigurationArgument(str);
                }
            }

            if (sourceValue is Hashtable hasht)
            {
                if (hasht.ContainsKey("Variable"))
                {
                    return new ConfigurationVariable(LanguagePrimitives.ConvertTo<string>(hasht["Variable"]));
                }
                if (hasht.ContainsKey("Value"))
                {
                    return new ConfigurationArgument(hasht["Variable"]);
                }

                if (hasht.ContainsKey(nameof(ConfigurationItem.ItemType))
                    && LanguagePrimitives.TryConvertTo<ConfigurationItemType>(hasht[nameof(ConfigurationItem.ItemType)], out var type)
                    && type == ConfigurationItemType.Parameters)
                {
                    var clone = (Hashtable)hasht.Clone();
                    clone.Remove(nameof(ConfigurationItem.ItemType));
                    return new ConfigurationParameters(clone);
                }
            }

            return new ConfigurationArgument(sourceValue);
        }

        public override object? ConvertTo(object sourceValue, Type destinationType, IFormatProvider formatProvider, bool ignoreCase)
        {
            if (sourceValue is ConfigurationValue value
                && value.IsSafeValue)
            {
                return value.SafeGetValue();
            }
            throw new PSInvalidCastException();
        }
    }

    public abstract class ConfigurationItem
    {
        internal ConfigurationItem()
        {

        }

        /// <summary>
        /// Invokes the item and returns a value represented by the configuration element.
        /// </summary>
        /// <param name="sessionState"></param>
        /// <returns></returns>
        protected abstract internal object? Invoke(SessionState sessionState);
        /// <summary>
        /// <see langword="true"/> if this item has a chance of returning a value when invoked.
        /// </summary>
        public abstract bool HasReturnValue { get; }
        public abstract ConfigurationItemType ItemType { get; }
    }
    public abstract class ConfigurationValue : ConfigurationItem
    {
        internal ConfigurationValue()
        {

        }
        public abstract bool IsSafeValue { get; }
        protected internal abstract object? SafeGetValue();
        public sealed override bool HasReturnValue => true;
    }
    public sealed class ConfigurationArgument : ConfigurationValue
    {
        public override bool IsSafeValue => true;
        protected internal override object? Invoke(SessionState sessionState) => SafeGetValue();
        protected internal override object? SafeGetValue() => _value;
        private readonly object? _value;
        public ConfigurationArgument(object? value) => _value = value;
        public override ConfigurationItemType ItemType => ConfigurationItemType.Argument;
        public override string ToString() => _value?.ToString() ?? "(null)";
    }
    public sealed class ConfigurationVariable : ConfigurationValue
    {
        public ConfigurationVariable(string variable)
        {
            _name = variable.TrimStart('?');
        }
        private readonly string _name;
        internal void SetValue(SessionState sessionState, object? value)
        {
            sessionState.PSVariable.Set(_name, value);
        }
        protected internal override object? Invoke(SessionState sessionState)
        {
            return sessionState.PSVariable.GetValue(_name);
        }
        protected internal override object? SafeGetValue()
        {
            throw new PSNotSupportedException();
        }
        public override bool IsSafeValue => false;
        public override ConfigurationItemType ItemType => ConfigurationItemType.Variable;

        public override string ToString() => $"${_name}";
    }
    public sealed class ConfigurationCommand : ConfigurationItem
    {
        public ConfigurationCommand() { }
        public ConfigurationCommand(string commandText)
        {
            var parts = commandText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0)
            {
                Command = parts[0];
            }
            if (parts.Length > 1)
            {
                ArgumentList = LanguagePrimitives.ConvertTo<ConfigurationValue[]>(parts[1..]);
            }
        }
        public ConfigurationCommand(ScriptBlock script)
        {
            Script = script;
        }



        public override ConfigurationItemType ItemType => ConfigurationItemType.Command;
        public ScriptBlock? Script { get; set; }
        public string? Command { get; set; }
        public ConfigurationValue?[]? ArgumentList { get; set; }
        public ConfigurationParameters? Parameters { get; set; }
        public ConfigurationVariable? Output { get; set; }
        public override bool HasReturnValue => true;
        internal bool ImportedFromDataFile { get; set; } = false;
        protected internal override object? Invoke(SessionState sessionState)
        {
            var args = GetArguments(sessionState);
            var parameters = Parameters?.Invoke(sessionState);
            Collection<PSObject>? output;
            if (Command is not null)
            {
                var command = sessionState.InvokeCommand.GetCommand(Command, CommandTypes.All);

                var scriptText = new StringBuilder();
                scriptText.Append(@"
param(
    [System.Management.Automation.CommandInfo]$Command");
                if (args is not null)
                {
                    scriptText.Append(@",
    [object[]]$ArgumentList");
                }
                if (parameters is not null)
                {
                    scriptText.Append(@",
    [hashtable]$Parameters");
                }
                scriptText.Append(@"
)
& $Command");
                if (args is not null)
                {
                    scriptText.Append(" $ArgumentList ");
                }
                if (parameters is not null)
                {
                    scriptText.Append(" @Parameters");
                }


                var argumentList = new List<object?>(3)
                {
                    command
                };
                if (args is not null) argumentList.Add(args);
                if (parameters is not null) argumentList.Add(parameters);
                var argumentListArray = argumentList.ToArray();

                var scriptBlock = sessionState.InvokeCommand.NewScriptBlock(scriptText.ToString());
                output = sessionState.InvokeCommand.InvokeScript(
                    false,
                    scriptBlock,
                    null,
                    args: argumentListArray);
            }
            else if (Script is not null)
            {
                output = sessionState.InvokeCommand.InvokeScript(false, Script, null, args);
                if (ImportedFromDataFile)
                {
                    output = sessionState.InvokeCommand.InvokeScript(false, output[0].BaseObject as ScriptBlock, null, args);
                }
                var writeOutputVerbose = sessionState.InvokeCommand.NewScriptBlock("Write-Verbose \"Output:\n$($args[0] | Out-String)\"");
                sessionState.InvokeCommand.InvokeScript(true, writeOutputVerbose, null, output);
            }
            else
            {
                output = null;
            }
            Output?.SetValue(sessionState, output);
            return output;
        }
        private object?[]? GetArguments(SessionState sessionState)
        {
            if (ArgumentList is null) return null;
            if (ArgumentList.Length == 0) return Array.Empty<object>();
            var arguments = new object?[ArgumentList.Length];
            for (int i = 0; i < ArgumentList.Length; i++)
            {
                arguments[i] = ArgumentList[i]?.Invoke(sessionState);
            }
            return arguments;
        }
        public override string ToString()
            => Command ??
            (Script is null
                ? "null"
                : Script.Ast.Extent.Text.Length < 30
                    ? Script.ToString()
                    : "scriptblock"
            );
    }
    public sealed class ConfigurationParameters : ConfigurationValue
    {
        public override ConfigurationItemType ItemType => ConfigurationItemType.Parameters;
        public override bool IsSafeValue => _isSafeValue;
        private bool _isSafeValue;
        private readonly Dictionary<object, ConfigurationValue> _values;
        private Hashtable? _safeValue;
        protected internal override object? SafeGetValue()
        {
            if (!_isSafeValue) throw new PSNotSupportedException();
            else
            {
                if (_safeValue is null)
                {
                    _safeValue = new Hashtable(StringComparer.OrdinalIgnoreCase);
                    foreach (var key in _values.Keys)
                    {
                        _safeValue[key] = _values[key].SafeGetValue();
                    }
                }
                return _safeValue;
            }
        }
        protected internal override object? Invoke(SessionState sessionState)
        {
            if (_isSafeValue) return SafeGetValue();
            var value = new Hashtable(StringComparer.OrdinalIgnoreCase);
            foreach (var key in _values.Keys)
            {
                value[key] = _values[key].Invoke(sessionState);
            }
            return value;
        }
        public ConfigurationParameters(Hashtable hashtable)
        {
            _values = new Dictionary<object, ConfigurationValue>();
            foreach (var key in hashtable)
            {
                var value = LanguagePrimitives.ConvertTo<ConfigurationValue>(hashtable[key]);
                if (!value.IsSafeValue) _isSafeValue = false;
                _values[key] = value;
            }
        }
    }
    public sealed class ConfigurationDynamicParameter : ConfigurationValue
    {
        public ConfigurationDynamicParameter() { }
        public ConfigurationDynamicParameter(string name) => Name = name;


        protected internal override object? Invoke(SessionState sessionState) => SafeGetValue();
        protected internal override RuntimeDefinedParameter? SafeGetValue()
        {
            var parameter = new RuntimeDefinedParameter(Name, LanguagePrimitives.ConvertTo<Type>(TypeName), null);
            foreach (var attribute in Attributes)
            {
                var actualAttribute = (Attribute)attribute.SafeGetValue()!;
                parameter.Attributes.Add(actualAttribute);
            }
            return parameter;
        }
        public override bool IsSafeValue => true;
        public override ConfigurationItemType ItemType => ConfigurationItemType.DynamicParameter;
        public override string ToString() => Name;

        public string Name { get; set; }
        public string TypeName { get; set; }
        public ConfigurationDynamicParameterAttribute[] Attributes { get; set; }
    }
    public sealed class ConfigurationDynamicParameterAttribute : ConfigurationValue
    {
        public ConfigurationDynamicParameterAttribute() { }
        public ConfigurationDynamicParameterAttribute(string typeName)
        {
            TypeName = typeName;
        }



        public string TypeName { get; set; }
        public object[]? ArgumentList { get; set; }
        public Hashtable? Properties { get; set; }

        public override bool IsSafeValue => true;
        protected internal override object? Invoke(SessionState sessionState) => SafeGetValue();
        protected internal override object? SafeGetValue()
        {
            var attributeType = LanguagePrimitives.ConvertTo<Type>(TypeName);
            var attribute = LanguagePrimitives.ConvertTo<Attribute>(ScriptBlock.Create("New-Object -TypeName $args[0] -ArgumentList $args[1] -Property $args[2]")
                .InvokeReturnAsIs(TypeName, ArgumentList, Properties));
            return attribute;
        }
        public override ConfigurationItemType ItemType => ConfigurationItemType.Undefined;
    }
    public sealed class ConfigurationModuleTemplate : ConfigurationItem
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Version Version { get; set; }



        internal const string TemplatePathVariable = "PSTemplateFile";
        internal const string TemplateDestinationVariable = "PSTemplateDestination";
        internal const string TemplateDynamicParameters = "PSTemplateDynamicParameters";

        public ConfigurationCommand[] BeforeExecute { get; set; }
        public ConfigurationCommand[] AfterExecute { get; set; }
        public ConfigurationDynamicParameter[] DynamicParameters { get; set; }
        public ConfigurationFileMap[] Files { get; set; }

        internal RuntimeDefinedParameterDictionary GetDynamicParameters()
        {
            var parameters = new RuntimeDefinedParameterDictionary();
            foreach (var dynamicParameter in DynamicParameters)
            {
                var runtimeParameter = dynamicParameter.SafeGetValue()!;
                parameters.Add(runtimeParameter.Name, runtimeParameter);
            }
            return parameters;
        }

        protected internal override object? Invoke(SessionState sessionState)
        {
            string destinationPath;
            if (sessionState.PSVariable.GetValue(TemplatePathVariable) is null
                || (destinationPath = (string)sessionState.PSVariable.GetValue(TemplateDestinationVariable)) is null)
            {
                throw new InvalidOperationException("Must set the session state parameters for template file and destination.");
            }

            if (!Directory.Exists(destinationPath)) Directory.CreateDirectory(destinationPath);
            foreach (var dynamicParameter in ((RuntimeDefinedParameterDictionary)sessionState.PSVariable.GetValue(TemplateDynamicParameters)).Values)
            {
                sessionState.PSVariable.Set(dynamicParameter.Name, dynamicParameter.Value);
            }
            using var pathVisitor = sessionState.Path.VisitPath(destinationPath);
            foreach (var command in BeforeExecute)
            {
                sessionState.InvokeCommand.InvokeScript(true, sessionState.InvokeCommand.NewScriptBlock("Write-Verbose ($args[0] | Select-Object Command, ArgumentList, Parameters, ScriptBlock | Format-List | Out-String)"), null, command);
                command?.Invoke(sessionState);
            }
            foreach (var file in Files)
            {
                file?.Invoke(sessionState);
            }
            foreach (var command in AfterExecute)
            {
                sessionState.InvokeCommand.InvokeScript(true, sessionState.InvokeCommand.NewScriptBlock("Write-Verbose ($args[0] | Select-Object Command, ArgumentList, Parameters, ScriptBlock | Format-List | Out-String)"), null, command);
                command?.Invoke(sessionState);
            }

            return null;
        }
        public override bool HasReturnValue => false;
        public override ConfigurationItemType ItemType => ConfigurationItemType.Document;
    }
    public sealed class ConfigurationFileMap : ConfigurationItem
    {
        public ConfigurationFileMap(string path)
        {
            DestinationPath = TemplatePath = path;
            InterpolateVariables = true;
        }
        public ConfigurationFileMap(Hashtable paths)
        {
            DestinationPath = LanguagePrimitives.ConvertTo<string>(paths[nameof(DestinationPath)]);
            TemplatePath = LanguagePrimitives.ConvertTo<string>(paths[nameof(TemplatePath)]);
            InterpolateVariables = LanguagePrimitives.ConvertTo<bool>(paths[nameof(InterpolateVariables)]);
        }


        public override bool HasReturnValue => true;

        public override ConfigurationItemType ItemType => ConfigurationItemType.Command;

        /// <summary>
        /// Relative path from template document to where the source file exists.
        /// </summary>
        public string TemplatePath { get; set; }
        /// <summary>
        /// Relative path from the destination directory to where the destination file should be created.
        /// </summary>
        public string DestinationPath { get; set; }

        /// <summary>
        /// Replace variables formatted as "{{VariableName}}" in the source file with the value of the
        /// dynamic parameter with the same name that is indicated by the
        /// <see cref="ConfigurationModuleTemplate.DynamicParameters"/>.
        /// </summary>
        public bool InterpolateVariables { get; set; }

        protected internal override object? Invoke(SessionState sessionState)
        {
            // the variable PSTemplateFile represents the path of the template .psd1 file
            // the variable PSTemplateDestination represents the destination directory for the template to be copied to

            var templateFileDir = Path.GetDirectoryName((string)sessionState.PSVariable.GetValue(ConfigurationModuleTemplate.TemplatePathVariable))!;
            var destinationDir = (string)sessionState.PSVariable.GetValue(ConfigurationModuleTemplate.TemplateDestinationVariable);
            var dynamicParameters = (RuntimeDefinedParameterDictionary)sessionState.PSVariable.GetValue(ConfigurationModuleTemplate.TemplateDynamicParameters);

            string templateFilePath;
            string destinationFilePath;
            using (sessionState.Path.VisitPath(templateFileDir))
            {
                templateFilePath = sessionState.Path.GetUnresolvedProviderPathFromPSPath(TemplatePath);
            }
            using (sessionState.Path.VisitPath(destinationDir))
            {
                var interpolatedDestinationPath = DestinationPath;
                foreach (var key in dynamicParameters.Keys)
                {
                    interpolatedDestinationPath = interpolatedDestinationPath.Replace($"{{{{{key}}}}}", dynamicParameters[key]?.Value?.ToString());
                }
                destinationFilePath = sessionState.Path.GetUnresolvedProviderPathFromPSPath(interpolatedDestinationPath);
            }

            var fileDestinationDir = Path.GetDirectoryName(destinationFilePath);
            if (fileDestinationDir is not null
                && !Directory.Exists(fileDestinationDir))
            {
                Directory.CreateDirectory(fileDestinationDir);
            }

            if (!InterpolateVariables)
            {
                File.Copy(templateFilePath, destinationFilePath);
            }
            else
            {
                using var templateFs = new FileStream(templateFilePath, FileMode.Open, FileAccess.Read);
                using var destinationFs = new FileStream(destinationFilePath, FileMode.Create, FileAccess.ReadWrite);
                using var reader = new StreamReader(templateFs);
                using var writer = new StreamWriter(destinationFs);

                string? currentLine;
                while ((currentLine = reader.ReadLine()) is not null)
                {
                    foreach (var key in dynamicParameters.Keys)
                    {
                        currentLine = currentLine.Replace($"{{{{{key}}}}}", dynamicParameters[key]?.Value?.ToString());
                    }
                    writer.WriteLine(currentLine);
                }
            }
            return destinationFilePath;
        }
    }
}
