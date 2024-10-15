module Tests

open Expecto

module Configuration =
    open Expecto.Logging
    open Expecto.Logging.Message

    let logger = Log.create "ResultTests"

    let config = {
        FsCheckConfig.defaultConfig with
            receivedArgs = fun _ name no args ->
                logger.debugWithBP (
                    eventX "For {test} {no}, generated {args}"
                    >> setField "test" name
                    >> setField "no" no
                    >> setField "args" args)
        }

open chamalulu.Result

[<Tests>]
let tests =
    testList "Result tests" [
        testProperty "map2 f (Ok a) (Ok b) -> Ok (f a b)" <|
            fun f a b -> map2 f (Ok a) (Ok b) = Ok (f a b)

        testProperty "map2 f (Ok a) (Error es) -> Error es" <|
            fun f a es -> map2 f (Ok a) (Error es) = Error es

        testProperty "map2 f (Error es) (Ok b) -> Error es" <|
            fun f es b -> map2 f (Error es) (Ok b) = Error es

        testProperty "map2 f (Error es1) (Error es2) -> Error (es1 @ es2)" <|
            fun f es1 es2 -> map2 f (Error es1) (Error es2) = Error (es1 @ es2)

        testProperty "ofOption e (Some a) -> Ok a" <|
            fun e a -> ofOption e (Some a) = Ok a
        
        testProperty "ofOption e None -> Error e" <|
            fun e -> ofOption e None = Error e
        
        testProperty "mapErrors f (Ok a) -> Ok a" <|
            fun f a -> mapErrors f (Ok a) = Ok a
        
        testProperty "mapErrors f (Error es) -> Error (List.map f es)" <|
            fun f es -> mapErrors f (Error es) = Error (List.map f es)
        
        testProperty "firstError (Ok a) -> Ok a" <|
            fun a -> firstError (Ok a) = Ok a
        
        testProperty "firstError (Error (e::es)) -> Error e" <|
            fun e es -> firstError (Error (e::es)) = Error e
        
        testProperty "toErrors (Ok a) -> Ok a" <|
            fun a -> toErrors (Ok a) = Ok a
        
        testProperty "toErrors (Error e) -> Error [e]" <|
            fun e -> toErrors (Error e) = Error [e]
        
        testCase "firstError (Error []) -> throws ArgumentException" <| fun _ ->
            Expect.throwsT<System.ArgumentException>
                (fun () -> firstError (Error []) |> ignore)
                "Should throw ArgumentException"
        
        testProperty "Functor identity" <| fun r -> id <!> r = r
        
        testProperty "Functor composition" <| fun f g r -> ((f << g) <!> r) = ((((<!>) f) << ((<!>) g)) r)
        
        testProperty "Applicative identity" <| fun r -> (Ok id <*> r) = (r)
        
        testProperty "Applicative composition" <| fun u v r -> (Ok (<<) <*> u <*> v <*> r) = (u <*> (v <*> r))

        ptestProperty "Applicative composition (explicit)" <| fun ue ve r ->
            let f x = x = 'Y'
            let g x = if x then 'Y' else 'N'
            let u = if ue then Error ["No U"] else Ok f
            let v = if ve then Error ["No V"] else Ok g
            (Ok (<<) <*> u <*> v <*> r) = (u <*> (v <*> r))

        testProperty "Applicative homomorphism" <| fun f x -> (Ok f <*> Ok x) = (Ok (f x))

        testProperty "Applicative interchange" <| fun u r -> (u <*> (Ok r)) = (Ok ((|>) r) <*> u)

        testProperty "Monad left identity" <| fun f x -> Ok x >>= f = f x

        testProperty "Monad right identity" <| fun r -> r >>= Ok = r

        testProperty "Monad associativity" <| fun f g r -> (r >>= (fun x -> f x >>= g)) = (r >>= f >>= g)

        testProperty "Traversing sequence of zero or more ok results gives ok list of results in order." <|
            fun xs -> traverse Ok xs = Ok xs
        
        testProperty "Traversing to first error of zero or more ok results gives ok list of results in order." <|
            fun xs -> traverseFirstError Ok xs = Ok xs
        
        testProperty "Traversing to first error of one or more error results gives first error." <|
            fun e es -> traverseFirstError Error (e::es) = Error e
        
        testProperty "Traversing to first error of mixed results gives first error." <|
            fun x xs e es seed ->
                let rng = new System.Random(seed)

                let source =
                    [
                        yield Ok x
                        yield Error e
                        yield! xs |> List.map Ok
                        yield! es |> List.map Error
                    ]
                    |> List.randomShuffleWith rng
                
                let expected =
                    source
                    |> List.find Result.isError
                    |> Result.map List.singleton
                
                let result = traverseFirstError id source

                result = expected

        testProperty "Sequencing sequence of zero or more ok results gives ok list of results in order." <|
            fun xs -> sequence (List.map Ok xs) = Ok xs
        
        // Testing the following two properties of `sequence` apply to `traverse id` and is good enough
        testProperty "Sequencing sequence of one or more error results gives error with collected errors in order." <|
            fun es ess ->
                let source = (es::ess) |> List.map Error

                let expected =
                    source
                    |> List.collect (function Error es -> es | Ok _ -> [])
                    |> Error

                let result = sequence source

                result = expected
        
        testProperty "Sequencing sequence of mixed results gives error with collected errors in order." <|
            fun x xs es ess seed ->
                let rng = new System.Random(seed)

                let source =
                    [
                        yield Ok x
                        yield Error es
                        yield! xs |> List.map Ok
                        yield! ess |> List.map Error
                    ]
                    |> List.randomShuffleWith rng

                let expected =
                    source
                    |> List.collect (function Error es' -> es' | Ok _ -> [])
                    |> Error

                let result = sequence source

                result = expected
        
        testCase "try' with non-throwing function gives Ok" <| fun _ ->
            let f () = ()
            let actual = try' f _.Message ()
            let expected = Ok ()
            Expect.equal actual expected "Should give Ok"
        
        testCase "try' with throwing function gives Error" <| fun _ ->
            let f () =
                raise (new System.Exception("Banan"))
                ()
            
            let actual = try' f _.Message ()
            let expected = Error "Banan"
            Expect.equal actual expected "Should give Error"
        
        testTheory "Map in Result functor CE" [
            Ok 1, Ok 2
            Error "Banan", Error "Banan"
        ] <| fun (r, expected) ->
            let ce = resultF {
                let! x = r
                return x*2
            }
            Expect.equal ce expected "Should be equal"
        
        testTheory "Apply in Result applicative CE" [
            Ok (+), Ok 2, Ok 3, Ok 5
            Ok (+), Ok 2, Error ["Banan"], Error ["Banan"]
            Ok (+), Error ["Banan"], Ok 3, Error ["Banan"]
            Ok (+), Error ["Apelsin"], Error ["Banan"], Error ["Apelsin";"Banan"]
            Error ["Banan"], Ok 2, Ok 3, Error ["Banan"]
            Error ["Apelsin"], Ok 2, Error ["Banan"], Error ["Apelsin";"Banan"]
            Error ["Apelsin"], Error ["Banan"], Ok 3, Error ["Apelsin";"Banan"]
            Error ["Apelsin"], Error ["Banan"], Error ["Citron"], Error ["Apelsin";"Banan";"Citron"]
        ] <| fun (rop, r1, r2, expected) ->
            let ce = resultA {
                let! op = rop
                and! t1 = r1
                and! t2 = r2
                return op t1 t2
            }
            Expect.equal ce expected "Should be equal"
        
        testTheory "Bind in Result monad CE" [
            "10", Ok 10
            "0", Error "Attempted to divide by zero."
            "banan", Error "The input string 'banan' was not in a correct format."
        ] <| fun (s, expected) ->
            let ce = resultM {
                let! n = s |> try' int _.Message
                let! q = n |> try' ((/) 100) _.Message
                return q
            }
            Expect.equal ce expected "Should be equal"
    ]
