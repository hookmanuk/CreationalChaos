using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganismGreen : Organism
{
    public override void Started()
    {
        Type = OrganismType.Green;
        TargetType = OrganismType.Red;
        ChasedByType = OrganismType.Blue;
    }

    public override void Move()
    {
        MoveInDirection(CalcDirection());

        base.Move();
    }
}
