// taken mostly from: https://gist.github.com/nthung-2k5/66ce083b8f6734900fb4664cf6fe27c1
// with custom modifications from ryi3r (the one that's writing this text)

using System;
using System.Text.Json;
using System.Linq;
using System.Collections.Specialized;
using System.Text.Encodings.Web;

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
    File.Copy(Path.Combine(FilePath, "../lang/lang_en.json"), "../output.json", true);
    return;
}

// Needed for decompiling the code entries
var decompileContext = new GlobalDecompileContext(Data);
var decompilerSettings = Data.ToolInfo.DecompilerSettings;

// Taken from ExportAllScripts.csx
var scriptCode = Data.Code.Where(c => c.ParentEntry is null).ToList();

ScriptMessage($"Extracting text (0/{scriptCode.Count})");

var idx = 0;
var lang = new Dictionary<string, (int, string)>();
var order = new List<string>();
var jp_order = JsonSerializer.Deserialize<OrderedDictionary<string, object>>(
    File.ReadAllBytes(Path.Combine(Path.Combine(Path.GetDirectoryName(FilePath), "lang"), "lang_ja.json")),
    new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, }
).SkipWhile(x => x.Key == "date").Select(x => x.Key).ToList();
foreach (var script in scriptCode.Select((code, i) => (Code: code, Index: i)))
{
    SearchInCode(script.Code, script.Index);
    ScriptMessage($"Extracting text ({++idx}/{scriptCode.Count})");
}
var output = new OrderedDictionary { ["date"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() };
foreach (var k in jp_order)
{
    if (lang.TryGetValue(k, out var v))
        output[k] = v.Item2;
}
foreach (var k in order.Where(x => !output.Contains(x)))
    output[k] = lang[k].Item2; // add missing keys last
File.WriteAllText("../output.json", JsonSerializer.Serialize(output, new JsonSerializerOptions
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
}));

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

    SearchStatements(rootBlock.Children);
    return;

    void SearchExpression(IExpressionNode expr)
    {
        switch (expr)
        {
            case ArrayInitNode arrInit:
                SearchExpressions(arrInit.Elements);
                break;
            case AssignNode assign:
                SearchExpression(assign.Value);
                break;
            case BinaryNode bin:
                SearchExpression(bin.Left);
                SearchExpression(bin.Right);
                break;
            case ConditionalNode cond:
                SearchExpression(cond.Condition);
                SearchExpression(cond.True);
                SearchExpression(cond.False);
                break;
            case FunctionDeclNode funcDecl:
                SearchStatement(funcDecl.Body);
                break;
            case NullishCoalesceNode nullCoalesce:
                SearchExpression(nullCoalesce.Left);
                SearchExpression(nullCoalesce.Right);
                break;
            case ShortCircuitNode shortCircuit:
                SearchExpressions(shortCircuit.Conditions);
                break;
            case NewObjectNode newObj:
                SearchExpressions(newObj.Arguments);
                break;
            case ThrowNode throwNode:
                SearchExpression(throwNode.Value);
                break;
            case VariableCallNode varCall:
                SearchExpressions(varCall.Arguments);
                break;
            case FunctionCallNode funcCall:
                {
                    (Index key, Index val)? index = funcCall.Function.Name.Content switch
                    {
                        "gml_Script_msgsetloc" or "gml_Script_c_msgsetloc" => (2, 1),
                        "gml_Script_msgnextloc" or "gml_Script_c_msgnextloc" or "gml_Script_stringsetloc" => (1, 0),
                        "gml_Script_msgsetsubloc" or "gml_Script_c_msgsetsubloc" => (^1, 1),
                        "gml_Script_stringsetsubloc" or "gml_Script_msgnextsubloc" or "gml_Script_c_msgnextsubloc" => (^1, 0),
                        _ => null
                    };

                    if (index is not null)
                    {
                        // What is wrong with Chapter 3?
                        try
                        {
                            StringNode keyString = (StringNode)funcCall.Arguments[index.Value.key];
                            StringNode valueString = (StringNode)funcCall.Arguments[index.Value.val];

                            string key = keyString.Value.Content;
                            string text = valueString.Value.Content;

                            // Add the FIRST key - text pair in the LAST script (according to script loading order)
                            if (!lang.TryGetValue(key, out var val) || val.Item1 < scriptIndex)
                            {
                                if (!lang.ContainsKey(key))
                                    order.Add(key);
                                lang[key] = (scriptIndex, text);
                            }
                        }
                        catch (Exception e)
                        {
                            ScriptMessage($"exception while searching in function call {funcCall.Function.Name.Content}: {e}");
                        }
                    }
                    else
                        SearchExpressions(funcCall.Arguments);
                    break;
                }
        }
    }

    void SearchStatement(IStatementNode stmt)
    {
        if (stmt is IExpressionNode expr)
            SearchExpression(expr);

        switch (stmt)
        {
            case BlockNode block:
                SearchStatements(block.Children);
                break;
            case DoUntilLoopNode doUntil:
                SearchStatement(doUntil.Body);
                SearchExpression(doUntil.Condition);
                break;
            case ForLoopNode forLoop:
                if (forLoop.Initializer is not null) SearchStatement(forLoop.Initializer);
                if (forLoop.Condition is not null) SearchExpression(forLoop.Condition);
                if (forLoop.Incrementor is not null) SearchStatement(forLoop.Incrementor);
                SearchStatement(forLoop.Body);
                break;
            case IfNode ifNode:
                SearchExpression(ifNode.Condition);
                SearchStatement(ifNode.TrueBlock);
                SearchStatement(ifNode.ElseBlock);
                break;
            case RepeatLoopNode repeat:
                SearchStatement(repeat.Body);
                break;
            case ReturnNode ret:
                SearchExpression(ret.Value);
                break;
            case SwitchCaseNode switchCase:
                SearchExpression(switchCase.Expression);
                break;
            case SwitchNode switchNode:
                SearchExpression(switchNode.Expression);
                SearchStatement(switchNode.Body);
                break;
            case TryCatchNode tryCatch:
                SearchStatement(tryCatch.Try);
                SearchStatement(tryCatch.Catch);
                break;
            case WhileLoopNode whileLoop:
                SearchExpression(whileLoop.Condition);
                SearchStatement(whileLoop.Body);
                break;
            case WithLoopNode with:
                SearchStatement(with.Body);
                break;
        }
    }

    void SearchExpressions(List<IExpressionNode> exprs) => exprs.ForEach(SearchExpression);

    void SearchStatements(List<IStatementNode> stmts) => stmts.ForEach(SearchStatement);
}