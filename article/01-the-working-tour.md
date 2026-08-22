# CSharpFunctionalExtensions: A Working Tour

*Article A of two. Everything you need to write real CFE code — `Maybe<T>`, the `Result` railway, and the pipeline vocabulary. Written for someone who already knows LanguageExt and wants the diff.*

> Plan and context: [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). This is **Stage 1 — SEE**.
> Verified against **CSharpFunctionalExtensions 3.7.0** source (tag `v3.7.0`), not from memory. Where I'm hedging, I say so.

---

## How to read this

Two passes, per your loop:

1. **Read it through.** Cover to cover, no typing, no editor. It's one sitting. Part 1 is prose by design; Parts 2–4 have code you'll skim now and type later.
2. **Start over and type it.** Parts 2, 3, and 4 are type-along. Section headers there end with `[ ]` — flip them to `[x]` as you go; the first unflipped one is your resume point. I maintain the **type-along checklists** at the end of each part.

Each part closes with two exercises that take five minutes and are worth more than the twenty minutes before them:

- **Predict-then-check** — write your guess down *before* running it. Being wrong is the point; a surprise you predicted wrong is the only kind you remember.
- **Explain-it-back** — two or three sentences in your own words, aimed at an imagined mid-level teammate who's never heard of any of this. This is the Teach step running with an audience of zero, and per your own history (the `MapLeft`/`Bind` reconciliation in the library kata) it's the thing that actually converts "I made it work" into "I understand it."

**The domain** is the expense tracker from `../learn-language-ext` — deliberately. You already know the domain cold, so every surprise you hit is an *API* surprise, and the LanguageExt-to-CFE diff shows up line by line in code you've already written once.

**Where the code goes:** `src/Expenses/` for production types, `tests/Expenses.Tests/` for the tests. New types start inline in the test file and get lifted into `src/` once they're settled — same rhythm as the other project, even though we're not doing TDD here.

---

# Part 1 — Why this library exists

*Read this part; don't type it.*

## The pitch

Vladimir Khorikov wrote CSharpFunctionalExtensions to serve an argument he'd already made in prose. That argument, from his 2015 "Functional C#" series, has three targets:

1. **Primitive obsession** — modeling a domain with `string` and `int` and `decimal`, so the compiler can't tell an email from a customer name and can't tell you whether either one is valid.
2. **Nulls** — `null` as the universal "no value," which every caller must remember to check and no signature ever mentions.
3. **Exceptions as control flow** — using `throw` for expected, ordinary outcomes like "that input was malformed," so the failure path is invisible in the type and unenforced by the compiler.

The library is the tooling for that argument: `Maybe<T>` for #2, `Result` for #3, `ValueObject`/`Entity` for #1. That's genuinely all of it. If you internalize *"this library is three blog posts with a NuGet package attached,"* you'll predict most of its API correctly before reading it.

Notice what's *not* on that list: no ambition to bring category theory to C#, no effect system, no attempt to make C# into F#. That's not an oversight — it's the product decision, and it's the whole reason you're evaluating this library instead of just recommending LanguageExt.

## What's in the box

The entire library, four areas:

| Area | Types |
|---|---|
| Optionality | `Maybe<T>` (+ non-generic `Maybe` entry point, `IMaybe<T>`, `MaybeEqualityComparer`) |
| Failure | `Result`, `Result<T>`, `Result<T, E>`, `UnitResult<E>`, `IResult`, `ResultFailureException`, `ResultSuccessException` |
| DDD building blocks | `ValueObject`, `ValueObject<T>`, `ComparableValueObject`, `SimpleValueObject<T>`, `EnumValueObject`, `Entity<TId>` |
| Plumbing | `ICombine`, `Result.Configuration`, `Maybe.Configuration`, System.Text.Json converters |

That's it. Six real types you'll use, four DDD base classes, some plumbing.

The extension-method surface *looks* enormous — the repo has hundreds of files under `Maybe/Extensions` and `Result/Methods/Extensions` — but that's overload multiplication, not concept multiplication. Every combinator ships in sync, `Task`, `ValueTask`, and "Left"/"Right" async variants (where Left/Right means "which side of the call is already a Task"). Strip that away and there are about **fifteen operations**, most of which you already know under LanguageExt names.

Concretely, on `Maybe<T>`: `Map`, `Bind`, `Where`, `Or`, `Match`, `Tap`, `TapNoValue`, `Execute`, `ExecuteNoValue`, `Flatten`, `GetValueOrDefault`, `GetValueOrThrow`, `Select`, `SelectMany`, `Deconstruct`, `AsMaybe`, `AsNullable`, `ToList`, `ToResult`, `ToUnitResult`, `ToInvertedResult`, `BindOptional`, `Optional`, plus the collection helpers `TryFirst`, `TryLast`, `TryFind`, `Choose`.

And on the `Result` family: `Map`, `MapIf`, `MapTry`, `MapError`, `Bind`, `BindIf`, `BindTry`, `BindZip`, `BindOptional`, `Ensure`, `EnsureNot`, `EnsureNotNull`, `Check`, `CheckIf`, `Tap`, `TapIf`, `TapTry`, `TapIfTry`, `TapError`, `TapErrorIf`, `Compensate`, `OnFailureCompensate`, `OnSuccessTry`, `Finally`, `Match`, `Combine`, `CombineInOrder`, `FirstFailureOrSuccess`, `ConvertFailure`, `Required`, `Deconstruct`, `AsMaybe`, `GetValueOrDefault`, `Select`, `SelectMany`, `WithTransactionScope`, `BindWithTransactionScope`, `MapWithTransactionScope`, plus factories `Success`, `Failure`, `SuccessIf`, `FailureIf`, `Of`, `Try`, `TryGet`.

Long lists, but read them again and notice: the `Result` list is mostly **one idea crossed with modifiers**. `Bind` / `BindIf` / `BindTry` / `BindZip`. `Tap` / `TapIf` / `TapTry` / `TapError` / `TapErrorIf`. Learn the four root verbs and three suffixes and you've learned the list:

| Suffix | Means |
|---|---|
| `…If` | apply only when a condition or predicate holds; otherwise pass through untouched |
| `…Try` | the delegate may throw; catch it and turn the exception into a failure |
| `…Error` | operate on the failure side instead of the success side |
| `…Not` | invert the predicate |

## Two philosophies, one page

You have a rare advantage here: you've already learned this problem space in a library that made the *opposite* trade at nearly every decision point. Use it. Here's the map — the single densest thing in this article, and the one piece of knowledge nobody else on your team has.

| LanguageExt (4.4.9) | CFE (3.7.0) | What it means for you |
|---|---|---|
| `Option<T>` | `Maybe<T>` | Both structs; `default` is the empty case in both. Rename and move on. |
| `Some(x)` — **throws** on null | `Maybe.From(x)` — null becomes **`None`** | CFE's only constructor behaves like LanguageExt's `Optional(x)`. There is no throwing constructor, so there's no "wrong" one to reach for. |
| `Optional(x)` | `Maybe.From(x)`, or implicit conversion from `T` | The nullable bridge is CFE's *default* path rather than a special interop function. |
| **no `.Value`** — deliberately absent | `.Value` **exists** and throws | The central philosophical difference. Discussed at length in Part 2. |
| `using static LanguageExt.Prelude;` → `Some`, `None`, `Right`… | no prelude; `Maybe.From`, `Result.Success` | Smaller vocabulary to carry; slightly noisier at the call site. |
| `Either<L, R>` | *(nothing)* | CFE has **no unbiased two-case type.** `Result<T, E>` is nearest, but its `E` side means *failure* — you can't use it for "one of two equally good things." |
| `Fin<T>` / the `Error` hierarchy | `Result<T>` (error is a `string`) or `Result<T, E>` | No built-in error type, no exception-carrying error. `E` is whatever you define. Freedom and homework in equal measure. |
| `Validation<F, S>` + `Apply` | `Result.Combine(...)` / `ICombine` | **The real gap.** No applicative accumulation. Article B, Part 6 — and Stage 2 is designed to make you feel it. |
| `Map` / `Bind` / `Match` / `Filter` / `IfNone` | `Map` / `Bind` / `Match` / `Where` / `GetValueOrDefault` | Identical semantics, different spellings. |
| `Map<K,V>`, `Seq<T>`, `Lst<T>` | *(nothing)* | No persistent collections. Use BCL plus `System.Collections.Immutable`. |
| `Eff`/`Aff`, HKT traits, v5's `IO<A>` | *(nothing)* | No effect system, no higher-kinded plumbing. This absence **is** the pitch. |
| `Unit` | *(nothing)* | `Result` (non-generic) and `UnitResult<E>` cover the ground. |
| *(nothing)* | `Tap`, `Check`, `Ensure`, `Compensate`, `Finally`, `*If`, `*Try` | CFE's pipeline vocabulary. **Genuinely additive** — Part 4. |

One line to carry around: **CFE is one-tenth the surface area, one escape hatch looser, missing applicative validation, and better at pipelines.**

That last row deserves emphasis because it cuts against the expected story. The expected story is "CFE is LanguageExt minus things." Mostly true — but not entirely. LanguageExt gives you `Bind` and expects you to build your own vocabulary on top; CFE ships the vocabulary. When your pipeline needs to *do something* (log, save, notify) without disturbing the value flowing through, LanguageExt has you writing `.Map(x => { effect(x); return x; })` and CFE has you writing `.Tap(effect)`. Across a real codebase that difference compounds, and it's the thing I'd expect your team to notice and like first.

## The trade CFE is actually making

Everything distinctive about this library follows from one decision: **it is designed to be adopted by a team that has never done any of this.**

That's why `.Value` still exists on `Maybe<T>`. It's why the error type defaults to `string`. It's why there's no prelude of free functions, no `Unit`, no typeclasses, no HKT encoding — nothing that makes a C# developer's first encounter feel like a different language. It's why the whole thing is a `netstandard2.0` assembly with **zero dependencies** that you could read in an afternoon.

Compare the honest costs on each side:

| | LanguageExt | CFE |
|---|---|---|
| Ceiling | very high — you can express nearly anything | modest — deliberately |
| Floor (what a novice does wrong) | high; the type system blocks most escapes | **lower**; `.Value` compiles, and a novice will find it |
| Onboarding a skeptical C# team | hard | plausible |
| Reading the source when confused | days | an afternoon |
| Error accumulation | first-class (`Validation`) | awkward (`Combine`) |
| Pipeline ergonomics | build it yourself | shipped |

Neither column is the right answer in the abstract. But notice that the CFE column is the one that matches your actual constraint, which isn't "what's the most expressive library" — it's "what could my team say yes to." Your global instructions already reached this conclusion, incidentally: *recommend the lightest adequate tool.* This project is you checking your own default advice against reality.

## Exercises — Part 1

**Predict-then-check.** Write down your answers now; Part 2 settles all three.

1. `Maybe<string> m = Maybe.From<string>(null);` — does this throw, or give you `None`? (LanguageExt's `Some(null)` throws `ValueIsNullException`.)
2. `Maybe<int> m = default;` — legal? If so, what is it?
3. `Maybe<string> m = Maybe<string>.None; var s = m.Value;` — compiles? Runs? What happens?

**Explain-it-back.** In three sentences, for a teammate who asks *"why would I use this instead of just `string?` and nullable reference types?"* — no jargon, no "monad," no reference to LanguageExt. If you can't do it in three sentences yet, note that; it's the honest starting point and the answer improves through Part 3.

---

# Part 2 — `Maybe<T>`

*Type-along from here.*

The goal for this part is the **category catalog**: a lookup returning `Maybe<Category>` — a value on a hit, nothing on a miss — and the habit of consuming it without ever reaching for `.Value`. Same exercise as Phase 1 of the LanguageExt tutorial, which is exactly why it's useful: you'll feel the differences instead of reading about them.

## 2.1 — Constructing a `Maybe<T>`  `[ ]`

Create `tests/Expenses.Tests/MaybeTests.cs`:

```csharp
namespace Expenses.Tests;

using CSharpFunctionalExtensions;

public class MaybeTests
{
    [Fact]
    public void FourWaysToMakeAMaybe()
    {
        Maybe<string> viaFactory      = Maybe.From("groceries");     // type inferred
        Maybe<string> viaGenericParam = Maybe<string>.From("groceries");
        Maybe<string> viaConversion   = "groceries";                 // implicit
        Maybe<string> none            = Maybe<string>.None;
        Maybe<int>    defaultIsNone   = default;

        Assert.True(viaFactory.HasValue);
        Assert.True(viaGenericParam.HasValue);
        Assert.True(viaConversion.HasValue);
        Assert.True(none.HasNoValue);
        Assert.True(defaultIsNone.HasNoValue);
    }
}
```

Run it. Five constructions, one type, no surprises yet.

Now the surprise — and the answer to the first prediction:

```csharp
[Fact]
public void NullBecomesNone_ItDoesNotThrow()
{
    string? missing = null;

    Maybe<string> m = Maybe.From(missing);   // no exception
    Maybe<string> viaConversion = missing;   // also no exception

    Assert.True(m.HasNoValue);
    Assert.True(viaConversion.HasNoValue);
}
```

**This is the single most important translation note in the article.** In LanguageExt you had *two* constructors with different contracts — `Some(x)` asserts "definitely a value" and throws `ValueIsNullException` if handed null, while `Optional(x)` is the bridge that maps null to `None`. You learned to reach for `Optional` at every nullable boundary and to treat `Some(null)` as a bug the library catches for you.

CFE has **one** constructor and it behaves like `Optional`. From the 3.7.0 source, `Maybe<T>`'s private constructor is literally:

```csharp
private Maybe(T? value)
{
    if (value == null)
    {
        _isValueSet = false;
        _value = default;
        return;
    }

    _isValueSet = true;
    _value = value;
}
```

Consequences, both directions:

- **Better:** there's no wrong constructor to pick. `Maybe.From(GetPossiblyNull())` is always right, and the implicit conversion means `return someNullableThing;` from a `Maybe<T>`-returning method just works. The nullable boundary stops being a thing you have to think about.
- **Worse:** you've lost a diagnostic. `Some(null)` throwing was LanguageExt telling you *"you asserted a value exists and it didn't — fix that upstream."* In CFE that same bug silently becomes a `None` that flows onward and surfaces somewhere else as a missing value. If you actually want "this must not be null, and I want to know immediately," CFE has nothing for it at construction time; you'd reach for `Result` and `EnsureNotNull` (Part 3), or a plain guard clause.

Worth writing in `ADOPTION.md` when you get there: for a team, "one obvious constructor" is probably the better trade even though it's the weaker type. Fewer choices, fewer wrong ones.

## 2.2 — The escape hatch: `.Value` and friends  `[ ]`

Now the second big difference, and the one your team will trip over.

```csharp
[Fact]
public void ValueThrowsOnNone()
{
    Maybe<string> none = Maybe<string>.None;

    var ex = Assert.Throws<InvalidOperationException>(() => none.Value);
    Assert.Equal("Maybe has no value.", ex.Message);
}
```

`Maybe<T>.Value` exists. LanguageExt deliberately has no such member — the only way to get a value out of an `Option<T>` is to handle both cases. CFE leaves the door open, and the library's own source comments on it:

```csharp
/// <summary>
/// Try to use GetValueOrThrow() or GetValueOrDefault() instead for better explicitness.
/// </summary>
public T Value => GetValueOrThrow();
```

So `.Value` isn't a different mechanism from `GetValueOrThrow()` — it *is* `GetValueOrThrow()`, wearing a name that doesn't warn you. That's a documentation-level guardrail around a type-level hole, which is precisely the difference in philosophy.

The full set of ways out:

```csharp
[Fact]
public void GettingTheValueOut()
{
    Maybe<string> hit  = "groceries";
    Maybe<string> miss = Maybe<string>.None;

    // Throwing, with a custom message or a custom exception
    Assert.Equal("groceries", hit.GetValueOrThrow());
    Assert.Throws<InvalidOperationException>(() => miss.GetValueOrThrow("no category"));
    Assert.Throws<ArgumentException>(() => miss.GetValueOrThrow(new ArgumentException()));

    // Defaulting
    Assert.Equal("groceries", hit.GetValueOrDefault("uncategorized"));
    Assert.Equal("uncategorized", miss.GetValueOrDefault("uncategorized"));
    Assert.Null(miss.GetValueOrDefault());          // default(string) — null is back

    // The out-param form, with nullable-analysis attributes
    Assert.True(hit.TryGetValue(out var value));
    Assert.Equal("groceries", value);
}
```

Three notes:

- **`GetValueOrDefault()` with no argument re-introduces `null`** for reference types (it returns `default`). That's the one member here that quietly undoes the whole point. Prefer the overload that takes a fallback, or `Match`.
- **`TryGetValue` is annotated** (`[NotNullWhen(true), MaybeNullWhen(false)]` on .NET 5+), so the compiler's flow analysis follows it. This is the idiomatic bridge *back* to imperative C#, and it's the one escape hatch I'd actively endorse: it forces the `if`, and NRT analysis keeps you honest inside it.
- **There are extension overloads too**, which are easy to miss: `GetValueOrDefault(Func<T>)` for a lazily-computed fallback, and `GetValueOrDefault(selector, defaultValue)` which is `Map`-then-unwrap in one call — `catalog.Find("Rent").GetValueOrDefault(c => c.Name, "none")`.

The rule to adopt for the rest of this tutorial, and to propose to your team: **`.Value` and `GetValueOrThrow()` are for tests and for the top of a call stack where you've already proven the value exists. Inside a pipeline they're a bug.** Article B, Part 8 looks at whether the Analyzers package can enforce that, which matters a lot more here than it would in LanguageExt — CFE's floor is set by convention, and conventions need tooling.

> **Heads-up on the README:** CFE's README shows `.Unwrap("No fruit")` in its `Map` example. There is no `Unwrap.cs` in 3.7.0's `Maybe/Extensions`, so if that fails to compile for you, that's why — use `GetValueOrDefault`. (I verified the file listing, not every file's contents, so treat this as "probably renamed" rather than gospel.)

## 2.3 — Consuming honestly: `Match`, `Or`  `[ ]`

Now build the domain type. Inline in the test file for the moment:

```csharp
public sealed record Category(string Name);

public sealed class CategoryCatalog
{
    private readonly Dictionary<string, Category> _byName;

    public CategoryCatalog(IEnumerable<Category> categories) =>
        _byName = categories.ToDictionary(c => c.Name);

    public Maybe<Category> Find(string name) => _byName.TryFind(name);
}
```

That `TryFind` is worth pausing on. CFE ships dictionary and sequence helpers that hand you a `Maybe` directly:

```csharp
public static Maybe<V> TryFind<K, V>(this IReadOnlyDictionary<K, V> dict, K key)
```

**One gotcha, and it's the kind that costs twenty minutes.** `TryFind.cs` actually defines *two* overloads — one on `IReadOnlyDictionary<K, V>` and one on `IDictionary<K, V>` — but they sit in mutually exclusive `#if` branches, and on any modern target (`net45` and up, netstandard, .NET Core, .NET 5+) **only the `IReadOnlyDictionary` one is compiled**. So it works on a concrete `Dictionary<K, V>` (which implements both interfaces), but if the variable is *typed* as `IDictionary<K, V>`, `TryFind` simply won't be in scope and the error message won't tell you why. Type the field as `Dictionary<>` or `IReadOnlyDictionary<>` and you'll never notice.

(Silver lining: because only one overload exists on your target, there's no ambiguity when calling it on a concrete `Dictionary`, which implements both interfaces. Had both been compiled, that call would be ambiguous.)

Compare what you wrote in the LanguageExt tutorial: `Optional(_byName.GetValueOrDefault(name))` — correct, idiomatic, and a two-step composition you had to know to assemble. CFE just has the function. Small thing, but it's representative: CFE tends to ship the *specific* helper where LanguageExt gives you the general parts.

Consuming it:

```csharp
public class CategoryCatalogTests
{
    private readonly CategoryCatalog _catalog =
        new([new Category("Groceries"), new Category("Rent")]);

    [Fact]
    public void Find_ReturnsSomeOrNone()
    {
        Assert.Equal(Maybe.From(new Category("Groceries")), _catalog.Find("Groceries"));
        Assert.True(_catalog.Find("Yacht").HasNoValue);
    }

    [Fact]
    public void Match_HandlesBothCases()
    {
        string found = _catalog.Find("Groceries")
            .Match(
                Some: c => $"Found: {c.Name}",
                None: () => "Not found");

        string missing = _catalog.Find("Yacht")
            .Match(
                Some: c => $"Found: {c.Name}",
                None: () => "Not found");

        Assert.Equal("Found: Groceries", found);
        Assert.Equal("Not found", missing);
    }
}
```

**Yes, those parameter names really are `Some` and `None`.** Straight from the 3.7.0 source:

```csharp
public static TE Match<TE, T>(in this Maybe<T> maybe, Func<T, TE> Some, Func<TE> None)
```

Unconventional C# (parameters capitalized like types) and identical to LanguageExt's `Match(Some:, None:)`. Use the named form — the positional order is value-branch-first, but naming it costs nothing and reads better. There's also a `void`-returning overload taking `Action<T>` and `Action` for the side-effecting case, and — nice touch — a `TContext` overload family that lets you pass state into the lambda instead of capturing it, if you're ever counting allocations.

`Or` supplies a fallback while *staying* in `Maybe`, which is the difference between it and `GetValueOrDefault`:

```csharp
[Fact]
public void Or_StaysInsideMaybe()
{
    Maybe<Category> uncategorized = new Category("Uncategorized");

    Maybe<Category> stillAMaybe = _catalog.Find("Yacht").Or(uncategorized);
    Category        aBareValue  = _catalog.Find("Yacht").GetValueOrDefault(new Category("Uncategorized"));

    Assert.Equal(uncategorized, stillAMaybe);
    Assert.Equal("Uncategorized", aBareValue.Name);
}
```

`Or` has four overloads — a bare `T`, a `Maybe<T>`, a `Func<T>`, and a `Func<Maybe<T>>`. The `Func` forms are lazy: the fallback is only computed on the empty path. Same eager-vs-thunk pairing you met in LanguageExt's `IfNone(value)` versus `IfNone(Func)`, and the same rule applies — cheap constant, use the value form; expensive or effectful, use the function form.

Translation summary for this section:

| LanguageExt | CFE |
|---|---|
| `.Match(Some:, None:)` | `.Match(Some:, None:)` — identical |
| `.IfNone(fallback)` | `.GetValueOrDefault(fallback)` |
| `.IfNone(() => …)` | `.GetValueOrDefault(() => …)` |
| `.IfSome(action)` | `.Execute(action)` (returns void) or `.Tap(action)` (returns the `Maybe`) |
| *(no direct equivalent)* | `.Or(...)` — fallback that stays in `Maybe` |

## 2.4 — Transforming: `Map`, `Bind`, `Where`  `[ ]`

Nothing new conceptually — this is the part you know cold. One test to pin the spellings, then move on.

Add a second `Maybe`-returning operation so there's something to chain:

```csharp
public Maybe<Category> FindParent(Category c) =>
    c.Name switch
    {
        "Groceries" => new Category("Food"),
        "Food"      => new Category("All Spending"),
        _           => Maybe<Category>.None
    };
```

(Note the implicit conversion earning its keep: the `switch` arms return bare `Category` values and a `Maybe<Category>.None`, and it all unifies. In LanguageExt you'd have written `Some(...)` on each arm.)

```csharp
[Fact]
public void MapBindWhere()
{
    // Map: A → B
    Assert.Equal(Maybe.From("Groceries"), _catalog.Find("Groceries").Map(c => c.Name));
    Assert.True(_catalog.Find("Yacht").Map(c => c.Name).HasNoValue);

    // Bind: A → Maybe<B>, flattened
    Assert.Equal(
        Maybe.From(new Category("All Spending")),
        _catalog.Find("Groceries").Bind(_catalog.FindParent).Bind(_catalog.FindParent));

    // Any miss anywhere collapses the rest
    Assert.True(_catalog.Find("Yacht").Bind(_catalog.FindParent).HasNoValue);
    Assert.True(_catalog.Find("Rent").Bind(_catalog.FindParent).HasNoValue);

    // Where: Some → None when the predicate fails (LanguageExt's Filter)
    Assert.True(_catalog.Find("Groceries").Where(c => c.Name.Length > 100).HasNoValue);
}
```

The decision rule is unchanged from what you already have wired in: **function returns a plain value → `Map`; function returns a `Maybe` → `Bind`.** `Flatten` exists too (`Maybe<Maybe<T>> → Maybe<T>`), for the same reason it does in LanguageExt and with the same advice — you shouldn't often need it, because chaining with `Bind` means you never accumulate the nesting.

`Where` is `Filter` renamed. Note the name choice: `Where`, `Select`, `SelectMany` are all present, which is what makes LINQ query syntax work over `Maybe`. Part 3 shows that once.

## 2.5 — The collection helpers  `[ ]`

These are a genuine convenience win and they're the ones you'll reach for daily.

```csharp
[Fact]
public void CollectionHelpers()
{
    var categories = new[] { new Category("Groceries"), new Category("Rent") };

    // TryFirst / TryLast, with or without a predicate
    Assert.Equal(Maybe.From(new Category("Groceries")), categories.TryFirst());
    Assert.Equal(Maybe.From(new Category("Rent")),      categories.TryLast());
    Assert.Equal(Maybe.From(new Category("Rent")),      categories.TryFirst(c => c.Name.StartsWith('R')));
    Assert.True(categories.TryFirst(c => c.Name == "Yacht").HasNoValue);
    Assert.True(Array.Empty<Category>().TryFirst().HasNoValue);

    // Choose: filter out the empties, in one pass
    Maybe<Category>[] mixed =
    [
        Maybe.From(categories[0]),
        Maybe<Category>.None,
        Maybe.From(categories[1])
    ];

    Assert.Equal(new[] { "Groceries", "Rent" }, mixed.Choose(c => c.Name));
    Assert.Equal(2, mixed.Choose().Count());
}
```

| Helper | Does |
|---|---|
| `TryFirst()` / `TryFirst(predicate)` | `FirstOrDefault` without the `null`/`default` ambiguity |
| `TryLast()` / `TryLast(predicate)` | same for `LastOrDefault` |
| `TryFind(key)` | dictionary lookup → `Maybe<V>`; works on `IDictionary` and `IReadOnlyDictionary` |
| `Choose()` | `IEnumerable<Maybe<T>>` → `IEnumerable<T>`, dropping empties |
| `Choose(selector)` | same, projecting as it goes |

`TryFirst` is the fix for a real and common C# bug: `FirstOrDefault()` on a sequence of `int` returns `0` on an empty sequence, which is indistinguishable from finding an actual `0`. `Choose` is F#'s `List.choose` / LanguageExt's `Somes`, and it's the tidy answer to "map a lookup over a list and keep the hits."

## 2.6 — Side effects: `Tap` versus `Execute`  `[ ]`

```csharp
[Fact]
public void TapChains_ExecuteTerminates()
{
    var log = new List<string>();

    Maybe<Category> stillAMaybe = _catalog.Find("Groceries")
        .Tap(c => log.Add($"found {c.Name}"))
        .TapNoValue(() => log.Add("nothing found"));

    _catalog.Find("Yacht")
        .Execute(c => log.Add($"found {c.Name}"))       // returns void
        ;
    _catalog.Find("Yacht")
        .ExecuteNoValue(() => log.Add("no yacht"));     // returns void

    Assert.Equal(new[] { "found Groceries", "no yacht" }, log);
    Assert.True(stillAMaybe.HasValue);
}
```

The distinction is just the return type, and it's the pattern for the whole library:

- **`Tap(Action<T>)` returns the `Maybe<T>`** — it's a pass-through, so it composes mid-chain. `TapNoValue(Action)` is the mirror for the empty case.
- **`Execute(Action<T>)` returns `void`** — it terminates the chain.

LanguageExt's `IfSome(Action)` is `Execute`. There is no LanguageExt equivalent of `Tap` — you'd write `.Map(x => { effect(x); return x; })`, which works and reads badly. This is the first appearance of the pipeline vocabulary, and Part 4 is nothing but this idea applied to `Result`.

## 2.7 — Equality, `ToString`, and good news about xUnit  `[ ]`

```csharp
[Fact]
public void EqualityAndFormatting()
{
    Maybe<string> a = "groceries";
    Maybe<string> b = "groceries";

    Assert.Equal(a, b);                       // structural
    Assert.True(a == b);
    Assert.True(a == "groceries");             // Maybe<T> == T works too
    Assert.Equal(Maybe<string>.None, Maybe<string>.None);

    Assert.Equal("groceries", a.ToString());
    Assert.Equal("No value", Maybe<string>.None.ToString());
}
```

Two things here are quietly better than what you had:

**`Maybe<T>` is not `IEnumerable`.** Remember the `[[…]]` mess in learn-language-ext, where `Assert.Equal` on two `Option`s bound to xUnit's *collection* overload because `Option<T>` implements `IEnumerable<T>`, and failures printed as nested brackets? That doesn't happen here. `Maybe<T>` implements `IEquatable<Maybe<T>>` and nothing sequence-shaped, so `Assert.Equal` picks the ordinary overload and failures print as `groceries` / `No value`. The `OptionAssert` helper you built there has no counterpart to build here. (`.ToList()` exists as an extension if you *want* the 0-or-1 sequence.)

**`Maybe<T> == T` compiles**, thanks to an `operator ==(Maybe<T>, T)` overload. Handy in tests. Mildly dangerous in production, because it silently unwraps a comparison you might have meant to be between two `Maybe`s — but it's the kind of dangerous that shows up immediately rather than at 3 a.m.

## Type-along checklist — Part 2

*I maintain these; they get ticked as the work lands.*

- [ ] Five constructions, including `default`
- [ ] `Maybe.From(null)` → `None`, no throw
- [ ] `.Value` throws; `GetValueOrThrow` / `GetValueOrDefault` / `TryGetValue`
- [ ] `Category` + `CategoryCatalog.Find` via `TryFind`
- [ ] `Match(Some:, None:)`
- [ ] `Or` versus `GetValueOrDefault`
- [ ] `Map` / `Bind` / `Where`, including a short-circuit chain
- [ ] `TryFirst` / `TryLast` / `Choose`
- [ ] `Tap` versus `Execute`
- [ ] Equality, `ToString`, and confirming there's no `[[…]]` problem
- [ ] Lift `Category` and `CategoryCatalog` into `src/Expenses/`

## Exercises — Part 2

**Predict-then-check.** Guesses first, in writing:

1. `Maybe<int> zero = 0;` — `HasValue` or `HasNoValue`? (Careful: the constructor's test is `value == null`, and `T` here is `int`.)
2. `Maybe<Category> m = _catalog.Find("Yacht"); var name = m.GetValueOrDefault()?.Name;` — does that compile with NRTs on, and does it warn?
3. `Maybe.From(Maybe.From("x"))` — what's the type, and how many layers deep is it? (Peek at the implicit conversion operator in `Maybe.cs` before you decide.)

**Explain-it-back.** Your teammate says: *"We already have nullable reference types. The compiler warns me if I don't check for null. What does `Maybe<T>` buy me?"* Answer in three or four sentences. There's a genuinely good answer and a genuinely honest concession in it — find both. (Hint for the good answer: what does `string?` say about `""`? What does a nullable *value* type do inside a generic method? And what happens to your `?` annotations the moment the value crosses into a library compiled without NRTs?)

---

# Part 3 — `Result` and the railway

`Maybe<T>` says "there might be nothing here." It doesn't say *why*. When absence needs a reason — parse failure, validation failure, a rule violation — you want `Result`.

## 3.1 — The family  `[ ]`

Four types, and the differences are mechanical:

| Type | Success carries | Failure carries |
|---|---|---|
| `Result` | nothing | `string` |
| `Result<T>` | a `T` | `string` |
| `Result<T, E>` | a `T` | an `E` of your choosing |
| `UnitResult<E>` | nothing | an `E` of your choosing |

That's the whole matrix: *value or no value* × *string error or typed error*. Article B, Part 5 takes the typed-error column seriously; this part stays in the `string` column, which is where CFE's own examples live and where a team would start.

Translation note: LanguageExt's `Fin<T>` is roughly `Result<T, Error>` with a library-supplied `Error` type that can carry exceptions and codes. CFE gives you no error type at all — the default is `string`, and anything richer is your own class. Simpler, and less capable; Part 5 shows what you lose and what you build.

There is **no `Either<L, R>`**. If you need "one of two equally valid things," CFE has nothing for you and you should reach for a discriminated-union-shaped record hierarchy, or a `OneOf`-style library. `Result<T, E>` is failure-biased: every combinator short-circuits on `E`, so using it for a neutral choice will fight you.

## 3.2 — Constructing  `[ ]`

New file, `tests/Expenses.Tests/ResultTests.cs`:

```csharp
namespace Expenses.Tests;

using CSharpFunctionalExtensions;

public class ResultTests
{
    [Fact]
    public void WaysToMakeAResult()
    {
        Result plain          = Result.Success();
        Result<int> withValue = Result.Success(42);
        Result failed         = Result.Failure("nope");
        Result<int> failedT   = Result.Failure<int>("nope");
        Result<int> implicitly = 42;                        // implicit conversion from T

        // Conditional factories
        Result ifTrue  = Result.SuccessIf(true, "would-be error");
        Result ifFalse = Result.FailureIf(false, "would-be error");
        Result<int> conditionalValue = Result.SuccessIf(1 > 0, 42, "not positive");

        // Exception-to-failure
        Result<int> caught = Result.Try(() => int.Parse("not a number"));

        Assert.True(plain.IsSuccess);
        Assert.True(withValue.IsSuccess);
        Assert.True(failed.IsFailure);
        Assert.True(failedT.IsFailure);
        Assert.Equal(42, implicitly.Value);
        Assert.True(ifTrue.IsSuccess);
        Assert.True(ifFalse.IsSuccess);
        Assert.Equal(42, conditionalValue.Value);
        Assert.True(caught.IsFailure);
    }
}
```

Points worth dwelling on:

**`Result.Try` is the exception boundary.** Signature: `Result<T> Try<T>(Func<T> func, Func<Exception, string> errorHandler = null)`. Omit the handler and it uses `Result.Configuration.DefaultTryErrorHandler`, which ships as `exc => exc.Message`. So `Result.Try(() => File.ReadAllText(path))` turns an IO exception into a failure whose error is the exception's message. This is your wrapper for the parts of the BCL and the parts of your own codebase that throw — the "impure edge" adapter.

Set the default once at startup if the out-of-the-box behavior loses too much:

```csharp
Result.Configuration.DefaultTryErrorHandler = exc => $"{exc.GetType().Name}: {exc.Message}";
```

**`Result.Failure(null)` throws.** From the source's error-state guard, a failure must carry an error: passing `null` gets you an `ArgumentNullException`, and constructing a *success* that somehow carries an error gets you an `ArgumentException`. So "failure with no reason" is unrepresentable, which is the right call and a nice example of the library applying its own principle to itself.

**`Result.Of` is a near-synonym for `Success`** with a `where T : notnull` constraint — useful when you want the compiler to reject a nullable argument at the call site. Low-value until you're in NRT-strict code, where it's a small win.

## 3.3 — Reading a `Result` (and the two exceptions)  `[ ]`

```csharp
[Fact]
public void ReadingSuccessAndFailure()
{
    Result<int> ok   = Result.Success(42);
    Result<int> bad  = Result.Failure<int>("nope");

    Assert.Equal(42, ok.Value);
    Assert.Equal("nope", bad.Error);

    // Each side throws when read from the wrong state
    Assert.Throws<ResultFailureException>(() => bad.Value);
    Assert.Throws<ResultSuccessException>(() => ok.Error);

    // Deconstruction, if you want the imperative shape
    var (isSuccess, isFailure, value, error) = ok;
    Assert.True(isSuccess);
    Assert.False(isFailure);
    Assert.Equal(42, value);
    Assert.Null(error);
}
```

Note the symmetry that `Maybe<T>` lacks: **both** sides are guarded. Reading `.Value` on a failure throws `ResultFailureException`; reading `.Error` on a success throws `ResultSuccessException`. So `Result` can't be quietly misused the way `Maybe.GetValueOrDefault()` can — every wrong read is loud.

`Deconstruct` is interesting as an adoption tool. It gives a team a shape they already understand (`var (ok, _, value, error) = result;`) as a stepping stone, without the `Match` lambda they may find alien on day one. It's not the destination, but it's a ramp.

## 3.4 — The railway: `Map`, `Bind`, `Ensure`  `[ ]`

Now the expense parser. Create `src/Expenses/ExpenseParser.cs` (or start inline, your call):

```csharp
namespace Expenses;

using CSharpFunctionalExtensions;

public sealed record ExpenseEntry(DateOnly Date, decimal Amount, Category Category, string Description);

public sealed class ExpenseParser(CategoryCatalog catalog)
{
    public static Result<DateOnly> ParseDate(string input) =>
        DateOnly.TryParse(input, out var date)
            ? Result.Success(date)
            : Result.Failure<DateOnly>($"Not a date: '{input}'");

    public static Result<decimal> ParseAmount(string input) =>
        decimal.TryParse(input, out var amount)
            ? Result.Success(amount)
            : Result.Failure<decimal>($"Not an amount: '{input}'");

    public Result<Category> ResolveCategory(string name) =>
        catalog.Find(name).ToResult($"Unknown category: '{name}'");
}
```

`ToResult` is the bridge, and it's the moment the two halves of the library click together: a `Maybe<T>` plus a reason becomes a `Result<T>`. Signature `Result<T> ToResult<T>(this Maybe<T> maybe, string errorMessage)`. Absence becomes failure, and you supply the "why" that `Maybe` never had.

The chain:

```csharp
[Fact]
public void ParsesAGoodRow()
{
    var parser = new ExpenseParser(new CategoryCatalog([new Category("Groceries")]));

    Result<ExpenseEntry> result = ExpenseParser.ParseDate("2026-08-22")
        .Bind(date => ExpenseParser.ParseAmount("41.50")
            .Bind(amount => parser.ResolveCategory("Groceries")
                .Map(category => new ExpenseEntry(date, amount, category, "weekly shop"))));

    Assert.True(result.IsSuccess);
    Assert.Equal(41.50m, result.Value.Amount);
}

[Fact]
public void ShortCircuitsOnTheFirstFailure()
{
    var parser = new ExpenseParser(new CategoryCatalog([new Category("Groceries")]));

    Result<ExpenseEntry> result = ExpenseParser.ParseDate("not-a-date")
        .Bind(date => ExpenseParser.ParseAmount("also-not-a-number")
            .Bind(amount => parser.ResolveCategory("Yacht")
                .Map(category => new ExpenseEntry(date, amount, category, "?"))));

    Assert.True(result.IsFailure);
    Assert.Equal("Not a date: 'not-a-date'", result.Error);
}
```

That second test is the important one, and not because it passes. **Three things are wrong with that row, and the error tells you about one.** Short-circuiting is correct behavior for `Bind` — it's the same railway you know, and it's what you want when later steps *depend* on earlier ones. But "date, amount, and category are three independent fields, and the user deserves to hear about all three" is a different problem, and `Bind` structurally cannot solve it.

In LanguageExt you'd switch types at this point: `Validation<F, S>` with an applicative `Apply` accumulates all three failures. **CFE has no such type.** Article B, Part 6 covers what it has instead (`Result.Combine`) and how much worse it is. I'm flagging it here rather than there because this test is where you'd notice, and because it's the single biggest thing to have an opinion about before you recommend this library to anyone.

Also notice the shape of that chain: a nesting pyramid, because each `Bind` needs the previous values in scope. `Ensure` adds validation without the nesting:

```csharp
[Fact]
public void EnsureAddsRulesWithoutNesting()
{
    Result<decimal> result = ExpenseParser.ParseAmount("-5")
        .Ensure(a => a > 0, "Amount must be positive")
        .Ensure(a => a < 10_000, "Amount exceeds the single-expense limit");

    Assert.True(result.IsFailure);
    Assert.Equal("Amount must be positive", result.Error);
}
```

`Ensure(predicate, error)` keeps the value and fails when the predicate does. The error can be a `string` or a `Func<T, string>` if the message needs the value in it. Once a link fails, later `Ensure`s don't run — so, again, first-failure-wins.

There's also an `Ensure(Func<Result> predicate)` overload that takes a *Result*-returning check rather than a bool. That overlaps with `Check`, which Part 4 covers; when in doubt, `Check` is the one with the clearer name for that job.

## 3.5 — Landing the plane: `Match`, `Finally`  `[ ]`

Every railway ends at the edge of your system, where you have to produce something that isn't a `Result`.

```csharp
[Fact]
public void LandingTheChain()
{
    Result<decimal> ok  = ExpenseParser.ParseAmount("41.50");
    Result<decimal> bad = ExpenseParser.ParseAmount("nope");

    // Match: both branches, same return type
    string okMessage = ok.Match(
        onSuccess: a => $"Recorded {a:C}",
        onFailure: e => $"Rejected: {e}");

    // Finally: one lambda that receives the whole Result
    string badMessage = bad.Finally(r => r.IsSuccess ? $"Recorded {r.Value:C}" : $"Rejected: {r.Error}");

    Assert.StartsWith("Recorded", okMessage);
    Assert.Equal("Rejected: Not an amount: 'nope'", badMessage);

    // GetValueOrDefault, when the failure genuinely doesn't matter
    Assert.Equal(0m, bad.GetValueOrDefault());
}
```

Note the parameter names: `Result.Match` uses **`onSuccess` / `onFailure`**, while `Maybe.Match` uses **`Some` / `None`**. Inconsistent, and worth knowing so your IDE's parameter hints don't surprise you.

`Match` versus `Finally` is a real choice, not a synonym:

- **`Match`** gives you two lambdas and hands each one the *unwrapped* thing — the value, or the error. Use it when the two branches are genuinely different computations.
- **`Finally`** gives you one lambda and hands it the *whole* `Result`. Use it when the branches are near-identical, or when you're mapping to a type whose construction wants to see both — the canonical case being HTTP:

```csharp
.Finally(r => r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(r.Error));
```

That one line is a big part of why your Stage 3 minimal-API instinct was right, and it's the version of this library your team is most likely to find persuasive.

## 3.6 — Bridging `Maybe` and `Result`  `[ ]`

Four functions, and they're worth memorizing as a set because real code crosses this boundary constantly:

```csharp
[Fact]
public void BridgingBothWays()
{
    Maybe<Category> found   = new Category("Groceries");
    Maybe<Category> missing = Maybe<Category>.None;

    // Maybe → Result: absence becomes failure, and you supply the reason
    Assert.True(found.ToResult("not found").IsSuccess);
    Assert.Equal("not found", missing.ToResult("not found").Error);

    // Result → Maybe: the reason is discarded
    Assert.True(Result.Failure<int>("nope").AsMaybe().HasNoValue);
    Assert.Equal(Maybe.From(42), Result.Success(42).AsMaybe());

    // Result<Maybe<T>> → Result<T>: "I looked it up and it must be there"
    Result<Maybe<Category>> lookedUp = Result.Success(missing);
    Assert.Equal("no category", lookedUp.Required("no category").Error);

    // Result<T?> → Result<T>: kill the null, keep the rail
    Result<string?> nullable = Result.Success<string?>(null);
    Assert.True(nullable.EnsureNotNull("was null").IsFailure);
}
```

| Direction | Function | Note |
|---|---|---|
| `Maybe<T>` → `Result<T>` | `ToResult(error)` | the main one; also `ToResult<T,E>(E)` for typed errors |
| `Maybe<T>` → `UnitResult<E>` | `ToUnitResult(...)` | when the success side carries nothing |
| `Result<T>` → `Maybe<T>` | `AsMaybe()` | **lossy** — the error is thrown away |
| `Result<Maybe<T>>` → `Result<T>` | `Required(error)` | flattens the awkward double-wrap you get from a fallible lookup |
| `Result<T?>` → `Result<T>` | `EnsureNotNull(error)` | the NRT bridge inside a chain |

`Required` deserves a callout because the shape it fixes — `Result<Maybe<T>>` — is one you'll produce by accident within a week: any time an operation can *fail* (network, parse) *and* legitimately *find nothing*, you get both wrappers. `Required` collapses them by declaring that "found nothing" is, in this context, a failure. Its sibling `BindOptional` handles the other direction, where nothing-found should stay legal.

## 3.7 — Query syntax, once  `[ ]`

`Result` implements `Select` and `SelectMany`, so LINQ query syntax works and flattens the nesting pyramid from §3.4:

```csharp
[Fact]
public void QuerySyntaxFlattensThePyramid()
{
    var parser = new ExpenseParser(new CategoryCatalog([new Category("Groceries")]));

    Result<ExpenseEntry> result =
        from date in ExpenseParser.ParseDate("2026-08-22")
        from amount in ExpenseParser.ParseAmount("41.50")
        from category in parser.ResolveCategory("Groceries")
        select new ExpenseEntry(date, amount, category, "weekly shop");

    Assert.True(result.IsSuccess);
}
```

Type it once, run it, and know how to read it — CFE's README leads with this form, so you'll meet it in other people's code and in Stack Overflow answers. `from … from …` desugars to `SelectMany`, which is `Bind`; `select` desugars to `Select`, which is `Map`. It's sugar over the exact chain you already wrote, with the flat scoping that `Bind`'s nesting denies you.

Then set it aside. You prefer the fluent form, the fluent form keeps the `Map`/`Bind` vocabulary visible, and the pipeline vocabulary in Part 4 has no query-syntax equivalent at all — the moment you want a `Tap` or an `Ensure` mid-chain, you're back to method calls anyway. The one place query syntax genuinely wins is async composition, which Article B revisits.

The fluent answer to the same nesting problem is `BindZip`, which pairs each step's value with the accumulated tuple:

```csharp
Result<ExpenseEntry> result = ExpenseParser.ParseDate("2026-08-22")
    .BindZip(_ => ExpenseParser.ParseAmount("41.50"))                      // Result<(DateOnly, decimal)>
    .BindZip(_ => parser.ResolveCategory("Groceries"))                     // Result<(DateOnly, decimal, Category)>
    .Map(t => new ExpenseEntry(t.First, t.Second, t.Third, "weekly shop"));
```

Overloads exist to extend a 2-tuple to a 3-tuple and onward for a few arities; check the set in your IDE rather than trusting me on how far it goes. It's still short-circuiting — `BindZip` accumulates *values*, not errors.

## Type-along checklist — Part 3

- [ ] The four factory families, including `Try` and `SuccessIf`
- [ ] `Result.Failure(null)` throws (predict-then-check below)
- [ ] `.Value` / `.Error` and their two exceptions; `Deconstruct`
- [ ] `ExpenseEntry` + `ExpenseParser` with `ParseDate` / `ParseAmount` / `ResolveCategory`
- [ ] A `Bind` chain that succeeds
- [ ] A `Bind` chain that short-circuits — **and noticing it reports only one of three errors**
- [ ] `Ensure` with two stacked rules
- [ ] `Match` versus `Finally`
- [ ] All five bridge functions
- [ ] Query syntax once; `BindZip` as the fluent alternative

## Exercises — Part 3

**Predict-then-check.**

1. `Result.Failure<int>("")` — success, failure, or throw?
2. `Result<int> r = Result.Success(42); r.Error` — you know it throws. Now: `r.GetValueOrDefault(selector: v => v.ToString(), defaultValue: "none")` — what do you get, and did you need to check `IsSuccess` first?
3. `ParseAmount("41.50").Ensure(a => a > 0, "must be positive").Map(a => a * 2).Ensure(a => a < 50, "too big")` — what's the result, and how many of the four operations actually executed?

**Explain-it-back.** Your teammate says: *"This is just exceptions with extra steps. I already have try/catch, and it's less typing."* Give the honest answer in four sentences — including the part where they're partly right. (Two threads to pull: which failures are *expected* versus *exceptional*; and what a method signature tells a caller in each style.)

---

# Part 4 — The pipeline vocabulary

This is the part with no LanguageExt equivalent, and the part I'd expect to sell the library to your team. Everything so far has been "LanguageExt, renamed." This isn't.

## 4.1 — The problem  `[ ]`

Real application code isn't a pure transformation from input to output. It's a transformation *plus* a pile of things that have to happen along the way: persist this, notify that, log the failure, roll back if the third step fails, don't bother with step four unless a flag is set. In the functional-core/imperative-shell split, this is the shell — and the shell is where most FP libraries leave you on your own.

Here's the shape without the vocabulary — an "approve an expense" use case. This one is **illustration, not type-along** (it leans on ASP.NET's `Results` and a logger that don't exist in your test project); read it, and keep it beside you for the comparison in §4.8:

```csharp
public IResult Approve(string id)
{
    var entry = _repository.Find(id);
    if (entry is null)
        return Results.NotFound($"No expense with id '{id}'");

    if (entry.Amount <= 0)
    {
        _log.Warning("Approval failed for {Id}: non-positive amount", id);
        return Results.BadRequest("Amount must be positive");
    }

    var policyCheck = _policy.AllowsApproval(entry);
    if (!policyCheck.IsAllowed)
    {
        _log.Warning("Approval failed for {Id}: {Reason}", id, policyCheck.Reason);
        return Results.BadRequest(policyCheck.Reason);
    }

    _repository.MarkApproved(entry);
    _notifier.Notify(entry);
    return Results.Ok(entry);
}
```

Nothing *wrong* with it. It's also 20 lines in which the happy path is interleaved with three failure paths, the logging is duplicated, and adding a fourth rule means finding the right place to insert another `if` block. You've read a thousand of these. Some of them were 200 lines.

## 4.2 — `Tap` and `TapError`  `[ ]`

`Tap` runs a side effect and passes the value through unchanged, so effects stop interrupting the chain:

```csharp
[Fact]
public void TapRunsOnSuccessOnly_TapErrorOnFailureOnly()
{
    var log = new List<string>();

    Result<decimal> ok = ExpenseParser.ParseAmount("41.50")
        .Tap(a => log.Add($"parsed {a}"))
        .TapError(e => log.Add($"failed: {e}"));

    Result<decimal> bad = ExpenseParser.ParseAmount("nope")
        .Tap(a => log.Add($"parsed {a}"))
        .TapError(e => log.Add($"failed: {e}"));

    Assert.True(ok.IsSuccess);
    Assert.True(bad.IsFailure);
    Assert.Equal(new[] { "parsed 41.50", "failed: Not an amount: 'nope'" }, log);
}
```

Overloads take `Action<T>` (gets the value), or plain `Action` (doesn't care). `TapError` takes `Action<string>` for `Result<T>` — or `Action<E>` for the typed variants — or a bare `Action`.

The mental model, which generalizes to the whole vocabulary: **`Tap` is a `Map` that can't change anything.** It's the type system letting you do the impure thing while promising it didn't touch the value. That promise is why a `Tap`-heavy chain is still readable top to bottom: you know at a glance which lines can change the outcome (`Bind`, `Ensure`, `Check`, `Map`) and which are just noise-with-purpose (`Tap`, `TapError`).

## 4.3 — `Ensure` versus `Check` versus `Bind`  `[ ]`

Three ways to add a step that can fail, and picking correctly is most of what fluency in this library means.

```csharp
[Fact]
public void EnsureVersusCheckVersusBind()
{
    // Ensure: a bool predicate. Keeps the value.
    Result<decimal> ensured = ExpenseParser.ParseAmount("41.50")
        .Ensure(a => a > 0, "must be positive");
    Assert.Equal(41.50m, ensured.Value);

    // Check: a Result-returning step. Keeps the ORIGINAL value, discards the check's.
    Result<decimal> checked_ = ExpenseParser.ParseAmount("41.50")
        .Check(a => WithinDailyLimit(a));            // returns Result<decimal> — its value is dropped
    Assert.Equal(41.50m, checked_.Value);

    // Bind: a Result-returning step. Keeps the NEW value.
    Result<string> bound = ExpenseParser.ParseAmount("41.50")
        .Bind(a => Describe(a));                     // returns Result<string> — its value is kept
    Assert.Equal("41.50 USD", bound.Value);

    static Result<decimal> WithinDailyLimit(decimal amount) =>
        amount < 500 ? Result.Success(amount) : Result.Failure<decimal>("over the daily limit");

    static Result<string> Describe(decimal amount) => Result.Success($"{amount} USD");
}
```

The decision table — worth internalizing until it's reflexive, because it's the table you'll consult twenty times an hour when you start writing this code for real:

| Your step is… | and you want to keep… | use |
|---|---|---|
| `T → B` (can't fail) | the new value | **`Map`** |
| `T → Result<B>` | the **new** value | **`Bind`** |
| `T → bool` (+ an error message) | the **original** value | **`Ensure`** |
| `T → Result<anything>` | the **original** value | **`Check`** |
| `T → void` (a side effect) | the **original** value | **`Tap`** |

`Check` is the one with no analogue anywhere in LanguageExt, and it's more useful than it looks. It's for validation that needs to *do work* rather than evaluate a predicate — "is this category still active?" requires a lookup that can itself fail, and you want the original entry to keep flowing regardless of what the lookup returned. Without `Check` you'd write `.Bind(e => IsActive(e.Category).Map(_ => e))`, which works and obscures the intent.

## 4.4 — `Compensate`: recovering from failure  `[ ]`

`Compensate` is the mirror image of `Bind` — it runs only on the failure path, and it can put you back on the success rail:

```csharp
[Fact]
public void CompensateRecovers()
{
    Result<Category> resolved = LookUpInPrimaryCatalog("Groceries")
        .Compensate(error => LookUpInFallbackCatalog("Groceries"));

    Assert.True(resolved.IsSuccess);
    Assert.Equal("Groceries (fallback)", resolved.Value.Name);

    static Result<Category> LookUpInPrimaryCatalog(string _) =>
        Result.Failure<Category>("primary catalog unavailable");

    static Result<Category> LookUpInFallbackCatalog(string name) =>
        Result.Success(new Category($"{name} (fallback)"));
}
```

It receives the error, so you can decide whether *this particular* failure is recoverable:

```csharp
.Compensate(error => error.Contains("unavailable")
    ? LookUpInFallbackCatalog(name)
    : Result.Failure<Category>(error))
```

`OnFailureCompensate` does very nearly the same job; the difference is that it also offers a no-argument overload (`Func<Result<T>>`) for when you don't care *why* it failed, and `Compensate` always hands you the error. They overlap enough that I'd pick one and standardize — `Compensate` reads better and always gives you the information, so make it the house style and treat `OnFailureCompensate` as something you recognize in other people's code.

`Maybe`'s `Or` is the same idea one type down: fallback on the empty path.

## 4.5 — `Finally`: leaving the rails  `[ ]`

Covered in §3.5, but it belongs to this vocabulary. `Finally` is the terminus — it takes the whole `Result` and returns something else entirely, which is exactly what an application boundary needs to do:

```csharp
[Fact]
public void FinallyLandsTheChain()
{
    string rendered = ExpenseParser.ParseAmount("nope")
        .Ensure(a => a > 0, "must be positive")
        .Finally(r => r.IsSuccess ? $"OK {r.Value}" : $"ERR {r.Error}");

    Assert.Equal("ERR Not an amount: 'nope'", rendered);
}
```

Anything CFE-shaped stops at `Finally`. Everything downstream is ordinary C#. That's the "unwrap at the very end" discipline you already know from LanguageExt, given a name and a single obvious place to put it.

## 4.6 — The `…If` family  `[ ]`

Conditional links that pass through untouched when the condition is false:

```csharp
[Fact]
public void ConditionalLinks()
{
    var log = new List<string>();
    bool auditingEnabled = false;

    Result<decimal> result = ExpenseParser.ParseAmount("41.50")
        .TapIf(auditingEnabled, a => log.Add($"audited {a}"))
        .MapIf(a => a > 100, a => a * 0.9m)              // volume discount, only over 100
        .CheckIf(auditingEnabled, a => RecordAudit(a));

    Assert.Equal(41.50m, result.Value);
    Assert.Empty(log);

    static Result RecordAudit(decimal amount) => Result.Success();
}
```

`BindIf`, `MapIf`, `TapIf`, `CheckIf`, `TapErrorIf` all follow the pattern, and each comes in two flavors: a `bool condition` (evaluated by the caller) and a `Func<T, bool> predicate` (evaluated against the flowing value). `EnsureNot` is the same idea for inverted predicates.

Honest assessment: this family is the least essential part of the vocabulary. `.MapIf(cond, f)` versus `.Map(x => cond ? f(x) : x)` is a genuine readability win, but a chain with four `…If`s in it has usually smuggled control flow into a pipeline that would read better as an `if` statement. Use them where they clarify; be suspicious when they multiply.

## 4.7 — The `…Try` family  `[ ]`

For steps that throw. `MapTry` / `BindTry` / `TapTry` / `OnSuccessTry` wrap the delegate in a try/catch and turn an exception into a failure:

```csharp
[Fact]
public void TryVariantsCatchExceptions()
{
    Result<int> result = Result.Success("not a number")
        .MapTry(s => int.Parse(s));                       // throws → failure, no exception escapes

    Assert.True(result.IsFailure);
    Assert.Contains("not a number", result.Error);        // default handler is exc => exc.Message

    Result<int> custom = Result.Success("not a number")
        .MapTry(s => int.Parse(s), exc => $"bad input: {exc.GetType().Name}");

    Assert.Equal("bad input: FormatException", custom.Error);
}
```

Each takes an optional `Func<Exception, string> errorHandler`, defaulting to `Result.Configuration.DefaultTryErrorHandler` (which ships as `exc => exc.Message`).

**Use these at the boundary, not everywhere.** `MapTry` around a call into legacy code or a throwing BCL API is exactly right. `MapTry` sprinkled through your own domain logic means you've given up on knowing what can fail — a `catch`-all with better syntax. The point of the railway is that failures are *visible in the types*; `…Try` is the adapter that gets exception-shaped code onto the rails, and its blast radius should be the adapter layer.

Also here: `WithTransactionScope`, `BindWithTransactionScope`, `MapWithTransactionScope`, which wrap a segment in a `TransactionScope` and commit only if the segment succeeds. Useful, `System.Transactions`-flavored, and worth knowing exists.

## 4.8 — The whole thing  `[ ]`

Now rewrite §4.1's use case with the vocabulary. This is the payoff, and it's worth typing carefully and then reading side by side with the imperative version above.

The collaborators are invented, so stub them first — inline in the test file is fine, they exist only to make the chain compile:

```csharp
public interface IExpenseRepository
{
    Maybe<ExpenseEntry> Find(string id);
    void MarkApproved(ExpenseEntry entry);
}

public interface IApprovalPolicy
{
    Result AllowsApproval(ExpenseEntry entry);   // must return Result for Check to accept it
}

public interface INotifier
{
    void Notify(ExpenseEntry entry);
}

public interface IAuditLog
{
    void Warning(string message);
}
```

Then the service:

```csharp
public sealed class ExpenseApprovalService(
    IExpenseRepository repository,
    IApprovalPolicy policy,
    INotifier notifier,
    IAuditLog log)
{
    public Result<ExpenseEntry> Approve(string id) =>
        repository.Find(id)                                              // Maybe<ExpenseEntry>
            .ToResult($"No expense with id '{id}'")                      // Result<ExpenseEntry>
            .Ensure(e => e.Amount > 0, "Amount must be positive")
            .Check(e => policy.AllowsApproval(e))                        // Result — value passes through
            .Tap(e => repository.MarkApproved(e))
            .Tap(e => notifier.Notify(e))
            .TapError(error => log.Warning($"Approval failed for {id}: {error}"));
}
```

And at the HTTP boundary, one more line — illustration again, since there's no web host here; this is the shape Stage 3 builds for real:

```csharp
app.MapPost("/expenses/{id}/approve", (string id, ExpenseApprovalService svc) =>
    svc.Approve(id).Finally(r => r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(r.Error)));
```

What changed, concretely:

- **The happy path reads top to bottom** as a list of steps. The failure paths aren't interleaved with it; they're implied by the operators.
- **Logging happens once**, in one place, for every failure — including failures added later by someone who never reads this paragraph.
- **Adding a rule is adding a line.** No decision about where the `if` goes, no chance of putting it after the side effect by mistake.
- **The ordering constraint is enforced by types.** You cannot `Tap(MarkApproved)` before the `Ensure`, because there's nothing to tap yet.
- **The signature tells the truth.** `Result<ExpenseEntry>` says "this can fail, and the reason is a string." The imperative version's `IResult` said nothing.

Two honest caveats, because this is the example that oversells the library everywhere it appears:

1. **The error is still a bare `string`**, so the HTTP layer can't distinguish "not found" (404) from "policy violation" (400) — everything lands as `BadRequest`. Fixing that needs `Result<T, E>`, and that's Article B, Part 5. If you show this chain to your team, expect this to be the first question, and have the answer.
2. **The debugger experience is worse.** Stepping through a lambda chain is genuinely more annoying than stepping through `if` blocks, and a stack trace from inside a `Tap` is less informative than one from a named method. That's a real cost, not FUD, and your team will feel it in week one. Worth an `ADOPTION.md` entry.

## Type-along checklist — Part 4

- [ ] Read the imperative "before" version and keep it visible for the §4.8 comparison
- [ ] `Tap` / `TapError` with a log list
- [ ] `Ensure` vs `Check` vs `Bind`, all three in one test
- [ ] `Compensate`, including the conditional-recovery form
- [ ] `Finally` at the end of a failing chain
- [ ] One `…If` (`TapIf` or `MapIf`) — enough to know the shape
- [ ] `MapTry` with the default handler and with a custom one
- [ ] The full `Approve` pipeline, read side by side with the imperative version

## Exercises — Part 4

**Predict-then-check.**

1. In the `Approve` chain, the policy check fails. How many of the two `Tap`s run? Now: `Ensure` fails instead. Same question.
2. `Result.Success(5).Check(v => Result.Success(v * 100))` — what's the value at the end, `5` or `500`? Why?
3. `.TapError(e => throw new Exception("boom"))` on a failed result — what happens? (Think about it before running: is `Tap` protected, or is that what `TapTry` is for?)

**Explain-it-back.** You're in a code review. Someone has written the §4.1 imperative version. Write the review comment you'd actually leave — one that teaches the `Result` chain without being insufferable about it, and that acknowledges what their version does better. This is the most realistic Teach rehearsal in the article, because it's the exact situation where the opportunity will actually show up.

---

# Where you are

You can now write ordinary application code in this library. Specifically:

- `Maybe<T>` for absence, consumed through `Match` / `GetValueOrDefault` / `TryGetValue`, with `.Value` recognized as the escape hatch it is.
- `Result` / `Result<T>` for failure, composed with `Map` / `Bind` / `Ensure`, bridged to and from `Maybe`, landed with `Match` or `Finally`.
- The pipeline vocabulary — `Tap`, `Check`, `Compensate`, the `…If` and `…Try` families — for the impure shell.

That's about 80% of what you'd type in a real codebase.

**What's still missing, and where it lives:**

| Gap you'll have noticed | Where |
|---|---|
| Errors are `string`s, so callers can't branch on *kind* of failure | Article B, Part 5 — `Result<T, E>`, `UnitResult<E>` |
| Short-circuiting reports one error when three fields are bad | Article B, Part 6 — `Result.Combine`, and the honest LanguageExt comparison |
| `Category` is still a `record` wrapping an unvalidated `string` | Article B, Part 7 — `ValueObject`, `Create` factories |
| Everything here is synchronous | Article B, Part 8 — the `Task`/`ValueTask` overloads |

## Before you move on

**Write the first `ADOPTION.md` entry.** Four lines, per the format in the plan: *date · what I hit · what I concluded · what I'd tell the team.* Candidate subjects, pick whichever you actually have an opinion about:

- `.Value` existing at all — is a documented convention enough for a team, or does it need the analyzer?
- One constructor that swallows `null` versus two constructors where one throws.
- Whether the pipeline vocabulary is worth a dependency on its own merits, independent of `Maybe`/`Result`.

**Optional application challenge** (~20–30 minutes, fresh domain, skip it freely): a **vending machine**. `Maybe<Product>` for slot lookup, `Result<Coin>` for parsing an inserted coin, and a `Dispense` operation that has to check the slot exists, the product is in stock, and the balance is sufficient — then decrement stock and record the sale as side effects. Do it with one chain and no `if` statements. You'll reach for `ToResult`, `Ensure`, `Check`, and `Tap`, and you'll notice within five minutes that "insufficient funds" and "out of stock" both being `string`s is unsatisfying — which is precisely the itch Article B, Part 5 scratches.

---

## Notes & questions

_Yours._

-
