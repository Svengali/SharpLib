




using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

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

	public static FieldInfo? GetFieldInfo(Type? t, string name)
	{
		if (t == null) return null;

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

	static HashSet<char> s_badChars = new( new char[] { '<', '>', ' ', ',', '.', '+', '[', ']', '$', ':' } );

	static public string TypeToIdentifier(string typename)
	{
		var safeStr = new StringBuilder( typename );

		for( int i = 0; i < safeStr.Length; ++i )
		{
			if( s_badChars.Contains(safeStr[i]) ) safeStr[i] = '_';
		}

		return safeStr.ToString();
	}

	static public FileSystemWatcher s_watcher;
	static public Action<Assembly> s_fnAss = (ass) => {
		log.warn( $"Need to replace s_fnAss with custom function" );
	};

	public static void WatchPluginDir( string dir, Action<Assembly> fnAss )
	{
		log.info( $"Watching {dir} for changes" );

		s_fnAss = fnAss;

		s_watcher = new FileSystemWatcher( dir );

		s_watcher.Created += OnCreated;
		s_watcher.Deleted += OnDeleted;
		s_watcher.Renamed += OnRenamed;

		s_watcher.Filter = "*.cs";
		s_watcher.IncludeSubdirectories = true;
		s_watcher.EnableRaisingEvents = true;

		var existingFiles = Directory.GetFiles( dir, "*.cs", SearchOption.AllDirectories );

		foreach( var filename in existingFiles )
		{
			Process( filename );
		}

	}

	static void OnCreated( object sender, FileSystemEventArgs e )
	{
		log.debug( $"{e.Name} got {e.ChangeType}" );

		if( e.Name.EndsWith( ".cs" ) )
		{
			Process( e.FullPath );
		}
	}

	static void OnDeleted( object sender, FileSystemEventArgs e )
	{
		log.debug( $"{e.Name} got {e.ChangeType}" );
	}

	static void OnRenamed( object sender, FileSystemEventArgs e )
	{
		log.debug( $"{e.Name} got {e.ChangeType}" );

		if( e.Name.EndsWith(".cs") )
		{
			while( true )
			{
				try
				{
					Process( e.FullPath );
					return;
				}
				catch( System.IO.IOException ex )
				{

				}
				catch( Exception ex )
				{
					log.error( $"Got ex {ex.GetType().Name} trying to process {e.FullPath}" );
					log.error( $"-> {ex.Message}" );
					return;
				}

				Thread.Sleep( 100 );
			}
		}
	}

	static void Process( string filename )
	{
		CompileFile( filename, ( ass ) => { s_fnAss( ass ); }, ( diags ) => { } );
	}

	public static void CompileFile( string filename, Action<Assembly> onSuccess, Action<ImmutableArray<Diagnostic>> onFailure, Platform platform = Platform.X86 )
	{
		var fullpath = Path.GetFullPath( filename );

		//string text = System.IO.File.ReadAllText( fullpath );

		var stream = File.OpenRead( fullpath );

		var sourceText = SourceText.From( stream );

		Compile( sourceText, fullpath, onSuccess, onFailure );
	}

	public static void Compile( string str, string uniquePath, Action<Assembly> onSuccess, Action<ImmutableArray<Diagnostic>> onFailure, Platform platform = Platform.X86 )
	{
		var sourceText = SourceText.From( str );

		Compile( sourceText, uniquePath, onSuccess, onFailure );
	}


	public static void Compile( SourceText sourceText, string uniquePath, Action<Assembly> onSuccess, Action<ImmutableArray<Diagnostic>> onFailure, Platform platform = Platform.X86 )
	{
		string assemblyName = Path.GetRandomFileName();

		var options = new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose, kind: SourceCodeKind.Regular);

		SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText( sourceText, options, uniquePath );

		//SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceText, options, uniquePath, encoding: System.Text.Encoding.UTF8);

		var memRef = new MemoryRefResolver();


		using (var ms = new MemoryStream())
		using (var pdb = new MemoryStream())
		{
			var result = CompileAndEmit(assemblyName, new[] { syntaxTree }, ms, pdb, platform);

			if (!result.Success)
			{
				if (onFailure == null)
				{
				}
				else
				{
					LogDiags( uniquePath, result.Diagnostics.Length, result.Diagnostics );

					onFailure( result.Diagnostics );
				}
			}
			else
			{
				ms.Seek(0, SeekOrigin.Begin);
				var assembly = Assembly.Load(ms.ToArray(), pdb.ToArray());

				onSuccess( assembly );
			}
		}
	}

	public static void LogDiags( string uniquePath, int count, IEnumerable<Diagnostic> diags )
	{
		log.warn( $"{count} Problems building script with name {uniquePath}" );
		foreach( var diag in diags )
		{
			log.debug( $"{diag}" );
		}
	}

	private static EmitResult CompileAndEmit(string assemblyName, SyntaxTree[] syntaxTrees, MemoryStream ms, MemoryStream pdb, Platform platform)
	{
		var memRef = new MemoryRefResolver();

		CSharpCompilation compilation = CSharpCompilation.Create(
				assemblyName,
				syntaxTrees: syntaxTrees,
				references: RefCache.References,
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

	private static class RefCache
	{
		// create the list of references on first use, but if two threads both *start* making the list thats fine since we'll just use whichever wins.
		private static readonly Lazy<ImmutableArray<MetadataReference>> lazyRef = new Lazy<ImmutableArray<MetadataReference>>(GetReferences, LazyThreadSafetyMode.PublicationOnly);

		public static IReadOnlyList<MetadataReference> References => lazyRef.Value;

		private static ImmutableArray<MetadataReference> GetReferences()
		{
			var builder = ImmutableArray.CreateBuilder<MetadataReference>();

			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach( var ass in assemblies )
			{
				if( ass != null && !ass.IsDynamic && ass.Location != null )
				{
					try
					{
						builder.Add( MetadataReference.CreateFromFile( ass.Location ) );
					}
					catch( Exception ex )
					{
						log.warn( $"Got {ex.GetType().Name} sayaing {ex.Message}" );
					}
				}
			}

			return builder.ToImmutable();
		}
	}

	public static string PrettyName( Type t )
	{
		if( t.GetGenericArguments().Length == 0 )
		{
			return t.FullName.Replace( '+', '.' );
		}
		var genArgs = t.GetGenericArguments();
		var typeDef = t.FullName;

		var indexOfTick = typeDef.IndexOf("`");

		var unmangledOuterName = typeDef.Substring(0, typeDef.IndexOf('`')).Replace('+', '.');

		var innerName = "";

		//Check for inner class
		if( typeDef.ElementAt( indexOfTick + 2 ) != '[' )
		{
			var indexOfOpenBracket = typeDef.IndexOf('[', indexOfTick);

			innerName = typeDef.Substring( indexOfTick + 2, indexOfOpenBracket - (indexOfTick + 2) ).Replace( '+', '.' );
		}

		return unmangledOuterName + "<" + String.Join( ",", genArgs.Select( PrettyName ) ) + ">" + innerName;
	}


	private static void AddIfFirst<TKey, TValue>(IDictionary<TKey, TValue> dict, TKey key, TValue value)
	{
		if (!dict.ContainsKey(key))
		{
			dict.Add(key, value);
		}
	}
}

