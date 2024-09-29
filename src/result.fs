[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module chamalulu.Result

/// <summary>Map two results into one, preserving any errors.</summary>
/// <param name="mapping">Map function 'a -> 'b -> 'c</param>
/// <param name="result1">First source result</param>
/// <param name="result2">Second source result</param>
/// <typeparam name="'a">Ok type of first source result</typeparam>
/// <typeparam name="'b">Ok type of second source result</typeparam>
/// <typeparam name="'c">Ok type of result</typeparam>
/// <typeparam name="'d">Error type</typeparam>
/// <returns>Mapped result from given two results</returns>
/// <remarks>
/// map2 for Result constrains the error type parameter to be a list. The reason
/// is to preserve errors from both sources and is used by the Applicative
/// Computation Expression builder. I.e. let! ... and! ... expressions.
/// </remarks>
let inline map2
    ([<InlineIfLambda>] mapping: 'a -> 'b -> 'c)
    (result1: Result<'a,'e list>)
    (result2: Result<'b,'e list>)
    : Result<'c,'e list> =
    match result1, result2 with
    | Ok value1, Ok value2 -> mapping value1 value2 |> Ok
    | Error errors1, Ok _ -> Error errors1
    | Ok _, Error errors2 -> Error errors2
    | Error errors1, Error errors2 -> Error (errors1 @ errors2)

/// <summary>Convert option to result with given error value if none.</summary>
/// <param name="error">Error value</param>
/// <param name="option">Source option</param>
/// <typeparam name="'a">Type of option value</typeparam>
/// <typeparam name="'e">Error type</typeparam>
/// <returns>Ok if option is Some, otherwise Error error</returns>
let inline ofOption (error: 'e) (option: 'a option) : Result<'a,'e> =
    match option with
    | Some x -> Ok x
    | None -> Error error

/// <summary>Map errors of result.</summary>
/// <param name="mapping">Map function 'e -> 'f</param>
/// <param name="result">Source result</param>
/// <typeparam name="'a">Ok type</typeparam>
/// <typeparam name="'e">Error type of source</typeparam>
/// <typeparam name="'f">Error type of target</typeparam>
/// <returns>Error with mapped errors if result is Error, otherwise Ok</returns>
let inline mapErrors
    ([<InlineIfLambda>] mapping: 'e -> 'f)
    (result: Result<'a,'e list>)
    : Result<'a,'f list> =
    Result.mapError (List.map mapping) result

/// <summary>Produce first error of result if Error.</summary>
/// <param name="result">Source result</param>
/// <typeparam name="'a">Ok type of source</typeparam>
/// <typeparam name="'e">Error type of source</typeparam>
/// <returns>Error with first error if result is Error, otherwise Ok</returns>
let inline firstError (result: Result<'a,'e list>) : Result<'a,'e> = Result.mapError List.head result

/// <summary>Map result</summary>
/// <param name="mapping">Map function 'a -> 'b</param>
/// <param name="result">Source result</param>
/// <typeparam name="'a">Ok type of source result</typeparam>
/// <typeparam name="'b">Ok type of target result</typeparam>
/// <typeparam name="'e">Error type</typeparam>
/// <returns>Ok with mapped value if source is Ok, otherwise Error</returns>
let inline (<!>) (mapping: 'a -> 'b) (result: Result<'a,'e>) : Result<'b,'e> = Result.map mapping result

/// <summary>Apply result</summary>
/// <param name="application">Lifted function to apply to result</param>
/// <param name="result">Source result</param>
/// <typeparam name="'a">Ok type of source result</typeparam>
/// <typeparam name="'b">Ok type of target result</typeparam>
/// <typeparam name="'e">Error type</typeparam>
/// <returns>
/// Ok result of application applied to source result value if application and source is Ok,
/// otherwise Error of (concatenated) errors
/// </returns>
let inline (<*>)
    (application: Result<'a -> 'b,'e list>)
    (result: Result<'a,'e list>)
    : Result<'b,'e list> =
    map2 (<|) application result

/// <summary>Bind result</summary>
/// <param name="result">Source result</param>
/// <param name="binder">Bind function</param>
/// <typeparam name="'a">Ok type of source result</typeparam>
/// <typeparam name="'e">Ok type of target result</typeparam>
/// <typeparam name="'b">Error type</typeparam>
/// <returns>Result of binder applied to source value if source is Ok, otherwise source Error</returns>
let inline (>>=) (result: Result<'a,'e>) (binder: 'a -> Result<'b,'e>) = Result.bind binder result

/// <summary>Traverse source sequence with result-yielding function inverting effects of list and result.</summary>
/// <param name="traverser">Traversing function returning result</param>
/// <param name="source">Source sequence of values</param>
/// <typeparam name="'a">Type of source values</typeparam>
/// <typeparam name="'b">Ok type of result values</typeparam>
/// <typeparam name="'e">Error type of result</typeparam>
/// <returns>
/// Ok list of values returned from traverser if all are Ok, otherwise Error list of concatenated errors.
/// </returns>
let inline traverse
    ([<InlineIfLambda>] traverser: 'a -> Result<'b,'e list>)
    (source: 'a seq)
    : Result<'b list,'e list> =
    let inline cons x xs = x::xs
    let inline folder x result = map2 cons (traverser x) result
    Ok [] |> Seq.foldBack folder source

/// <summary>Traverse source sequence of results with identity function inverting effects of list and result.</summary>
/// <param name="source">Source sequence of results</param>
/// <typeparam name="'a">Ok type</typeparam>
/// <typeparam name="'b">Error type</typeparam>
/// <returns>Ok list of result values from source sequence if all are Ok, otherwise Error list of concatenated errors.</returns>
let inline sequence (source: Result<'a,'b list> seq) : Result<'a list,'b list> = traverse id source

/// <summary>Wrap an exception-throwing function to a result-returning function.</summary>
/// <param name="f">Exception-throwing function 'a -> 'b</param>
/// <param name="emap">Map of exception to Error type</param>
/// <param name="p">Parameter of f</param>
/// <typeparam name="'a">Parameter type of f</typeparam>
/// <typeparam name="'b">Return type of f and Ok type of result</typeparam>
/// <typeparam name="'e">Error type of result</typeparam>
/// <returns>Error with mapped exception if f throws, otherwise Ok with return value of f</returns>
let inline try' (f: 'a -> 'b) ([<InlineIfLambda>] emap: exn -> 'e) (p: 'a) : Result<'b,'e> =
    try
        f p |> Ok
    with
        | e -> emap e |> Error

/// <summary>Computation expression builder for Result functor</summary>
type FunctorBuilder() =

    /// <summary>Support `return`</summary>
    member inline _.Return(x: 'a) : Result<'a,'e> = Ok x

    /// <summary>Support `let! ... return`</summary>
    member inline _.BindReturn(result: Result<'a,'e>, mapping: 'a -> 'b) = Result.map mapping result

/// <summary>Computation expression builder instance for Result functor</summary>
let resultF = FunctorBuilder()

/// <summary>Computation expression builder for Result applicative functor</summary>
type ApplicativeBuilder() =
    inherit FunctorBuilder()

    /// <summary>Support `let! ... (and! ... )+ return`</summary>
    member inline _.MergeSources
        (result1: Result<'a,'e list>, result2: Result<'b,'e list>)
        : Result<struct ('a * 'b),'e list> =
        map2 (fun x1 x2 -> struct(x1, x2)) result1 result2

/// <summary>Computation expression builder instance for Result applicative functor</summary>
let resultA = ApplicativeBuilder()

/// <summary>Computation expression builder for Result monad</summary>
type MonadBuilder() =
    inherit ApplicativeBuilder()

    /// <summary>Support `let! ... (let! ... )+ return`</summary>
    member inline _.Bind(result: Result<'a,'e>, binder: 'a -> Result<'b,'e>) : Result<'b,'e> = Result.bind binder result

    /// <summary>Support `return!`</summary>
    member inline _.ReturnFrom(result: 'a) : 'a = result

/// <summary>Computation expression builder instance for Result monad</summary>
let resultM = MonadBuilder()

[<System.Obsolete>]
let traverseListFirstError (traverser: 'a -> Result<'b,'e>) (list: 'a list) : Result<'b list,'e> =

    let rec inner result rest =
        match rest with
        | [] -> Ok result
        | x::xs ->
            match traverser x with
            | Ok r -> inner (r::result) xs
            | Error e -> Error e
    
    inner [] list
    |> Result.map List.rev

[<System.Obsolete>]
let traverseSeqFirstError (traverser: 'a -> Result<'b,'e>) (source: 'a seq) : Result<'b list,'e> =

    use etor = source.GetEnumerator()

    let rec inner result =
        if etor.MoveNext() then
            match traverser etor.Current with
            | Ok r -> inner (r::result)
            | Error e -> Error e
        else
            Ok result

    inner []
    |> Result.map List.rev
