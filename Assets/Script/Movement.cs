using UnityEngine;

public class Movement : MonoBehaviour
{
    public float Speed;
    public float JumpForce;
    public int direction = 1;

    Rigidbody2D rb;

    bool Top;
    bool Inside;

    public GameManager gameManager;
    public bool OneTouch;
    float timeRemaining = 1;

    public ParticleSystem FxSmoke;
    public ParticleSystem FxFire;

    public AudioClip Bounce;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            Jump();
        }

        if(OneTouch)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining < 0)
            {
                OneTouch = false;
                timeRemaining = 1;
            }
        }
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        rb.AddForce(Vector2.up * JumpForce, ForceMode2D.Impulse);

        rb.linearVelocity = new Vector2(Speed * direction * Time.fixedDeltaTime, rb.linearVelocity.y);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"[Ball] ENTER tag={collision.tag} obj={collision.gameObject.name} | dir={direction} Top={Top} Inside={Inside}");
        if (collision.CompareTag("Enter"))
        {
            Top = true;
        }

        if (collision.CompareTag("Inside"))
        {
            Inside = true;
        }

        //Nomi Commentted
        //if(collision.CompareTag("TimerBoost"))
        //{
        //    Destroy(collision.gameObject,0.5f);
        //    collision.GetComponent<DOTweenAnimation>().DORestartById("Scaleup");
        //    gameManager.GameBarfull();
        //}
    }

    int firecounter;
    private void OnTriggerExit2D(Collider2D collision)
    {
        Debug.Log($"[Ball] EXIT tag={collision.tag} obj={collision.gameObject.name} | dir={direction} Top={Top} Inside={Inside}");
        if(collision.CompareTag("RightEnd"))
        {
            if(direction == 1)
            {
                Vector2 newPos = new Vector2(-collision.transform.position.x + 1f, transform.position.y);
                Debug.Log($"[Ball] RightEnd TELEPORT | wall worldX={collision.transform.position.x} | ball spawns at X={newPos.x}");
                transform.position = newPos;
                Top = false;
                Inside = false;
            }
        }
        else if(collision.CompareTag("LeftEnd"))
        {
            if (direction == -1)
            {
                Vector2 newPos = new Vector2(-collision.transform.position.x - 1f, transform.position.y);
                Debug.Log($"[Ball] LeftEnd TELEPORT | wall worldX={collision.transform.position.x} | ball spawns at X={newPos.x}");
                transform.position = newPos;
                Top = false;
                Inside = false;
            }
        }

        if (collision.CompareTag("Exit"))
        {
            Debug.Log($"[Ball] EXIT-ZONE | Top={Top} Inside={Inside} | will_score={Top && Inside} | dir={direction}");
            if (Top == true && Inside == true)
            {
                Top = false;
                Inside = false;
                
                GetComponent<Movement>().direction *= -1;

                if(OneTouch == false)
                {
                    if(FxSmoke.isPlaying == false)
                    {
                        FxSmoke.Play();
                        FxFire.Stop();

                        gameManager.SwooshTxt.gameObject.SetActive(true);
                        gameManager.SwooshTxt.text = "SWOOSH x1";
                        gameManager.GenerateAppriciation(2);

                        gameManager.PlayerScored(2);
                    }
                    else
                    {
                        FxSmoke.Stop();
                        FxFire.Play();

                        if(firecounter <= 0)
                        {
                            gameManager.SwooshTxt.gameObject.SetActive(true);
                            gameManager.SwooshTxt.text = "SWOOSH x2";
                            gameManager.GenerateAppriciation(4);

                            gameManager.PlayerScored(4);

                            firecounter++;
                        }
                        else
                        {
                            gameManager.SwooshTxt.gameObject.SetActive(true);
                            gameManager.SwooshTxt.text = "BALL ON FIRE";
                            gameManager.GenerateAppriciation(8);

                            gameManager.PlayerScored(8);

                            firecounter++;
                        }
                        
                    }
                    
                }
                else
                {
                    FxSmoke.Stop();
                    FxFire.Stop();
                    gameManager.PlayerScored(1);

                    gameManager.SwooshTxt.gameObject.SetActive(false);
                    gameManager.SwooshTxt.text = "";
                }
            }
        }
    }



    private float distanceToGround = Mathf.Infinity; // Default distance
    public string groundTag = "Ground";
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("RightEnd"))
        {
            Top = false;
            Inside = false;
        }
        else if (collision.gameObject.CompareTag("LeftEnd"))
        {
            Top = false;
            Inside = false;
        }

        if(collision.gameObject.CompareTag("OneTouch"))
        {
            OneTouch = true;
        }

        if(collision.gameObject.CompareTag("OneTouch"))
        {
            OneTouch = true;
        }

        if(collision.gameObject.CompareTag(groundTag))
        {
            // Check if the collision is with the ground
            if (collision.gameObject.CompareTag(groundTag))
            {
                // Loop through all contact points
                foreach (ContactPoint2D contact in collision.contacts)
                {
                    // Calculate distance from the player's position to the contact point
                    float distance = Vector2.Distance(transform.position, contact.point);

                    // Store the shortest distance
                    if (distance < distanceToGround)
                    {
                        distanceToGround = distance;
                    }

                    if (distance > 0.43f) 
                    {
                        if (!GameManager.instance.isGameOver)
                        {
                            AudioSource audioSource = GetComponent<AudioSource>();

                            // Check if the audio source is already playing
                            if (!audioSource.isPlaying)
                            {
                                audioSource.PlayOneShot(Bounce);
                            }

                        }
                    }
                   
                }

            }

        }
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        // Reset distance when no longer colliding with the ground
        if (collision.gameObject.CompareTag(groundTag))
        {
            distanceToGround = Mathf.Infinity;
        }
    }
}
