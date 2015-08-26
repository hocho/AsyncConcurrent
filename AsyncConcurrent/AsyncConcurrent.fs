namespace CodeSuperior

open System
open System.Threading

module AsyncConcurrent = 

    // task runner
    let RunSynchronously<'a, 'b>
            maxConcurrent    
            taskCreateWaitMs
            (captureResult                  :   int     ->  'a  -> unit)
            (getResult                      :   unit    ->  'b) 
            (tasks                          :   Async<'a> seq)
                = 

        let mutable taskCount = 0

        let waitTillCanCreate () = 
            while taskCount >= maxConcurrent do
                if taskCreateWaitMs > 0 then
                    Async.Sleep taskCreateWaitMs |> Async.RunSynchronously
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
        |>  Seq.mapi 
                (fun idx task ->
                    waitTillCanCreate () 
                    taskWrapper idx task)
        |>  Seq.iter 
                (fun t -> Async.Start t)                                
        |>  wait 
    
        getResult ()

    let Run<'a, 'b>
            maxConcurrent    
            taskCreateWaitMs    
            (captureResult                  :   int     ->  'a  -> unit)
            (tasks                          :   Async<'a> seq)
                = 

        let mutable taskCount = 0

        let waitTillCanCreate () = 
            while taskCount >= maxConcurrent do
                if taskCreateWaitMs > 0 then
                    Async.Sleep taskCreateWaitMs |> Async.RunSynchronously
                ()
            
        // wrap task                                            
        let taskWrapper 
                idx 
                (task : Async<'a>) 
                (cancellationToken          :   CancellationToken) 
                    = 
         
            Interlocked.Increment(&taskCount)    |> ignore            

            async {
                if not cancellationToken.IsCancellationRequested then
                
                    let! result = task 

                    captureResult idx result

                Interlocked.Decrement(&taskCount) |> ignore
            }

        let cancellationTokenSource = new CancellationTokenSource()
        let token = cancellationTokenSource.Token

        // return 
        // async block & isComplete function
        async {
            tasks
            |>  Seq.mapi 
                    (fun idx task ->
                        waitTillCanCreate () 
                        taskWrapper idx task token)
            |>  Seq.takeWhile 
                    (fun _ -> not token.IsCancellationRequested)                                    
            |>  Seq.iter 
                    (fun t -> Async.Start (t, token))       
            
            // final decrement will bring the count to -1, to signal completion      
            Interlocked.Decrement(&taskCount) |> ignore
        }
        ,
        (fun () -> taskCount = -1)
        ,
        cancellationTokenSource                                    
                                            
                                            

