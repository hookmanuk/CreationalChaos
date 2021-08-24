using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreationManager : MonoBehaviour
{
    public TextMesh CountValue;
    public TextMesh CountLabel;
    public TextMesh TimeValue;
    public TextMesh TimeLabel;

    private static CreationManager _creationManager;
    public static CreationManager Instance { get { return _creationManager; } }

    const float ORGANISMSIZE = 20f;
    const float SPAWNWAITTIME = 0.05f;
    public const int CLONECOUNT = 2;
    const int TOTALTOPLACE = 30;
    public const float SCREENXMAX = 7f;
    public const float SCREENXMIN = -7f;
    public const float SCREENYMIN = -2f;
    public const float SCREENYMAX = 5f;


    public Organism ActiveOrganism { get; set; }    
    public TextMesh ClickToBegin;
    public GameObject TitleScreen;
    public TextMesh Phase1Text;
    public TextMesh Phase2Text;
    public TextMesh Phase3Text;

    public float WaitTime { get; set; } = 0;
    private Vector3 _lastClicked = Vector3.zero;
    private int _score = 0;
    private int _count = 0;
    private float _timeLeft = 30;
    public bool IsPlaying { get; set; } = false;
    public bool IsSetup { get; set; } = false;

    public List<Organism> MenuItemOrganisms { get; set; } = new List<Organism>();
    public List<Organism> Organisms { get; set; } = new List<Organism>();

    private void Awake()
    {
        _creationManager = this;
    }    

    // Start is called before the first frame update
    void Start()
    {
        TimeLabel.gameObject.SetActive(false);
        TimeValue.gameObject.SetActive(false);
        CountLabel.gameObject.SetActive(false);
        CountValue.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (IsPlaying)
        {            
            if (IsSetup)
            {
                if (ActiveOrganism == null && MenuItemOrganisms.Count > 0)
                {
                    ActiveOrganism = MenuItemOrganisms[0];
                    ActiveOrganism.Select();
                }

                if (WaitTime > 0)
                {
                    WaitTime -= Time.deltaTime;

                    if (WaitTime < 0)
                    {
                        WaitTime = 0;
                        if (_lastClicked != Vector3.zero)
                        {
                            MakeOrganism(GetPositionFromClick(_lastClicked), ActiveOrganism.Source);
                            _lastClicked = Vector3.zero;
                        }
                    }
                }


                foreach (var touch in Input.touches)
                {
                    CheckClickedPoint(touch.position);
                }

                if (Input.GetMouseButtonDown(0))
                {
                    CheckClickedPoint(Input.mousePosition);
                }
            }
            else
            {
                _timeLeft -= Time.deltaTime;

                if (_timeLeft <= 0)
                {
                    //game finished
                    Phase1Text.gameObject.SetActive(false);
                    Phase2Text.gameObject.SetActive(false);
                    Phase3Text.gameObject.SetActive(true);

                    TimeLabel.gameObject.SetActive(false);
                    TimeValue.gameObject.SetActive(false);
                    
                    IsPlaying = false;
                }

                string strNewTimeLeft = System.Math.Round(_timeLeft).ToString() + " secs";
                if (strNewTimeLeft != TimeValue.text)
                {
                    TimeValue.text = strNewTimeLeft;
                }
            }
        }
        else
        {
            foreach (var touch in Input.touches)
            {
                StartGame();
                break;
            }

            if (Input.GetMouseButtonDown(0))
            {
                StartGame();
            }
        }
    }    

    private void StartGame()
    {
        IsPlaying = true;
        IsSetup = true;        

        for (int i = 0; i < Organisms.Count; i++)
        {
            Destroy(Organisms[i].gameObject);
            Organisms.RemoveAt(i);
            i--;
        }

        ResetCount();

        Phase1Text.gameObject.SetActive(true);
        Phase2Text.gameObject.SetActive(false);
        Phase3Text.gameObject.SetActive(false);
        TitleScreen.SetActive(false);

        TimeLabel.gameObject.SetActive(false);
        TimeValue.gameObject.SetActive(false);
        CountLabel.gameObject.SetActive(true);
        CountValue.gameObject.SetActive(true);
    }

    private void ResetCount()
    {
        _count = TOTALTOPLACE;
        CountValue.text = _count.ToString();
    }

    private void DecreaseCount()
    {
        _count--;
        CountValue.text = _count.ToString();
    }

    private void CheckClickedPoint(Vector3 clickedPoint)
    {
        float heightRatio = GetScreenHeightRatio();
        float widthRatio = GetScreenWidthRatio();

        if (clickedPoint.y < 120 * heightRatio)
        {
            foreach (var org in MenuItemOrganisms)
            {
                Vector3 screenPoint = org.GetScreenPoint();

                if (clickedPoint.x > screenPoint.x - ORGANISMSIZE * widthRatio &&
                    clickedPoint.x < screenPoint.x + ORGANISMSIZE * widthRatio &&
                    clickedPoint.y > screenPoint.y - ORGANISMSIZE * heightRatio &&
                    clickedPoint.y < screenPoint.y + ORGANISMSIZE * heightRatio)
                {
                    if (ActiveOrganism != null)
                    {
                        ActiveOrganism.DeSelect();
                    }
                    ActiveOrganism = org;
                    ActiveOrganism.Select();
                    return;
                }
            }
        }
        else
        {
            if (ActiveOrganism != null)
            {
                //spawning happened too recently
                if (WaitTime > 0)
                {
                    //prevent double click pickup for the same spawn
                    if (WaitTime < SPAWNWAITTIME / 5f)
                    {
                        //store new click for spawn when ready
                        _lastClicked = clickedPoint;
                    }
                }
                else
                {
                    //spawn allowed
                    MakeOrganism(GetPositionFromClick(clickedPoint), ActiveOrganism.Source);
                }
            }
        }
    }

    public Vector3 GetPositionFromClick(Vector3 clickedPoint)
    {
        clickedPoint.z = 7.5f; // select distance = 10 units from the camera
        return Camera.main.ScreenToWorldPoint(clickedPoint);
    }

    public void MakeOrganism(Vector3 clickedPoint, Organism source)
    {
        if (IsSetup)
        {
            DecreaseCount();

            if (_count == 0)
            {
                //set up finished 

                IsSetup = false;
                Phase1Text.gameObject.SetActive(false);
                Phase2Text.gameObject.SetActive(true);
                Phase3Text.gameObject.SetActive(false);
                _timeLeft = 30;

                TimeLabel.gameObject.SetActive(true);
                TimeValue.gameObject.SetActive(true);

                CountLabel.gameObject.SetActive(false);
                CountValue.gameObject.SetActive(false);
            }            
        }

        WaitTime = SPAWNWAITTIME;
        Organism newOrganism = Instantiate(source);

        newOrganism.LastMatedTime = DateTime.Now;
        newOrganism.transform.position = clickedPoint;
        newOrganism.transform.position = new Vector3(newOrganism.transform.position.x, newOrganism.transform.position.y, ActiveOrganism.transform.position.z);      
    }

    public float GetScreenHeightRatio()
    {
        return Screen.height / 600;
    }

    public float GetScreenWidthRatio()
    {
        return Screen.width / 960;
    }
}
