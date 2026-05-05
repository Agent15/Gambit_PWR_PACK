using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Gambonanza.Patcher;

/// <summary>
/// Generic Cecil patcher. Injects three calls into Assembly-CSharp.dll:
///   1. Gambonanza.ModHost.ModHost.LoadAll()                       at GameManager.Start
///   2. Gambonanza.ModHost.ModHost.OnSettingsOpenedInvoke(this)    at SettingsCanvas.OnEnable
///   3. Gambonanza.ModHost.ModHost.OnHomeMenuOpenedInvoke(this)    at CanvasMenu.OnEnable
///
/// All mod-specific logic lives in mods loaded by ModHost at runtime — this patcher
/// has no knowledge of any individual mod (including SpeedMod).
///
/// Usage:
///   GambonanzaPatcher &lt;ManagedFolder&gt; &lt;ModSdk.dll&gt; &lt;ModHost.dll&gt; [extra-runtime-dlls...]
///
/// Any DLLs after &lt;ModHost.dll&gt; are also copied into Managed/ so they get loaded
/// by Unity at startup. Use this for runtime helper libs like Gambonanza.GameUI.dll.
/// </summary>
internal static class Program
{
    private const string ModHostAsmName  = "Gambonanza.ModHost";
    private const string ModHostTypeFull = "Gambonanza.ModHost.ModHost";
    private const string MarkerType      = "__GambonanzaModHostPatched";

    private static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine(
                "usage: GambonanzaPatcher <Managed-folder> <ModSdk.dll> <ModHost.dll> [extra-runtime-dlls...]");
            return 2;
        }

        var managedDir = args[0];
        var modSdkSrc  = args[1];
        var modHostSrc = args[2];
        var extraDlls  = args.Skip(3).ToArray();
        var asmCsharp  = Path.Combine(managedDir, "Assembly-CSharp.dll");
        var backup     = asmCsharp + ".orig";

        if (!File.Exists(asmCsharp))
        {
            Console.Error.WriteLine($"Assembly-CSharp.dll not found at {asmCsharp}");
            return 1;
        }
        foreach (var src in (new[] { modSdkSrc, modHostSrc }).Concat(extraDlls))
        {
            if (!File.Exists(src))
            {
                Console.Error.WriteLine($"required dll missing: {src}");
                return 1;
            }
        }

        // 1. Backup original (only the first time we patch).
        if (!File.Exists(backup))
        {
            File.Copy(asmCsharp, backup);
            Console.WriteLine($"  backup -> {Path.GetFileName(backup)}");
        }

        // 2. Install ModSdk + ModHost (+ any extra runtime DLLs) into Managed/ so
        //    Unity loads them at startup.
        foreach (var src in (new[] { modSdkSrc, modHostSrc }).Concat(extraDlls))
        {
            var dest = Path.Combine(managedDir, Path.GetFileName(src));
            File.Copy(src, dest, overwrite: true);
            Console.WriteLine($"  install -> {Path.GetFileName(dest)}");
        }

        // 3. Always patch from the original backup. Idempotent.
        var asmResolver = new DefaultAssemblyResolver();
        asmResolver.AddSearchDirectory(managedDir);
        var readerParams = new ReaderParameters
        {
            AssemblyResolver = asmResolver,
            ReadWrite = false,
            InMemory = true,
        };

        using var asm = AssemblyDefinition.ReadAssembly(backup, readerParams);
        var module = asm.MainModule;

        // 4. Build references into Gambonanza.ModHost.
        var modHostAsmRef = new AssemblyNameReference(ModHostAsmName, new Version(0, 1, 0, 0));
        module.AssemblyReferences.Add(modHostAsmRef);

        var modHostTypeRef = new TypeReference(
            "Gambonanza.ModHost", "ModHost", module, modHostAsmRef, valueType: false);

        var loadAllRef = new MethodReference(
            "LoadAll", module.TypeSystem.Void, modHostTypeRef) { HasThis = false };

        // OnSettingsOpenedInvoke(MonoBehaviour). Resolve MonoBehaviour from existing references.
        var monoBehaviourRef = ResolveMonoBehaviour(module);
        var onSettingsOpenedRef = new MethodReference(
            "OnSettingsOpenedInvoke", module.TypeSystem.Void, modHostTypeRef) { HasThis = false };
        onSettingsOpenedRef.Parameters.Add(new ParameterDefinition(monoBehaviourRef));

        var onHomeMenuOpenedRef = new MethodReference(
            "OnHomeMenuOpenedInvoke", module.TypeSystem.Void, modHostTypeRef) { HasThis = false };
        onHomeMenuOpenedRef.Parameters.Add(new ParameterDefinition(monoBehaviourRef));

        // 5. Patch GameManager.Start — prepend ModHost.LoadAll().
        var gameManager = module.GetType("Blukulele.Core.GameManager");
        var startMethod = gameManager?.Methods.FirstOrDefault(m => m.Name == "Start" && !m.IsStatic);
        if (gameManager == null || startMethod == null)
        {
            Console.Error.WriteLine("Could not find Blukulele.Core.GameManager.Start — aborting.");
            return 3;
        }
        var ilStart = startMethod.Body.GetILProcessor();
        var firstInstr = startMethod.Body.Instructions.First();
        ilStart.InsertBefore(firstInstr, ilStart.Create(OpCodes.Call, loadAllRef));
        Console.WriteLine("  patched -> Blukulele.Core.GameManager.Start (prepended ModHost.LoadAll)");

        // 6. Patch SettingsCanvas.OnEnable — append ModHost.OnSettingsOpenedInvoke(this) before every ret.
        var settingsCanvas = module.GetType("Blukulele.CHE.SettingsCanvas");
        var onEnable = settingsCanvas?.Methods.FirstOrDefault(m => m.Name == "OnEnable" && !m.IsStatic);
        if (settingsCanvas == null || onEnable == null)
        {
            Console.WriteLine("  warn: SettingsCanvas.OnEnable not found; settings injection disabled.");
        }
        else
        {
            var ilOnEnable = onEnable.Body.GetILProcessor();
            var retInstrs = onEnable.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToList();
            foreach (var ret in retInstrs)
            {
                ilOnEnable.InsertBefore(ret, ilOnEnable.Create(OpCodes.Ldarg_0));
                ilOnEnable.InsertBefore(ret, ilOnEnable.Create(OpCodes.Call, onSettingsOpenedRef));
            }
            Console.WriteLine("  patched -> Blukulele.CHE.SettingsCanvas.OnEnable (appended ModHost.OnSettingsOpenedInvoke)");
        }

        // 7. Patch CanvasMenu.OnEnable — append ModHost.OnHomeMenuOpenedInvoke(this) before every ret.
        var canvasMenu = module.GetType("Blukulele.CHE.CanvasMenu");
        var menuOnEnable = canvasMenu?.Methods.FirstOrDefault(m => m.Name == "OnEnable" && !m.IsStatic);
        if (canvasMenu == null || menuOnEnable == null)
        {
            Console.WriteLine("  warn: CanvasMenu.OnEnable not found; mods button injection disabled.");
        }
        else
        {
            var ilMenu = menuOnEnable.Body.GetILProcessor();
            var rets = menuOnEnable.Body.Instructions.Where(i => i.OpCode == OpCodes.Ret).ToList();
            foreach (var ret in rets)
            {
                ilMenu.InsertBefore(ret, ilMenu.Create(OpCodes.Ldarg_0));
                ilMenu.InsertBefore(ret, ilMenu.Create(OpCodes.Call, onHomeMenuOpenedRef));
            }
            Console.WriteLine("  patched -> Blukulele.CHE.CanvasMenu.OnEnable (appended ModHost.OnHomeMenuOpenedInvoke)");
        }

        // 8. Add idempotency marker.
        AddMarker(asm);

        // 8. Write patched assembly out.
        asm.Write(asmCsharp);
        Console.WriteLine($"  wrote   -> {Path.GetFileName(asmCsharp)}");
        Console.WriteLine("Done.");
        return 0;
    }

    private static TypeReference ResolveMonoBehaviour(ModuleDefinition module)
    {
        var coreModule = module.AssemblyReferences.FirstOrDefault(r => r.Name == "UnityEngine.CoreModule");
        var unityRef   = coreModule
                      ?? module.AssemblyReferences.First(r => r.Name == "UnityEngine");
        return module.ImportReference(
            new TypeReference("UnityEngine", "MonoBehaviour", module, unityRef));
    }

    private static void AddMarker(AssemblyDefinition asm)
    {
        var module = asm.MainModule;
        if (module.GetType(MarkerType) != null) return;

        var t = new TypeDefinition("", MarkerType,
            TypeAttributes.NotPublic | TypeAttributes.Sealed | TypeAttributes.Abstract,
            module.TypeSystem.Object);
        module.Types.Add(t);
    }
}
