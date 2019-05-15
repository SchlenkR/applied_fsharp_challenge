
#load "4_Optional_Initial_Values.fsx"

open ``4_Optional_Initial_Values``
open ``4_Optional_Initial_Values``.ComputationExpressionSyntax

type Fbd<'fbdValue, 'value> = { feedback: 'fbdValue; out: 'value }

let (<=>) seed (f: 'fbdValue -> Block<Fbd<'fbdValue,'value>,'state>) =
    fun prev ->
        let myPrev,innerPrev = 
            match prev with
            | None            -> seed,None
            | Some (my,inner) -> my,inner
        let lRes = (f myPrev) innerPrev
        let feed = lRes.value
        let innerState = lRes.state
        { value = feed.out; state = feed.feedback,Some innerState }

