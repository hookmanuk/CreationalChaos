using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreationManager : MonoBehaviour
{
    public TextMesh CountValue;   
    public TextMesh TimeValue;    
    public TextMesh ScoreValue;
    public TextMesh SimulationTip;
    public TextMesh PlayTip;    
    public TextMesh HighScores;
    public GameObject Title;
    public TMPro.TMP_InputField InputField;    
    public GameObject Keyboard;
    public Texture2D[] LogoTextures;
    public Material LogoMaterial;
    public GameObject TitleLogo;
    public AudioSource Music;
    public GameObject[] Phases;
    public GameObject[] PhaseLabels;
    public GameObject SimulationCycleOfLife;

    private static CreationManager _creationManager;
    public static CreationManager Instance { get { return _creationManager; } }
    public PlayFabManager PlayFabManager { get; set; } = new PlayFabManager();

    const float ORGANISMSIZE = 20f;
    const float SPAWNWAITTIME = 0.1f;
    const float SCOREINTERVAL = 0.05f;
    public const int CLONECOUNT = 2;
    const int TOTALTOPLACE = 60;
    const int PHASE2LENGTH = 40;
    const float SPEEDUPTIME = 4f;
    public const float SCREENXMAX = 7f;
    public const float SCREENXMIN = -7f;
    public const float SCREENYMIN = -2f;
    public const float SCREENYMAX = 5f;


    public Organism ActiveOrganism { get; set; }    

    public TextMesh ClickToBegin;
    public GameObject TitleScreen;   
    public Organism RedOrganismSource;
    public Organism GreenOrganismSource;
    public Organism BlueOrganismSource;

    public float WaitTime { get; set; } = 0;
    private Vector3 _lastClicked = Vector3.zero;
    private int _score = 0;
    private int _count = 0;
    private float _timeLeft = 30;
    private float _timeSinceLastScore = 0;
    private int _lastOrganismScored = 0;
    private int _lastLogoTextureShownIndex = 0;
    private float _lastLogoTextureShownSecs = 0;
    private string playerName = "";
    private float _last1ColourCheckTime = 0;
    private bool _all1Type = false;
    private int _currentPhase = 0;
    public bool IsPlaying { get; set; } = false;
    public bool IsSetup { get; set; } = false;
    public bool IsScoring { get; set; } = false;

    public bool IsEnteringName { get; set; } = false;
    public bool IsReportingScore { get; set; } = false;

    public bool IsRenderingScores { get; set; } = false;

    public bool IsEndScreen { get; set; } = false;

    public List<Organism> MenuItemOrganisms { get; set; } = new List<Organism>();
    public List<Organism> Organisms { get; set; } = new List<Organism>();

    public float MusicIntro1  = 12.85f;

    public float MusicIntro2  = 17.03f;

    public float MusicIntro3 = 21.21f;
    public float MusicIntro4  = 25.39f;
    public float MusicIntro5  = 29.56f;

    public float MusicMainStart  = 29.56f;

    private void Awake()
    {
        _creationManager = this;
    }

    // Start is called before the first frame update
    void Start()
    {        
        TimeValue.gameObject.SetActive(false);     
        CountValue.gameObject.SetActive(false);
        ScoreValue.gameObject.SetActive(false);        
        SimulationTip.gameObject.SetActive(false);
        HighScores.gameObject.SetActive(false);
        SimulationCycleOfLife.SetActive(false);


        Music.Play();

        SetPhase(0);
    }    

    private void LoopIntroMusic()
    {
        if (!IsEndScreen)
        {
            if (Music.time > MusicIntro5)
            {
                Music.time = MusicIntro1;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {                
        if (IsPlaying)
        {
            if (IsSetup)
            {                
                LoopIntroMusic();

                if (ActiveOrganism == null && MenuItemOrganisms.Count > 0)
                {
                    foreach (var item in MenuItemOrganisms)
                    {
                        if (item.name == "OrganismRedMenu")
                        {
                            ActiveOrganism = item;
                            ActiveOrganism.Select();
                        }
                    }                    
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
                    if (Music.time < MusicMainStart && 
                        ((Music.time >= MusicIntro1 && Music.time < MusicIntro1 + 0.1f)
                        || (Music.time >= MusicIntro2 && Music.time < MusicIntro2 + 0.1f)
                        || (Music.time >= MusicIntro3 && Music.time < MusicIntro3 + 0.1f)
                        || (Music.time >= MusicIntro4 && Music.time < MusicIntro4 + 0.1f)
                        || (Music.time >= MusicIntro5 && Music.time < MusicIntro5 + 0.1f)
                        ))
                    {
                        Music.time = MusicMainStart;
                    }
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

                        if (!org.IsMenuItem && org.isActiveAndEnabled)
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
                                //loop all non red in a row, in one frame
                                while (org.Type != OrganismType.Red)
                                {
                                    org.DestroyMe();            
                                    if (Organisms.Count >= _lastOrganismScored)
                                    {
                                        break;
                                    }
                                    org = Organisms[_lastOrganismScored];                                    
                                }                                                               
                            }

                            //as score goes by, reduce the time for each score to show
                            _timeSinceLastScore = Math.Min(_score / 400f, 1f) * SCOREINTERVAL * 0.9f;
                        }
                        else
                        {
                            _lastOrganismScored++;
                        }
                    }
                    else
                    {
                        StartCoroutine(ShowKeyboard());
                        _lastOrganismScored = 0;                        
                    }
                }
            }
            else
            {
                if (IsEnteringName)
                {
                    //show keyboard
                    if (!Keyboard.activeSelf)
                    {
                        Keyboard.SetActive(true);
                        InputField.text = PlayFabManager.GetName();
                    }
                }
                else
                {
                    if (IsReportingScore)
                    {
                        //add IsReportingScore section for high scores here                                        
                        PlayFabManager.RecordScore(playerName, _score, (result) =>
                        {
                            StartCoroutine(ShowHighScores());
                        });

                        IsReportingScore = false;
                        IsRenderingScores = true;
                    }
                    else
                    {
                        if (IsRenderingScores)
                        {
                            //wait for rendering to finsh on another thread
                        }
                        else
                        {                            
                            LoopIntroMusic();                                                        

                            if (TitleLogo.activeSelf)
                            {
                                _lastLogoTextureShownSecs += Time.deltaTime;

                                if (_lastLogoTextureShownSecs >= 0.066)
                                {
                                    _lastLogoTextureShownSecs = 0;
                                    _lastLogoTextureShownIndex++;

                                    if (_lastLogoTextureShownIndex >= 46)
                                    {
                                        _lastLogoTextureShownIndex = 0;
                                    }

                                    LogoMaterial.SetTexture("_EmissionMap", LogoTextures[_lastLogoTextureShownIndex]);
                                }
                            }

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
            }
        }
    }

    IEnumerator ShowKeyboard()
    {
        SetPhase(4);
        yield return new WaitForSeconds(1);
        
        IsSetup = false;
        IsScoring = false;
        IsEnteringName = true;
        IsReportingScore = false;

        Time.timeScale = 1;

        PlayTip.gameObject.SetActive(false);
        SimulationTip.gameObject.SetActive(false);        
    }

    IEnumerator ShowHighScores()
    {
        //wait for the scores to update on the server with the new score added
        ScoreValue.gameObject.SetActive(false);
        HighScores.gameObject.SetActive(true);
        TitleScreen.SetActive(true);
        HighScores.text = "High Scores:" + Environment.NewLine + Environment.NewLine + "Updating...";

        yield return new WaitForSeconds(1);

        //show high scores
        PlayFabManager.GetHighScores((result) =>
        {
            string strScores = "High Scores:" + Environment.NewLine + Environment.NewLine;
            foreach (var item in result.Leaderboard)
            {
                strScores += item.Position + 1 + ". " + item.DisplayName + " - " + item.StatValue + Environment.NewLine;
            }
        
            HighScores.text = strScores;
        });

        IsRenderingScores = false;

        IsEndScreen = true;

        SetPhase(0);
    }

    private void CheckDoubleTime()
    {
        _last1ColourCheckTime += Time.deltaTime;
        if (!_all1Type && _last1ColourCheckTime > 1f && Organisms.Count > 0)
        {
            _last1ColourCheckTime = 0;

            OrganismType orgType;
            _all1Type = true;
            orgType = Organisms[0].Type;
            for (int i = 0; i < Organisms.Count; i++)
            {
                if (orgType != Organisms[i].Type)
                {
                    _all1Type = false;
                    break;
                }
            }            
        }
        
        if (Input.touches.Length > 0 || Input.GetMouseButton(0))
        {
            Time.timeScale = (_all1Type ? 12f : SPEEDUPTIME);
        }
        else
        {
            Time.timeScale = 1;
        }        
    }

    IEnumerator RestartMusic()
    {
        float t = 0;
        while (t <= 2f)
        {
            t += 0.1f;
            Music.volume = 1 - (1 * t/2);

            yield return new WaitForSeconds(0.1f);            
        }

        Music.time = 0;
        Music.volume = 1;
        IsEndScreen = false;
    }

    private void StartGame()
    {
        SetPhase(1);
        IsPlaying = true;
        IsSetup = true;
        if (IsEndScreen)
        {
            StartCoroutine(RestartMusic());
        }

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
        
        TitleScreen.SetActive(false);
        Title.SetActive(false);
        
        TimeValue.gameObject.SetActive(false);
        CountValue.gameObject.SetActive(true);

        ScoreValue.gameObject.SetActive(false);

        SimulationTip.gameObject.SetActive(true);
        PlayTip.gameObject.SetActive(false);        
        HighScores.gameObject.SetActive(false);
        TitleLogo.gameObject.SetActive(false);
        SimulationCycleOfLife.SetActive(true);

        WaitTime = 0.5f;
        _all1Type = false;
    }

    private void ScoringPhase()
    {
        //game finished        
        
        TimeValue.gameObject.SetActive(false);
        SimulationCycleOfLife.SetActive(false);

        ScoreValue.gameObject.SetActive(true);

        IsPlaying = false;
        IsScoring = true;
        SetPhase(3);

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
        CountValue.text = "0 / " + _count.ToString();
        ScoreValue.text = _score.ToString();
    }

    private void DecreaseCount()
    {
        _count--;
        CountValue.text = (TOTALTOPLACE - _count).ToString() + " / " + TOTALTOPLACE.ToString();
    }

    public void SetActiveOrganism(Organism org)
    {
        ActiveOrganism?.DeSelect();
        ActiveOrganism = org;
        ActiveOrganism.Select();
    }

    private void CheckClickedPoint(Vector3 clickedPoint)
    {
        float heightRatio = GetScreenHeightRatio();
        float widthRatio = GetScreenWidthRatio();

        if (clickedPoint.y < 120 * heightRatio)
        {
            //foreach (var org in MenuItemOrganisms)
            //{
            //    Vector3 screenPoint = org.GetScreenPoint();

            //    if (clickedPoint.x > screenPoint.x - ORGANISMSIZE * widthRatio &&
            //        clickedPoint.x < screenPoint.x + ORGANISMSIZE * widthRatio &&
            //        clickedPoint.y > screenPoint.y - ORGANISMSIZE * heightRatio &&
            //        clickedPoint.y < screenPoint.y + ORGANISMSIZE * heightRatio)
            //    {
            //        if (ActiveOrganism != null)
            //        {
            //            ActiveOrganism.DeSelect();
            //        }
            //        ActiveOrganism = org;
            //        ActiveOrganism.Select();
            //        return;
            //    }
            //}
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
                SetPhase(2);
                IsSetup = false;                
                _timeLeft = PHASE2LENGTH;
                
                TimeValue.gameObject.SetActive(true);

                CountValue.gameObject.SetActive(false);

                SimulationTip.gameObject.SetActive(false);
                PlayTip.gameObject.SetActive(true);                
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

    public void TextEntryDone()
    {
        playerName = InputField.text;        
        Keyboard.SetActive(false);
        
        IsEnteringName = false;
        IsReportingScore = true;        
    }

    private void SetPhase(int phase)
    {
        _currentPhase = phase;

        Color baseWhite = new Color(0.5f, 0.5f, 0.5f, 1);

        for (int i = 0; i < Phases.Length; i++)
        {
            if (i == phase)
            {
                Color highlightColor = new Color(4f,4f,4f, 1);
                Phases[i].GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", highlightColor);

                if (PhaseLabels[i] != null)
                {
                    PhaseLabels[i].SetActive(true);
                }
            }
            else
            {
                Phases[i].GetComponent<MeshRenderer>().material.SetColor("_EmissionColor", baseWhite);

                if (PhaseLabels[i] != null)
                {
                    PhaseLabels[i].SetActive(false);
                }
            }
        }             
    }
}
