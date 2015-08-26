open System

[<EntryPoint>]
let main argv = 
    
    printfn "See unit tests for usage."
    printfn "Press any key to exit ..."
    Console.ReadKey() |> ignore

    0 // return an integer exit code
