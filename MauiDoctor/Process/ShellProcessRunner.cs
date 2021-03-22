﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MauiDoctor
{
	public class ShellProcessRunner
	{

		public static ShellProcessResult Run(string executable, string args)
		{
			var p = new ShellProcessRunner(executable, args, System.Threading.CancellationToken.None);
			return p.WaitForExit();
		}

		readonly List<string> standardOutput;
		readonly List<string> standardError;
		readonly Process process;

		public Action<string> OutputHandler { get; private set; }

		public ShellProcessRunner(string executable, string args, System.Threading.CancellationToken cancellationToken, Action<string> outputHandler = null, bool useSystemCmd = true, bool redirectStdInput = false, bool redirectOutput = true)
		{
			OutputHandler = outputHandler;

			standardOutput = new List<string>();
			standardError = new List<string>();

			process = new Process();

			if (useSystemCmd)
			{
				// process.StartInfo.FileName = Util.IsWindows ? "cmd.exe" : (File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash");
				// process.StartInfo.Arguments = Util.IsWindows ? $"/c \"{executable} {args}\"" : $"-c \"{executable} {args}\"";
				process.StartInfo.FileName = Util.IsWindows ? executable : (File.Exists("/bin/zsh") ? "/bin/zsh" : "/bin/bash");
				process.StartInfo.Arguments = Util.IsWindows ? args : $"-c \"{executable} {args}\"";
			}
			else
			{
				process.StartInfo.FileName = executable;
				process.StartInfo.Arguments = args;
			}

			process.StartInfo.UseShellExecute = false;

			if (redirectOutput)
			{
				process.StartInfo.RedirectStandardOutput = true;
				process.StartInfo.RedirectStandardError = true;
			}

			// Process any env variables to be set that might have been set by other checkups
			// ie: JavaJdkCheckup sets MAUI_DOCTOR_JAVA_HOME
			foreach (var ev in Util.EnvironmentVariables)
				process.StartInfo.Environment[ev.Key] = ev.Value?.ToString();

			if (redirectStdInput)
				process.StartInfo.RedirectStandardInput = true;

			process.OutputDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardOutput.Add(e.Data);
					OutputHandler?.Invoke(e.Data);
				}
			};
			process.ErrorDataReceived += (s, e) =>
			{
				if (e.Data != null)
				{
					standardError.Add(e.Data);
					OutputHandler?.Invoke(e.Data);
				}
			};
			process.Start();

			if (redirectOutput)
			{
				process.BeginOutputReadLine();
				process.BeginErrorReadLine();
			}

			if (cancellationToken != System.Threading.CancellationToken.None)
			{
				cancellationToken.Register(() =>
				{
					try { process.Kill(); }
					catch { }

					try { process?.Dispose(); }
					catch { }
				});
			}
		}

		public void Write(string txt)
			=> process.StandardInput.Write(txt);

		public int ExitCode
			=> process.HasExited ? process.ExitCode : -1;

		public bool HasExited
			=> process?.HasExited ?? false;

		public void Kill()
			=> process?.Kill();

		public ShellProcessResult WaitForExit()
		{
			try
			{
				process.WaitForExit();
			} catch { }

			if (standardError?.Any(l => l?.Contains("error: more than one device/emulator") ?? false) ?? false)
				throw new Exception("More than one Device/Emulator detected, you must specify which Serial to target.");

			return new ShellProcessResult(standardOutput, standardError, process.ExitCode);
		}

		public async Task<ShellProcessResult> WaitForExitAsync()
		{
			await process.WaitForExitAsync();

			return new ShellProcessResult(standardOutput, standardError, process.ExitCode);
		}

		public class ShellProcessResult
		{
			public readonly List<string> StandardOutput;
			public readonly List<string> StandardError;

			public string GetOutput()
				=> string.Join(Environment.NewLine, StandardOutput.Concat(StandardError));

			public readonly int ExitCode;

			public bool Success
				=> ExitCode == 0;

			internal ShellProcessResult(List<string> stdOut, List<string> stdErr, int exitCode)
			{
				StandardOutput = stdOut;
				StandardError = stdErr;
				ExitCode = exitCode;
			}
		}
	}
}
