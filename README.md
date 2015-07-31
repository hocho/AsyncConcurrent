# AsyncConcurrent

Allows specifying the maximum number to concurrent threads used to process a number of async tasks.
Wraps the tasks and increments / decrements a counter to ensure that only a max number of tasks are being executed at a given moment in time.
