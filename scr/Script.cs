using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

public class MemoryRefResolver : SourceReferenceResolver
{
	public override bool Equals(object other)
	{
		return false;
	}

	public override int GetHashCode()
	{
		return 0;
	}

	public override string NormalizePath(string path, string baseFilePath)
	{
		return path;
	}

	public override Stream OpenRead(string resolvedPath)
	{
		return null;
	}

	public override SourceText ReadText(string resolvedPath)
	{
		return null;
	}

	public override string ResolveReference(string path, string baseFilePath)
	{
		return null;
	}

}




public static class scr
{


	public static bool firstErrorSourceLogged = false;

	private static class ReferencesCache
	{
		// create the list of references on first use, but if two threads both *start* making the list thats fine since we'll just use whichever wins.
		private static readonly Lazy<ImmutableArray<MetadataReference>> lazyReferences = new Lazy<ImmutableArray<MetadataReference>>(GetReferences, LazyThreadSafetyMode.PublicationOnly);

		public static IReadOnlyList<MetadataReference> References => lazyReferences.Value;

		private static ImmutableArray<MetadataReference> GetReferences()
		{
			var builder = ImmutableArray.CreateBuilder<MetadataReference>();

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (var ass in assemblies)
			{
				if (ass != null && !ass.IsDynamic && ass.Location != null)
				{
					try
					{
						builder.Add(MetadataReference.CreateFromFile(ass.Location));
					}
					catch (Exception)
					{
					}
				}
			}

			return builder.ToImmutable();
		}
	}

	public static string CleanString(string dirty)
	{
		// TODO @@ Get a clean string implementation in
		return dirty; //return Newtonsoft.Json.JsonConvert.ToString(dirty);
	}

	public static string PrettyName(Type type)
	{
		if (type.GetGenericArguments().Length == 0)
		{
			return type.FullName.Replace('+', '.');
		}
		var genericArguments = type.GetGenericArguments();
		var typeDef = type.FullName;

		var indexOfTick = typeDef.IndexOf("`");

		var unmangledOuterName = typeDef.Substring(0, typeDef.IndexOf('`')).Replace('+', '.');

		var innerName = "";

		//Check for inner class
		if (typeDef.ElementAt(indexOfTick + 2) != '[')
		{
			var indexOfOpenBracket = typeDef.IndexOf('[', indexOfTick);

			innerName = typeDef.Substring(indexOfTick + 2, indexOfOpenBracket - (indexOfTick + 2)).Replace('+', '.');
		}

		return unmangledOuterName + "<" + String.Join(",", genericArguments.Select(PrettyName)) + ">" + innerName;
	}

	public static FieldInfo GetFieldInfo(Type t, string name)
	{
		if (t == null)
			return null;

		var fi = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

		if (fi != null)
			return fi;

		if (t.BaseType != null)
			return GetFieldInfo(t.BaseType, name);

		return null;
	}

	// From stack overflow
	static Lazy<ISet<Type>> typeSetLazy =
						new Lazy<ISet<Type>>(() => {
							var types = AppDomain
										.CurrentDomain
										.GetAssemblies()
										.SelectMany(a => a.GetTypes()
												.Where(t => t.IsClass));
							var typesAndBaseTypes = types
										.Select(t => new { Type = t, t.BaseType })
										.ToList();
							var typesWithSubclasses = typesAndBaseTypes
										.Join(
												typesAndBaseTypes,
												t => t.Type,
												t => t.BaseType,
												(t1, t2) => t2.BaseType);
							var typesHs = new HashSet<Type>(types);
							typesHs.ExceptWith(typesWithSubclasses);
							return typesHs;
						});

	static bool IsLeafType(this Type type)
	{
		return typeSetLazy.Value.Contains(type);
	}

	static public string TypeToIdentifier(string typename)
	{
		return typename.Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_').Replace('.', '_').Replace('+', '_').Replace('[', '_').Replace(']', '_').Replace('$', '_').Replace(':', '_');
	}

	public static void LogDiagnosticErrorsAndWarnings(ImmutableArray<Diagnostic> diagnostics, string sourceFile)
	{
		foreach (Diagnostic diagnostic in diagnostics)
		{
			//if (diagnostic.Severity != DiagnosticSeverity.Hidden)
			{
				switch (diagnostic.Severity)
				{
					case DiagnosticSeverity.Error:
						{
							var msg = $"{sourceFile}({diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}): {diagnostic.GetMessage()}";
							System.Diagnostics.Debug.WriteLine(msg);

						}
						break;

					case DiagnosticSeverity.Warning:
					case DiagnosticSeverity.Info:
						{
							var msg = $"{sourceFile}({diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}): {diagnostic.GetMessage()}";
							System.Diagnostics.Debug.WriteLine(msg);
						}
						break;
				}
			}
		}
	}

	public static void Compile(string dynamicScript, string uniquePath, Action<Assembly> onSuccess, Action<ImmutableArray<Diagnostic>> onFailure, Platform platform = Platform.X86)
	{
		string assemblyName = Path.GetRandomFileName();

		var parseOptions = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose, kind: SourceCodeKind.Regular);

		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(dynamicScript, parseOptions, uniquePath, encoding: System.Text.Encoding.UTF8);

		var memRef = new MemoryRefResolver();

		/*
		CSharpCompilation compilation = CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: new[] { syntaxTree },
				references: ReferencesCache.References,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
						sourceReferenceResolver: memRef,
						optimizationLevel: OptimizationLevel.Debug,
						platform: platform));
		*/

		using (var ms = new MemoryStream())
		using (var pdb = new MemoryStream())
		{
			var result = CompileAndEmit(assemblyName, new[] { syntaxTree }, ms, pdb, platform);

			if (!result.Success)
			{
				if (onFailure == null)
				{
					LogDiagnosticErrorsAndWarnings(result.Diagnostics, "unknown");
				}
				else
				{
					onFailure(result.Diagnostics);
				}
			}
			else
			{
				/*
				foreach (Diagnostic diagnostic in result.Diagnostics)
				{
						//if (diagnostic.Severity != DiagnosticSeverity.Hidden)
						{
								if (diagnostic.Id == "CS1591") continue;

								switch (diagnostic.Severity)
								{
										case DiagnosticSeverity.Error:
										{
												var msg = $"{source.file}({diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}): {diagnostic.GetMessage()}";
												log.Error(msg);
												System.Diagnostics.Debug.WriteLine(msg);

										}
										break;

										case DiagnosticSeverity.Warning:
										case DiagnosticSeverity.Info:
										{
												var msg = $"{source.file}({diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}): {diagnostic.GetMessage()}";
												log.Warn(msg);
												System.Diagnostics.Debug.WriteLine(msg);
										}
										break;
								}
						}
				}
				*/

				ms.Seek(0, SeekOrigin.Begin);
				var assembly = Assembly.Load(ms.ToArray(), pdb.ToArray());

				onSuccess(assembly);


			}
		}
	}

	private static EmitResult CompileAndEmit(string assemblyName, SyntaxTree[] syntaxTrees, MemoryStream ms, MemoryStream pdb, Platform platform)
	{
		var memRef = new MemoryRefResolver();

		CSharpCompilation compilation = CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: syntaxTrees,
				references: ReferencesCache.References,
				options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
						sourceReferenceResolver: memRef,
						optimizationLevel: OptimizationLevel.Debug,
						platform: platform,
						specificDiagnosticOptions: new Dictionary<string, ReportDiagnostic>
						{
							{ "CS1701", ReportDiagnostic.Suppress }
						}));

		return compilation.Emit(ms, pdb);
	}

	private static void AddIfFirst<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key, TValue value)
	{
		if (!dict.ContainsKey(key))
		{
			dict.Add(key, value);
		}
	}
}

