open System
open CodeSuperior.AsyncConcurrent



[<EntryPoint>]
let main argv = 

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
        
    let maxThreads = 10
    let taskCount = 1000

    // -----------------------------------------------------------------------------------------------------------------
    
    let resultList = 
        RunToList
            maxThreads
            (createTasks taskCount)

    printfn ""   
    printfn "%A" resultList

    // -----------------------------------------------------------------------------------------------------------------
    
    let resultArray = 
        RunToArray
            maxThreads
            (createTasks taskCount)
    printfn ""   
    printfn "%A" resultArray
    
    // -----------------------------------------------------------------------------------------------------------------
    
    printfn "Press any key to exit ..."
    Console.ReadKey() |> ignore

    0 // return an integer exit code
