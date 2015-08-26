module AsyncConcurrentTest

// https://github.com/fsharp/FsCheck/blob/master/Docs/Documentation.md
// https://github.com/fsharp/FsUnit
// https://code.google.com/p/unquote/

open System
open CodeSuperior

open FsUnit
open FsCheck
open NUnit.Framework
open Swensen.Unquote

open NUnitRunner

// ---------------------------------------------------------------------------------------------------------------------
// helper functions

let rnd = new Random()

let getRnd () = 
            lock 
                rnd
                rnd.Next

let createTask () =
     
    async {
        return getRnd ()
    }

let createTasks count = 
        (fun _ -> createTask ()) 
        |>  Seq.initInfinite 
        |>  Seq.take count
        
let maxConcurrent    = 10
let taskCount        = 1000
let taskCreateWaitMs = 50
// ---------------------------------------------------------------------------------------------------------------------
// tests

[<Test>]
let ``Task results to List synchronously, list count is number of tasks`` () =
    
        let mutable results = list<int * int>.Empty     // index * random int
        let lockObj         = new Object()

        let response = 
            (createTasks taskCount)
            |>
            AsyncConcurrent.RunSynchronously
                maxConcurrent
                taskCreateWaitMs
                (fun idx result -> 
                    lock 
                        lockObj
                        (fun () -> results <- (idx, result) :: results))
                (fun () -> results)

        Assert.AreEqual (response.Length, taskCount)
                

[<Test>]
let ``Task results to Array synchronously, no array elements has default value`` () =
    
        let defaultValue = -1
        let results      = Array.create taskCount defaultValue
        let lockObj      = new Object()

        let response = 
            (createTasks taskCount)
            |>
            AsyncConcurrent.RunSynchronously
                maxConcurrent
                taskCreateWaitMs
                (fun idx result -> 
                    lock 
                        lockObj
                        (fun () -> results.[idx] <- result))
                (fun () -> results)

        let hasDefaultValue = 
            response
            |>  Seq.exists (fun v -> v = defaultValue)
            
        Assert.IsFalse hasDefaultValue
                
[<Test>]
let ``Task results to List manual, list count is number of tasks`` () =
    
        let mutable results = list<int * int>.Empty     // index * random int
        let lockObj         = new Object()

        let task, isComplete, cancellationToken = 
            (createTasks taskCount)
            |>
            AsyncConcurrent.Run
                maxConcurrent
                taskCreateWaitMs
                (fun idx result -> 
                    lock 
                        lockObj
                        (fun () -> results <- (idx, result) :: results))

        task
        |> Async.RunSynchronously

        Assert.AreEqual (results.Length, taskCount)

[<Test>]
let ``Task results to List manual with wait, list count is number of tasks`` () =
    
        let mutable results = list<int * int>.Empty     // index * random int
        let lockObj         = new Object()

        let task, isComplete, cancellationToken = 
            (createTasks taskCount)
            |>
            AsyncConcurrent.Run
                maxConcurrent
                taskCreateWaitMs
                (fun idx result -> 
                    lock 
                        lockObj
                        (fun () -> results <- (idx, result) :: results))

        task
        |> Async.Start

        let wait () = 
            while not <| isComplete () do
                Async.Sleep 50 |> Async.RunSynchronously
                ()
        wait ()

        Assert.AreEqual (results.Length, taskCount)


[<Test>]
let ``Task results to List manual with cancel, list count is not number of tasks`` () =
    
        let mutable results = list<int * int>.Empty     // index * random int
        let lockObj         = new Object()

        let task, isComplete, cancellationToken = 
            (createTasks taskCount)
            |>
            AsyncConcurrent.Run
                maxConcurrent
                taskCreateWaitMs
                (fun idx result -> 
                    lock 
                        lockObj
                        (fun () -> results <- (idx, result) :: results))

        task
        |> Async.Start

        Async.Sleep 30 |> Async.RunSynchronously

        cancellationToken.Cancel ()

        Assert.AreNotEqual (results.Length, taskCount)


