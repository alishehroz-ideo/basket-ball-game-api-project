using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Timer")]
    public float countdownTime = 30f;
    private float currentTime;
    public Text countdownText;

    public GameObject free_failed_complete, paid_failed_complete,
        service_failed, service_complete, service_Panel;

    public Image[] lifeIcons;

    [Header("Score UI")]
    public int PT = 0;
    public int RB = 0;
    public Text txt_PT;
    public Text txt_RB;
    public Text txt_GameOver_PT_RB;
    public Text txt_service_GameOver_PT, txt_remaining_service_GameOver_PT;

    public static GameManager instance;
    public bool isGameOver;
    public GameObject Ball;

    public Text ScoreTxt;
    public Text HighScoreTxt;
    public Text TournamentEndScoreTxt;
    public Text Coins;

    public GameObject LeftHoop;
    public GameObject RightHoop;
    public GameObject CurrentHoopPoint;

    public int Score;
    int Hoopdirection = 1;

    public GameObject LoadingSlider;
    public GameObject LoadingPanel;

    [Header("Gameplay")]
    public Text BestTxt;
    public Color NormalColor;
    public Color WarningColor;
    public Image FillBar;
    public float waitTime;
    public GameObject TimerBoost;
    public Transform[] TimerPos;
    public GameObject[] Appriciate;
    public Image SoundBtn;
    public Sprite SoundOn;
    public Sprite SoundOff;
    public Text SwooshTxt;
    public Text AddTxt;
    public GameObject InGameBar;
    public GameObject InGameClock;

    [Header("Sounds")]
    private AudioSource AS;
    public AudioClip Timer;
    public AudioClip BallGoesIn;
    public AudioClip Timeout;
    public AudioClip AudinceSound;
    public AudioClip AudinceSoundAww;

    [Header("Result")]
    public GameObject GameOverPanel;
    public GameObject[] TimeUpPanel;
    public GameObject HighScorePanel;
    public GameObject TournamentScorePanel;

    [Header("Screens")]
    public GameObject MainMenuPanel;
    public GameObject GameplayPanel;

    void Awake()
    {
        if (instance == null)
            instance = this;
    }

    void Start()
    {
        AS = GetComponent<AudioSource>();
        LeftHoop.SetActive(false);
        RightHoop.SetActive(true);
        CurrentHoopPoint = RightHoop.GetComponent<StayAtEdge>().Point;
        ScoreTxt.text = Score.ToString();
        HighScoreTxt.text = "" + PlayerPrefs.GetInt("Highscore", 0);
        BestTxt.text = "" + PlayerPrefs.GetInt("Highscore", 0);
        StartSingleGame();

        txt_PT.gameObject.SetActive(false);
        if (HighScoreTxt != null) HighScoreTxt.gameObject.SetActive(false);
        txt_RB.gameObject.SetActive(false);

        txt_GameOver_PT_RB.text = txt_PT.text;

        if (free_failed_complete != null) free_failed_complete.SetActive(false);
        countdownTime = ResolveTimerDuration();
        currentTime = countdownTime;
        UpdateCountdownText();
        StartCoroutine(ApplyPartnerBallSprite());
    }

    bool Once;
    public void PlayerScored(int gainScore)
    {
        Score += gainScore;
        AddTxt.text = gainScore.ToString();
        AddTxt.GetComponent<DOTweenAnimation>().DORestartAllById("AddText");
        ScoreTxt.text = Score.ToString();

        if (Score > PlayerPrefs.GetInt("Highscore"))
        {
            PlayerPrefs.SetInt("Highscore", Score);
            HighScorePanel.SetActive(true);
            if (!Once)
            {
                AS.PlayOneShot(AudinceSound);
                Once = true;
            }
        }

        HighScoreTxt.text = "" + PlayerPrefs.GetInt("Highscore", 0);
        BestTxt.text = "" + PlayerPrefs.GetInt("Highscore", 0);
        AS.PlayOneShot(BallGoesIn);
        ScoreTxt.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0.25f), 0.5f);
        PlayerPrefs.SetInt("TotalCoins", PlayerPrefs.GetInt("TotalCoins", 0) + gainScore);
        ChangeDirection();
        NextLevel();
    }

    bool soundToggle = true;
    public void SoundOnOff()
    {
        soundToggle = !soundToggle;
        if (soundToggle)
        {
            AudioListener.pause = false;
            SoundBtn.sprite = SoundOn;
        }
        else
        {
            AudioListener.pause = true;
            SoundBtn.sprite = SoundOff;
        }
    }

    public void StartSingleGame()
    {
        InvokeRepeating("GenerateTimerBoost", 15f, 20f);
        Ball.GetComponent<Movement>().enabled = true;
        Ball.GetComponent<Rigidbody2D>().simulated = true;
    }

    public void StartGame()
    {
        InvokeRepeating("GenerateTimerBoost", 15f, 20f);
    }

    public void GenerateTimerBoost()
    {
        int rand = Random.Range(0, TimerPos.Length);
        Instantiate(TimerBoost, TimerPos[rand].position, Quaternion.identity);
    }

    public void GenerateAppriciation(int i)
    {
        switch (i)
        {
            case 2: Instantiate(Appriciate[0], CurrentHoopPoint.transform.position, Quaternion.identity); break;
            case 4: Instantiate(Appriciate[1], CurrentHoopPoint.transform.position, Quaternion.identity); break;
            case 8: Instantiate(Appriciate[2], CurrentHoopPoint.transform.position, Quaternion.identity); break;
        }
    }

    void ChangeDirection()
    {
        Hoopdirection *= -1;
        if (Hoopdirection == 1)
        {
            RightHoop.SetActive(true);
            LeftHoop.SetActive(false);
            CurrentHoopPoint = RightHoop.GetComponent<StayAtEdge>().Point;
            RightHoop.transform.position = new Vector3(RightHoop.transform.position.x, Random.Range(-2, 4), 0);
        }
        else
        {
            RightHoop.SetActive(false);
            LeftHoop.SetActive(true);
            CurrentHoopPoint = LeftHoop.GetComponent<StayAtEdge>().Point;
            LeftHoop.transform.position = new Vector3(LeftHoop.transform.position.x, Random.Range(-2, 4), 0);
        }
    }

    static int num;

    void Update()
    {
        if (Coins != null)
            Coins.text = "" + PlayerPrefs.GetInt("TotalCoins", 0);

        if (!isGameOver)
        {
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                UpdateCountdownText();
            }
            else
            {
                currentTime = 0;
                isGameOver = true;
                OnGameEnd();
            }
        }
    }

    public void OnGameEnd()
    {
        isGameOver = true;
        if (free_failed_complete != null) free_failed_complete.SetActive(true);
        if (txt_GameOver_PT_RB != null)
            txt_GameOver_PT_RB.text = txt_GameOver_PT_RB.text.Replace("PT", "").Replace(":", "");
        CancelInvoke();
        AS.PlayOneShot(Timeout);
        AS.PlayOneShot(PT == 8 ? AudinceSound : AudinceSoundAww);
        GameOverPanel.SetActive(true);
        Ball.GetComponent<Movement>().enabled = false;
        if (GameBull.GameBullLobbyController.Instance != null)
            GameBull.GameBullLobbyController.Instance.SubmitScore(Score, 30000);
        var gb = GameBull.GameBullLobbyController.Instance;
        if (gb != null && (gb.IsRoomGame || gb.IsTournamentGame))
        {
            if (GameOverPanel != null) GameOverPanel.SetActive(false);
            if (free_failed_complete != null) free_failed_complete.SetActive(false);
        }
    }

    public void OpenUrl(string str)
    {
        Application.OpenURL(str);
    }

    void UpdateCountdownText()
    {
        if (countdownText == null) return;
        int seconds = Mathf.FloorToInt(currentTime);
        countdownText.text = seconds >= 0 ? seconds + "s" : "0s";
    }

    private float ResolveTimerDuration()
    {
        float solo = 60f;
        var ctrl = GameBull.GameBullLobbyController.Instance;
        if (ctrl == null) return solo;
        if (ctrl.IsTournamentGame) return 60f;
        if (ctrl.IsRoomGame) return solo; // GetRoomRemainingSeconds() not yet on SDK — using solo default
        return solo;
    }

    private System.Collections.IEnumerator ApplyPartnerBallSprite()
    {
        float t = 0f;
        while (GameBull.GameBullLobbyController.PartnerBallSprite == null && t < 5f)
        {
            t += Time.deltaTime;
            yield return null;
        }
        if (GameBull.GameBullLobbyController.PartnerBallSprite != null && Ball != null)
        {
            var sr = Ball.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = GameBull.GameBullLobbyController.PartnerBallSprite;
                Debug.Log("[GB] Applied partner ball sprite.");
            }
        }
    }

    public void PostScore()
    {
        // Scores saved locally via PlayerPrefs — no backend
    }

    public void NextLevel()
    {
        PT++;
        string str_PT = PT >= 0 ? (PT * 10).ToString() : "00";
        txt_PT.text = "PT : " + str_PT;
        txt_GameOver_PT_RB.text = Score.ToString();
    }
}
