using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class Method
{
    public string functionName;
    public string entryPoint;
    public string Wrapper = null;
    public string PropertyType = null;

    public string[] flags;
    string description = "";

    List<Argument> args = new List<Argument>();
    List<string> ifDef = new List<string>();

    public Argument returnValue;
    public Library library;

    public Method( Library library )
    {
        this.library = library;
    }

    public class Argument
    {
        public string name;
        public string type;
        public string description;
    }

    public void AddLine( string line )
    {
        line = line.Substring( 3 );
        var parts = line.Split( new[] { ':' }, System.StringSplitOptions.RemoveEmptyEntries );
        if ( parts.Length != 2 ) return;

        switch ( parts[0] )
        {
            case "Function":
                {
                    functionName = parts[1].Trim();
                    return;
                }

            case "Flags":
                {
                    flags = parts[1].Split( new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries ).Select( x => x.Trim().ToLower() ).ToArray();
                    return;
                }

            case "Description":
                {
                    description += parts[1];
                    return;
                }

            case "Arg":
                {
                    var arg = parts[1].Split( new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries );
                    if ( arg.Length != 3 ) return;
                    args.Add( new Argument() { type = arg[0].Trim(), name = arg[1].Trim(), description = arg[2].Trim() } );
                    return;
                }

            case "Return":
                {
                    var arg = parts[1].Split( new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries );
                    if ( arg.Length != 3 ) return;
                    returnValue = new Argument() { type = arg[0].Trim(), name = arg[1].Trim(), description = arg[2].Trim() };
                    return;
                }
            case "IfDef":
                {
                    ifDef.Add( parts[1].Trim() );
                    return;
                }
            case "Wrap":
                {
                    Wrapper = parts[1].Trim();
                    return;
                }
            case "Property":
                {
                    PropertyType = parts[1].Trim();
                    return;
                }
        }

        Console.WriteLine( "Unhandled key: " + parts[0] );
    }

    internal int Read( string[] lines, int i )
    {
        while ( lines[i].StartsWith( "//" ) )
        {
            if ( lines[i].Contains( ":" ) )
            {
                AddLine( lines[i] );
            }

            i++;
        }

        ReadFunctionLine( lines[i] );

        return i;
    }

    public void ReadFunctionLine( string line )
    {
        var firstBracket = line.IndexOf( '(' );
        var firstSpaceBeforeBracket = line.LastIndexOf( ' ', firstBracket );
        entryPoint = line.Substring( firstSpaceBeforeBracket, firstBracket - firstSpaceBeforeBracket ).Trim();
    }

    public string GetComment( string indent, bool minimal = false )
    {
        var sb = new StringBuilder();

        sb.AppendFormat( "{0}/// <summary>\n", indent );
        sb.AppendFormat( "{0}/// {1}\n", indent, description.Trim() );
        sb.AppendFormat( "{0}/// </summary>\n", indent );

        if ( !minimal )
        {
            foreach ( var arg in args )
            {
                sb.AppendFormat( "{0}/// <param name=\"{1}\">{2}</param>\n", indent, arg.name.Trim(), arg.description.Trim() );
            }

            if ( returnValue != null )
                sb.AppendFormat( "{0}/// <returns>{1}</returns>\n", indent, returnValue.description.Trim() );
        }

        return sb.ToString();
    }

    public string GetDefinition( string indent, bool hasGlobalDefines, Method setMethod = null )
    {
        var sb = new StringBuilder();

        if ( library.ifDef.Count > 0 )
        {
            foreach ( var defined in library.ifDef )
            {
                sb.AppendFormat( "{0}#if {1}\n", indent, defined );
            }
        }

        if ( ifDef.Count > 0 )
        {
            foreach ( var defined in ifDef )
            {
                sb.AppendFormat( "{0}#if {1}\n", indent, defined );
            }
        }

        var internalReturnType = returnValue == null ? "void" : returnValue.type.Trim();
        var marshalFunction = returnValue == null ? "{0};" : "return {0};"; // {0} = "Internal_FUNC()"
        bool needsMarshalFunction = flags.Contains( "member" );

        if ( returnValue != null && returnValue.type == "string" )
        {
            needsMarshalFunction = true;
            internalReturnType = "IntPtr";
            marshalFunction = "IntPtr ptr = {0};\nif ( ptr == IntPtr.Zero ) return string.Empty;\n\nreturn Marshal.PtrToStringAnsi( ptr );";
        }

        if ( flags.Contains( "property" ) )
        {
            needsMarshalFunction = true;
        }


        //
        // MEMBER MARSHAL FUNCTION
        //
        if ( needsMarshalFunction )
        {
            // Internal_FUNCNAME( STRAIGHT ARGUMENTS )
            var internalFunctionCall = string.Format( "{0}({1})", FunctionName( true ),  GetArgumentString( false, true ) );

            if ( Wrapper != null ) internalFunctionCall = Wrapper.Replace( "#", internalFunctionCall );

            if ( flags.Contains( "property" ) )
            {
                if ( returnValue != null )
                {
                    sb.AppendFormat( "{0}{2} {1} {3}\n",
                       indent,
                       PropertyType != null ? PropertyType : returnValue.type.Trim(),
                       GetMarshalFunctionKeywordsFromFlags(),
                       FunctionName( false ) );
                    sb.AppendFormat( "{0}{{\n", indent );
                    indent += "\t";

                    sb.Append( GetComment( indent, true ) );
                    sb.AppendFormat( "{0} get {{ return {1}; }}\n", indent, internalFunctionCall );

                    if ( setMethod  != null )
                    {
                        sb.Append( setMethod.GetComment( indent, true ) );
                        var setFunctionCall = string.Format( "{0}({1})", setMethod.FunctionName( true ),  setMethod.GetArgumentString( false, true, "value" ) );
                        sb.AppendFormat( "{0} set {{ {1}; }}\n", indent, setFunctionCall );
                    }

                    // Find a SET function

                    indent = indent.Substring( 1 );
                    sb.AppendFormat( "{0}}}\n", indent );
                    sb.AppendLine();
                }
            }
            else
            {
                sb.Append( GetComment( indent ) );
                // Function header
                sb.AppendFormat( "{0}{2} {1} {3}({4})\n",
                    indent,
                    returnValue == null ? "void" : returnValue.type.Trim(),
                    GetMarshalFunctionKeywordsFromFlags(),
                    FunctionName( false ), GetArgumentString( true, false ) );
                sb.AppendFormat( "{0}{{\n", indent );
                indent += "\t";

                // Call internal function
                sb.AppendFormat( indent + marshalFunction.Replace( "\n", "\n" + indent ) + "\n",
                                            internalFunctionCall );

                indent = indent.Substring( 1 );
                sb.AppendFormat( "{0}}}\n", indent );
                sb.AppendLine();
            }
        }


        //
        // The Internal function, the binding
        //
        {

            if ( !needsMarshalFunction )
                sb.Append( GetComment( indent ) );

            var keywords = GetExternFunctionKeywordsFromFlags();
            if ( needsMarshalFunction ) keywords = keywords.Replace( "public", "private" ).Replace( "internal", "private" ).Replace( "protected", "private" ); // If we're automatically marshalling, hide the function

            var remaining = library.librarySource.Count;
            foreach ( var librarySource in library.librarySource )
            {
                if ( librarySource.Key != "*" )
                {
                    if ( remaining != library.librarySource.Count )
                        sb.AppendFormat( "{0}#elif {1}\n", indent, librarySource.Key );
                    else 
                        sb.AppendFormat( "{0}#if {1}\n", indent, librarySource.Key );
                }

                sb.AppendFormat( "{0}[DllImport( \"{1}\", EntryPoint=\"{2}\", CharSet = CharSet.Ansi)]\n", indent, librarySource.Value/*.Replace( ".dll", "" )*/, entryPoint );

                if ( remaining == 1 && librarySource.Key != "*" )
                {
                    sb.AppendFormat( "{0}#endif\n", indent, librarySource.Key );
                }

                remaining--;
            }         

            sb.AppendFormat( "{0}{2} {1} {3}({4});\n",
                                        indent,
                                        internalReturnType,
                                        keywords,
                                        FunctionName( needsMarshalFunction ),
                                        GetArgumentString( true, true ) );
        }

        if ( ifDef.Count > 0 )
        {
            foreach ( var defined in ifDef )
            {
                sb.AppendFormat( "{0}#endif // {1}\n", indent, defined );
            }
        }

        if ( library.ifDef.Count > 0 )
        {
            foreach ( var defined in library.ifDef )
            {
                sb.AppendFormat( "{0}#endif // {1}\n", indent, defined );
            }
        }

        sb.AppendLine();
        return sb.ToString();
    }

    public string GetExternFunctionKeywordsFromFlags()
    {
        var tags = "";
        if ( flags.Contains( "unsafe" ) ) tags += "unsafe ";
        if ( flags.Contains( "member" ) ) return tags + "internal extern static";



        if ( flags.Contains( "public" ) ) tags += "public ";
        else if ( flags.Contains( "internal" ) ) tags += "internal ";
        else tags += "protected ";

        if ( flags.Contains( "static" ) ) tags += "extern static ";


        return tags.Trim();
    }

    public string GetMarshalFunctionKeywordsFromFlags()
    {
        var tags = "";

        if ( flags.Contains( "unsafe" ) ) tags += "unsafe ";

        if ( flags.Contains( "public" ) ) tags += "public ";
        else if ( flags.Contains( "internal" ) ) tags += "internal ";
        else tags += "protected ";

        if ( flags.Contains( "static" ) ) tags += "static ";


        return tags.Trim();
    }

    public string GetArgumentString( bool includeArgType, bool includeThis, string renameVariables = null )
    {
        var sb = new StringBuilder();

        if ( includeThis && flags.Contains( "member" ) )
        {
            if ( includeArgType ) sb.AppendFormat( "{0} {1}, ", "ref Facepunch.Native.Handle /*<" + library.ClassName + ">*/ ", "self" );
            else sb.AppendFormat( "{1}, ", "", "ref Handle" );
        }

        foreach ( var arg in args )
        {
            var type = arg.type.Trim();
            var name = arg.name.Trim();

            if ( renameVariables != null ) name = renameVariables;
            if ( Wrapper != null && !includeArgType ) name = Wrapper.Replace( "#", name );

            if ( type == "string" ) type = "[MarshalAs(UnmanagedType.LPStr)] string";

            if ( includeArgType )
                sb.AppendFormat( "{0} {1}, ", type, name );
            else
                sb.AppendFormat( "{0}, ", name );
        }

        var val = " " +sb.ToString().Trim( ',', ' ' ) + " ";
        if ( val.Trim() == string.Empty ) return "";

        return val;
    }

    public string FunctionName( bool intrnal )
    {
        if ( intrnal ) return functionName + "_Internal";
        return functionName;
    }
}