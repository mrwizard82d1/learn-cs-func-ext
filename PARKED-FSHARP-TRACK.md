# PARKED — the F# track

**Status: deliberately deferred, 2026-08-23. Trigger: November 2026.**
**Temporary home.** This belongs in its own repo; it lives here (and as a pointer in `../learn-language-ext`) until that repo exists.

---

## The ambition

Learn FP concepts → illustrate them in C# → **propose F#, with functional core / imperative shell, for production code eventually.** Twenty-plus years of C#; F# as a culmination, not a detour.

## Why it's parked

**1. Bandwidth.** Three active learning tracks already — CSharpFunctionalExtensions, LanguageExt, AI/Claude. A fourth starves all four. Serialize.

**2. CFE is a poor rung, so nothing is lost by decoupling them.** CFE's product decision is "feel like ordinary C#," which makes it the *least* F#-shaped option available:

| F# idea | CFE builds the muscle? |
|---|---|
| `option`, `Result`, railway-oriented composition | yes — and F# has `Result` built in, so it transfers |
| **Discriminated unions** | **no** — the single biggest F# idea |
| **Exhaustive matching** | **no** |
| Computation expressions | no |
| Applicative validation | no |
| Currying, partial application, `\|>`, `>>` | no |
| Immutability by default | no |

Worth noting the concepts are F#-*derived* — railway-oriented programming is Scott Wlaschin's, and Khorikov's whole 2015 argument is "here's what F# taught me, in C#." CFE is mechanically alien to F#, not conceptually alien. **LanguageExt is the F#-shaped one** (an explicit F#/Haskell port), and there are already four phases of it in `../learn-language-ext`.

Also: functional core / imperative shell needs neither F# nor a library. It's an architecture — plain C# records and pure static functions get you there.

**3. The trigger — this is the real reason, not procrastination.** **C# 15 ships `union` types with .NET 11, GA expected November 2026** (in preview since .NET 11 Preview 2, April 2026, behind `<LangVersion>preview</LangVersion>`).

The strongest technical case for F# at a C#-committed shop has always been *"we need discriminated unions and exhaustive matching to model our domain honestly."* In November that argument either evaporates or gets much sharper — and either outcome changes the proposal. Since the ambition is **production code**, not a bounded tool, that's information worth having before spending a year building a case.

Caveats: they're *type* unions (case types are types; discriminated unions expressed via fresh type declarations), so not identical to F#. Preview dates slip.

Sources: [.NET blog](https://devblogs.microsoft.com/dotnet/csharp-15-union-types/) · [MS Learn spec](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/unions)

## What to do when it unpins

1. **Evaluate C# 15 unions honestly**, against a real domain — ideally the expense tracker, so it's directly comparable to what you built in both tutorials. Do they close the gap? Where do they fall short of F#?
2. **Then decide the vehicle.** If unions land well, the F# case narrows to computation expressions, immutability-by-default, and type inference — a real but much smaller argument, and one better made from a functional-core-in-C# position.
3. **If F# still earns it:** Scott Wlaschin, *[Domain Modeling Made Functional](https://pragprog.com/titles/swdddf/domain-modeling-made-functional/)* (2018) — DDD, functional core, and F#, which is precisely this ambition. Plus a small F# project and `FsToolkit.ErrorHandling`, whose `validation` computation expression natively closes the accumulation gap that CFE can't (see `article/02-going-deeper.md` §6).
4. **Scope the ask realistically.** "F# in production" is a large organizational request against a large existing C# codebase. The ladder that actually gets climbed: bounded internal tool → analysis utility or test DSL → a leaf service. Never a migration.

## Honest note to future me

The recommendation to decouple this from CFE was Claude's, and it was received as a bitter pill. Worth recording that the disappointment was about *organizational* constraints — hiring, tooling, an existing codebase — not about the merit of the idea. Those constraints are real but they are not permanent, and the pull toward F# was reading the industry correctly, not away from it: C# has spent a decade importing F#'s ideas, and unions are the biggest one yet.
