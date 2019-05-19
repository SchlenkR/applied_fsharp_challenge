
#load "5_Evaluation.fsx"
open ``5_Evaluation``
open ``4_Optional_Initial_Values``

#load "10_Modulation_with_map_and_apply.fsx"
open ``10_Modulation_with_map_and_apply``

block {
    let! added = block {
        let! count1 = counter 0.0 1.0
        let! count2 = counter 0.0 2.0
        let! result = toggleAB count1 count2
        return result
    }

    let! whatever = counter 0.0 added
    return whatever
}
