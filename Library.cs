using System.Collections.Generic;

public class Library
{
    public string ClassName = "";
    public string ClassType = "";
    public string FileName = "";
    public Dictionary<string, string> librarySource = new Dictionary< string, string >();
    public string baseObject = "";
    public List<string> memberOf = new List<string>();
    public List<string> ifDef = new List<string>();

    public bool IsValid()
    {
        return !string.IsNullOrEmpty( ClassName );
    }

    public int Read( string[] lines, int i )
    {
        while ( lines[i].StartsWith( "// " ) && lines[i].Contains( ":" ) )
        {
            var args = lines[i].Substring( 3 ).Split( ':' );

            switch ( args[0] )
            {
                case "Bind":
                {
                    ClassName = args[1].Trim();
                    break;
                }
                case "Type":
                {
                    ClassType = args[1].Trim();
                    break;
                }
                case "Output":
                {
                    FileName = args[1].Trim();
                    break;
                }

                case "MemberOf":
                {
                    memberOf.Add( args[1].Trim() );
                    break;
                }
                case "Library":
                {
                    librarySource.Add( "*", args[1].Trim() );
                    break;
                }
                case "LibraryCondition":
                    {
                        var parts = args[1].Split( ';' );

                        librarySource.Add( parts[0].Trim(), parts[1].Trim() );
                        break;
                    }
                case "Base":
                {
                    baseObject = " : " + args[1].Trim();
                    break;
                }
                case "IfDef":
                    {
                        ifDef.Add( args[1].Trim() );
                        break;
                    }
            }

            i++;
        }

        return i;
    }
}
