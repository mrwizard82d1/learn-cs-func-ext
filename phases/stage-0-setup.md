# Stage 0 — Setup

> Summary in [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is the detailed walkthrough.

## How to use this file

- Step headers end with `[ ]`. Flip to `[x]` when complete — I'll remind you.
- First unchecked step = your resume point.
- "Notes & questions" at the bottom is yours.
- **Test runs:** Rider's built-in runner ("Run All Tests from Solution") or `dotnet test` from the CLI.

## Goal

Stand up a .NET 10 solution structurally identical to learn-language-ext's, with:

- `src/Expenses/` — production code, references `CSharpFunctionalExtensions`
- `tests/Expenses.Tests/` — xUnit 2 tests, gets the package transitively
- A smoke test proving xUnit **and** CFE are both wired up

Nothing here should be novel. That's the point: the environment is a solved problem so all of Stage 1's novelty is the library.

## Decisions made

- **Same toolchain as learn-language-ext**, deliberately: .NET 10 pinned via `global.json`, xUnit 2 (classic VSTest), classic `.sln`, Rider via Gateway. Every one of those was argued out in the other project; none of it gets re-litigated here.
- **Solution name `LearnCfe.sln`, not `Expenses.sln`.** Stage 1 reuses the expense domain, but Stage 2 will add a *different* domain to the same solution. Naming the solution after the tutorial rather than the first domain avoids a rename later. (Override this if you'd rather keep one solution per domain.)
- **`CSharpFunctionalExtensions` 3.7.0** — latest stable (2026-03-02), MIT, **zero dependencies**. Targets `netstandard2.0`/`net6.0`/`net8.0`; a `net10.0` project consumes the `net8.0` assets. There is no v4-vs-v5 fork in the road here (contrast LanguageExt) — 3.x has been the line since 2021 and the API is additive.
- **Main package only.** The companions (`.Analyzers`, `.FluentAssertions`, `.HttpResults`) are evaluated in Stage 1 Part 8, added then if wanted — not up front.
- **Test-project csproj is copied, not templated.** See Step 2's note on template drift.

---

## Steps

### Step 1 — Create the solution structure  `[x]`

Target layout:

```
learn-cs-func-ext/
├── global.json
├── LearnCfe.sln
├── src/
│   └── Expenses/
│       └── Expenses.csproj
└── tests/
    └── Expenses.Tests/
        └── Expenses.Tests.csproj
```

Copy `global.json` straight across:

```
cp ../learn-language-ext/global.json .
```

It pins `10.0.107` with `rollForward: latestFeature`; your installed SDK is 10.0.111, so that resolves fine.

Then, from the repo root:

```
dotnet new sln -n LearnCfe --format sln
dotnet new classlib -n Expenses -o src/Expenses
dotnet new classlib -n Expenses.Tests -o tests/Expenses.Tests
dotnet sln add src/Expenses tests/Expenses.Tests
```

Two things to notice:

- **`--format sln`** is required. .NET 10's `dotnet new sln` defaults to `.slnx`, which your Gateway remote Solution-File picker still rejects (retested 2026-08-08 against Gateway 2026.2.1 with the Rider 2026.2.0.2 backend — the file shows red at *pick* time, before the backend ever loads it). Classic `.sln` it is.
- **`classlib` for the test project too**, not `dotnet new xunit`. Reason in Step 2.

Delete both generated `Class1.cs` placeholders.

### Step 2 — Wire up the test project  `[x]`

The .NET 10 `dotnet new xunit` template's xUnit version has moved around (that's how learn-language-ext ended up on xUnit v3 + MTP and then had to migrate back). Rather than template-then-fix, copy the csproj that's already known-good:

```
cp ../learn-language-ext/tests/Expenses.Tests/Expenses.Tests.csproj tests/Expenses.Tests/
cp ../learn-language-ext/tests/Expenses.Tests/xunit.runner.json tests/Expenses.Tests/
```

That file already has everything you want — `net10.0`, `Nullable`/`ImplicitUsings` enabled, `IsTestProject`, `<Using Include="Xunit" />`, the `xunit.runner.json` content copy, and the pinned trio:

| Package | Version |
|---|---|
| `Microsoft.NET.Test.Sdk` | 17.14.1 |
| `xunit` | 2.9.3 |
| `xunit.runner.visualstudio` | 3.1.4 |

The copied file's `<ProjectReference>` and `<RootNamespace>` will point at learn-language-ext's paths — both happen to be correct here (`..\..\src\Expenses\Expenses.csproj`, `Expenses.Tests`) because the layout is identical. Check that rather than assume it.

Also confirm `src/Expenses/Expenses.csproj` has `<Nullable>enable</Nullable>` and `<ImplicitUsings>enable</ImplicitUsings>`. Keep nullable reference types **on** for the whole tutorial: one of the questions you're implicitly answering for your team is *"where does `Maybe<T>` earn its keep over plain `string?`"*, and you can't feel that with NRTs switched off.

### Step 3 — Add the package  `[x]`

```
dotnet add src/Expenses package CSharpFunctionalExtensions
```

That should resolve to **3.7.0**. If it lands on something else, note the actual version in **Decisions made** above — the docs in this repo were written against 3.7.0's source and I'd want to know about drift.

Add it to the **test** project as well:

```
dotnet add tests/Expenses.Tests package CSharpFunctionalExtensions
```

This is a departure from learn-language-ext, where the test project got LanguageExt transitively. Here you'll be writing `Maybe<T>`/`Result<T>` in test-local helpers and inline types constantly, and a direct reference makes that intent explicit rather than accidentally-transitive. It's also what a real test project would do.

### Step 4 — Write the smoke test  `[ ]`

Create `tests/Expenses.Tests/SmokeTests.cs`:

```csharp
namespace Expenses.Tests;

using CSharpFunctionalExtensions;

public class SmokeTests
{
    [Fact]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void MaybeIsAvailable()
    {
        Maybe<int> answer = Maybe.From(42);

        Assert.True(answer.HasValue);
        Assert.Equal(42, answer.GetValueOrThrow());
    }

    [Fact]
    public void ResultIsAvailable()
    {
        Result<string> ok = Result.Success("wired up");

        Assert.True(ok.IsSuccess);
        Assert.Equal("wired up", ok.Value);
    }
}
```

Three things worth noticing already, all of which get their own treatment later:

- **No `using static` prelude.** LanguageExt hands you `Some`/`None`/`Right`/`Left` as free functions via `using static LanguageExt.Prelude;`. CFE has no prelude — everything is a static method on the type (`Maybe.From`, `Result.Success`) or an extension method. One `using CSharpFunctionalExtensions;` is the whole story. Smaller vocabulary to carry around; slightly noisier at the call site.
- **`answer.GetValueOrThrow()` instead of `answer.Value`.** Both work and do exactly the same thing — `Value` is literally implemented as `=> GetValueOrThrow()`, and the library's own doc comment on it reads *"Try to use GetValueOrThrow() or GetValueOrDefault() instead for better explicitness."* Getting in the habit now costs nothing; Part 2 makes the case properly.
- **`ok.Value` on a `Result<T>`, though.** Result's `Value` throws `ResultFailureException` if you read it on a failure. In a test, right after asserting `IsSuccess`, that's fine and idiomatic. In production code inside a pipeline it's the thing you're trying to avoid. Different rules for different places — that distinction is a recurring theme.

### Step 5 — Verify  `[x]`

```
dotnet test
```

Three green. Then open the solution in Rider via Gateway and run them from the built-in runner too, so you know both paths work before you start relying on one.

**If the package won't restore:** check `dotnet nuget list source` — this is a fresh repo and nothing else here has pulled from nuget.org yet.

**If `Maybe<int> answer = Maybe.From(42);` won't compile:** you're on a pre-3.x version. `Maybe` (the non-generic entry point with the type-inferring `From<T>`) is a 3.x addition; older code used `Maybe<int>.From(42)`.

### Step 6 — Record what actually landed  `[ ]`

Update **Decisions made** with the real package version, and note anything that differed from these instructions. This section is the repo's record of "what was true when we started."

---

## Stretch (optional)

- Skim the [CFE README](https://github.com/vkhorikov/CSharpFunctionalExtensions) end to end. It's short — 20 minutes — and reading it *before* Part 1 means Part 1 becomes a second pass rather than a first, which is a better use of it. (Fair warning: the README leads with LINQ query syntax and with `.Value` in several examples. Both are things we'll push back on.)
- Open the v3.7.0 source for `Maybe<T>` ([`Maybe/Maybe.cs`](https://github.com/vkhorikov/CSharpFunctionalExtensions/blob/v3.7.0/CSharpFunctionalExtensions/Maybe/Maybe.cs)) — it's ~260 lines and you can read the *entire* type in one sitting. Compare that to the size of `LanguageExt.Option<A>`. That ratio is the library's central claim, and it's worth seeing with your own eyes before anyone tries to sell it to you (including me).
- Predict, before Part 2 tells you: what does `Maybe.From<string>(null)` do? LanguageExt's `Some(null)` throws `ValueIsNullException`. Write down your guess.

---

## Notes & questions

_Yours to fill in._

-
