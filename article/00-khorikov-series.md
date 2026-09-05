# Reading guide — Khorikov's "Functional C#" series (2015)

> Prepared 2026-08-28. Read this **before** [`01-the-working-tour.md`](01-the-working-tour.md)'s first pass.
> Budget: ~1 hour for all four. Chronological order — they build.

## Why read an eleven-year-old blog series

Because **Stage 3 needs the *why*, not the API.** Your deliverable is a written on-ramp for teammates who have never seen any of this, and an on-ramp that opens with `Maybe<T>` has already lost them. This series is the canonical statement of the argument that CFE was built to serve — the library is downstream of these four posts.

It's also your own loop's step 2 done properly: read the primary source, *then* read my summary of it, so Part 1 of Article A becomes a second exposure rather than a first.

## The four posts

| # | Post | Date |
|---|---|---|
| 1 | [Functional C#: Immutability](https://enterprisecraftsmanship.com/posts/functional-c-immutability/) | 2015-03-02 |
| 2 | [Functional C#: Primitive obsession](https://enterprisecraftsmanship.com/posts/functional-c-primitive-obsession/) | 2015-03-07 |
| 3 | [Functional C#: Non-nullable reference types](https://enterprisecraftsmanship.com/posts/functional-c-non-nullable-reference-types/) | 2015-03-13 |
| 4 | [Functional C#: Handling failures and input errors](https://enterprisecraftsmanship.com/posts/functional-c-handling-failures-input-errors/) | 2015-03-20 |

Optional, if you want the primary source on the library's own motivation:
[C# functional extensions NuGet library](https://enterprisecraftsmanship.com/posts/c-functional-extensions-nuget-library/) ·
[What is functional programming?](https://enterprisecraftsmanship.com/posts/what-is-functional-programming/)

## Read critically — eleven years is a long time in C#

| Post | How it's aged | Read for |
|---|---|---|
| **1. Immutability** | Argument holds; **mechanics obsolete**. He hand-rolls immutable classes because C# 6 had nothing. Records, `init`, and `readonly struct` make this nearly free now. | the reasoning; ignore the code |
| **2. Primitive obsession** | **Holds up completely.** Still the best short statement of it in C#. | the one you'd hand a teammate unedited |
| **3. Non-nullable reference types** | ⚠️ **Most critical read.** Written *four years before* C# 8 shipped NRTs. He argues for a `Maybe` type because C# had **no `?` annotations at all**. | what the argument becomes once `string?` exists |
| **4. Handling failures, input errors** | Railway-oriented core intact, but uses a **hand-rolled `Result`** and predates `Result<T, E>`, `UnitResult<E>`, and the whole pipeline vocabulary. | the ROP argument; expect the API to look nothing like CFE's |

**The load-bearing gap is post 3.** "Why `Maybe<T>` instead of just turning NRTs on?" is the first question any teammate will ask, and the canonical source structurally cannot answer it — the alternative didn't exist when it was written. You have to answer that one yourself. Article A §2.7's explain-it-back exercise pushes at exactly this; **try to answer it before reading post 3**, so you find out what you actually think.

**Standing November caveat:** C# 15 `union` types (GA ~Nov 2026) change the answer to "how do I model a closed set of cases," which touches posts 2 and 4. See [`../PARKED-FSHARP-TRACK.md`](../PARKED-FSHARP-TRACK.md).

## Capture as you go

Where you **disagree** with 2015-Khorikov is the most valuable output of this hour — that's precisely the material Stage 3 needs, because it's what makes your on-ramp better than the stale blog post a teammate would otherwise google.

### Read 2026-09-05 — general reactions

**Nothing new in the content.** All four arguments were familiar from previous FP reading; details differed, substance didn't. *That's the useful result:* it confirms the gap is **application, not comprehension**. Had the posts taught something new about immutability, that would have been the worrying outcome.

**The one surprise was the date.** 2015 — earlier than expected for someone integrating functional ideas into the C# ecosystem. In hindsight unsurprising: F# had shipped as a first-class Visual Studio language in 2010 (research releases from 2005), and Wlaschin's canonical Railway Oriented Programming post is May 2013. Khorikov wasn't a lone voice; he was part of a small wave.

What sharpens the surprise: he was arguing **uphill**. March 2015 C# had no nullable reference types (2019), no records (2020), no switch expressions, no usable pattern matching. Every mechanism he proposes is hand-rolled because the language offered nothing.

**Liked the organization** — start with immutability, then layer the rest on.

**The register finding — most valuable takeaway.** Struck by the near-total absence of functional *jargon*. No `monad`, `functor`, or `applicative` anywhere. His vocabulary is **Evans and Fowler**: primitive obsession, immutability, illegal states unrepresentable.

Two things follow:

1. **That is the register the Stage 3 on-ramp needs**, because the team already has the DDD vocabulary and has none of the FP vocabulary. Test for every sentence: *would Khorikov-in-2015 have written it this way?*
2. **The fact that it read as "foreign" is a warning.** Months in LanguageExt-land have shifted me into the specialist register — a normal drift, and an occupational hazard for anyone about to teach.

**The background challenge is unchanged, and arguably worse:** how do these ideas enter day-to-day work on a large, old, unusual codebase? Day-to-day work is mostly **connectors** — smaller programs moving data between systems, with translation en route — and there is still a great deal to learn about the products themselves. See the ADOPTION.md entry of the same date; this reshaped Stage 2.

### 1. Immutability

- Argument familiar; mechanics visibly pre-records. Worth noting he explicitly declines to universalize it ("some classes are inherently mutable") — weigh case by case. That hedge is a good model for the on-ramp's tone.

### 2. Primitive obsession

- The post that holds up best, and the one a teammate could read unedited. Also the argument with the most direct purchase on connector work: source-system codes, IDs, and formats are exactly where `string`-typed everything hurts.

### 3. Non-nullable reference types

*(Write your own answer to "why not just NRTs?" here, before you read it.)*

#### Cold answer — 2026-09-04, written before reading anything

> My first question is: "What if you forget?" (pressed for time, tired). My second question is, "What about a less-experienced teammate or one less familiar with the code base? How do you ensure that she does not make `null` errors?" And, given the age of our code base(es), which of them have already set "nullability" so that the compiler checks it? The `Maybe<T>` gives you something like `nullability` checks but you see them in the IDE instead of discovering them when compiling or when testing the code. More importantly, it gives you a "mindset" by not allowing you to "forget" how to handle things.

#### Sharpened — same day, after review

**Lead with the codebase-age argument.** It was buried third in the cold answer; it's the strongest one, and it argues from *our* code rather than from principle. Nullable reference types are:

- **opt-in per project** (`<Nullable>enable</Nullable>`) — off by default for anything predating C# 8
- **advisory** — warnings, not errors, unless someone escalates them
- **a flag-day problem** — enabling them on a decade-old project surfaces thousands of warnings at once, which is precisely why nobody does it

`Maybe<T>` needs no flag day. It works in one method, in one file, today.

**"What if you forget" — get the mechanism right.** The cold answer said you see `Maybe` problems "in the IDE instead of when compiling," which is backwards: NRT warnings also appear live in the IDE, and `Maybe<T>` mistakes are *compile errors* — earlier and harder. The real axis is **advisory vs. binding**:

| | NRTs | `Maybe<T>` |
|---|---|---|
| A violation is a… | **warning** you can ignore | **type error** you cannot |
| Turning it on | project-wide decision | per-use |
| Survives boundaries | annotations only; erased at runtime | a real type at runtime |

That's the "mindset" point made concrete: a `Maybe<T>` can't be read without stating what absence means.

**Two arguments missing from the cold answer:**

- **Empty-value sentinels.** NRTs say nothing about `""`, `-1`, `Guid.Empty`, or an empty list. A non-null but empty `string` passes every nullable check and still means "no value." In business code, `""`-as-absent is at least as common as `null`-as-absent.
- **Composition — probably the biggest practical win.** `Maybe<T>` chains (`.Map`, `.Bind`, `.Where`). `string?` doesn't compose; you get `?.` chains that quietly produce more nulls, or `if` ladders. There's no way to write "four lookups, any of which may miss" as one expression.

#### The concession — volunteer it before anyone finds it

**CFE's `.Value` exists**, so `Maybe<T>` is *not* fully binding — a tired teammate can type `.Value` and get an exception. Worse, `GetValueOrDefault()` with no argument returns `default`, which for a reference type is `null`: the exact thing the type was adopted to eliminate. (Discovered firsthand 2026-08-22 in `ResultRedsTests`.)

So the honest answer to "how do you protect a less-experienced teammate" is **not** "the type protects her." It's *the type, plus a documented convention, plus an analyzer* — and the analyzer is `CSharpFunctionalExtensions.Analyzers` 1.4.1, third-party, last released 2024-07-03, with no documented rule IDs. Saying that out loud is what will make the Stage 3 on-ramp credible.

### 4. Handling failures and input errors

- Railway-oriented core intact; the hand-rolled `Result` shows its age. The connector application is direct: bad record, missing field, and unmappable code are all *routine expected* failures, which is precisely the case for `Result` over exceptions.

### Candidates for `ADOPTION.md`

- ✅ *Written 2026-09-05* — the register finding (Evans/Fowler vocabulary, not category theory).
- ✅ *Written 2026-09-05* — the maintenance-reality constraint, and what survives of the opportunity.
