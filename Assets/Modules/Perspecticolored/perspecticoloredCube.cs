using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class perspecticoloredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] List<KMSelectable> Faces; // 0 - Top; 1 - Front; 2 - Right; 3 - Back; 4 - Left
    [SerializeField] GameObject ColorblindTextsParent;
    [SerializeField] TextMesh[] ColorblindTexts;
    [SerializeField] TextMesh IndexText;
    [SerializeField] TextMesh DebugAngleText;

    string[] FaceNames = { "Top", "Front", "Right", "Back", "Left", "Bottom" };
    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };
    int[,] TableNumbers = new int[8, 8]
    {
        { -1, 51, 45, 67, 62, 94, 42, 11 },
        { 92, -1, 19, 53, 90, 55, 47, 34 },
        { 59, 96, -1, 79, 63, 26, 50, 52 },
        { 14, 27, 10, -1, 83, 15, 6, 75 },
        { 18, 22, 85, 99, -1, 37, 73, 4 },
        { 35, 66, 97, 77, 13, -1, 80, 25 },
        { 28, 57, 69, 93, 68, 41, -1, 8 },
        { 24, 88, 43, 3, 60, 64, 56, -1 }
    };
    string[] TableOperations =
    {
        " +++*-*+",
        "- -*----",
        "*+ -+++-",
        "+-- -++*",
        "*+-+ -++",
        "+**+- +-",
        "-+---- +",
        "-*+-++- "
    };
    Vector3[] faceAngles =
    {
        new Vector3(90, 0, 90),
        new Vector3(0, 0, 90),
        new Vector3(90, 0, 180),
        new Vector3(180, 0, 90),
        new Vector3(90, 0, 0)
    };

    int[] faceColors = new int[6];
    int[] referredFaces = { 0, 1, 2, 3, 4, 5 };
    int[] pairedFaces = new int[2];
    int[] pairValues = new int[2];

    int topNumber;
    int displayedFace;
    float holdTime;
    int holdDigit;
    bool holding, holdIndicated;
    int inputsRecieved = 0;
    int[] heldFaces = new int[2];
    int[] heldDigits = new int[2];

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        foreach (KMSelectable face in Faces)
        {
            face.OnInteract += delegate () { FaceHold(); return false; };
            face.OnInteractEnded += delegate () { FaceRelease(Faces.IndexOf(face)); };
        }
        ColorblindTextsParent.SetActive(Colorblind.ColorblindModeActive);
    }

    void SetCubeColor(int color, int face)
    {
        CubeRenderer.material.color = ColorList[color];
        IndexText.color = color == 6 ? Color.black : Color.white;
        for (int f = 0; f < 5; f++)
        {
            ColorblindTexts[f].text = f == face ? ColorShortNames[color] : "";
            ColorblindTexts[f].color = color == 6 ? Color.black : Color.white;
        }
    }

    void FaceHold()
    {
        holding = true;
        holdDigit = (int)Bomb.GetTime() % 10;
    }

    void FaceRelease(int f)
    {
        Faces[f].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Faces[f].transform);

        holding = false;
        holdTime = 0;
        if (holdIndicated)
        {
            holdIndicated = false;
            heldFaces[inputsRecieved] = f;
            heldDigits[inputsRecieved] = holdDigit;
            inputsRecieved++;
            if (inputsRecieved == 2)
            {
                bool correct = ContentsEqual(heldFaces, pairedFaces, true) && ContentsEqual(heldDigits, pairValues, false);
                Log($"Held the {FaceNames[heldFaces[0]]} face at a {heldDigits[0]}, then the {FaceNames[heldFaces[1]]} face at a {heldDigits[1]}, which is {(correct ? "correct. Module Solved!" : "incorrect. Strike!")}");
                if (correct)
                {
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    CubeRenderer.material.color = ColorList[1];
                    IndexText.text = "";
                    for (int i = 0; i < 5; i++)
                    {
                        ColorblindTexts[i].text = i == 0 ? "!" : "";
                        ColorblindTexts[i].color = Color.white;
                    }
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    inputsRecieved = 0;
                }
            }
        }
        else ModifyNumber(f);
    }

    void ModifyNumber(int f)
    {
        int tapColor = faceColors[f];
        int referredColor = faceColors[referredFaces[f]];

        int number = TableNumbers[tapColor, referredColor];
        char operation = TableOperations[tapColor][referredColor];
        switch (operation)
        {
            case '+':
                topNumber += number;
                break;
            case '-':
                topNumber -= number;
                break;
            case '*':
                topNumber *= number;
                break;
            default:
                break;
        }
        topNumber = Mod(topNumber, 100);
        IndexText.text = topNumber.ToString();
    }

    void Start()
    {
        faceColors = new int[8] { 0, 1, 2, 3, 4, 5, 6, 7 }.Shuffle().Take(6).ToArray();
        Log($"Face Colors: Top - {ColorFullNames[faceColors[0]]}, Front - {ColorFullNames[faceColors[1]]}, Right - {ColorFullNames[faceColors[2]]}, Back - {ColorFullNames[faceColors[3]]}, Left - {ColorFullNames[faceColors[4]]} (Bottom - {ColorFullNames[faceColors[5]]}).");
        topNumber = Rnd.Range(0, 100);
        IndexText.text = topNumber.ToString();
        GenRefers();
        Log($"Referred Faces: Top - {FaceNames[referredFaces[0]]}, Front - {FaceNames[referredFaces[1]]}, Right - {FaceNames[referredFaces[2]]}, Back - {FaceNames[referredFaces[3]]}, Left - {FaceNames[referredFaces[4]]} (Bottom - {FaceNames[referredFaces[5]]}).");
        CalculateValues();
        Log($"Face A - {FaceNames[pairedFaces[0]]}, Face B - {FaceNames[pairedFaces[1]]}; Value A = {pairValues[0]}, Value B = {pairValues[1]}.");
    }

    void GenRefers()
    {
        bool valid = false;
        while (!valid)
        {
            referredFaces = referredFaces.Shuffle();
            bool selfRefer = false;
            int pairedFacesCount = 0;
            for (int f = 0; f < 6; f++)
            {
                if (referredFaces[f] == f)
                {
                    selfRefer = true;
                    break;
                }
                if (referredFaces[referredFaces[f]] == f)
                {
                    pairedFacesCount++;
                    pairedFaces[0] = referredFaces[f];
                    pairedFaces[1] = f;
                }
            }
            if (selfRefer) continue;
            valid = pairedFacesCount == 2;
        }
    }

    void CalculateValues()
    {
        int colorA = faceColors[pairedFaces[0]];
        int colorB = faceColors[pairedFaces[1]];
        pairValues[0] = TableNumbers[colorB, colorA] % 10;
        pairValues[1] = TableNumbers[colorA, colorB] % 10;
    }

    void Log(string arg)
    {
        Debug.Log($"[Perspecticolored Cube #{ModuleId}] {arg}");
    }

    void Update()
    {
        if (ModuleSolved) return;
        if (!holdIndicated)
        {
            if (holding) holdTime += Time.deltaTime;
            if (holdTime >= 2)
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
                holdIndicated = true;
            }
        }

        var firstAngle = Vector3.Angle(transform.up, Camera.main.transform.up);
        var secondAngle = Vector3.Angle(transform.up, Camera.main.transform.right);
        var combinedAngleView = new Vector3(firstAngle, 0, secondAngle);
        DebugAngleText.text = string.Format("{0}\n{1}", displayedFace, combinedAngleView.ToString());
        DebugAngleText.color = faceAngles.Any(a => CheckEularAnglesWithinMarginOfError(a, combinedAngleView, 15f)) ? Color.green : Color.white;

        var idxesWithinMarginOfError = Enumerable.Range(1, faceAngles.Length).Where(a => CheckEularAnglesWithinMarginOfError(faceAngles[a - 1], combinedAngleView, 15f));
        // Get all idxes that match within a margin of error.
        if (idxesWithinMarginOfError.Count() > 1)
        {
            var errorMargins = idxesWithinMarginOfError.Select(a => GetErrorEularAngles(faceAngles[a - 1], combinedAngleView));
            displayedFace = idxesWithinMarginOfError.ElementAt(errorMargins.IndexOf(a => a <= errorMargins.Min())) - 1;
            // Get the idx that is closest to the desired direction if there are multiples.
        }
        else
            displayedFace = idxesWithinMarginOfError.SingleOrDefault() - 1;
        if (displayedFace == -1) displayedFace++;
        SetCubeColor(faceColors[displayedFace], displayedFace);
    }

    bool CheckEularAnglesWithinMarginOfError(Vector3 firstVector, Vector3 secondVector, float errorMargin = 1f)
    {
        var absDiffX = Mathf.Abs(firstVector.x - secondVector.x);
        var absDiffY = Mathf.Abs(firstVector.y - secondVector.y);
        var absDiffZ = Mathf.Abs(firstVector.z - secondVector.z);

        return absDiffX * absDiffX + absDiffY * absDiffY + absDiffZ * absDiffZ <= errorMargin * errorMargin;
    }
    float GetErrorEularAngles(Vector3 firstVector, Vector3 secondVector)
    {
        var absDiffX = Mathf.Abs(firstVector.x - secondVector.x);
        var absDiffY = Mathf.Abs(firstVector.y - secondVector.y);
        var absDiffZ = Mathf.Abs(firstVector.z - secondVector.z);

        return absDiffX * absDiffX + absDiffY * absDiffY + absDiffZ * absDiffZ;
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

    bool ContentsEqual(int[] a, int[] b, bool faces)
    {
        if (faces)
        {
            if (a[0] == 5) a[0] = a[1];
            if (a[1] == 5) a[1] = a[0];
            if (b[0] == 5) b[0] = b[1];
            if (b[1] == 5) b[1] = b[0];
        }
        return (a[0] == b[0] && a[1] == b[1]) || (a[0] == b[1] && a[1] == b[0]);
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} top/front/right/back/left (#)> or <!{0} t/f/r/b/l (#)> to tap the corresponding faces (or, optionally, hold them when the last digit of the timer is # for two seconds then release). This command isn't chainable. Use <!{0} cb> to toggle colorblind mode. Use <!{0} tilt u/r/d/l> to tilt the module around to look directly at the cube's faces (this is a general Twitch Plays command).";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
		var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        int commandHoldDigit = -1;
        int commandFace = -1;
        bool cb = false;
        if (commandArgs.Length == 1)
        {
            if (commandArgs[0] == "CB")
            {
                yield return null;
                ColorblindTextsParent.SetActive(!ColorblindTextsParent.activeInHierarchy);
                cb = true;
            }
        }
        if (!cb)
        {
            if (commandArgs.Length > 0)
            {
                switch (commandArgs[0])
                {
                    case "TOP":
                    case "T":
                        commandFace = 0;
                        break;
                    case "FRONT":
                    case "F":
                        commandFace = 1;
                        break;
                    case "RIGHT":
                    case "R":
                        commandFace = 2;
                        break;
                    case "BACK":
                    case "B":
                        commandFace = 3;
                        break;
                    case "LEFT":
                    case "L":
                        commandFace = 4;
                        break;
                    default:
                        yield return "sendtochatmessage Invalid face!";
                        break;
                }
                if (commandFace != -1)
                {
                    if (commandArgs.Length == 2)
                    {
                        int tryParse;
                        if (int.TryParse(commandArgs[1], out tryParse))
                        {
                            if (tryParse > -1 && tryParse < 10) commandHoldDigit = tryParse;
                            else
                            {
                                yield return "sendtochatmessage Invalid hold digit!";
                                yield break;
                            }
                        }
                        else
                        {
                            yield return "sendtochatmessage Invalid hold digit!";
                            yield break;
                        }
                    }
                    if (commandHoldDigit != -1)
                    {
                        while ((int)Bomb.GetTime() % 10 != commandHoldDigit) yield return null;
                        if ((int)Bomb.GetTime() % 10 == commandHoldDigit) Faces[commandFace].OnInteract();
                        else
                        {
                            while ((int)Bomb.GetTime() % 10 != commandHoldDigit) yield return null;
                            Faces[commandFace].OnInteract();
                        }
                        yield return new WaitUntil(() => holdIndicated);
                        Faces[commandFace].OnInteractEnded();
                    }
                    else
                    {
                        yield return null;
                        Faces[commandFace].OnInteractEnded();
                    }
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        inputsRecieved = 0;
        yield return null;
        for (int p = 0; p < 2; p++)
        {
            while ((int)Bomb.GetTime() % 10 != pairValues[p]) yield return null;
            Faces[pairedFaces[p]].OnInteract();
            yield return new WaitUntil(() => holdIndicated);
            Faces[pairedFaces[p]].OnInteractEnded();
        }
    }
}
