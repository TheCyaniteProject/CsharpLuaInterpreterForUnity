
using System.Threading;
using UnityEngine;
using static LuaInterpreter.Lexer;
using static LuaInterpreter;

public class Test : MonoBehaviour
{
    CancellationTokenSource cts;

    [TextArea(5, 25)]
    public string input;
    [Space]
    public bool execute = false;

    // Start is called before the first frame update
    void Start()
    {
        // Create a cancellation token source that you could trigger if needed.
        cts = new CancellationTokenSource();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            cts.Cancel();
        }

        if (execute)
        {
            execute = false;
            Run();
        }
    }

    void Run()
    {
        // Create the environment and register built-in functions.
        Environment env = new Environment();
        BuiltIns.Register(env);

        // Create the evaluator.
        Evaluator evaluator = new Evaluator(env);

        // Your script as an array of lines:

        string[] script = input.Split('\n');

        // Create a cancellation token source that you could trigger if needed.
        cts = new CancellationTokenSource();

        ScriptRunner runner = new ScriptRunner(evaluator);
        runner.RunScript(script, cts.Token, false);
    }
}
