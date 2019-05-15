
#load "5_Evaluation.fsx"
open ``5_Evaluation``
open ``4_Optional_Initial_Values``
open ``4_Optional_Initial_Values``.Blocks


type Fbd<'fbdValue, 'value> = { feedback: 'fbdValue; out: 'value }

let (<->) seed (f: 'fbdValue -> Block<Fbd<'fbdValue,'value>,'state>) =
    Block <| fun prev ->
        let myPrev,innerPrev = 
            match prev with
            | None            -> seed,None
            | Some (my,inner) -> my,inner
        let fRes = f myPrev
        let lRes = (runB fRes) innerPrev
        let feed = lRes.value
        let innerState = lRes.state
        { value = feed.out; state = feed.feedback,Some innerState }


module ``UseCase 1: Two counter alternatives`` =

    // some simple blocks
    let counter (seed: float) (increment: float) =
        Block <| fun maybeState ->
            let state = match maybeState with | None -> seed | Some v -> v
            let res = state + increment
            {value=res; state=res}

    // we can rewrite 'counter' by using feedback:
    let counterAlt (seed: float) (increment: float) =
        seed <-> fun state ->
            block {
                let res = state + increment
                return { out=res; feedback=res }
            }

    // evaluate
    let evaluate f =
        let evaluateWithValues = f |> createEvaluatorWithValues
        evaluateWithValues (Seq.init 10 id) |> Seq.toList

    let evaluatedCounter = (fun i -> counter 0.0 1.5) |> evaluate
    // evaluates to: [1.5; 3.0; 4.5; 6.0; 7.5; 9.0; 10.5; 12.0; 13.5; 15.0]

    let evaluatedCounterAlt = (fun i -> counterAlt 0.0 1.5) |> evaluate
    // evaluates to: [1.5; 3.0; 4.5; 6.0; 7.5; 9.0; 10.5; 12.0; 13.5; 15.0]

module ``UseCase 2: State in 'block' syntax`` =

    let myFx input =
        block {
            // I would like to feed back the amped value
            // and access it in the next cycly
            // - but how?
            let amped = amp 0.5 input (* - (lastAmped * 0.1) *)
            let! lp = lowPass 0.2 amped
            return lp
        }

    let myFxWithFeedback input =
        // initial value for lastAmped is: 0.0
        0.0 <-> fun lastAmped ->
            block {
                let amped = amp 0.5 input - (lastAmped * 0.1)
                let! lp = lowPass 0.2 amped
                // we emit our actual value (lp), and the feedback value (amped)
                return { out=lp; feedback=amped }
            }
