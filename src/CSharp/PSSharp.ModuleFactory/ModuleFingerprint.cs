using System.Management.Automation.Internal;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PSSharp.ModuleFactory
{
    /// <summary>
    /// Identifies the public API of a PowerShell command.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ExplicitPropertyJsonConverter<CommandFingerprint>))]
    public sealed class CommandFingerprint : FingerprintBase, IEquatable<CommandFingerprint>
    {
        [JsonPropertyName("name")]
        [Newtonsoft.Json.JsonProperty("name")]
        private readonly string _name;
        [JsonPropertyName("parameterSets")]
        [Newtonsoft.Json.JsonProperty("parameterSets")]
        private readonly ParameterSetFingerprint[] _parameterSets;
        private readonly int _aggregateParameterCount;
        private long _parameterCombinatorials = -1;
        public CommandFingerprint(CommandInfo command)
        {
            _name = command.Name.ToLower();
            _parameterSets = new ParameterSetFingerprint[command.ParameterSets.Count];
            int i = 0;
            foreach (var parameterSet in command.ParameterSets.OrderBy(x => x.Name))
            {
                var parameterSetFingerprint = new ParameterSetFingerprint(parameterSet);
                _parameterSets[i++] = parameterSetFingerprint;
                _aggregateParameterCount += parameterSetFingerprint.ParameterCount;
            }
        }
        [JsonConstructor]
        [Newtonsoft.Json.JsonConstructor]
        public CommandFingerprint(string name, ParameterSetFingerprint[] parameterSets)
        {
            _name = name;
            _parameterSets = new ParameterSetFingerprint[parameterSets.Length];
            Array.Copy(parameterSets, _parameterSets, parameterSets.Length);
            for (int i = 0; i < parameterSets.Length; i++)
            {
                _aggregateParameterCount += parameterSets[i].ParameterCount;
            }
        }

        public CommandFingerprint(Hashtable hashtable)
        {
            _name = GetHashtableMember<string>(hashtable, nameof(Name), true).ToLower();
            var parameterSets = GetHashtableMember<ParameterSetFingerprint[]>(hashtable, "ParameterSets", true);
            _parameterSets = new ParameterSetFingerprint[parameterSets.Length];
            Array.Copy(parameterSets, _parameterSets, parameterSets.Length);
            for (int i = 0; i < parameterSets.Length; i++)
            {
                _aggregateParameterCount += parameterSets[i].ParameterCount;
            }
        }


        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string Name => _name;
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int ParameterSetCount => _parameterSets.Length;
        /// <summary>
        /// The sum total of the number of parameters of each parameter set.
        /// </summary>
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int AggregateParameterCount => _aggregateParameterCount;
        /// <summary>
        /// The total number of permutations possible for the parameters that may be bound by name to the
        /// command, using only static (non-dynamic) parameters.
        /// </summary>
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public long ParameterCombinatorials
            => _parameterCombinatorials == -1
            ? (_parameterCombinatorials = CalculateCombinatorials(_parameterSets))
            : _parameterCombinatorials;

        public ParameterSetFingerprint[] GetParameterSets()
        {
            if (_parameterSets.Length == 0) return Array.Empty<ParameterSetFingerprint>();
            var arr = new ParameterSetFingerprint[_parameterSets.Length];
            Array.Copy(_parameterSets, arr, _parameterSets.Length);
            return arr;
        }

        public override bool Equals(object? obj)
            => Equals(obj as CommandFingerprint);
        public bool Equals(CommandFingerprint? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_parameterSets.Length != other._parameterSets.Length) return false;
            if (_name != other._name) return false;
            if (_aggregateParameterCount != other._aggregateParameterCount) return false;
            if (_parameterCombinatorials != -1 && other._parameterCombinatorials != -1 && _parameterCombinatorials != other._parameterCombinatorials) return false;
            if (GetHashCode() != other.GetHashCode()) return false;
            for (int i = 0; i < _parameterSets.Length; i++)
            {
                if (!_parameterSets[i].Equals(other._parameterSets[i])) return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            if (_parameterSets.Length > 1)
            {
                return HashCode.Combine(_name, _parameterSets.Length, _parameterSets[0], _parameterSets[1], _parameterSets[^2], _parameterSets[^1]);
            }
            else if (_parameterSets.Length == 1)
            {
                return HashCode.Combine(_name, _parameterSets.Length, _parameterSets[0], _parameterSets[^1]);
            }
            else
            {
                return HashCode.Combine(_name, _parameterSets.Length);
            }
        }
        public override string ToString() => _name;

        protected internal override ModuleFingerprintChange IdentifyChangesFromPrevious(FingerprintBase previous)
        {
            var fingerprint = (CommandFingerprint)previous;
            var changes = ModuleFingerprintChange.None;

            if (_name != fingerprint._name) changes |= ModuleFingerprintChange.CommandAdded | ModuleFingerprintChange.CommandRemoved;

            var currentParameterSets = _parameterSets.ToDictionary(i => i.Name);
            var previousParameterSets = _parameterSets.ToDictionary(i => i.Name);
            var parameterSetNames = currentParameterSets.Keys.Union(previousParameterSets.Keys).ToArray();
            foreach (var parameterSetName in parameterSetNames)
            {
                currentParameterSets.TryGetValue(parameterSetName, out var currentParameterSet);
                previousParameterSets.TryGetValue(parameterSetName, out var previousParameterSet);

                if (currentParameterSet is null && previous is null)
                {
                    continue;
                }
                else if (currentParameterSet is null && previous is not null)
                {
                    changes |= ModuleFingerprintChange.ParameterSetRemoved;
                }
                else if (currentParameterSet is not null && previous is null)
                {
                    changes |= ModuleFingerprintChange.ParameterSetAdded;
                }
                else
                {
                    changes |= currentParameterSet!.IdentifyChangesFromPrevious(previousParameterSet!);
                }
            }

            return changes;
        }

        private static long CalculateCombinatorials(ParameterSetFingerprint[] parameterSets)
        {
            long combinatorials = 0;
            for (int i = 0; i < parameterSets.Length; i++)
            {
                combinatorials += parameterSets[i].ParameterCombinatorials;
            }
            return combinatorials;
        }
    }


    /// <summary>
    /// Base class for the <see cref="ModuleFingerprint"/> and fingerprint classes used by it.
    /// </summary>
    [Serializable]
    public abstract class FingerprintBase
    {
        internal FingerprintBase()
        {

        }

        /// <summary>
        /// Used for recursively identifying changes made between the current fingerprint instance and the
        /// fingerprint from the previous version.
        /// </summary>
        /// <param name="previous">An instance of the same type as the current object to which comparison will
        /// be made. This parameter will not be passed <see langword="null"/>.</param>
        /// <returns></returns>
        protected abstract internal ModuleFingerprintChange IdentifyChangesFromPrevious(FingerprintBase previous);

        internal static T GetHashtableMember<T>(Hashtable hashtable, string name, bool isRequired)
        {
            if (hashtable.ContainsKey(name))
            {
                var rawValue = hashtable[name];
                if (LanguagePrimitives.TryConvertTo<T>(rawValue, out var value))
                {
                    return value;
                }
                else
                {
                    throw new PSInvalidCastException(string.Format(Resources.MemberTypeInvalid, name, typeof(T)));
                }
            }
            else
            {
                if (isRequired)
                {
                    throw new PSInvalidCastException(string.Format(Resources.RequiredMemberInterpolated, name));
                }
                else
                {
                    return default!;
                }
            }
        }

        /// <summary>
        /// Only deserializes properties explicitly referenced by <see cref="JsonPropertyNameAttribute"/>.
        /// Deserialization is only supported through a single <see cref="JsonConstructorAttribute"/>
        /// constructor defined for the class.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        protected sealed class ExplicitPropertyJsonConverter<T> : JsonConverter<T>
        {
            public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
            {
                var properties = typeof(T).GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy)
                    .Where(i => i is PropertyInfo or FieldInfo)
                    .Select(i => (property: i, propertyNameAttribute: i.GetCustomAttribute<JsonPropertyNameAttribute>()))
                    .Where(i => i.propertyNameAttribute is not null)
                    .ToArray();

                var obj = new Dictionary<string, object?>();
                foreach (var property in properties)
                {
                    var propertyValue = (property.property as PropertyInfo)?.GetValue(value)
                        ?? (property.property as FieldInfo)?.GetValue(value);
                    var propertyName = property.propertyNameAttribute!.Name;
                    obj[propertyName] = propertyValue;
                }
                JsonSerializer.Serialize(writer, obj, options);
            }
        }

        public abstract override bool Equals(object? obj);
        public abstract override int GetHashCode();
        public abstract override string ToString();
    }

    /// <summary>
    /// Identifies the public API of a PowerShell module, which can be used to detect changes between module
    /// versions or builds.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ExplicitPropertyJsonConverter<ModuleFingerprint>))]
    public sealed class ModuleFingerprint : FingerprintBase, IEquatable<ModuleFingerprint>
    {
        [JsonPropertyName("commands")]
        [Newtonsoft.Json.JsonProperty("commands")]
        private readonly CommandFingerprint[] _commands;
        private long _commandParameterCombinatorials = -1;
        public ModuleFingerprint()
        {
            _commands = Array.Empty<CommandFingerprint>();
        }
        public ModuleFingerprint(PSModuleInfo module)
        {
            int i = 0;
            _commands = new CommandFingerprint[module.ExportedCommands.Count];
            foreach (var command in module.ExportedCommands.Values.OrderBy(i => i.Name))
            {
                _commands[i++] = new(command);
            }
        }
        [JsonConstructor]
        [Newtonsoft.Json.JsonConstructor]
        public ModuleFingerprint(CommandFingerprint[] commands)
        {
            _commands = new CommandFingerprint[commands.Length];
            Array.Copy(commands, _commands, commands.Length);
        }
        public ModuleFingerprint(Hashtable hashtable)
        {
            var commands = GetHashtableMember<CommandFingerprint[]>(hashtable, "commands", true);
            _commands = new CommandFingerprint[commands.Length];
            Array.Copy(commands, _commands, commands.Length);
        }

        /// <summary>
        /// The number of syntactically valid command and parameter combinations that can be invoked using
        /// commands exported from the module, without the use of any common or dynamic parameters.
        /// </summary>
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public long CommandParameterCombinatorials
            => _commandParameterCombinatorials == -1
            ? (_commandParameterCombinatorials = CalculateCombinatorials(_commands))
            : _commandParameterCombinatorials;

        /// <summary>
        /// The number of exported commands from the module.
        /// </summary>
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int CommandCount => _commands.Length;

        public CommandFingerprint[] GetCommands()
        {
            var arr = new CommandFingerprint[_commands.Length];
            Array.Copy(_commands, arr, _commands.Length);
            return arr;
        }

        public bool Equals(ModuleFingerprint? other)
            => Equals(this, other);
        public override bool Equals(object? other)
            => Equals(other as ModuleFingerprint);
        public static bool Equals(ModuleFingerprint? left, ModuleFingerprint? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            if (left._commands.Length != right._commands.Length) return false;
            if (left.GetHashCode() != right.GetHashCode()) return false;
            for (int i = 0; i < left._commands.Length; i++)
            {
                if (!left._commands[i].Equals(right._commands[i])) return false;
            }
            return true;
        }
        public static bool Equals(ModuleFingerprint? left, object? right)
        {
            return ReferenceEquals(left, right)
                || (left is null && right is null)
                || (right is ModuleFingerprint fingerprint && Equals(left, fingerprint));
        }
        public static bool operator ==(ModuleFingerprint? left, ModuleFingerprint? right)
            => Equals(left, right);
        public static bool operator !=(ModuleFingerprint? left, ModuleFingerprint? right)
            => !Equals(left, right);
        public override int GetHashCode()
        {
            if (_commands.Length > 2)
                return HashCode.Combine(_commands.Length, _commands[0], _commands[1], _commands[2], _commands[^3], _commands[^2], _commands[^1]);
            else if (_commands.Length == 2)
                return HashCode.Combine(_commands.Length, _commands[0], _commands[1], _commands[^2], _commands[^1]);
            else if (_commands.Length == 1)
                return HashCode.Combine(_commands.Length, _commands[0]);
            else if (_commands.Length == 0)
                return 0;
            else return -1;
        }

        public override string ToString() => string.Format(Resources.ModuleFingerprintToString, CommandParameterCombinatorials, CommandCount);

        protected internal override ModuleFingerprintChange IdentifyChangesFromPrevious(FingerprintBase previous)
        {
            var fingerprint = (ModuleFingerprint)previous;
            var changes = ModuleFingerprintChange.None;

            var currentCommands = _commands.ToDictionary(i => i.Name);
            var previousCommands = fingerprint._commands.ToDictionary(i => i.Name);
            var commandNames = currentCommands.Keys.Union(previousCommands.Keys);

            foreach (var commandName in commandNames)
            {
                currentCommands.TryGetValue(commandName, out var currentCommand);
                previousCommands.TryGetValue(commandName, out var previousCommand);

                if (currentCommand is null && previous is null)
                {
                    continue;
                }
                else if (currentCommand is not null && previousCommand is null)
                {
                    changes |= ModuleFingerprintChange.CommandAdded;
                }
                else if (currentCommand is null && previousCommand is not null)
                {
                    changes |= ModuleFingerprintChange.CommandRemoved;
                }
                else
                {
                    changes |= currentCommand!.IdentifyChangesFromPrevious(previousCommand!);
                }
            }

            return changes;
        }

        public ModuleFingerprintChange GetChangesFrom(ModuleFingerprint previous)
            => IdentifyChangesFromPrevious(previous);

        private static long CalculateCombinatorials(CommandFingerprint[] commands)
        {
            long combinatorials = 0;
            for (int i = 0; i < commands.Length; i++)
            {
                combinatorials += commands[i].ParameterCombinatorials;
            }
            return combinatorials;
        }
    }
    
    /// <summary>
    /// Changes detected between two <see cref="ModuleFingerprint"/> instances.
    /// </summary>
    [Flags]
    public enum ModuleFingerprintChange
    {
        None = 0,
        CommandAdded = 1,
        CommandRemoved = 2,
        ParameterSetNameChanged = 4,
        MandatoryParameterRemoved = 8,
        MandatoryParameterAdded = 16,
        NonMandatoryParameterAdded = 32,
        NonMandatoryParameterRemoved = 64,
        ParameterTypeChanged = 128,
        ParameterPositionChanged = 256,
        ParameterBecameMandatory = 512,
        ParameterBecameNonMandatory = 1024,
        ParameterSetAdded = 2048,
        ParameterSetRemoved = 4096,
    }

    /// <summary>
    /// The portions of a PowerShell module version which may be increased.
    /// </summary>
    public enum ModuleVersionStep
    {
        None = 0,
        Patch = 1,
        Minor = 2,
        Major = 3,
    }
    
    /// <summary>
    /// Extensions for <see cref="ModuleVersionStep"/>.
    /// </summary>
    public static class ModuleVersionStepExtensions
    {
        public static Version Step(this ModuleVersionStep moduleVersionStep, Version fromVersion)
        {
            if (fromVersion is null) throw new ArgumentNullException(nameof(fromVersion));

            return moduleVersionStep switch
            {
                ModuleVersionStep.Major => new Version(fromVersion.Major + 1, 0, 0),
                ModuleVersionStep.Minor => new Version(fromVersion.Minor, fromVersion.Minor + 1, 0),
                ModuleVersionStep.Patch => new Version(fromVersion.Major, fromVersion.Minor, fromVersion.Build + 1),
                _ => new Version(fromVersion.Minor, fromVersion.Minor, fromVersion.Build),
            };
        }
        public static SemanticVersion Step(this ModuleVersionStep moduleVersionStep, SemanticVersion semanticVersion)
        {
            if (semanticVersion is null) throw new ArgumentNullException(nameof(semanticVersion));

            return moduleVersionStep switch
            {
                ModuleVersionStep.Major => new SemanticVersion(semanticVersion.Major + 1, 0, 0),
                ModuleVersionStep.Minor => new SemanticVersion(semanticVersion.Major, semanticVersion.Minor + 1, 0),
                ModuleVersionStep.Patch => new SemanticVersion(semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch + 1),
                _ => new SemanticVersion(semanticVersion.Major, semanticVersion.Minor, semanticVersion.Patch)
            };
        }
    }

    /// <summary>
    /// Identifies the public API of a parameter within a parameter set of a PowerShell command.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ExplicitPropertyJsonConverter<ParameterFingerprint>))]
    public sealed class ParameterFingerprint : FingerprintBase, IEquatable<ParameterFingerprint>
    {
        [JsonPropertyName("name")]
        [Newtonsoft.Json.JsonProperty("name")]
        private readonly string _name;
        [JsonPropertyName("typeName")]
        [Newtonsoft.Json.JsonProperty("typeName")]
        private readonly string _typeName;
        [JsonPropertyName("isMandatory")]
        [Newtonsoft.Json.JsonProperty("isMandatory")]
        private readonly bool _isMandatory;
        [JsonPropertyName("position")]
        [Newtonsoft.Json.JsonProperty("position")]
        private readonly int _position;
        public ParameterFingerprint(CommandParameterInfo parameterInfo)
        {
            _name = parameterInfo.Name.ToLower();
            _typeName = parameterInfo.ParameterType.FullName!.ToLower();
            _isMandatory = parameterInfo.IsMandatory;
            _position = parameterInfo.Position;
        }
        [JsonConstructor]
        [Newtonsoft.Json.JsonConstructor]
        public ParameterFingerprint(string name, string typeName, bool isMandatory, int position)
        {
            _name = name;
            _typeName = typeName;
            _isMandatory = isMandatory;
            _position = position;
        }
        public ParameterFingerprint(Hashtable hashtable)
        {
            _name = GetHashtableMember<string>(hashtable, nameof(Name), true).ToLower();
            _typeName = GetHashtableMember<string>(hashtable, nameof(TypeName), true).ToLower();
            _isMandatory = GetHashtableMember<bool>(hashtable, nameof(IsMandatory), false);
            _position = GetHashtableMember<int>(hashtable, nameof(Position), true);
        }

        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string Name => _name;
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string TypeName => _typeName;
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool IsMandatory => _isMandatory;
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int Position => _position;

        public override bool Equals(object? obj)
            => Equals(obj as ParameterFingerprint);
        public bool Equals(ParameterFingerprint? other)
        {
            return other is not null
                && _name == other._name
                && _typeName == other._typeName
                && _isMandatory == other._isMandatory
                && _position == other._position;
        }

        public override int GetHashCode()
            => HashCode.Combine(_name, _typeName, _isMandatory, _position);

        public override string ToString() => _name;

        protected internal override ModuleFingerprintChange IdentifyChangesFromPrevious(FingerprintBase previous)
        {
            var fingerprint = (ParameterFingerprint)previous;

            var changes = ModuleFingerprintChange.None;
            if (fingerprint._typeName != _typeName) changes |= ModuleFingerprintChange.ParameterTypeChanged;
            if (fingerprint._isMandatory != _isMandatory) changes |= _isMandatory ? ModuleFingerprintChange.ParameterBecameMandatory : ModuleFingerprintChange.ParameterBecameNonMandatory;
            if (fingerprint._position != _position) changes |= ModuleFingerprintChange.ParameterPositionChanged;

            return changes;
        }
    }

    /// <summary>
    /// Identifies the public API of a parameter set of a PowerShell command.
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(ExplicitPropertyJsonConverter<ParameterSetFingerprint>))]
    public sealed class ParameterSetFingerprint : FingerprintBase, IEquatable<ParameterSetFingerprint>
    {
        private readonly static HashSet<string> s_excludeParameters;
        static ParameterSetFingerprint()
        {
            s_excludeParameters = new HashSet<string>()
            {
            nameof(CommonParameters.Debug),
            nameof(CommonParameters.ErrorAction),
            nameof(CommonParameters.ErrorVariable),
            nameof(CommonParameters.InformationAction),
            nameof(CommonParameters.InformationVariable),
            nameof(CommonParameters.OutBuffer),
            nameof(CommonParameters.OutVariable),
            nameof(CommonParameters.PipelineVariable),
            nameof(CommonParameters.Verbose),
            nameof(CommonParameters.WarningAction),
            nameof(CommonParameters.WarningVariable),
            nameof(ShouldProcessParameters.Confirm),
            nameof(ShouldProcessParameters.WhatIf),
            nameof(PagingParameters.First),
            nameof(PagingParameters.IncludeTotalCount),
            nameof(PagingParameters.Skip)
            };
        }


        [JsonPropertyName("name")]
        [Newtonsoft.Json.JsonProperty("name")]
        private readonly string _name;
        [JsonPropertyName("parameters")]
        [Newtonsoft.Json.JsonProperty("parameters")]
        private readonly ParameterFingerprint[] _parameters;
        [JsonPropertyName("isDefaultParameterSet")]
        [Newtonsoft.Json.JsonProperty("isDefaultParameterSet")]
        private readonly bool _isDefaultParameterSet;
        private readonly int _mandatoryParameterCount;
        private long _parameterCombinatorials = -1;
        public ParameterSetFingerprint(CommandParameterSetInfo parameterSet)
        {
            _name = parameterSet.Name.ToLower();
            _isDefaultParameterSet = parameterSet.IsDefault;
            _parameters = new ParameterFingerprint[parameterSet.Parameters.Count];
            int i = 0;
            foreach (var parameterInfo in parameterSet.Parameters.OrderBy(x => x.Name))
            {
                if (s_excludeParameters.Contains(parameterInfo.Name))
                {
                    continue;
                }

                var parameterFingerprint = new ParameterFingerprint(parameterInfo);
                _parameters[i++] = parameterFingerprint;
                if (parameterInfo.IsMandatory) _mandatoryParameterCount++;
            }
            if (i < _parameters.Length)
            {
                var actualParameters = new ParameterFingerprint[i];
                Array.Copy(_parameters, actualParameters, i);
                _parameters = actualParameters;
            }
        }
        /// <summary>
        /// Constructor to support PowerShell cast from hashtable through the common syntax
        /// `[TypeName]@{PropertyName = 'Value'}`.
        /// </summary>
        /// <param name="hashtable"></param>
        public ParameterSetFingerprint(Hashtable hashtable)
        {
            _name = GetHashtableMember<string>(hashtable, nameof(Name), true).ToLower();
            var parameters = GetHashtableMember<ParameterFingerprint[]>(hashtable, "Parameters", true);
            _parameters = new ParameterFingerprint[parameters.Length];
            Array.Copy(parameters, _parameters, parameters.Length);
            _mandatoryParameterCount = _parameters.Count(i => i.IsMandatory);
        }
        [JsonConstructor]
        [Newtonsoft.Json.JsonConstructor]
        public ParameterSetFingerprint(string name, bool isDefaultParameterSet, ParameterFingerprint[] parameters)
        {
            if (parameters is null) throw new ArgumentNullException(nameof(parameters));
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _isDefaultParameterSet = isDefaultParameterSet;
            _parameters = new ParameterFingerprint[parameters.Length];
            Array.Copy(parameters, _parameters, parameters.Length);
            _mandatoryParameterCount = _parameters.Count(i => i.IsMandatory);
        }
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public string Name => _name;
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int ParameterCount => _parameters.Length;
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public int MandatoryParameterCount => _mandatoryParameterCount;
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public bool IsDefaultParameterSet => _isDefaultParameterSet;
        /// <summary>
        /// The number of permutations possible for the parameter set to be invoked with using only named
        /// parameters (no positional parameters). That is, given a parameter set with three parameters
        /// 'Path', 'Destination', and 'Force', where 'Path' is mandatory, there would be (Path),
        /// (Path, Destination), (Path, Force), and (Path, Destination, Force) = 4 possible permutations.
        /// </summary>
        [JsonIgnore]
        [Newtonsoft.Json.JsonIgnore]
        public long ParameterCombinatorials
            => _parameterCombinatorials == -1
            ? (_parameterCombinatorials = CalculateCombinatorials(ParameterCount, MandatoryParameterCount))
            : _parameterCombinatorials;

        public ParameterFingerprint[] GetParameters()
        {
            if (_parameters.Length == 0) return Array.Empty<ParameterFingerprint>();
            var arr = new ParameterFingerprint[_parameters.Length];
            Array.Copy(_parameters, arr, _parameters.Length);
            return arr;
        }

        public override bool Equals(object? obj)
            => Equals(obj as ParameterSetFingerprint);
        public bool Equals(ParameterSetFingerprint? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_parameters.Length != other._parameters.Length) return false;
            if (_mandatoryParameterCount != other._mandatoryParameterCount) return false;
            if (_isDefaultParameterSet != other._isDefaultParameterSet) return false;
            if (_parameterCombinatorials != -1 && other._parameterCombinatorials != -1 && _parameterCombinatorials != other._parameterCombinatorials)
                return false;
            if (_name != other._name) return false;
            if (GetHashCode() != other.GetHashCode()) return false;
            for (int i = 0; i < _parameters.Length; i++)
            {
                if (!_parameters[i].Equals(other._parameters[i]))
                    return false;
            }
            return true;
        }
        public override int GetHashCode()
        {
            if (_parameters.Length > 1)
            {
                return HashCode.Combine(_name, _parameters.Length, _parameters[0], _parameters[1], _parameters[^2], _parameters[^1]);
            }
            else if (_parameters.Length == 1)
            {
                return HashCode.Combine(_name, _parameters.Length, _parameters[0], _parameters[^1]);
            }
            else
            {
                return HashCode.Combine(_name, _parameters.Length);
            }
        }
        public override string ToString() => _name;

        protected internal override ModuleFingerprintChange IdentifyChangesFromPrevious(FingerprintBase previous)
        {
            var fingerprint = (ParameterSetFingerprint)previous;
            var changes = ModuleFingerprintChange.None;

            if (fingerprint._name != _name) changes |= ModuleFingerprintChange.ParameterSetNameChanged;
            var currentParameters = _parameters.ToDictionary(i => i.Name);
            var previousParameters = fingerprint._parameters.ToDictionary(i => i.Name);
            var parameterNames = currentParameters.Keys.Union(previousParameters.Keys);

            foreach (var parameterName in parameterNames)
            {
                currentParameters.TryGetValue(parameterName, out var currentParameter);
                previousParameters.TryGetValue(parameterName, out var previousParameter);

                if (currentParameter is null && previousParameter is null)
                {
                    continue;
                }
                else if (currentParameter is null && previousParameter is not null)
                {
                    if (previousParameter.IsMandatory) changes |= ModuleFingerprintChange.MandatoryParameterRemoved;
                    else changes |= ModuleFingerprintChange.NonMandatoryParameterRemoved;
                }
                else if (currentParameter is not null && previousParameter is null)
                {
                    if (currentParameter.IsMandatory) changes |= ModuleFingerprintChange.MandatoryParameterAdded;
                    else changes |= ModuleFingerprintChange.NonMandatoryParameterAdded;
                }
                else
                {
                    changes |= currentParameter!.IdentifyChangesFromPrevious(previousParameter!);
                }
            }

            return changes;
        }


        private static long CalculateCombinatorials(int parameterCount, int mandatoryParameterCount)
        {
            // combinations of k objects from a set with n objects is {{n C k}}
            // {{n C k}} = n! / [k! (n-k) !].

            var k = parameterCount;
            var n = parameterCount;

            long total = 0;
            while (k > mandatoryParameterCount)
            {
                var nCk = CalculateFactorial(n) / (CalculateFactorial(k) * CalculateFactorial(n - k));
                total += nCk;
                k--;
            }
            return total;
        }

        private static long CalculateFactorial(int value)
        {
            long result = 1;
            while (value > 1)
            {
                result *= value--;
            }
            return result;
        }
    }
}
