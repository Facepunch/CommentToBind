using System;
using System.Runtime.InteropServices;

namespace Facepunch.My.Namespace
{
	public partial class MyClass
	{
		/// <summary>
		/// Creates a new class
		/// </summary>
		/// <param name="config">configuration information</param>
		/// <returns>A new class of SomeClass</returns>
		[DllImport( "Facepunch.MyLibrary", EntryPoint="CSomeClass___Create", CharSet = CharSet.Ansi)]
		protected extern static IntPtr Create( ref ConfigData config );

		/// <summary>
		/// Destroy this
		/// </summary>
		protected void Destroy()
		{
			Destroy_Internal( ref Handle );
		}

		[DllImport( "Facepunch.MyLibrary", EntryPoint="CSomeClass___Destroy", CharSet = CharSet.Ansi)]
		private extern static void Destroy_Internal( ref Facepunch.Native.Handle /*<MyClass>*/  self );

		/// <summary>
		/// Set a key value on this class
		/// </summary>
		/// <param name="key">Key Name</param>
		/// <param name="value">Key Value</param>
		/// <returns>Returns true on success</returns>
		public bool SetValue( [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value )
		{
			return SetValue_Internal( ref Handle, key, value );
		}

		[DllImport( "Facepunch.MyLibrary", EntryPoint="CSomeClass_SetValue", CharSet = CharSet.Ansi)]
		private extern static bool SetValue_Internal( ref Facepunch.Native.Handle /*<MyClass>*/  self, [MarshalAs(UnmanagedType.LPStr)] string key, [MarshalAs(UnmanagedType.LPStr)] string value );

		/// <summary>
		/// Returns a string
		/// </summary>
		/// <returns>Returns true on success</returns>
		public string GetAString()
		{
			IntPtr ptr = GetAString_Internal( ref Handle );
			if ( ptr == IntPtr.Zero ) return string.Empty;
			
			return Marshal.PtrToStringAnsi( ptr );
		}

		[DllImport( "Facepunch.MyLibrary", EntryPoint="Domain___InvokeFunction_ArgS", CharSet = CharSet.Ansi)]
		private extern static IntPtr GetAString_Internal( ref Facepunch.Native.Handle /*<MyClass>*/  self );

	}
}
