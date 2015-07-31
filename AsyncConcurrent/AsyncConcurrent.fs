namespace CodeSuperior

open System
open System.Threading

module AsyncConcurrent = 

    let private getCaptureResultsAsList<'a> () =

        let results     = ref list<int * 'a>.Empty
        let lockObj     = new Object()

        let captureResultsAsList idx result =  
                lock 
                    lockObj
                    (fun () -> results := (idx, result) :: !results)
            
        let getResult () = results

        captureResultsAsList, getResult 

    // task runner
    let RunImpl<'a, 'b>
            (captureResult                  :   int     ->  'a  -> unit)
            (getResult                      :   unit    ->  'b) 
            maxConcurrent    
            (tasks                          :   Async<'a> seq)
                = 

        let mutable taskCount = 0

        let rec waitTillCanCreate () = 
            if taskCount >= maxConcurrent then    
                //Async.Sleep 50                     |> Async.RunSynchronously
                waitTillCanCreate ()
            
        // wait until all completed
        let rec wait () = 
            if taskCount <> 0 then
                Async.Sleep 500 |> Async.RunSynchronously
                wait () 

        // wrap task                                            
        let taskWrapper idx (task : Async<'a>) =
         
            Interlocked.Increment(ref taskCount)    |> ignore            

            async {
                let! result = task 

                captureResult idx result

                Interlocked.Decrement(ref taskCount) |> ignore
            }

        // schedule
        tasks
        |>  Seq.mapi (fun idx task ->
                            waitTillCanCreate () 
                            taskWrapper idx task)
        |>  Seq.iter (fun t -> Async.Start t)                                
                    
        wait () 
    
        getResult ()

    // task runner
    let RunToList<'a> 
            maxConcurrent    
            (tasks                          :   Async<'a> seq)
                = 

        let capture, getResult = getCaptureResultsAsList ()

        RunImpl 
            capture
            getResult
            maxConcurrent
            tasks

