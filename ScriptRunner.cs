using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static LuaInterpreter.Lexer;

public class ScriptRunner
{
    private readonly Evaluator evaluator;

    public ScriptRunner(Evaluator evaluator)
    {
        this.evaluator = evaluator;
    }

    /// Runs an array of script lines in a separate thread.
    public void RunScript(string[] lines, CancellationToken cancellationToken, bool debugOutput = false)
    {
        string[] _lines = FixLines(lines);

        Task.Run(() => {
            foreach (var line in _lines)
            {
                line.Replace("\n", "");
                // Periodically check whether execution should be cancelled
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Script execution cancelled!");
                    return;
                }

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    // Tokenize, parse, and execute each line.
                    var lexer = new LuaInterpreter.Lexer(line);
                    var tokens = lexer.Tokenize();
                    var parser = new Parser(tokens);
                    Statement stmt = parser.ParseDeclaration();
                    // Execute statement:
                    evaluator.ExecuteStatement(stmt, evaluator.env);
                }
                catch (Exception ex)
                {
                    Debug.Log($"Error executing line '{line}': {ex.Message}");
                }
            }
        }, cancellationToken);
    }

    private string RemoveComments(string line)
    {
        // Remove everything after the Lua comment marker (“--”).
        line = line.Replace("'", "\"");
        int commentIndex = line.IndexOf("--");
        if (commentIndex >= 0)
        {
            return line.Substring(0, commentIndex);
        }
        return line;
    }

    private bool StartsBlock(string trimmedLine)
    {
        // Define which keywords open a block.
        // You can add additional keywords as needed (like "for", "while", etc.)
        return trimmedLine.StartsWith("if ") ||
               trimmedLine.StartsWith("for ") ||
               trimmedLine.StartsWith("while ") ||
               trimmedLine.StartsWith("function ") ||
               trimmedLine.StartsWith("local function");
    }

    public string[] FixLines(string[] lines)
    {
        var fixedLines = new List<string>();
        var blockBuilder = new StringBuilder();
        int blockDepth = 0;

        foreach (var originalLine in lines)
        {
            // Remove any comments and trim whitespace.
            string noCommentLine = RemoveComments(originalLine);
            string trimmedLine = noCommentLine.Trim();

            // Skip blank lines.
            if (string.IsNullOrWhiteSpace(trimmedLine))
                continue;

            // If block starts now and we're not in any block
            if (blockDepth == 0 && StartsBlock(trimmedLine))
            {
                blockDepth = 1;
                blockBuilder.Append(trimmedLine + " ");
                continue;
            }
            // If we are inside a block
            if (blockDepth > 0)
            {
                // If a new block starts, increment the depth.
                if (StartsBlock(trimmedLine))
                {
                    blockDepth++;
                }

                blockBuilder.Append(trimmedLine + " ");

                // If this line is exactly "end", treat it as closing the innermost block.
                // (Note: You might later want to handle 'else' separately if further parsing is needed.)
                if (trimmedLine.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    blockDepth--;
                    // Once all blocks have closed, flush the accumulated block.
                    if (blockDepth == 0)
                    {
                        fixedLines.Add(blockBuilder.ToString().Trim());
                        blockBuilder.Clear();
                    }
                }
                continue;
            }

            // If we are not in a block and this is not a block start, simply add the line.
            if (blockDepth == 0)
            {
                fixedLines.Add(trimmedLine);
            }
        }

        // In case a block was not closed, flush it.
        if (blockBuilder.Length > 0)
        {
            fixedLines.Add(blockBuilder.ToString().Trim());
        }

        return fixedLines.ToArray();
    }
}