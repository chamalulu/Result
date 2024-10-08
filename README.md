# chamalulu.Result

This repository contains the module `chamalulu.ResultModule` which provides
functions, operators and computation expression builders for the result type
`Microsoft.FSharp.Core.FSharpResult`.

## Functions

```fs
val inline map2:
    mapping: ('a -> 'b -> 'c) ->
    result1: Result<'a,'e list> ->
    result2: Result<'b,'e list> ->
        Result<'c,'e list>
```

Map two results into one, preserving any errors.

`map2` constrains the error type (`'e list` above) parameter to be a list. The
reason is to preserve errors from both arguments and is used by the applicative
Computation Expression builder. I.e. `let! ... and! ...` expressions.

---

```fs
val inline ofOption:
    error: 'e ->
    option: 'a option ->
        Result<'a,'e>
```

Convert option to result with given error value if `None`.

---

```fs
val inline mapErrors:
    mapping: ('e -> 'f) ->
    result: Result<'a,'e list> ->
        Result<'a,'f list>
```

Map errors of result.

Simple convenience function equivalent to
`Result.mapError (List.map mapping) result`.

---

```fs
val inline firstError:
    result: Result<'a,'e list> ->
        Result<'a,'e>
```

Produce first error of result if `Error`.

Simple convenience function equivalent to `Result.mapError List.head result`.

---

```fs
val inline traverse:
    traverser: ('a -> Result<'b,'e list>) ->
    source: 'a seq ->
        Result<'b list,'e list>
```

Traverse source sequence with result-yielding function inverting effects of list
and result. Produces ok list of values returned from `traverser` if all are
`Ok`, otherwise error list of concatenated errors.

---

```fs
val inline traverseFirstError:
    traverser: ('a -> Result<'b,'e>) ->
    source: 'a seq ->
        Result<'b list,'e>
```

Traverse source sequence with result-yielding function inverting effects of
sequence and result but short-circuiting at first error. Produces ok list of
values returned from `traverser` if all are `Ok`, otherwise first error
encountered while applying `traverser`. The source sequence is only evaluated as
long as `traverser` results are `Ok`.

---

```fs
val inline sequence:
    source: Result<'a,'e list> seq ->
        Result<'a list,'e list>
```

Traverse source sequence of results inverting effects of list and result.
Produces ok list of values from `source` if all are `Ok`, otherwise error list
of concatenated errors. `sequence source` is equivalent to `traverse id source`.

---

```fs
val inline try':
    f: ('a -> 'b) ->
    emap: (exn -> 'e) ->
        ('a -> Result<'b,'e>)
```

Wrap an exception-throwing function to a result-returning function using `emap`
to map an exception to error if `f` throws.

## Operators

```fs
val inline (<!>) :
    mapping: ('a -> 'b) ->
    result: Result<'a,'e> ->
        Result<'b,'e>
```

Map result. `mapping <!> result` is equivalent to `Result.map mapping result`.

---

```fs
val inline (<*>) :
    application: Result<('a -> 'b),'e list> ->
    result: Result<'a,'e list> ->
        Result<'b,'e list>
```

Applies lifted function to given result. If either application or result are
errors, they are concatenated. `application <*> result` is equivalent to
`map2 (<|) application result`.

---

```fs
val inline (>>=) :
    result: Result<'a,'e> ->
    binder: ('a -> Result<'b,'e>) ->
        Result<'b,'e>
```

Bind result. `result >>= binder` is equivalent to `Result.bind binder result`.

## Computation Expression builders

`FunctorBuilder` (with instance provided by `resultF`) implements members
`BindReturn` and `Return` thus supporting expressions with at most one `let!`
binding.

---

`ApplicativeBuilder` (with instance provided by `resultA`) inherits
`FunctorBuilder` and additionally implements member `MergeSources` thus
supporting expressions with a single `let!` binding followed by one or more
`and!` bindings.  
Using `and!` bindings constrains the error type of `Result` to
be a list since this implementation of applicative binding concatenates
subsequent errors.

---

`MonadBuilder` (with instance provided by `resultM`) inherits
`ApplicativeBuilder` and additionally implements members `Bind` and `ReturnFrom`
thus supporting expressions with more than one `let!` binding.
