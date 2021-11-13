@{
    Projects = @(
        @{
            Name               = 'PSSharp.ModuleFactory'
            BuildVersion       = '1.0.0.4'
            ReleaseVersion     = '1.0.0'
            Manifest           = @{
                Author      = 'Caleb Frederickson'
                CompanyName = 'PSSharp'
            }

            FileConfiguration  = @{
                ScriptFiles    = 'src/PowerShell/PSSharp.ModuleFactory/**/*.psm1'
                HelpFiles      = 'src/Documentation/PSSharp.ModuleFactory'
                DotnetProjects = @(
                    @{
                        # The name of the DotNet project
                        Name      = 'PSSharp.ModuleFactory'
                        # The framework(s) to build for, and where to output the contents to
                        # Can also be a string, in which case the output path will be ''
                        # An element can also be a single string, in which case the output
                        # path will be the same as the framework
                        Framework = @(
                            # Framework is the framework to build
                            # OutputPath is the output path, relative to the output of the module
                            # during module build.
                            @{ Framework = 'net5.0' ; OutputPath = '' }
                        )
                        # The path to the dotnet project root
                        Path      = 'src/CSharp/PSSharp.ModuleFactory'
                    }
                )
                ExternalFiles  = @(
                    # Additional files that will be copied to the module output
                    'src/PowerShell/PSSharp.ModuleFactory'
                )
                PesterTests    = 'Tests/PSSharp.ModuleFactory'
            }

            ExportedFunctions  = @{
                # A function name pattern such as '*-*' to export all functions with a hyphen in the name.
                Pattern      = $null
                # Export functions according to the name of a file.
                FileMatching = 'src/PowerShell/PSSharp.ModuleFactory/Public/**/*.*'
                # Export commands that are in a static list of function names.
                Functions    = $null
            }

            ExportedCmdlets    = @{
                # Determine exports by matching against a file name pattern.
                FileMatching = 'src/CSharp/PSSharp.ModuleFactory/Commands/Public/**/*.*'
                # Export commands that are in a static list of cmdlet names.
                Commands     = $null
            }

            # Not defined => export all
            # ExportedAliases = @()
            ExportedVariables  = @()
            # ExportedTypeFiles = @()
            # ExportedFormatFiles = @()

            BuildConfiguration = @(
                @{
                    # Name of the configuration
                    Configuration             = 'Debug'
                    # Determines if script files will be joined into a 'monolith' file
                    CompileScriptFiles        = $false
                    RunTestsOnSuccessfulBuild = $false

                    VersionBehavior           = @{
                        # Auto-incrment the module version
                        IncrementOnBuild = $true
                        # One of 'Major', 'Minor', 'Revision', 'Build' to be incremented.
                        # Mutually exclusive to FingerprintPath
                        Step             = 'Build'
                        # A file path that stores a fingerprint of the public components of a module, which is compared
                        # to a build to determine the portion of the version number to step.
                        FingerprintPath  = $null
                    }
                    # Static version to build the module at. Mutually exclusive to VersionBehavior.
                    Version                   = '1.0.0'

                    OutputPath                = 'Build/Debug/{{Name}}'
                    PrebuildCommands          = @()
                    PostbuildCommands         = @()
                },
                @{
                    Configuration             = 'Publish'
                    CompileScriptFiles        = $true
                    RunTestsOnSuccessfulBuild = $true
                    VersionBehavior           = @{
                        IncrementOnBuild = $true
                        FingerprintPath  = 'Build/Fingerprint/PSSharp.ModuleFactory-Debug.fingerprint'
                    }

                    OutputPath                = 'Build/{{Name}}/{{Version}}'
                    PrebuildCommands          = @()
                    PostbuildCommands         = @()
                }
            )
        }
    )
}