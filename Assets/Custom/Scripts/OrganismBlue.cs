using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrganismBlue : Organism
{
    public override void Started()
    {
        Type = OrganismType.Blue;
        TargetType = OrganismType.Green;
        ChasedByType = OrganismType.Red;
    }

    public override void Move()
    {
        MoveInDirection(CalcDirection());

        base.Move();
    }
}
