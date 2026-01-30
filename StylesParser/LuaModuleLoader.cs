using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Lua;

public class LuaModuleLoader : ILuaModuleLoader
{
    public readonly Dictionary<string, LuaModule> Cache = [];

    public bool Exists(string moduleName)
    {
        return File.Exists(Path.Combine("LuaLibraries", $"{moduleName.Replace('.', '/')}.lua"));
    }

    public async ValueTask<LuaModule> LoadAsync(string moduleName, CancellationToken cancellationToken = default)
    {
        if (Cache.TryGetValue(moduleName, out var cached))
            return cached;

        var filePath = Path.Combine("LuaLibraries", $"{moduleName.Replace('.', '/')}.lua");
        if (!File.Exists(filePath))
            throw new LuaModuleNotFoundException(moduleName);

        var module = new LuaModule(moduleName, await File.ReadAllTextAsync(filePath, cancellationToken));
        Cache[moduleName] = module;
        return module;
    }
}