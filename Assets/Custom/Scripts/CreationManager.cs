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
    public TextMesh ScoreValue;
    public TextMesh SimulationTip;
    public TextMesh PlayTip;
    public TextMesh PreGameTip;

    private static CreationManager _creationManager;
    public static CreationManager Instance { get { return _creationManager; } }

    const float ORGANISMSIZE = 20f;
    const float SPAWNWAITTIME = 0.1f;
    const float SCOREINTERVAL = 0.05f;
    public const int CLONECOUNT = 2;
    const int TOTALTOPLACE = 50;
    const int PHASE2LENGTH = 30;
    const float SPEEDUPTIME = 3f;
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
    private float _timeSinceLastScore = 0;
    private int _lastOrganismScored = 0;
    public bool IsPlaying { get; set; } = false;
    public bool IsSetup { get; set; } = false;
    public bool IsScoring { get; set; } = false;

    public bool IsReportingScore { get; set; } = false;

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
        ScoreValue.gameObject.SetActive(false);
        PreGameTip.gameObject.SetActive(true);
        SimulationTip.gameObject.SetActive(false);
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
                        //don't really need this, just use the current click
                        //if (_lastClicked != Vector3.zero)
                        //{
                        //    MakeOrganism(GetPositionFromClick(_lastClicked), ActiveOrganism.Source);
                        //    _lastClicked = Vector3.zero;
                        //}
                    }
                }


                foreach (var touch in Input.touches)
                {
                    CheckClickedPoint(touch.position);
                }

                if (Input.GetMouseButton(0))
                {
                    CheckClickedPoint(Input.mousePosition);
                }
            }
            else
            {
                _timeLeft -= Time.deltaTime;

                if (_timeLeft <= 0)
                {
                    ScoringPhase();
                }
                else
                {
                    CheckDoubleTime();
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
            if (IsScoring)
            {
                CheckDoubleTime();

                _timeSinceLastScore += Time.deltaTime;

                if (_timeSinceLastScore > SCOREINTERVAL)
                {
                    _timeSinceLastScore = 0;

                    if (_lastOrganismScored < Organisms.Count)
                    {
                        Organism org = Organisms[_lastOrganismScored];

                        if (!org.IsMenuItem)
                        {
                            if (org.Type == OrganismType.Red)
                            {
                                _score++;

                                ScoreValue.text = _score.ToString();

                                org.PulseDestroy();
                                _lastOrganismScored++; //destroy on a thread, so not removed from collection yet
                            }
                            else
                            {
                                _timeSinceLastScore += SCOREINTERVAL / 2;
                                org.DestroyMe();
                            }
                        }
                        else
                        {
                            _lastOrganismScored++;
                        }
                    }
                    else
                    {
                        IsSetup = false;
                        IsScoring = false;
                        IsReportingScore = true;

                        Time.timeScale = 1;

                        PlayTip.gameObject.SetActive(false);
                        SimulationTip.gameObject.SetActive(false);
                        PreGameTip.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                //add IsReportingScore section for high scores here                

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
    }

    private void CheckDoubleTime()
    {
        if (Input.touches.Length > 0 || Input.GetMouseButton(0))
        {
            Time.timeScale = SPEEDUPTIME;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    private void StartGame()
    {
        IsPlaying = true;
        IsSetup = true;

        for (int i = 0; i < Organisms.Count; i++)
        {
            try
            {
                Destroy(Organisms[i].gameObject);
            }
            catch (Exception)
            {

            }
        }

        Organisms.Clear();

        ResetCount();

        Phase1Text.gameObject.SetActive(true);
        Phase2Text.gameObject.SetActive(false);
        Phase3Text.gameObject.SetActive(false);
        TitleScreen.SetActive(false);

        TimeLabel.gameObject.SetActive(false);
        TimeValue.gameObject.SetActive(false);
        CountLabel.gameObject.SetActive(true);
        CountValue.gameObject.SetActive(true);

        ScoreValue.gameObject.SetActive(false);

        SimulationTip.gameObject.SetActive(true);
        PlayTip.gameObject.SetActive(false);
        PreGameTip.gameObject.SetActive(false);

        WaitTime = 0.5f;
    }

    private void ScoringPhase()
    {
        //game finished
        Phase1Text.gameObject.SetActive(false);
        Phase2Text.gameObject.SetActive(false);
        Phase3Text.gameObject.SetActive(true);

        TimeLabel.gameObject.SetActive(false);
        TimeValue.gameObject.SetActive(false);

        ScoreValue.gameObject.SetActive(true);

        IsPlaying = false;
        IsScoring = true;

        //dim all organisms
        for (int i = 0; i < Organisms.Count; i++)
        {
            Organism org = Organisms[i];
            if (!org.IsMenuItem)
            {
                org.Material.SetFloat("_Dim", 1f);
            }
        }
    }

    private void ResetCount()
    {
        _score = 0;
        _count = TOTALTOPLACE;
        CountValue.text = _count.ToString();
        ScoreValue.text = _score.ToString();
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
                _timeLeft = PHASE2LENGTH;

                TimeLabel.gameObject.SetActive(true);
                TimeValue.gameObject.SetActive(true);

                CountLabel.gameObject.SetActive(false);
                CountValue.gameObject.SetActive(false);

                SimulationTip.gameObject.SetActive(false);
                PlayTip.gameObject.SetActive(true);
                PreGameTip.gameObject.SetActive(false);
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

    //IEnumerator CaptureScreen()
    //{
    //    //Path to PHP
    //    //string screenShotURL = "http://youtphppath/yourphpname.php";


    //    //Wait until frame ends
    //    yield return new WaitForEndOfFrame();

    //    // Create a texture the size of the screen, RGB24 format
    //    int width = Screen.width;
    //    int height = Screen.height;
    //    var tex = new Texture2D(width, height, TextureFormat.RGB24, false);

    //    // Read screen contents into the texture        
    //    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
    //    tex.Apply();


    //    // Encode texture into PNG
    //    byte[] bytes = tex.EncodeToPNG();
    //    Destroy(tex);

    //    //Convert image to texture
    //    Texture2D loadTexture = new Texture2D(2, 2);
    //    loadTexture.LoadImage(bytes);


    //}
}
