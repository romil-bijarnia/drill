# Etude

A local trainer that moves you from copying C# to producing it. Full design in
SPEC.md. Coaching rules for Claude Code in CLAUDE.md. Session log in PROGRESS.md.

## Setup

Requires the .NET SDK (9 or later). Change `TargetFramework` in
`src/Etude/Etude.csproj` if you are on a different major version.

    cd src/Etude
    dotnet add package Spectre.Console

Stage 2 and 3 packages, when you get there:

    dotnet add package Microsoft.Data.Sqlite
    dotnet add package Microsoft.CodeAnalysis.CSharp.Scripting

## Starting a session

From the repo root:

    claude

First message: "Read SPEC.md and PROGRESS.md. Start me on stage 1."

`src/Etude/Program.cs` does not exist yet. Creating it is the first thing you do.

## Layout

    CLAUDE.md        coaching rules, read automatically by Claude Code
    SPEC.md          the design: modes, scheduling, data, stack, build order
    PROGRESS.md      one line per signed-off session
    snippets/        one Markdown file per snippet (front matter + reference code)
    src/Etude/    the trainer (yours to write)
    tests/           test projects, when you add them
