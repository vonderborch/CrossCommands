using System;
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

namespace CrossCommands
{
    /// <summary>
    /// Executes a command regardless of operating system
    /// </summary>
	public class Cross
    {
        /// <summary>
        /// The lazy
        /// </summary>
        private static readonly Lazy<Cross> lazy = new Lazy<Cross>(() => new Cross());

        private OS operatingSystem = OS.Unknown;

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static Cross Commands => lazy.Value;

        /// <summary>
        /// Initializes the CrossCommands class. Mostly used for determining what platform we're running on
        /// </summary>
        private Cross()
        {
            if (OperatingSystem.IsWindows())
            {
                operatingSystem = OS.Windows;
            }
            else if (OperatingSystem.IsMacOS())
            {
                operatingSystem = OS.MacOS;
            }
            else if (OperatingSystem.IsLinux())
            {
                operatingSystem = OS.Linux;
            }
            else
            {
                throw new NotImplementedException("CrossCommands is not implemented for your operating system!");
            }
        }

        /// <summary>
        /// Executes the command in the specified working directory
        /// </summary>
        /// <param name="directory">The working directory to execute the command in</param>
        /// <param name="command">The command to execute</param>
        /// <param name="arguments">Arguments for the command</param>
        /// <param name="waitForCompletion">True to wait for the command to finish, False otherwise</param>
        /// <param name="raiseOnError">True to raise on error, False otherwise</param>
        /// <param name="createNewWindow">True to create a new window, False otherwise</param>
        /// <param name="useShellExecute">True to use shell execution, False otherwise</param>
        /// <param name="redirectStandardInput">True to redirect input, False otherwise</param>
        /// <param name="redirectStandardOutput">True to redirect output, False otherwise</param>
        /// <param name="redirectStandardError">True to redirect error, False otherwise</param>
        /// <param name="standardOutputMethod">The method to use for standard error</param>
        /// <param name="errorOutputMethod">The method to use for standard input</param>
        /// <returns>True if command completed without error, False otherwise</returns>
        /// <exception cref="NotImplementedException">The OS is unsupported by CrossCommands</exception>
        public bool RunCommand(
            string directory,
            string command,
            List<string>? arguments = null,
            bool waitForCompletion = true,
            bool raiseOnError = true,
            bool createNewWindow = false,
            bool useShellExecute = true,
            bool redirectStandardInput = false,
            bool redirectStandardOutput = false,
            bool redirectStandardError = false,
            DataReceivedEventHandler? standardOutputMethod = null,
            DataReceivedEventHandler? errorOutputMethod = null
        )
        {
            // parameter error checking
            if (redirectStandardOutput && standardOutputMethod == null)
            {
                throw new ArgumentNullException(nameof(standardOutputMethod));
            }
            if (redirectStandardError && errorOutputMethod == null)
            {
                throw new ArgumentNullException(nameof(errorOutputMethod));
            }

            // format the command and arguments
            var actualCommand = command;
            if (arguments?.Count > 0)
            {
                actualCommand = $"{actualCommand} {string.Join(' ', arguments)}";
            }

            // create the process start info object
            var startInfo = new ProcessStartInfo();

            // populate the start info object...
            switch (operatingSystem)
            {
                // Execute against the Windows Command Line!
                case OS.Windows:
                    startInfo.FileName = "CMD.exe";
                    actualCommand = $"/C cd \"{directory}\" & {actualCommand}";
                    break;

                // Execute against the macOS Terminal!
                case OS.MacOS:
                    if (!command.EndsWith(".sh"))
                    {
                        throw new NotImplementedException("CrossCommands only currently supports executing .sh scripts on macOS!");
                    }
                    startInfo.FileName = "osascript";
                    startInfo.Verb = "runas";
                    actualCommand = Path.Combine(directory, actualCommand);
                    actualCommand = $"-e 'tell application \"Terminal\" to activate' -e 'tell application \"Terminal\" to do script \"cd \"{directory}\" && sh {actualCommand}\"'";
                    break;

                // Execute against the Linux Shell!
                case OS.Linux:
                    startInfo.FileName = "/bin/bash";
                    actualCommand = $"cd \"{directory}\" && {actualCommand}";
                    break;

                // Not implemented exception! Should have been caught earlier, but just to be safe...
                default:
                    throw new NotImplementedException("CrossCommands is not implemented for your operating system!");
            }

            // common parameters
            startInfo.CreateNoWindow = !createNewWindow;
            startInfo.UseShellExecute = useShellExecute;
            startInfo.RedirectStandardError = redirectStandardError;
            startInfo.RedirectStandardInput = redirectStandardInput;
            startInfo.RedirectStandardOutput = redirectStandardOutput;

            // try executing the command...
            try
            {
                startInfo.Arguments = actualCommand;
                var process = new Process()
                {
                    StartInfo = startInfo,
                };

                if (errorOutputMethod != null)
                {
                    if (!redirectStandardError)
                    {
                        throw new ArgumentException($"{nameof(redirectStandardError)} must be true if {nameof(errorOutputMethod)} is set!");
                    }
                    process.ErrorDataReceived += errorOutputMethod;
                }
                if (standardOutputMethod != null)
                {
                    if (!redirectStandardOutput)
                    {
                        throw new ArgumentException($"{nameof(redirectStandardOutput)} must be true if {nameof(standardOutputMethod)} is set!");
                    }
                    process.OutputDataReceived += standardOutputMethod;
                }

                process.Start();
                if (waitForCompletion)
                {
                    process.WaitForExit();
                }
            }
            catch (Exception _)
            {
                if (raiseOnError)
                {
                    throw;
                }
                return false;
            }
            return true;
        }
	}
}

