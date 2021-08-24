using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Organism : MonoBehaviour
{
    public OrganismType Type { get; set; }
    public OrganismType TargetType { get; set; }
    public OrganismType ChasedByType { get; set; }
    public float Speed = 0.005f;
    public Organism Source;
    public bool IsMenuItem;
    private Vector3 _screenpoint = Vector3.zero;
    protected Vector3 _direction;
    protected Vector3 _rotation;
    public Organism LastMated { get; set; }
    public DateTime LastMatedTime { get; set; } = DateTime.MinValue;
    private float _secsSinceLastCalc = 99;

    // Start is called before the first frame update
    void Start()
    {
        Material material = GetComponent<MeshRenderer>().material;

        material.SetFloat("_RandomRotation1", UnityEngine.Random.Range(-1f, 1f) * (IsMenuItem ? 1f : 3f));
        material.SetFloat("_RandomRotation2", UnityEngine.Random.Range(-1f, 1f) * (IsMenuItem ? 1f : 3f));
        material.SetFloat("_TimeOffset", UnityEngine.Random.Range(-1f, 1f));

        if (IsMenuItem)
        {
            CreationManager.Instance.MenuItemOrganisms.Add(this);
        }
        else
        {
            CreationManager.Instance.Organisms.Add(this);
        }

        _direction = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), 0);
        _rotation = new Vector3(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f));
        Started();
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

    public Vector3 CalcDirection()
    {
        Organism closeOrg = null;
        _secsSinceLastCalc += Time.deltaTime;
        
        if (_secsSinceLastCalc > 0.1f)
        {
            _secsSinceLastCalc = 0;
            if (CreationManager.Instance.IsPlaying)
            {
                float distance = 0;
                List<Tuple<Organism, float>> closeOrganisms = new List<Tuple<Organism, float>>();

                if (!IsMenuItem)
                {
                    Speed = 0.01f;                    

                    for (int i = 0; i < CreationManager.Instance.Organisms.Count; i++)
                    {
                        Organism org = CreationManager.Instance.Organisms[i];

                        //check for target type close by, if they haven't mated last together, they can mate
                        if (org != this && (org.Type == TargetType)
                                        // && this.LastMated != org
                                        //&& org.LastMated != this
                                        && (DateTime.Now - this.LastMatedTime).TotalSeconds > 2
                                        //&& (DateTime.Now - org.LastMatedTime).TotalSeconds > 1
                                        )
                        {
                            distance = System.Math.Abs((transform.position - org.transform.position).sqrMagnitude);
                            if (distance < 3)
                            {
                                if (distance < 0.01f)
                                {
                                    //LastMated = org;
                                    LastMatedTime = DateTime.Now;
                                    //org.LastMated = this;
                                    //org.LastMatedTime = DateTime.Now;

                                    //spawn 2 children
                                    for (int j = 0; j < CreationManager.CLONECOUNT; j++)
                                    {
                                        CreationManager.Instance.MakeOrganism(this.transform.position, this);                                        
                                    }

                                    //kill target
                                    org.DestroyMe();
                                    break;
                                }
                                else
                                {
                                    closeOrganisms.Add(new Tuple<Organism, float>(org, distance));
                                    if (distance < 0.1f)
                                    {
                                        closeOrg = org;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (closeOrganisms.Count > 0)
                    {
                        Speed = 0.015f;

                        if (closeOrg == null)
                        {
                            closeOrganisms.Sort((x, y) => x.Item2.CompareTo(y.Item2));
                            closeOrg = closeOrganisms[0].Item1;
                        }
                        
                        _direction = closeOrg.transform.position - transform.position;
                    }
                    else
                    {
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
                        Speed = 0.005f;
                        //}
                        //}
                    }
                }
            }
        }

        return _direction;
    }

    public void MoveInDirection(Vector3 direction)
    {
        if (!IsMenuItem && !CreationManager.Instance.IsSetup && CreationManager.Instance.IsPlaying)
        {
            transform.position = transform.position + direction.normalized * Speed;
        }
    }

    public void DestroyMe()
    {
        CreationManager.Instance.Organisms.Remove(this);
        GameObject.Destroy(this.gameObject);        
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
}

public enum OrganismType
{
    Red,
    Green,
    Blue
}