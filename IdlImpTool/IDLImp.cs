// Copyright (c) 2002-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace SIL.IdlImporterTool
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Imports the interfaces of an IDL file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class IDLImpConsole
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		/// <returns><c>0</c> for success, <c>1</c> for internal error, <c>2</c> for error
		/// in data.</returns>
		/// ------------------------------------------------------------------------------------
		[STAThread]
		public static int Main(string[] args)
		{
			string sFileName;
			var inputFile = new Argument<FileInfo>("file.idl")
			{
				Description = ".IDL file to process"
			};
			Option<String> outputFile = new("--output", ["-o", "/o"])
			{
				Description = "/o outfile\tname of created file (Default: file.cs)"
			};
			Option<String> configFile = new("--config", ["-c", "/c"])
			{
				Description = $"/c configfile\tname of XML configuration file",
				DefaultValueFactory = _ => Path.ChangeExtension(
					Path.GetFileName(Assembly.GetEntryAssembly().Location),
					"xml"
				)
			};
			Option<String> genNamespace = new("--namespace", ["-n", "/n"])
			{
				Description = "/n namespace\tNamespace of the file to be produced"
			};
			Option<List<String>> usingNamespaces = new("--using", ["-u", "/u"])
			{
				Description = "/u namespace\tadditional using namespaces"
			};
			Option<List<String>> idhFiles = new("--idh", ["-i", "/i"])
			{
				Description = "/i idhfile\tname of IDH file for comments"
			};
			Option<List<String>> refFiles = new("--ref", ["-r", "/r"])
			{
				Description = "/r jsonfile\tFile name of .json file to use to resolve references"
			};
			Option<int> genComments = new("--comments", ["-x", "/x"])
			{
				Description = "/x (0|1)\t1= create, 0= suppress XML comments",
				DefaultValueFactory = _ => 1
			};

			RootCommand rootCommand = new("""
IDLImporter. Creates .NET interfaces from an IDL file.
Copyright (c) 2002-2022, SIL International. All Rights Reserved.
""")
			{
				inputFile,
				outputFile,
				configFile,
				genNamespace,
				usingNamespaces,
				idhFiles,
				refFiles,
				genComments,
			};

			rootCommand.SetAction(parseResult =>
			{
				if (parseResult.Errors.Count == 0 && parseResult.GetValue(inputFile) is FileInfo parsedFile)
				{
					sFileName = parsedFile.FullName;
				}
				else
				{
					foreach (ParseError parseError in parseResult.Errors)
					{
						Console.Error.WriteLine(parseError.Message);
					}
					Environment.Exit(1);
					return;
				}

				// Get all necessary file names
				string sXmlFile = parseResult.GetValue(configFile);
				string sOutFile = parseResult.GetValue(outputFile) ?? Path.ChangeExtension(sFileName, "cs");
				string sNamespace = parseResult.GetValue(genNamespace) ?? Path.GetFileNameWithoutExtension(sFileName);

				Console.WriteLine("Generating {0}...", Path.GetFileName(sOutFile));

				IDLImporter imp = new IDLImporter();
				var fOk = imp.Import(
					parseResult.GetValue(usingNamespaces),
					sFileName,
					sXmlFile,
					sOutFile,
					sNamespace,
					parseResult.GetValue(idhFiles),
					parseResult.GetValue(refFiles),
					parseResult.GetValue(genComments) == 1);

				Environment.Exit(fOk ? 0 : 2);
			});

			try
			{
				return rootCommand.Parse(args).Invoke();
			}
			catch(Exception e)
			{
				System.Console.WriteLine("Internal program error in program {0}", e.Source);
				System.Console.WriteLine("\nDetails:\n{0}\nin method {1}.{2}\nStack trace:\n{3}",
					e.Message, e.TargetSite.DeclaringType.Name, e.TargetSite.Name, e.StackTrace);

				return 1;
			}
			return 1;
		}
	}
}
