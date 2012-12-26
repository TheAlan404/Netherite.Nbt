using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Resources;

[assembly: AssemblyTitle( "fNbt" )]
[assembly: AssemblyDescription("A library to read and write NBT files.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany( "github.com/fragmer/fnbt" )]
[assembly: AssemblyProduct( "fNbt" )]
[assembly: AssemblyCopyright( "fNbt - 2012-2013 Matvei Stefarov; LibNbt - 2010 Erik Davidson," )]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("9253db1f-f1d4-45aa-a277-4f3ba635d651")]

[assembly: AssemblyVersion( "0.5.0.0" )]
[assembly: AssemblyFileVersion( "0.5.0.0" )]

// Allow the unit tests to see internal classes
[assembly: InternalsVisibleTo("LibNbt.Test")]
[assembly: NeutralResourcesLanguageAttribute( "en-US" )]
