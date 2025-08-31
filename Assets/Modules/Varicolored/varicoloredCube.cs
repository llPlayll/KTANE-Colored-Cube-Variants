using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class varicoloredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] List<GameObject> FacePairs; // 0 - Horiz.; 1 - Vert.
    [SerializeField] List<KMSelectable> HorizFaces; // 0 - Left; 1 - Right
    [SerializeField] List<KMSelectable> VertFaces; // 0 - Back; 1 - Front
    [SerializeField] TextMesh ColorblindText;
    [SerializeField] TextMesh IndexText;

    string[] PairNames = { "left/right", "back/front" };
    string[] SubjectTypeNames = { "Number", "Letter", "Color", "Direction" };
    Dictionary<string, string> DirectionNames = new Dictionary<string, string>
    {
        { "⭡", "Up"},
        { "⭧", "Up-Right"},
        { "⭢", "Right"},
        { "⭨", "Down-Right"},
        { "⭣", "Down"},
        { "⭩", "Down-Left"},
        { "⭠", "Left"},
        { "⭦", "Up-Left"},
    };
    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };
    int[,] SubjectTypesTable = new int[8, 8] {
        { 1, 3, 2, 3, 0, 2, 3, 3 },
        { 1, 0, 0, 3, 1, 2, 2, 0 },
        { 0, 0, 0, 3, 2, 1, 3, 1 },
        { 0, 1, 1, 3, 0, 1, 2, 2 },
        { 2, 3, 1, 0, 2, 2, 3, 3 },
        { 1, 1, 0, 1, 3, 2, 0, 3 },
        { 0, 2, 1, 3, 3, 1, 1, 0 },
        { 2, 1, 2, 1, 2, 0, 0, 3 }
    };
    string[,] SubjectsTable = new string[8, 8]
    {
        { "B", "⭧", "M", "⭠", "9", "W", "⭢", "⭠" },
        { "N", "5", "16", "⭩", "Z", "W", "K", "2" },
        { "15", "4", "10", "⭡", "R", "I", "⭩", "X" },
        { "8", "V", "F", "⭨", "14", "P", "R", "C" },
        { "Y", "⭨", "A", "6", "G", "M", "⭣", "⭦" },
        { "L", "Q", "1", "K", "⭣", "B", "3", "⭢" },
        { "12", "Y", "U", "⭧", "⭦", "C", "D", "13" },
        { "B", "G", "K", "S", "G", "11", "7", "⭡" }
    };
    string[] FirstHalves = { "Is your letter", "Is your color", "Is your direction", "Is your color", "Is your letter", "Is your number", "Is your direction", "Is your number" };
    string[] SecondHalves =
    {
        " positioned before N in the alphabet?",
        "’s alphabetic position a multiple of 6?",
        " a primary color (Red, Green, or Blue)?",
        " a multiple of 7?",
        " pointing upwards (diagonals included)?",
        "’s last digit present in the serial number?",
        " pointing in a diagonal direction?",
        " a prime number?",
        "’s first letter of its name present in the serial number?",
        " positioned after P in the alphabet?",
        " the current color of the cube?",
        " pointing in a cardinal direction (Up, Right, Down, or Left)?"
    };
    int[] FirstHalveTypes = { 1, 2, 3, 2, 1, 0, 3, 0 };
    int[] SecondHalveTypes = { 1, 1, 2, 0, 3, 0, 3, 0, 2, 1, 2, 3 };

    int initColor, initNum;
    int subjectType;
    string subjectValue;

    int activePair;
    int moduleState; // 0 - Displaying Subject; 1 - Accepting and Rejecting Questions; 2 - Answering Questions
    int curQuestion, ansQuestions, maxQuestions;
    int qColor, qNumber;
    bool qValid, qAnswer;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        HorizFaces[0].OnInteract += delegate () { HorizPress(0); return false; };
        HorizFaces[1].OnInteract += delegate () { HorizPress(1); return false; };
        VertFaces[0].OnInteract += delegate () { VertPress(0); return false; };
        VertFaces[1].OnInteract += delegate () { VertPress(1); return false; };
        ColorblindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
    }

    void HorizPress(int h)
    {
        HorizFaces[h].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, HorizFaces[h].transform);
        if (moduleState == 0)
        {
            TransitionToState(1);
            return;
        }

        if (qValid != (h == 0))
        {
            Log($"Incorrectly {(h == 0 ? "accepted" : "rejected")} the question when it should've been {(h == 0 ? "rejected" : "accepted")}. Strike! Displaying the subject again...");
            GetComponent<KMBombModule>().HandleStrike();
            TransitionToState(0);
        }
        else
        {
            if (h == 0) TransitionToState(2);
            else
            {
                Log($"Correctly rejected the question. Generating a new one...");
                curQuestion++;
                GenerateQuestion();
            }
        }
    }

    void VertPress(int v)
    {
        VertFaces[v].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, VertFaces[v].transform);
        if (moduleState == 0)
        {
            TransitionToState(1);
            return;
        }

        if (qAnswer != (v == 0))
        {
            Log($"Incorrectly answered {(v == 0 ? "\"Yes\"" : "\"No\"")} to the question. Strike! Displaying the subject again...");
            GetComponent<KMBombModule>().HandleStrike();
            TransitionToState(0);
        }
        else
        {
            curQuestion++;
            ansQuestions++;
            if (ansQuestions == 5)
            {
                Log($"Correctly answered a total of 5 questions. Module solved!");
                GetComponent<KMBombModule>().HandlePass();
                ModuleSolved = true;
                CubeRenderer.material.color = ColorList[1];
                IndexText.text = "";
                ColorblindText.text = "!";
                ColorblindText.color = Color.white;
                if (TwitchPlaysActive)
                {
                    FacePairs[activePair].transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                    FacePairs[activePair].transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                }
            }
            else
            {
                Log($"Correctly answered question {ansQuestions}/5. Generating a new one...");
                TransitionToState(1);
            }
        }
    }

    void SetCubeColor(int color)
    {
        CubeRenderer.material.color = ColorList[color];
        ColorblindText.text = ColorShortNames[color];
        IndexText.color = color == 6 ? Color.black : Color.white;
        ColorblindText.color = color == 6 ? Color.black : Color.white;
    }

    void SetFaces()
    {
        FacePairs[activePair].SetActive(true);
        FacePairs[activePair ^ 1].SetActive(false);
    }

    void Start()
    {
        maxQuestions = Rnd.Range(12, 16);

        activePair = Rnd.Range(0, 2);
        initColor = Rnd.Range(0, 8);
        initNum = Rnd.Range(1, 9);
        SetFaces();
        SetCubeColor(initColor);
        IndexText.text = initNum.ToString();

        subjectType = SubjectTypesTable[initNum - 1, initColor];
        subjectValue = SubjectsTable[initNum - 1, initColor];

        Log($"Cube's initial color is {ColorFullNames[initColor]}, the initial number is {initNum}, and the initially selectable faces pair is {PairNames[activePair]}.");
        Log($"The module's subject is the {SubjectTypeNames[subjectType]} {LogSubject()}.");
    }

    void TransitionToState(int s)
    {
        moduleState = s;
        switch (s)
        {
            case 0:
                activePair = Rnd.Range(0, 2);
                SetFaces();
                SetCubeColor(initColor);
                IndexText.text = initNum.ToString();
                break;
            case 1:
                activePair = 0;
                SetFaces();
                GenerateQuestion();
                break;
            case 2:
                activePair = 1;
                SetFaces();
                break;
            default:
                break;
        }
    }

    void GenerateQuestion()
    {
        bool forceValid = (maxQuestions - curQuestion) == (5 - ansQuestions);
        if (!forceValid) forceValid = Rnd.Range(0, 5) == 0;

        qColor = Rnd.Range(0, 8);
        qNumber = Rnd.Range(1, 13);
        qValid = subjectType == FirstHalveTypes[qColor] && subjectType == SecondHalveTypes[qNumber - 1];
        if (forceValid)
        {
            while (!qValid)
            {
                qColor = Rnd.Range(0, 8);
                qNumber = Rnd.Range(1, 13);
                qValid = subjectType == FirstHalveTypes[qColor] && subjectType == SecondHalveTypes[qNumber - 1];
            }
        }
        SetCubeColor(qColor);
        IndexText.text = qNumber.ToString();
        if (qValid) GetQuestionAnswer();

        Log($"Generated question (from color {ColorFullNames[qColor]} and number {qNumber}): \"{FirstHalves[qColor]}{SecondHalves[qNumber - 1]}\".");
        Log($"The question should be {(qValid ? $"accepted, then answered {(qAnswer ? "\"Yes\"." : "\"No\".")}" : "rejected.")}");
    }

    void GetQuestionAnswer()
    {
        switch (qNumber)
        {
            case 1: // Is your letter positioned before N in the alphabet?
                qAnswer = "ABCDEFGHIJKLM".Contains(subjectValue);
                break;
            case 2: // Is your letter’s alphabetic position a multiple of 6?
                qAnswer = "FLRX".Contains(subjectValue);
                break;
            case 3: // Is your color a primary color (Red, Green, or Blue)?
                qAnswer = "RGB".Contains(subjectValue);
                break;
            case 4: // Is your number a multiple of 7?
                qAnswer = int.Parse(subjectValue) % 7 == 0;
                break;
            case 5: // Is your direction pointing upwards (diagonals included)?
                qAnswer = "⭦⭡⭧".Contains(subjectValue);
                break;
            case 6: // Is your number’s last digit present in the serial number?
                int last = int.Parse(subjectValue) % 10;
                qAnswer = Bomb.GetSerialNumberNumbers().Contains(last);
                break;
            case 7: // Is your direction pointing in a diagonal direction?
                qAnswer = "⭧⭨⭩⭦".Contains(subjectValue);
                break;
            case 8: // Is your number a prime number?
                qAnswer = isPrime(int.Parse(subjectValue));
                break;
            case 9: // Is your color’s first letter of its name present in the serial number?
                char first = ColorFullNames[ColorShortNames.IndexOf(x => x == subjectValue)][0];
                qAnswer = Bomb.GetSerialNumber().Contains(first);
                break;
            case 10: // Is your letter positioned after P in the alphabet?
                qAnswer = "QRSTUVWXYZ".Contains(subjectValue);
                break;
            case 11: // Is your color the current color of the cube?
                qAnswer = subjectValue == ColorShortNames[qColor];
                break;
            case 12: // Is your direction pointing in a cardinal direction (Up, Right, Down, or Left)?
                qAnswer = "⭡⭢⭣⭠".Contains(subjectValue);
                break;
            default:
                break;
        }
    }

    bool isPrime(int n)
    {
        if (n == 1) return false;
        bool prime = true;
        for (int i = 2; i < (int)Math.Sqrt(n) + 1; i++)
        {
            if (n % i == 0)
            {
                prime = false;
                break;
            }
        }
        return prime;
    }

    string LogSubject()
    {
        switch (subjectType)
        {
            case 0:
                return subjectValue;
            case 1:
                return $"'{subjectValue}'";
            case 2:
                return $"{ColorFullNames[ColorShortNames.IndexOf(x => x == subjectValue)]}";
            case 3:
                return $"{subjectValue} ({DirectionNames[subjectValue]})";
            default:
                return "";
        }
    }

    void Log(string arg)
    {
        Debug.Log($"[Varicolored Cube #{ModuleId}] {arg}");
    }

    void Update()
    {
        if (ModuleSolved) return;
        FacePairs[activePair].transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().enabled = TwitchPlaysActive;
        FacePairs[activePair].transform.GetChild(1).GetChild(0).GetComponent<MeshRenderer>().enabled = TwitchPlaysActive;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} left/right/back/front/l/r/b/f> to press the corresponding faces. This command isn't chainable. Use <!{0} cb> to toggle colorblind mode.";
    private bool TwitchPlaysActive = false;
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        switch (Command.ToUpperInvariant())
        {
            case "CB":
                yield return null;
                ColorblindText.gameObject.SetActive(!ColorblindText.gameObject.activeInHierarchy);
                break;
            case "LEFT":
            case "L":
                if (activePair == 1) yield return "sendtochaterror You are unable to interact with the left face!";
                else
                {
                    yield return null;
                    HorizFaces[0].OnInteract();
                }
                break;
            case "RIGHT":
            case "R":
                if (activePair == 1) yield return "sendtochaterror You are unable to interact with the right face!";
                else
                {
                    yield return null;
                    HorizFaces[1].OnInteract();
                }
                break;
            case "BACK":
            case "B":
                if (activePair == 0) yield return "sendtochaterror You are unable to interact with the back face!";
                else
                {
                    yield return null;
                    VertFaces[0].OnInteract();
                }
                break;
            case "FRONT":
            case "F":
                if (activePair == 0) yield return "sendtochaterror You are unable to interact with the front face!";
                else
                {
                    yield return null;
                    VertFaces[1].OnInteract();
                }
                break;
            default:
                yield return "sendtochaterror Invalid command!";
                break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (!ModuleSolved)
        {
            switch (moduleState)
            {
                case 0:
                    if (activePair == 0) HorizFaces[0].OnInteract();
                    else VertFaces[0].OnInteract();
                    break;
                case 1:
                    if (qValid) HorizFaces[0].OnInteract();
                    else HorizFaces[1].OnInteract();
                    break;
                default:
                    if (qAnswer) VertFaces[0].OnInteract();
                    else VertFaces[1].OnInteract();
                    break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}
