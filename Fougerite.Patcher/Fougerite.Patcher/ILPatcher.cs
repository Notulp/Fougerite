using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Fougerite.Patcher
{

    public class ILPatcher
    {
        private AssemblyDefinition rustAssembly = null;
        private AssemblyDefinition fougeriteAssembly = null;
        private AssemblyDefinition unityAssembly = null;
        private AssemblyDefinition mscorlib = null;
        private TypeDefinition hooksClass = null;
        private TypeDefinition SaveHandlerClass = null;

        public ILPatcher()
        {
            try
            {
                rustAssembly = AssemblyDefinition.ReadAssembly("Assembly-CSharp.dll");
                // rustFirstPassAssembly = AssemblyDefinition.ReadAssembly("Assembly-CSharp-firstpass.dll");
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
        }

        private void WrapWildlifeUpdateInTryCatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("WildlifeManager");
            TypeDefinition data = type.GetNestedType("Data");
            data.IsPublic = true;
            MethodDefinition think = data.GetMethod("Think");
            MethodDefinition update = type.GetMethod("Update");

            Instruction y = null;
            foreach (Instruction x in think.Body.Instructions)
            {
                if (x.ToString().Contains("LogException"))
                {
                    y = x;
                    break;
                }
            }
            think.Body.Instructions.Remove(y);

            TypeDefinition logger = fougeriteAssembly.MainModule.GetType("Fougerite.Logger");
            MethodDefinition logex = logger.GetMethod("LogException");

            // TODO: This should be checked instead of suppressed
            WrapMethod(update, logex, rustAssembly, false);
            //WrapMethod(think, logex, rustAssembly, false);

            foreach (var x in type.Fields)
            {
                x.SetPublic(true);
            }
            
            foreach (var x in data.Fields)
            {
                x.SetPublic(true);
            }

            MethodDefinition AddWildlifeInstance = type.GetMethod("AddWildlifeInstance");
            MethodDefinition AddWildlifeInstanceHook = hooksClass.GetMethod("AddWildlifeInstance");
            MethodDefinition RemoveWildlifeInstance = type.GetMethod("RemoveWildlifeInstance");
            MethodDefinition RemoveWildlifeInstanceHook = hooksClass.GetMethod("RemoveWildlifeInstance");
            
            ILProcessor iLProcessor = AddWildlifeInstance.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(AddWildlifeInstanceHook)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor2 = RemoveWildlifeInstance.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(RemoveWildlifeInstanceHook)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void PatchFacePunch()
        {
            AssemblyDefinition FacepunchMeshBatch = AssemblyDefinition.ReadAssembly("Facepunch.MeshBatch.dll");
            TypeDefinition MeshBatchPhysicalOutput = FacepunchMeshBatch.MainModule.GetType("Facepunch.MeshBatch.Runtime.MeshBatchPhysicalOutput");
            TypeDefinition MeshBatchPhysicalIntegration = FacepunchMeshBatch.MainModule.GetType("Facepunch.MeshBatch.Runtime.Sealed.MeshBatchPhysicalIntegration");
            
            MethodDefinition ActivateImmediatelyUnchecked = MeshBatchPhysicalOutput.GetMethod("ActivateImmediatelyUnchecked");
            MethodDefinition ActivateImmediatelyUncheckedHook = hooksClass.GetMethod("ActivateImmediatelyUncheckedHook");
            
            ILProcessor ilProcessor = ActivateImmediatelyUnchecked.Body.GetILProcessor();
            ilProcessor.Body.Instructions.Clear();
            ilProcessor.Body.ExceptionHandlers.Clear();
            ilProcessor.Body.Variables.Clear();
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, 
                FacepunchMeshBatch.MainModule.Import(ActivateImmediatelyUncheckedHook)));
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            MeshBatchPhysicalOutput.GetField("FlaggedForActivation").SetPublic(true);
            MeshBatchPhysicalOutput.GetField("Activated").SetPublic(true);
            MeshBatchPhysicalOutput.GetField("GameObject").SetPublic(true);

            MeshBatchPhysicalIntegration.IsPublic = true;
            MeshBatchPhysicalIntegration.GetMethod("Cancel").SetPublic(true);
            
            FacepunchMeshBatch.Write("Facepunch.MeshBatch.dll");
        }

        private void PatchuLink2(ref AssemblyDefinition asm)
        {
            ModuleDefinition mod = asm.MainModule;

            TypeDefinition Class1 = mod.GetType("Class1");
            TypeDefinition Class0 = mod.GetType("Class0");
            TypeDefinition Class33 = mod.GetType("Class33");
            TypeDefinition Class35 = mod.GetType("Class35");
            TypeDefinition Class56 = mod.GetType("Class56");
            TypeDefinition Class18 = mod.GetType("Class18");
            TypeDefinition Class23 = mod.GetType("Class23");
            TypeDefinition Class20 = mod.GetType("Class20");
            TypeDefinition Class58 = mod.GetType("Class58");
            TypeDefinition Class14 = mod.GetType("Class14");
            TypeDefinition Enum3 = mod.GetType("Enum3");
            TypeDefinition Enum4 = mod.GetType("Enum4");
            TypeDefinition Enum11 = mod.GetType("Enum11");
            
            MethodDefinition AntiDDos = hooksClass.GetMethod("AntiDDos");
            MethodDefinition vmethod_2 = Class1.GetMethod("vmethod_2");

            Class1.IsPublic = true;
            Class0.IsPublic = true;
            Class33.IsPublic = true;
            Class35.IsPublic = true;
            Class56.IsPublic = true;
            Class18.IsPublic = true;
            Class23.IsPublic = true;
            Class20.IsPublic = true;
            Class58.IsPublic = true;
            Class14.IsPublic = true;
            Enum3.IsPublic = true;
            Enum4.IsPublic = true;
            Enum11.IsPublic = true;
            
            Class33.Fields.First(f => f.Name == "class18_0").SetPublic(true);
            Class33.Fields.First(f => f.Name == "enum3_0").SetPublic(true);
            Class33.Fields.First(f => f.Name == "enum4_0").SetPublic(true);

            Class35.Fields.First(f => f.Name == "class56_0").SetPublic(true);
            Class35.Fields.First(f => f.Name == "ipendPoint_0").SetPublic(true);
            
            Class1.Fields.First(f => f.Name == "dictionary_1").SetPublic(true);
            Class1.Fields.First(f => f.Name == "dictionary_0").SetPublic(true);
            Class1.Fields.First(f => f.Name == "list_3").SetPublic(true);
            Class1.Fields.First(f => f.Name == "bool_3").SetPublic(true);
            
            Class1.Methods.First(m => m.Name == "method_62").SetPublic(true);

            Class0.Fields.First(f => f.Name == "class18_2").SetPublic(true);
            Class0.Fields.First(f => f.Name == "class14_0").SetPublic(true);
            Class0.Fields.First(f => f.Name == "byte_0").SetPublic(true);
            Class0.Fields.First(f => f.Name == "class20_0").SetPublic(true);
            Class0.Fields.First(f => f.Name == "enum4_0").SetPublic(true);
            Class0.Methods.First(m => m.Name == "method_25").SetPublic(true);
            Class0.Methods.First(m => m.Name == "method_37").SetPublic(true);
            Class0.Methods.First(m => m.Name == "method_10").SetPublic(true);
            Class0.Methods.First(m => m.Name == "method_26").SetPublic(true);
            Class0.Methods.First(m => m.Name == "method_18").SetPublic(true);
            Class0.Methods.First(m => m.Name == "method_8").SetPublic(true);

            Class56.Fields.First(f => f.Name == "bool_0").SetPublic(true);
            Class56.Methods.First(m => m.Name == "method_38").SetPublic(true);
            Class56.Methods.First(m => m.Name == "method_32").SetPublic(true);
            Class56.Methods.First(m => m.Name == "method_23").SetPublic(true);
            
            foreach (var ctor in Class56.Methods.Where(m => m.IsConstructor))
            {
                ctor.IsPublic = true;
            }

            Class18.Fields.First(f => f.Name == "byte_0").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_2").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_83").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_27").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_5").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_9").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_89").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_36").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_93").SetPublic(true);
            Class18.Methods.First(m => m.Name == "method_58").SetPublic(true);

            Class23.Methods.First(m => m.Name == "smethod_1").SetPublic(true);

            Class20.Fields.First(f => f.Name == "int_4").SetPublic(true);
            Class20.Methods.First(m => m.Name == "method_2").SetPublic(true);
            Class20.Methods.First(m => m.Name == "method_34").SetPublic(true);

            Class58.Methods.First(m => m.Name == "smethod_10").SetPublic(true);

            Class14.Methods.First(m => m.Name == "method_4").SetPublic(true);
            Class14.Methods.First(m => m.Name == "method_6").SetPublic(true);
            
            ILProcessor ilProcessor = vmethod_2.Body.GetILProcessor();
            ilProcessor.Body.Instructions.Clear();
            ilProcessor.Body.ExceptionHandlers.Clear();
            ilProcessor.Body.Variables.Clear();
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, 
                asm.MainModule.Import(AntiDDos)));
            ilProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void uLinkLateUpdateInTryCatch()
        {
            AssemblyDefinition ulink = AssemblyDefinition.ReadAssembly("uLink.dll");
            TypeDefinition type = ulink.MainModule.GetType("uLink.InternalHelper");
            TypeDefinition Class5 = ulink.MainModule.GetType("Class5");
            TypeDefinition Class18 = ulink.MainModule.GetType("Class18");
            TypeDefinition Class62 = ulink.MainModule.GetType("Class62");
            TypeDefinition Class56 = ulink.MainModule.GetType("Class56");
            TypeDefinition Class52 = ulink.MainModule.GetType("Class52");
            TypeDefinition Class46 = ulink.MainModule.GetType("Class46");
            TypeDefinition Class48 = ulink.MainModule.GetType("Class48");
            TypeDefinition Struct10 = ulink.MainModule.GetType("Struct10");
            TypeDefinition Struct6 = ulink.MainModule.GetType("Struct6");
            //TypeDefinition Class46 = ulink.MainModule.GetType("Class46");
            TypeDefinition Class45 = ulink.MainModule.GetType("Class45");
            TypeDefinition Class11 = ulink.MainModule.GetType("Class11");
            TypeDefinition Class7 = ulink.MainModule.GetType("Class7");
            TypeDefinition Class1 = ulink.MainModule.GetType("Class1");
            //TypeDefinition Class1 = ulink.MainModule.GetType("Class1");
            //MethodDefinition method_61 = Class1.GetMethod("method_61");
            MethodDefinition method_36 = Class56.GetMethod("method_36");
            //MethodDefinition method_44 = Class56.GetMethod("method_44");
            MethodDefinition method_20 = Class56.GetMethod("method_20");
            MethodDefinition method_25 = Class56.GetMethod("method_25");
            MethodDefinition method_22 = Class56.GetMethod("method_22");
            MethodDefinition method_435 = Class52.GetMethod("method_435");
            MethodDefinition vmethod_3 = Class52.GetMethod("vmethod_3");
            MethodDefinition method_250 = Class48.GetMethod("method_250");
            MethodDefinition method_252 = Class48.GetMethod("method_252");
            MethodDefinition method_269 = Class48.GetMethod("method_269");
            MethodDefinition method_299 = Class48.GetMethod("method_299");
            MethodDefinition method_249 = Class48.GetMethod("method_249");
            MethodDefinition method_275 = Class48.GetMethod("method_275");
            MethodDefinition method_277 = Class48.GetMethod("method_277");
            MethodDefinition method_270 = Class48.GetMethod("method_270");
            MethodDefinition method_4 = Class45.GetMethod("method_4");
            MethodDefinition method_273 = Class48.GetMethod("method_273");
            MethodDefinition method_261 = Class48.GetMethod("method_261");
            
            MethodDefinition vmethod_35 = Class48.GetMethod("vmethod_35");
            vmethod_35.SetPublic(true);
            MethodDefinition method_281 = Class48.GetMethod("method_281");
            method_281.SetPublic(true);
            MethodDefinition method_73 = Class46.GetMethod("method_73");
            method_73.SetPublic(true);
            MethodDefinition method_338 = Class48.GetMethod("method_338");
            method_338.SetPublic(true);
            MethodDefinition method_235 = Class48.GetMethod("method_235");
            method_235.SetPublic(true);
    
            MethodDefinition vmethod2 = Class1.GetMethod("vmethod_2");

            method_273.SetPublic(true);

            MethodDefinition method_259 = Class48.GetMethod("method_259");
            MethodDefinition method_335 = Class48.GetMethod("method_335");
            MethodDefinition method_336 = Class48.GetMethod("method_336");
            MethodDefinition method_337 = Class48.GetMethod("method_337");
            MethodDefinition vmethod_25 = Class48.GetMethod("vmethod_25");
            MethodDefinition method_262 = Class48.GetMethod("method_262");
            MethodDefinition method_260 = Class48.GetMethod("method_260");
            MethodDefinition method_263 = Class48.GetMethod("method_263");
            MethodDefinition method_264 = Class48.GetMethod("method_264");
            
            method_260.SetPublic(true);
            method_261.SetPublic(true);
            method_262.SetPublic(true);
            method_263.SetPublic(true);
            method_264.SetPublic(true);
            method_259.SetPublic(true);
            method_335.SetPublic(true);
            method_336.SetPublic(true);
            method_337.SetPublic(true);
            vmethod_25.SetPublic(true);

            Class11.IsPublic = true;
            Class7.IsPublic = true;
            Struct6.IsPublic = true;
            Struct10.IsPublic = true;
            Class18.IsPublic = true;
            Class62.IsPublic = true;
            Class46.IsPublic = true;
            Class1.IsPublic = true;
            Class52.IsPublic = true;
            MethodDefinition structmethod_0 = Struct10.GetMethod("method_0");
            MethodDefinition RPCCatch = hooksClass.GetMethod("RPCCatch");
            int si = structmethod_0.Body.Instructions.Count - 46;
            ILProcessor siiLProcessor = structmethod_0.Body.GetILProcessor();
            siiLProcessor.InsertBefore(structmethod_0.Body.Instructions[si],
                Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(RPCCatch)));
            siiLProcessor.InsertBefore(structmethod_0.Body.Instructions[si], Instruction.Create(OpCodes.Ldarg_2));

            ulink.MainModule.GetType("Enum4").IsPublic = true;
            ulink.MainModule.GetType("Enum8").IsPublic = true;
            ulink.MainModule.GetType("Struct15").IsPublic = true;

            foreach (var x in Class5.Fields)
            {
                x.SetPublic(true);
            }
            
            foreach (var x in Class56.Fields)
            {
                x.SetPublic(true);
            }
            
            foreach (var x in Class46.Fields)
            {
                x.SetPublic(true);
            }
            
            foreach (var x in Class62.Methods)
            {
                x.SetPublic(true);
            }

            foreach (var x in Class62.Fields)
            {
                x.SetPublic(true);
            }
            
            foreach (var x in Class18.Methods)
            {
                x.SetPublic(true);
            }

            foreach (var x in Class18.Fields)
            {
                x.SetPublic(true);
            }

            foreach (var x in Class48.Fields)
            {
                x.SetPublic(true);
            }

            foreach (var x in Class5.NestedTypes)
            {
                x.IsPublic = true;
            }
            
            MethodDefinition uLinkAuthorizationCheck = hooksClass.GetMethod("uLinkAuthorizationCheck");
            ILProcessor iLProcessor273 = method_273.Body.GetILProcessor();
            iLProcessor273.Body.Instructions.Clear();
            iLProcessor273.Body.ExceptionHandlers.Clear();
            iLProcessor273.Body.Variables.Clear();
            iLProcessor273.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor273.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor273.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(uLinkAuthorizationCheck)));
            iLProcessor273.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor261 = method_261.Body.GetILProcessor();
            Instruction il = null;
            foreach (var x in iLProcessor261.Body.Instructions)
            {
                if (x.ToString().Contains("::Error<System.String"))
                {
                    il = x;
                }
            }

            if (il != null)
            {
                iLProcessor261.Body.Instructions.Remove(il);
            }
            
            /*MethodDefinition uLinkMessageCheck = hooksClass.GetMethod("uLinkMessageCheck");
            ILProcessor iLProcessor261 = method_261.Body.GetILProcessor();
            iLProcessor261.InsertBefore(iLProcessor261.Body.Instructions[0], Instruction.Create(OpCodes.Ret));
            iLProcessor261.InsertBefore(iLProcessor261.Body.Instructions[0], Instruction.Create(OpCodes.Brtrue_S, iLProcessor261.Body.Instructions[1]));
            iLProcessor261.InsertBefore(iLProcessor261.Body.Instructions[0], Instruction.Create(OpCodes.Call, ulink.MainModule.Import(uLinkMessageCheck)));
            iLProcessor261.InsertBefore(iLProcessor261.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor261.InsertBefore(iLProcessor261.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor261.InsertBefore(iLProcessor261.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor261.InsertBefore(iLProcessor261.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_0));*/

            /*MethodDefinition uLinkMessageCheck = hooksClass.GetMethod("uLinkMessageCheck");
            ILProcessor iLProcessorv25 = vmethod_25.Body.GetILProcessor();
            iLProcessorv25.Body.Instructions.Clear();
            iLProcessorv25.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessorv25.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(uLinkMessageCheck)));
            iLProcessorv25.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));*/
            
            /*MethodDefinition uLinkMessageCheck = hooksClass.GetMethod("uLinkMessageCheck");
            ILProcessor iLProcessor261 = method_261.Body.GetILProcessor();
            iLProcessor261.Body.Instructions.Clear();
            iLProcessor261.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor261.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor261.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor261.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor261.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(uLinkMessageCheck)));
            iLProcessor261.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));*/

            
            MethodDefinition method_17 = Class5.GetMethod("method_17");
            MethodDefinition InternalRPCCheck = hooksClass.GetMethod("InternalRPCCheck");
            ILProcessor iLProcessor17 = method_17.Body.GetILProcessor();
            /*ILProcessor iLProcessor17 = method_17.Body.GetILProcessor();
            int Position = iLProcessor17.Body.Instructions.Count - 67;
            iLProcessor17.InsertBefore(iLProcessor17.Body.Instructions[Position],
                Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(InternalRPCCheck)));
            iLProcessor17.InsertBefore(iLProcessor17.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_0));*/
            
            iLProcessor17.InsertBefore(iLProcessor17.Body.Instructions[0], Instruction.Create(OpCodes.Ret));
            iLProcessor17.InsertBefore(iLProcessor17.Body.Instructions[0], Instruction.Create(OpCodes.Brtrue_S, iLProcessor17.Body.Instructions[1]));
            iLProcessor17.InsertBefore(iLProcessor17.Body.Instructions[0], Instruction.Create(OpCodes.Call, ulink.MainModule.Import(InternalRPCCheck)));
            iLProcessor17.InsertBefore(iLProcessor17.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_0));
            
            

            //MethodDefinition method_124 = Class46.GetMethod("method_124");
            
            //TODO: Removing most of the try catches from ulink for now.
            MethodDefinition update = type.GetMethod("LateUpdate");
            TypeDefinition logger = fougeriteAssembly.MainModule.GetType("Fougerite.Logger");

            method_277.IsPublic = true;
            method_270.IsPublic = true;
            Class5.IsPublic = true;
            Class48.IsPublic = true;
            Class56.IsPublic = true;

            MethodDefinition method = hooksClass.GetMethod("HandleuLinkDisconnect");
            MethodDefinition method2 = hooksClass.GetMethod("RPCFix");
            ILProcessor iLProcessor = method_435.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            method_275.Body.Instructions.Clear();
            method_275.Body.ExceptionHandlers.Clear();
            method_275.Body.Variables.Clear();
            method_275.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            method_275.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            method_275.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            method_275.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(method2)));
            method_275.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            //TODO: Removing most of the try catches from ulink for now.
            TypeDefinition Network = ulink.MainModule.GetType("uLink.Network");
            IEnumerable<MethodDefinition> CloseConnections = Network.GetMethods();
            MethodDefinition logex = logger.GetMethod("LogException");
            
            WrapMethod(method_249, logex, ulink, false);

            WrapMethod(update, logex, ulink, false);
            WrapMethod(method_36, logex, ulink, false);
            WrapMethod(method_25, logex, ulink, false);
            WrapMethod(method_22, logex, ulink, false); 

            WrapMethod(vmethod_3, logex, ulink, false);
            WrapMethod(method_250, logex, ulink, false);
            WrapMethod(method_252, logex, ulink, false);
            WrapMethod(method_269, logex, ulink, false);
            WrapMethod(method_4, logex, ulink, false);
            WrapMethod(method_299, logex, ulink, false);
            WrapMethod(method_20, logex, ulink, false);
            foreach (var x in CloseConnections.Where(x => x.Name == "CloseConnection"))
            {
                WrapMethod(x, logex, ulink, false);
            }
            
            
            /*List<Instruction> ls = method_124.Body.Instructions.Where(x => x.ToString().Contains("ArgumentOutOfRangeException") || x.ToString().Contains("throw")).ToList();
            foreach (var x in ls)
            {
                method_124.Body.Instructions.Remove(x);
            }*/
            this.PatchuLink(ref ulink);
            this.PatchuLink2(ref ulink);
            ulink.Write("uLink.dll");
        }

        /*private void uLinkKeyDuplicationError()
        {
            AssemblyDefinition ulink = AssemblyDefinition.ReadAssembly("uLink.dll");
            TypeDefinition Class48 = ulink.MainModule.GetType("Class48");
            MethodDefinition method_299 = Class48.GetMethod("method_299");
            int Position2 = method_299.Body.Instructions.Count - 3;

             ILProcessor iLProcessor2 = method_299.Body.GetILProcessor();
             iLProcessor2.InsertBefore(method_299.Body.Instructions[Position2],
                 Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(hooksClass.GetMethod("Ulala"))));
             iLProcessor2.InsertBefore(method_299.Body.Instructions[Position2], Instruction.Create(OpCodes.Ldarg_1));
            Instruction md = null;
            foreach (var x in method_299.Body.Instructions)
            {
                Logger.Log(" - " + x);
                if (x.ToString().Contains("Fougerite.Logger")) md = x;
            }
            Logger.Log("s: " + md);
            method_299.Body.Instructions.Remove(md);
            ulink.Write("uLink.dll");
        }*/

        private void LatePostInTryCatch()
        {
            AssemblyDefinition ulink = AssemblyDefinition.ReadAssembly("uLink.dll");
            TypeDefinition Class56 = ulink.MainModule.GetType("Class56");
            MethodDefinition method_22 = Class56.GetMethod("method_22");
            MethodDefinition method_25 = Class56.GetMethod("method_25");
            //MethodDefinition method_44 = Class56.GetMethod("method_44");
            method_22.SetPublic(true);
            method_25.SetPublic(true);
            //method_44.SetPublic(true);

            ulink.Write("uLink.dll");

            TypeDefinition type = rustAssembly.MainModule.GetType("NetCull");
            TypeDefinition type2 = type.GetNestedType("Callbacks");
            MethodDefinition def = type2.GetMethod("FirePreUpdate");
            MethodDefinition def2 = type2.GetMethod("FirePostUpdate");
            def.SetPublic(true);
            def2.SetPublic(true);
            rustAssembly.Write("Assembly-CSharp.dll");
        }

        private void LatePostInTryCatch2()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("NetCull");
            TypeDefinition type2 = type.GetNestedType("Callbacks");
            MethodDefinition def = type2.GetMethod("FirePreUpdate");
            Instruction y = null;
            foreach (Instruction x in def.Body.Instructions)
            {
                if (x.ToString().Contains("LogException"))
                {
                    y = x;
                }
            }
            def.Body.Instructions.Remove(y);

            MethodDefinition def2 = type2.GetMethod("FirePostUpdate");
            Instruction y2 = null;
            foreach (Instruction x in def.Body.Instructions)
            {
                if (x.ToString().Contains("LogException"))
                {
                    y2 = x;
                }
            }
            def2.Body.Instructions.Remove(y2);
        }
        
        private void UpdateDelegatePatch()
        {
            TypeDefinition NetCull = rustAssembly.MainModule.GetType("NetCull");
            TypeDefinition Callbacks = NetCull.GetNestedType("Callbacks");
            TypeDefinition UpdateDelegate = Callbacks.GetNestedType("UpdateDelegate");
            MethodDefinition Invoke = UpdateDelegate.GetMethod("Invoke");
            Instruction y = null;
            foreach (Instruction x in Invoke.Body.Instructions)
            {
                if (x.ToString().Contains("LogException"))
                {
                    y = x;
                }
            }
            Invoke.Body.Instructions.Remove(y);
            rustAssembly.Write("Assembly-CSharp.dll");
        }

        private void LooterPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("LootableObject");
            TypeDefinition Useable = rustAssembly.MainModule.GetType("Useable");
            MethodDefinition ClearLooter = type.GetMethod("ClearLooter");
            ClearLooter.SetPublic(true);
            type.GetField("_currentlyUsingPlayer").SetPublic(true);
            type.GetField("thisClientIsInWindow").SetPublic(true);
            type.GetField("occupierText").SetPublic(true);
            type.GetField("_useable").SetPublic(true);
            type.GetMethod("SendCurrentLooter").SetPublic(true);
            type.GetMethod("DestroyInExit").SetPublic(true);
            type.GetMethod("StopLooting").SetPublic(true);
            
            foreach (var x in Useable.Fields)
            {
                x.SetPublic(true);
            }
            foreach (var x in Useable.Methods)
            {
                x.SetPublic(true);
            }
            

            MethodDefinition SetLooter = type.GetMethod("SetLooter");
            MethodDefinition method = hooksClass.GetMethod("SetLooter");
            ILProcessor iLProcessor = SetLooter.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            MethodDefinition method3 = hooksClass.GetMethod("EnterHandler");
            MethodDefinition Enter = Useable.GetMethod("Enter");
            ILProcessor iLProcessor3 = Enter.Body.GetILProcessor();
            iLProcessor3.Body.Variables.Clear();
            iLProcessor3.Body.ExceptionHandlers.Clear();
            iLProcessor3.Body.Instructions.Clear();
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method3)));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            
            MethodDefinition OnUseEnter = type.GetMethod("OnUseEnter");
            MethodDefinition method2 = hooksClass.GetMethod("OnUseEnter");
            ILProcessor iLProcessor2 = OnUseEnter.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method2)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void UseablePatch()
        {
            TypeDefinition Useable = rustAssembly.MainModule.GetType("Useable");
            Useable.GetMethod("Refresh").SetPublic(true);
            Useable.GetMethod("OnDestroy").SetPublic(true);
            Useable.GetMethod("Update").SetPublic(true);
            Useable.GetMethod("RunUpdate").SetPublic(true);
            Useable.GetMethod("LatchUse").SetPublic(true);
            Useable.GetMethod("Reset").SetPublic(true);
            Useable.GetField("canUse").SetPublic(true);
            Useable.GetField("_user").SetPublic(true);
            Useable.GetField("canUpdate").SetPublic(true);
            Useable.GetField("callState").SetPublic(true);
            Useable.GetNestedType("FunctionCallState").IsPublic = true;
        }

        private void ResearchPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("ResearchToolItem`1");
            MethodDefinition TryCombine = type.GetMethod("TryCombine");
            MethodDefinition method = hooksClass.GetMethod("ResearchItem");

            ILProcessor iLProcessor = TryCombine.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void AntiDecay()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("EnvDecay");
            TypeDefinition definition2 = rustAssembly.MainModule.GetType("StructureMaster");

            MethodDefinition awake = type.GetMethod("Awake");
            MethodDefinition doDecay = type.GetMethod("DoDecay");
            MethodDefinition decayDisabled = hooksClass.GetMethod("DecayDisabled");

            ILProcessor iLProcessor = awake.Body.GetILProcessor();
            iLProcessor.InsertBefore(awake.Body.Instructions[0], Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(decayDisabled)));
            iLProcessor.InsertAfter(awake.Body.Instructions[0], Instruction.Create(OpCodes.Brtrue, awake.Body.Instructions[awake.Body.Instructions.Count - 1]));
            iLProcessor = doDecay.Body.GetILProcessor();
            iLProcessor.InsertBefore(doDecay.Body.Instructions[0], Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(decayDisabled)));
            iLProcessor.InsertAfter(doDecay.Body.Instructions[0], Instruction.Create(OpCodes.Brtrue, doDecay.Body.Instructions[doDecay.Body.Instructions.Count - 1]));
        }

        private void SlotOperationPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("Inventory");
            TypeDefinition CraftingInventory = rustAssembly.MainModule.GetType("CraftingInventory");
            
            IEnumerable<MethodDefinition> allmethods = type.GetMethods();
            MethodDefinition SlotOperation = null;
            foreach (MethodDefinition m in allmethods)
            {
                if (m.Name.Equals("SlotOperation"))
                {
                    m.SetPublic(true);
                    if (m.Parameters.Count == 4)
                    {
                        SlotOperation = m;
                    }
                }
                else if (m.Name.Equals("SlotOperationsMerge"))
                {
                    m.SetPublic(true);
                }
                else if (m.Name.Equals("SlotOperationsMove"))
                {
                    m.SetPublic(true);
                }
            }
            if (SlotOperation != null)
            {
                MethodDefinition method = hooksClass.GetMethod("FGSlotOperation");
                SlotOperation.Body.Instructions.Clear();
                SlotOperation.Body.ExceptionHandlers.Clear();
                SlotOperation.Body.Variables.Clear();
                SlotOperation.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
                SlotOperation.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
                SlotOperation.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
                SlotOperation.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
                SlotOperation.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, SlotOperation.Parameters[3]));
                SlotOperation.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
                SlotOperation.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            }
            MethodDefinition ITSP = type.GetMethod("ITSP");
            MethodDefinition IACT = type.GetMethod("IACT");
            MethodDefinition IAST = type.GetMethod("IAST");
            MethodDefinition ISMV = type.GetMethod("ISMV");
            MethodDefinition ITMG = type.GetMethod("ITMG");
            MethodDefinition ITMV = type.GetMethod("ITMV");
            MethodDefinition ITSM = type.GetMethod("ITSM");
            MethodDefinition SVUC = type.GetMethod("SVUC");
            MethodDefinition CRFS = CraftingInventory.GetMethod("CRFS");
            
            MethodDefinition ITSPHook = hooksClass.GetMethod("ITSPHook");
            MethodDefinition IACTHook = hooksClass.GetMethod("IACTHook");
            MethodDefinition IASTHook = hooksClass.GetMethod("IASTHook");
            MethodDefinition ISMVHook = hooksClass.GetMethod("ISMVHook");
            MethodDefinition ITMGHook = hooksClass.GetMethod("ITMGHook");
            MethodDefinition ITMVHook = hooksClass.GetMethod("ITMVHook");
            MethodDefinition ITSMHook = hooksClass.GetMethod("ITSMHook");
            MethodDefinition SVUCHook = hooksClass.GetMethod("SVUCHook");
            MethodDefinition CRFSHook = hooksClass.GetMethod("CRFSHook");
            
            CRFS.Body.Instructions.Clear();
            CRFS.Body.ExceptionHandlers.Clear();
            CRFS.Body.Variables.Clear();
            CRFS.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            CRFS.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            CRFS.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            CRFS.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            CRFS.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(CRFSHook)));
            CRFS.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            SVUC.Body.Instructions.Clear();
            SVUC.Body.ExceptionHandlers.Clear();
            SVUC.Body.Variables.Clear();
            SVUC.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            SVUC.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            SVUC.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            SVUC.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(SVUCHook)));
            SVUC.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ITSM.Body.Instructions.Clear();
            ITSM.Body.ExceptionHandlers.Clear();
            ITSM.Body.Variables.Clear();
            ITSM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ITSM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            ITSM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            ITSM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            ITSM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, ITSM.Parameters[3]));
            ITSM.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(ITSMHook)));
            ITSM.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ITMV.Body.Instructions.Clear();
            ITMV.Body.ExceptionHandlers.Clear();
            ITMV.Body.Variables.Clear();
            ITMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ITMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            ITMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            ITMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            ITMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, ITMV.Parameters[3]));
            ITMV.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(ITMVHook)));
            ITMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ITMG.Body.Instructions.Clear();
            ITMG.Body.ExceptionHandlers.Clear();
            ITMG.Body.Variables.Clear();
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, ITMG.Parameters[3]));
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, ITMG.Parameters[4]));
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(ITMGHook)));
            ITMG.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ISMV.Body.Instructions.Clear();
            ISMV.Body.ExceptionHandlers.Clear();
            ISMV.Body.Variables.Clear();
            ISMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ISMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            ISMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            ISMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            ISMV.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(ISMVHook)));
            ISMV.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ITSP.Body.Instructions.Clear();
            ITSP.Body.ExceptionHandlers.Clear();
            ITSP.Body.Variables.Clear();
            ITSP.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ITSP.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            ITSP.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            ITSP.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(ITSPHook)));
            ITSP.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            IACT.Body.Instructions.Clear();
            IACT.Body.ExceptionHandlers.Clear();
            IACT.Body.Variables.Clear();
            IACT.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            IACT.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            IACT.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            IACT.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            IACT.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(IACTHook)));
            IACT.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            IAST.Body.Instructions.Clear();
            IAST.Body.ExceptionHandlers.Clear();
            IAST.Body.Variables.Clear();
            IAST.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            IAST.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            IAST.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            IAST.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            IAST.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(IASTHook)));
            IAST.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
        }

        private void RepairBenchEvent()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("RepairBench");
            MethodDefinition CompleteRepair = type.GetMethod("CompleteRepair");
            MethodDefinition method = hooksClass.GetMethod("FGCompleteRepair");
            CompleteRepair.Body.Instructions.Clear();
            CompleteRepair.Body.ExceptionHandlers.Clear();
            CompleteRepair.Body.Variables.Clear();
            CompleteRepair.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            CompleteRepair.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            CompleteRepair.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            CompleteRepair.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }


        private void FieldsUpdatePatch()
        {
            TypeDefinition Metabolism = rustAssembly.MainModule.GetType("Metabolism");
            Metabolism.GetField("coreTemperature").SetPublic(true);
            Metabolism.GetField("caloricLevel").SetPublic(true);
            Metabolism.GetField("radiationLevel").SetPublic(true);
            Metabolism.GetField("waterLevelLitre").SetPublic(true);
            Metabolism.GetField("antiRads").SetPublic(true);
            Metabolism.GetField("poisonLevel").SetPublic(true);
            Metabolism.GetField("_lastWarmTime").SetPublic(true);

            TypeDefinition User = rustAssembly.MainModule.GetType("RustProto", "User");
            User.GetField("displayname_").SetPublic(true);

            TypeDefinition ResourceGivePair = rustAssembly.MainModule.GetType("ResourceGivePair");
            ResourceGivePair.GetField("_resourceItemDatablock").SetPublic(true);

            TypeDefinition StructureMaster = rustAssembly.MainModule.GetType("StructureMaster");
            StructureMaster.GetField("_structureComponents").SetPublic(true);
            StructureMaster.GetField("_weightOnMe").SetPublic(true);
            StructureMaster.GetField("meshBatchTargetPhysical").SetPublic(true);
            StructureMaster.GetField("_materialType").SetPublic(true);
            
            TypeDefinition StructureComponent = rustAssembly.MainModule.GetType("StructureComponent");
            StructureComponent.GetMethod("OnOwnedByMasterStructure").SetPublic(true);
            StructureComponent.GetMethod("ClearMaster").SetPublic(true);

            TypeDefinition InventoryHolder = rustAssembly.MainModule.GetType("InventoryHolder");
            InventoryHolder.GetField("isPlayerInventory").SetPublic(true);
            MethodDefinition m = InventoryHolder.GetMethod("GetPlayerInventory");
            m.SetPublic(true);

            TypeDefinition PlayerInventory = rustAssembly.MainModule.GetType("PlayerInventory");
            PlayerInventory.GetField("_boundBPs").SetPublic(true);

            TypeDefinition Inv2 = rustAssembly.MainModule.GetType("Inventory");
            Inv2.GetNestedType("SlotOperationsInfo").IsPublic = true;
            Inv2.GetNestedType("SlotOperations").IsPublic = true;

            TypeDefinition BasicDoor = rustAssembly.MainModule.GetType("BasicDoor");
            BasicDoor.GetField("state").SetPublic(true);
            BasicDoor.GetNestedType("State").IsPublic = true;
            BasicDoor.GetNestedType("Side").IsPublic = true;
            BasicDoor.GetNestedType("RunFlags").IsPublic = true;
            
            foreach (MethodDefinition met in BasicDoor.Methods)
            {
                if (met.Name.Equals("ToggleStateServer"))
                {
                    met.SetPublic(true);
                }
            }

            TypeDefinition SleepingAvatar = rustAssembly.MainModule.GetType("SleepingAvatar");
            var methods = SleepingAvatar.Methods;
            foreach (var x in methods)
            {
                if (!x.IsPublic && x.Name == "Close")
                {
                    x.SetPublic(true);
                    break;
                }
            }

            TypeDefinition BulletWeaponDataBlock = rustAssembly.MainModule.GetType("BulletWeaponDataBlock");
            BulletWeaponDataBlock.GetMethod("ConstructItem").SetPublic(true);
            TypeDefinition ITEM_TYPE = BulletWeaponDataBlock.GetNestedType("ITEM_TYPE");
            ITEM_TYPE.IsPublic = true;
            ITEM_TYPE.IsSealed = false;
            foreach (var x in ITEM_TYPE.GetMethods())
            {
                ITEM_TYPE.GetMethod(x.Name).SetPublic(true);
            }

            TypeDefinition IBulletWeaponItem = rustAssembly.MainModule.GetType("IBulletWeaponItem");
            IBulletWeaponItem.GetProperty("cachedCasings").GetMethod.SetPublic(true);
            IBulletWeaponItem.GetProperty("cachedCasings").SetMethod.SetPublic(true);

            IBulletWeaponItem.GetProperty("clipAmmo").GetMethod.SetPublic(true);
            IBulletWeaponItem.GetProperty("clipAmmo").SetMethod.SetPublic(true);

            IBulletWeaponItem.GetProperty("clipType").GetMethod.SetPublic(true);
            //IBulletWeaponItem.GetProperty("clipType").SetMethod.SetPublic(true);

            IBulletWeaponItem.GetProperty("nextCasingsTime").GetMethod.SetPublic(true);
            //IBulletWeaponItem.GetProperty("nextCasingsTime").SetMethod.SetPublic(true);

            TypeDefinition IHeldItem = rustAssembly.MainModule.GetType("IHeldItem");

            IHeldItem.GetProperty("canActivate").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("canActivate").SetMethod.SetPublic(true);

            IHeldItem.GetProperty("canDeactivate").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("canDeactivate").SetMethod.SetPublic(true); 

            IHeldItem.GetProperty("freeModSlots").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("freeModSlots").SetMethod.SetPublic(true);

            IHeldItem.GetProperty("itemMods").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("itemMods").SetMethod.SetPublic(true);

            IHeldItem.GetProperty("itemRepresentation").GetMethod.SetPublic(true);
            IHeldItem.GetProperty("itemRepresentation").SetMethod.SetPublic(true);

            IHeldItem.GetProperty("modFlags").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("modFlags").SetMethod.SetPublic(true);

            IHeldItem.GetProperty("totalModSlots").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("totalModSlots").SetMethod.SetPublic(true);

            IHeldItem.GetProperty("usedModSlots").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("usedModSlots").SetMethod.SetPublic(true);

            IHeldItem.GetProperty("viewModelInstance").GetMethod.SetPublic(true);
            //IHeldItem.GetProperty("viewModelInstance").SetMethod.SetPublic(true);

            IHeldItem.GetMethod("AddMod").SetPublic(true);
            IHeldItem.GetMethod("FindMod").SetPublic(true);
            IHeldItem.GetMethod("ServerFrame").SetPublic(true);
            IHeldItem.GetMethod("SetTotalModSlotCount").SetPublic(true);
            IHeldItem.GetMethod("SetUsedModSlotCount").SetPublic(true);

            TypeDefinition IInventoryItem = rustAssembly.MainModule.GetType("IInventoryItem");

            IInventoryItem.GetProperty("active").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("active").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("character").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("character").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("condition").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("condition").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("controllable").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("controllable").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("controller").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("controller").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("datablock").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("datablock").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("dirty").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("dirty").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("doNotSave").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("doNotSave").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("idMain").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("idMain").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("inventory").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("inventory").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("isInLocalInventory").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("isInLocalInventory").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("lastUseTime").GetMethod.SetPublic(true);
            IInventoryItem.GetProperty("lastUseTime").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("maxcondition").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("maxcondition").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("slot").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("slot").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("toolTip").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("toolTip").SetMethod.SetPublic(true);

            IInventoryItem.GetProperty("uses").GetMethod.SetPublic(true);
            //IInventoryItem.GetProperty("uses").SetMethod.SetPublic(true);

            IInventoryItem.GetMethod("AddUses").SetPublic(true);
            IInventoryItem.GetMethod("Consume").SetPublic(true);
            IInventoryItem.GetMethod("FireClientSideItemEvent").SetPublic(true);
            IInventoryItem.GetMethod("GetConditionPercent").SetPublic(true);
            IInventoryItem.GetMethod("IsBroken").SetPublic(true);
            IInventoryItem.GetMethod("IsDamaged").SetPublic(true);
            IInventoryItem.GetMethod("Load").SetPublic(true);
            IInventoryItem.GetMethod("MarkDirty").SetPublic(true);
            IInventoryItem.GetMethod("OnAddedTo").SetPublic(true);
            IInventoryItem.GetMethod("OnBeltUse").SetPublic(true);
            IInventoryItem.GetMethod("OnMenuOption").SetPublic(true);
            IInventoryItem.GetMethod("OnMovedTo").SetPublic(true);
            IInventoryItem.GetMethod("Save").SetPublic(true);
            IInventoryItem.GetMethod("SetCondition").SetPublic(true);
            IInventoryItem.GetMethod("SetMaxCondition").SetPublic(true);
            IInventoryItem.GetMethod("SetUses").SetPublic(true);
            IInventoryItem.GetMethod("TryCombine").SetPublic(true);
            IInventoryItem.GetMethod("TryConditionLoss").SetPublic(true);
            IInventoryItem.GetMethod("TryStack").SetPublic(true);

            TypeDefinition IWeaponItem = rustAssembly.MainModule.GetType("IWeaponItem");

            IWeaponItem.GetProperty("canAim").GetMethod.SetPublic(true);
            //IWeaponItem.GetProperty("canAim").SetMethod.SetPublic(true);

            IWeaponItem.GetProperty("canPrimaryAttack").GetMethod.SetPublic(true);
            //IWeaponItem.GetProperty("canPrimaryAttack").SetMethod.SetPublic(true);

            IWeaponItem.GetProperty("canSecondaryAttack").GetMethod.SetPublic(true);
            //IWeaponItem.GetProperty("canSecondaryAttack").SetMethod.SetPublic(true);

            IWeaponItem.GetProperty("deployed").GetMethod.SetPublic(true);
            //IWeaponItem.GetProperty("deployed").SetMethod.SetPublic(true);

            IWeaponItem.GetProperty("deployFinishedTime").GetMethod.SetPublic(true);
            IWeaponItem.GetProperty("deployFinishedTime").SetMethod.SetPublic(true);

            IWeaponItem.GetProperty("nextPrimaryAttackTime").GetMethod.SetPublic(true);
            IWeaponItem.GetProperty("nextPrimaryAttackTime").SetMethod.SetPublic(true);

            IWeaponItem.GetProperty("nextSecondaryAttackTime").GetMethod.SetPublic(true);
            IWeaponItem.GetProperty("nextSecondaryAttackTime").SetMethod.SetPublic(true);

            IWeaponItem.GetProperty("possibleReloadCount").GetMethod.SetPublic(true);
            //IWeaponItem.GetProperty("possibleReloadCount").SetMethod.SetPublic(true);

            IWeaponItem.GetMethod("PrimaryAttack").SetPublic(true);
            IWeaponItem.GetMethod("Reload").SetPublic(true);
            IWeaponItem.GetMethod("SecondaryAttack").SetPublic(true);
            IWeaponItem.GetMethod("ValidatePrimaryMessageTime").SetPublic(true);


            ITEM_TYPE.GetMethod("IBulletWeaponItem.get_cachedCasings").SetPublic(true);
            ITEM_TYPE.GetMethod("IBulletWeaponItem.get_clipAmmo").SetPublic(true);
            ITEM_TYPE.GetMethod("IBulletWeaponItem.get_clipType").SetPublic(true);
            ITEM_TYPE.GetMethod("IBulletWeaponItem.get_nextCasingsTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IBulletWeaponItem.set_cachedCasings").SetPublic(true);
            ITEM_TYPE.GetMethod("IBulletWeaponItem.set_clipAmmo").SetPublic(true);
            ITEM_TYPE.GetMethod("IBulletWeaponItem.set_nextCasingsTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.AddMod").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.FindMod").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_canActivate").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_canDeactivate").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_freeModSlots").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_itemMods").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_itemRepresentation").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_modFlags").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_totalModSlots").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_usedModSlots").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.get_viewModelInstance").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.set_itemRepresentation").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.SetTotalModSlotCount").SetPublic(true);
            ITEM_TYPE.GetMethod("IHeldItem.SetUsedModSlotCount").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.AddUses").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.Consume").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_active").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_character").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_condition").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_controllable").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_controller").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_dirty").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_idMain").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_inventory").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_isInLocalInventory").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_lastUseTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_maxcondition").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_slot").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.get_uses").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.GetConditionPercent").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.IsBroken").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.IsDamaged").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.Load").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.MarkDirty").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.Save").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.set_lastUseTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.SetCondition").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.SetMaxCondition").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.SetUses").SetPublic(true);
            ITEM_TYPE.GetMethod("IInventoryItem.TryConditionLoss").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.get_canAim").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.get_deployFinishedTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.get_nextPrimaryAttackTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.get_nextSecondaryAttackTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.set_deployFinishedTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.set_nextPrimaryAttackTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.set_nextSecondaryAttackTime").SetPublic(true);
            ITEM_TYPE.GetMethod("IWeaponItem.ValidatePrimaryMessageTime").SetPublic(true);
            ITEM_TYPE.GetProperty("IInventoryItem.datablock").GetMethod.SetPublic(true);

            TypeDefinition ItemModDataBlock = rustAssembly.MainModule.GetType("ItemModDataBlock");
            ItemModDataBlock.GetMethod("ConstructItem").SetPublic(true);
            ITEM_TYPE = ItemModDataBlock.GetNestedType("ITEM_TYPE");
            ITEM_TYPE.IsPublic = true;
            ITEM_TYPE.IsSealed = false;
            TypeDefinition ItemDataBlock = rustAssembly.MainModule.GetType("ItemDataBlock");
            ItemDataBlock.GetMethod("ConstructItem").SetPublic(true);
            ITEM_TYPE = ItemDataBlock.GetNestedType("ITEM_TYPE");
            ITEM_TYPE.IsPublic = true;
            ITEM_TYPE.IsSealed = false;
            //TypeDefinition ServerManagement = rustAssembly.MainModule.GetType("ServerManagement");
            //MethodDefinition EraseCharactersForClient = ServerManagement.GetMethod("EraseCharactersForClient");
            //EraseCharactersForClient.SetPublic(true);

            /*TypeDefinition Inventory = rustAssembly.MainModule.GetType("Inventory");
            TypeDefinition SlotOperationsInfo = Inventory.GetNestedType("SlotOperationsInfo").;
            Logger.Log(SlotOperationsInfo.ToString());*/

            /*TypeDefinition wildlifeManager = rustAssembly.MainModule.GetType("WildlifeManager");
            wildlifeManager.GetNestedType("Data").SetPublic(true);*/
        }

        private void MetaBolismMethodMod()
        {
            TypeDefinition Metabolism = rustAssembly.MainModule.GetType("Metabolism");
            MethodDefinition method = hooksClass.GetMethod("RecieveNetwork");
            MethodDefinition RecieveNetwork = Metabolism.GetMethod("RecieveNetwork");
            this.CloneMethod(RecieveNetwork);
            Array a = RecieveNetwork.Parameters.ToArray();
            RecieveNetwork.Body.Instructions.Clear();
            RecieveNetwork.Body.ExceptionHandlers.Clear();
            RecieveNetwork.Body.Variables.Clear();
            RecieveNetwork.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            foreach (ParameterDefinition p in a)
            {
                RecieveNetwork.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, p));
            }
            RecieveNetwork.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            RecieveNetwork.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            TypeDefinition logger = fougeriteAssembly.MainModule.GetType("Fougerite.Logger");
            MethodDefinition logex = logger.GetMethod("LogException");

            WrapMethod(RecieveNetwork, logex, rustAssembly, false);
        }

        private void ItemPickupHook()
        {
            TypeDefinition ItemPickup = rustAssembly.MainModule.GetType("ItemPickup");
            ItemPickup.GetMethod("RemoveThis").SetPublic(true);
            ItemPickup.GetMethod("UpdateItemInfo").SetPublic(true);
            MethodDefinition PlayerUse = ItemPickup.GetMethod("PlayerUse");
            MethodDefinition method = hooksClass.GetMethod("ItemPickup");
            
            ILProcessor iLProcessor = PlayerUse.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void FallDamageHook()
        {
            TypeDefinition falldamage = rustAssembly.MainModule.GetType("falldamage");
            falldamage.IsPublic = true;
            
            TypeDefinition FallDamage = rustAssembly.MainModule.GetType("FallDamage");
            MethodDefinition FallImpact = FallDamage.GetMethod("FallImpact");
            MethodDefinition method = hooksClass.GetMethod("FallDamage");

            ILProcessor iLProcessor = FallImpact.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            MethodDefinition FallDamageCheck = hooksClass.GetMethod("FallDamageCheck");

            MethodDefinition flo = FallDamage.GetMethod("fIo");
            ILProcessor iLProcessor2 = flo.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(FallDamageCheck)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

        }

        private void FurnacePatch()
        {
            TypeDefinition FireBarrel = rustAssembly.MainModule.GetType("FireBarrel");
            MethodDefinition SetOn = FireBarrel.GetMethod("SetOn");
            MethodDefinition UpdateNetState = FireBarrel.GetMethod("UpdateNetState");
            MethodDefinition GetCookDuration = FireBarrel.GetMethod("GetCookDuration");
            MethodDefinition FireBarrelSetOn = hooksClass.GetMethod("FireBarrelSetOn");
            MethodDefinition PlayerUse = FireBarrel.GetMethod("PlayerUse");

            FireBarrel.GetField("_deployable").SetPublic(true);
            FireBarrel.GetField("_lightFlickerTarget").SetPublic(true);
            FireBarrel.GetField("_lightIntensityInitial").SetPublic(true);
            UpdateNetState.SetPublic(true);
            GetCookDuration.SetPublic(true);
            
            // Add a new field to store the last controllable user
            FieldDefinition lastUserField = new FieldDefinition("lastControllableUser", 
                FieldAttributes.Public, 
                rustAssembly.MainModule.GetType("Controllable"));
            FireBarrel.Fields.Add(lastUserField);
            
            ILProcessor playerUseIL = PlayerUse.Body.GetILProcessor();
    
            // Insert at beginning: this._lastControllableUser = looter;
            Instruction firstInstr = playerUseIL.Body.Instructions[0];
            playerUseIL.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldarg_0));
            playerUseIL.InsertBefore(firstInstr, Instruction.Create(OpCodes.Ldarg_1));
            playerUseIL.InsertBefore(firstInstr, Instruction.Create(OpCodes.Stfld, lastUserField));
            
            ILProcessor iLProcessor = SetOn.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(FireBarrelSetOn)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void PatchFireBarrelActTrigger()
        {
            TypeDefinition fireBarrelType = rustAssembly.MainModule.GetType("FireBarrel");
            TypeDefinition characterType = rustAssembly.MainModule.GetType("Character");
            
            PropertyDefinition controllableProperty =
                characterType.Properties.First(p => p.Name == "controllable");

            MethodReference getterRef = rustAssembly.MainModule.Import(controllableProperty.GetMethod);
            
            MethodDefinition actTrigger = fireBarrelType.Methods.First(m =>
                m.Name == "ActTrigger" && m.Parameters.Count == 3);

            ILProcessor processor = actTrigger.Body.GetILProcessor();
            var instructions = actTrigger.Body.Instructions;
            
            var targetIndices = new List<int>();
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Call && instructions[i].Operand.ToString().Contains("PlayerUse"))
                {
                    if (i > 0 && instructions[i - 1].OpCode == OpCodes.Ldnull)
                    {
                        targetIndices.Add(i);
                    }
                }
            }
            
            foreach (int callIdx in targetIndices.OrderByDescending(x => x))
            {
                Instruction callInstr = instructions[callIdx];
                Instruction oldLdNull = instructions[callIdx - 1];
                
                Instruction loadForCheck = processor.Create(OpCodes.Ldarg_1);
                Instruction loadForGetter = processor.Create(OpCodes.Ldarg_1);
                Instruction callGetter = processor.Create(OpCodes.Callvirt, getterRef);
                Instruction pushNull = processor.Create(OpCodes.Ldnull);
                
                Instruction jumpToGetter = processor.Create(OpCodes.Brtrue_S, loadForGetter);
                Instruction jumpToCall = processor.Create(OpCodes.Br_S, callInstr);
                
                processor.Replace(oldLdNull, loadForCheck);
                
                processor.InsertAfter(loadForCheck, jumpToGetter);
                processor.InsertAfter(jumpToGetter, pushNull);
                processor.InsertAfter(pushNull, jumpToCall);
                processor.InsertAfter(jumpToCall, loadForGetter);
                processor.InsertAfter(loadForGetter, callGetter);
            }
        }

        private void PatchFireBarrelContextRespond()
        {
            TypeDefinition fireBarrelType = rustAssembly.MainModule.GetType("FireBarrel");

            FieldReference lastUserField = fireBarrelType.Fields.First(f => f.Name == "lastControllableUser");

            MethodDefinition contextMethod = fireBarrelType.Methods.First(m =>
                m.Name == "ContextRespond_SetFireBarrelOn");

            ILProcessor processor = contextMethod.Body.GetILProcessor();
            var instructions = contextMethod.Body.Instructions;

            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].OpCode == OpCodes.Callvirt &&
                    instructions[i].Operand.ToString().Contains("TrySetOn"))
                {
                    Instruction targetInstr = instructions[i - 4];

                    if (targetInstr.OpCode == OpCodes.Ldarg_0)
                    {
                        Instruction ldThis = processor.Create(OpCodes.Ldarg_0);
                        Instruction ldControllable = processor.Create(OpCodes.Ldarg_1);

                        Instruction assignInstr = processor.Create(OpCodes.Stfld,
                            rustAssembly.MainModule.Import(lastUserField));

                        processor.InsertBefore(targetInstr, ldThis);
                        processor.InsertBefore(targetInstr, ldControllable);
                        processor.InsertBefore(targetInstr, assignInstr);

                        break; // We only need to patch this once
                    }
                }
            }
        }

        private void HumanControllerPatch()
        {
            TypeDefinition HumanController = rustAssembly.MainModule.GetType("HumanController");
            MethodDefinition method = hooksClass.GetMethod("ClientMove");
            MethodDefinition ProcessGetClientMove = hooksClass.GetMethod("ProcessGetClientMove");
            MethodDefinition GetClientMove = HumanController.GetMethod("GetClientMove");
            
            HumanController.GetField("clockTest").SetPublic(true);
            HumanController.GetField("thatsRightPatWeDontNeedComments").SetPublic(true);
            HumanController.GetField("serverLastTimestamp").SetPublic(true);
            HumanController.GetField("clientMoveDropped").SetPublic(true);
            HumanController.GetField("clockTest").SetPublic(true);

            
            this.CloneMethod(GetClientMove);

            int num = 0;
            ParameterDefinition parameter = method.Parameters[0];
            MethodReference reference =
                this.rustAssembly.MainModule.GetType("IDLocalCharacter").GetMethod("get_netUser");
            MethodReference reference2 = this.rustAssembly.MainModule.GetType("NetUser").GetMethod("Kick");
            TypeDefinition self = this.unityAssembly.MainModule.GetType("UnityEngine.Vector3");
            FieldReference field = this.rustAssembly.MainModule.Import(self.GetField("x"));
            FieldReference reference4 = this.rustAssembly.MainModule.Import(self.GetField("y"));
            FieldReference reference5 = this.rustAssembly.MainModule.Import(self.GetField("z"));
            TypeDefinition definition7 = this.mscorlib.MainModule.GetType("System.Single");
            MethodReference reference6 = this.rustAssembly.MainModule.Import(definition7.GetMethod("IsNaN"));
            MethodReference reference7 = this.rustAssembly.MainModule.Import(definition7.GetMethod("IsInfinity"));
            ILProcessor iLProcessor = GetClientMove.Body.GetILProcessor();
            
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldarga_S, parameter));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldfld, field));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Call, reference6));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldarga_S, parameter));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldfld, field));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Call, reference7));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldarga_S, parameter));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldfld, reference4));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Call, reference6));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldarga_S, parameter));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldfld, reference4));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Call, reference7));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldarga_S, parameter));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldfld, reference5));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Call, reference6));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldarga_S, parameter));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldfld, reference5));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Call, reference7));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Call, reference));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldc_I4, 0x8d));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ldc_I4_1));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Callvirt, reference2));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Pop));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[num++], Instruction.Create(OpCodes.Ret));
            iLProcessor.Replace(GetClientMove.Body.Instructions[3],
                Instruction.Create(OpCodes.Brtrue_S, GetClientMove.Body.Instructions[0x18]));
            iLProcessor.Replace(GetClientMove.Body.Instructions[7],
                Instruction.Create(OpCodes.Brtrue_S, GetClientMove.Body.Instructions[0x18]));
            iLProcessor.Replace(GetClientMove.Body.Instructions[11],
                Instruction.Create(OpCodes.Brtrue_S, GetClientMove.Body.Instructions[0x18]));
            iLProcessor.Replace(GetClientMove.Body.Instructions[15],
                Instruction.Create(OpCodes.Brtrue_S, GetClientMove.Body.Instructions[0x18]));
            iLProcessor.Replace(GetClientMove.Body.Instructions[0x13],
                Instruction.Create(OpCodes.Brtrue_S, GetClientMove.Body.Instructions[0x18]));
            iLProcessor.Replace(GetClientMove.Body.Instructions[0x17],
                Instruction.Create(OpCodes.Brfalse_S, GetClientMove.Body.Instructions[0x1f]));

            
            Array a = GetClientMove.Parameters.ToArray();
            Array.Reverse(a);
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[32],
                Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            foreach (ParameterDefinition p in a)
            {
                iLProcessor.InsertBefore(GetClientMove.Body.Instructions[32], Instruction.Create(OpCodes.Ldarg_S, p));
            }
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[32], Instruction.Create(OpCodes.Ldarg_0));
            
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[0], Instruction.Create(OpCodes.Ret));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[0], Instruction.Create(OpCodes.Brtrue_S, GetClientMove.Body.Instructions[1]));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[0], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(ProcessGetClientMove)));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_S, GetClientMove.Parameters[GetClientMove.Parameters.Count - 1]));
            iLProcessor.InsertBefore(GetClientMove.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_0));

            //TypeDefinition logger = fougeriteAssembly.MainModule.GetType("Fougerite.Logger");
            //MethodDefinition logex = logger.GetMethod("LogException");
            //WrapMethod(GetClientMove, logex, rustAssembly, false);
        }

        private void CraftingPatch()
        {
            TypeDefinition CraftingInventory = rustAssembly.MainModule.GetType("CraftingInventory");
            CraftingInventory.GetMethod("FindBlueprint").SetPublic(true);
            
            MethodDefinition CancelCrafting = CraftingInventory.GetMethod("CancelCrafting");
            CancelCrafting.SetPublic(true);
            MethodDefinition StartCrafting = null;
            IEnumerable<MethodDefinition> allmethods = CraftingInventory.GetMethods();
            foreach (MethodDefinition m in allmethods)
            {
                if (m.Name.Equals("StartCrafting"))
                {
                    m.SetPublic(true);
                    if (m.Parameters.Count == 3)
                    {
                        StartCrafting = m;
                    }
                }
            }
            MethodDefinition method = hooksClass.GetMethod("CraftingEvent");

            this.CloneMethod(StartCrafting);

            ILProcessor iLProcessor = StartCrafting.Body.GetILProcessor();
            int Position = StartCrafting.Body.Instructions.Count - 2;
            iLProcessor.InsertBefore(StartCrafting.Body.Instructions[Position], 
                Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(StartCrafting.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor.InsertBefore(StartCrafting.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.InsertBefore(StartCrafting.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.InsertBefore(StartCrafting.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_0));
        }

        private void CraftingCancelAndCompletePatch()
        {
            TypeDefinition CraftingInventory = rustAssembly.MainModule.GetType("CraftingInventory");
            MethodDefinition UpdateCraftingDataToOwner = CraftingInventory.GetMethod("UpdateCraftingDataToOwner");
            MethodDefinition CancelCrafting = CraftingInventory.GetMethod("CancelCrafting");
            MethodDefinition CompleteCrafting = CraftingInventory.GetMethod("CompleteCrafting");
            MethodDefinition EndCrafting = CraftingInventory.GetMethod("EndCrafting");
            MethodDefinition cancelHook = hooksClass.GetMethod("CraftingCancelEvent");
            MethodDefinition completeHook = hooksClass.GetMethod("CraftingCompleteEvent");
            FieldDefinition crafting = CraftingInventory.GetField("crafting");

            crafting.SetPublic(true);
            EndCrafting.SetPublic(true);
            UpdateCraftingDataToOwner.SetPublic(true);
            
            CancelCrafting.Body.Instructions.Clear();
            CancelCrafting.Body.Variables.Clear();
            CancelCrafting.Body.ExceptionHandlers.Clear();
            CancelCrafting.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            CancelCrafting.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(cancelHook)));
            CancelCrafting.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            CompleteCrafting.SetPublic(true);
            CompleteCrafting.Body.Instructions.Clear();
            CompleteCrafting.Body.Variables.Clear();
            CompleteCrafting.Body.ExceptionHandlers.Clear();
            CompleteCrafting.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            CompleteCrafting.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(completeHook)));
            CompleteCrafting.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void NavMeshPatch()
        {
            TypeDefinition BasicWildLifeMovement = rustAssembly.MainModule.GetType("BaseAIMovement");
            MethodDefinition DoMove = BasicWildLifeMovement.GetMethod("DoMove");
            MethodDefinition method = hooksClass.GetMethod("AnimalMovement");
            this.CloneMethod(DoMove);

            ILProcessor iLProcessor = DoMove.Body.GetILProcessor();
            iLProcessor.InsertBefore(DoMove.Body.Instructions[0],
                Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(DoMove.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.InsertBefore(DoMove.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.InsertBefore(DoMove.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_0));

        }

        private void ResourceSpawned()
        {
            TypeDefinition ResourceTarget = rustAssembly.MainModule.GetType("ResourceTarget");
            ResourceTarget.GetField("gatherProgress").SetPublic(true);
            ResourceTarget.GetField("startingTotal").SetPublic(true);

            MethodDefinition Awake = ResourceTarget.GetMethod("Awake");
            this.CloneMethod(Awake);

            MethodDefinition method = hooksClass.GetMethod("ResourceSpawned");
            ILProcessor iLProcessor = Awake.Body.GetILProcessor();

            int c = Awake.Body.Instructions.Count - 1;

            iLProcessor.InsertBefore(Awake.Body.Instructions[c],
                Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(Awake.Body.Instructions[c], Instruction.Create(OpCodes.Ldarg_0));
        }

        private void InventoryModifications()
        {
            // ItemAdded part.
            TypeDefinition Inventory = rustAssembly.MainModule.GetType("Inventory");
            TypeDefinition Payload = Inventory.GetNestedType("Payload");
            TypeDefinition Assignment = Payload.GetNestedType("Assignment");
            TypeDefinition collection = Inventory.GetNestedType("Collection`1");
            collection.IsPublic = true;
            Assignment.IsPublic = true;
            Payload.IsPublic = true;
            Inventory.GetField("_netListeners").SetPublic(true);
            MethodDefinition CheckSlotFlagsAgainstSlot = Inventory.GetMethod("CheckSlotFlagsAgainstSlot");
            CheckSlotFlagsAgainstSlot.SetPublic(true);
            MethodDefinition ItemAdded = Inventory.GetMethod("ItemAdded");
            ItemAdded.SetPublic(true);
            
            MethodDefinition ADDHOOK = hooksClass.GetMethod("ItemAdded");
            MethodDefinition REMHOOK = hooksClass.GetMethod("ItemRemoved");

            MethodDefinition AssignItem = Payload.GetMethod("AssignItem");
            ILProcessor iLProcessorASS = AssignItem.Body.GetILProcessor();
            iLProcessorASS.Body.Instructions.Clear();
            iLProcessorASS.Body.ExceptionHandlers.Clear();
            iLProcessorASS.Body.Variables.Clear();
            iLProcessorASS.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessorASS.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(ADDHOOK)));
            iLProcessorASS.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            // Remove part.
            Inventory.GetProperty("collection").GetMethod.SetPublic(true);
            Inventory.GetMethod("ItemRemoved").SetPublic(true);


            MethodDefinition RemoveItem = null;
            foreach (var x in Inventory.GetMethods())
            {
                if (x.Name == "RemoveItem" && x.Parameters.Count == 3)
                {
                    RemoveItem = x;
                    break;
                }
            }
            
            ILProcessor iLProcessorREM = RemoveItem.Body.GetILProcessor();
            iLProcessorREM.Body.Instructions.Clear();
            iLProcessorREM.Body.ExceptionHandlers.Clear();
            iLProcessorREM.Body.Variables.Clear();
            iLProcessorREM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessorREM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessorREM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessorREM.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessorREM.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(REMHOOK)));
            iLProcessorREM.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void AirdropPatch()
        {
            TypeDefinition SupplyDropZone = rustAssembly.MainModule.GetType("SupplyDropZone");
            MethodDefinition CallAirDropAt = SupplyDropZone.GetMethod("CallAirDropAt");
            MethodDefinition Airdrop = hooksClass.GetMethod("Airdrop");
            
            ILProcessor iLProcessorCallAirDropAt = CallAirDropAt.Body.GetILProcessor();
            iLProcessorCallAirDropAt.Body.Instructions.Clear();
            iLProcessorCallAirDropAt.Body.Variables.Clear();
            iLProcessorCallAirDropAt.Body.ExceptionHandlers.Clear();
            iLProcessorCallAirDropAt.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessorCallAirDropAt.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(Airdrop)));
            iLProcessorCallAirDropAt.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            TypeDefinition SupplyDropPlane = rustAssembly.MainModule.GetType("SupplyDropPlane");
            MethodDefinition DropCrate = SupplyDropPlane.GetMethod("DropCrate");
            MethodDefinition AirdropCrateDropped = hooksClass.GetMethod("AirdropCrateDropped");
            
            ILProcessor iLProcessor = DropCrate.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(AirdropCrateDropped)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            MethodDefinition SupplyDropPlaneConstructor = null;
            foreach (var x in SupplyDropPlane.GetConstructors())
            {
                if (x.IsConstructor && x.Parameters.Count == 1)
                {
                    SupplyDropPlaneConstructor = x;
                    break;
                }
            }
            
            int Position = SupplyDropPlaneConstructor.Body.Instructions.Count - 1;

            MethodDefinition SupplyDropPlaneCreated = hooksClass.GetMethod("SupplyDropPlaneCreated");
            iLProcessor = SupplyDropPlaneConstructor.Body.GetILProcessor();
            iLProcessor.InsertBefore(SupplyDropPlaneConstructor.Body.Instructions[Position],
                Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(SupplyDropPlaneCreated)));
            iLProcessor.InsertBefore(SupplyDropPlaneConstructor.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_0));
        }

        private void ClientConnectionPatch()
        {
            TypeDefinition ClientConnection = rustAssembly.MainModule.GetType("ClientConnection");
            MethodDefinition DenyAccess = ClientConnection.GetMethod("DenyAccess");
            ILProcessor iLProcessor = DenyAccess.Body.GetILProcessor();
            this.CloneMethod(DenyAccess);
            MethodDefinition method = hooksClass.GetMethod("SteamDeny");
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void ConnectionAcceptorPatch()
        {
            TypeDefinition ConnectionAcceptor = rustAssembly.MainModule.GetType("ConnectionAcceptor");
            MethodDefinition uLink_OnPlayerApproval = ConnectionAcceptor.GetMethod("uLink_OnPlayerApproval");
            ILProcessor iLProcessor = uLink_OnPlayerApproval.Body.GetILProcessor();
            this.CloneMethod(uLink_OnPlayerApproval);
            MethodDefinition method = hooksClass.GetMethod("PlayerApproval");
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void SupplyCratePatch()
        {
            TypeDefinition SupplyCrate = rustAssembly.MainModule.GetType("SupplyCrate");
            MethodDefinition FixedUpdate = SupplyCrate.GetMethod("FixedUpdate");
            MethodDefinition DoNetwork = SupplyCrate.GetMethod("DoNetwork");

            TypeDefinition logger = fougeriteAssembly.MainModule.GetType("Fougerite.Logger");
            MethodDefinition logex = logger.GetMethod("LogException");
            WrapMethod(FixedUpdate, logex, rustAssembly, false);
            WrapMethod(DoNetwork, logex, rustAssembly, false);
        }

        private void BootstrapAttachPatch()
        {
            TypeDefinition fougeriteBootstrap = fougeriteAssembly.MainModule.GetType("Fougerite.Bootstrap");
            TypeDefinition serverInit = rustAssembly.MainModule.GetType("ServerInit");
            MethodDefinition attachBootstrap = fougeriteBootstrap.GetMethod("AttachBootstrap");
            MethodDefinition awake = serverInit.GetMethod("Awake");
            awake.Body.GetILProcessor().InsertAfter(awake.Body.Instructions[0x74], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(attachBootstrap)));
        }

        private void EntityDecayPatch_StructureMaster()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("StructureMaster");
            MethodDefinition orig = type.GetMethod("DoDecay");
            MethodDefinition method = hooksClass.GetMethod("EntityDecay");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor();
            iLProcessor.InsertBefore(orig.Body.Instructions[244], Instruction.Create(OpCodes.Stloc_S, orig.Body.Variables[6]));
            iLProcessor.InsertBefore(orig.Body.Instructions[244], Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[244], Instruction.Create(OpCodes.Ldloc_S, orig.Body.Variables[6]));
            iLProcessor.InsertBefore(orig.Body.Instructions[244], Instruction.Create(OpCodes.Ldloc_3));
        }

        private void EntityDecayPatch_EnvDecay()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("EnvDecay");
            MethodDefinition orig = type.GetMethod("DecayThink");
            MethodDefinition method = hooksClass.GetMethod("EntityDecay");
            FieldDefinition Field = type.GetField("_deployable");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor();
            iLProcessor.InsertBefore(orig.Body.Instructions[49], Instruction.Create(OpCodes.Stloc_S, orig.Body.Variables[2]));
            iLProcessor.InsertBefore(orig.Body.Instructions[49], Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[49], Instruction.Create(OpCodes.Ldloc_S, orig.Body.Variables[2]));
            iLProcessor.InsertBefore(orig.Body.Instructions[49], Instruction.Create(OpCodes.Ldfld, Field));
            iLProcessor.InsertBefore(orig.Body.Instructions[49], Instruction.Create(OpCodes.Ldarg_0));
        }

        private void PlayerSpawningSpawnedPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("ServerManagement");
            MethodDefinition orig = type.GetMethod("SpawnPlayer");
            MethodDefinition method = hooksClass.GetMethod("PlayerSpawning");
            MethodDefinition SpawnedHook = hooksClass.GetMethod("PlayerSpawned");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor();

            // 141 - playerFor.hasLastKnownPosition = true;
            int Position = orig.Body.Instructions.Count - 2;
            iLProcessor.InsertBefore(orig.Body.Instructions[Position], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(SpawnedHook)));
            iLProcessor.InsertBefore(orig.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.InsertBefore(orig.Body.Instructions[Position], Instruction.Create(OpCodes.Ldloc_0));
            iLProcessor.InsertBefore(orig.Body.Instructions[Position], Instruction.Create(OpCodes.Ldarg_1));

            // 114 - user.truthDetector.NoteTeleported(zero, 0.0);
            iLProcessor.InsertBefore(orig.Body.Instructions[114], Instruction.Create(OpCodes.Stloc_0));
            iLProcessor.InsertBefore(orig.Body.Instructions[114], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[114], Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.InsertBefore(orig.Body.Instructions[114], Instruction.Create(OpCodes.Ldloc_0));
            iLProcessor.InsertBefore(orig.Body.Instructions[114], Instruction.Create(OpCodes.Ldarg_1));
        }

        private void ServerShutdownPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("LibRust");
            MethodDefinition orig = type.GetMethod("OnDestroy");
            MethodDefinition method = hooksClass.GetMethod("ServerShutdown");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor(); // 5 - Shutdown();
            iLProcessor.InsertBefore(orig.Body.Instructions[5], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
        }

        private void PlayerGatherWoodPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("MeleeWeaponDataBlock");
            MethodDefinition orig = type.GetMethod("DoAction1");
            MethodDefinition method = hooksClass.GetMethod("PlayerGatherWood");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor(); // 184 - if (byName != null)
            iLProcessor.InsertBefore(orig.Body.Instructions[184], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[184], Instruction.Create(OpCodes.Ldloca_S, orig.Body.Variables[16]));
            iLProcessor.InsertBefore(orig.Body.Instructions[184], Instruction.Create(OpCodes.Ldloca_S, orig.Body.Variables[14]));
            iLProcessor.InsertBefore(orig.Body.Instructions[184], Instruction.Create(OpCodes.Ldloca_S, orig.Body.Variables[17]));
            iLProcessor.InsertBefore(orig.Body.Instructions[184], Instruction.Create(OpCodes.Ldloc_S, orig.Body.Variables[11]));
            iLProcessor.InsertBefore(orig.Body.Instructions[184], Instruction.Create(OpCodes.Ldloc_S, orig.Body.Variables[5]));
        }

        private void PlayerGatherPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("ResourceTarget");
            MethodDefinition orig = type.GetMethod("DoGather");
            MethodDefinition method = hooksClass.GetMethod("PlayerGather");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor(); // 30 - int amount = (int) Mathf.Abs(this.gatherProgress);
            iLProcessor.InsertBefore(orig.Body.Instructions[30], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[30], Instruction.Create(OpCodes.Ldloca, orig.Body.Variables[1]));
            iLProcessor.InsertBefore(orig.Body.Instructions[30], Instruction.Create(OpCodes.Ldloc_0));
            iLProcessor.InsertBefore(orig.Body.Instructions[30], Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.InsertBefore(orig.Body.Instructions[30], Instruction.Create(OpCodes.Ldarg_1));
        }

        private void EntityDeployedPatch_DeployableItemDataBlock()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("DeployableItemDataBlock");
            type.GetMethod("SetupDeployableObject").SetPublic(true);
            MethodDefinition orig = type.GetMethod("DoAction1");
            MethodDefinition method = hooksClass.GetMethod("EntityDeployed");
            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor(); // 60 - leave (end of try block)
            iLProcessor.InsertBefore(orig.Body.Instructions[60], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[60], Instruction.Create(OpCodes.Ldarg_S, orig.Parameters[2]));
            iLProcessor.InsertBefore(orig.Body.Instructions[60], Instruction.Create(OpCodes.Ldloc_S, orig.Body.Variables[8]));
            
            MethodDefinition DeployableCheckHook = hooksClass.GetMethod("DeployableCheckHook");
            MethodDefinition CheckPlacement = type.GetMethod("CheckPlacement");
            ILProcessor iLProcessor2 = CheckPlacement.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_S, CheckPlacement.Parameters[3]));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(DeployableCheckHook)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void EntityDeployedPatch_StructureComponentDataBlock()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("StructureComponentDataBlock");
            MethodDefinition orig = type.GetMethod("DoAction1");
            MethodDefinition method = hooksClass.GetMethod("EntityDeployed");
            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor(); // 102 - int count = 1;
            iLProcessor.InsertBefore(orig.Body.Instructions[102], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[102], Instruction.Create(OpCodes.Ldarg_S, orig.Parameters[2]));
            iLProcessor.InsertBefore(orig.Body.Instructions[102], Instruction.Create(OpCodes.Ldloc_S, orig.Body.Variables[8]));
        }

        private void BlueprintUsePatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("BlueprintDataBlock");
            MethodDefinition orig = type.GetMethod("UseItem");
            MethodDefinition method = hooksClass.GetMethod("BlueprintUse");

            this.CloneMethod(orig);
            orig.Body.Instructions.Clear();
            orig.Body.ExceptionHandlers.Clear();
            orig.Body.Variables.Clear();
            orig.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            orig.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            orig.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            orig.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void ChatPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("chat");
            MethodDefinition orig = type.GetMethod("say");
            MethodDefinition method = hooksClass.GetMethod("ChatReceived");

            this.CloneMethod(orig);
            orig.Body.Instructions.Clear();
            orig.Body.ExceptionHandlers.Clear();
            orig.Body.Variables.Clear();
            orig.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            orig.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            orig.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private MethodDefinition CloneMethod(MethodDefinition orig) // Method Backuping
        {
            MethodDefinition definition = new MethodDefinition($"{orig.Name}Original", orig.Attributes, orig.ReturnType);
            foreach (VariableDefinition definition2 in orig.Body.Variables)
            {
                definition.Body.Variables.Add(definition2);
            }
            foreach (ParameterDefinition definition3 in orig.Parameters)
            {
                definition.Parameters.Add(definition3);
            }
            foreach (Instruction instruction in orig.Body.Instructions)
            {
                definition.Body.Instructions.Add(instruction);
            }
            return definition;
        }

        private void ConsolePatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("ConsoleSystem");
            MethodDefinition orig = type.GetMethod("RunCommand");
            MethodDefinition method = hooksClass.GetMethod("HandleRunCommand");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            /*for (int i = 0; i < 8; i++)
            {
                iLProcessor.Remove(orig.Body.Instructions[11]);
            }
            iLProcessor.InsertBefore(orig.Body.Instructions[11], Instruction.Create(OpCodes.Ret));
            iLProcessor.InsertBefore(orig.Body.Instructions[11], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[11], Instruction.Create(OpCodes.Ldarg_0));*/
        }

        private void DoorSharing()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("DeployableObject");
            MethodDefinition definition2 = type.GetMethod("BelongsTo");
            MethodDefinition method = hooksClass.GetMethod("CheckOwner");

            definition2.Body.Instructions.Clear();
            definition2.Body.ExceptionHandlers.Clear();
            definition2.Body.Variables.Clear();
            definition2.Body.Instructions.Add(Instruction.Create(OpCodes.Nop));
            definition2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            definition2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            definition2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            definition2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void EntityHurtPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("TakeDamage");
            type.GetField("takenodamage").SetPublic(true);
            type.GetField("_health").SetPublic(true);

            MethodDefinition EntityHurt2 = hooksClass.GetMethod("EntityHurt2");
            MethodDefinition ProcessDamageEvent = type.GetMethod("ProcessDamageEvent");
            ProcessDamageEvent.Body.Instructions.Clear();
            ProcessDamageEvent.Body.ExceptionHandlers.Clear();
            ProcessDamageEvent.Body.Variables.Clear();
            ProcessDamageEvent.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            ProcessDamageEvent.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            ProcessDamageEvent.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(EntityHurt2)));
            ProcessDamageEvent.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            //ProcessDamageEvent
            /*TypeDefinition type = rustAssembly.MainModule.GetType("StructureComponent");
            TypeDefinition definition2 = rustAssembly.MainModule.GetType("DeployableObject");
            MethodDefinition definition3 = type.GetMethod("OnHurt");
            MethodDefinition definition4 = definition2.GetMethod("OnHurt");
            MethodDefinition method = hooksClass.GetMethod("EntityHurt");

            MethodReference reference = rustAssembly.MainModule.Import(method);
            definition3.Body.Instructions.Clear();
            definition3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            definition3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarga_S, definition3.Parameters[0]));
            definition3.Body.Instructions.Add(Instruction.Create(OpCodes.Call, reference));
            definition3.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            definition4.Body.Instructions.Clear();
            definition4.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            definition4.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarga_S, definition4.Parameters[0]));
            definition4.Body.Instructions.Add(Instruction.Create(OpCodes.Call, reference));
            definition4.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));*/
        }

        private void ItemsTablesLoadedPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("DatablockDictionary");
            MethodDefinition orig = type.GetMethod("Initialize");
            MethodDefinition method = hooksClass.GetMethod("ItemsLoaded");
            MethodDefinition definition4 = hooksClass.GetMethod("TablesLoaded");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor();
            for (int i = 0; i < 13; i++)
            {
                iLProcessor.Remove(orig.Body.Instructions[0x17]);
            }
            orig.Body.Instructions[0x24] = Instruction.Create(OpCodes.Callvirt, rustAssembly.MainModule.Import(method));
            iLProcessor.InsertBefore(orig.Body.Instructions[0x24], Instruction.Create(OpCodes.Ldsfld, type.Fields[2]));
            iLProcessor.InsertBefore(orig.Body.Instructions[0x24], Instruction.Create(OpCodes.Ldsfld, type.Fields[1]));
            iLProcessor.InsertBefore(orig.Body.Instructions[0x3f], Instruction.Create(OpCodes.Stsfld, type.Fields[4]));
            iLProcessor.InsertBefore(orig.Body.Instructions[0x3f], Instruction.Create(OpCodes.Callvirt, rustAssembly.MainModule.Import(definition4)));
            iLProcessor.InsertBefore(orig.Body.Instructions[0x3f], Instruction.Create(OpCodes.Ldsfld, type.Fields[4]));
        }

        private void PlayerJoinLeavePatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("ServerManagement");
            TypeDefinition definition2 = rustAssembly.MainModule.GetType("ConnectionAcceptor");
            MethodDefinition orig = type.GetMethod("OnUserConnected");
            //MethodDefinition method = hooksClass.GetMethod("PlayerConnect");
            MethodDefinition method = hooksClass.GetMethod("ConnectHandler");
            MethodDefinition definition5 = definition2.GetMethod("uLink_OnPlayerDisconnected");
            MethodDefinition definition6 = hooksClass.GetMethod("PlayerDisconnect");

            this.CloneMethod(orig);
            this.CloneMethod(definition5);
            ILProcessor iLProcessor = orig.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            //ConnectHandler
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            //iLProcessor.InsertBefore(orig.Body.Instructions[0], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            //iLProcessor.InsertBefore(orig.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_1));
            /*iLProcessor.InsertBefore(orig.Body.Instructions[0], Instruction.Create(OpCodes.Brfalse, orig.Body.Instructions[orig.Body.Instructions.Count - 1]));
            iLProcessor.InsertBefore(orig.Body.Instructions[0], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(orig.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_1));*/
            iLProcessor = definition5.Body.GetILProcessor();
            iLProcessor.InsertBefore(definition5.Body.Instructions[0], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(definition6)));
            iLProcessor.InsertBefore(definition5.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_1));
            //iLProcessor.InsertAfter(definition5.Body.Instructions[0x23], Instruction.Create(OpCodes.Ldloc_1));
            //iLProcessor.InsertAfter(definition5.Body.Instructions[0x24], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(definition6)));
        }

        private void ServerSavePatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("ServerSaveManager");
            rustAssembly.MainModule.GetType("save").IsPublic = true;
            //MethodDefinition method = hooksClass.GetMethod("ServerSaved");

            type.GetField("_loadedOnce").SetPublic(true);
            type.GetField("_loading").SetPublic(true);
            type.GetField("keys").SetPublic(true);
            type.GetField("values").SetPublic(true);
            type.GetMethod("DateTimeFileString").SetPublic(true);
            type.GetMethod("Get").SetPublic(true);
            type.GetMethod("DoSave").SetPublic(true);
            type.GetMethod("DateTimeFileString").SetPublic(true);

            //MethodDefinition AutoSave = type.GetMethod("AutoSave");
            //MethodReference import = this.rustAssembly.MainModule.Import(method);
            //ILProcessor iLProcessor = AutoSave.Body.GetILProcessor();
            //iLProcessor.Body.Instructions.Clear();
            //iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, import));
            //iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            MethodDefinition GlobalQuit = hooksClass.GetMethod("GlobalQuit");
            TypeDefinition global = rustAssembly.MainModule.GetType("global");
            ILProcessor iLProcessor2 = global.GetMethod("quit").Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(GlobalQuit)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            TypeDefinition Instances = type.GetNestedType("Instances");
            Instances.IsPublic = true;
            Instances.GetField("registers").SetPublic(true);
            Instances.GetField("ordered").SetPublic(true);
            
            MethodDefinition RegisterHook = hooksClass.GetMethod("RegisterHook");
            MethodDefinition UnRegisterHook = hooksClass.GetMethod("UnRegisterHook");
            
            ILProcessor iLProcessor3 = Instances.GetMethod("Register").Body.GetILProcessor();
            iLProcessor3.Body.Instructions.Clear();
            iLProcessor3.Body.ExceptionHandlers.Clear();
            iLProcessor3.Body.Variables.Clear();
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(RegisterHook)));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor4 = Instances.GetMethod("Unregister").Body.GetILProcessor();
            iLProcessor4.Body.Instructions.Clear();
            iLProcessor4.Body.ExceptionHandlers.Clear();
            iLProcessor4.Body.Variables.Clear();
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(UnRegisterHook)));
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor5 = type.GetMethod("Save").Body.GetILProcessor();
            iLProcessor5.Body.Instructions.Clear();
            iLProcessor5.Body.ExceptionHandlers.Clear();
            iLProcessor5.Body.Variables.Clear();
            iLProcessor5.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            TypeDefinition ServerSave = rustAssembly.MainModule.GetType("ServerSave");
            ServerSave.GetNestedType("Reged").IsPublic = true;
            ServerSave.GetProperty("REGED").GetMethod.SetPublic(true);

            //MethodDefinition ActualAutoSave = hooksClass.GetMethod("ActualAutoSave");
            //ILProcessor iLProcessor3 = type.GetMethod("Save").Body.GetILProcessor();
            //iLProcessor3.Body.Instructions.Clear();
            //iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, this.rustAssembly.MainModule.Import(ActualAutoSave)));
            //iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void PlayerKilledPatch()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("HumanController");
            MethodDefinition orig = type.GetMethod("OnKilled");
            MethodDefinition method = hooksClass.GetMethod("PlayerKilled");

            this.CloneMethod(orig);
            ILProcessor iLProcessor = orig.Body.GetILProcessor();
            iLProcessor.InsertAfter(orig.Body.Instructions[0x15], Instruction.Create(OpCodes.Ldarga_S, orig.Parameters[0]));
            iLProcessor.InsertAfter(orig.Body.Instructions[0x16], Instruction.Create(OpCodes.Callvirt, rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertAfter(orig.Body.Instructions[0x17], Instruction.Create(OpCodes.Brfalse, orig.Body.Instructions[0x2f]));
            orig.Body.Instructions[0x11] = Instruction.Create(OpCodes.Brfalse, orig.Body.Instructions[0x16]);
        }

        private void PatchuLink(ref AssemblyDefinition ulink)
        {
            TypeDefinition Class0 = ulink.MainModule.GetType("Class0");
            TypeDefinition Class8 = ulink.MainModule.GetType("Class8");
            TypeDefinition Class33 = ulink.MainModule.GetType("Class33");
            TypeDefinition Class35 = ulink.MainModule.GetType("Class35");
            TypeDefinition Class55 = ulink.MainModule.GetType("Class55");
            TypeDefinition Class71 = ulink.MainModule.GetType("Class71");
            TypeDefinition Enum13 = ulink.MainModule.GetType("Enum13");
            TypeDefinition Exception0 = ulink.MainModule.GetType("Exception0");
            MethodDefinition uLinkCatch17 = hooksClass.GetMethod("uLinkCatch17");
            MethodDefinition uLinkCatch41 = hooksClass.GetMethod("uLinkCatch41");
            
            Enum13.IsPublic = true;
            Exception0.IsPublic = true;
            Class71.IsPublic = true;
            Class55.IsPublic = true;
            Class8.IsPublic = true;

            Class0.GetField("class18_0").SetPublic(true);
            Class0.GetField("class8_0").SetPublic(true);
            Class0.GetField("socket_0").SetPublic(true);
            Class0.GetMethod("vmethod_2").SetPublic(true);

            Class0.IsPublic = true;
            Class0.GetField("endPoint_0").SetPublic(true);

            Class8.GetMethod("method_11").SetPublic(true);
            
            Class33.GetField("enum8_0").SetPublic(true);
            Class35.GetMethod("method_0").SetPublic(true);
            
            Class55.GetMethod("method_25").SetPublic(true);
            Class55.GetMethod("method_26").SetPublic(true);

            MethodDefinition method_17 = Class0.GetMethod("method_17");

            ILProcessor iLProcessor = method_17.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(uLinkCatch17)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));

            MethodDefinition method_41 = Class0.GetMethod("method_41");

            ILProcessor iLProcessor2 = method_41.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Callvirt, ulink.MainModule.Import(uLinkCatch41)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void TalkerNotifications()
        {
            TypeDefinition type = rustAssembly.MainModule.GetType("VoiceCom");
            type.IsSealed = false;
            MethodDefinition method2 = hooksClass.GetMethod("ShowTalker");

            MethodDefinition clientspeak = type.GetMethod("clientspeak");
            MethodDefinition method = hooksClass.GetMethod("ConfirmVoice");
            
            type.GetField("playerList").SetPublic(true);

            /*ILProcessor iLProcessor = clientspeak.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));*/

            int i = 0;
            ILProcessor iLProcessor = clientspeak.Body.GetILProcessor();
            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i], Instruction.Create(OpCodes.Ret));
            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i], Instruction.Create(OpCodes.Brtrue_S, clientspeak.Body.Instructions[1]));
            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i], Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i], Instruction.Create(OpCodes.Ldarg_0));


            /*ILProcessor iLProcessor = clientspeak.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));*/


            VariableDefinition variable1 = clientspeak.Body.Variables[0];
            VariableDefinition variable3 = clientspeak.Body.Variables[6];

            int i2 = clientspeak.Body.Instructions.Count - 42;

            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i2], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method2)));
            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i2], Instruction.Create(OpCodes.Ldloc_S, variable1));
            iLProcessor.InsertBefore(clientspeak.Body.Instructions[i2], Instruction.Create(OpCodes.Ldloc_S, variable3));

            /*MethodDefinition method2 = hooksClass.GetMethod("ShowTalker");
            FieldDefinition field = definition2.GetField("netPlayer");
            VariableDefinition variable = null;
            variable = clientspeak.Body.Variables[6];

            iLProcessor.InsertAfter(clientspeak.Body.Instructions[0x57], Instruction.Create(OpCodes.Ldloc_S, variable));
            iLProcessor.InsertAfter(clientspeak.Body.Instructions[0x58], Instruction.Create(OpCodes.Ldfld, rustAssembly.MainModule.Import(field)));
            iLProcessor.InsertAfter(clientspeak.Body.Instructions[0x59], Instruction.Create(OpCodes.Ldloc_0));
            iLProcessor.InsertAfter(clientspeak.Body.Instructions[90], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method2)));*/
        }

        private void ConditionDebug()
        {
            TypeDefinition Controllable = rustAssembly.MainModule.GetType("Controllable");
            Controllable.GetMethod("Disconnect").SetPublic(true);
            Controllable.GetNestedType("Chain").IsPublic = true;
            Controllable.GetField("localPlayerControllableCount").SetPublic(true);
            //Controllable.GetField("bt").SetPublic(true);
            Controllable.GetField("ch").SetPublic(true);
            Controllable.GetField("_sentRootControlCount").SetPublic(true);
            Controllable.GetField("RT").SetPublic(true);
            Controllable.GetField("kClientSideRootNumberRPCName").IsPublic = true;
            Controllable.GetField("kClientSideRootNumberRPCName").SetPublic(true);

            Controllable.GetMethod("OverrideControlOfHandleRPC").SetPublic(true);
            Controllable.GetMethod("SharedPostCLR").SetPublic(true);
            
            
            MethodDefinition OC1 = Controllable.GetMethod("OC1");
            MethodDefinition OC2 = Controllable.GetMethod("OC2");
            MethodDefinition CLR = Controllable.GetMethod("CLR");
            MethodDefinition CLD = Controllable.GetMethod("CLD");
            
            MethodDefinition OC1Hook = hooksClass.GetMethod("OC1Hook");
            MethodDefinition OC2Hook = hooksClass.GetMethod("OC2Hook");
            MethodDefinition CLRHook = hooksClass.GetMethod("CLRHook");
            MethodDefinition CLDHook = hooksClass.GetMethod("CLDHook");
            
            CLD.Body.Instructions.Clear();
            CLD.Body.ExceptionHandlers.Clear();
            CLD.Body.Variables.Clear();
            CLD.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            CLD.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            CLD.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(CLDHook)));
            CLD.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            
            CLR.Body.Instructions.Clear();
            CLR.Body.ExceptionHandlers.Clear();
            CLR.Body.Variables.Clear();
            CLR.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            CLR.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            CLR.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(CLRHook)));
            CLR.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            OC1.Body.Instructions.Clear();
            OC1.Body.ExceptionHandlers.Clear();
            OC1.Body.Variables.Clear();
            OC1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            OC1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            OC1.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            OC1.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(OC1Hook)));
            OC1.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            OC2.Body.Instructions.Clear();
            OC2.Body.ExceptionHandlers.Clear();
            OC2.Body.Variables.Clear();
            OC2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            OC2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            OC2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            OC2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            OC2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(OC2Hook)));
            OC2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void NetcullPatch()
        {
            TypeDefinition NetCull = rustAssembly.MainModule.GetType("NetCull");
            TypeDefinition NGC = rustAssembly.MainModule.GetType("NGC");
            TypeDefinition CullGrid = rustAssembly.MainModule.GetType("CullGrid");
            TypeDefinition NetworkCullInfo = rustAssembly.MainModule.GetType("NetworkCullInfo");
            TypeDefinition NetInstance = rustAssembly.MainModule.GetType("NetInstance");
            TypeDefinition NGCGlobal = NGC.GetNestedType("Global");
            NGCGlobal.IsPublic = true;
             
            MethodDefinition CloseConnection = NetCull.GetMethod("CloseConnection");
            TypeDefinition logger = fougeriteAssembly.MainModule.GetType("Fougerite.Logger");
            MethodDefinition logex = logger.GetMethod("LogException");

            WrapMethod(CloseConnection, logex, rustAssembly, false);

            NetCull.GetMethod("RegisterCullInfo").SetPublic(true);
            NetCull.GetMethod("ShutdownNetworkCullInfoAndDestroy").SetPublic(true);
            CullGrid.GetMethod("RegisterPlayerRootNetworkCullInfo").SetPublic(true);
            CullGrid.GetMethod("RegisterPlayerNonRootNetworkCullInfo").SetPublic(true);
            NetworkCullInfo.GetMethod("OnInitialRegistrationComplete").SetPublic(true);

            foreach (var x in NetInstance.GetMethods())
            {
                if (x.Name == "PreServerDestroy")
                {
                    x.SetPublic(true);
                }
            }

            MethodDefinition Instantiated = NetCull.GetMethod("Instantiated");
            
            MethodDefinition InstantiatedHook = hooksClass.GetMethod("Instantiated");
            MethodDefinition DestroyByViewHook = hooksClass.GetMethod("DestroyByView");
            MethodDefinition DestroyByNetworkIdHook = hooksClass.GetMethod("DestroyByNetworkId");
            MethodDefinition DestroyByGameObjectHook = hooksClass.GetMethod("DestroyByGameObject");
            MethodDefinition InstantiateNGCHook = hooksClass.GetMethod("InstantiateNGC");

            MethodDefinition DestroyByView = null;
            MethodDefinition DestroyByNetworkId = null;
            MethodDefinition DestroyByGameObject = null;
            MethodDefinition InstantiateNGC = null;
            
            foreach (var x in NGC.GetMethods())
            {
                if (x.Name != "Instantiate") continue;

                if (x.Parameters[0].ParameterType.Name.Contains("InstantiateArgs"))
                {
                    InstantiateNGC = x;
                    break;
                }
            }

            foreach (var x in NetCull.GetMethods())
            {
                if (x.Name != "Destroy") continue;
                
                switch (x.Parameters[0].ParameterType.Name)
                {
                    case "NetworkView":
                        DestroyByView = x;
                        break;
                    case "NetworkViewID":
                        DestroyByNetworkId = x;
                        break;
                    case "GameObject":
                        DestroyByGameObject = x;
                        break;
                }
            }
            
            InstantiateNGC.Body.Instructions.Clear();
            InstantiateNGC.Body.ExceptionHandlers.Clear();
            InstantiateNGC.Body.Variables.Clear();
            InstantiateNGC.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            InstantiateNGC.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            InstantiateNGC.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(InstantiateNGCHook)));
            InstantiateNGC.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            DestroyByView.Body.Instructions.Clear();
            DestroyByView.Body.ExceptionHandlers.Clear();
            DestroyByView.Body.Variables.Clear();
            DestroyByView.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            DestroyByView.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(DestroyByViewHook)));
            DestroyByView.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            DestroyByNetworkId.Body.Instructions.Clear();
            DestroyByNetworkId.Body.ExceptionHandlers.Clear();
            DestroyByNetworkId.Body.Variables.Clear();
            DestroyByNetworkId.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            DestroyByNetworkId.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(DestroyByNetworkIdHook)));
            DestroyByNetworkId.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            DestroyByGameObject.Body.Instructions.Clear();
            DestroyByGameObject.Body.ExceptionHandlers.Clear();
            DestroyByGameObject.Body.Variables.Clear();
            DestroyByGameObject.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            DestroyByGameObject.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(DestroyByGameObjectHook)));
            DestroyByGameObject.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            Instantiated.Body.Instructions.Clear();
            Instantiated.Body.ExceptionHandlers.Clear();
            Instantiated.Body.Variables.Clear();
            Instantiated.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            Instantiated.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            Instantiated.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            Instantiated.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            Instantiated.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(InstantiatedHook)));
            Instantiated.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void BowShootPatch()
        {
            TypeDefinition BowWeaponDataBlock = rustAssembly.MainModule.GetType("BowWeaponDataBlock");
            MethodDefinition DoAction1 = BowWeaponDataBlock.GetMethod("DoAction1");

            MethodDefinition method = hooksClass.GetMethod("BowShootEvent");

            int i = DoAction1.Body.Instructions.Count - 1;
            ILProcessor iLProcessor = DoAction1.Body.GetILProcessor();
            iLProcessor.InsertBefore(DoAction1.Body.Instructions[i], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(DoAction1.Body.Instructions[i], Instruction.Create(OpCodes.Ldloc_0));
            iLProcessor.InsertBefore(DoAction1.Body.Instructions[i], Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor.InsertBefore(DoAction1.Body.Instructions[i], Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.InsertBefore(DoAction1.Body.Instructions[i], Instruction.Create(OpCodes.Ldarg_0));
        }

        private void DecayStopPatch()
        {
            TypeDefinition EnvDecay = rustAssembly.MainModule.GetType("EnvDecay");
            TypeDefinition CallBacks = EnvDecay.GetNestedType("Callbacks");
            CallBacks.IsPublic = true;
            CallBacks.GetMethod("RunDecayThink").SetPublic(true);
            IEnumerable<MethodDefinition> Consts = CallBacks.GetConstructors();
            MethodDefinition md = Consts.ToArray()[0];
            md.SetPublic(true);
            md.Body.Instructions.Clear();
            md.Body.ExceptionHandlers.Clear();
            md.Body.Variables.Clear();
            md.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            TypeDefinition StructureMaster = rustAssembly.MainModule.GetType("StructureMaster");
            TypeDefinition Callbacks = StructureMaster.GetNestedType("Callbacks");
            Callbacks.IsPublic = true;
            MethodDefinition RunDecayThink = Callbacks.GetMethod("RunDecayThink");
            RunDecayThink.SetPublic(true);
            MethodDefinition constructor = Callbacks.GetStaticConstructor();
            constructor.Body.Instructions.Clear();
            constructor.Body.ExceptionHandlers.Clear();
            constructor.Body.Variables.Clear();
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void GenericSpawnerPatch()
        {
            TypeDefinition GenericSpawner = rustAssembly.MainModule.GetType("GenericSpawner");
            GenericSpawner.GetField("spawnStagger").SetPublic(true);
            MethodDefinition OnServerLoad = GenericSpawner.GetMethod("OnServerLoad");

            MethodDefinition method = hooksClass.GetMethod("GenericHook");
            
            int i = OnServerLoad.Body.Instructions.Count - 16;
            ILProcessor iLProcessor = OnServerLoad.Body.GetILProcessor();
            iLProcessor.InsertBefore(OnServerLoad.Body.Instructions[i], Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(method)));
            iLProcessor.InsertBefore(OnServerLoad.Body.Instructions[i], Instruction.Create(OpCodes.Ldarg_0));
        }

        private void ServerLoadedPatch()
        {
            TypeDefinition ServerInit = rustAssembly.MainModule.GetType("ServerInit");
            MethodDefinition LoadLevel = ServerInit.GetMethod("LoadLevel");

            MethodDefinition method = hooksClass.GetMethod("ServerLoadedHook");

            ILProcessor iLProcessor = LoadLevel.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void BeltPatch()
        {
            TypeDefinition InventoryHolder = rustAssembly.MainModule.GetType("InventoryHolder");
            InventoryHolder.GetMethod("ValidateAntiBeltSpam").SetPublic(true);
            MethodDefinition DoBeltUse = InventoryHolder.GetMethod("DoBeltUse");

            MethodDefinition method = hooksClass.GetMethod("DoBeltUseHook");

            ILProcessor iLProcessor = DoBeltUse.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void TossPatch()
        {
            TypeDefinition InventoryHolder = rustAssembly.MainModule.GetType("InventoryHolder");
            MethodDefinition TOSS = InventoryHolder.GetMethod("TOSS");

            MethodDefinition method = hooksClass.GetMethod("TossBypass");

            ILProcessor iLProcessor = TOSS.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void SupplySignalExplosion()
        {
            TypeDefinition SignalGrenade = rustAssembly.MainModule.GetType("SignalGrenade");
            SignalGrenade.GetField("fuseLength").SetPublic(true);
            
            MethodDefinition OnDone = SignalGrenade.GetMethod("OnDone");

            MethodDefinition method = hooksClass.GetMethod("OnSupplySignalExplosion");

            ILProcessor iLProcessor = OnDone.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(method)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void ItemRepresentation()
        {
            TypeDefinition ItemRepresentation = rustAssembly.MainModule.GetType("ItemRepresentation");
            ItemRepresentation.GetMethod("Action1").SetPublic(true);
            ItemRepresentation.GetMethod("Action1B").SetPublic(true);
            ItemRepresentation.GetMethod("RunServerAction").SetPublic(true);
            
            MethodDefinition Action1Hook = hooksClass.GetMethod("Action1Hook");
            MethodDefinition Action1BHook = hooksClass.GetMethod("Action1BHook");
            
            
            MethodDefinition Action1 = ItemRepresentation.GetMethod("Action1");
            MethodDefinition Action1B = ItemRepresentation.GetMethod("Action1B");
            
            ILProcessor iLProcessor = Action1.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(Action1Hook)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor2 = Action1B.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(Action1BHook)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void DoAction1Patch()
        {
            TypeDefinition BulletWeaponDataBlock = rustAssembly.MainModule.GetType("BulletWeaponDataBlock");
            TypeDefinition ShotgunDataBlock = rustAssembly.MainModule.GetType("ShotgunDataBlock");
            TypeDefinition HandGrenadeDataBlock = rustAssembly.MainModule.GetType("HandGrenadeDataBlock");
            TypeDefinition StructureComponentDataBlock = rustAssembly.MainModule.GetType("StructureComponentDataBlock");
            TypeDefinition TorchItemDataBlock = rustAssembly.MainModule.GetType("TorchItemDataBlock");
            
            BulletWeaponDataBlock.GetMethod("ReadHitInfo").SetPublic(true);
            BulletWeaponDataBlock.GetMethod("ApplyDamage").SetPublic(true);


            StructureComponentDataBlock.GetField("_structureToPlace").SetPublic(true);

            MethodDefinition BulletWeaponDataBlockDoAction1 = BulletWeaponDataBlock.GetMethod("DoAction1");
            MethodDefinition ShotgunDataBlockDoAction1 = ShotgunDataBlock.GetMethod("DoAction1");
            MethodDefinition HandGrenadeDataBlockDoAction1 = HandGrenadeDataBlock.GetMethod("DoAction1");
            MethodDefinition StructureComponentDataBlockDoAction1 = StructureComponentDataBlock.GetMethod("DoAction1");
            MethodDefinition TorchItemDataBlockDoAction1 = TorchItemDataBlock.GetMethod("DoAction1");

            MethodDefinition BulletWeaponDoAction1 = hooksClass.GetMethod("BulletWeaponDoAction1");
            MethodDefinition ShotgunDoAction1 = hooksClass.GetMethod("ShotgunDoAction1");
            MethodDefinition HandGrenadeDoAction1 = hooksClass.GetMethod("HandGrenadeDoAction1");
            MethodDefinition StructureComponentDoAction1 = hooksClass.GetMethod("StructureComponentDoAction1");
            MethodDefinition TorchDoAction1 = hooksClass.GetMethod("TorchDoAction1");
                
            /*ILProcessor iLProcessor = DeployableItemDataBlockDoAction1.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(DeployableItemDoAction1)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));*/
            
            ILProcessor iLProcessor2 = BulletWeaponDataBlockDoAction1.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(BulletWeaponDoAction1)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor3 = ShotgunDataBlockDoAction1.Body.GetILProcessor();
            iLProcessor3.Body.Instructions.Clear();
            iLProcessor3.Body.ExceptionHandlers.Clear();
            iLProcessor3.Body.Variables.Clear();
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(ShotgunDoAction1)));
            iLProcessor3.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor4 = HandGrenadeDataBlockDoAction1.Body.GetILProcessor();
            iLProcessor4.Body.Instructions.Clear();
            iLProcessor4.Body.ExceptionHandlers.Clear();
            iLProcessor4.Body.Variables.Clear();
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(HandGrenadeDoAction1)));
            iLProcessor4.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor5 = StructureComponentDataBlockDoAction1.Body.GetILProcessor();
            iLProcessor5.Body.Instructions.Clear();
            iLProcessor5.Body.ExceptionHandlers.Clear();
            iLProcessor5.Body.Variables.Clear();
            iLProcessor5.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor5.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor5.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor5.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor5.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(StructureComponentDoAction1)));
            iLProcessor5.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor6 = TorchItemDataBlockDoAction1.Body.GetILProcessor();
            iLProcessor6.Body.Instructions.Clear();
            iLProcessor6.Body.ExceptionHandlers.Clear();
            iLProcessor6.Body.Variables.Clear();
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(TorchDoAction1)));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void NGCPatch()
        {
            TypeDefinition NGC = rustAssembly.MainModule.GetType("NGC");
            TypeDefinition NGCView = rustAssembly.MainModule.GetType("NGCView");
            NGCView.GetMethod("PostInstantiate").SetPublic(true);
            foreach (var x in NGC.Fields)
            {
                x.SetPublic(true);
            }
            foreach (var x in NGC.Methods)
            {
                x.SetPublic(true);
            }
            
            MethodDefinition AHook = hooksClass.GetMethod("AHook");
            MethodDefinition CHook = hooksClass.GetMethod("CHook");
            
            MethodDefinition A = NGC.GetMethod("A");
            MethodDefinition C = NGC.GetMethod("C");
            
            ILProcessor iLProcessor = A.Body.GetILProcessor();
            iLProcessor.InsertBefore(iLProcessor.Body.Instructions[0], Instruction.Create(OpCodes.Ret));
            iLProcessor.InsertBefore(iLProcessor.Body.Instructions[0], Instruction.Create(OpCodes.Brtrue_S, iLProcessor.Body.Instructions[1]));
            iLProcessor.InsertBefore(iLProcessor.Body.Instructions[0], Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(AHook)));
            iLProcessor.InsertBefore(iLProcessor.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_2)); 
            iLProcessor.InsertBefore(iLProcessor.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_1)); 
            iLProcessor.InsertBefore(iLProcessor.Body.Instructions[0], Instruction.Create(OpCodes.Ldarg_0)); 
            
            ILProcessor iLProcessor6 = C.Body.GetILProcessor();
            iLProcessor6.Body.Instructions.Clear();
            iLProcessor6.Body.ExceptionHandlers.Clear();
            iLProcessor6.Body.Variables.Clear();
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(CHook)));
            iLProcessor6.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void TimedExplosivePatch()
        {
            TypeDefinition TimedExplosive = rustAssembly.MainModule.GetType("TimedExplosive");
            MethodDefinition TimedExplosiveSpawnHook = hooksClass.GetMethod("TimedExplosiveSpawn");
            MethodDefinition TimedExplosiveAwake = TimedExplosive.GetMethod("Awake");
            TimedExplosive.GetField("testView").SetPublic(true);
            
            ILProcessor iLProcessor = TimedExplosiveAwake.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(TimedExplosiveSpawnHook)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void SleepingAvatarPatch()
        {
            TypeDefinition SleepingAvatar = rustAssembly.MainModule.GetType("SleepingAvatar");
            TypeDefinition Registry = SleepingAvatar.GetNestedType("Registry");
            Registry.IsPublic = true;
            Registry.GetField("all").SetPublic(true);
            SleepingAvatar.GetField("registered").SetPublic(true);
            
            MethodDefinition Register = Registry.GetMethod("Register");
            MethodDefinition UnRegister = Registry.GetMethod("UnRegister");
            
            MethodDefinition RegisterHook = hooksClass.GetMethod("SleeperRegister");
            MethodDefinition UnRegisterHook = hooksClass.GetMethod("SleeperUnRegister");

            
            ILProcessor iLProcessor = Register.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(RegisterHook)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            ILProcessor iLProcessor2 = UnRegister.Body.GetILProcessor();
            iLProcessor2.Body.Instructions.Clear();
            iLProcessor2.Body.ExceptionHandlers.Clear();
            iLProcessor2.Body.Variables.Clear();
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(UnRegisterHook)));
            iLProcessor2.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void PatchEnvironmentCycle()
        {
            TypeDefinition ecc = rustAssembly.MainModule.GetType("EnvironmentControlCenter");
            MethodDefinition DayCycleChange = hooksClass.GetMethod("DayCycleChange");
            ecc.GetField("sky").SetPublic(true);
            
            
            MethodDefinition update = ecc.GetMethod("Update");
            ILProcessor iLProcessor = update.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(DayCycleChange)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void PatchConsumableAction()
        {
            TypeDefinition ConsumableDataBlock = rustAssembly.MainModule.GetType("ConsumableDataBlock");
            MethodDefinition ConsumableUseItem = hooksClass.GetMethod("ConsumableUseItem");
            
            MethodDefinition UseItem = ConsumableDataBlock.GetMethod("UseItem");
            ILProcessor iLProcessor = UseItem.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(ConsumableUseItem)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }

        private void PatchBasicMedkitUse()
        {
            TypeDefinition BasicHealthKitDataBlock = rustAssembly.MainModule.GetType("BasicHealthKitDataBlock");
            MethodDefinition BasicHealthKitUse = hooksClass.GetMethod("BasicHealthKitUse");
            
            MethodDefinition UseItem = BasicHealthKitDataBlock.GetMethod("UseItem");
            ILProcessor iLProcessor = UseItem.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(BasicHealthKitUse)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void PatchModInstall()
        {
            TypeDefinition ItemRepresentation = rustAssembly.MainModule.GetType("ItemRepresentation");
            TypeDefinition ItemModDataBlock = rustAssembly.MainModule.GetType("ItemModDataBlock");
            TypeDefinition ItemModRepresentation = rustAssembly.MainModule.GetType("ItemModRepresentation");
            MethodDefinition ItemAddMod = hooksClass.GetMethod("ItemAddMod");
            TypeDefinition HeldItem = rustAssembly.MainModule.GetType("HeldItem`1");
            MethodDefinition AddMod = HeldItem.GetMethod("AddMod");
            
            foreach (FieldDefinition field in HeldItem.Fields)
            {
                if (field.Name == "_itemMods") 
                    field.SetPublic(true);
            }
            foreach (MethodDefinition method in HeldItem.Methods)
            {
                if (method.Name == "RecalculateMods" || method.Name == "OnModAdded")
                    method.SetPublic(true);
            }
            
            foreach (MethodDefinition method in ItemModRepresentation.Methods)
            {
                if (method.Name == "Initialize")
                {
                    method.IsPublic = true;
                }
            }
            
            foreach (TypeDefinition nested in ItemRepresentation.NestedTypes)
            {
                if (nested.Name == "ItemModPair" || nested.Name == "BindState" || nested.Name == "ItemModPairArray")
                {
                    nested.IsNestedPublic = true;
                }
            }

            foreach (MethodDefinition method in ItemRepresentation.Methods)
            {
                if (method.Name == "KillModRep")
                {
                    method.SetPublic(true);
                }
            }

            foreach (FieldDefinition field in ItemRepresentation.Fields)
            {
                if (field.Name == "_itemMods")
                {
                    field.SetPublic(true);
                }
            }

            foreach (MethodDefinition method in ItemModDataBlock.Methods)
            {
                if (method.Name == "AddModRepresentationComponent")
                {
                    method.SetPublic(true);
                }
            }
            
            var genericAddMod = new GenericInstanceMethod(rustAssembly.MainModule.Import(ItemAddMod));
            genericAddMod.GenericArguments.Add(HeldItem.GenericParameters[0]);
            
            ILProcessor iLProcessor = AddMod.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, genericAddMod));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void PatchBloodDrawUse()
        {
            TypeDefinition BloodDrawDatablock = rustAssembly.MainModule.GetType("BloodDrawDatablock");
            MethodDefinition BloodDrawUse = hooksClass.GetMethod("BloodDrawUse");
            TypeDefinition LateLoaded = BloodDrawDatablock.NestedTypes.First(n => n.Name == "LateLoaded");
            LateLoaded.IsNestedPublic = true;
            
            MethodDefinition UseItem = BloodDrawDatablock.GetMethod("UseItem");
            ILProcessor iLProcessor = UseItem.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(BloodDrawUse)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void PatchArmorEquip()
        {
            TypeDefinition ArmorDataBlock = rustAssembly.MainModule.GetType("ArmorDataBlock");
            MethodDefinition ArmorEquipped = hooksClass.GetMethod("ArmorEquipped");
            MethodDefinition ArmorUnEquipped = hooksClass.GetMethod("ArmorUnEquipped");

            MethodDefinition OnEquipped = ArmorDataBlock.GetMethod("OnEquipped");
            ILProcessor equipProcessor = OnEquipped.Body.GetILProcessor();
            equipProcessor.Body.Instructions.Clear();
            equipProcessor.Body.ExceptionHandlers.Clear();
            equipProcessor.Body.Variables.Clear();
            equipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); // this (ArmorDataBlock)
            equipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1)); // item (IEquipmentItem)
            equipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(ArmorEquipped)));
            equipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            
            MethodDefinition OnUnEquipped = ArmorDataBlock.GetMethod("OnUnEquipped");
            ILProcessor unEquipProcessor = OnUnEquipped.Body.GetILProcessor();
            unEquipProcessor.Body.Instructions.Clear();
            unEquipProcessor.Body.ExceptionHandlers.Clear();
            unEquipProcessor.Body.Variables.Clear();
            unEquipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0)); // this (ArmorDataBlock)
            unEquipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1)); // item (IEquipmentItem)
            unEquipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(ArmorUnEquipped)));
            unEquipProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void PatchTorchIgnite()
        {
            TypeDefinition TorchItemDataBlock = rustAssembly.MainModule.GetType("TorchItemDataBlock");
            MethodDefinition TorchDoAction2 = hooksClass.GetMethod("TorchDoAction2");
            MethodDefinition DoAction2 = TorchItemDataBlock.GetMethod("DoAction2");

            ILProcessor iLProcessor = DoAction2.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();

            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
    
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(TorchDoAction2)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void PatchBasicTorchIgnite()
        {
            TypeDefinition BasicTorchItemDataBlock = rustAssembly.MainModule.GetType("BasicTorchItemDataBlock");
            MethodDefinition BasicTorchDoAction2Hook = hooksClass.GetMethod("BasicTorchDoAction2");
            MethodDefinition DoAction2 = BasicTorchItemDataBlock.GetMethod("DoAction2");

            ILProcessor iLProcessor = DoAction2.Body.GetILProcessor();
            iLProcessor.Body.Instructions.Clear();
            iLProcessor.Body.ExceptionHandlers.Clear();
            iLProcessor.Body.Variables.Clear();
            
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_1));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_2));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_3));
    
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(BasicTorchDoAction2Hook)));
            iLProcessor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        private void PatchZones()
        {
            TypeDefinition heatZone = rustAssembly.MainModule.GetType("HeatZone");
            heatZone.GetField("_isOn").SetPublic(true);
            MethodDefinition heatOnTrigger = heatZone.GetMethod("OnTriggerStay");
            MethodDefinition heatHook = hooksClass.GetMethod("HeatZoneOnTriggerStay");

            heatOnTrigger.Body.Instructions.Clear();
            heatOnTrigger.Body.ExceptionHandlers.Clear();
            heatOnTrigger.Body.Variables.Clear();
            ILProcessor heatProcessor = heatOnTrigger.Body.GetILProcessor();
            heatProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
            heatProcessor.Append(Instruction.Create(OpCodes.Ldarg_1));
            heatProcessor.Append(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(heatHook)));
            heatProcessor.Append(Instruction.Create(OpCodes.Ret));

            TypeDefinition workZone = rustAssembly.MainModule.GetType("WorkZone");
            workZone.GetField("_isOn").SetPublic(true);
            MethodDefinition workOnTrigger = workZone.GetMethod("OnTriggerStay");
            MethodDefinition workHook = hooksClass.GetMethod("WorkZoneOnTriggerStay");

            workOnTrigger.Body.Instructions.Clear();
            workOnTrigger.Body.ExceptionHandlers.Clear();
            workOnTrigger.Body.Variables.Clear();
            ILProcessor workProcessor = workOnTrigger.Body.GetILProcessor();
            workProcessor.Append(Instruction.Create(OpCodes.Ldarg_0));
            workProcessor.Append(Instruction.Create(OpCodes.Ldarg_1));
            workProcessor.Append(Instruction.Create(OpCodes.Call, rustAssembly.MainModule.Import(workHook)));
            workProcessor.Append(Instruction.Create(OpCodes.Ret));
        }
        
        private void MetabolismPatch()
        {
            TypeDefinition Metabolism = rustAssembly.MainModule.GetType("Metabolism");
            Metabolism.GetMethod("CalculateMetabolicVitals").SetPublic(true);
            Metabolism.GetField("_lastTickTime").SetPublic(true);
            TypeDefinition vitalsUpdate = Metabolism.GetNestedType("VitalsUpdate");
            vitalsUpdate.IsPublic = true;
            
            MethodDefinition MetabolicUpdateFrame = Metabolism.GetMethod("MetabolicUpdateFrame");
            MetabolicUpdateFrame.SetPublic(true);
    
            MethodDefinition metabHook = hooksClass.GetMethod("MetabolicUpdateHook");

            MetabolicUpdateFrame.Body.Instructions.Clear();
            MetabolicUpdateFrame.Body.Variables.Clear();
            MetabolicUpdateFrame.Body.ExceptionHandlers.Clear();
            
            MetabolicUpdateFrame.Body.Instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            MetabolicUpdateFrame.Body.Instructions.Add(Instruction.Create(OpCodes.Call, this.rustAssembly.MainModule.Import(metabHook)));
            MetabolicUpdateFrame.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
        }
        
        // uLink Class56.method_36 has been patched here: https://i.imgur.com/WIEQXhX.png
        // I modified using dynspy to avoid the struggle.

        public bool FirstPass()
        {
            try
            {
                bool flag = true;
                
                TypeDefinition serverInit = rustAssembly.MainModule.GetType("ServerInit");

                if (serverInit.GetField("Fougerite_Patched_FirstPass") != null)
                {
                    Logger.Log("Assembly-CSharp.dll is already patched, please use a clean library.");
                    return false;
                }

                try
                {
                    this.FieldsUpdatePatch();
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                    flag = false;
                }

                try
                {
                    TypeReference type = AssemblyDefinition.ReadAssembly("mscorlib.dll").MainModule.GetType("System.String");
                    TypeReference fieldType = rustAssembly.MainModule.Import(type);
                    FieldDefinition definition3 = new FieldDefinition("Fougerite_Patched_FirstPass", FieldAttributes.CompilerControlled | FieldAttributes.FamANDAssem | FieldAttributes.Family, fieldType);
                    definition3.HasConstant = true;
                    definition3.Constant = Program.Version;
                    serverInit.Fields.Add(definition3);
                    rustAssembly.Write("Assembly-CSharp.dll");
                }
                catch (Exception ex)
                {
                    flag = false;
                    Logger.Log(ex);
                }
                
                WaitForPendingFinalizers();
                return flag;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }

        public bool SecondPass()
        {
            Logger.Log("Prepraching LatePost method...");
            this.LatePostInTryCatch();
            WaitForPendingFinalizers();
            Logger.Log("Prepatching UpdateDelegate");
            this.UpdateDelegatePatch();
            WaitForPendingFinalizers();
            Logger.Log("Success! Patching other methods...");
            try
            {
                bool flag = true;

                fougeriteAssembly = AssemblyDefinition.ReadAssembly("Fougerite.dll");
                unityAssembly = AssemblyDefinition.ReadAssembly("UnityEngine.dll");
                mscorlib = AssemblyDefinition.ReadAssembly("mscorlib.dll");
                hooksClass = fougeriteAssembly.MainModule.GetType("Fougerite.Hooks");
                SaveHandlerClass = fougeriteAssembly.MainModule.GetType("Fougerite.ServerSaveHandler");

                TypeDefinition serverInit = rustAssembly.MainModule.GetType("ServerInit");

                if (serverInit.GetField("Fougerite_Patched_SecondPass") != null)
                {
                    Logger.Log("Assembly-CSharp.dll is already patched, please use a clean library.");
                    return false;
                }

                try
                {
                    this.WrapWildlifeUpdateInTryCatch();
                    this.uLinkLateUpdateInTryCatch();
                    WaitForPendingFinalizers();

                    this.BootstrapAttachPatch();
                    this.EntityDecayPatch_StructureMaster();
                    this.EntityDecayPatch_EnvDecay();
                    this.ServerShutdownPatch();
                    this.ServerSavePatch();
                    this.BlueprintUsePatch();
                    this.EntityDeployedPatch_DeployableItemDataBlock();
                    //this.EntityDeployedPatch_StructureComponentDataBlock();
                    this.PlayerGatherWoodPatch();
                    this.PlayerGatherPatch();
                    this.PlayerSpawningSpawnedPatch();
                    this.ChatPatch();
                    this.ConsolePatch();
                    this.PlayerJoinLeavePatch();
                    this.PlayerKilledPatch();
                    this.EntityHurtPatch();
                    this.ItemsTablesLoadedPatch();
                    this.DoorSharing();
                    this.TalkerNotifications();
                    this.MetaBolismMethodMod();
                    this.CraftingPatch();
                    this.CraftingCancelAndCompletePatch();
                    this.NavMeshPatch();
                    this.ResourceSpawned();
                    this.InventoryModifications();
                    this.AirdropPatch();
                    this.ClientConnectionPatch();
                    this.ConnectionAcceptorPatch();
                    this.HumanControllerPatch();
                    this.SupplyCratePatch();
                    this.LatePostInTryCatch2();
                    this.ResearchPatch();
                    this.ConditionDebug();
                    this.NetcullPatch();
                    this.PatchFacePunch();
                    this.ItemPickupHook();
                    this.FallDamageHook();
                    this.PatchFireBarrelActTrigger();
                    this.FurnacePatch();
                    this.PatchFireBarrelContextRespond();
                    this.LooterPatch();
                    this.UseablePatch();
                    this.BowShootPatch();
                    this.SlotOperationPatch();
                    this.RepairBenchEvent();
                    this.DecayStopPatch();
                    this.GenericSpawnerPatch();
                    this.ServerLoadedPatch();
                    this.BeltPatch();
                    this.SupplySignalExplosion();
                    this.TossPatch();
                    this.ItemRepresentation();
                    this.DoAction1Patch();
                    this.NGCPatch();
                    this.TimedExplosivePatch();
                    this.SleepingAvatarPatch();
                    this.PatchEnvironmentCycle();
                    this.PatchConsumableAction();
                    this.PatchBasicMedkitUse();
                    this.PatchModInstall();
                    this.PatchBloodDrawUse();
                    this.PatchArmorEquip();
                    this.PatchTorchIgnite();
                    this.PatchBasicTorchIgnite();
                    this.PatchZones();
                    this.MetabolismPatch();
                }
                catch (Exception ex)
                {
                    Logger.Log("Make sure you have a clean uLink.dll from our package.");
                    Logger.Log(ex);
                    flag = false;
                }

                try
                {
                    TypeReference type = AssemblyDefinition.ReadAssembly("mscorlib.dll").MainModule.GetType("System.String");
                    TypeReference fieldType = rustAssembly.MainModule.Import(type);
                    FieldDefinition definition3 = new FieldDefinition("Fougerite_Patched_SecondPass", FieldAttributes.CompilerControlled | FieldAttributes.FamANDAssem | FieldAttributes.Family, fieldType);
                    definition3.HasConstant = true;
                    definition3.Constant = Program.Version;
                    serverInit.Fields.Add(definition3);
                    rustAssembly.Write("Assembly-CSharp.dll");
                }
                catch (Exception ex)
                {
                    flag = false;
                    Logger.Log(ex);
                }
                
                WaitForPendingFinalizers();
                return flag;
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                return false;
            }
        }

        private void WaitForPendingFinalizers()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void WrapMethod(MethodDefinition md, MethodDefinition origMethod, AssemblyDefinition asm, bool logEx = false)
        {
            Instruction instruction2;
            ILProcessor iLProcessor = md.Body.GetILProcessor();
            //Instruction instruction = Instruction.Create(OpCodes.Ldarg_0);
            if (md.ReturnType.Name == "Void")
            {
                instruction2 = md.Body.Instructions[md.Body.Instructions.Count - 1];
            }
            else
            {
                instruction2 = md.Body.Instructions[md.Body.Instructions.Count - 2];
            }
            //iLProcessor.InsertBefore(instruction2, instruction);

            iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Leave, instruction2));

            TypeReference type = AssemblyDefinition.ReadAssembly("mscorlib.dll").MainModule.GetType("System.Exception");
            md.Body.Variables.Add(new VariableDefinition("ex", asm.MainModule.Import(type))); ;

            Instruction instruction = null;
            if (logEx)
            {
                instruction = Instruction.Create(OpCodes.Stloc_0);
                iLProcessor.InsertBefore(instruction2, instruction);
                iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Nop));
                iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Ldloc_0));
                iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Ldnull));

                for (int i = 0; i < md.Parameters.Count; i++)
                {
                    iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Ldarga_S, md.Parameters[i]));
                }
                iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Call, asm.MainModule.Import(origMethod)));
            }
            else
            {
                instruction = Instruction.Create(OpCodes.Nop);
                iLProcessor.InsertBefore(instruction2, instruction);
                iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Nop));
            }
            iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Nop));
            iLProcessor.InsertBefore(instruction2, Instruction.Create(OpCodes.Leave, instruction2));

            ExceptionHandler item = new ExceptionHandler(ExceptionHandlerType.Catch);
            item.TryStart = md.Body.Instructions[0];
            item.TryEnd = instruction;
            item.HandlerStart = instruction;
            item.HandlerEnd = instruction2;
            if (md.ReturnType.Name != "Void")
            {
                Instruction instruction3 = Instruction.Create(OpCodes.Ret);
                iLProcessor.InsertBefore(instruction2, instruction3);
            }

            item.CatchType = asm.MainModule.Import(type);
            md.Body.ExceptionHandlers.Add(item);
        }
    }
}