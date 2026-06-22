using System;
using System.Text.Json;
using System.Linq;

using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.AST;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using System.Collections.Generic;
using UndertaleModLib.Models;
using System.IO;

if (!Data.Code.Any(x => x.Name.Content == "gml_GlobalScript_msgnextloc"))
{
    // ch1
    File.Copy("../input.json", Path.Combine(FilePath, "../lang/lang_en.json"), true);
    return;
}

var input = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText("../input.json")).SkipWhile(x => x.Key == "date").ToDictionary(x => x.Key, x => x.Value.GetString());

// Needed for decompiling the code entries
var decompileContext = new GlobalDecompileContext(Data);
var decompilerSettings = Data.ToolInfo.DecompilerSettings;

// Taken from ExportAllScripts.csx
var scriptCode = Data.Code.Where(c => c.ParentEntry is null).ToList();

var refs = new Dictionary<UndertaleString, int>();
foreach (var code in scriptCode)
{
    foreach (var inst in code.Instructions)
    {
        if (UndertaleInstruction.GetInstructionType(inst.Kind) == UndertaleInstruction.InstructionType.PushInstruction
            && inst.Type1 == UndertaleInstruction.DataType.String)
        {
            refs.TryAdd(inst.ValueString.Resource, 0);
            refs[inst.ValueString.Resource]++;
        }
    }
}

ScriptMessage($"Updating entries (0/{scriptCode.Count})");

var idx = 0;
foreach (var script in scriptCode.Select((code, i) => (Code: code, Index: i)))
{
    SearchInCode(script.Code, script.Index);
    ScriptMessage($"Updating entries ({++idx}/{scriptCode.Count})");
}
ScriptMessage("Finalized.");
return;

// Search inside a code entry for all the text codes and their corresponding language strings
void SearchInCode(UndertaleCode code, int scriptIndex)
{
    BlockNode rootBlock;
    // until UTMT can fix a decompilation error in Chapter 3, this try-catch stay here
    try
    {
        var context = new DecompileContext(decompileContext, code, decompilerSettings);
        rootBlock = (BlockNode)context.DecompileToAST();
    }
    catch (Exception e)
    {
        ScriptMessage($"exception while decompiling (ignoring): {e}");
        return;
    }

    var x = 0;
    SearchStatements(code, [..
        code.Instructions
            .Index()
            .Where(x =>
                UndertaleInstruction.GetInstructionType(x.Item.Kind) == UndertaleInstruction.InstructionType.CallInstruction
                && GetArgsIndexes(x.Item.ValueFunction.Name.Content) is not null
            ).Select(x => x.Index)
        ], ref x, rootBlock.Children);
    return;

    (Index key, Index val)? GetArgsIndexes(string funcName)
    {
        return funcName switch
        {
            "gml_Script_msgsetloc" or "gml_Script_c_msgsetloc" => (2, 1),
            "gml_Script_msgnextloc" or "gml_Script_c_msgnextloc" or "gml_Script_stringsetloc" => (1, 0),
            "gml_Script_msgsetsubloc" or "gml_Script_c_msgsetsubloc" => (^1, 1),
            "gml_Script_stringsetsubloc" or "gml_Script_msgnextsubloc" or "gml_Script_c_msgnextsubloc" => (^1, 0),
            _ => null
        };
    }

    void SearchExpression(UndertaleCode code, List<int> callInst, ref int cursor, IExpressionNode expr)
    {
        switch (expr)
        {
            case ArrayInitNode arrInit:
                SearchExpressions(code, callInst, ref cursor, arrInit.Elements);
                break;
            case AssignNode assign:
                SearchExpression(code, callInst, ref cursor, assign.Value);
                break;
            case BinaryNode bin:
                SearchExpression(code, callInst, ref cursor, bin.Left);
                SearchExpression(code, callInst, ref cursor, bin.Right);
                break;
            case ConditionalNode cond:
                SearchExpression(code, callInst, ref cursor, cond.Condition);
                SearchExpression(code, callInst, ref cursor, cond.True);
                SearchExpression(code, callInst, ref cursor, cond.False);
                break;
            case FunctionDeclNode funcDecl:
                SearchStatement(code, callInst, ref cursor, funcDecl.Body);
                break;
            case NullishCoalesceNode nullCoalesce:
                SearchExpression(code, callInst, ref cursor, nullCoalesce.Left);
                SearchExpression(code, callInst, ref cursor, nullCoalesce.Right);
                break;
            case ShortCircuitNode shortCircuit:
                SearchExpressions(code, callInst, ref cursor, shortCircuit.Conditions);
                break;
            case NewObjectNode newObj:
                SearchExpressions(code, callInst, ref cursor, newObj.Arguments);
                break;
            case ThrowNode throwNode:
                SearchExpression(code, callInst, ref cursor, throwNode.Value);
                break;
            case VariableCallNode varCall:
                SearchExpressions(code, callInst, ref cursor, varCall.Arguments);
                break;
            case FunctionCallNode funcCall:
                {
                    var index = GetArgsIndexes(funcCall.Function.Name.Content);

                    if (index is not null)
                    {
                        // What is wrong with Chapter 3?
                        try
                        {
                            var c = cursor++;
                            var k = callInst[c];
                            var inst = code.Instructions[k];
                            if (inst.ValueFunction is null || funcCall.Function.Name.Content != inst.ValueFunction.Name.Content)
                                throw new($"invalid function at cursor {c} (inst {k}) in {code.Name.Content}");
                            var keyString = (StringNode)funcCall.Arguments[index.Value.key];
                            var valueString = (StringNode)funcCall.Arguments[index.Value.val];

                            if (input.TryGetValue(keyString.Value.Content, out var val))
                            {
                                var f = false;
                                while (k >= 0)
                                {
                                    inst = code.Instructions[k];
                                    if (UndertaleInstruction.GetInstructionType(inst.Kind) == UndertaleInstruction.InstructionType.PushInstruction
                                        && inst.Type1 == UndertaleInstruction.DataType.String && inst.ValueString is not null
                                        && ReferenceEquals(inst.ValueString.Resource, valueString.Value))
                                    {
                                        f = true;
                                        var valStr = (UndertaleString)valueString.Value;
                                        if (refs.GetValueOrDefault(valStr, 0) <= 1)
                                            valStr.Content = val;
                                        else
                                            inst.ValueString = new(Data.Strings.MakeString(val, true));
                                        break;
                                    }
                                    k--;
                                }
                                if (!f)
                                    ScriptMessage($"wasn't able to replace the string in the instructions for: {keyString}: {valueString}");
                            }
                        }
                        catch (Exception e)
                        {
                            ScriptMessage($"exception while searching in function call {funcCall.Function.Name.Content}: {e}");
                        }
                    }
                    else
                        SearchExpressions(code, callInst, ref cursor, funcCall.Arguments);
                    break;
                }
        }
    }

    void SearchStatement(UndertaleCode code, List<int> callInst, ref int cursor, IStatementNode stmt)
    {
        if (stmt is IExpressionNode expr)
            SearchExpression(code, callInst, ref cursor, expr);

        switch (stmt)
        {
            case BlockNode block:
                SearchStatements(code, callInst, ref cursor, block.Children);
                break;
            case DoUntilLoopNode doUntil:
                SearchStatement(code, callInst, ref cursor, doUntil.Body);
                SearchExpression(code, callInst, ref cursor, doUntil.Condition);
                break;
            case ForLoopNode forLoop:
                if (forLoop.Initializer is not null) SearchStatement(code, callInst, ref cursor, forLoop.Initializer);
                if (forLoop.Condition is not null) SearchExpression(code, callInst, ref cursor, forLoop.Condition);
                if (forLoop.Incrementor is not null) SearchStatement(code, callInst, ref cursor, forLoop.Incrementor);
                SearchStatement(code, callInst, ref cursor, forLoop.Body);
                break;
            case IfNode ifNode:
                SearchExpression(code, callInst, ref cursor, ifNode.Condition);
                SearchStatement(code, callInst, ref cursor, ifNode.TrueBlock);
                SearchStatement(code, callInst, ref cursor, ifNode.ElseBlock);
                break;
            case RepeatLoopNode repeat:
                SearchStatement(code, callInst, ref cursor, repeat.Body);
                break;
            case ReturnNode ret:
                SearchExpression(code, callInst, ref cursor, ret.Value);
                break;
            case SwitchCaseNode switchCase:
                SearchExpression(code, callInst, ref cursor, switchCase.Expression);
                break;
            case SwitchNode switchNode:
                SearchExpression(code, callInst, ref cursor, switchNode.Expression);
                SearchStatement(code, callInst, ref cursor, switchNode.Body);
                break;
            case TryCatchNode tryCatch:
                SearchStatement(code, callInst, ref cursor, tryCatch.Try);
                SearchStatement(code, callInst, ref cursor, tryCatch.Catch);
                break;
            case WhileLoopNode whileLoop:
                SearchExpression(code, callInst, ref cursor, whileLoop.Condition);
                SearchStatement(code, callInst, ref cursor, whileLoop.Body);
                break;
            case WithLoopNode with:
                SearchStatement(code, callInst, ref cursor, with.Body);
                break;
        }
    }

    void SearchExpressions(UndertaleCode code, List<int> callInst, ref int cursor, List<IExpressionNode> exprs)
    {
        foreach (var x in exprs)
            SearchExpression(code, callInst, ref cursor, x);
    }

    void SearchStatements(UndertaleCode code, List<int> callInst, ref int cursor, List<IStatementNode> stmts)
    {
        foreach (var x in stmts)
            SearchStatement(code, callInst, ref cursor, x);
    }
}
