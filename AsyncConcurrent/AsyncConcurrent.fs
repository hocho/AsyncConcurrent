namespace CodeSuperior

open System
open System.Threading

module AsyncConcurrent = 

    // task runner
    let RunImpl<'a, 'b>
            (captureResult                  :   int     ->  'a  -> unit)
            (getResult                      :   unit    ->  'b) 
            maxConcurrent    
            (tasks                          :   Async<'a> seq)
                = 

        let mutable taskCount = 0

        let waitTillCanCreate () = 
            while taskCount >= maxConcurrent do
                //Async.Sleep 50                     |> Async.RunSynchronously
                ()
            
        // wait until all completed
        let wait () = 
            while taskCount <> 0 do
                Async.Sleep 500 |> Async.RunSynchronously

        // wrap task                                            
        let taskWrapper idx (task : Async<'a>) =
         
            Interlocked.Increment(&taskCount)    |> ignore            

            async {
                let! result = task 

                captureResult idx result

                Interlocked.Decrement(&taskCount) |> ignore
            }

        // schedule
        tasks
        |>  Seq.mapi (fun idx task ->
                            waitTillCanCreate () 
                            taskWrapper idx task)
        |>  Seq.iter (fun t -> Async.Start t)                                
        |>  wait 
    
        getResult ()

    // task runner
    let RunToList<'a>
            maxConcurrent    
            (tasks                          :   Async<'a> seq)
            = 

        let mutable results = list<int * 'a>.Empty
        let lockObj         = new Object()

        RunImpl
            (fun idx result -> 
                lock 
                    lockObj
                    (fun () -> results <- (idx, result) :: results))
            (fun () -> results)
            maxConcurrent
            tasks

    let RunToArray<'a> 
            maxConcurrent    
            (tasks                          :   Async<'a> seq)
            =
             
        RunToList<'a>
            maxConcurrent
            tasks
        |> List.sortBy  fst 
        |> List.map     snd
        |> List.toArray
