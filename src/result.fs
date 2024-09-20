[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module org.chamalulu.Result

// map2 for Result constrains the error type parameter to be a list. The reason
// is to preserve errors from both sources and is used by the Applicative
// Computation Expression builder. I.e. let! ... and! ... expressions.
let inline map2 ([<InlineIfLambda>] mapping) result1 result2 =
    match result1, result2 with
    | Ok value1, Ok value2 -> mapping value1 value2 |> Ok
    | Error errors1, Ok _ -> Error errors1
    | Ok _, Error errors2 -> Error errors2
    | Error errors1, Error errors2 -> Error (errors1 @ errors2)

let inline ofOption error option =
    match option with
    | Some x -> Ok x
    | None -> Error error

let inline mapErrors ([<InlineIfLambda>] mapping) result = Result.mapError (List.map mapping) result

let inline firstError result = Result.mapError List.head result

let inline (<!>) f r = Result.map f r

let inline (<*>) rf r = map2 (<|) rf r

let inline (>>=) r f = Result.bind f r

let inline traverse ([<InlineIfLambda>] traverser) source =
    let inline cons x xs = x::xs
    let inline folder x result = map2 cons (traverser x) result

    Seq.foldBack folder source (Ok [])

let inline sequence source = traverse id source

let inline try' f ([<InlineIfLambda>] emap) p =
    try
        f p |> Ok
    with
        | e -> emap e |> Error

type FunctorBuilder() =

    // support 'return'
    member inline this.Return(x) = Ok x

    // support 'let! ... return'
    member inline this.BindReturn(r, f) = Result.map f r

let resultF = FunctorBuilder()

type ApplicativeBuilder() =
    inherit FunctorBuilder()

    // support 'let! ... (and! ... )+ return' together with FunctorBuilder.BindReturn
    member inline this.MergeSources(r1, r2) = map2 (fun x1 x2 -> struct(x1, x2)) r1 r2

let resultA = ApplicativeBuilder()

type MonadBuilder() =
    inherit ApplicativeBuilder()

    // support 'let! ... (let! ... )+'
    member inline this.Bind(r, f) = Result.bind f r

    // support 'return!'
    member inline this.ReturnFrom(r) = r

let resultM = MonadBuilder()

[<System.ObsoleteAttribute>]
let traverseListFirstError traverser list =

    let rec inner result list =
        match list with
        | [] -> Ok result
        | x::xs ->
            match traverser x with
            | Ok r -> inner (r::result) xs
            | Error e -> Error e
    
    inner [] list
    |> Result.map List.rev

[<System.ObsoleteAttribute>]
let traverseSeqFirstError traverser (source : seq<_>) =

    let rec inner result (etor : System.Collections.Generic.IEnumerator<_>) =
        if etor.MoveNext() then
            match traverser etor.Current with
            | Ok r -> inner (r::result) etor
            | Error e -> Error e
        else
            Ok result

    use etor = source.GetEnumerator()
    inner [] etor
    |> Result.map List.rev
