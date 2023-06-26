using System.Diagnostics;

namespace CrossCommands.Dev.Test;

class Program
{
    /// <summary>
    /// Execute the program
    /// </summary>
    /// <param name="args">Command line arguments</param>
    static void Main(string[] args)
    {
        Cross.Commands.RunCommand("", "git --version", createNewWindow: false, useShellExecute: false, redirectStandardError: true, redirectStandardOutput: true, errorOutputMethod: GitInstalledCheck, standardOutputMethod: GitInstalledCheck);
    }

    static void GitInstalledCheck(object sender, DataReceivedEventArgs e)
    {
        Console.WriteLine(e.Data);
        if (true) ;
    }
}

