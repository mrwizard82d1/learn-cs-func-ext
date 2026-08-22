# CSharpFunctionalExtensions: Going Deeper

*Article B of two. Typed errors, error accumulation, domain modeling, and the async/tooling story — the parts you reach for once `Maybe` and the `Result` railway are reflexive.*

> Read [`01-the-working-tour.md`](01-the-working-tour.md) first. Plan and context: [`../TUTORIAL_PLAN.md`](../TUTORIAL_PLAN.md). Still **Stage 1 — SEE**.
> Verified against **CSharpFunctionalExtensions 3.7.0** source (tag `v3.7.0`). Where I'm hedging or couldn't confirm something, I say so explicitly.

---

## How to read this

Same two passes as Article A: read it through, then start over and type it. Section headers end with `[ ]` for you to flip; the type-along checklists at the end of each part are mine to maintain.

One difference in character, worth setting expectations for. Article A was mostly *"here's the tool, here's how it works."* This one has more *judgment* in it, because three of its four parts are places where the library is either incomplete (Part 6), overlapping with something newer in the language (Part 7), or dependent on third-party packages of uneven health (Part 8). That's not a criticism of the library — it's what "going deeper" actually means for any library that's eleven years old and deliberately small. But it means this article is where most of your `ADOPTION.md` entries will come from.

Article A ended with four named gaps. This article closes them in order:

| Gap from Article A | Part |
|---|---|
| Errors are `string`s, so callers can't branch on the *kind* of failure | **5** |
| Short-circuiting reports one error when three fields are bad | **6** |
| `Category` is a `record` wrapping an unvalidated `string` | **7** |
| Everything is synchronous | **8** |

---

# Part 5 — Typed errors

## 5.1 — The problem, precisely  `[ ]`

Article A's approval pipeline ended here:

```csharp
svc.Approve(id).Finally(r => r.IsSuccess ? Results.Ok(r.Value) : Results.BadRequest(r.Error));
```

Everything that fails becomes a 400. But "no expense with that id" is a 404, "amount must be positive" is a 400, and "the approval service is down" is a 503. The information exists — it's *in* the string — and the only way to recover it is to parse English, which is how you end up with `if (error.Contains("not found"))` in production code. I have seen this. You have seen this.

The fix is to make the error a type instead of a sentence. That's `Result<T, E>`.

## 5.2 — `Result<T, E>` and `UnitResult<E>`  `[ ]`

`E` is entirely yours — the library supplies no error type at all. That's the biggest difference from LanguageExt, where `Error` is a real hierarchy that carries codes, messages, inner exceptions, and knows how to combine.

Start by modeling the errors. The nearest thing C# has to a discriminated union is a sealed hierarchy of records:

```csharp
namespace Expenses;

public abstract record ExpenseError(string Message)
{
    public sealed record NotFound(string Id)          : ExpenseError($"No expense with id '{Id}'");
    public sealed record InvalidAmount(string Reason) : ExpenseError(Reason);
    public sealed record UnknownCategory(string Name) : ExpenseError($"Unknown category: '{Name}'");
    public sealed record PolicyViolation(string Rule) : ExpenseError($"Policy: {Rule}");
    public sealed record Unavailable(string Service)  : ExpenseError($"{Service} is unavailable");
}
```

Nesting the cases inside the abstract parent is a convention I like for this: it keeps `ExpenseError.NotFound` readable at the use site and makes the closed set obvious. (Sealing the leaves and leaving the base abstract is as close to "closed" as C# gets — nothing stops someone in another file from deriving another case. C# still has no real DU; if that bothers you, this is a place where F# or a `OneOf`-style library would be honest about it.)

Now the parser, retyped:

```csharp
using CSharpFunctionalExtensions;

public sealed class ExpenseParser(CategoryCatalog catalog)
{
    public static Result<decimal, ExpenseError> ParseAmount(string input) =>
        decimal.TryParse(input, out var amount)
            ? Result.Success<decimal, ExpenseError>(amount)
            : Result.Failure<decimal, ExpenseError>(new ExpenseError.InvalidAmount($"Not an amount: '{input}'"));

    public Result<Category, ExpenseError> ResolveCategory(string name) =>
        catalog.Find(name).ToResult<Category, ExpenseError>(new ExpenseError.UnknownCategory(name));
}
```

And there's the first thing you'll notice: **the ceremony**. `Result.Failure<decimal, ExpenseError>(...)` is not a pleasant thing to type forty times a day, and C# won't infer those arguments for you because the value type and the error type can't both be deduced from one argument.

The implicit conversions are the relief valve, and they're the reason to know they exist:

```csharp
[Fact]
public void ImplicitConversionsCutTheCeremony()
{
    // Both directions convert implicitly: T → success, E → failure
    Result<decimal, ExpenseError> ok  = 41.50m;
    Result<decimal, ExpenseError> bad = new ExpenseError.InvalidAmount("nope");

    Assert.True(ok.IsSuccess);
    Assert.True(bad.IsFailure);
    Assert.Equal("nope", bad.Error.Message);
}
```

Which lets the parser read like this instead:

```csharp
public static Result<decimal, ExpenseError> ParseAmount(string input) =>
    decimal.TryParse(input, out var amount)
        ? amount
        : new ExpenseError.InvalidAmount($"Not an amount: '{input}'");
```

Much better — though note that this only works because the ternary's target type is known from the return type. In a `var` context you're back to the explicit factories.

`UnitResult<E>` is the same idea for operations with nothing to return:

```csharp
public UnitResult<ExpenseError> Delete(string id) =>
    repository.Exists(id)
        ? UnitResult.Success<ExpenseError>()
        : UnitResult.Failure(new ExpenseError.NotFound(id));
```

The four-way matrix in full — memorize the shape, not the names:

| Type | Success carries | Failure carries |
|---|---|---|
| `Result` | nothing | `string` |
| `Result<T>` | `T` | `string` |
| `Result<T, E>` | `T` | `E` |
| `UnitResult<E>` | nothing | `E` |

They interconvert by implicit operator in the directions you'd expect: `Result<T> → Result`, `Result<T> → UnitResult<string>`, `Result<T, E> → UnitResult<E>`, `Result → UnitResult<string>`. So a method returning `Result<T>` can be consumed where a `Result` is wanted, and dropping the value is free.

## 5.3 — Three traps  `[ ]`

The implicit conversions that just saved you all that typing also set three traps. All three are worth pinning with a test so you recognize them later.

**Trap 1: `T` and `E` must not be the same type.** `Result<string, string>` declares two implicit conversion operators with identical signatures. The type itself compiles, but using the conversions is ambiguous, and if you're lucky you get a compiler error rather than the wrong branch:

```csharp
// Don't. Result<string, string> makes "is this a value or an error?" undecidable.
Result<string, string> confused = "hello";   // success or failure? The compiler can't tell either.
```

The rule: **make `E` a type that could never be a legitimate success value.** A dedicated error record does that by construction. A `string` error with a `string` value does not.

**Trap 2: a stray `E`-typed value silently becomes a failure.** Because `E → Result<T, E>` is implicit, any code path that returns something of type `E` produces a failure — including one you didn't mean as an error:

```csharp
[Fact]
public void AStrayErrorTypedValueBecomesAFailure()
{
    // Looks like it's building a value. It's building a failure.
    Result<Category, ExpenseError> r = new ExpenseError.NotFound("42");

    Assert.True(r.IsFailure);
}
```

With `E` as `string` — the tempting shortcut for "I want typed errors but I don't want to write a type" — this bites hard: every string-returning expression in that method now converts to a failure. Another reason `E` should be a purpose-built type.

**Trap 3: no variance.** The implicit conversion is from `E` exactly. If `E` is `ExpenseError` and you have an `ExpenseError.NotFound`, the conversion works (it's a reference conversion to the base first). But going the other way — a `Result<T, NotFound>` where a `Result<T, ExpenseError>` is wanted — does *not* convert, because `Result<T, E>` is a struct and structs have no variance. You'll be writing `MapError` to widen:

```csharp
Result<Category, ExpenseError> widened = narrowResult.MapError(e => (ExpenseError)e);
```

If that pattern feels familiar, it should: it's the same `MapLeft`-to-widen move you worked out in the LanguageExt library kata, where you reconciled a `NotFound(Error)` with a `CannotBorrow(BorrowError)` by mapping the error side and casting to the common base. Same problem, same solution, different spelling. Declare your error parameter at the widest type you'll need and you mostly avoid it.

## 5.4 — Working with the error side  `[ ]`

Two operations, and between them they cover everything.

**`MapError`** transforms the failure and leaves success alone. It's the workhorse, and it has an overload for every direction across the four types:

```csharp
[Fact]
public void MapErrorTranslatesBetweenErrorWorlds()
{
    // string error → typed error (Result<T> → Result<T, E>)
    Result<decimal, ExpenseError> typed = Result.Failure<decimal>("Not an amount: 'nope'")
        .MapError(msg => (ExpenseError)new ExpenseError.InvalidAmount(msg));

    // typed error → string (Result<T, E> → Result<T>)
    Result<decimal> stringly = typed.MapError(e => e.Message);

    Assert.IsType<ExpenseError.InvalidAmount>(typed.Error);
    Assert.Equal("Not an amount: 'nope'", stringly.Error);
}
```

**A resolution wrinkle to expect.** Going from a typed error *to* a `string` error is genuinely ambiguous on paper: `MapError<T, E>(this Result<T, E>, Func<E, string>)` returns `Result<T>`, while `MapError<T, E, E2>(this Result<T, E>, Func<E, E2>)` with `E2 = string` returns `Result<T, string>`. Both match a lambda returning `string`, and C# ignores the return type when resolving overloads. If the compiler complains, state the type arguments explicitly — `typed.MapError<decimal, ExpenseError, string>(e => e.Message)` — or map to something that isn't `string`. I flagged this from reading the signatures rather than by compiling it, so it may resolve cleanly in practice; either way, now you know where to look if it doesn't.

That first one is the important pattern: `MapError` is how you **adapt a `string`-error library call into your typed-error domain** at the boundary. Since half the world (including `Result.Try`) produces `string` errors, this is a line you'll write often. All `MapError` overloads also come in a `TContext` flavor that passes state to the lambda instead of capturing it, if you're ever chasing allocations.

**`ConvertFailure<K>()`** re-types a failure that has no value to carry:

```csharp
Result<Category, ExpenseError> failed = new ExpenseError.NotFound("42");
Result<ExpenseEntry, ExpenseError> retyped = failed.ConvertFailure<ExpenseEntry>();
```

It exists because a failed `Result<Category, E>` and a failed `Result<ExpenseEntry, E>` hold exactly the same information, but the type system won't let you pass one as the other. It's an instance method (not an extension) on all four types. It only makes sense on a failure — I'd expect it to throw on a success, though I read the signatures rather than the body, so confirm that with a test rather than taking my word for it.

## 5.5 — The payoff: errors that reach the boundary intact  `[ ]`

Now the thing that motivated all of this:

```csharp
public static IResult ToHttp(this Result<ExpenseEntry, ExpenseError> result) =>
    result.Finally(r => r.IsSuccess
        ? Results.Ok(r.Value)
        : r.Error switch
        {
            ExpenseError.NotFound e        => Results.NotFound(e.Message),
            ExpenseError.UnknownCategory e => Results.BadRequest(e.Message),
            ExpenseError.InvalidAmount e   => Results.BadRequest(e.Message),
            ExpenseError.PolicyViolation e => Results.Conflict(e.Message),
            ExpenseError.Unavailable e     => Results.Problem(e.Message, statusCode: 503),
            _                              => Results.Problem("Unexpected error")
        });
```

*(Illustration — no web host in the test project. Stage 3 builds this for real.)*

One `switch` expression, in one place, and the compiler tells you when you add a case and forget to handle it — as a warning on the missing arm if you drop the `_`, which for a sealed-ish hierarchy is the closest C# gets to exhaustiveness checking.

**The honest cost accounting**, because this is a real decision and not a free upgrade:

| | `string` errors | typed errors (`Result<T, E>`) |
|---|---|---|
| Ceremony at call sites | low | **high** — explicit generic args, or reliance on implicit conversion |
| Callers can branch on failure kind | no (or by string matching) | **yes** |
| Adding a new failure mode | free | touches the error type + every exhaustive `switch` |
| Interop with `Result.Try` and other libraries | direct | needs `MapError` at each boundary |
| Right choice for | internal helpers, leaf functions | **use-case boundaries, anything an API surface returns** |

My recommendation, and the one I'd put in the adoption journal: **`Result<T>` inside a module, `Result<T, E>` at the module's edge.** Typed errors earn their ceremony exactly where someone downstream has to make a decision based on *which* thing went wrong. Everywhere else they're tax. Being able to state that rule crisply is worth more to a team than either extreme.

## Type-along checklist — Part 5

- [ ] `ExpenseError` hierarchy
- [ ] `ParseAmount` / `ResolveCategory` returning `Result<T, ExpenseError>`
- [ ] Both implicit conversions (`T` → success, `E` → failure), and the terser parser they enable
- [ ] `UnitResult<ExpenseError>` for an operation with no return value
- [ ] All three traps pinned as tests
- [ ] `MapError` in all three directions
- [ ] `ConvertFailure<K>()`, including what it does on a success (find out)
- [ ] The error-to-HTTP `switch` (read only)

## Exercises — Part 5

**Predict-then-check.**

1. `Result<string, string> r = "hello";` — compiler error, or does it pick a branch? If it picks one, which?
2. `Result.Success<decimal, ExpenseError>(41.50m).MapError(e => e.Message)` — what type comes out, and does the lambda run?
3. `Result<Category, ExpenseError> ok = someCategory; ok.ConvertFailure<ExpenseEntry>();` — called on a **success**. Throw, or a success of the new type?

**Explain-it-back.** Your teammate asks: *"Why not just make the error an `int` code, or an enum? Why a whole record hierarchy?"* Answer in four sentences. (The good answer involves what a `NotFound` needs to carry that a code can't, and what happens to the enum approach when someone needs to add a field to just one case.)

---

# Part 6 — Error accumulation, and the gap

This is the part where I stop selling and tell you what's missing. It's also the part most likely to determine your recommendation, so it's worth reading slowly.

## 6.1 — The problem restated  `[ ]`

Article A left this test on the floor:

```csharp
// Three things are wrong with this row. The error mentions one.
ExpenseParser.ParseDate("not-a-date")
    .Bind(date => ExpenseParser.ParseAmount("also-not-a-number")
        .Bind(amount => parser.ResolveCategory("Yacht")
            .Map(category => new ExpenseEntry(date, amount, category, "?"))));
// → Failure("Not a date: 'not-a-date'")
```

Short-circuiting is *correct* for `Bind` — later steps depend on earlier values, so there's nothing else it could do. But date, amount, and category are **independent**: none needs the others to be validated. A user submitting a form with three bad fields deserves to hear about three bad fields.

In LanguageExt you'd change types. `Validation<F, S>` is applicative rather than monadic, and `(v1, v2, v3).Apply(...)` runs all three and concatenates failures. You worked exactly this problem in Phase 3, and the test name you wrote there — `ParseEntry_ShortCircuitsIncorrectlyIfBothBad` — names the exact defect.

**CFE has no `Validation` type and no `Apply`.** What it has is `Result.Combine`.

## 6.2 — `Result.Combine`  `[ ]`

```csharp
[Fact]
public void CombineReportsAllTheFailures()
{
    Result<DateOnly> date     = ExpenseParser.ParseDate("not-a-date");
    Result<decimal>  amount   = ExpenseParser.ParseAmount("also-not-a-number");
    Result<Category> category = _parser.ResolveCategory("Yacht");

    Result combined = Result.Combine(date, amount, category);

    Assert.True(combined.IsFailure);
    Assert.Equal(
        "Not a date: 'not-a-date', Not an amount: 'also-not-a-number', Unknown category: 'Yacht'",
        combined.Error);
}
```

All three errors, joined with `", "` — the default from `Result.Configuration.ErrorMessagesSeparator`, overridable per call or globally:

```csharp
Result.Combine("; ", date, amount, category);   // separator first, then the results
```

Now look hard at that return type. **`Result`, not `Result<(DateOnly, decimal, Category)>`.** `Combine` tells you *whether* everything succeeded and *what all the errors were*. It does not give you the values back. So the officially-documented usage — this is straight from CFE's README, adapted — is:

```csharp
Result<CustomerName> name = CustomerName.Create(model.Name);
Result<Email> email = Email.Create(model.PrimaryEmail);

Result result = Result.Combine(name, email);
if (result.IsFailure)
    return Error(result.Error);

var customer = new Customer(name.Value, email.Value);   // ← .Value, unguarded, by design
```

Read that last line again. **The library's own recommended accumulation pattern requires you to use the unguarded `.Value` escape hatch.** It's safe — `Combine` just proved every input succeeded — but the compiler doesn't know that, so you're back to a convention held together by the reader's attention. If you adopt a "never touch `.Value`" rule for your team (and Article A argued you should), this is the one place the library itself will violate it.

That's the gap, concretely. Not "CFE lacks a fancy type" — **CFE's accumulation story hands the values back through an escape hatch instead of through the type.**

The full `Combine` family, verified from the source:

| Overload | Returns | Notes |
|---|---|---|
| `Combine(params Result[])` | `Result` | errors joined by the configured separator |
| `Combine<T>(params Result<T>[])` | `Result` | values discarded |
| `Combine(string separator, params Result[])` | `Result` | per-call separator |
| `Combine(IEnumerable<Result>, string separator = null)` | `Result` | the collection form |
| `Combine<E>(IEnumerable<UnitResult<E>>, Func<IEnumerable<E>, E> composerError)` | `UnitResult<E>` | **typed errors, folded by your function** |
| `Combine<E>(params UnitResult<E>[]) where E : ICombine` | `UnitResult<E>` | folding via the `ICombine` interface |
| `Combine<T, E>(IEnumerable<Result<T, E>>, Func<IEnumerable<E>, E>)` | `Result<bool, E>` | the `bool` is meaningless — the source says so in a comment |

That `Result<bool, E>` is not me being unkind; the library's own comment reads *"Ideally, we would be using BaseResult\<E\> or equivalent instead of Result\<bool, E\>… NB: The bool value type is arbitrary - the value is not intended to be used."* An acknowledged wart.

Two relatives:

- **`FirstFailureOrSuccess(params Result[])`** — returns the first failure rather than combining. Cheaper than `Combine` when you only want to know *that* something failed.
- **`CombineInOrder`** — exists **only** in `Task`/`ValueTask` flavors (there's no synchronous file for it). It preserves error ordering when combining results that completed out of order. If you go looking for a sync `CombineInOrder`, that's why you can't find it.

## 6.3 — `ICombine`, and why it's awkward  `[ ]`

For typed errors, the alternative to passing a `composerError` function every time is implementing `ICombine`. Here's the entire interface:

```csharp
public interface ICombine
{
    ICombine Combine(ICombine value);
}
```

Untyped in and untyped out. So an implementation has to cast:

```csharp
public sealed record ExpenseErrors(ImmutableArray<ExpenseError> Errors) : ICombine
{
    public ICombine Combine(ICombine value) =>
        new ExpenseErrors(Errors.AddRange(((ExpenseErrors)value).Errors));   // ← cast, unavoidable
}
```

That cast is the interface's whole problem: it's a runtime promise that the thing you're combining with is the same type as you, which the signature can't express. A generic `ICombine<T>` would have fixed it; changing it now would be a breaking change to a library with 36 million downloads. So it stays.

Practical guidance: **skip `ICombine`.** The `composerError` overload does the same job with no cast and no interface:

```csharp
UnitResult<ExpenseErrors> combined = Result.Combine(
    new[] { errA, errB, errC },
    errors => new ExpenseErrors(errors.SelectMany(e => e.Errors).ToImmutableArray()));
```

## 6.4 — Side by side with LanguageExt  `[ ]`

```csharp
// LanguageExt — applicative, accumulating, values come back through the type
Validation<Error, ExpenseEntry> entry =
    (ValidateDate(date), ValidateAmount(amount), ValidateCategory(cat))
        .Apply((d, a, c) => new ExpenseEntry(d, a, c, description));

// CFE — combine to check, then read the values out of the originals
Result check = Result.Combine(dateResult, amountResult, categoryResult);
ExpenseEntry? entry = check.IsSuccess
    ? new ExpenseEntry(dateResult.Value, amountResult.Value, categoryResult.Value, description)
    : null;
```

The difference isn't syntax preference. In the LanguageExt version the successful values **arrive as arguments** to the function that needs them, so there is no state in which you can read a value that isn't there. In the CFE version the values arrive by you going back and asking for them, guarded by a check the compiler doesn't connect to the read.

That's a real, structural difference in what the type system guarantees, and it's the strongest single argument for LanguageExt in this whole comparison. Worth saying plainly because everything else in these two articles has been "CFE is smaller and that's mostly good."

## 6.5 — What to actually do about it  `[ ]`

You have four options. I'd pick the fourth.

**Option 1 — accept short-circuiting.** For internal code, first-failure-wins is often genuinely fine. A malformed row in a batch import doesn't need three reasons; it needs to go in the reject pile with one. Don't pay for accumulation you don't need.

**Option 2 — `Combine` plus `.Value`, as documented.** Works, safe, and violates the `.Value` rule locally. If you go here, quarantine it: one small function per aggregate, with a comment explaining why `.Value` is safe.

**Option 3 — accumulate by hand.** Collect errors in a list, then lift once. Verbose, obvious, no cleverness — sometimes the right answer for a team that's new to all of this:

```csharp
public static Result<ExpenseEntry, ExpenseErrors> Parse(ExpenseRow row)
{
    var errors = ImmutableArray.CreateBuilder<ExpenseError>();

    var date     = ParseDate(row.Date);
    var amount   = ParseAmount(row.Amount);
    var category = ResolveCategory(row.Category);

    if (date.IsFailure)     errors.Add(date.Error);
    if (amount.IsFailure)   errors.Add(amount.Error);
    if (category.IsFailure) errors.Add(category.Error);

    return errors.Count > 0
        ? new ExpenseErrors(errors.ToImmutable())
        : new ExpenseEntry(date.Value, amount.Value, category.Value, row.Description);
}
```

**Option 4 — write the value-preserving combine you wish existed.** It's about fifteen lines per arity, it's the missing piece, and once written you never think about this again:

```csharp
public static class ResultCombineExtensions
{
    /// <summary>
    /// Combines three independent results, accumulating ALL errors, and hands the
    /// successful values to <paramref name="onSuccess"/> — so no .Value access is needed.
    /// </summary>
    public static Result<TOut, ExpenseErrors> Zip<T1, T2, T3, TOut>(
        Result<T1, ExpenseError> first,
        Result<T2, ExpenseError> second,
        Result<T3, ExpenseError> third,
        Func<T1, T2, T3, TOut> onSuccess)
    {
        var errors = ImmutableArray.CreateBuilder<ExpenseError>();

        if (first.IsFailure)  errors.Add(first.Error);
        if (second.IsFailure) errors.Add(second.Error);
        if (third.IsFailure)  errors.Add(third.Error);

        return errors.Count > 0
            ? new ExpenseErrors(errors.ToImmutable())
            : onSuccess(first.Value, second.Value, third.Value);
    }
}
```

Yes, it reads `.Value` — but *once*, inside a reviewed helper, where the guarantee is local and visible. Every call site gets the LanguageExt property: values arrive as parameters, and there's no state where you can read one that isn't there.

```csharp
Result<ExpenseEntry, ExpenseErrors> entry = ResultCombineExtensions.Zip(
    ParseDate(row.Date),
    ParseAmount(row.Amount),
    ResolveCategory(row.Category),
    (d, a, c) => new ExpenseEntry(d, a, c, row.Description));
```

That's the applicative, hand-rolled, for the arities you actually use. **This is the single most valuable thing in this article**, and it's the thing to have in your pocket when someone asks "but doesn't CFE fall down on validation?" The honest answer is: *it does, and here's the fifteen lines that fix it, and you should know that LanguageExt ships this.*

Stage 2 will make you write this for real. Don't skip ahead — writing it after feeling the pain lands differently.

## Type-along checklist — Part 6

- [ ] `Result.Combine` with three failures, default separator
- [ ] A custom separator
- [ ] The documented `Combine`-then-`.Value` pattern, and noticing what it costs
- [ ] `FirstFailureOrSuccess`
- [ ] `composerError` with a typed error
- [ ] An `ICombine` implementation, including the cast (then decide never to use it again)
- [ ] Option 3, by hand
- [ ] **Option 4 — the value-preserving `Zip`** ← the one that matters
- [ ] First `ADOPTION.md` entry on the accumulation gap

## Exercises — Part 6

**Predict-then-check.**

1. `Result.Combine(Result.Success(1), Result.Success(2))` — what's the return type, and can you get `1` and `2` out of it?
2. `Result.Combine()` with no arguments at all — success or failure?
3. `Result.Combine(a, b, c)` where all three *succeed* — do the error-message lambdas in your `Ensure` calls run? Does anything get joined?

**Explain-it-back.** Write the paragraph you'd say if someone on your team asked *"we picked CFE — how do we do form validation with multiple field errors?"* You have to give them a real answer, not a caveat. Include what you'd standardize on and why.

---

# Part 7 — Modeling the domain

Back to target #1 from Part 1: primitive obsession. This is the part of CFE with the most overlap with things C# has gained since 2015, so it's the part needing the most judgment.

## 7.1 — The `Create` factory pattern  `[ ]`

The core move — and this one is a *pattern*, not an API:

```csharp
namespace Expenses;

using CSharpFunctionalExtensions;

public sealed class CategoryName : SimpleValueObject<string>
{
    public const int MaxLength = 50;

    private CategoryName(string value) : base(value) { }

    public static Result<CategoryName> Create(string? input) =>
        Result.Success(input ?? string.Empty)
            .Ensure(v => !string.IsNullOrWhiteSpace(v), "Category name is required")
            .Map(v => v.Trim())
            .Ensure(v => v.Length <= MaxLength, $"Category name must be {MaxLength} characters or fewer")
            .Map(v => new CategoryName(v));
}
```

Three things are happening, and together they're the whole idea:

1. **The constructor is private.** The only way to get a `CategoryName` is through `Create`.
2. **`Create` returns `Result<CategoryName>`.** Validation failure is a value, not an exception.
3. **Therefore every `CategoryName` in the system is valid**, and no function that accepts one needs to check anything.

That's "make illegal states unrepresentable" in this library's idiom, and it's the direct answer to the repeated-`null`-and-`IsNullOrEmpty`-guards smell. Those guards don't disappear — they *concentrate*, into one `Create` per type, at the boundary. The interior gets to be dumb.

Note also that `Create` is itself a `Result` railway, so it reads as a validation pipeline rather than a wall of `if`-throws. This is the point where the two halves of the library finally reinforce each other rather than just coexisting.

## 7.2 — The `ValueObject` family  `[ ]`

Five base classes. Here's what each is for and which ones to actually use:

| Base class | Use when | How you implement it |
|---|---|---|
| **`ValueObject`** | multi-field value objects — **the default** | override `IEnumerable<object> GetEqualityComponents()` |
| **`SimpleValueObject<T>`** | a single wrapped value (`T : IComparable`) | pass it to `base(value)`; done |
| **`ComparableValueObject`** | you need ordering as well as equality | override `IEnumerable<IComparable> GetComparableEqualityComponents()` |
| **`EnumValueObject<…>`** | a closed set of named instances (a "smart enum") | static fields; see §7.4 |
| ~~`ValueObject<T>`~~ | **don't** | legacy; its own doc comment says *"Use non-generic ValueObject whenever possible"* |

The non-generic `ValueObject` is the one to reach for:

```csharp
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency) =>
        Result.SuccessIf(amount >= 0, amount, "Amount cannot be negative")
            .Ensure(_ => currency?.Length == 3, "Currency must be a 3-letter code")
            .Map(a => new Money(a, currency!.ToUpperInvariant()));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}
```

`GetEqualityComponents` gives you equality, `GetHashCode` (cached after the first call), and `==`/`!=` operators for free. And because *you* choose the components, you can deliberately exclude a field from equality — a surrogate key, a cached derived value, a timestamp. That's the main thing this buys over a `record`, and §7.5 weighs that properly.

`SimpleValueObject<T>` handles the single-value case and throws in a convenience:

```csharp
public static implicit operator T(SimpleValueObject<T> valueObject) =>
    valueObject == null ? default : valueObject.Value;
```

So a `CategoryName` is implicitly usable anywhere a `string` is wanted. Convenient — `Console.WriteLine(name)` and `dict[name]` just work. Also leaky: the wrapper you built to stop `string`s flowing around freely will silently decay into a `string` at any call site expecting one, and you'll never see it happen. My advice: use `SimpleValueObject<T>` and be aware that the implicit conversion is a one-way valve out of your own abstraction. If you want the wrapper airtight, derive from `ValueObject` and expose `Value` explicitly.

## 7.3 — `Entity<TId>`  `[ ]`

Value objects are equal when their *contents* match. Entities are equal when their *identities* match:

```csharp
public sealed class Expense : Entity<Guid>
{
    public Money Amount { get; private set; }
    public CategoryName Category { get; private set; }

    private Expense(Guid id, Money amount, CategoryName category) : base(id)
    {
        Amount = amount;
        Category = category;
    }
    // ... Create factory as above
}
```

Two behaviors in `Entity<TId>` that will surprise you if you don't know them, both read from the source:

**Transient entities are never equal — not even to themselves by value.** `IsTransient()` is `Id is null || Id.Equals(default(TId))`, and `Equals` returns `false` if either side is transient. So two freshly-constructed, unsaved entities with `default(Guid)` ids compare *unequal*, and an entity compared to itself is still `true` only because `ReferenceEquals` is checked first. This is correct for the ORM lifecycle it's designed around, and it will bite you in a test that constructs two entities without ids and expects them to match.

**`GetUnproxiedType` exists for ORMs.** Both `Entity` and `ValueObject` compare `GetUnproxiedType(this) != GetUnproxiedType(other)` rather than `GetType()`, so an NHibernate or EF Core lazy-loading proxy compares equal to the real type it proxies. If you've ever debugged an equality failure caused by `Castle.Proxies.ExpenseProxy != Expense`, this is the fix, pre-solved. It's also a genuine reason to prefer these base classes over records in an ORM-heavy codebase, and it's the kind of detail that reveals the library came out of real production work.

`Entity<TId>` also implements `IComparable`/`IComparable<Entity<TId>>` on the id, so entities sort.

## 7.4 — `EnumValueObject`: smart enums  `[ ]`

The most interesting and most booby-trapped type in the library. Two flavors: `EnumValueObject<TEnumeration>` (string id) and `EnumValueObject<TEnumeration, TId>` (id plus name).

```csharp
public sealed class ExpenseStatus : EnumValueObject<ExpenseStatus>
{
    public static readonly ExpenseStatus Draft    = new("draft");
    public static readonly ExpenseStatus Approved = new("approved");
    public static readonly ExpenseStatus Rejected = new("rejected");

    private ExpenseStatus(string id) : base(id) { }
}
```

What you get, all verified from the source:

```csharp
IReadOnlyCollection<ExpenseStatus> all = ExpenseStatus.All;          // 3
Maybe<ExpenseStatus> parsed = ExpenseStatus.FromId("approved");      // Some
Maybe<ExpenseStatus> bad    = ExpenseStatus.FromId("banana");        // None
bool exists = ExpenseStatus.Is("draft");                             // true
bool matches = ExpenseStatus.Draft == "draft";                       // true — operator ==(T, string)
string s = ExpenseStatus.Approved.ToString();                        // "approved"
```

`FromId` returning `Maybe<T>` is a nice touch — the library eating its own cooking, so parsing an unknown status is a `None` rather than an exception or a `null`.

**Now the traps, because the mechanism is reflection.** `All` is built by:

```csharp
enumerationType
    .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
    .Where(info => info.FieldType == typeof(TEnumeration))
```

Which means:

- **Fields only, not properties.** `public static readonly ExpenseStatus Draft = new("draft");` works. `public static ExpenseStatus Draft { get; } = new("draft");` produces an *empty* `All` and silently broken `FromId`. This is the one that will cost you an hour.
- **`DeclaredOnly`** — inherited members don't count. No hierarchies of smart enums.
- **The field type must match `TEnumeration` exactly.** Declare the field as a base type and it's invisible.
- **`All` is a public mutable static field**, not a property. Anyone can reassign it. Cosmetic, but it tells you the vintage.
- **Static initialization order matters.** `All` and the lookup dictionaries are static fields initialized by reflecting over the derived type's static fields. If you ever see an empty `All`, suspect initialization order or the field-vs-property mistake before you suspect your own code.

Compared with the alternatives in 2026: C# still has no discriminated unions, and `enum` still can't carry data or behavior. `EnumValueObject` is a decent implementation of the Ardalis-style smart enum, with the reflection caveats every such implementation has. If you want this pattern, it's here and it works. Just write a test that asserts `All.Count` equals the number of cases — cheap insurance against the property/field trap.

## 7.5 — The honest question: `record`s versus `ValueObject`  `[ ]`

This is the part where the library shows its age, and you should have a clear position on it before recommending anything.

`ValueObject` was written for C# 6, when giving a type structural equality meant hand-writing `Equals`, `GetHashCode`, and two operators, correctly, every time. C# 9 shipped records, which do that for free:

```csharp
public sealed record Money(decimal Amount, string Currency);   // equality, hashing, ToString, deconstruction
```

So: is `ValueObject` obsolete? **Mostly, but not entirely.** Here's the split as I read it:

**Use a `record` when** the type is a straightforward bag of values with equality over all of them. That's the common case, it's less code, it's idiomatic modern C#, and it gets you `with`-expressions and deconstruction that `ValueObject` doesn't. You can still have the private-constructor + `Create`-factory discipline:

```csharp
public sealed record CategoryName
{
    public string Value { get; }
    private CategoryName(string value) => Value = value;

    public static Result<CategoryName> Create(string? input) => /* as before */;
}
```

Note that this is a record *without* a primary constructor — using the positional form (`record CategoryName(string Value)`) would generate a public constructor and defeat the whole point.

**Use `ValueObject` when** you need something records don't give you:

- **Equality over a *subset* of members.** Records compare every field, full stop. `GetEqualityComponents` lets you exclude one. This comes up more than you'd think — caches, audit stamps, surrogate keys.
- **`GetUnproxiedType` behavior**, if you're behind an ORM that generates proxies. Records will compare a proxy unequal to its target; `ValueObject` won't.
- **Ordering via `ComparableValueObject`**, which is less code than implementing `IComparable` across several members by hand.
- **Consistency with an existing codebase** that already uses these base classes. Don't mix idioms for purity's sake.

**Use `EnumValueObject`** when you want a smart enum, since records don't address that at all.

And the punchline for your team pitch: *`Maybe`, `Result`, and the pipeline vocabulary are why you'd take this dependency. The DDD base classes are a bonus that C# has partly caught up with.* Saying that out loud makes the recommendation more credible, not less — it shows you evaluated the thing rather than adopting it.

## Type-along checklist — Part 7

- [ ] `CategoryName : SimpleValueObject<string>` with a private ctor and a `Create` railway
- [ ] `Money : ValueObject` with `GetEqualityComponents`
- [ ] Confirm equality and hashing work; confirm the implicit `string` conversion on `CategoryName`
- [ ] `Expense : Entity<Guid>`
- [ ] **Pin the transient-entity surprise**: two entities with `default(Guid)` ids are *not* equal
- [ ] `ExpenseStatus : EnumValueObject<ExpenseStatus>` with `All` / `FromId` / `Is`
- [ ] **Break it on purpose** — change a static field to a static property and watch `All` go empty
- [ ] A `record`-based `CategoryName` alongside the `ValueObject` one; compare the code you had to write

## Exercises — Part 7

**Predict-then-check.**

1. `CategoryName.Create("  Groceries  ")` — what's `.Value` on success? (Look at the order of `Ensure` and `Map` in the pipeline.)
2. `Money.Create(-1, "USD")` and `Money.Create(-1, "US")` — both are invalid in two ways. How many errors does each report, and which one?
3. Two `Expense` instances with the *same* non-default `Guid` but *different* amounts — equal or not? Now the same question for two `Money` values with the same amount and currency but constructed separately.

**Explain-it-back.** Your teammate says: *"We have records now. Why would I inherit from `ValueObject`?"* Give the four-sentence answer — and make sure one of those sentences concedes that they're usually right.

---

# Part 8 — Async, tooling, and the verdict

## 8.1 — How async actually works here  `[ ]`

Every combinator in the library has async overloads, and the naming convention is the key to the whole thing. There are **three static extension classes**, and knowing which is which turns a confusing IntelliSense list into an obvious choice:

| Static class | File suffix | Receiver | Function |
|---|---|---|---|
| `AsyncResultExtensionsLeftOperand` | `.Task.Left.cs` | `Task<Result<T>>` | **sync** — `Func<T, Result<K>>` |
| `AsyncResultExtensionsRightOperand` | `.Task.Right.cs` | `Result<T>` | **async** — `Func<T, Task<Result<K>>>` |
| `AsyncResultExtensionsBothOperands` | `.Task.cs` | `Task<Result<T>>` | **async** — `Func<T, Task<Result<K>>>` |

"Left" and "Right" mean *which operand is the asynchronous one* — left being the thing you're calling the method on, right being the function you're passing in. Once you see that, the async story collapses into one sentence: **whichever side is async, there's an overload for it, so you never have to await mid-chain.**

```csharp
public async Task<Result<ExpenseEntry>> ApproveAsync(string id) =>
    await repository.FindAsync(id)                                  // Task<Maybe<ExpenseEntry>>
        .ToResult($"No expense with id '{id}'")                     // stays in Task<Result<…>>
        .Ensure(e => e.Amount > 0, "Amount must be positive")       // sync predicate, async receiver → Left
        .Check(e => policy.AllowsApprovalAsync(e))                  // async function → Both
        .Tap(e => repository.MarkApprovedAsync(e))                  // async effect
        .TapError(error => log.Warning(error));                     // sync effect
```

One `await`, at the front, for the whole chain. Compare the `await`-per-step version and this is a genuine readability win — arguably a bigger one than the sync case, because `await` noise is exactly what obscures a pipeline.

Everything above has a `ValueTask` twin, in the same three shapes. That's the real explanation for the library's file count: 15 concepts × 4 types × 4 async shapes.

**`Result.Configuration.DefaultConfigureAwait`** controls the `ConfigureAwait` used inside those overloads, and it ships as `false` — i.e. the library doesn't capture the synchronization context, which is the right default for library code and for ASP.NET Core. It's a **mutable public static**, so it's process-global and last-writer-wins; if you're in a UI framework that needs context capture, that's where you'd change it, once, at startup. (Same caveat for `Result.Configuration.ErrorMessagesSeparator` and `DefaultTryErrorHandler`: global mutable state, so set it at startup and never from a test that runs in parallel with others.)

## 8.2 — Where query syntax finally wins  `[ ]`

Article A showed LINQ query syntax once and set it aside. Here's the exception:

```csharp
Result<BillingInfo> billing = await (
    from customer in _customerRepository.GetByIdAsync(id)          // Task<Result<Customer>>
    from charge in _paymentGateway.ChargeAsync(customer, amount)   // Task<Result<BillingInfo>>
    select charge);
```

*(Adapted from CFE's README.)* Every intermediate value stays in scope, there's a single `await`, and there's no nesting. The fluent equivalent needs `BindZip` or nested lambdas to keep `customer` visible to the second step.

So: fluent by default, and reach for query syntax when you need **several async results in scope at once**. That's a narrow, specific rule rather than a style preference, which is the kind of rule that survives contact with a team.

## 8.3 — JSON serialization  `[ ]`

There are System.Text.Json converters for `Result`, `Result<T>`, `Result<T, E>`, and `UnitResult<E>`, registered in one call:

```csharp
var options = new JsonSerializerOptions().AddCSharpFunctionalExtensionsConverters();
```

Three things to know:

- **`netstandard2.0` doesn't have it.** The csproj removes `Result\Json\Serialization\**` from that target, so the JSON support exists only on the `net6.0` and `net8.0` assets. Fine for you; relevant if you ever consume this from a `netstandard` library.
- **There is no `Maybe<T>` converter.** Only `Result`-family types. Serializing a `Maybe<T>` is on you — which is worth knowing before you put one in a DTO.
- **An oddity I couldn't fully explain.** `JsonSerializerOptionsExtensionMethods.cs` (in both `v3.7.0` and `master`) opens with `using C2i.Common.C2iCSharpFunctionalExtensions.FunctionalApiResult;` — a namespace that doesn't appear in any other file I checked in that folder, and which looks like a contributor's internal namespace that leaked in. I can't square that with the package building for `net8.0`, so I'm probably missing something; I'm flagging it rather than concluding anything. Treat the JSON corner as the least-polished part of the library and verify it works the way you expect before relying on it.

## 8.4 — The companion packages  `[ ]`

Three exist. I verified their versions on NuGet; I have **not** verified their APIs, so treat the usage sketches below as "roughly this shape, confirm when you get there."

| Package | Latest | Author | What it's for |
|---|---|---|---|
| `CSharpFunctionalExtensions.Analyzers` | **1.4.1**, released **2024-07-03** | AlmarAubel (third party) | Roslyn rules against unguarded `.Value` |
| `CSharpFunctionalExtensions.FluentAssertions` | 3.6.0 | NitroDevs (third party) | assertion helpers for `Result`/`Maybe` |
| `CSharpFunctionalExtensions.HttpResults` | 1.2.1 | third party | `Result` → ASP.NET Core `IResult` |

**The analyzer deserves real attention, and also real scrutiny.** Its whole job is closing the hole Article A identified: it flags reading `.Value` without first checking `IsSuccess`, and understands `if`, ternaries, switch expressions, and early-return-on-`IsFailure` as valid guards. That is *exactly* the guardrail a team needs, because CFE's floor is set by convention and conventions need tooling.

But be clear-eyed:

- It's **third-party**, not Khorikov's.
- Its latest release is **2024-07-03** — about two years stale as of now, against a library that shipped 3.6.0 and 3.7.0 since.
- Its README **documents no diagnostic IDs or severities**, which makes it awkward to configure per-project in `.editorconfig` — you'd have to discover the IDs from the build output.
- From its own description it targets **`Result`**; whether it covers `Maybe<T>.Value` (the case Article A worried about most) I could not confirm.

So the adoption question sharpens into something concrete: *if the enforcement mechanism for your central convention is a two-year-stale third-party analyzer with undocumented rule IDs, is a documented convention plus code review enough?* Try the package, see what it actually flags, and write down the answer. That's a genuinely load-bearing entry for `ADOPTION.md`, and it's the sort of question your team will respect you for having asked before proposing anything.

`HttpResults` is worth a look at Stage 3 specifically — mapping `Result` to `IResult` is the seam where the library pays off most visibly in a web app, and it's plausible the package does it better than the hand-rolled `switch` in Part 5. `FluentAssertions` matters only if your team already uses FluentAssertions; with plain xUnit you don't need it, since `Result` and `Maybe` both assert cleanly with `Assert.Equal` (no `IEnumerable` formatter problem — see Article A, §2.7).

## 8.5 — The verdict  `[ ]`

You've now seen all of it. Here's my summary; the point of writing yours is to disagree with mine where you do.

**What CFE is genuinely good at:**

- `Maybe<T>` and `Result<T>` cover 90% of what everyday application code needs from FP, at a fraction of the conceptual cost of a full FP library.
- The pipeline vocabulary (`Tap`, `Check`, `Ensure`, `Compensate`, `Finally`) is a real contribution and is *better* than what LanguageExt offers for imperative-shell code.
- Zero dependencies, one small assembly, readable in an afternoon. That's not a small thing for a team that has to maintain a choice for years.
- The `Create`-factory pattern gives you "illegal states unrepresentable" without asking anyone to learn a new vocabulary.
- Async composition is thoroughly done, and the Left/Right/Both structure is coherent once explained.

**What it's genuinely weak at:**

- **Error accumulation.** No applicative; `Combine` discards values and the documented workaround needs `.Value`. Fixable in fifteen lines, but you have to know to write them.
- **The `.Value` escape hatch**, whose only enforcement is a third-party analyzer of uncertain health.
- **No unbiased two-case type** (`Either`) and no discriminated unions, so error hierarchies are sealed-record conventions rather than closed types.
- **Typed-error ceremony** — `Result<T, E>` costs enough at the call site that you'll be tempted back to `string`s.
- **The DDD base classes have been partly overtaken** by records.
- **JSON support is the rough edge**, and `Maybe<T>` isn't covered at all.

**When I'd choose what:**

| Situation | Choose |
|---|---|
| Null-only problems, greenfield, no appetite for a dependency | nullable reference types alone |
| A team new to this, ordinary line-of-business code, wants clearer optional/failure handling | **CFE** |
| Validation-heavy domain where accumulating errors is the core job | LanguageExt (`Validation`) — or CFE plus the hand-rolled `Zip` |
| You want `Eff`/`IO`, effect tracking, HKT-generic code, persistent collections | LanguageExt |
| Existing codebase already committed to one of them | that one; don't mix |

Which lands on the same conclusion your global instructions already encode — *recommend the lightest adequate tool* — with the difference that you can now defend it from experience rather than from principle. That's the whole point of Stage 1.

## Type-along checklist — Part 8

- [ ] An async chain with one `await` at the front, mixing sync and async steps
- [ ] Deliberately find the Left/Right overload distinction in IntelliSense
- [ ] Query syntax over two async results
- [ ] Register the JSON converters and round-trip a `Result<T>`; then try a `Maybe<T>` and see what happens
- [ ] Install the Analyzers package, write an unguarded `.Value`, and record what it flags (and what it doesn't)
- [ ] Write your own §8.5 verdict before re-reading mine

## Exercises — Part 8

**Predict-then-check.**

1. `await someTaskOfResult.Tap(e => DoAsync(e))` where `DoAsync` returns `Task` — which of the three static classes supplies that overload?
2. Set `Result.Configuration.ErrorMessagesSeparator = " | "` in one test and run the whole suite in parallel. What could go wrong?
3. Serialize `Maybe.From("x")` with `AddCSharpFunctionalExtensionsConverters()` — what do you get?

**Explain-it-back — the big one.** Write the five-minute version of "what is CSharpFunctionalExtensions and should we use it," as if you had a whiteboard and a skeptical senior colleague. Constraints: no jargon, one code example, one honest weakness volunteered *before* they find it. This is the Teach step, rehearsed. If the opportunity ever shows up, you'll have already done it once.

---

# Where you are, and what's next

You've now seen the whole library. Concretely, you can:

- Model absence with `Maybe<T>` and failure with the whole `Result` family, string-error or typed.
- Compose either into pipelines with `Map`/`Bind`/`Ensure`/`Check`/`Tap` and land them with `Match`/`Finally`.
- Accumulate errors — including knowing that you'll write the value-preserving helper yourself.
- Build validated domain types with `Create` factories, and know when a `record` is the better choice.
- Compose async chains without `await`-per-step.
- State, with reasons, where this library is the right call and where it isn't.

**Next: Stage 2 — DO.** The CSV ingest → report CLI, fresh domain, strict TDD, low guidance. Everything above was watching someone else do it; Stage 2 is where you find out which parts you actually absorbed. My prediction, for the record so you can check it later: the thing that bites you won't be `Maybe` or `Bind` — it'll be **deciding where the boundary is** between the validated interior and the messy exterior, and how much of the row-level error accumulation belongs in the parser versus the report.

**Before you start, finish the `ADOPTION.md` entries.** The open-questions list in that file was written before you'd seen any of this; go answer the ones you now have an opinion about. Specifically the analyzer question (§8.4) and the accumulation question (§6.5) — those two will drive everything else.

**Optional application challenge** (~30–45 minutes, fresh domain, skip freely): a **URL shortener**. `CategoryName`-style value objects for the slug and the target URL (both with real validation rules), `Result<T, E>` with a typed error at the API boundary, a `Maybe<Redirect>` lookup, and a create operation that must accumulate *all* validation failures on the way in — because a user submitting a bad slug *and* a bad URL should hear both. You'll end up writing the Part 6 `Zip` helper. That's the point.

---

## Notes & questions

_Yours._

-
