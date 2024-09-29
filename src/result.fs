[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module chamalulu.Result

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

let inline (<!>) ([<InlineIfLambda>] mapping: 'a -> 'b) (result: Result<'a,'e>) : Result<'b,'e> = Result.map mapping result

let inline (<*>) application result = map2 (<|) application result

let inline (>>=) result ([<InlineIfLambda>] binder) = Result.bind binder result

let inline traverse ([<InlineIfLambda>] traverser) source =
    let inline cons x xs = x::xs
    let inline folder x result = map2 cons (traverser x) result
    Ok [] |> Seq.foldBack folder source

let inline sequence source = traverse id source

let inline try' f ([<InlineIfLambda>] emap) p =
    try
        f p |> Ok
    with
        | e -> emap e |> Error

type FunctorBuilder() =

    member inline _.Return(x) = Ok x

    member inline _.BindReturn(result, mapping) = Result.map mapping result

let resultF = FunctorBuilder()

type ApplicativeBuilder() =
    inherit FunctorBuilder()

    member inline _.MergeSources (result1, result2) = map2 (fun x1 x2 -> struct(x1, x2)) result1 result2

let resultA = ApplicativeBuilder()

type MonadBuilder() =
    inherit ApplicativeBuilder()

    member inline _.Bind(result, binder) = Result.bind binder result

    member inline _.ReturnFrom(result) = result

let resultM = MonadBuilder()

[<System.Obsolete>]
let traverseListFirstError traverser list =

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
let traverseSeqFirstError traverser (source: 'a seq) =

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
