using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class uncoloredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] List<KMSelectable> Halves; // 0 - Front; 1 - Back
    [SerializeField] TextMesh ColorblindText;
    [SerializeField] TextMesh IndexText;
    [SerializeField] TextMesh TPSolverText;

    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };

    int gameNum, round;
    int discard = -1;

    bool messageSent;
    int opColor, opNumber;
    int plColor, plNumber;
    int opWins, plWins;
    int winStreak;
    bool plWonRound, tie;
    List<string> playedCubes = new List<string> { };

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    bool ZenModeActive;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        foreach (KMSelectable half in Halves)
        {
            half.OnInteract += delegate () { HalfPressed(Halves.IndexOf(half)); return false; };
        }
        ColorblindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
    }

    void HalfPressed(int h)
    {
        Halves[h].AddInteractionPunch();
        if (ModuleSolved) return;
        bool specialSound = false;
        if (!messageSent) SendMessage(h);
        else
        {
            if (tie)
            {
                Log($"Correctly pressed the {(h == 0 ? "front-face" : "back-face")} half of the cube when the players tied.");
                NewRound();
            }
            else if (plWonRound)
            {
                if (h == 0)
                {
                    Log($"Incorrectly pressed the front-face half of the cube when your player won the round, making the opponent quit. Strike! A new opponent is here to play.");
                    GetComponent<KMBombModule>().HandleStrike();
                    NewGame();
                }
                else
                {
                    Log($"Correctly pressed the back-face half of the cube when your player won the round.");
                    plWins++;
                    winStreak++;
                    if (winStreak == 3)
                    {
                        Log($"Your player has now won three games in a row, making the opponent quit. Strike! A new opponent is here to play.");
                        GetComponent<KMBombModule>().HandleStrike();
                        NewGame();
                    }
                    else if (plWins == 3)
                    {
                        Log($"Your player won this game of Colocuber. Module solved!");
                        GetComponent<KMBombModule>().HandlePass();
                        ModuleSolved = true;
                        CubeRenderer.material.color = ColorList[1];
                        IndexText.text = "";
                        ColorblindText.text = "!";
                        ColorblindText.color = Color.white;
                    }
                    else
                    {
                        discard = plColor;
                        NewRound();
                    }
                }
            }
            else
            {
                if (h == 1)
                {
                    Log($"Incorrectly pressed the back-face half of the cube when the opponent won the round, making the opponent quit. Strike! A new opponent is here to play.");
                    GetComponent<KMBombModule>().HandleStrike();
                    NewGame();
                }
                else
                {
                    Log($"Correctly pressed the front-face half of the cube when the opponent won the round.");
                    opWins++;
                    winStreak = 0;
                    if (opWins == 3)
                    {
                        Log($"The opponent won this game of Colocuber. Starting a new game...");
                        NewGame();
                        specialSound = true;
                    }
                    else
                    {
                        discard = opColor;
                        NewRound();
                    }
                }
            }
        }
        Audio.PlayGameSoundAtTransform(specialSound ? KMSoundOverride.SoundEffect.ButtonRelease : KMSoundOverride.SoundEffect.ButtonPress, Halves[h].transform);
    }

    void SetCube(bool opponent)
    {
        IndexText.text = (opponent ? opNumber : plNumber).ToString();
        int color = (opponent ? opColor : plColor) - 1;
        CubeRenderer.material.color = ColorList[color];
        ColorblindText.text = ColorShortNames[color];
        IndexText.color = color == 6 ? Color.black : Color.white;
        ColorblindText.color = color == 6 ? Color.black : Color.white;
    }

    void NewGame()
    {
        gameNum++;
        Log($"Game #{gameNum}");

        discard = -1;
        round = 0;
        plWins = 0;
        opWins = 0;
        winStreak = 0;
        playedCubes = new List<string> { };

        NewRound();
    }

    void NewRound()
    {
        round++;
        Log($"Round {round}:");

        tie = false;
        messageSent = false;
        opColor = Rnd.Range(1, 9);
        opNumber = Rnd.Range(1, 11);
        while (playedCubes.Contains($"{opColor} {opNumber}"))
        {
            opColor = Rnd.Range(1, 9);
            opNumber = Rnd.Range(1, 11);
        }

        Log($"Opponent played a {ColorFullNames[opColor - 1]} cube (color value of {opColor}) with the number value of {opNumber}.");
        SetCube(true);
        playedCubes.Add($"{opColor} {opNumber}");
    }

    void SendMessage(int h)
    {
        bool messageIgnored = false;
        int T = (int)Bomb.GetTime() % 60;
        int C = T % 8 + 1;
        int N = opNumber + (T / 7) * (h == 0 ? -1 : 1);
        if (N < 1 || N > 10) messageIgnored = true;
        Log($"Pressed the {(h == 0 ? "front-face" : "back-face")} half of the cube when the seconds were {LogTime()}. {(messageIgnored ? $"N = {N}. Message ignored" : $"Message sent: C = {C}, N = {N}")}.");
        if (!messageIgnored)
        {
            messageSent = true;
            plColor = C;
            plNumber = N;
            Log($"Your player played a {ColorFullNames[plColor - 1]} cube (color value of {plColor}) with the number value of {plNumber}.");
            if (playedCubes.Contains($"{plColor} {plNumber}"))
            {
                Log($"Your player played a {ColorFullNames[plColor - 1]} cube that was already played this game, making the opponent quit. Strike! A new opponent is here to play.");
                GetComponent<KMBombModule>().HandleStrike();
                NewGame();
            }
            else
            {
                SetCube(false);
                playedCubes.Add($"{plColor} {plNumber}");
                int winner = Judge(true);
                if (winner == 0) tie = true;
                else if (winner == 1) plWonRound = true;
                else plWonRound = false;
            }
        }
    }

    // -1 - Opponent; 0 - Tie; 1 - Player;
    int Judge(bool log)
    {
        int opNewColor = KeepInRange(plColor - opNumber, 8);
        int opNewNumber = KeepInRange(plNumber - opColor, 10);
        int opTotal = KeepInRange(opNewColor * opNewNumber, 10);
        if (log) Log($"Opponent's total cube value is {opNewColor} * {opNewNumber}, kept in range - {opTotal}.");

        int plNewColor = KeepInRange(opColor - plNumber, 8);
        int plNewNumber = KeepInRange(opNumber - plColor, 10);
        int plTotal = KeepInRange(plNewColor * plNewNumber, 10);
        if (log) Log($"Your player's total cube value is {plNewColor} * {plNewNumber}, kept in range - {plTotal}.");

        if (plTotal == opTotal)
        {
            if (log) Log($"Both of the players' total cube values are equal - no one wins this round.");
            return 0;
        }
        else
        {
            List<int> priorities = new List<int> { };
            if (discard != -1)
            {
                switch (discard - 1)
                {
                    case 0: // Red
                        priorities = new List<int> { 1, 2, 3, 4 };
                        break;
                    case 1: // Green
                        priorities = new List<int> { 2, 4, 6, 8, 10 };
                        break;
                    case 2: // Blue
                        priorities = new List<int> { 4, 6, 8, 9, 10 };
                        break;
                    case 3: // Yellow
                        priorities = new List<int> { 2, 3, 5, 7 };
                        break;
                    case 4: // Magenta
                        priorities = new List<int> { 1, 3, 5, 7, 9 };
                        break;
                    case 5: // Cyan
                        priorities = new List<int> { 6, 7, 8, 9, 10 };
                        break;
                    case 6: // White
                        priorities = new List<int> { 3, 6, 9 };
                        break;
                    default: // Black/Empty
                        break;
                }
                if (log) Log($"Discard \"deck\"'s top cube is {ColorFullNames[discard - 1]} - prioritized total cube values: {priorities.Join(", ")}.");
            }
            else if (log) Log($"Discard \"deck\"{(discard == -1 ? " is empty" : "'s top cube is Black")} - no total cube values are prioritized.");

            if (priorities.Contains(plTotal) ^ priorities.Contains(opTotal))
            {
                bool pl = priorities.Contains(plTotal);
                if (log) Log($"{(pl ? "Your player" : "The opponent")}'s total cube value is prioritized, but {(pl ? "the opponent" : "your player")}'s isn't - {(pl ? "your player" : "the opponent")} wins this round.");
                return pl ? 1 : -1;
            }
            else
            {
                bool neither = !priorities.Contains(plTotal) & !priorities.Contains(opTotal);
                bool pl = plTotal > opTotal;
                if (log) Log($"{(neither ? "Neither" : "Both")} of the total cube values are prioritized, and {(pl ? "your player" : "the opponent")}'s total value is greater than {(pl ? "the opponent" : "your player")}'s - {(pl ? "your player" : "the opponent")} wins this round.");
                return pl ? 1 : -1;
            }
        }
    }

    void Start()
    {
        NewGame();
    }

    int KeepInRange(int n, int r)
    {
        return Mod((n - 1), r) + 1;
    }

    int Mod(int n, int m)
    {
        if (m == 0) return 0;
        else if (n > 0) return n % m;
        else
        {
            while (n < 0) n += m;
            return n;
        }
    }

    string LogTime()
    {
        int t = (int)Bomb.GetTime() % 60;
        if (t > 9) return t.ToString();
        else return "0" + t.ToString();
    }

    string IntoTwoDigitNumber(int n)
    {
        if (n > 9) return n.ToString();
        else return "0" + n.ToString();
    }

    void Log(string arg)
    {
        Debug.Log($"[Uncolored Cube #{ModuleId}] {arg}");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} b/back/f/front (##)> to press the back-face/front-face half of the cube, optionally when the seconds on the timer are ##. Use <!{0} cb> to toggle colorblind mode";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        int targetTime = -1;
        bool cb = false;
        if (commandArgs.Length < 1 || commandArgs.Length > 2) yield return "sendtochaterror Invalid command!";
        else if (commandArgs[0] == "CB" && commandArgs.Length == 1)
        {
            yield return null;
            ColorblindText.gameObject.SetActive(!ColorblindText.gameObject.activeInHierarchy);
            cb = true;
        }
        else if (!new string[] { "B", "BACK", "F", "FRONT" }.Contains(commandArgs[0])) yield return "sendtochaterror Invalid press!";
        else if (commandArgs.Length == 2)
        {
            if (!int.TryParse(commandArgs[1], out targetTime)) yield return "sendtochaterror Invalid time!";
            else if (!(0 <= targetTime && targetTime <= 59)) yield return "sendtochaterror Invalid time!";
        }
        if (!cb)
        {
            yield return null;
            if (targetTime != -1)
            {
                while ((int)Bomb.GetTime() % 60 != targetTime)
                {
                    yield return null;
                }
            }
            Halves[commandArgs[0][0] == 'F' ? 0 : 1].OnInteract();
            yield return new WaitForSeconds(0.5f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        TPSolverText.gameObject.SetActive(true);
        while (!ModuleSolved)
        {
            if (!messageSent)
            {
                for (int t = 0; t < 60; t++)
                {
                    for (int h = 0; h < 2; h++)
                    {
                        int T = ((int)Bomb.GetTime() - t * (ZenModeActive ? -1 : 1)) % 60;
                        int C = T % 8 + 1;
                        int N = opNumber + (T / 7) * (h == 0 ? -1 : 1);
                        if (N < 1 || N > 10) continue;

                        plColor = C;
                        plNumber = N;
                        if (playedCubes.Contains($"{plColor} {plNumber}")) continue;
                        int winner = Judge(false);
                        if (winStreak == 2) // Winning now would strike the module, we need to tie/lose
                        {
                            if (opWins == 2 && winner == -1) continue; // Losing now would start a new game
                            if (winner == 1) continue;
                        }
                        else if (winner != 1) continue; // Otherwise, we should win
                        if (T > 0)
                        {
                            TPSolverText.text = $"WAITING FOR {IntoTwoDigitNumber(T)}...";
                            while ((int)Bomb.GetTime() % 60 != T)
                            {
                                yield return null;
                            }
                            Halves[h].OnInteract();
                            yield return new WaitForSeconds(0.1f);
                        }
                        else
                        {
                            TPSolverText.text = $"NOT ENOUGH TIME,\nAUTOSOLVING NOW!";
                            TPSolverText.fontSize = 120;
                            StartCoroutine(AutoSolveTextAnim());
                            messageSent = true;
                            plWins = 2;
                            winStreak = 0;
                            plWonRound = true;
                            tie = false;
                            HalfPressed(1);
                        }
                        break;
                    }
                    if (messageSent) break;
                }
            }
            else
            {
                yield return null;
                if (plWins == 2 && winStreak != 2 && plWonRound && !tie)
                {
                    TPSolverText.text = "AUTOSOLVED!";
                    StartCoroutine(AutoSolveTextAnim());
                }
                Halves[plWonRound ? 1 : 0].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
    }
    IEnumerator AutoSolveTextAnim()
    {
        yield return new WaitForSeconds(2f);
        while (TPSolverText.text.Length > 0)
        {
            TPSolverText.text = TPSolverText.text.Substring(0, TPSolverText.text.Length - 1);
            yield return new WaitForSeconds(0.05f);
        }
    }
}
