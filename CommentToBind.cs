using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class Program
{
    public static void Main( string[] args )
    {
        var path = ".";
        var output = "out";

        if ( args.Length > 0 ) path = args[0];
        if ( args.Length > 1 ) output = args[1];

        var c = new CommentToBind();
        c.AddPath( path );
        c.ProcessAll();
        c.OutputTo( output );
    }
}

public class CommentToBind
{
    public string nameSpace = "Facepunch.Native";
    public List<string> foundFiles = new List<string>();
    public List<Method> bindings = new List<Method>();

    public void AddPath( string path )
    {
        foundFiles.AddRange( System.IO.Directory.EnumerateFiles( path, "*.cpp", System.IO.SearchOption.AllDirectories ).ToArray() );
    }

    public void ProcessAll()
    {
        Console.WriteLine( "Processing " + foundFiles.Count() + " files" );

        foreach ( var file in foundFiles )
        {
            ProcessFile( file );
        }

        //Parallel.ForEach( foundFiles, ProcessFile );
    }

    public void ProcessFile( string filename )
    {
        var lines = System.IO.File.ReadAllLines( filename );

        Library library = null;

        for ( int i = 0; i < lines.Length; i++ )
        {
            //
            // Header
            //
            if ( lines[i].StartsWith( "// Bind: " ) )
            {
                library = new Library();
                i = library.Read( lines, i );
            }

            if ( library == null || !library.IsValid() ) continue;

            //
            // Entry
            //
            if ( lines[i].StartsWith( "// Function: " ) )
            {
                var b = new Method( library );
                i = b.Read( lines, i );
                bindings.Add( b );
            }
        }

    }

    public void OutputTo( string foldername )
    {
        foldername = foldername.TrimEnd( '/', '\\' );

        if ( !System.IO.Directory.Exists( foldername ) )
            System.IO.Directory.CreateDirectory( foldername );

        // Delete all with .generated in the name

        foreach ( var classbindings in bindings.GroupBy( x => x.library.FileName ) )
        {
            var filename = foldername + "/" + classbindings.Key;
            Console.WriteLine( "Writing " + classbindings.Count() + " functions to " + filename );

            var sb = new StringBuilder();

            sb.AppendLine( "using System;" );
            sb.AppendLine( "using System.Runtime.InteropServices;" );
            sb.AppendLine( "" );

            var indent = "";
            foreach ( var bind in classbindings.GroupBy( x => x.library.ClassName ) )
            {
                var hasGlobalDefines = bind.First().library.ifDef.Count > 0 &&
                                      (bind.GroupBy( x => string.Join( ",", x.library.ifDef ) ).Count() == bind.First().library.ifDef.Count );

                //
                // #if SERVER
                //
                if ( hasGlobalDefines )
                {
                    foreach ( var defined in bind.First().library.ifDef )
                    {
                        sb.AppendFormat( "#if {0}\n", defined );
                    }

                    sb.AppendFormat( "\n" );
                }

                //
                // namespace Facepunch
                //
                foreach ( var m in classbindings.First().library.memberOf )
                {
                    sb.AppendFormat( "{1}{0}\n{1}{{\n", m, indent );
                    indent += "\t";
                }

                //
                // public partial class BaseEntity
                //
                sb.AppendFormat( "{2}{0} {1}{3}\n{2}{{\n", classbindings.First().library.ClassType, classbindings.First().library.ClassName, indent, classbindings.First().library.baseObject );
                indent += "\t";

                //
                // Functions
                //
                foreach ( var binding in classbindings )
                {
                    Method setMethod = null;

                    if ( binding.flags.Contains( "property" ) && binding.returnValue != null )
                        setMethod = classbindings.FirstOrDefault( x => x.functionName == binding.functionName && x != binding );

                    sb.Append( binding.GetDefinition( indent, hasGlobalDefines, setMethod ) );
                }

                //
                // } class
                //
                indent = indent.Substring( 1 );
                sb.AppendFormat( "{0}}}\n", indent );

                //
                // } namespace
                //
                foreach ( var m in classbindings.First().library.memberOf )
                {
                    indent = indent.Substring( 1 );
                    sb.AppendFormat( "{0}}}\n", indent );
                }

                //
                // #endif
                //
                if ( hasGlobalDefines )
                {
                    sb.AppendFormat( "\n" );

                    foreach ( var defined in bind.First().library.ifDef )
                    {
                        sb.AppendFormat( "#endif // {0}\n", defined );
                    }                    
                }
            }

            //
            // Format and save the code
            //
            {
                var code = sb.ToString();
                System.IO.File.WriteAllText( filename, code );
            }
        }
    }

    
}