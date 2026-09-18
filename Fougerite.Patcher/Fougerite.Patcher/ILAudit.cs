using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Fougerite.Patcher
{
    /// <summary>
    /// Snapshots every method body before and after patching, diffs them, and runs a set
    /// of structural checks that catch the ways Cecil patches usually break:
    /// orphaned branch targets, unbalanced stacks, stale MaxStackSize, bad ldarg indices.
    ///
    /// Nothing here needs to know which patches ran. It compares the whole module, so
    /// collateral damage from things like RetargetToCallvirt shows up on its own.
    /// </summary>
    public static class ILAudit
    {
        public static string OutputFolder =
            Path.Combine(Directory.GetCurrentDirectory(), "Fougerite_PatchAudit");

        // ------------------------------------------------------------------
        // Snapshots
        // ------------------------------------------------------------------

        /// <summary>
        /// method full name -> rendered IL. Offsets are deliberately excluded and branch
        /// targets are rendered as instruction indices, so inserting an instruction does
        /// not make every later line look changed.
        /// </summary>
        public static Dictionary<string, string> SnapshotIL(params AssemblyDefinition[] assemblies)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (AssemblyDefinition asm in assemblies)
            {
                foreach (TypeDefinition t in asm.MainModule.GetTypes())
                {
                    foreach (MethodDefinition m in t.Methods)
                    {
                        if (!m.HasBody) continue;
                        string key = asm.Name.Name + "::" + m.FullName;
                        if (map.ContainsKey(key)) continue;
                        map[key] = RenderBody(m);
                    }
                }
            }

            return map;
        }

        /// <summary>
        /// member full name -> accessibility string. Separate from the IL snapshot because
        /// OpenType touches thousands of members and would drown the IL diff.
        /// </summary>
        public static Dictionary<string, string> SnapshotAccess(params AssemblyDefinition[] assemblies)
        {
            Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (AssemblyDefinition asm in assemblies)
            {
                string a = asm.Name.Name;
                foreach (TypeDefinition t in asm.MainModule.GetTypes())
                {
                    Put(map, a + "::T:" + t.FullName, TypeAccess(t));

                    foreach (FieldDefinition f in t.Fields)
                        Put(map, a + "::F:" + f.FullName, f.IsPublic ? "public" : (f.IsPrivate ? "private" : "other"));

                    foreach (MethodDefinition m in t.Methods)
                        Put(map, a + "::M:" + m.FullName, MethodAccess(m));
                }
            }

            return map;
        }

        private static void Put(Dictionary<string, string> map, string k, string v)
        {
            if (!map.ContainsKey(k)) map[k] = v;
        }

        private static string TypeAccess(TypeDefinition t)
        {
            string vis = t.IsNested
                ? (t.IsNestedPublic ? "nested-public" : "nested-other")
                : (t.IsPublic ? "public" : "internal");
            return vis + (t.IsSealed ? " sealed" : "") + (t.IsAbstract ? " abstract" : "");
        }

        private static string MethodAccess(MethodDefinition m)
        {
            string vis = m.IsPublic ? "public" : (m.IsPrivate ? "private" : "other");
            return vis
                   + (m.IsVirtual ? " virtual" : "")
                   + (m.IsFinal ? " final" : "")
                   + (m.IsNewSlot ? " newslot" : "");
        }

        // ------------------------------------------------------------------
        // Rendering
        // ------------------------------------------------------------------

        private static string RenderBody(MethodDefinition m)
        {
            MethodBody body = m.Body;
            Dictionary<Instruction, int> index = BuildIndex(body);
            StringBuilder sb = new StringBuilder();

            sb.Append("maxstack ").Append(body.MaxStackSize)
                .Append("  locals ").Append(body.Variables.Count)
                .Append("  handlers ").Append(body.ExceptionHandlers.Count)
                .Append('\n');

            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction ins = body.Instructions[i];
                sb.Append(i.ToString("D4")).Append(": ")
                    .Append(ins.OpCode.Name);

                string op = RenderOperand(ins.Operand, index);
                if (op != null) sb.Append(' ').Append(op);
                sb.Append('\n');
            }

            foreach (ExceptionHandler h in body.ExceptionHandlers)
            {
                sb.Append("handler ").Append(h.HandlerType)
                    .Append(" try ").Append(Idx(index, h.TryStart)).Append("..").Append(Idx(index, h.TryEnd))
                    .Append(" handler ").Append(Idx(index, h.HandlerStart)).Append("..")
                    .Append(Idx(index, h.HandlerEnd))
                    .Append('\n');
            }

            return sb.ToString();
        }

        private static Dictionary<Instruction, int> BuildIndex(MethodBody body)
        {
            Dictionary<Instruction, int> index = new Dictionary<Instruction, int>();
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                if (!index.ContainsKey(body.Instructions[i]))
                    index[body.Instructions[i]] = i;
            }

            return index;
        }

        private static string Idx(Dictionary<Instruction, int> index, Instruction ins)
        {
            if (ins == null) return "none";
            int i;
            return index.TryGetValue(ins, out i) ? "#" + i.ToString("D4") : "ORPHAN";
        }

        private static string RenderOperand(object operand, Dictionary<Instruction, int> index)
        {
            if (operand == null) return null;

            Instruction target = operand as Instruction;
            if (target != null) return "-> " + Idx(index, target);

            Instruction[] targets = operand as Instruction[];
            if (targets != null)
                return "-> [" + string.Join(", ", targets.Select(x => Idx(index, x)).ToArray()) + "]";

            VariableDefinition v = operand as VariableDefinition;
            if (v != null) return "loc_" + v.Index;

            ParameterDefinition p = operand as ParameterDefinition;
            if (p != null) return "arg_" + p.Index + " (" + p.Name + ")";

            MemberReference mr = operand as MemberReference;
            if (mr != null) return mr.FullName;

            string s = operand as string;
            if (s != null) return "\"" + s.Replace("\n", "\\n").Replace("\r", "\\r") + "\"";

            return operand.ToString();
        }

        // ------------------------------------------------------------------
        // Diff
        // ------------------------------------------------------------------

        /// <summary>
        /// The methods whose rendered IL differs between the two snapshots.
        /// </summary>
        public static HashSet<string> ChangedKeys(Dictionary<string, string> pre, Dictionary<string, string> post)
        {
            HashSet<string> set = new HashSet<string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string> kv in post)
            {
                string before;
                if (!pre.TryGetValue(kv.Key, out before))
                {
                    set.Add(kv.Key);
                    continue;
                }

                if (!string.Equals(before, kv.Value, StringComparison.Ordinal)) set.Add(kv.Key);
            }

            return set;
        }

        public static void WriteILDiff(Dictionary<string, string> pre, Dictionary<string, string> post)
        {
            Directory.CreateDirectory(OutputFolder);
            string path = Path.Combine(OutputFolder, "il_diff.txt");

            List<string> changed = new List<string>();
            List<string> added = new List<string>();
            List<string> removed = new List<string>();

            foreach (KeyValuePair<string, string> kv in post)
            {
                string before;
                if (!pre.TryGetValue(kv.Key, out before))
                {
                    added.Add(kv.Key);
                    continue;
                }

                if (!string.Equals(before, kv.Value, StringComparison.Ordinal)) changed.Add(kv.Key);
            }

            foreach (string k in pre.Keys)
            {
                if (!post.ContainsKey(k)) removed.Add(k);
            }

            changed.Sort(StringComparer.Ordinal);
            added.Sort(StringComparer.Ordinal);
            removed.Sort(StringComparer.Ordinal);

            using (StreamWriter w = new StreamWriter(path, false))
            {
                w.WriteLine("Fougerite patcher IL diff");
                w.WriteLine("generated {0}", DateTime.Now);
                w.WriteLine("methods with bodies: before {0}, after {1}", pre.Count, post.Count);
                w.WriteLine("changed {0}   body added {1}   body removed {2}",
                    changed.Count, added.Count, removed.Count);
                w.WriteLine();

                w.WriteLine("== summary ==");
                foreach (string k in changed) w.WriteLine("  CHANGED  {0}", k);
                foreach (string k in added) w.WriteLine("  ADDED    {0}", k);
                foreach (string k in removed) w.WriteLine("  REMOVED  {0}", k);
                w.WriteLine();

                foreach (string k in changed)
                {
                    w.WriteLine("================================================================");
                    w.WriteLine(k);
                    w.WriteLine("================================================================");
                    WriteUnified(w, pre[k], post[k]);
                    w.WriteLine();
                }
            }

            Logger.Log($"[ILAudit] IL diff written: {path} ({changed.Count} changed)");
        }

        public static void WriteAccessDiff(Dictionary<string, string> pre, Dictionary<string, string> post)
        {
            Directory.CreateDirectory(OutputFolder);
            string path = Path.Combine(OutputFolder, "access_diff.txt");

            List<string> lines = new List<string>();
            foreach (KeyValuePair<string, string> kv in post)
            {
                string before;
                if (!pre.TryGetValue(kv.Key, out before)) continue;
                if (before == kv.Value) continue;
                lines.Add(string.Format("{0,-22} -> {1,-22} {2}", before, kv.Value, kv.Key));
            }

            lines.Sort(StringComparer.Ordinal);

            using (StreamWriter w = new StreamWriter(path, false))
            {
                w.WriteLine("Fougerite patcher accessibility diff");
                w.WriteLine("generated {0}", DateTime.Now);
                w.WriteLine("{0} member(s) changed", lines.Count);
                w.WriteLine();
                foreach (string l in lines) w.WriteLine(l);
            }

            Logger.Log($"[ILAudit] access diff written: {path} ({lines.Count} changed)");
        }

        /// <summary>
        /// Plain LCS unified diff. Bodies are small, so the O(n*m) table is fine.
        /// </summary>
        private static void WriteUnified(StreamWriter w, string before, string after)
        {
            string[] a = before.Split('\n');
            string[] b = after.Split('\n');

            int[,] lcs = new int[a.Length + 1, b.Length + 1];
            for (int i = a.Length - 1; i >= 0; i--)
            {
                for (int j = b.Length - 1; j >= 0; j--)
                {
                    lcs[i, j] = a[i] == b[j]
                        ? lcs[i + 1, j + 1] + 1
                        : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }

            int x = 0, y = 0;
            while (x < a.Length && y < b.Length)
            {
                if (a[x] == b[y])
                {
                    w.WriteLine("    {0}", a[x]);
                    x++;
                    y++;
                }
                else if (lcs[x + 1, y] >= lcs[x, y + 1])
                {
                    w.WriteLine("  - {0}", a[x]);
                    x++;
                }
                else
                {
                    w.WriteLine("  + {0}", b[y]);
                    y++;
                }
            }

            while (x < a.Length)
            {
                w.WriteLine("  - {0}", a[x]);
                x++;
            }

            while (y < b.Length)
            {
                w.WriteLine("  + {0}", b[y]);
                y++;
            }
        }

        // ------------------------------------------------------------------
        // Validation
        // ------------------------------------------------------------------

        /// <summary>
        /// Runs the structural checks over every method body. Returns the problem list and
        /// also writes it to disk. An empty list does not prove the patch is correct, it
        /// only means the IL is well formed.
        /// </summary>
        public static List<string> Validate(params AssemblyDefinition[] assemblies)
        {
            return Validate(null, assemblies);
        }

        /// <summary>
        /// Same, but restricted to the methods whose IL actually changed. Pass the key set
        /// from ChangedKeys. This is the one to run by default: it keeps the report about
        /// your patches instead of drowning it in vanilla code.
        /// </summary>
        public static List<string> Validate(HashSet<string> onlyThese, params AssemblyDefinition[] assemblies)
        {
            List<string> problems = new List<string>();

            foreach (AssemblyDefinition asm in assemblies)
            {
                foreach (TypeDefinition t in asm.MainModule.GetTypes())
                {
                    foreach (MethodDefinition m in t.Methods)
                    {
                        if (!m.HasBody) continue;
                        if (onlyThese != null && !onlyThese.Contains(asm.Name.Name + "::" + m.FullName)) continue;
                        try
                        {
                            ValidateMethod(asm.Name.Name, m, problems);
                        }
                        catch (Exception ex)
                        {
                            problems.Add($"[crash] {m.FullName}: validator threw {ex.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }

            Directory.CreateDirectory(OutputFolder);
            string path = Path.Combine(OutputFolder, "validation.txt");
            using (StreamWriter w = new StreamWriter(path, false))
            {
                w.WriteLine("Fougerite patcher IL validation");
                w.WriteLine("generated {0}", DateTime.Now);
                w.WriteLine("scope: {0}", onlyThese == null ? "whole module" : onlyThese.Count + " changed method(s)");
                w.WriteLine("{0} problem(s)", problems.Count);
                w.WriteLine();
                foreach (string p in problems) w.WriteLine(p);
            }

            Logger.Log($"[ILAudit] validation written: {path} ({problems.Count} problems)");
            return problems;
        }

        private static void ValidateMethod(string asmName, MethodDefinition m, List<string> problems)
        {
            MethodBody body = m.Body;
            string who = asmName + "::" + m.FullName;

            if (body.Instructions.Count == 0)
            {
                problems.Add($"[empty] {who}: method has a body with zero instructions");
                return;
            }

            HashSet<Instruction> present = new HashSet<Instruction>(body.Instructions);

            // 1. Branch targets that are not in this body. The classic Cecil bug: clearing
            //    or rebuilding a body while a branch elsewhere still points at an old
            //    instruction. Produces InvalidProgramException at JIT time.
            for (int i = 0; i < body.Instructions.Count; i++)
            {
                Instruction ins = body.Instructions[i];

                Instruction target = ins.Operand as Instruction;
                if (target != null && !present.Contains(target))
                    problems.Add(
                        $"[orphan-branch] {who}: #{i} {ins.OpCode.Name} targets an instruction not in the body");

                Instruction[] targets = ins.Operand as Instruction[];
                if (targets != null)
                {
                    for (int j = 0; j < targets.Length; j++)
                    {
                        if (targets[j] != null && !present.Contains(targets[j]))
                            problems.Add(
                                $"[orphan-switch] {who}: #{i} switch case {j} targets an instruction not in the body");
                    }
                }

                // 2. Argument and local indices.
                ParameterDefinition p = ins.Operand as ParameterDefinition;
                if (p != null && !m.Parameters.Contains(p) && !(m.HasThis && p == body.ThisParameter))
                    problems.Add(
                        $"[bad-arg] {who}: #{i} {ins.OpCode.Name} references a parameter that is not on this method");

                VariableDefinition v = ins.Operand as VariableDefinition;
                if (v != null && !body.Variables.Contains(v))
                    problems.Add($"[bad-local] {who}: #{i} {ins.OpCode.Name} references a local not in this body");

                int shortArg = ShortFormArgIndex(ins.OpCode);
                if (shortArg >= 0)
                {
                    int available = m.Parameters.Count + (m.HasThis ? 1 : 0);
                    if (shortArg >= available)
                        problems.Add(
                            $"[bad-arg] {who}: #{i} {ins.OpCode.Name} but the method only takes {available} argument slot(s)");
                }

                int shortLoc = ShortFormLocalIndex(ins.OpCode);
                if (shortLoc >= 0 && shortLoc >= body.Variables.Count)
                    problems.Add(
                        $"[bad-local] {who}: #{i} {ins.OpCode.Name} but the body declares {body.Variables.Count} local(s)");
            }

            // 3. Exception handler boundaries still pointing at live instructions.
            foreach (ExceptionHandler h in body.ExceptionHandlers)
            {
                CheckHandlerEdge(who, "TryStart", h.TryStart, present, problems);
                CheckHandlerEdge(who, "TryEnd", h.TryEnd, present, problems);
                CheckHandlerEdge(who, "HandlerStart", h.HandlerStart, present, problems);
                CheckHandlerEdge(who, "HandlerEnd", h.HandlerEnd, present, problems);
            }

            // 4. Body must not fall off the end.
            Instruction last = body.Instructions[body.Instructions.Count - 1];
            if (!IsTerminator(last.OpCode))
                problems.Add(
                    $"[fallthrough] {who}: body ends with {last.OpCode.Name}, which does not terminate control flow");

            // 5. Stack simulation.
            SimulateStack(who, m, problems);
        }

        private static void CheckHandlerEdge(string who, string label, Instruction ins,
            HashSet<Instruction> present, List<string> problems)
        {
            if (ins == null) return;
            if (!present.Contains(ins))
                problems.Add(
                    $"[orphan-handler] {who}: exception handler {label} points at an instruction not in the body");
        }

        private static bool IsTerminator(OpCode op)
        {
            return op == OpCodes.Ret
                   || op == OpCodes.Throw
                   || op == OpCodes.Rethrow
                   || op == OpCodes.Br || op == OpCodes.Br_S
                   || op == OpCodes.Leave || op == OpCodes.Leave_S
                   || op == OpCodes.Endfinally
                   || op == OpCodes.Endfilter
                   || op == OpCodes.Jmp;
        }

        private static int ShortFormArgIndex(OpCode op)
        {
            if (op == OpCodes.Ldarg_0) return 0;
            if (op == OpCodes.Ldarg_1) return 1;
            if (op == OpCodes.Ldarg_2) return 2;
            if (op == OpCodes.Ldarg_3) return 3;
            return -1;
        }

        private static int ShortFormLocalIndex(OpCode op)
        {
            if (op == OpCodes.Ldloc_0 || op == OpCodes.Stloc_0) return 0;
            if (op == OpCodes.Ldloc_1 || op == OpCodes.Stloc_1) return 1;
            if (op == OpCodes.Ldloc_2 || op == OpCodes.Stloc_2) return 2;
            if (op == OpCodes.Ldloc_3 || op == OpCodes.Stloc_3) return 3;
            return -1;
        }

        // ------------------------------------------------------------------
        // Stack simulation
        // ------------------------------------------------------------------

        /// <summary>
        /// Propagates stack height through the control flow graph. Catches underflow,
        /// paths that reach the same instruction at different heights, values left on the
        /// stack at ret, and a MaxStackSize that is too small for the patched body.
        /// </summary>
        private static void SimulateStack(string who, MethodDefinition m, List<string> problems)
        {
            MethodBody body = m.Body;
            List<Instruction> ins = body.Instructions.ToList();
            Dictionary<Instruction, int> idx = new Dictionary<Instruction, int>();
            for (int i = 0; i < ins.Count; i++)
                if (!idx.ContainsKey(ins[i]))
                    idx[ins[i]] = i;

            int[] heightAt = new int[ins.Count];
            bool[] known = new bool[ins.Count];
            Queue<int> work = new Queue<int>();

            heightAt[0] = 0;
            known[0] = true;
            work.Enqueue(0);

            // Handler and filter entry points start with the exception on the stack.
            foreach (ExceptionHandler h in body.ExceptionHandlers)
            {
                Seed(h.HandlerStart, h.HandlerType == ExceptionHandlerType.Catch ? 1 : 0,
                    idx, heightAt, known, work);
                if (h.FilterStart != null) Seed(h.FilterStart, 1, idx, heightAt, known, work);
            }

            int observedMax = 0;

            while (work.Count > 0)
            {
                int i = work.Dequeue();
                Instruction cur = ins[i];
                int h = heightAt[i];

                int pop = PopCount(cur, m, h);
                if (pop > h)
                {
                    problems.Add(
                        $"[stack-underflow] {who}: #{i} {cur.OpCode.Name} pops {pop} with only {h} on the stack");
                    continue;
                }

                int after = h - pop + PushCount(cur, m);
                if (cur.OpCode.StackBehaviourPop == StackBehaviour.PopAll) after = PushCount(cur, m);
                if (after > observedMax) observedMax = after;

                if (cur.OpCode == OpCodes.Ret)
                {
                    bool returnsValue = m.ReturnType.MetadataType != MetadataType.Void;
                    int expected = returnsValue ? 1 : 0;
                    if (h != expected)
                        problems.Add(
                            $"[stack-at-ret] {who}: #{i} ret with {h} value(s) on the stack, expected {expected}");
                    continue;
                }

                foreach (Instruction next in Successors(cur, ins, i))
                {
                    int ni;
                    if (next == null || !idx.TryGetValue(next, out ni)) continue;
                    if (known[ni])
                    {
                        if (heightAt[ni] != after)
                            problems.Add(
                                $"[stack-conflict] {who}: #{ni} {ins[ni].OpCode.Name} reached at height {heightAt[ni]} and {after}");
                    }
                    else
                    {
                        heightAt[ni] = after;
                        known[ni] = true;
                        work.Enqueue(ni);
                    }
                }
            }

            if (body.MaxStackSize < observedMax)
            {
                problems.Add(
                    $"[maxstack] {who}: MaxStackSize is {body.MaxStackSize} but the body needs {observedMax}. " +
                    "If your Cecil version does not recompute this on write, the JIT will reject the method.");
            }
        }

        private static void Seed(Instruction ins, int height, Dictionary<Instruction, int> idx,
            int[] heightAt, bool[] known, Queue<int> work)
        {
            int i;
            if (ins == null || !idx.TryGetValue(ins, out i)) return;
            if (known[i]) return;
            heightAt[i] = height;
            known[i] = true;
            work.Enqueue(i);
        }

        private static IEnumerable<Instruction> Successors(Instruction cur, List<Instruction> ins, int i)
        {
            OperandType ot = cur.OpCode.OperandType;
            FlowControl fc = cur.OpCode.FlowControl;

            if (ot == OperandType.InlineSwitch)
            {
                Instruction[] targets = cur.Operand as Instruction[];
                if (targets != null)
                {
                    foreach (Instruction t in targets) yield return t;
                }

                if (i + 1 < ins.Count) yield return ins[i + 1];
                yield break;
            }

            if (ot == OperandType.InlineBrTarget || ot == OperandType.ShortInlineBrTarget)
            {
                yield return cur.Operand as Instruction;
                if (fc == FlowControl.Cond_Branch && i + 1 < ins.Count) yield return ins[i + 1];
                yield break;
            }

            if (fc == FlowControl.Throw || fc == FlowControl.Return) yield break;

            if (i + 1 < ins.Count) yield return ins[i + 1];
        }

        private static int PopCount(Instruction ins, MethodDefinition owner, int currentHeight)
        {
            OpCode op = ins.OpCode;

            if (op.StackBehaviourPop == StackBehaviour.Varpop)
            {
                if (op == OpCodes.Ret)
                    return owner.ReturnType.MetadataType == MetadataType.Void ? 0 : 1;

                IMethodSignature sig = ins.Operand as IMethodSignature;
                if (sig == null) return 0;

                int n = sig.Parameters.Count;
                if (op == OpCodes.Newobj) return n;
                if (sig.HasThis && !sig.ExplicitThis) n++;
                if (op == OpCodes.Calli) n++;
                return n;
            }

            // leave / leave.s empty the evaluation stack per ECMA. Model it explicitly
            // so try/catch heavy vanilla code does not produce false conflicts.
            if (op == OpCodes.Leave || op == OpCodes.Leave_S) return currentHeight;
            if (op == OpCodes.Endfinally || op == OpCodes.Endfilter) return currentHeight;

            if (op.StackBehaviourPop == StackBehaviour.PopAll) return currentHeight;

            return Simple(op.StackBehaviourPop);
        }

        private static int PushCount(Instruction ins, MethodDefinition owner)
        {
            OpCode op = ins.OpCode;

            if (op.StackBehaviourPush == StackBehaviour.Varpush)
            {
                IMethodSignature sig = ins.Operand as IMethodSignature;
                if (sig == null) return 0;
                return sig.ReturnType.MetadataType == MetadataType.Void ? 0 : 1;
            }

            if (op == OpCodes.Newobj) return 1;

            return Simple(op.StackBehaviourPush);
        }

        private static int Simple(StackBehaviour b)
        {
            switch (b)
            {
                case StackBehaviour.Pop0:
                case StackBehaviour.Push0:
                    return 0;

                case StackBehaviour.Pop1:
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    return 1;

                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                case StackBehaviour.Push1_push1:
                    return 2;

                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    return 3;

                default:
                    return 0;
            }
        }
    }
}