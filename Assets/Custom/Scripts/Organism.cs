using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Organism : MonoBehaviour
{
    const float SLOWSPEED = 0.3f;
    const float FASTSPEED = 0.9f;
    public OrganismType Type { get; set; }
    public OrganismType TargetType { get; set; }
    public OrganismType ChasedByType { get; set; }
    public float Speed = 0.005f;
    public Organism Source;
    public bool IsMenuItem;
    private Vector3 _screenpoint = Vector3.zero;
    protected Vector3 _direction;
    protected Vector3 _rotation;
    private bool _isGlowing = false;
    public Organism LastMated { get; set; }
    public DateTime LastMatedTime { get; set; } = DateTime.MinValue;
    private float _secsSinceLastCalc = 99;
    public Material Material { get; set; }
    public Collider[] CloseByOrganisms { get; set; }

    private bool _initDone = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!_initDone)
        {
            Init();
        }        
    }

    public void Init()
    {
        Material = GetComponent<MeshRenderer>().material;

        Material.SetFloat("_RandomRotation1", UnityEngine.Random.Range(-1f, 1f) * (IsMenuItem ? 1f : 3f));
        Material.SetFloat("_RandomRotation2", UnityEngine.Random.Range(-1f, 1f) * (IsMenuItem ? 1f : 3f));
        Material.SetFloat("_TimeOffset", UnityEngine.Random.Range(-1f, 1f));

        if (IsMenuItem)
        {
            CreationManager.Instance.MenuItemOrganisms.Add(this);
        }
        else
        {
            CreationManager.Instance.Organisms.Add(this);
        }

        SetRandomDirection();
        _rotation = new Vector3(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f));
        Started();

        _initDone = true;
    }

    private void SetRandomDirection()
    {
        _direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0);
    }

    // Update is called once per frame
    void Update()
    {        
        Move();
    }

    public void SetActiveOrganism()
    {

    }

    public virtual void Move()
    {
        //transform.Rotate(_rotation * Time.deltaTime * (IsMenuItem ? 0.1f : 0.5f));

        if (!IsMenuItem && !CreationManager.Instance.IsSetup)
        {            
            if (transform.position.x < CreationManager.SCREENXMIN)
            {
                transform.position = new Vector3(CreationManager.SCREENXMAX, transform.position.y, transform.position.z);
            }
            if (transform.position.x > CreationManager.SCREENXMAX)
            {
                transform.position = new Vector3(CreationManager.SCREENXMIN, transform.position.y, transform.position.z);
            }
            if (transform.position.y < CreationManager.SCREENYMIN)
            {
                transform.position = new Vector3(transform.position.x, CreationManager.SCREENYMAX, transform.position.z);
            }
            if (transform.position.y > CreationManager.SCREENYMAX)
            {
                transform.position = new Vector3(transform.position.x, CreationManager.SCREENYMIN, transform.position.z);
            }
        }       
    }    

    public Vector3 CalcDirectionFromGrid()
    {        
        _secsSinceLastCalc += Time.deltaTime;

        if (_secsSinceLastCalc > 0.15f)
        {
            _secsSinceLastCalc = 0;
            if (CreationManager.Instance.IsPlaying && !CreationManager.Instance.IsSetup)
            {
                float distance = 0;
                List<Tuple<Organism, float>> closeOrganisms = new List<Tuple<Organism, float>>();

                if (!IsMenuItem)
                {
                    Speed = SLOWSPEED;

                    if ((DateTime.Now - this.LastMatedTime).TotalSeconds > 1)
                    {
                        //int layer = 0;

                        //switch (TargetType)
                        //{
                        //    case OrganismType.Red:
                        //        layer = 1 << 10;
                        //        break;
                        //    case OrganismType.Green:
                        //        layer = 1 << 11;
                        //        break;
                        //    case OrganismType.Blue:
                        //        layer = 1 << 12;
                        //        break;
                        //    default:
                        //        break;
                        //}
                        //Collider[] colliders = new Collider[0];

                        //float distanceCheck = 0.1f;
                        //while (colliders.Count() == 0 && distanceCheck <= 1.6f)
                        //{
                        //    colliders = Physics.OverlapSphere(transform.position, distanceCheck, layer); //sq root of 3
                        //    distanceCheck = distanceCheck * 2;
                        //}

                        //var orderedByProximity = colliders
                        //    //.Where(c => c.GetComponent<Organism>()?.Type == TargetType)
                        //    .OrderBy(c => (transform.position - c.transform.position).sqrMagnitude).ToArray();

                        List<Organism> orderedByProximity = new List<Organism>();
                        int? x = GetIndexX();
                        int? y = GetIndexY();

                        if (x.HasValue && y.HasValue)
                        {

                            switch (Type)
                            {
                                case OrganismType.Red:
                                    orderedByProximity = CreationManager.Instance.BlueGrid[x.Value, y.Value].CloseOrganisms;                                    
                                    break;
                                case OrganismType.Green:
                                    orderedByProximity = CreationManager.Instance.RedGrid[x.Value, y.Value].CloseOrganisms;
                                    break;
                                case OrganismType.Blue:
                                    orderedByProximity = CreationManager.Instance.GreenGrid[x.Value, y.Value].CloseOrganisms;
                                    break;
                                default:
                                    break;
                            }
                            orderedByProximity = orderedByProximity
                                                       .OrderBy(c => (transform.position - c.transform.position).sqrMagnitude).ToList();

                            if (orderedByProximity.Count() > 0)
                            {
                                Organism org = orderedByProximity[0].GetComponent<Organism>();

                                distance = System.Math.Abs((transform.position - org.transform.position).sqrMagnitude);

                                if (distance < 0.01f && !CreationManager.Instance.IsPerformanceTest)
                                {
                                    LastMatedTime = DateTime.Now;
                                    //org.LastMated = this;
                                    //org.LastMatedTime = DateTime.Now;

                                    StartCoroutine(Mated());

                                    //spawn 2 children
                                    for (int j = 0; j < CreationManager.CLONECOUNT; j++)
                                    {
                                        CreationManager.Instance.MakeOrganism(this.transform.position, this);
                                    }

                                    //kill target
                                    org.DestroyMe();

                                }
                                else
                                {
                                    _direction = org.transform.position - transform.position;
                                    Speed = FASTSPEED;

                                }
                            }
                        }
                    }                
                }
            }

            //hack to reset glow
            if (!_isGlowing)
            {
                Vector4 vector = new Vector4(1f, 1f, 1f, 1f);
                if (Material.GetVector("_Glow") != vector)
                {
                    Material.SetVector("_Glow", vector);
                }
            }
        }

        return _direction;
    }

    public Vector3 CalcDirection()
    {
        //Speed = SLOWSPEED;
        //return _direction;

        Organism closeOrg = null;
        _secsSinceLastCalc += Time.deltaTime;
        
        if (_secsSinceLastCalc > 0.15f)
        {
            _secsSinceLastCalc = 0;
            if (CreationManager.Instance.IsPlaying && !CreationManager.Instance.IsSetup)
            {
                float distance = 0;
                List<Tuple<Organism, float>> closeOrganisms = new List<Tuple<Organism, float>>();

                if (!IsMenuItem)
                {
                    Speed = SLOWSPEED;

                    //old code to get close orgs
                    //for (int i = 0; i < CreationManager.Instance.Organisms.Count; i++)
                    //{
                    //    Organism org = CreationManager.Instance.Organisms[i];

                    //    //check for target type close by, if they haven't mated last together, they can mate
                    //    if (org != this && (org.Type == TargetType)
                    //                    // && this.LastMated != org
                    //                    //&& org.LastMated != this
                    //                    && (DateTime.Now - this.LastMatedTime).TotalSeconds > 1
                    //                    //&& (DateTime.Now - org.LastMatedTime).TotalSeconds > 1
                    //                    )
                    //    {
                    //        distance = System.Math.Abs((transform.position - org.transform.position).sqrMagnitude);
                    //        if (distance < 3)
                    //        {
                    //            if (distance < 0.01f)
                    //            {
                    //                //LastMated = org;
                    //                LastMatedTime = DateTime.Now;
                    //                //org.LastMated = this;
                    //                //org.LastMatedTime = DateTime.Now;

                    //                StartCoroutine(Mated());

                    //                //spawn 2 children
                    //                for (int j = 0; j < CreationManager.CLONECOUNT; j++)
                    //                {
                    //                    CreationManager.Instance.MakeOrganism(this.transform.position, this);                                        
                    //                }

                    //                //kill target
                    //                org.DestroyMe();
                    //                break;
                    //            }
                    //            else
                    //            {
                    //                closeOrganisms.Add(new Tuple<Organism, float>(org, distance));
                    //                if (distance < 0.1f)
                    //                {
                    //                    closeOrg = org;
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    if ((DateTime.Now - this.LastMatedTime).TotalSeconds > 1)
                    {
                        //check to see if your closest org is already set by another organism, if so just use that
                        if ((CloseByOrganisms?.Count() ?? 0) > 0)
                        {
                            CloseByOrganisms = CloseByOrganisms?.Where(c => c.gameObject.activeSelf).ToArray();
                        }
                        if ((CloseByOrganisms?.Count() ?? 0) == 0)
                        {                            
                            int targetLayer = 0;
                            int sourceLayer = 0;

                            switch (TargetType)
                            {
                                case OrganismType.Red:
                                    targetLayer = 1 << 10;
                                    sourceLayer = 1 << 11;
                                    break;
                                case OrganismType.Green:
                                    targetLayer = 1 << 11;
                                    sourceLayer = 1 << 12;
                                    break;
                                case OrganismType.Blue:
                                    targetLayer = 1 << 12;
                                    sourceLayer = 1 << 10;
                                    break;
                                default:
                                    break;
                            }
                            Collider[] colliders = new Collider[0];                            

                            float distanceCheck = 0.1f;
                            //while (colliders.Count() == 0 && distanceCheck <= 1.6f)
                            //{
                            //    colliders = Physics.OverlapSphere(transform.position, distanceCheck, targetLayer); //sq root of 3

                            //    distanceCheck = distanceCheck * 2;
                            //}
                            colliders = Physics.OverlapSphere(transform.position, 0.1f, targetLayer); //sq root of 3
                            if (colliders.Count() == 0)
                            {
                                colliders = Physics.OverlapSphere(transform.position, 1.7f, targetLayer); //sq root of 3
                            }

                            if (colliders.Count() > 0)
                            {
                                CloseByOrganisms = colliders;

                                ////set closet org to any other orgs that are also close by here
                                var closeSameTypes = Physics.OverlapSphere(transform.position, 0.1f, sourceLayer);
                                for (int i = 0; i < closeSameTypes.Count(); i++)
                                {
                                    closeSameTypes[i].GetComponent<Organism>().CloseByOrganisms = CloseByOrganisms;
                                }
                            }
                        }                        

                        var orderedByProximity = CloseByOrganisms?
                            .Where(c => c.gameObject.activeSelf)
                            .OrderBy(c => (transform.position - c.transform.position).sqrMagnitude).ToArray();                                

                        if (orderedByProximity?.Count() > 0)
                        {
                            Organism org = orderedByProximity[0].GetComponent<Organism>();

                            distance = System.Math.Abs((transform.position - org.transform.position).sqrMagnitude);

                            if (distance < 0.01f)
                            {
                                LastMatedTime = DateTime.Now;
                                //org.LastMated = this;
                                //org.LastMatedTime = DateTime.Now;

                                StartCoroutine(Mated());

                                int spawnNumber = CreationManager.CLONECOUNT;

                                //reduce spawns if performance would break!
                                if (CreationManager.Instance.Organisms.Where(o => o.gameObject.activeSelf).Count() > 3000)
                                {
                                    spawnNumber = 1;
                                }
                                //spawn 2 children
                                for (int j = 0; j < spawnNumber; j++)
                                {
                                    CreationManager.Instance.MakeOrganism(this.transform.position, this);
                                }

                                //kill target
                                org.DestroyMe();

                            }
                            else
                            {
                                _direction = org.transform.position - transform.position;
                                Speed = FASTSPEED;

                            }
                        }                        
                    }
                   
                    //if (closeOrganisms.Count > 0)
                    //{
                    //    Speed = FASTSPEED;

                    //    if (closeOrg == null)
                    //    {
                    //        closeOrganisms.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                    //        closeOrg = closeOrganisms[0].Item1;
                    //    }
                        
                    //    _direction = closeOrg.transform.position - transform.position;
                    //}
                    //else
                    //{
                        //don't need chased by phase
                        //for (int i = 0; i < CreationManager.Instance.Organisms.Count; i++)
                        //{
                        //    Organism org = CreationManager.Instance.Organisms[i];

                        //    //check for chased by type close by
                        //    if (org != this && (org.Type == ChasedByType)                                    
                        //                    && (DateTime.Now - this.LastMatedTime).TotalSeconds > 2                                    
                        //                    )
                        //    {
                        //        distance = System.Math.Abs((transform.position - org.transform.position).sqrMagnitude);
                        //        if (distance < 3)
                        //        {
                        //            closeOrganisms.Add(new Tuple<Organism, float>(org, distance));                            
                        //        }
                        //    }
                        //}
                        //
                        //if (closeOrganisms.Count > 0)
                        //{
                        //    closeOrganisms.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                        //    direction = closeOrganisms[0].Item1.transform.position - transform.position;
                        //}
                        //else
                        //{
                        //no need to take each other out?
                        //for (int i = 0; i < CreationManager.Instance.Organisms.Count; i++)
                        //{
                        //    Organism org = CreationManager.Instance.Organisms[i];

                        //    //check for own type close by
                        //    if (org != this && org.Type == this.Type
                        //                    //&& this.LastMated != org
                        //                    //&& org.LastMated != this
                        //                    && (DateTime.Now - this.LastMatedTime).TotalSeconds > 2)
                        //    //&& (DateTime.Now - org.LastMatedTime).TotalSeconds > 1)
                        //    {
                        //        distance = System.Math.Abs((transform.position - org.transform.position).sqrMagnitude);
                        //        if (distance < 0.02f)
                        //        {
                        //            if (distance < 0.01f)
                        //            {
                        //                org.DestroyMe();
                        //                DestroyMe();
                        //                break;
                        //            }
                        //            else
                        //            {
                        //                closeOrganisms.Add(new Tuple<Organism, float>(org, distance));
                        //            }
                        //        }
                        //    }
                        //}
                        //
                        //if (closeOrganisms.Count > 0)
                        //{
                        //    Speed = 0.015f;
                        //    closeOrganisms.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                        //    direction = closeOrganisms[0].Item1.transform.position - transform.position;
                        //}
                        //else
                        //{
                        //Speed = SLOWSPEED;
                        //}
                        //}
                    //}
                }
            }

            //hack to reset glow
            if (!_isGlowing)
            {
                Vector4 vector = new Vector4(1f, 1f, 1f, 1f);
                if (Material.GetVector("_Glow") != vector)
                {
                    Material.SetVector("_Glow", vector);
                }                
            }
        }

        return _direction;
    }

    public void MoveInDirection(Vector3 direction)
    {
        if (!IsMenuItem && !CreationManager.Instance.IsSetup && CreationManager.Instance.IsPlaying)
        {
            transform.position = transform.position + direction.normalized * Speed * Time.deltaTime;
        }
    }

    public void PulseDestroy()
    {        
        StartCoroutine(WaitThenDestroy());
    }

    IEnumerator WaitThenDestroy()
    {        
        float t = 0f;
        while (t < 1f)
        {
            yield return new WaitForSeconds(0.1f);
            Material.SetFloat("_Dim", 1f + (-4 * t));

            t += 0.2f;
        }

        //GameObject.Destroy(this.gameObject);
        this.gameObject.SetActive(false);
    }    

    public void DestroyMe(bool removeFromList = false)
    {
        if (removeFromList)
        {
            CreationManager.Instance.Organisms.Remove(this);
        }
        else
        {
            if (Type == OrganismType.Red)
            {
                CreationManager.Instance.CountGreen++;

                if (CreationManager.Instance.CountGreen == CreationManager.ELECTRICTARGET)
                {
                    CreationManager.Instance.CountGreen = 0;

                    if ((CreationManager.Instance.GreenTimeInterval - DateTime.Now).Seconds < CreationManager.ELECTRICINTERVAL)
                    {
                        GameObject green = Instantiate(CreationManager.Instance.GreenElectric);
                        green.transform.position = this.transform.position;
                    }
                    CreationManager.Instance.GreenTimeInterval = DateTime.Now;
                }
            }
            if (Type == OrganismType.Green)
            {
                CreationManager.Instance.CountBlue++;

                if (CreationManager.Instance.CountBlue == CreationManager.ELECTRICTARGET)
                {
                    CreationManager.Instance.CountBlue = 0;

                    if ((CreationManager.Instance.BlueTimeInterval - DateTime.Now).Seconds < CreationManager.ELECTRICINTERVAL)
                    {
                        GameObject blue = Instantiate(CreationManager.Instance.BlueElectric);
                        blue.transform.position = this.transform.position;
                    }
                    CreationManager.Instance.BlueTimeInterval = DateTime.Now;

                }
            }
            if (Type == OrganismType.Blue)
            {
                CreationManager.Instance.CountRed++;

                if (CreationManager.Instance.CountRed == CreationManager.ELECTRICTARGET)
                {
                    CreationManager.Instance.CountRed = 0;

                    if ((CreationManager.Instance.RedTimeInterval - DateTime.Now).Seconds < CreationManager.ELECTRICINTERVAL)
                    {
                        GameObject red = Instantiate(CreationManager.Instance.RedElectric);
                        red.transform.position = this.transform.position;
                    }
                    CreationManager.Instance.RedTimeInterval = DateTime.Now;

                }
            }
        }

        //GameObject.Destroy(this.gameObject);        
        this.gameObject.SetActive(false);
    }

    public IEnumerator Mated()
    {
        float t = 0f;
        float increment = 20f;
        float glow = 0f;
        _isGlowing = true;
        while (t < 1f)
        {            
            if (glow >= 60)
            {
                increment = -10f;
            }
            glow += increment;

            if (glow <= 0)
            {
                break;
            }

            Material.SetVector("_Glow", new Vector4(glow,glow,glow,glow));

            yield return new WaitForSeconds(0.1f);
            t += 0.1f;
        }
        _isGlowing = false;

        Material.SetVector("_Glow", new Vector4(1f, 1f, 1f, 1f));
    }

    public virtual void Started()
    {        
    }

    public Vector3 GetScreenPoint()
    {
        if (IsMenuItem)
        {
            if (_screenpoint == Vector3.zero)
            {
                _screenpoint = Camera.main.WorldToScreenPoint(this.transform.position);
            }

            return _screenpoint;
        }
        else
        {
            return Camera.main.WorldToScreenPoint(this.transform.position);
        }
    }

    public void Select()
    {
        this.transform.localScale = this.transform.localScale * 2f;
    }

    public void DeSelect()
    {
        this.transform.localScale = this.transform.localScale / 2f;
    }

    public int? GetIndexX()
    {
        for (int i = 0; i < 14; i++)
        {
            if (transform.position.x < CreationManager.SCREENXMIN + i + 1)
            {
                return i;
            }
        }
        return null;
    }

    public int? GetIndexY()
    {
        for (int j = 0; j < 7; j++)
        {
            if (transform.position.x < CreationManager.SCREENYMIN + j + 1)
            {
                return j;
            }
        }
        return null;
    }
}

public enum OrganismType
{
    Red,
    Green,
    Blue
}

public class OrganismGridCell
{
    public List<Organism> ContainedOrganisms { get; set; } = new List<Organism>();
    public List<Organism> CloseOrganisms { get; set; } = new List<Organism>();
}