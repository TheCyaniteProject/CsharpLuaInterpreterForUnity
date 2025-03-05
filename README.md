# Unity Lua Interpreter

A lightweight Lua interpreter implemented in C# for Unity 2022.3.11f1 and higher.  
This project is licensed under the GNU General Public License v3.

---

## Overview

The Unity Lua Interpreter is an embeddable Lua scripting engine built in C# specifically for Unity projects. It supports many of Lua's core features including:
- Arithmetic expressions with proper operator precedence and grouping.
- String concatenation.
- Relational operators (==, ~=, <, <=, >, >=).
- Logical operators (and, or, not) with Lua’s short-circuit behavior.
- Boolean literals (true, false) and nil.
- Variable assignments that gracefully return nil for undefined variables.
- Function definitions (local and global) including recursive functions.
- Multi-return capabilities (supporting functions that return more than one value).

The interpreter includes a built-in environment for registering additional functions (like math functions and print) and supports a multi-line parsing system with block grouping (for if statements, function definitions, etc.). It is designed as a learning and prototyping tool for integrating Lua scripting into Unity applications.

---

## Features

- **Arithmetic & Grouping**  
  + Supports operator precedence (e.g., multiplication before addition) and grouping via parentheses.

- **String Concatenation**  
  + Uses the Lua `..` operator for concatenating strings.

- **Relational Operators**  
  + Implements `==`, `~=`, `<`, `<=`, `>`, and `>=` for numerical and boolean comparisons.

- **Boolean and Nil Literals**  
  + Recognizes `true`, `false`, and `nil` (returns nil rather than throwing an error when an undefined variable is referenced).

- **Logical Operators**  
  + Supports `and`, `or`, and `not` with proper short-circuit evaluation semantics.

- **Control Flow**  
  + Implements if/then/else/end statements and nested control structures.

- **Functions and Recursion**  
  + Allows function definitions (both local and global) including recursion.
  + Supports multi-return values for functions.

- **Built-in Functions**  
  + Includes a basic `print` function and a framework for adding additional built-in functionality (e.g., math library functions).

- **Multi-line Parsing & Block Grouping**  
  + A custom `FixLines` utility that groups code blocks (e.g., multi-line function definitions and if statements), and strips out Lua comments (`--`).

- **Unity Integration**  
  + Developed and tested with Unity 2022.3.11f1, ensuring compatibility with modern Unity projects.

---

## Getting Started

### Prerequisites

- Unity 2022.3.11f1 (or later versions may work)
- .NET (compatible with Unity's C# version)

### Installation

1. Clone this repository:
   ```
   git clone https://github.com/yourusername/UnityLuaInterpreter.git
   ```
2. Copy the interpreter source files into your Unity project's Assets folder.
3. Add any dependencies if necessary (this project is self-contained).

### Usage

- **Running a Script:**  
  Add Test.cs to a gameobject in your scene, and paste lua code into the TextArea labeled Input.
See below for an Example Lua Script.

- **Embedding in Unity:**  
  Instantiate the interpreter’s environment and evaluator, then pass script lines to the `ScriptRunner` class.  
  Use CancellationTokens if you need to cancel script execution.
  ```
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
  ```

---

## Example Lua Script

```lua
-- Arithmetic, grouping, and concatenation
a = 3 + 4 * 2       -- 3 + (4 * 2) = 11
b = (3 + 4) * 2     -- (3 + 4) * 2 = 14
print("a =", a, "b =", b)

-- Relational and boolean operators
print("1 == 1:", 1 == 1)
print("1 ~= 2:", 1 ~= 2)
print("a < b:", a < b)
print("a > b:", a > b)

-- Undefined variable yields nil
print("Undefined variable s:", s)
print("1 == s:", 1 == s)

-- Logical operators
if 1 == 1 and 2 == 2 then
    print("Logical AND: true")
else
    print("Logical AND: false")
end

if 1 == 2 or 2 == 2 then
    print("Logical OR: true")
else
    print("Logical OR: false")
end

if not false then
    print("Logical NOT: true")
end

-- Function definitions and recursion
local function factorial(n)
    if n == 0 then
        return 1
    else
        return n * factorial(n - 1)
    end
end

print("Factorial of 5 is", factorial(5))

-- Multi-return values
local function multiReturn(a, b)
    return a + b, a * b
end

sum, product = multiReturn(3, 4)
print("Sum:", sum, "Product:", product)
```

---

## Contributing

Contributions are welcome! If you would like to improve the interpreter or add new features, please feel free to open issues or submit pull requests. Please make sure your contributions adhere to the GPL v3 license.

---

## License

This project is licensed under the GNU General Public License v3.  
See [LICENSE](LICENSE) for details.

---

## Acknowledgments

- Inspired by Lua’s design and lightweight scripting capabilities.
- This project contains code that was generated by OpenAI's o3-mini LLM.
