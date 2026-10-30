using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public enum GameState
{
    Playing,
    Dead,
    Won
}

// The heist game loop. Put ONE of these in the level.
//
//   Steal the item  ->  get to the exit  ->  end screen.
//   An enemy that is CHASING reaches the player  ->  the player dies.
//   H  = restart the whole game (any time).
//   R  = respawn at the last checkpoint (only when dead).
//
// The text is drawn with OnGUI, so you do NOT have to set up a Canvas.
//
// IMPORTANT: the level scene must be added to Build Settings
// (File > Build Profiles, or File > Build Settings).
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public bool HasObjective { get; private set; }

    public bool IsPlaying
    {
        get { return State == GameState.Playing; }
    }

    public Transform Player
    {
        get { return player; }
    }

    [Header("Player")]
    [SerializeField] private Transform player;
    [Tooltip("The colour the player turns when it dies.")]
    [SerializeField] private Color deadColor = Color.gray;

    [Header("Keys")]
    [SerializeField] private KeyCode restartKey = KeyCode.H;
    [SerializeField] private KeyCode respawnKey = KeyCode.R;

    private readonly List<StealthEnemyAI> enemies = new List<StealthEnemyAI>();
    private NavMeshAgent playerAgent;
    private Renderer playerRenderer;
    private Color playerNormalColor;
    private Vector3 checkpointPosition;
    private string hintMessage = "";
    private float hintTimer;

    private void Awake()
    {
        Instance = this;
        State = GameState.Playing;
        Time.timeScale = 1f;

        if (player == null)
        {
            Debug.LogError("GameManager: drag the Player into the Player box!");
            enabled = false;
            return;
        }

        playerAgent = player.GetComponent<NavMeshAgent>();
        playerRenderer = player.GetComponentInChildren<Renderer>();

        if (playerRenderer != null)
        {
            playerNormalColor = playerRenderer.material.color;
        }
    }

    private void Start()
    {
        // The start of the level is the first checkpoint.
        checkpointPosition = FindFloorPosition(player.position);
    }

    private void Update()
    {
        if (hintTimer > 0f)
        {
            hintTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(restartKey))
        {
            RestartLevel();
            return;
        }

        if (State == GameState.Dead && Input.GetKeyDown(respawnKey))
        {
            RespawnAtCheckpoint();
        }
    }

    // ---------- Called by other scripts ----------

    // Every enemy tells the GameManager it exists, so a respawn can
    // calm them all down.
    public void RegisterEnemy(StealthEnemyAI enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    // ObjectiveItem calls this when the player picks it up.
    public void CollectObjective()
    {
        if (!IsPlaying)
        {
            return;
        }

        HasObjective = true;
        ShowHint("Got the item! Now escape to the exit!", 3f);
    }

    // A Checkpoint calls this when the player reaches it.
    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = FindFloorPosition(position);
        ShowHint("Checkpoint saved!", 2f);
    }

    // An enemy calls this when it is chasing and reaches the player.
    public void PlayerDied()
    {
        if (!IsPlaying)
        {
            return;
        }

        State = GameState.Dead;

        if (playerAgent != null)
        {
            playerAgent.ResetPath();
            playerAgent.velocity = Vector3.zero;
        }

        if (playerRenderer != null)
        {
            playerRenderer.material.color = deadColor;
        }
    }

    // ExitZone calls this when the player stands in the exit.
    public void TryFinish()
    {
        if (!IsPlaying)
        {
            return;
        }

        if (!HasObjective)
        {
            ShowHint("You need to steal the item first!", 2f);
            return;
        }

        State = GameState.Won;
        Time.timeScale = 0f;
    }

    // ---------- Restart and respawn ----------

    // H, or the Retry button: start the whole game again.
    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // R: come back to life at the last checkpoint.
    // The item you already stole stays stolen.
    public void RespawnAtCheckpoint()
    {
        if (State != GameState.Dead)
        {
            return;
        }

        if (playerAgent != null)
        {
            playerAgent.Warp(checkpointPosition);
            playerAgent.ResetPath();
        }
        else
        {
            player.position = checkpointPosition + Vector3.up;
        }

        if (playerRenderer != null)
        {
            playerRenderer.material.color = playerNormalColor;
        }

        foreach (StealthEnemyAI enemy in enemies)
        {
            enemy.ResetToPatrol();
        }

        State = GameState.Playing;
        ShowHint("Respawned at the checkpoint.", 2f);
    }

    public void NextLevel()
    {
        Time.timeScale = 1f;

        if (HasNextLevel())
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    // ---------- Helpers ----------

    private bool HasNextLevel()
    {
        return SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
    }

    private void ShowHint(string message, float seconds)
    {
        hintMessage = message;
        hintTimer = seconds;
    }

    // Finds the spot on the floor (the NavMesh) under a position.
    private Vector3 FindFloorPosition(Vector3 position)
    {
        NavMeshHit hit;

        if (NavMesh.SamplePosition(position, out hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return position;
    }

    // ---------- Screen text and buttons ----------

    private void OnGUI()
    {
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 26;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.white;

        string levelName = SceneManager.GetActiveScene().name;
        string objectiveText = HasObjective ? "Escape to the exit!" : "Steal the item!";
        GUI.Label(new Rect(15f, 10f, 900f, 40f), levelName + "  -  " + objectiveText, textStyle);

        if (State == GameState.Playing && hintTimer > 0f)
        {
            textStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(15f, 50f, 900f, 40f), hintMessage, textStyle);
        }

        GUIStyle smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 20;
        smallStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(15f, Screen.height - 45f, 900f, 40f), restartKey + " = restart the game", smallStyle);

        if (State == GameState.Dead)
        {
            DrawDeathScreen();
        }

        if (State == GameState.Won)
        {
            DrawEndScreen();
        }
    }

    private void DrawDeathScreen()
    {
        GUIStyle bigStyle = new GUIStyle(GUI.skin.label);
        bigStyle.fontSize = 64;
        bigStyle.fontStyle = FontStyle.Bold;
        bigStyle.alignment = TextAnchor.MiddleCenter;
        bigStyle.normal.textColor = Color.red;

        GUI.Label(new Rect(0f, Screen.height * 0.30f, Screen.width, 100f), "YOU DIED", bigStyle);

        GUIStyle infoStyle = new GUIStyle(GUI.skin.label);
        infoStyle.fontSize = 32;
        infoStyle.fontStyle = FontStyle.Bold;
        infoStyle.alignment = TextAnchor.MiddleCenter;
        infoStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(0f, Screen.height * 0.30f + 100f, Screen.width, 50f), "Press " + respawnKey + " to respawn at the checkpoint", infoStyle);
        GUI.Label(new Rect(0f, Screen.height * 0.30f + 150f, Screen.width, 50f), "Press " + restartKey + " to restart the game", infoStyle);
    }

    private void DrawEndScreen()
    {
        float boxWidth = 520f;
        float boxHeight = 320f;
        Rect boxRect = new Rect((Screen.width - boxWidth) / 2f, (Screen.height - boxHeight) / 2f, boxWidth, boxHeight);

        // Drawn twice so the box is darker and easier to read.
        GUI.Box(boxRect, "");
        GUI.Box(boxRect, "");

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 40;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.green;

        GUI.Label(new Rect(boxRect.x, boxRect.y + 20f, boxWidth, 70f), "LEVEL COMPLETE!", titleStyle);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 28;

        if (GUI.Button(new Rect(boxRect.x + 50f, boxRect.y + 120f, boxWidth - 100f, 70f), "Retry Level", buttonStyle))
        {
            RestartLevel();
        }

        string nextLabel = HasNextLevel() ? "Next Level" : "Play Again";

        if (GUI.Button(new Rect(boxRect.x + 50f, boxRect.y + 215f, boxWidth - 100f, 70f), nextLabel, buttonStyle))
        {
            NextLevel();
        }
    }
}