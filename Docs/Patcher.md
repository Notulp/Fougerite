# The Fougerite Patcher

Fougerite doesn't modify the game at runtime - it ships a build-time tool, **Fougerite.Patcher**
(`Fougerite.Patcher\`), that uses [Mono.Cecil](https://github.com/jbevain/cecil) to rewrite the IL of three
shipped Rust Legacy assemblies (`Assembly-CSharp.dll`, `uLink.dll`, `Facepunch.MeshBatch.dll`) before Fougerite
is ever loaded. This is what lets Fougerite call into private game internals, add hooks at points the original
game never exposed, and wrap fragile legacy methods in a `try/catch` so one plugin/engine bug can't take the
whole server down.

See the [`Compilation`](../README.md#compilation) section of the main README for the short step-by-step. This
page explains *why* the patcher exists, what the clean DLLs are, and the stub-function pattern you'll need
whenever you hook something new.

## Why you need "clean" DLLs

`ILPatcher.FirstPass()`/`SecondPass()` are **not idempotent** - they assume the input assembly is in its
original, unmodified shape (expected fields/methods at expected points) and they stamp a
`Fougerite_Patched_FirstPass`/`Fougerite_Patched_SecondPass` marker field on `ServerInit` once done, refusing to
run again on an already-patched DLL (see the check at the top of `FirstPass()`). If you patch an already-patched
or hand-edited DLL, or one from the wrong Rust Legacy build, the patcher can silently patch the wrong offsets or
throw partway through a pass, corrupting the assembly.

That's what [`Fougerite\References\CleanPatchTargetDlls\`](../Fougerite/References/CleanPatchTargetDlls/ReadMe.txt)
is for: it holds known-good, **deobfuscated** copies of the three target DLLs (plus untouched originals under
`UnTouchedVersions\`) to patch from, so every patch run starts from the exact same baseline. Per its `ReadMe.txt`:

- Most of the DLLs there are cleaned/deobfuscated versions - pick whichever matching set you intend to patch
  (there's also a raw/untouched option, with the caveat that the untouched `uLink.dll` can't be patched by
  Fougerite).
- The stock `uLink.dll` has known bugs; `uLink-DYN.dll` is a manually-patched variant fixing one of them
  (GitHub issue #95) - prefer it over the plain `uLink.dll` unless you have a reason not to.
- Whichever set you pick, copy **all three** target DLLs from it (not a mix of cleaned + untouched) into the
  patcher's output directory before running it, per the main README's compilation steps.

After a successful run the patcher writes the patched DLLs in place; copy those three into
`Fougerite\References\PatchedRustDlls\` so the Fougerite project (and anything compiled against it) builds
against the patched surface.

## The skeleton-stub + "patch to public" pattern

`ILPatcher` reads **both** `Assembly-CSharp.dll` *and* `Fougerite.dll` with Mono.Cecil
(`fougeriteAssembly = AssemblyDefinition.ReadAssembly("Fougerite.dll")` in `SecondPass()`), then injects a
`call` instruction straight into the game's IL that jumps into a method you already wrote in Fougerite - see
`WaterSystemServer`'s `ServerFrame` patch for a direct example, where the whole body of the game's method is
replaced with a single call into a Fougerite hook. This is how most of Fougerite's hooks exist at all: the
patcher is effectively hand-wiring a call the original game never had.

That creates a chicken-and-egg problem the moment the hook needs to touch a **private** field or method on a
game type instead of just being called from one:

- `Fougerite.dll`'s C# source is compiled against the **patched** `Assembly-CSharp.dll` reference (from
  `PatchedRustDlls\`), so if your new hook code directly reads/writes a private field, it simply won't compile
  until that field is public in the reference assembly you're building against.
- But the patcher step that would make that field public (`OpenField`/`OpenMethod`, see below) runs against the
  **clean** DLL, and the patcher itself is built and run independently of whether `Fougerite.dll` compiles -
  there's no way to "first patch, then compile, then patch again" in one pass without manually juggling two
  half-built pieces.

The practical workaround used throughout `ILPatcher.cs` (e.g. `MetabolismPatch`, `HumanControllerServerFramePatch`,
the `WaterSupport`-tagged `OpenField`/`OpenMethod` calls) is to split the work into two independent, separately
buildable steps instead of trying to do it atomically:

1. **Write the Fougerite-side method as an empty skeleton first.** Add the method with its final signature, but
   comment out (or stub out with a no-op/default return) the part of the body that actually touches the
   not-yet-public member. This keeps `Fougerite.dll` compiling against whatever reference assembly you currently
   have, clean or already-patched.
2. **Add an `OpenField`/`OpenMethod` call to the patcher** for the specific field/method you need:
   ```csharp
   private void MyNewHookPatch()
   {
       TypeDefinition someType = rustAssembly.MainModule.GetType("SomeGameType");
       OpenField(someType, "m_SomePrivateField");   // flips the field to public
       OpenMethod(someType, "SomePrivateMethod");   // flips every overload named this to public
   }
   ```
   `OpenField`/`OpenMethod` (bottom of `ILPatcher.cs`) just call `FieldDefinition.SetPublic(true)` /
   `MethodDefinition.SetPublic(true)` - no behavior changes, purely an accessibility edit - and log a warning
   instead of throwing if the type/member isn't found, so a bad member name doesn't take down an entire patch
   pass.
3. **Run the patcher against the clean DLLs**, copy the result into `PatchedRustDlls\` as usual.
4. **Rebuild `Fougerite.dll`** against the newly patched reference - the member is now public, so you can
   uncomment the real implementation in your skeleton method and build normally from then on.

This two-step dance (stub now, implement after the next patch cycle) comes up constantly for two reasons beyond
the circular-dependency mechanics above:

- **Changing behavior outright.** Sometimes the goal isn't just reading a private field but altering what a
  method does (see `WrapWildlifeUpdateInTryCatch`, `AntiDecay`, `MetaBolismMethodMod`, etc.) - IL-level patches
  like `WrapMethod` splice try/catch handling or replace instructions directly in the game's method body, which
  has nothing to do with C# accessibility and has to happen in the patcher regardless.
- **Rust Legacy's original code quality.** A lot of the private state the patcher opens up exists because the
  original (obfuscated, minimally-reviewed) Facepunch code buried useful state behind `private`/`internal` for
  no real encapsulation reason - it's legacy code, not a deliberately protected API surface - so "just make it
  public" is usually both the simplest and the safest fix compared to re-deriving the same value through
  reflection or a parallel calculation at runtime.

## A note on `ILAudit`

Every `FirstPass()`/`SecondPass()` run snapshots the assembly's IL and member accessibility before and after
patching (`BeginAudit()`/`RunAudit()`, backed by `ILAudit.cs`) and writes a diff to
`Fougerite_PatchAudit\validation.txt`. If you add a new `OpenField`/`OpenMethod`/IL patch and something about it
is structurally wrong (e.g. a method body left unbalanced), this is the first place to check - it reports
exactly which members changed and flags anything that looks broken, without needing to decompile the output by
hand.
