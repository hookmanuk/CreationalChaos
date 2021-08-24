using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganismRed : Organism
{
    public override void Started()
    {
        Type = OrganismType.Red;
        TargetType = OrganismType.Blue;
        ChasedByType = OrganismType.Green;
    }

    public override void Move()
    {        
        MoveInDirection(CalcDirection());

        base.Move();
    }
}
