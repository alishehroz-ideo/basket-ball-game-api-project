using UnityEngine;
using UnityEngine.UI;

public class DataFromReact : MonoBehaviour
{
    public static string gameType = "free";
    public static int difficulty = 1;
    public static string server_userName = "Player";
    public static int server_userID = 0;
    public static int server_coupons = 0;
    public static int server_PT = 0;
    public static int server_RB = 0;
    public static bool isFirstTime;
    public static DataFromReact instance;

    public GameObject leaderboard, couponDetails;
    public Text server_coupon_name, server_coupon_detail, server_coupon_period;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GeneralFunction.intance != null)
            GeneralFunction.intance.myloading.SetActive(false);
        CouponDetailsLeaderboard();
    }

    public void CouponDetailsLeaderboard()
    {
        if (leaderboard != null) leaderboard.SetActive(false);
        if (couponDetails != null) couponDetails.SetActive(false);
    }
}
