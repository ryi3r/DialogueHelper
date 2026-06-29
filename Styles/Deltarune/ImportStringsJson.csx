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

    SearchStatements(code, [..
        code.Instructions
            .Index()
            .Where(x =>
                UndertaleInstruction.GetInstructionType(x.Item.Kind) == UndertaleInstruction.InstructionType.CallInstruction
                && GetArgsIndexes(x.Item.ValueFunction.Name.Content) is not null
            ).Select(x => x.Index)
        ], rootBlock.Children);
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

    void SearchExpression(UndertaleCode code, List<int> callInst, IExpressionNode expr)
    {
        switch (expr)
        {
            case ArrayInitNode arrInit:
                SearchExpressions(code, callInst, arrInit.Elements);
                break;
            case StructNode stct:
                SearchStatements(code, callInst, stct.Body.Children);
                break;
            case AssignNode assign:
                SearchExpression(code, callInst, assign.Value);
                break;
            case BinaryNode bin:
                SearchExpression(code, callInst, bin.Left);
                SearchExpression(code, callInst, bin.Right);
                break;
            case ConditionalNode cond:
                SearchExpression(code, callInst, cond.Condition);
                SearchExpression(code, callInst, cond.True);
                SearchExpression(code, callInst, cond.False);
                break;
            case FunctionDeclNode funcDecl:
                SearchStatement(code, callInst, funcDecl.Body);
                break;
            case NullishCoalesceNode nullCoalesce:
                SearchExpression(code, callInst, nullCoalesce.Left);
                SearchExpression(code, callInst, nullCoalesce.Right);
                break;
            case ShortCircuitNode shortCircuit:
                SearchExpressions(code, callInst, shortCircuit.Conditions);
                break;
            case NewObjectNode newObj:
                SearchExpressions(code, callInst, newObj.Arguments);
                break;
            case ThrowNode throwNode:
                SearchExpression(code, callInst, throwNode.Value);
                break;
            case VariableCallNode varCall:
                SearchExpressions(code, callInst, varCall.Arguments);
                break;
            case FunctionCallNode funcCall:
                {
                    var index = GetArgsIndexes(funcCall.Function.Name.Content);

                    if (index is not null)
                    {
                        // What is wrong with Chapter 3?
                        var lastInst = -1;
                        try
                        {
                            var keyString = (StringNode)funcCall.Arguments[index.Value.key];
                            var valueString = (StringNode)funcCall.Arguments[index.Value.val];

                            if (input.TryGetValue(keyString.Value.Content, out var val))
                            {
                                var keyInst = (UndertaleInstruction)null;
                                var valueInst = (UndertaleInstruction)null;

                                for (var cii = 0; cii < callInst.Count; cii++)
                                {
                                    var instNum = callInst[cii];
                                    var baseInstNum = instNum;

                                    var searchingKey = index.Value.val.GetOffset(funcCall.Arguments.Count) > index.Value.key.GetOffset(funcCall.Arguments.Count);

                                    try
                                    {
                                        var inst = code.Instructions[instNum];
                                        if (funcCall.Function.Name.Content != inst.ValueFunction.Name.Content)
                                            continue; // not found any proper function calls yet
                                        if (inst.ValueFunction is null)
                                            throw new($"invalid function (instuction n. {instNum}) in {code.Name.Content}: {funcCall.Function.Name.Content} != {inst.ValueFunction.Name.Content}");

                                        while (instNum > lastInst)
                                        {
                                            inst = code.Instructions[instNum];
                                            if (UndertaleInstruction.GetInstructionType(inst.Kind) == UndertaleInstruction.InstructionType.PushInstruction
                                                && inst.Type1 == UndertaleInstruction.DataType.String && inst.ValueString is not null
                                                && ReferenceEquals(inst.ValueString.Resource, (searchingKey ? keyString : valueString).Value))
                                            {
                                                if (searchingKey)
                                                    keyInst = inst;
                                                else
                                                    valueInst = inst;
                                                searchingKey = !searchingKey;
                                                if (keyInst is not null && valueInst is not null)
                                                    break;
                                            }
                                            instNum--;
                                        }
                                    }
                                    finally
                                    {
                                        lastInst = baseInstNum;
                                    }
                                    if (keyInst is not null && valueInst is not null)
                                    {
                                        var valStr = valueInst.ValueString.Resource;
                                        if (refs.GetValueOrDefault(valStr, 0) <= 1)
                                            valStr.Content = val;
                                        else
                                        {
                                            valueInst.ValueString = new(Data.Strings.MakeString(val, true));
                                            refs[valStr]--;
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        keyInst = null;
                                        valueInst = null;
                                    }
                                }

                                if (keyInst is null && valueInst is null)
                                    ScriptMessage($"wasn't able to replace the string in the instructions for {keyString.Value.Content} = {valueString.Value.Content}");
                            }
                        }
                        catch (Exception e)
                        {
                            ScriptMessage($"exception while searching in function call {funcCall.Function.Name.Content}: {e}");
                        }
                    }
                    else
                        SearchExpressions(code, callInst, [.. funcCall.Arguments.Reverse<IExpressionNode>()]);
                    break;
                }
        }
    }

    void SearchStatement(UndertaleCode code, List<int> callInst, IStatementNode stmt)
    {
        if (stmt is IExpressionNode expr)
            SearchExpression(code, callInst, expr);

        switch (stmt)
        {
            case BlockNode block:
                SearchStatements(code, callInst, block.Children);
                break;
            case DoUntilLoopNode doUntil:
                SearchStatement(code, callInst, doUntil.Body);
                SearchExpression(code, callInst, doUntil.Condition);
                break;
            case ForLoopNode forLoop:
                if (forLoop.Initializer is not null) SearchStatement(code, callInst, forLoop.Initializer);
                if (forLoop.Condition is not null) SearchExpression(code, callInst, forLoop.Condition);
                if (forLoop.Incrementor is not null) SearchStatement(code, callInst, forLoop.Incrementor);
                SearchStatement(code, callInst, forLoop.Body);
                break;
            case IfNode ifNode:
                SearchExpression(code, callInst, ifNode.Condition);
                SearchStatement(code, callInst, ifNode.TrueBlock);
                SearchStatement(code, callInst, ifNode.ElseBlock);
                break;
            case RepeatLoopNode repeat:
                SearchStatement(code, callInst, repeat.Body);
                break;
            case ReturnNode ret:
                SearchExpression(code, callInst, ret.Value);
                break;
            case SwitchCaseNode switchCase:
                SearchExpression(code, callInst, switchCase.Expression);
                break;
            case SwitchNode switchNode:
                SearchExpression(code, callInst, switchNode.Expression);
                SearchStatement(code, callInst, switchNode.Body);
                break;
            case TryCatchNode tryCatch:
                SearchStatement(code, callInst, tryCatch.Try);
                SearchStatement(code, callInst, tryCatch.Catch);
                break;
            case WhileLoopNode whileLoop:
                SearchExpression(code, callInst, whileLoop.Condition);
                SearchStatement(code, callInst, whileLoop.Body);
                break;
            case WithLoopNode with:
                SearchStatement(code, callInst, with.Body);
                break;
        }
    }

    void SearchExpressions(UndertaleCode code, List<int> callInst, List<IExpressionNode> exprs)
    {
        foreach (var x in exprs)
            SearchExpression(code, callInst, x);
    }

    void SearchStatements(UndertaleCode code, List<int> callInst, List<IStatementNode> stmts)
    {
        foreach (var x in stmts)
            SearchStatement(code, callInst, x);
    }
}
