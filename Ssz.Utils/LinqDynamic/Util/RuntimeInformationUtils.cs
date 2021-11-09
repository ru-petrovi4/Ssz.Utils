namespace System.Linq.Dynamic.Core.Util
{
    internal static class RuntimeInformationUtils
    {
        public static bool IsBlazorWASM;

        static RuntimeInformationUtils()
        {
            IsBlazorWASM =
                // Used for Blazor WebAssembly .NET Core 3.x / .NET Standard 2.x
                Type.GetType("Mono.Runtime") is not null ||

               // Use for Blazor WebAssembly .NET
               // See also https://github.com/mono/mono/pull/19568/files
               Runtime.InteropServices.RuntimeInformation.IsOSPlatform(Runtime.InteropServices.OSPlatform.Create("BROWSER"));
        }
    }
}
