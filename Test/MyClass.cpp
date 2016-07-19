
//
// Bind: MyClass
// Output: MyClass.generated.cs
// Type: public partial class
// DerivedFrom: IDisposable
// MemberOf: namespace Facepunch.My.Namespace
// Library: Facepunch.MyLibrary
//

//
// Function: Create
// Flags: Static
// Description: Creates a new class
// Arg: ref ConfigData;config;configuration information
// Return: IntPtr;someclass;A new class of SomeClass
//
CORE_API_EXPORT CSomeClass* CSomeClass___Create( Config config )
{
    return new CSomeClass( config );
}

//
// Function: Destroy
// Flags: Member
// Description: Destroy this
//
CORE_API_EXPORT void CSomeClass___Destroy( EngineObject& obj )
{
    ENGINEOBJECT_SELF( CSomeClass );

    // delete this class
    delete self;
	
    obj._pointer = NULL;
}

//
// Function: SetValue
// Flags: Member;Public
// Description: Set a key value on this class
// Arg: string;key;Key Name
// Arg: string;value;Key Value
// Return: bool;success;Returns true on success
//
CORE_API_EXPORT bool CSomeClass_SetValue( EngineObject& obj, const char* key, const char* value )
{
    ENGINEOBJECT_SELF( CSomeClass );
    
    return self->SetKV( key, value );
}


//
// Function: GetAString
// Flags: Member;Public
// Description: Returns a string
// Return: string;success;Returns true on success
//
CORE_API_EXPORT const char* Domain___InvokeFunction_ArgS( EngineObject& obj )
{
    ENGINEOBJECT_SELF( CSomeClass );

    return "A string! Wow!";
}
