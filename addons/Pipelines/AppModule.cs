using System.Reflection;
using System.Text.Json;

internal class AppModule
{
    [System.Runtime.CompilerServices.ModuleInitializer]
    public static void Initialize()
    {
        System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly()).Unloading += alc =>
        {
            var assembly = typeof(JsonSerializerOptions).Assembly;
            var updateHandlerType = assembly.GetType("System.Text.Json.JsonSerializerOptionsUpdateHandler");
            var clearCacheMethod = updateHandlerType?.GetMethod("ClearCache", BindingFlags.Static | BindingFlags.Public);
            clearCacheMethod?.Invoke(null, new object?[] { null });

            // Unload any other unloadable references
        };
    }
}
