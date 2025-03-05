using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static LuaInterpreter;

public class LuaTestRunner : MonoBehaviour
{
    // A list to collect test results.
    private List<string> testResults = new List<string>();

    void Start()
    {
        Debug.Log("------------ Lua Tests Summary ------------");
        RunTests();
        foreach (string result in testResults)
        {
            Debug.Log(result);
        }
    }

    void RunTests()
    {
        try
        {
            LuaInterpreter.Environment env = new LuaInterpreter.Environment();
            BuiltIns.Register(env);

            // Override the print function to capture output.
            List<string> capturedOutput = new List<string>();
            env.Define("print", new Func<List<object>, object>((args) =>
            {
                string line = "";
                foreach (object arg in args)
                {
                    line += (arg != null ? arg.ToString() : "nil") + " ";
                }
                line = line.Trim();
                capturedOutput.Add(line);
                Debug.Log(line);
                return null;
            }));

            // Create the evaluator with our environment.
            Lexer.Evaluator evaluator = new Lexer.Evaluator(env);

            // Define our tests as pairs of (test name, array of Lua code lines).
            // Each test script is an array of lines that will be run in sequence.
            List<(string testName, string[] scriptLines)> tests = new List<(string, string[])>
            {
                ("Arithmetic and Grouping Test",
                    new string[]
                    {
                        "a = 3 + 4 * 2",      // multiplication before addition: a = 11
                        "b = (3 + 4) * 2",    // grouping: b = 14
                        "print('a =', a, 'b =', b)"
                    }
                ),
                ("Relational Operators Test",
                    new string[]
                    {
                        "print('1 == 1:', 1 == 1)",
                        "print('1 ~= 2:', 1 ~= 2)",
                        "print('a < b:', a < b)"
                    }
                ),
                ("Nil and Undefined Variable Test",
                    new string[]
                    {
                        "print('s:', s)",      // s is undefined and should return nil
                        "print('1 == s:', 1 == s)"
                    }
                ),
                ("Logical Operators Test",
                    new string[]
                    {
                        "if 1 == 1 and 2 == 2 then",
                        "   print('and: true')",
                        "else",
                        "   print('and: false')",
                        "end",
                        "if 1 == 2 or 2 == 2 then",
                        "   print('or: true')",
                        "else",
                        "   print('or: false')",
                        "end",
                        "if not false then",
                        "   print('not: true')",
                        "end"
                    }
                ),
                ("Function and Recursion Test",
                    new string[]
                    {
                        "local function factorial(n)",
                        "   if n == 0 then",
                        "      return 1",
                        "   else",
                        "      return n * factorial(n - 1)",
                        "   end",
                        "end",
                        "print('factorial(5):', factorial(5))" // Expected 120
                    }
                ),
                ("Multi-Return Test",
                    new string[]
                    {
                        "local function multiReturn(a, b)",
                        "   return a + b, a * b",
                        "end",
                        "sum, product = multiReturn(3, 4)",
                        "print('sum:', sum, 'product:', product)" // Expected: sum = 7 and product = 12
                    }
                ),
                ("Nested if/else Test",
                    new string[]
                    {
                        "if 1 == 1 then",
                        "   if 2 == 3 then",
                        "      print('nested: inner if (should not print)')",
                        "   else",
                        "      print('nested: inner else')",
                        "   end",
                        "end"
                    }
                ),
                ("String Concatenation Test",
                    new string[]
                    {
                        "name = 'Alice'",
                        "greeting = 'Hello ' .. name .. '!'",
                        "print(greeting)"
                    }
                )
            };

            // Loop through each test, process its script lines, and run them.
            foreach (var (testName, scriptLines) in tests)
            {
                capturedOutput.Clear();
                bool testPassed = true;
                string errorMessage = "";

                try
                {
                    // Use the FixLines utility (if available) to join multi-line blocks (e.g., if/then/end blocks).
                    ScriptRunner runner = new ScriptRunner(evaluator);
                    string[] fixedLines = runner.FixLines(scriptLines);
                    foreach (string line in fixedLines)
                    {
                        // Remove any newline characters
                        string trimmedLine = line.Replace("\n", "").Trim();
                        if (string.IsNullOrEmpty(trimmedLine))
                            continue;

                        // Tokenize, parse, and execute the line.
                        Lexer lexer = new Lexer(trimmedLine);
                        var tokens = lexer.Tokenize();
                        Lexer.Parser parser = new Lexer.Parser(tokens);
                        Lexer.Statement stmt = parser.ParseDeclaration();
                        evaluator.ExecuteStatement(stmt, evaluator.env);
                    }
                }
                catch (Exception ex)
                {
                    testPassed = false;
                    errorMessage = ex.Message;
                }

                // Log the test result.
                string result = $"{testName}: " + (testPassed ? "PASS" : "FAIL");
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    result += " - " + errorMessage;
                }
                testResults.Add(result);
                Debug.Log(result);
                if (!testPassed)
                {
                    Debug.Log($"Error in Block: [ {string.Join(", ", scriptLines)} ]");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error during test setup: " + ex.Message);
        }
    }
}