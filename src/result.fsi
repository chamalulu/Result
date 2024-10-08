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
val inline map2:
    [<InlineIfLambda>] mapping: ('a -> 'b -> 'c) ->
    result1: Result<'a,'e list> ->
    result2: Result<'b,'e list> ->
        Result<'c,'e list>

/// <summary>Convert option to result with given error value if none.</summary>
/// <param name="error">Error value</param>
/// <param name="option">Source option</param>
/// <typeparam name="'a">Type of option value</typeparam>
/// <typeparam name="'e">Error type</typeparam>
/// <returns>Ok if option is Some, otherwise Error error</returns>
val inline ofOption: error: 'e -> option: 'a option -> Result<'a,'e>

/// <summary>Map errors of result.</summary>
/// <param name="mapping">Map function 'e -> 'f</param>
/// <param name="result">Source result</param>
/// <typeparam name="'a">Ok type</typeparam>
/// <typeparam name="'e">Error type of source</typeparam>
/// <typeparam name="'f">Error type of target</typeparam>
/// <returns>Error with mapped errors if result is Error, otherwise Ok</returns>
val inline mapErrors:
    [<InlineIfLambda>] mapping: ('e -> 'f) ->
    result: Result<'a,'e list> ->
        Result<'a,'f list>

/// <summary>Produce first error of result if Error.</summary>
/// <param name="result">Source result</param>
/// <typeparam name="'a">Ok type of source</typeparam>
/// <typeparam name="'e">Error type of source</typeparam>
/// <returns>Error with first error if result is Error, otherwise Ok</returns>
val inline firstError: result: Result<'a,'e list> -> Result<'a,'e>

/// <summary>Map result</summary>
/// <param name="mapping">Map function 'a -> 'b</param>
/// <param name="result">Source result</param>
/// <typeparam name="'a">Ok type of source result</typeparam>
/// <typeparam name="'b">Ok type of target result</typeparam>
/// <typeparam name="'e">Error type</typeparam>
/// <returns>Ok with mapped value if source is Ok, otherwise Error</returns>
val inline (<!>) :
    [<InlineIfLambda>] mapping: ('a -> 'b) ->
    result: Result<'a,'e> ->
        Result<'b,'e>

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
val inline (<*>) :
    application: Result<('a -> 'b),'e list> ->
    result: Result<'a,'e list> ->
        Result<'b,'e list>

/// <summary>Bind result</summary>
/// <param name="result">Source result</param>
/// <param name="binder">Bind function</param>
/// <typeparam name="'a">Ok type of source result</typeparam>
/// <typeparam name="'e">Ok type of target result</typeparam>
/// <typeparam name="'b">Error type</typeparam>
/// <returns>Result of binder applied to source value if source is Ok, otherwise source Error</returns>
val inline (>>=) :
    result: Result<'a,'e> ->
    [<InlineIfLambda>] binder: ('a -> Result<'b,'e>) ->
        Result<'b,'e>

/// <summary>Traverse source sequence with result-yielding function inverting effects of list and result.</summary>
/// <param name="traverser">Traversing function returning result</param>
/// <param name="source">Sequence of values to traverse</param>
/// <typeparam name="'a">Type of source values</typeparam>
/// <typeparam name="'b">Ok type of result values</typeparam>
/// <typeparam name="'e">Error type of result</typeparam>
/// <returns>
/// Ok list of values returned from traverser if all are Ok, otherwise Error list of concatenated errors.
/// </returns>
val inline traverse:
    [<InlineIfLambda>] traverser: ('a -> Result<'b,'e list>) ->
    source: 'a seq ->
        Result<'b list,'e list>

/// <summary>
/// Traverse sequence with result-yielding function inverting effects of
/// sequence (list really) and result but short-circuiting at first error.
/// </summary>
/// <param name="traverser">Traversing function returning result</param>
/// <param name="source">Source sequence of values to traverse</param>
/// <typeparam name="'a">Type of source values</typeparam>
/// <typeparam name="'b">Ok type of result values</typeparam>
/// <typeparam name="'e">Error type of result</typeparam>
/// <returns>
/// Ok list of values returned from traverser if all are Ok, otherwise first
/// Error encountered while applying traverser.
/// </returns>
val inline traverseFirstError:
    [<InlineIfLambda>] traverser: ('a -> Result<'b,'e>) ->
    source: 'a seq ->
        Result<'b list,'e>

/// <summary>Traverse source sequence of results with identity function inverting effects of list and result.</summary>
/// <param name="source">Source sequence of results</param>
/// <typeparam name="'a">Ok type</typeparam>
/// <typeparam name="'b">Error type</typeparam>
/// <returns>Ok list of result values from source sequence if all are Ok, otherwise Error list of concatenated errors.</returns>
val inline sequence: source: Result<'a,'e list> seq -> Result<'a list,'e list>

/// <summary>Wrap an exception-throwing function to a result-returning function.</summary>
/// <param name="f">Exception-throwing function 'a -> 'b</param>
/// <param name="emap">Map of exception to Error type</param>
/// <typeparam name="'a">Parameter type of f</typeparam>
/// <typeparam name="'b">Return type of f and Ok type of result</typeparam>
/// <typeparam name="'e">Error type of result</typeparam>
/// <returns>Function returning Error with mapped exception if f throws, otherwise returning Ok with return value of f</returns>
val inline try':
    f: ('a -> 'b) ->
    [<InlineIfLambda>] emap: (exn -> 'e) ->
        ('a -> Result<'b,'e>)

/// <summary>Computation expression builder for Result functor</summary>
type FunctorBuilder =
    
    new: unit -> FunctorBuilder
    
    /// <summary>Support `let! ... return`</summary>
    member inline BindReturn:
        result: Result<'a,'e> *
        mapping: ('a -> 'b) ->
            Result<'b,'e>
    
    /// <summary>Support `return`</summary>
    member inline Return: x: 'a -> Result<'a,'e>

/// <summary>Computation expression builder instance for Result functor</summary>
val resultF: FunctorBuilder

/// <summary>Computation expression builder for Result applicative functor</summary>
type ApplicativeBuilder =
    inherit FunctorBuilder
    
    new: unit -> ApplicativeBuilder
    
    /// <summary>Support `let! ... (and! ... )+ return`</summary>
    member inline MergeSources:
        result1: Result<'a,'e list> *
        result2: Result<'b,'e list> ->
            Result<struct ('a * 'b),'e list>

/// <summary>Computation expression builder instance for Result applicative functor</summary>
val resultA: ApplicativeBuilder

/// <summary>Computation expression builder for Result monad</summary>
type MonadBuilder =
    inherit ApplicativeBuilder
    
    new: unit -> MonadBuilder
    
    /// <summary>Support `let! ... (let! ... )+ return`</summary>
    member inline Bind:
        result: Result<'a,'e> *
        binder: ('a -> Result<'b,'e>) ->
            Result<'b,'e>
    
    /// <summary>Support `return!`</summary>
    member inline ReturnFrom: result: 'a -> 'a

/// <summary>Computation expression builder instance for Result monad</summary>
val resultM: MonadBuilder
