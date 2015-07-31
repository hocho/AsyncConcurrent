open System
open CodeSuperior.AsyncConcurrent



[<EntryPoint>]
let main argv = 

    let rnd = new Random()

    let createTask () =
     
        async {
           let r () = 
                lock 
                    rnd
                    rnd.Next
           
           let num = r ()        

           return num
        }

    let result = 
        RunToList
            10
            (   (fun _ -> createTask ()) 
                |>  Seq.initInfinite 
                |>  Seq.take 1000)

    printfn "%A" result
    
    0 // return an integer exit code
