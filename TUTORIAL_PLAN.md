# Learn CSharpFunctionalExtensions — Tutorial Plan

## Goal

Learn **CSharpFunctionalExtensions** (CFE) well enough to (a) write everyday C# in its idiom without looking things up, and (b) decide for yourself whether — and how — you'd put it in front of your team.

By the end you should be able to:

- Reach for `Maybe<T>` instead of `null`/`""`/`-1` sentinels, and consume it without the `.Value` escape hatch.
- Express fallible operations as `Result` / `Result<T>` / `Result<T, E>` and compose them into a railway with `Bind` / `Ensure` / `Tap` / `Check`.
- Use CFE's **pipeline vocabulary** (`Tap`, `TapError`, `Check`, `Compensate`, `Finally`, the `*If` and `*Try` families) — the part of the library with no LanguageExt equivalent, and arguably its best idea.
- Model a domain with `ValueObject` / `SimpleValueObject<T>` / `Entity<TId>` and static `Create` factories returning `Result<T>` — Khorikov's "make illegal states unrepresentable" pattern, which is exactly the design consideration in your global instructions.
- Say precisely where CFE is *enough*, where nullable reference types are enough, and where LanguageExt earns its weight.

---

## The learning model: See — Do — Teach

This is the spine of the plan, stated explicitly because it drives design decisions that would otherwise look arbitrary.

| | Phase | Here | Mode |
|---|---|---|---|
| **See** | Watch someone competent demonstrate it | **Stage 1** — [`article/`](article/) | Read, then re-read while typing. Not TDD. |
| **Do** | Apply it yourself, under your own steam | **Stage 2** — CSV → report CLI | Strict TDD, low guidance, expected to hurt. |
| **Do → Teach** | Build the thing you'd *show* someone | **Stage 3** — minimal-API service | Self-directed. Review on request only. |
| **Teach** | Explain it to someone else | **[The Teach thread](#the-teach-thread)** | Opportunistic — but prepared for, not left to chance. |

Three design consequences, so they don't read as arbitrary later:

- **Stage 1 is deliberately not TDD.** TDD is a *design* discipline — the wrong tool for "show me what's in the box." Driving out an API you've never seen via failing tests just means writing tests you can't yet name. Stage 1 is **See**; tests there are executable notes that pin behavior, not design drivers. Red-green-refactor resumes in full at Stage 2, where it belongs, because Stage 2 is **Do**.
- **Stage 3 is yours alone.** A demonstration you'd show your team has to be *yours*, or it won't survive the first question anyone asks about it. That's not me stepping back for its own sake; it's the Do→Teach handoff working as intended.
- **Teach doesn't wait for an audience.** You said the last step has historically been opportunistic and you can't control it. True — but *readiness* is controllable. So the plan produces teaching material as a **byproduct** of Stages 1–3 rather than as a separate project, and adds cheap "teach-in-place" moves along the way. Details in [the Teach thread](#the-teach-thread) below.

### How this meshes with your actual 6-step loop

You also described the loop you really use for a new package. It's the same model with more resolution, and both fit:

| Your step | See/Do/Teach | Here |
|---|---|---|
| 1. Become curious | — | ✅ done. You found the package and it looked simpler than LanguageExt. Keep that motivation; it's the fuel. |
| 2. Search for online articles | See | [Reading list](#step-2-what-is-actually-out-there) below — honored, not replaced |
| 3. Read an article showing "how to do it" | See | **Stage 1**, first pass |
| 4. Re-read it while typing the code | See → Do | **Stage 1**, second pass |
| 5. Try a small project on your own | Do | **Stage 2** |
| 6. Prototype a demonstration | Do → Teach | **Stage 3** |

Two things this resolution buys us:

**Your step 3 wants a *whole article*, not a drip feed.** So Stage 1 is not eight tutorial installments — it's **two articles**, each complete and readable cover to cover in one sitting before you type a line. That's the loop you actually use, so that's what gets written.

**Steps 5 and 6 are different activities, and you already sensed it.** A small project is for *struggling*; a prototype demonstration is for *showing*. That's why the CSV CLI and the minimal API both belong here rather than one replacing the other.

### Step 2: what is actually out there

Real, worth skimming before Stage 1 so the article becomes your *second* pass rather than your first:

- Khorikov's **"Functional C#" series** (2015) on Enterprise Craftsmanship — the principles the library was built to serve:
  [Primitive obsession](https://enterprisecraftsmanship.com/posts/functional-c-primitive-obsession/) ·
  [Non-nullable reference types](https://enterprisecraftsmanship.com/posts/functional-c-non-nullable-reference-types/) ·
  [Handling failures, input errors](https://enterprisecraftsmanship.com/posts/functional-c-handling-failures-input-errors/)
- The [CFE README](https://github.com/vkhorikov/CSharpFunctionalExtensions) — short, example-driven, the closest thing to official docs.
- His Pluralsight course [*Applying Functional Principles in C#*](https://www.pluralsight.com/courses/csharp-applying-functional-principles) — the long-form version of the same argument.

**Why that isn't enough, honestly:** the blog series is eleven years old and argues for *principles* (immutability, no primitive obsession, no nulls, `Result` over exceptions) using a hand-rolled `Result` class. It predates `Result<T, E>`, `UnitResult<E>`, `Maybe`'s current shape, and the entire pipeline vocabulary (`Tap`/`Check`/`Compensate`/`Finally`/`*If`/`*Try`) that is most of what you'd actually type today. And nothing out there is written for someone who already knows LanguageExt and wants the diff. Hence Stage 1.

---

## Stage 1 — SEE: the article(s)

Domain: the **expense tracker** from learn-language-ext, on purpose. Reusing a domain you've already modeled means zero domain load, so every surprise is an *API* surprise — and it makes the LanguageExt-vs-CFE diff pop line by line. (Fresh domains are right for Stage 2 and for katas, where transfer is what's being tested. Stage 1 isn't testing anything.)

### Article A — [`article/01-the-working-tour.md`](article/01-the-working-tour.md)

Everything you need to write real CFE code. Read it through, then type it.

| Part | Topic |
|---|---|
| 1 | Why the library exists, what's in the box, and the LanguageExt diff in one page |
| 2 | `Maybe<T>` — construction, consumption, `Map`/`Bind`/`Where`, collection helpers, and an honest look at `.Value` |
| 3 | `Result` / `Result<T>` — the railway: `Success`/`Failure`/`Try`/`Of`, `Map`, `Bind`, `Ensure`, `Match`, and `ToResult` as the bridge from `Maybe` |
| 4 | The pipeline vocabulary — `Tap`, `TapError`, `Check` vs `Ensure` vs `Bind`, `Compensate`, `Finally`, the `*If` and `*Try` families |

### Article B — `article/02-going-deeper.md`

The parts you reach for once the basics are reflexive. Written after you've worked through A.

| Part | Topic |
|---|---|
| 5 | Typed errors — `Result<T, E>`, `UnitResult<E>`, `MapError`, `ConvertFailure`, and the implicit-conversion traps |
| 6 | Error accumulation — `Result.Combine`, `ICombine`, side by side with LanguageExt `Validation`. **The real gap.** |
| 7 | Modeling the domain — `ValueObject`, `SimpleValueObject<T>`, `EnumValueObject`, `Entity<TId>`, `Create` factories, parse-at-the-boundary |
| 8 | Async and tooling — `Task`/`ValueTask` overloads, `DefaultConfigureAwait`, LINQ over async results, the Analyzers / FluentAssertions / HttpResults companions |

Each part ends with a **predict-then-check** exercise and an **explain-it-back** prompt (see [Teach](#the-teach-thread)). Each article ends with an optional right-sized application challenge in a fresh domain — take it or skip it depending on energy.

## Stage 2 — DO: CSV ingest → report CLI

Fresh domain, your call which (a log parser, a bank-statement importer, a race-results tabulator — anything with dirty input and a summary at the end). Chosen deliberately because it hits the *hardest* part of CFE head-on: per-row validation with multiple independent errors, which is exactly where the missing `Validation` applicative bites and where `Result.Combine` has to earn its keep.

- **Strict TDD.** Red must be a runnable assertion failure, never a compile error. Baseline first. New types start inline in the test file, extracted as the first refactor after green.
- **Low guidance.** Requirements and concept hints from me; you drive. Asking questions isn't cheating — it's the mechanism.
- Walkthroughs get authored as `phases/stage-2-*.md` when we reach it, with candidate test lists.

## Stage 3 — DO → TEACH: minimal-API service

Your idea, and the right one. `Result` → HTTP is where the library pays off most visibly in the kind of code your team actually ships: `Finally(result => result.IsSuccess ? Ok() : BadRequest(result.Error))` collapses a pile of branching into one line, and the `CSharpFunctionalExtensions.HttpResults` companion exists specifically for this.

**You build this one end to end.** My role drops to review-on-request.

Scope suggestion for when you get there: one resource, three endpoints, one genuinely messy validation rule. Small enough to finish in a sitting or two; real enough that the `Result` railway visibly beats the `if`-ladder alternative. If you can put the two versions side by side, you have the demo.

## Stage 4 — optional, last: OpCon RPA recon

Your instinct — *"am I really going to learn a new tool and a very large codebase at once?"* — is right, and the answer is **don't**. But there's a version that turns the conflict into an advantage, and it belongs after Stage 3, not instead of it.

**Read-only reconnaissance, not implementation.** Go into `../RPA` and find real sites of what CFE exists to fix: repeated `null` / `IsNullOrEmpty` guards, magic `""`/`-1`/empty returns, exceptions as control flow, out-params. Write up 3–5, each with a sketch of the shape it *would* take. **Nothing gets committed to RPA.**

Three payoffs at once: you learn a slice of RPA (a goal you already have), you practice recognizing the smells (the actual transferable skill), and you get the concrete evidence any adoption argument needs — real code from your real product, not fruit-basket examples. Only after that, and only if the evidence is good, is a bounded pilot worth proposing: one new class, one new use case, one leaf module. Never a migration.

---

## The Teach thread

You can't schedule the moment someone asks you about this. You *can* be the person who already has the answer written down. So Teach is handled three ways, all cheap, all byproducts.

**1. Teach-in-place (during Stage 1–2).** At the end of each article part, an **explain-it-back** prompt: write two or three sentences, in your own words, aimed at an imagined mid-level teammate who has never heard of any of this. Not a summary of what you read — a *translation* of it. This is the Feynman move, and your learn-language-ext history says it works on you specifically: the `MapLeft`/`Bind` reconciliation in the library kata was the thing you found genuinely hard, and you got there by articulating it in your own words across several commits. That's Teach doing its job with an audience of zero.

**2. Artifacts that fall out anyway.**

| Artifact | Produced during | Becomes |
|---|---|---|
| [`ADOPTION.md`](ADOPTION.md) — decision journal | Stages 1–4, one entry at a time | the argument, if you ever want to make it |
| The LanguageExt ↔ CFE map (below) | Stage 1 | the thing *you specifically* can teach that nobody else on the team can |
| The Stage 3 service, if built two ways | Stage 3 | the 10-minute demo |
| The Stage 4 write-up | Stage 4 | evidence from your own product |

**3. `ADOPTION.md` is a decision journal — for now.** Your framing was right: adoption is more about relationships than technical knowledge, though they're intertwined. That's precisely why the journal is the *correct* first artifact and not a lesser one — you can't have the relationship conversation until you have a real opinion, and you won't have a real opinion until Stage 2 has hurt a bit.

So it gets written to convert cheaply. Each entry is four lines: **date · what I hit · what I concluded · what I'd tell the team.** The first three are notes you'd want regardless. The fourth is the one that becomes a circulated doc if the opportunity ever shows up — and if it doesn't, you've lost nothing.

Your global instructions already say the quiet part: adopt a library like this as a *deliberate, team-wide convention*, never smuggled in via a single bug fix. The journal is how you'd earn the right to propose the convention.

---

## Setup notes

Mirrors `../learn-language-ext` deliberately — same toolchain, so nothing about the *environment* is new and all the novelty is the library.

- **Project root**: `~/professional/projects/learn-cs-func-ext/`
- **Editor**: JetBrains Rider via Gateway Remote Development (Path D)
- **Runtime**: .NET 10, pinned with `global.json` (copied from learn-language-ext: `10.0.107`, `rollForward: latestFeature`; installed SDK is 10.0.111)
- **Testing**: xUnit 2 (classic VSTest) — `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.4, `Microsoft.NET.Test.Sdk` 17.14.1. Rider's built-in runner works; so does `dotnet test`.
- **Solution format**: classic `.sln`. **Not `.slnx`** — retested twice in learn-language-ext (most recently 2026-08-08, Gateway 2026.2.1 + Rider backend 2026.2.0.2) and it still fails at Gateway's remote Solution-File picker. Settled; don't retry.
- **Package**: `CSharpFunctionalExtensions` **3.7.0** (2026-03-02, MIT, **zero dependencies**, targets `netstandard2.0`/`net6.0`/`net8.0` — a `net10.0` project consumes the `net8.0` assets).
- **Companions, later and optional**: `.Analyzers` 1.4.1, `.FluentAssertions` 3.6.0, `.HttpResults` 1.2.1. Evaluated in Part 8; the analyzer matters more here than usual, for reasons that'll be obvious by then.

## What's actually in the box

Verified by reading the **v3.7.0** source tree, not from memory. The library is small — four areas:

| Area | Types |
|---|---|
| Optionality | `Maybe<T>` (+ non-generic `Maybe` entry point, `IMaybe<T>`, `MaybeEqualityComparer`) |
| Failure | `Result`, `Result<T>`, `Result<T, E>`, `UnitResult<E>`, `IResult`, `ResultFailureException`, `ResultSuccessException` |
| DDD building blocks | `ValueObject`, `ValueObject<T>`, `ComparableValueObject`, `SimpleValueObject<T>`, `EnumValueObject`, `Entity<TId>` |
| Plumbing | `ICombine`, `Result.Configuration`, `Maybe.Configuration`, System.Text.Json converters |

The extension surface *looks* enormous (hundreds of files) but that's overload multiplication — every combinator ships sync, `Task`, `ValueTask`, and "Left"/"Right" async variants. The **concept** count is about fifteen.

**On `Maybe<T>`:** `Bind`, `BindOptional`, `Map`, `Match`, `Where`, `Or`, `Tap`, `TapNoValue`, `Execute`, `ExecuteNoValue`, `Flatten`, `GetValueOrDefault`, `GetValueOrThrow`, `Select`, `SelectMany`, `Deconstruct`, `AsMaybe`, `AsNullable`, `ToList`, `ToResult`, `ToUnitResult`, `ToInvertedResult`, `Optional`, plus collection helpers `TryFirst`, `TryLast`, `TryFind`, `Choose`.

**On the `Result` family:** `Bind`, `BindIf`, `BindOptional`, `BindTry`, `BindZip`, `BindWithTransactionScope`, `Map`, `MapIf`, `MapTry`, `MapError`, `Ensure`, `EnsureNot`, `EnsureNotNull`, `Check`, `CheckIf`, `Tap`, `TapIf`, `TapTry`, `TapIfTry`, `TapError`, `TapErrorIf`, `Compensate`, `OnFailureCompensate`, `OnSuccessTry`, `Finally`, `Match`, `Combine`, `CombineInOrder`, `FirstFailureOrSuccess`, `ConvertFailure`, `Required`, `Deconstruct`, `AsMaybe`, `GetValueOrDefault`, `Select`, `SelectMany`, `WithTransactionScope`, `MapWithTransactionScope`, and factories `Success`, `Failure`, `SuccessIf`, `FailureIf`, `Of`, `Try`, `TryGet`.

## LanguageExt → CFE, at a glance

Your main translation axis — and, per the Teach thread, the one piece of knowledge here that nobody else on your team has. (Full treatment in Part 1; this is the map.)

| LanguageExt (4.4.9) | CFE (3.7.0) | Note |
|---|---|---|
| `Option<T>` | `Maybe<T>` | Both structs; `default` is the empty case in both. |
| `Some(x)` — **throws** on null | `Maybe.From(x)` — null becomes **`None`** | CFE's constructor behaves like LanguageExt's `Optional(x)`, not `Some(x)`. There is no throwing constructor. |
| `Optional(x)` | `Maybe.From(x)` / implicit conversion | The nullable bridge is CFE's *default* path, not a special one. |
| *no `.Value`* — deliberately absent | `.Value` **exists** and throws | The biggest philosophical difference. CFE leaves the escape hatch in (with a doc comment steering you to `GetValueOrThrow()`/`GetValueOrDefault()`); LanguageExt removes it. |
| `Either<L, R>` | *(no equivalent)* | CFE has no unbiased two-case type. `Result<T, E>` is nearest but is *failure*-biased. |
| `Fin<T>` / `Error` | `Result<T>` (error is a `string`) or `Result<T, E>` | No `Error` hierarchy, no exception-carrying error type. `E` is whatever you want. |
| `Validation<F, S>` + `Apply` | `Result.Combine(...)` / `ICombine` | **The real gap.** No applicative accumulation; `Combine` string-joins errors (default separator `", "`) or folds them via a `composerError`. Part 6. |
| `Map` / `Bind` / `Match` / `Filter` / `IfNone` | `Map` / `Bind` / `Match` / `Where` / `GetValueOrDefault` | Same semantics, different names. |
| *(nothing)* | `Tap`, `Check`, `Ensure`, `Compensate`, `Finally`, `*If`, `*Try` | CFE's pipeline vocabulary. Genuinely additive — Part 4. |
| `Map<K,V>`, `Seq<T>`, `Lst<T>` | *(none)* | No persistent collections. Use BCL + `System.Collections.Immutable`. |
| `Eff`/`Aff`, HKT traits, `IO<A>` (v5) | *(none)* | No effect system, no higher-kinded plumbing. That absence *is* the library's pitch. |
| `Unit` | *(none)* | `Result` (non-generic) and `UnitResult<E>` fill the role. |

One line to keep in your head: **CFE is one-tenth the surface area, one escape hatch looser, missing applicative validation, and better at pipelines.**

## Repository layout

```
TUTORIAL_PLAN.md            ← this file
ADOPTION.md                 ← decision journal; first entry after Article A
article/
  01-the-working-tour.md    ← Stage 1 / SEE, Article A (Parts 1–4)
  02-going-deeper.md        ← Stage 1 / SEE, Article B (Parts 5–8)
phases/
  stage-0-setup.md          ← start here
  stage-2-*.md              ← authored when we get there
src/ tests/                 ← the code you type
katas/                      ← fresh-domain challenges, if/when wanted
```

## Style

Carried over from learn-language-ext, because it works:

- **You type the code.** I describe what to write and why; I don't scaffold files or run your build unless you ask for a specific step.
- **Fluent method chains, not query syntax.** CFE's own README leads with `from x in … select …`; I'll show it once (Part 3) and once more where it genuinely wins (async, Part 8), then default to `.Map(...)`/`.Bind(...)`.
- **Stage 2 onward: TDD red-green-refactor**, baseline first. Smoke tests are permanent canaries.
- **Translation notes** LanguageExt ↔ CFE wherever they diverge — especially where they use the same word for slightly different behavior.
- **Verified, not remembered.** API claims here are checked against the v3.7.0 source or live NuGet/GitHub, and say so when it matters. If something contradicts your compiler, the compiler wins — tell me and I'll fix the doc.

## Resume protocol

- Step headers end with `[ ]`. **You** flip them to `[x]`; I'll remind you explicitly at the end of every step.
- First unchecked step is the resume point.
- Candidate test lists / exercise checklists: **I** maintain those, ticked as each one lands.
- "Notes & questions" sections are yours; "Decisions" sections get recorded where the decision was made.

## Decisions made (2026-08-22)

- Learning model: **See — Do — Teach**, made explicit as the spine. Teach is opportunistic by nature, so the plan produces teaching material as a byproduct and adds explain-it-back prompts rather than scheduling a teaching event.
- Stage 1 domain: **expense tracker again**, for the zero-domain-load / maximum-diff reasons above.
- Stage 1 delivery: **two whole articles**, not eight installments — matches your read-then-type loop.
- Stage 2: **CSV ingest → report CLI**, fresh domain, strict TDD.
- Stage 3: **minimal-API service**, self-directed, doubling as the demonstration.
- Stage 4: **RPA recon, optional and last**, read-only.
- `ADOPTION.md`: **decision journal**, four lines per entry, structured to convert to a circulated doc later.
