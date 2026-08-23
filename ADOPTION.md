# Adoption journal — CSharpFunctionalExtensions

A decision journal, not a pitch deck. Written for me; structured so it *could* become something I circulate.

**Format — four lines per entry:**

```
### YYYY-MM-DD — <short title>
- **Hit:** what I ran into
- **Concluded:** what I now think, and why
- **Tell the team:** the one sentence I'd actually say out loud
```

The first two lines are notes I'd want regardless. The third is the one that converts. If an entry has no honest third line yet, leave it blank — a blank is information.

**Ground rule, from my global instructions:** a library like this gets adopted as a *deliberate, team-wide convention*, never smuggled in via a single bug fix. This journal is how I'd earn the right to propose the convention.

---

## Open questions to answer by the end

Not homework — just the list of things I'd need a real answer to before recommending anything.

- [ ] Is `.Value` existing at all a dealbreaker for a team, or is a documented convention enough? Does `CSharpFunctionalExtensions.Analyzers` close the gap? *(Article B, Part 8)*
- [ ] Is the missing `Validation` applicative a genuine blocker or an annoyance I can design around? *(Article B, Part 6; Stage 2 will settle it)*
- [ ] Is the pipeline vocabulary (`Tap`/`Check`/`Compensate`/`Finally`) worth a dependency **on its own**, separately from `Maybe`/`Result`?
- [ ] Where's the honest line between "just turn on nullable reference types" and "take the dependency"?
- [ ] What does the debugging/stack-trace experience actually cost in practice?
- [ ] Would I recommend CFE or LanguageExt to *this* team, and does that answer change in three years?

---

## Entries

<!-- newest first -->

### 2026-08-22 — CFE has no on-ramp

- **Hit:** Skimmed the official [CFE README](https://github.com/vkhorikov/CSharpFunctionalExtensions) cold, before reading anything else. Couldn't get purchase on it — hard to grok.
- **Concluded:** Not a me-problem. The README is a *reference catalog*, not a tutorial: it walks the API surface in the library's own organizational order, with apples-and-bananas examples that carry no domain and no narrative, so there's no sense of which operations you'd reach for first. It also front-loads LINQ query syntax and uses `.Value` freely — two things a newcomer shouldn't copy. The rest of the official material doesn't fill the gap: Khorikov's "Functional C#" blog series is from 2015 and predates `Result<T, E>`, `UnitResult<E>`, and the entire pipeline vocabulary; the thorough treatment is a paid Pluralsight course.
- **Tell the team:** *If we adopt this, we write the onboarding doc ourselves — there's nothing to point people at.*

Corollary worth costing out before proposing anything: "write the onboarding doc" is not a footnote. It's the artifact that decides whether the convention survives the first three people who join after us.

Second, smaller data point from the same sitting — the size claim the library makes about itself checks out. Core type only: CFE `Maybe.cs` **262 lines** vs LanguageExt `Option.cs` **1,244** (v5 `main`, so indicative rather than like-for-like against the 4.4.9 I used). Roughly 5:1. That ratio is the actual product, and it cuts both ways: it's why a team could own this, and why the accumulation gap in §6 has no library answer.
