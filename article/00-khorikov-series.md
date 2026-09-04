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

### 1. Immutability

-

### 2. Primitive obsession

-

### 3. Non-nullable reference types

*(Write your own answer to "why not just NRTs?" here, before you read it.)*

-

### 4. Handling failures and input errors

-

### Candidates for `ADOPTION.md`

-
