# Learn CSharpFunctionalExtensions — Tutorial Plan

> **Re-scoped 2026-08-23.** This project used to carry two goals: learn CFE *and* serve as a rung toward F#. It's a poor rung (see [`PARKED-FSHARP-TRACK.md`](PARKED-FSHARP-TRACK.md)), so the F# ambition is now a separate, parked track. What's left here is sharper for it.

## Goal

**Introduce your C# teammates to functional concepts, using CSharpFunctionalExtensions as the vehicle** — and decide, from real experience, whether it's a tool you'd actually put in front of them.

That's a **Teach** goal, which is the third step of your own See–Do–Teach model and the one you've said you can't schedule. You can't schedule the *audience*, but you can build the material. So this project now aims at a concrete deliverable rather than at fluency for its own sake.

By the end you should be able to:

- Write everyday C# in CFE's idiom without looking things up — `Maybe<T>`, the `Result` railway, the pipeline vocabulary.
- Say precisely where CFE is *enough*, where nullable reference types are enough, and where LanguageExt earns its weight.
- Hand a teammate something that gets them productive — which, per the 2026-08-22 finding that **CFE has no on-ramp**, does not currently exist anywhere.

## The learning model: See — Do — Teach

| | Phase | Here | Mode |
|---|---|---|---|
| **See** | Watch someone competent demonstrate it | **Stage 1** — [`article/`](article/) | Read, then re-read while typing. Not TDD. |
| **Do** | Apply it yourself | **Stage 2** — CSV → report CLI (compressed) | Strict TDD, low guidance. |
| **Teach** | Give it to someone else | **Stage 3** — the onboarding artifact | The deliverable. |

Two consequences worth keeping stated:

- **Stage 1 is deliberately not TDD.** TDD is a *design* discipline — the wrong tool for touring an API you've never seen. Stage 1 tests are executable notes; red-green-refactor resumes at Stage 2, where it belongs.
- **Teach now has a deliverable**, so it no longer depends on an opportunity arriving. If one does, you're ready; if it doesn't, you still produced the thing.

### How this meshes with your 6-step loop

| Your step | See/Do/Teach | Here |
|---|---|---|
| 1. Become curious | — | ✅ done |
| 2. Search for online articles | See | ✅ done 2026-08-22 — and the finding *was* the finding: there's no usable on-ramp |
| 3. Read an article showing "how to do it" | See | **Stage 1**, first pass |
| 4. Re-read it while typing | See → Do | **Stage 1**, second pass |
| 5. Try a small project | Do | **Stage 2** |
| 6. Prototype a demonstration | Teach | **Stage 3** |

## Stage 1 — SEE: the articles

Domain: the **expense tracker** from learn-language-ext, on purpose. Zero domain load means every surprise is an *API* surprise, and the LanguageExt-vs-CFE diff shows up line by line.

**Read first: [`article/00-khorikov-series.md`](article/00-khorikov-series.md)** — a guided, critical read of Khorikov's 2015 "Functional C#" series (~1 hour). It's the *why* behind the library, which is what Stage 3 needs; reading it first also makes Article A a second exposure rather than a first.

**Article A — [`article/01-the-working-tour.md`](article/01-the-working-tour.md)** *(written)*

| Part | Topic |
|---|---|
| 1 | Why the library exists, what's in it, the LanguageExt diff |
| 2 | `Maybe<T>` — construction, consumption, `Map`/`Bind`/`Where`, collection helpers, the `.Value` hatch |
| 3 | `Result` / `Result<T>` — the railway, and `ToResult` as the bridge |
| 4 | The pipeline vocabulary — `Tap`, `TapError`, `Check` vs `Ensure` vs `Bind`, `Compensate`, `Finally`, `*If`, `*Try` |

**Article B — [`article/02-going-deeper.md`](article/02-going-deeper.md)** *(written)*

| Part | Topic |
|---|---|
| 5 | Typed errors — `Result<T, E>`, `UnitResult<E>`, `MapError`, the implicit-conversion traps |
| 6 | Error accumulation — `Result.Combine`, `ICombine`, vs LanguageExt `Validation`. **The real gap.** |
| 7 | Domain modeling — `ValueObject`, `Entity<TId>`, `Create` factories, and `record` vs `ValueObject` |
| 8 | Async and tooling — `Task`/`ValueTask` overloads, the companion packages, the verdict |

Each part ends with **predict-then-check** and **explain-it-back**. The explain-it-back prompts matter more than before: they're first drafts of Stage 3.

> ⚠️ **Part 5 has a November expiry.** Its sealed-record error hierarchies are C#'s best available approximation of a discriminated union — and C# 15 ships real `union` types in **November 2026**. Revisit that part then; the workaround becomes a footnote.

## Stage 2 — DO: maintain a connector *(compressed)*

**Retargeted 2026-09-05** from a generic CSV→report CLI to a **connector under maintenance** — because that's the shape of the actual work, in both senses: what a connector *does*, and how change *arrives*.

Synthetic source and target formats. Invented systems, no product code. It stays a learning exercise.

### Why a connector

| Connector stage | What the material says |
|---|---|
| Read from the source system | untrusted external data → **parse at the boundary** |
| Translate / map fields | a pure transformation → **the functional core** |
| Write to the target system | I/O → **the imperative shell** |
| Bad record, missing field, unmappable code | routine, expected failure → **`Result`, not exceptions** |
| Several fields wrong in one record | independent errors → **the accumulation problem** (Article B §6) |
| 900 of 1000 records succeeded | partial success → needs a real error-collection strategy |

### Brownfield on purpose

Greenfield would be the wrong drill. The organization has existed since 1980 and has *many* working connectors that need maintenance, not rebuilds. The realistic unit of change is "I'm already touching this mapping for a ticket" — never "let's rewrite it." And the strongest argument against rewriting isn't sunk cost: a connector that's run for fifteen years encodes hundreds of edge cases nobody wrote down, discovered one support ticket at a time. A rewrite discards that and rediscovers it at the customer's expense.

So:

**Claude writes the starting point.** A plausible legacy connector — the kind a competent developer wrote under deadline in 2015. Imperative, `null` guards, a `catch` that swallows, magic strings, validation interleaved with I/O, one long method. Realistically bad, not strawman bad: it works on the happy path. **With few or no tests.**

> ⚠️ This is the one place Claude writes production-shaped code in this repo. It's the *legacy artifact*, not the learning code — everything from the first ticket onward is Larry's to type. Larry's request, 2026-09-05.

Two consequences worth naming:

- **Unfamiliar code is the point.** You can't lean on remembering your own intent, which is exactly the maintenance condition.
- **No tests makes baseline-first real.** Your first move isn't a refactor — it's characterization tests to pin current behaviour before touching anything. That's your own documented working style, applied where it actually bites.

### Change arrives as tickets

Not as "now add typed errors." As it arrives on the board:

> *"Customer A hit this issue applying the connector to their process…"*
> *"Customer B is using it a different way and…"*

**Claude issues tickets one at a time and does not reveal them in advance.** Seeing the sequence would let you design for it, and real maintenance is blind. Each ticket applies pressure to a different seam; whether your functional core absorbs it well is the only honest test of the design.

Rhythm per ticket: baseline → characterization test if needed → behaviour-preserving refactor as its own commit → then the change. Strict TDD from the first ticket on; red is a runnable assertion failure, never a compile error.

**Compressed** means one connector, one source format, a handful of tickets — enough to have felt it, not a portfolio piece.

The by-product is the Stage 3 artifact: a **before/after pair from the same file**, which is far more persuasive to a maintenance-bound team than any greenfield example.

## Stage 3 — TEACH: the artifact that doesn't exist

The deliverable. Not a demo app — a **written on-ramp** for a C# developer who has never seen any of this, of the kind you went looking for on 2026-08-22 and couldn't find.

Rough shape (settle it when you get there):

- The 20-minute version: `Maybe` and `Result`, one domain, no jargon.
- **The chapter nobody writes: how to use this in maintenance work on code you didn't write and can't rewrite.** Every tutorial in this space — Khorikov's included — assumes greenfield. Your team is maintenance-bound, so this is the section that makes the artifact worth reading. Seam-level adoption, not transformation.
- The decision tables that took you longest to internalize — `Map`/`Bind`/`Ensure`/`Check`/`Tap`, and `Result` vs `Result<T>` arity.
- The traps, because they cost you real time: the `.Value` hatch, `var` hiding the wrong result type, `Result.Failure<T>` being a method not a type.
- House rules you'd propose: where `.Value` is allowed, `Result<T>` inside a module vs `Result<T, E>` at its edge, what to do about accumulation.
- Honest weaknesses, volunteered before anyone finds them.

**Register note (from the 2026-09-05 reading):** Khorikov's 2015 series uses *Evans and Fowler* vocabulary — primitive obsession, immutability, illegal states unrepresentable — with no `monad`/`functor`/`applicative` anywhere. That's the register this artifact needs, because the team has the DDD vocabulary and not the FP one. Test for every sentence: *would Khorikov-in-2015 have written it this way?*

Its natural companion is [`ADOPTION.md`](ADOPTION.md) — the journal answers *should we*, the artifact answers *how*.

## `ADOPTION.md` — decision journal

Four lines per entry: **date · what I hit · what I concluded · what I'd tell the team.** The first three you'd want regardless; the fourth is what converts into Stage 3.

Adoption is more about relationships than technical knowledge — which is why the journal is the correct first artifact and not a lesser one. You can't have the relationship conversation until you have a real opinion, and you won't have one until Stage 2 has hurt a bit.

## Parked

- **The F# track** — see [`PARKED-FSHARP-TRACK.md`](PARKED-FSHARP-TRACK.md). Deferred deliberately, with a November 2026 trigger. Moves to its own repo when you create one.
- **OpCon RPA recon** — was Stage 4. Read-only reconnaissance for null/empty-guard and exception-as-control-flow sites in `../RPA`, written up, nothing committed there. Still a good idea; not part of this project's scope. Revisit after Stage 3.
- **Minimal-API capstone** — was Stage 3. `Result` → HTTP is where CFE looks best, and `CSharpFunctionalExtensions.HttpResults` exists for it. Cut because a demo app is a weaker teaching artifact than a written on-ramp, and you already know what it would prove.

## Setup notes

- **Project root**: `~/professional/projects/learn-cs-func-ext/` — note this is *not* under `~/source/repos`, so the `_laj` OneDrive backup job does **not** cover it. Durability here comes from `git push`.
- **Editor**: JetBrains Rider via Gateway. The backend (the thing with the version number you see) runs in WSL under `~/.cache/JetBrains/RemoteDev/dist/`; the Windows-side JetBrains Client is auto-matched. Toolbox's Rider updates refer to the *Windows-native* Rider and are unrelated.
- **Runtime**: .NET 10, pinned by `global.json` (`10.0.107`, `rollForward: latestFeature`; installed SDK 10.0.111).
- **Testing**: xUnit 2 (classic VSTest) — `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `Microsoft.NET.Test.Sdk` 17.14.1. Note `xunit.runner.visualstudio`'s 3.x version is unrelated to xUnit v3.
- **Solution**: classic `.sln` (`LearnCfe.sln`). **Not `.slnx`** — blocked by Gateway's remote Solution-File picker, retested twice. Settled.
- **Package**: `CSharpFunctionalExtensions` **3.7.0**, MIT, zero dependencies, targets `netstandard2.0`/`net6.0`/`net8.0`.

## What's in the box

Verified from the **v3.7.0** source.

| Area | Types |
|---|---|
| Optionality | `Maybe<T>` (+ non-generic `Maybe`, `IMaybe<T>`, `MaybeEqualityComparer`) |
| Failure | `Result`, `Result<T>`, `Result<T, E>`, `UnitResult<E>`, `ResultFailureException`, `ResultSuccessException` |
| DDD | `ValueObject`, `ValueObject<T>`, `ComparableValueObject`, `SimpleValueObject<T>`, `EnumValueObject`, `Entity<TId>` |
| Plumbing | `ICombine`, `Result.Configuration`, `Maybe.Configuration`, STJ converters |

The extension surface *looks* enormous but that's overload multiplication — every combinator ships sync, `Task`, `ValueTask`, and Left/Right async variants. About fifteen concepts, mostly four verbs (`Map`, `Bind`, `Tap`, `Check`) crossed with four suffixes (`…If`, `…Try`, `…Error`, `…Not`).

## LanguageExt → CFE

Your main translation axis, and the one thing you know that nobody on your team does.

| LanguageExt (4.4.9) | CFE (3.7.0) | Note |
|---|---|---|
| `Option<T>` | `Maybe<T>` | both structs; `default` is empty in both |
| `Some(x)` — throws on null | `Maybe.From(x)` — null → **`None`** | CFE's constructor behaves like `Optional(x)`. No throwing constructor exists. |
| *no `.Value`* | `.Value` **exists** and throws | the central philosophical difference |
| `Either<L, R>` | *(none)* | no unbiased two-case type |
| `Fin<T>` / `Error` | `Result<T>` (string) or `Result<T, E>` | no error hierarchy; `E` is yours |
| `Validation<F, S>` + `Apply` | `Result.Combine` / `ICombine` | **the real gap** — no applicative accumulation |
| `Map`/`Bind`/`Match`/`Filter`/`IfNone` | `Map`/`Bind`/`Match`/`Where`/`GetValueOrDefault` | same semantics, different names |
| *(nothing)* | `Tap`, `Check`, `Ensure`, `Compensate`, `Finally`, `*If`, `*Try` | **genuinely additive** |
| `Map<K,V>`, `Seq<T>`, `Lst<T>` | *(none)* | no persistent collections |
| `Eff`/`Aff`, HKT traits, `IO<A>` | *(none)* | that absence *is* the pitch |
| `Unit` | *(none)* | `Result` and `UnitResult<E>` cover it |

**CFE is one-tenth the surface area, one escape hatch looser, missing applicative validation, and better at pipelines.**

## Repository layout

```
TUTORIAL_PLAN.md            ← this file
ADOPTION.md                 ← decision journal
PARKED-FSHARP-TRACK.md      ← the deferred F# ambition + November trigger
article/
  00-khorikov-series.md     ← guided read of the 2015 source material + capture slots
  01-the-working-tour.md    ← Stage 1, Article A
  02-going-deeper.md        ← Stage 1, Article B
phases/
  stage-0-setup.md          ← complete
  stage-2-*.md              ← authored when we get there
src/ tests/                 ← the code you type
```

## Style

- **You type the code.** I describe what to write and why; no scaffolding on your behalf unless you ask.
- **Annotate result types while learning.** `var` hides which of the four result types you got, and the error then surfaces at an overload set far from the cause. Go back to `var` once they're reflexive.
- **Fluent method chains, not query syntax** — except async, where query syntax genuinely wins.
- **Stage 2 onward: TDD**, baseline first. Smoke tests are permanent canaries.
- **Verified, not remembered.** API claims here are checked against v3.7.0 source. If something contradicts your compiler, the compiler wins.

## Resume protocol

- Step headers end with `[ ]`. **You** flip them; I remind you explicitly at the end of each step.
- Candidate test lists / type-along checklists: **I** maintain those.
- "Notes & questions" sections are yours.

## Decisions

**2026-08-22** — See–Do–Teach made the explicit spine. Stage 1 domain: expense tracker again. Stage 1 delivered as two whole articles. `ADOPTION.md` as a four-line-entry decision journal.

**2026-08-23** — Re-scoped. Goal narrowed to *introducing C# teammates to functional concepts*; F# split out to a parked track with a November 2026 trigger (C# 15 unions); Stage 2 compressed; minimal-API capstone and RPA recon moved to **Parked**; Stage 3 became the written on-ramp, motivated by the 2026-08-22 finding that no usable CFE tutorial exists.
