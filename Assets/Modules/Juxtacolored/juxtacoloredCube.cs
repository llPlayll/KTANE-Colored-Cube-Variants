using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class juxtacoloredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] List<KMSelectable> Corners; // 0 - TL; 1 - TR; 2 - BR; 3 - BL
    [SerializeField] KMSelectable Center;
    [SerializeField] KMSelectable StatusLight;
    [SerializeField] TextMesh ColorblindText;
    [SerializeField] TextMesh IndexText;

    Color[] ColorList = new Color[27]; // Filled through code later; colors are ordered in ascending order of the colors' ternary numbers
    string[] ColorShortNames = {
        "K", "I", "B", "F", "T", "Z", "G", "J", "C",
        "N", "U", "V", "OL", "E", "MA", "L", "MI", "A",
        "R", "S", "M", "O", "SA", "P", "Y", "CR", "W"
    };
    string[] ColorFullNames = {
        "Black", "Indigo", "Blue", "Forest", "Teal", "Azure", "Green", "Jade", "Cyan",
        "Maroon", "Plum", "Violet", "Olive", "Grey", "Maya", "Lime", "Mint", "Aqua",
        "Red", "Rose", "Magenta", "Orange", "Salmon", "Pink", "Yellow", "Cream", "White"
    };
    string[] ColorModifiersGrid = new string[] {
        "HDAFBCE",
        "FHBIGDC",
        "BDAGECI",
        "HIBFADC",
        "JIBCJEJ",
        "EFADGHJ"
    };
    string Base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    int minMoves = 3;
    int maxMoves = 7;

    int baseNumber;
    int[] ColorModifierBlanks = new int[10];
    int validatedGoals = 0;
    int[] Goals = new int[3];
    string[] GoalPaths = new string[3];

    int initColor;
    int[] initPos = new int[2];
    int curColor;
    int[] curPos = new int[2];
    string curPath = "";
    int curGoal = 0;
    float holdTime;
    bool holding, resetIndicated;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        Corners[0].OnInteract += delegate () { CornerPress(0); return false; };
        Corners[1].OnInteract += delegate () { CornerPress(1); return false; };
        Corners[2].OnInteract += delegate () { CornerPress(2); return false; };
        Corners[3].OnInteract += delegate () { CornerPress(3); return false; };
        Center.OnInteract += delegate () { holding = true; return false; };
        Center.OnInteractEnded += delegate () { CenterRelease(); };
        StatusLight.OnInteract += delegate () { SLPress(); return false; };
        ColorblindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
    }

    void CornerPress(int c)
    {
        Corners[c].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Corners[c].transform);

        curPos = Move(curPos, c);
        curColor = Modifier(ColorModifiersGrid[curPos[0]][curPos[1]], curColor, false);
        curPath += c.ToString();
        SetCubeColor();
    }

    void CenterRelease()
    {
        Center.AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Center.transform);

        holding = false;
        holdTime = 0;
        if (resetIndicated)
        {
            resetIndicated = false;
            curColor = initColor;
            curPos[0] = initPos[0];
            curPos[1] = initPos[1];
            curPath = "";
            SetCubeColor();
        }
        else
        {
            if (curColor == Goals[curGoal] && curPath.Length >= minMoves && curPath.Length <= maxMoves)
            {
                curGoal++;
                Log($"Submitted a path of {LogPath(curPath)}, resulting in the color {ColorFullNames[curColor]}, which is the current goal color!{(curGoal != 3 ? $" Next goal color - {ColorFullNames[Goals[curGoal]]}." : "")}");
                if (curGoal == 3)
                {
                    Log($"Submitted all three goal colors in order. Module Solved!");
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    CubeRenderer.material.color = ColorList[6];
                    IndexText.text = "";
                    ColorblindText.text = "!";
                    ColorblindText.color = Color.white;
                    ColorblindText.fontSize = 500;
                }
            }
            else
            {
                if (curColor != Goals[curGoal]) Log($"Submitted a path of {LogPath(curPath)}, resulting in the color {ColorFullNames[curColor]}, which is not the current goal color! Strike!");
                else if (curPath.Length < minMoves) Log($"Submitted a path of {LogPath(curPath)}, which is too short! Strike!");
                else Log($"Submitted a path of {LogPath(curPath)}, which is too long! Strike!");
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

    void SLPress()
    {
        ColorblindText.gameObject.SetActive(!ColorblindText.gameObject.activeInHierarchy);
    }

    void SetCubeColor()
    {
        CubeRenderer.material.color = ColorList[curColor];
        ColorblindText.text = ColorShortNames[curColor];
        IndexText.color = curColor == 26 ? Color.black : Color.white;
        ColorblindText.color = curColor == 26 ? Color.black : Color.white;
        ColorblindText.fontSize = new int[] { 500, 350 }[ColorblindText.text.Length - 1];
    }

    int Modifier(char m, int c, bool forceIncrement)
    {
        int r = TernaryDigit(c, 0);
        int g = TernaryDigit(c, 1);
        int b = TernaryDigit(c, 2);
        switch (m)
        {
            case 'A': // Invert all
                r = r == 0 ? 2 : (r == 2 ? 0 : 1);
                g = g == 0 ? 2 : (g == 2 ? 0 : 1);
                b = b == 0 ? 2 : (b == 2 ? 0 : 1);
                break;
            case 'B': // [Increment/Decrement] all
                if (ColorModifierBlanks[1] == 0 || forceIncrement)
                {
                    r = r == 0 ? 1 : (r == 1 ? 2 : 0);
                    g = g == 0 ? 1 : (g == 1 ? 2 : 0);
                    b = b == 0 ? 1 : (b == 1 ? 2 : 0);
                }
                else
                {
                    r = r == 0 ? 2 : (r == 1 ? 0 : 1);
                    g = g == 0 ? 2 : (g == 1 ? 0 : 1);
                    b = b == 0 ? 2 : (b == 1 ? 0 : 1);
                }
                break;
            case 'C': // Cycle [left/right]
                if (ColorModifierBlanks[2] == 0)
                {
                    r = TernaryDigit(c, 1);
                    g = TernaryDigit(c, 2);
                    b = TernaryDigit(c, 0);
                }
                else
                {
                    r = TernaryDigit(c, 2);
                    g = TernaryDigit(c, 0);
                    b = TernaryDigit(c, 1);
                }
                break;
            case 'D': // Invert [R/G/B]
                if (ColorModifierBlanks[3] == 0) r = r == 0 ? 2 : (r == 2 ? 0 : 1);
                else if (ColorModifierBlanks[3] == 1) g = g == 0 ? 2 : (g == 2 ? 0 : 1);
                else b = b == 0 ? 2 : (b == 2 ? 0 : 1);
                break;
            case 'E': // Increment [R/G/B]
                if (ColorModifierBlanks[4] == 0) r = r == 0 ? 1 : (r == 1 ? 2 : 0);
                else if (ColorModifierBlanks[4] == 1) g = g == 0 ? 1 : (g == 1 ? 2 : 0);
                else b = b == 0 ? 1 : (b == 1 ? 2 : 0);
                break;
            case 'F': // Decrement [R/G/B]
                if (ColorModifierBlanks[5] == 0) r = r == 0 ? 2 : (r == 1 ? 0 : 1);
                else if (ColorModifierBlanks[5] == 1) g = g == 0 ? 2 : (g == 1 ? 0 : 1);
                else b = b == 0 ? 2 : (b == 1 ? 0 : 1);
                break;
            case 'G': // Set to [COLOR]
                return ColorModifierBlanks[6];
            case 'H': // Set R to [0/1/2]
                r = ColorModifierBlanks[7];
                break;
            case 'I': // Set G to [0/1/2]
                g = ColorModifierBlanks[8];
                break;
            case 'J': // Set B to [0/1/2]
                b = ColorModifierBlanks[9];
                break;
            default:
                break;
        }
        return r * 9 + g * 3 + b;
    }

    void Start()
    {
        FillInColors();

        initColor = Rnd.Range(0, 27);
        initPos[0] = Rnd.Range(0, 6);
        initPos[1] = Rnd.Range(0, 7);

        curColor = initColor;
        curPos[0] = initPos[0];
        curPos[1] = initPos[1];
        Log($"Cube's initial position is {LogPos(curPos)}, and cube's initial color is {ColorFullNames[curColor]}");

        SetCubeColor();
        GenerateModulesPuzzle();

        IndexText.text = baseNumber.ToString();
    }

    void GenerateModulesPuzzle()
    {
        validatedGoals = 0;
        for (int i = 0; i < 10; i++)
        {
            switch (i)
            {
                case 0:
                    ColorModifierBlanks[i] = 0;
                    break;
                case 1:
                case 2:
                    ColorModifierBlanks[i] = Rnd.Range(0, 1);
                    break;
                case 3:
                case 4:
                case 5:
                case 7:
                case 8:
                case 9:
                    ColorModifierBlanks[i] = Rnd.Range(0, 2);
                    break;
                case 6:
                    ColorModifierBlanks[i] = Rnd.Range(0, 27);
                    break;
                default:
                    break;
            }
        }

        baseNumber = Rnd.Range(0, 27);
        for (int i = 0; i < 27; i++)
        {
            CalculateGoals();
            ValidateGoals();
            if (validatedGoals == 3) break;
            baseNumber++;
            baseNumber %= 27;
        }
        if (validatedGoals != 3) GenerateModulesPuzzle();
        else
        {
            Log($"Color Modifiers' \"Blanks\": " +
                $"B - {new string[] { "Increment", "Decrement" }[ColorModifierBlanks[1]]}, " +
                $"C - {new string[] { "Left", "Right" }[ColorModifierBlanks[2]]}, " +
                $"D - {new string[] { "Red", "Green", "Blue" }[ColorModifierBlanks[3]]}, " +
                $"E - {new string[] { "Red", "Green", "Blue" }[ColorModifierBlanks[4]]}, " +
                $"F - {new string[] { "Red", "Green", "Blue" }[ColorModifierBlanks[5]]}, " +
                $"G - {ColorFullNames[ColorModifierBlanks[6]]}, " +
                $"H - {ColorModifierBlanks[7]}, " +
                $"I - {ColorModifierBlanks[8]}, " +
                $"J - {ColorModifierBlanks[9]}.");

            Log($"The goal colors (determined from the Base Number of {baseNumber}) are {ColorFullNames[Goals[0]]}, {ColorFullNames[Goals[1]]}, and {ColorFullNames[Goals[2]]}.");
            string[] logPaths = new string[3];
            for (int i = 0; i < 3; i++) logPaths[i] = LogPath(GoalPaths[i]);
            Log($"The goals can be reached as follows: {logPaths[0]}, {logPaths[1]}, {logPaths[2]}.");
        }
    }

    void ValidateGoals()
    {
        List<string> checkedPaths = new List<string> { "" };
        List<int> uncheckedGoals = Goals.Select(x => x).ToList();

        validatedGoals = 0;
        for (int i = 0; i < maxMoves; i++)
        {
            List<string> newPaths = new List<string>();
            foreach (string path in checkedPaths)
            {
                foreach (char m in "0123")
                {
                    string newPath = path + m;
                    newPaths.Add(newPath);
                    int pathColor = FindGoalFromPath(newPath);
                    if (uncheckedGoals.Contains(pathColor))
                    {
                        if (i < minMoves)
                        {
                            validatedGoals = -1;
                            return;
                        }
                        else
                        {
                            validatedGoals++;
                            GoalPaths[Goals.IndexOf(x => x == pathColor)] = newPath;
                            uncheckedGoals.Remove(pathColor);
                        }
                    }
                    if (validatedGoals == 3) return;
                }
            }
            checkedPaths = newPaths.Select(x => x).ToList();
        }
    }

    int FindGoalFromPath(string path)
    {
        int color = initColor;
        int[] pos = new int[] { initPos[0], initPos[1] };
        foreach (char m in path)
        {
            pos = Move(pos, m - '0');
            color = Modifier(ColorModifiersGrid[pos[0]][pos[1]], color, false);
        }
        return color;
    }

    void CalculateGoals()
    {
        for (int i = 0; i < 3; i++)
        {
            int obtTernary = Base36.IndexOf(Bomb.GetSerialNumber()[i * 2]) * 36 + Base36.IndexOf(Bomb.GetSerialNumber()[i * 2 + 1]);
            obtTernary %= 27;

            int setTernary = 0;
            for (int p = 0; p < 3; p++)
            {
                if (TernaryDigit(obtTernary, p) == TernaryDigit(baseNumber, p)) setTernary += (int)Math.Pow(3, 2 - p) * TernaryDigit(obtTernary, p);
                else
                {
                    List<int> digits = new List<int> { 0, 1, 2 };
                    digits.Remove(TernaryDigit(obtTernary, p));
                    digits.Remove(TernaryDigit(baseNumber, p));
                    setTernary += (int)Math.Pow(3, 2 - p) * digits[0];
                }
            }
            Goals[i] = setTernary;
        }
        while (Goals.Distinct().Count() != 3)
        {
            if (Goals[1] == Goals[0]) Goals[1] = Modifier('B', Goals[1], true);
            if (Goals[2] == Goals[0]) Goals[2] = Modifier('B', Goals[2], true);
            if (Goals[2] == Goals[1]) Goals[2] = Modifier('B', Goals[2], true);
        }
    }

    void FillInColors()
    {
        for (int a = 0; a < 3; a++)
        {
            for (int b = 0; b < 3; b++)
            {
                for (int c = 0; c < 3; c++)
                {
                    ColorList[a * 9 + b * 3 + c] = new Color(0.5f * a, 0.5f * b, 0.5f * c);
                }
            }
        }
    }

    int[] Move(int[] pos, int m)
    {
        switch (m)
        {
            case 0: // UL
                pos[0] -= 1;
                pos[1] -= 1;
                break;
            case 1: // UR
                pos[0] -= 1;
                pos[1] += 1;
                break;
            case 2: // DR
                pos[0] += 1;
                pos[1] += 1;
                break;
            case 3: // DL
                pos[0] += 1;
                pos[1] -= 1;
                break;
            default:
                break;
        }
        return new int[] { Mod(pos[0], 6), Mod(pos[1], 7) };
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

    int TernaryDigit(int n, int d)
    {
        switch (d)
        {
            case 0:
                return (n / 9) % 3;
            case 1:
                return (n / 3) % 3;
            case 2:
                return n % 3;
            default:
                return -1;
        }
    }

    string LogPos(int[] pos)
    {
        return $"{"ABCDEFG"[pos[1]]}{pos[0] + 1}";
    }

    string LogPath(string path)
    {
        if (path.Length == 0) return "NONE";
        string logPath = "";
        for (int m = 0; m < path.Length; m++)
        {
            logPath += new string[] { "UL", "UR", "DR", "DL" }[path[m] - '0'];
            if (m < path.Length - 1) logPath += "-";
            if (m >= maxMoves)
            {
                logPath += "...";
                break;
            }
        }
        return logPath;
    }

    void Log(string arg)
    {
        Debug.Log($"[Juxtacolored Cube #{ModuleId}] {arg}");
    }

    void Update()
    {
        if (ModuleSolved) return;
        if (!resetIndicated)
        {
            if (holding) holdTime += Time.deltaTime;
            if (holdTime >= 2)
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, Center.transform);
                resetIndicated = true;
            }
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} UL/UR/DR/DL (TL/TR/BR/BL)> to move up-left/up-right/down-right/down-left (or press the top-left/top-right/bottom-right/bottom-left corners). Chain moves with spaces. Use <!{0} submit> to submit the current color. Use <!{0} reset> to reset the cube. Use <!{0} cb> to toggle colorblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
		var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        bool inputtedSpecial = false;
        if (commandArgs.Length == 1)
        {
            switch (commandArgs[0])
            {
                case "CB":
                    yield return null;
                    StatusLight.OnInteract();
                    inputtedSpecial = true;
                    break;
                case "SUBMIT":
                    yield return null;
                    Center.OnInteractEnded();
                    inputtedSpecial = true;
                    break;
                case "RESET":
                    yield return null;
                    Center.OnInteract();
                    yield return new WaitForSeconds(2.1f);
                    Center.OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                    inputtedSpecial = true;
                    break;
                default:
                    break;
            }
        }
        if (!inputtedSpecial)
        {
            bool valid = true;
            List<int> moves = new List<int>();
            foreach (string arg in commandArgs)
            {
                switch (arg)
                {
                    case "UL":
                    case "TL":
                        moves.Add(0);
                        break;
                    case "UR":
                    case "TR":
                        moves.Add(1);
                        break;
                    case "DR":
                    case "BR":
                        moves.Add(2);
                        break;
                    case "DL":
                    case "BL":
                        moves.Add(3);
                        break;
                    default:
                        valid = false;
                        break;
                }
            }
            if (valid)
            {
                yield return null;
                foreach (int move in moves)
                {
                    Corners[move].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            else yield return "sendtochaterror Invalid command!";
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        for (int g = curGoal; g < 3; g++)
        {
            Center.OnInteract();
            yield return new WaitForSeconds(2.1f);
            Center.OnInteractEnded();
            foreach (char m in GoalPaths[g])
            {
                Corners[m - '0'].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            Center.OnInteractEnded();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
