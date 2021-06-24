﻿using CarinaStudio.Collections;
using CarinaStudio.ULogViewer.Logs.DataSources;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CarinaStudio.ULogViewer.Logs
{
	/// <summary>
	/// Tests of <see cref="LogReader"/>.
	/// </summary>
	[TestFixture]
	class LogReaderTests : AppBasedTests
	{
		// Constants.
		const string TimestampFormat = "yyyy-MM-dd HH-mm-ss.SSS";


		// Static fields.
		static readonly Regex LogHeaderRegex = new Regex("^(?<Timestamp>[^\\s]+[\\s]+[^\\s]+)[\\s]+(?<Level>[^\\s]+)[\\s]+(?<SourceName>[^\\s]+)\\:[\\s]*(?<Message>.*)$");
		static readonly Dictionary<string, LogLevel> LogLevelMap = new Dictionary<string, LogLevel>()
		{
			{ "D", LogLevel.Debug },
			{ "E", LogLevel.Error },
			{ "F", LogLevel.Fatal },
			{ "I", LogLevel.Info },
			{ "V", LogLevel.Verbose },
			{ "W", LogLevel.Warn },
		};
		static readonly Regex LogMessageRegex = new Regex("^[\\s]{2}(?<Message>.*)$");
		static readonly Regex LogTailRegex = new Regex("^[\\s]{2}\\[TAIL\\]$");


		// Fields.
		string? testDirectoryPath;


		/// <summary>
		/// Delete generated test directory.
		/// </summary>
		[OneTimeTearDown]
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void DeleteTestDirectory()
		{
			if (this.testDirectoryPath != null)
			{
				Directory.Delete(this.testDirectoryPath, true);
				this.testDirectoryPath = null;
			}
		}


		// Generate header line of log.
		string GenerateLogHeaderLine()
		{
			var timestampString = DateTime.Now.ToString(TimestampFormat);
			var levelString = ((LogLevel[])Enum.GetValues(typeof(LogLevel))).SelectRandomElement() switch
			{
				LogLevel.Debug => "D",
				LogLevel.Error => "E",
				LogLevel.Fatal => "F",
				LogLevel.Info => "I",
				LogLevel.Verbose => "V",
				LogLevel.Warn => "W",
				_ => "U",
			};
			var sourceName = Tests.Random.GenerateRandomString(4);
			var message = Tests.Random.GenerateRandomString(8);
			return $"{timestampString} {levelString} {sourceName}: {message}";
		}


		// Generate file with random logs.
		[MethodImpl(MethodImplOptions.Synchronized)]
		string GenerateLogFile(int logCount)
		{
			if (this.testDirectoryPath == null)
				this.testDirectoryPath = this.Application.CreatePrivateDirectory(this.GetType().Name + "_test").FullName;
			return Tests.Random.CreateFileWithRandomName(this.testDirectoryPath).Use(stream =>
			{
				var isFirstLine = true;
				using var writer = new StreamWriter(stream, Encoding.UTF8);
				foreach (var logLine in this.GenerateLogLines(logCount))
				{
					if (!isFirstLine)
						writer.WriteLine();
					else
						isFirstLine = false;
					writer.Write(logLine);
				}
				return stream.Name;
			});
		}


		// Generate random log lines.
		string[] GenerateLogLines(int logCount) => new List<string>().Also(it =>
		{
			if (Tests.Random.Next(2) == 0)
				it.Add("(Invalid)");
			for (var i = 0; i < logCount; ++i)
			{
				var messageLineCount = Tests.Random.Next(1, 5);
				it.Add(this.GenerateLogHeaderLine());
				for (var j = 1; j <= messageLineCount; ++j)
					it.Add(this.GenerateLogMessageLine());
				it.Add(this.GenerateLogTailLine());
				if (Tests.Random.Next(2) == 0)
					it.Add("(Invalid)");
			}
		}).ToArray();


		// Generate message line of log.
		string GenerateLogMessageLine() => $"  {Tests.Random.GenerateRandomString(8)}";


		// Generate tail line of log.
		string GenerateLogTailLine() => "  [TAIL]";
	}
}
