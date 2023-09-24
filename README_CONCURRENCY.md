# Overview

This document is to describe our understanding of sync/async as it relates to the library.

Starting with version 2.5.0 there was an effort to port the whole library over to async/await.

Since our USB driver library does not include async functions _and_ because we have a lot of properties
(in lieu of methods), which are not advised to be made async, and which interact with USB in both their
getters and setters, and finally to maintain sync functions without forcing developers to port,
there is _a lot_ of sync-to-async and async-to-sync "magic" going on.

This link provides a view of some of the things we're trying to do:
https://cpratt.co/async-tips-tricks/

On the other hand, this is what we are mostly trying to avoid:
https://markheath.net/post/async-antipatterns

From the first link, the following is something we have found to be extremely important:
"There's whole lot of terminology that gets confused with async in C#. You hear that sync code blocks the thread,
while async code does not. That's actually not true. Whether or not a thread is blocked, really has nothing at 
all to do with whether is sync or async. The only reason it comes into the discussion at all is that async 
is at least better than sync if your goal is to not block a thread, because sometimes, in some scenarios, 
it maybe might just run on a different thread. If you have any synchronous code in your async method (anything 
that's not awaiting something else) that code will always run sync. Further, an async operation may run sync if 
what it awaits on has already completed. Lastly, async doesn't ensure that the work won't actually be done on 
the same thread. It just opens up the possibility for a thread-switch."

The first major bug we found with async was related to this. close() would call laserEnabled = false. laserEnabled
includes a call to the async USB sendCMD. This sendCMD would get launched, and then what happens next is a bit of 
a mystery at this point. _Either_ that sendCMD Task would float without finishing, the set laserEnabled would return,
and close() would move forward, ultimately closing our USB interface before sendCMD was able to use it and keeping
laser on, _Or_ control would be given back to close() before the set laserEnabled finished, which would also result
in the same thing (USB closed before sendCMD could fire).

We tried a number of solutions to this. We were able to fix it a couple of different ways. First, calling .Wait()
on the sendCMD task from within laserEnabled set. The other solution was to wrap laserEnabled = false as a
task in itself, and await that task. That _both_ of these solutions work is exceptionally confusing. One would think
that the Wait() fixing the problem would indicate that the setter is returning too early, while the await solution
working would seem to indicate that close() is continuing without the setter completing unless we force it to. Maybe
await Task.Run _also_ causes the program to wait for the internal sendCMD task to complete? See, confusing!
Using just task.Result from within the laserEnabled set does not fix the issue. Revising the laserEnabled set to 
use the non-async sendCMD also fixes the issue. Finally using the old syncronous close() function does _not_ fix
the issue, the USB still seems to get closed first. This would seem to indicate it really is the setter returning
early. This does not explain why the await wrapper works. 

The upshot of this is that, as of writing, we cannot assume that our property setters/getters that use USB will 
execute in order. This means that developers will need to wrap and await these calls if they want to be absolutely
certain that their execution order is as intended, or alternatively to use thread sleeps between calls. 

Alternatively, we could make all of these functions synchronous by moving to the sync CMD calls, or by calling
Wait() on all the tasks that currently just "float." Another option would be a total paradigm shift in how we
deal with USB calls in general (incorporating an internal queue perhaps?). We could also try turning the USB
parameters into async parameters. My understanding is that using that much Wait() or using async parameters
are ideas that are not encouraged by the .NET community. But maybe in our case, we are justified in "blocking"
otherwise async code. We could also turn our parameters into functions to make them natively async, but this 
would break compatibility for devs. Perhaps we could add those async functions and turn our getters and
setters into wrappers for our functions?

There are a lot of potential options, none perfect. But this needs to be fixed before wider release

-TS, 10/28/2022