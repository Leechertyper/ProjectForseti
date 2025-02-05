using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatAI : Enemy
{
    public Animator animator;
    public Health hp;

    [SerializeField] private float leapDistance = 20f;
    private GlobalGrid _grid;
    private Vector2Int _gridPos;
    private Transform _target;
    [SerializeField] private state _myState;
    private float _distX;
    private float _distY;
    private bool _awake;
    private bool _attackReady = true;
    private bool _leapReady = true;
    enum state { Dash, Leap, Next, Wait, Attack }


    public override void Awaken()
    {
        _awake = true;
        _myState = state.Dash;
    }

    public override void Sleep()
    {
        StopAllCoroutines();
        _awake = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        _awake = false;
        _myState = state.Next;
        _target = GameObject.FindGameObjectWithTag("Player").transform;
        animator.SetInteger("Direction", 2);
        _grid = GameManager.Instance.Grid;
        _grid.GetTile(transform.position, out _gridPos);
        //BalanceVariables.seenDictionaries["dogEnemy"] = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_awake) return;

        if (_myState == state.Next)
        {
            animator.SetBool("IsAttacking", false);
            animator.SetBool("IsPouncing", false);
            //Debug.Log(Vector2.Distance(_target.transform.position, transform.position));
            if (Vector2.Distance(_target.transform.position, transform.position) < leapDistance && _leapReady)
            {
                _myState = state.Leap;
                //_myState = state.Attack;
            }
            else
            {
                _myState = state.Dash;
            }
        }

        if (_myState == state.Dash)
        {
            _myState = state.Wait;
            _distX = _target.transform.position.x - transform.transform.position.x;
            _distY = _target.transform.position.y - transform.transform.position.y;
            if (Mathf.Abs(_distX) > Mathf.Abs(_distY))
            {
                if (_distX > 0)
                {
                    StartCoroutine(Dash(1, 2));
                }
                else
                {
                    StartCoroutine(Dash(3, 2));
                }
            }
            else
            {
                if (_distY > 0)
                {
                    StartCoroutine(Dash(0, 2));
                }
                else
                {
                    StartCoroutine(Dash(2, 2));
                }
            }
        }
        if (_myState == state.Leap)
        {
            StartCoroutine(LeapAtPlayer());
        }
        if(_myState == state.Attack)
        {
            StartCoroutine(catAttack());
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (_attackReady)
            {
                AkSoundEngine.PostEvent("Play_Cat_Swipe", this.gameObject);
                _myState = state.Attack;
                collision.gameObject.GetComponent<Player>().TakeDamage(BalanceVariables.catEnemy["attackDamage"] * (Mathf.Sqrt(GameManager.Instance.getLevelNum())));
                StartCoroutine(AttackCooldown(2));
            }
        }
    }

    private IEnumerator catAttack()
    {
        while(_myState == state.Attack)
        {
            animator.SetBool("IsAttacking", true);
            yield return null;
        }

        _myState = state.Next;
    }

    private IEnumerator AttackCooldown(float time)
    {
        _attackReady = false;
        while (true)
        {
            yield return new WaitForSeconds(time);
            _attackReady = true;
        }
    }

    private IEnumerator Dash(int direction, int distance)
    {
        animator.SetInteger("Direction", direction);
        Debug.Log(direction);
        animator.SetBool("IsRunning", true);
        distance *= 3;
        float timeElapsed = 0;
        float runTime = 1f;
        Vector3 move;
        switch (direction)
        {
            case 0:
                _gridPos += new Vector2Int(0, -1);
                move = _grid.TileLocation(transform.position, _gridPos);
                break;
            case 1:
                _gridPos += new Vector2Int(1, 0);
                move = _grid.TileLocation(transform.position, _gridPos);
                break;
            case 2:
                _gridPos += new Vector2Int(0, 1);
                move = _grid.TileLocation(transform.position, _gridPos);
                break;
            case 3:
                _gridPos += new Vector2Int(-1, 0);
                move = _grid.TileLocation(transform.position, _gridPos);
                break;
            default:
                move = new Vector3(0, 0, 0);
                break;
        }

        Vector3 inital = transform.position;
        Vector3 goal = new Vector3(move.x, move.y, 0);
        // Debug.Log(goal);
        while (timeElapsed < runTime)
        {
            transform.position = Vector2.Lerp(inital, goal, timeElapsed / runTime);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = goal;
        animator.SetBool("IsRunning", false);
        _myState = state.Next;
        yield break;
    }

    private IEnumerator LeapAtPlayer()
    {
        _myState = state.Wait;
        Vector3 player = _target.transform.position;
        float timeElapsed = 0;
        float runTime = 1f;
        Vector3 inital = transform.position;
        _distX = _target.transform.position.x - transform.transform.position.x;
        _distY = _target.transform.position.y - transform.transform.position.y;
        animator.SetBool("IsPouncing", true);
        while (timeElapsed < runTime)
        {
            transform.position = SampleParabola(inital, player, 2, timeElapsed / runTime);
            if (Mathf.Abs(_distX) > Mathf.Abs(_distY))
            {
                if (_distX > 0)
                {
                    animator.SetInteger("Direction", 1);
                }
                else
                {
                    animator.SetInteger("Direction", 3);
                }
            }
            else
            {
                if (_distY > 0)
                {
                    animator.SetInteger("Direction", 0);
                }
                else
                {
                    animator.SetInteger("Direction", 2);
                }
            }
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = _grid.GetTile(player, out _gridPos);
        StartCoroutine(LeapCooldown(2f));
        _myState = state.Next;
        
        yield break;
    }

    private IEnumerator LeapCooldown(float time)
    {
         _leapReady = false;
        while (true)
        {
            yield return new WaitForSeconds(time);
            _leapReady = true;
        }
    }

    Vector3 SampleParabola(Vector3 start, Vector3 end, float height, float t)
    {
        float parabolicT = t * 2 - 1;
        if (Mathf.Abs(start.y - end.y) < 0.1f)
        {
            //start and end are roughly level, pretend they are - simpler solution with less steps
            Vector3 travelDirection = end - start;
            Vector3 result = start + t * travelDirection;
            result.y += (-parabolicT * parabolicT + 1) * height;
            return result;
        }
        else
        {
            //start and end are not level, gets more complicated
            Vector3 travelDirection = end - start;
            Vector3 levelDirecteion = end - new Vector3(start.x, end.y, start.z);
            Vector3 right = Vector3.Cross(travelDirection, levelDirecteion);
            Vector3 up = Vector3.Cross(right, travelDirection);
            if (end.y > start.y)
                up = -up;
            Vector3 result = start + t * travelDirection;
            result += ((-parabolicT * parabolicT + 1) * height) * up.normalized;
            return result;
        }
    }

    public override void Die()
    {
        AkSoundEngine.PostEvent("Play_Cat_Death", this.gameObject);
        GameObject.Find("ScoreManager").GetComponent<Score>().AddScore(100);
        StopAllCoroutines();
        this.enabled = false;
    }

    public override void TakeDamage()
    {
        int count = Random.Range(0, 2);
        if (count == 0)
        {
            AkSoundEngine.PostEvent("Play_Cat_Hurt", this.gameObject);
        }
    }

    public override float GetHealthVariable()
    {
       return BalanceVariables.catEnemy["maxHealth"] * (Mathf.Sqrt(GameManager.Instance.getLevelNum()));
    }
}
