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
