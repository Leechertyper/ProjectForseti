using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DroneBoss : MonoBehaviour
{
    // player reference. Will be removed when gameManager is added
    [SerializeField] private GameObject player;

    // the rooms boss grid
    [SerializeField] private DroneBossGrid grid;

    // enemies projectile object
    [SerializeField] private GameObject throwablePackage;


    [SerializeField] private GameObject quickTarget;

    // enemies base movespeed
    [SerializeField] private float moveSpeed = 0.01f;

    // enemys base health
    [SerializeField] private float maxHealth = 1000;

    
    [SerializeField] private float timeBetweenMoves = 5;

    [SerializeField] private Image healthBar;

    [SerializeField] private Image healthTrail;


    //enemy will start using its special attack when it gets lower than a multiple of this number
    private float _healthIntervals = 250;

    private float _currentHealth;

    private float _nextLargeAttack;
    
    private bool _inPosition = false;

    private bool _inMotion = false;

    private Vector2 _targetPos;

    private int _moves = 5;

    private int _movesLeft;

    private bool _healthChanging = false;

    private bool _healthTrailChanging = false;

    // Start is called before the first frame update
    void Start()
    {
        _movesLeft = _moves;
        _currentHealth = maxHealth;
        _healthIntervals = maxHealth / 6;
        _nextLargeAttack = maxHealth -= _healthIntervals;
        StartCoroutine(TimeUntilNextDirectAttack());
    }

    // Update is called once per frame
    void Update()
    {
        if (_inMotion)
        {
            transform.position = Vector2.Lerp(transform.position, _targetPos, moveSpeed * Time.deltaTime);
            if(Mathf.Round(transform.position.x) == _targetPos.x && Mathf.Round(transform.position.y) == _targetPos.y)
            {
                _inPosition = true;
            }
        }

        if (_healthChanging)
        {
            healthBar.fillAmount = Mathf.Lerp(healthBar.fillAmount * 1000, _currentHealth, 2f * Time.deltaTime) / 1000 ;
            if(Mathf.Round(healthBar.fillAmount * 1000) == Mathf.Round(_currentHealth)){
                _healthChanging = false;
            }
        }

        if (_healthTrailChanging)
        {
            healthTrail.fillAmount = Mathf.Lerp(healthTrail.fillAmount * 1000, _currentHealth, 1.5f * Time.deltaTime) / 1000;
            if (Mathf.Round(healthTrail.fillAmount * 1000) == Mathf.Round(_currentHealth))
            {
                _healthTrailChanging = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space) && !grid.isAttacking)
        {
            TakeDamage(100);
        }
    }

    /// <summary>
    /// Will move the boss to a tile at a rapid speed, will do this "fakes" # of times
    /// </summary>
    private void FakeMoveToTile()
    {
        if (!_inMotion)
        {
            Debug.Log("Moving...");
            _targetPos = grid.Grid[(int)Mathf.Round(Random.Range(0, grid.Grid.GetLength(0))), (int)Mathf.Round(Random.Range(0, grid.Grid.GetLength(1)))].GetPosAsVector();
            Debug.Log(_targetPos);
            _inMotion = true;
            _movesLeft--;
            StartCoroutine(Moving());
        }
    }

    /// <summary>
    /// Moves the enemy to a tile where they will begin attacking
    /// </summary>
    /// <param name="tilePos">The index of the tile to move to</param>
    private void MoveToTile(Vector2 tilePos)
    {

    }

    /// <summary>
    /// Will begin the bosses direct attack
    /// </summary>
    private void DirectAttack()
    {
        DroneBossGrid.Tile[,] _grid = grid.Grid;
        float distance = float.PositiveInfinity;
        DroneBossGrid.Tile target = _grid[0, 0];
        Vector2 posInGrid = new Vector2(0,0);
        for (int i = 0; i < grid.Grid.GetLength(0); i++)
        {
            for(int j = 0; j < grid.Grid.GetLength(1); j++)
            {
                if(distance > Vector2.Distance(player.transform.position, _grid[i, j].GetPosAsVector()))
                {
                    distance = Vector2.Distance(player.transform.position, _grid[i, j].GetPosAsVector());
                    target = _grid[i, j];
                    posInGrid = new Vector2(i, j);
                }
            }
        }
        GameObject projectile = Instantiate(throwablePackage);
        projectile.transform.position = transform.position;
        projectile.GetComponent<BoxLerp>().Throw(new Vector3(target.GetX(), target.GetY(), 0));
        SpawnTargets(target, posInGrid);
    }

    void SpawnTargets(DroneBossGrid.Tile pos, Vector2 gridPos)
    {
        Vector2 offsetX = new Vector2(-1, 1);
        Vector2 offsetY = new Vector2(-1, 1);
        DroneBossGrid.Tile[,] _grid = grid.Grid;
        GameObject target = Instantiate(quickTarget);
        target.transform.position = new Vector3(pos.GetX(), pos.GetY(), 0);
        //Debug.Log("X Offset: " + offsetX + "/nY Offset" + offsetY);
        StartCoroutine(Ring2Wait(gridPos, offsetX, offsetY));
    }

    void SpawnRing2(Vector2 gridPos, Vector2 offsetX, Vector2 offsetY)
    {
        DroneBossGrid.Tile[,] _grid = grid.Grid;
        if (offsetX.y + gridPos.x >= _grid.GetLength(0))
        {
            offsetX.y = 0;
        }
        if (gridPos.x + offsetX.x < 0)
        {
            offsetX.x = 0;
        }
        if (offsetY.y + gridPos.y >= _grid.GetLength(1))
        {
            offsetY.y = 0;
        }
        if (gridPos.y + offsetY.x < 0)
        {
            offsetY.x = 0;
        }

        for (int i = (int)(gridPos.x + offsetX.x); i <= (gridPos.x + offsetX.y); i++)
        {
            for (int j = (int)(gridPos.y + offsetY.x); j <= (gridPos.y + offsetY.y); j++)
            {
                if(!Equals(new Vector2(i, j), gridPos)){
                    GameObject target = Instantiate(quickTarget);
                    target.transform.position = _grid[i, j].GetPosAsVector();
                }
                        
            }
        }
    }

    /// <summary>
    /// Will begin the bosses AOE attack
    /// </summary>
    private void MassAttack()
    {
        grid.StartBombAttack();
        //StopAllCoroutines();
        StartCoroutine(MassAttackExit());

    }

    /// <summary>
    /// Will deal damage to the boss
    /// </summary>
    /// <param name="damage">The amount of damage to deal to the boss</param>
    private void TakeDamage(float damage)
    {
        _currentHealth -= damage;
        _healthChanging = true;
        StopCoroutine(HealthBarWait());
        StartCoroutine(HealthBarWait());
        if(_currentHealth < _nextLargeAttack)
        {
            _nextLargeAttack -= _healthIntervals;
            MassAttack();
        }

    }

    /// <summary>
    /// Waits until the player is done moving to a tile
    /// </summary>
    /// <returns></returns>
    IEnumerator Moving()
    {
        yield return new WaitUntil(() =>_inPosition);
        _inMotion = false;
        _inPosition = false;
        if(_movesLeft > 0)
        {
            StartCoroutine(Pause(0.25f));
        } 
        else
        {
            _movesLeft = _moves;
            StartCoroutine(TimeUntilNextDirectAttack());
            DirectAttack();
        }
    }

    IEnumerator Pause(float duration)
    {
        yield return new WaitForSeconds(duration);
        FakeMoveToTile();
    }

    /// <summary>
    /// Starts a timer for the next mass attack
    /// </summary>
    /// <returns></returns>
    IEnumerator TimeUntilNextDirectAttack()
    {
        yield return new WaitForSeconds(timeBetweenMoves);
        FakeMoveToTile();
    }

    IEnumerator MassAttackExit()
    {
        yield return new WaitUntil(() => !grid.isAttacking);
        StartCoroutine(TimeUntilNextDirectAttack());
    }

    IEnumerator Ring2Wait(Vector2 gridPos, Vector2 offsetX, Vector2 offsetY)
    {
        yield return new WaitForSeconds(0.5f);
        SpawnRing2(gridPos, offsetX, offsetY);
        
    }

    IEnumerator HealthBarWait()
    {
        yield return new WaitForSeconds(0.5f);
        _healthTrailChanging = true;
    }
    
}
