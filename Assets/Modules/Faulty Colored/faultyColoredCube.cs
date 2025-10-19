using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using Words;

public class faultyColoredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] KMSelectable CubeSelectable;
    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] TextMesh ColorblindText;
    [SerializeField] TextMesh IndexText;

    string decryptedWord;
    int[] readColors = new int[6];
    int[] readNumbers = new int[6];
    int[] submissionColors = new int[6];
    int[] submissionNumbers = new int[6];
    int[] expectedInput = new int[6];

    string monoKey;
    string encryptedWord;

    int flashIdx = 0;
    int[] submissionInput = new int[6];
    bool inSubmission;
    bool firstSubmission = true;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        CubeSelectable.OnInteract += delegate () { CubePress(); return false; };
        ColorblindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
    }

    void CubePress()
    {
        CubeSelectable.AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CubeSelectable.transform);

        if (!inSubmission)
        {
            inSubmission = true;
            SetCube();
        }
        else
        {
            if (flashIdx < 6)
            {
                submissionInput[flashIdx] += 1;
                submissionInput[flashIdx] %= 7;
                SetCube();
            }
            else
            {
                if (firstSubmission)
                {
                    LogSplit();
                    Log("Submissions:");
                    firstSubmission = false;
                }
                bool submissionCorrect = true;
                for (int i = 0; i < 6; i++)
                {
                    if (submissionInput[i] != expectedInput[i])
                    {
                        submissionCorrect = false;
                        break;
                    }
                }
                Log($"Submitted {submissionInput.Select(x => ColorFullNames[x]).Join(", ")}, which is {(submissionCorrect ? "correct. Module Solved!" : "incorrect. Strike!")}");
                if (submissionCorrect)
                {
                    StopCoroutine("FlashSequence");
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    CubeRenderer.material.color = ColorList[1];
                    IndexText.text = "";
                    ColorblindText.text = "!";
                    ColorblindText.color = Color.white;
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                    inSubmission = false;
                    SetCube();
                }
            }
        }
    }

    void SetCube()
    {
        if (flashIdx < 6)
        {
            IndexText.text = (inSubmission ? submissionNumbers : readNumbers)[flashIdx].ToString();
            int color = (inSubmission ? submissionInput : readColors)[flashIdx];
            CubeRenderer.material.color = ColorList[color];
            ColorblindText.text = ColorShortNames[color];
            IndexText.color = color == 6 ? Color.black : Color.white;
            ColorblindText.color = color == 6 ? Color.black : Color.white;
        }
        else
        {
            IndexText.text = "0";
            CubeRenderer.material.color = ColorList[7];
            ColorblindText.text = ColorShortNames[7];
            IndexText.color = Color.white;
            ColorblindText.color = Color.white;
        }
    }

    void Start()
    {
        decryptedWord = new Data().GenWord();
        Log($"The Decrypted Word is \"{decryptedWord}\".");
        GenerateModes();
        ObtainKey();
        EncryptMono();
        EncodeSubmission();
        StartCoroutine("FlashSequence");
    }

    void GenerateModes()
    {
        for (int i = 0; i < 6; i++)
        {
            readNumbers[i] = Rnd.Range(3, 11);
            readColors[i] = Rnd.Range(0, 7);
            while (readColors[i] == 1 && readNumbers[i] != 5 && readNumbers[i] != 7 && readNumbers[i] != 8) readColors[i] = Rnd.Range(0, 7);

            int subColor = Rnd.Range(0, 7);
            while (submissionColors.Contains(subColor)) subColor = Rnd.Range(0, 7);
            submissionColors[i] = subColor;
            submissionInput[i] = subColor;
        }
    }

    void ObtainKey()
    {
        LogSplit();
        Log($"Obtaining Monoalphabetic Cipher’s Key:");

        string[] readFlashes = new string[6];
        for (int i = 0; i < 6; i++) readFlashes[i] = $"{ColorFullNames[readColors[i]]}-{readNumbers[i]}";
        Log($"Flashes: {readFlashes.Join(", ")}");

        monoKey = (ColorFullNames[readColors[0]].ToUpperInvariant() + Alphabet).Distinct().Join("");
        Log($"Initial Alphabet Key: {monoKey}");

        for (int i = 0; i < 6; i++)
        {
            int c = readColors[i];
            int n = readNumbers[i];

            while (monoKey.Length % n != 0) monoKey += "#";
            string[] initGroups = GroupsOf(monoKey, n);
            string[] modifiedGroups = ModifyGroups(initGroups, c);
            monoKey = modifiedGroups.Join("").Replace("#", string.Empty);

            Log($"{readFlashes[i]}:\n{initGroups.Join()} →\n{modifiedGroups.Join()} →\n{(i < 5 ? "New" : "Final")} Alphabet Key: {monoKey}");
        }
    }

    string[] GroupsOf(string s, int n)
    {
        int groupCount = s.Length / n;
        string[] groups = new string[groupCount];
        for (int i = 0; i < groupCount; i++) groups[i] = s.Substring(n * i, n);
        return groups;
    }

    string[] ModifyGroups(string[] groups, int c)
    {
        int groupCount = groups.Count();
        string[] newGroups = new string[groupCount];
        switch (c)
        {
            case 0: // Red
                for (int i = 0; i < groupCount; i++) newGroups[i] = groups[i][groups[i].Length - 1] + groups[i].Substring(0, groups[i].Length - 1);
                break;
            case 1: // Green
                for (int i = 0; i < groupCount; i += 2)
                {
                    newGroups[i] = groups[i + 1];
                    newGroups[i + 1] = groups[i];
                }
                break;
            case 2: // Blue
                newGroups[0] = groups[groupCount - 1];
                for (int i = 1; i < groupCount; i++) newGroups[i] = groups[i - 1];
                break;
            case 3: // Yellow
                for (int i = 0; i < groupCount; i++) newGroups[i] = groups[groupCount - 1 - i];
                break;
            case 4: // Magenta
                newGroups[groupCount - 1] = groups[0];
                for (int i = 1; i < groupCount; i++) newGroups[i - 1] = groups[i];
                break;
            case 5: // Cyan
                for (int i = 0; i < groupCount; i++) newGroups[i] = groups[i].Substring(1) + groups[i][0];
                break;
            case 6: // White
                for (int i = 0; i < groupCount; i++) newGroups[i] = groups[i].ToCharArray().Reverse().Join("");
                break;
            default:
                break;
        }
        return newGroups;
    }

    void EncryptMono()
    {
        LogSplit();
        encryptedWord = decryptedWord.Select(x => monoKey[Alphabet.IndexOf(x)]).Join("");

        string[] logPairs = new string[6];
        for (int i = 0; i < 6; i++)
        {
            logPairs[i] = $"{decryptedWord[i]} → {encryptedWord[i]}";
            submissionNumbers[i] = Alphabet.IndexOf(encryptedWord[i]) + 1;
        }

        Log($"Encrypting the Monoalphabetic Cipher:");
        Log($"Alphabet Key: {monoKey}\n{monoKey}\n{Alphabet}");
        Log($"Decrypted Word: {decryptedWord}\n{logPairs.Join("\n")}");
        Log($"Encrypted Word: {encryptedWord}");
    }

    void EncodeSubmission()
    {
        LogSplit();
        int[] colorColumns = new int[7];
        for (int i = 0; i < 6; i++) colorColumns[i] = submissionColors[i];
        for (int i = 0; i < 7; i++)
        {
            if (!colorColumns.Contains(i))
            {
                colorColumns[6] = i;
                break;
            }
        }
        for (int i = 0; i < 6; i++) expectedInput[i] = colorColumns[Alphabet.IndexOf(decryptedWord[i]) % 7];

        string[] logPairs = new string[6];
        for (int i = 0; i < 6; i++)
        {
            logPairs[i] = $"{decryptedWord[i]} → {ColorFullNames[expectedInput[i]]}";
        }

        Log($"Submission Mode Flashes’ Colors: {submissionColors.Select(x => ColorFullNames[x]).Join(", ")}\nMissing Color: {ColorFullNames[colorColumns[6]]}\n" +
            $"{colorColumns.Select(x => ColorShortNames[x]).Join("")}\nABCDEFG\nHIJKLMN\nOPQRSTU\nVWXYZ");
        Log($"Decrypted Word: {decryptedWord}\n{logPairs.Join("\n")}");
        Log($"Encoded Decrypted Word: {expectedInput.Select(x => ColorFullNames[x]).Join(", ")}");
    }

    void Log(string arg)
    {
        Debug.Log($"[Faulty Colored Cube #{ModuleId}] {arg}");
    }

    void LogSplit()
    {
        Log("----------------------------------------------");
    }

    IEnumerator FlashSequence()
    {
        while (!ModuleSolved)
        {
            SetCube();
            yield return new WaitForSeconds(2);
            flashIdx++;
            flashIdx %= 7;
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} press> to press the cube at any time (only in Read Mode). Use <!{0} submit K yellow r Y Green b> to submit a sequence of colors (in this example - Black, Yellow, Red, Yellow, Green, Blue) into the module. (only in Submission Mode). Use <!{0} cb> to toggle colorblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
		var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        bool cbCmd = false;
        if (commandArgs.Length == 1)
        {
            if (commandArgs[0] == "CB")
            {
                yield return null;
                ColorblindText.gameObject.SetActive(!ColorblindText.gameObject.activeInHierarchy);
                cbCmd = true;
            }
        }
        if (!cbCmd)
        {
            if (!inSubmission && commandArgs.Length != 1) yield return "sendtochaterror Invalid command!";
            if (!inSubmission && commandArgs[0] != "PRESS") yield return "sendtochaterror Invalid command!";
            if (inSubmission && commandArgs.Length != 7) yield return "sendtochaterror Invalid command!";
            if (inSubmission && commandArgs[0] != "SUBMIT") yield return "sendtochaterror Invalid command!";

            if (!inSubmission)
            {
                yield return null;
                CubeSelectable.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                int[] tpInput = new int[6];
                bool invalid = false;
                for (int i = 0; i < 6; i++)
                {
                    switch (commandArgs[i + 1])
                    {
                        case "R":
                        case "RED":
                            tpInput[i] = 0;
                            break;
                        case "G":
                        case "GREEN":
                            tpInput[i] = 1;
                            break;
                        case "B":
                        case "BLUE":
                            tpInput[i] = 2;
                            break;
                        case "Y":
                        case "YELLOW":
                            tpInput[i] = 3;
                            break;
                        case "M":
                        case "MAGENTA":
                            tpInput[i] = 4;
                            break;
                        case "C":
                        case "CYAN":
                            tpInput[i] = 5;
                            break;
                        case "W":
                        case "WHITE":
                            tpInput[i] = 6;
                            break;
                        default:
                            invalid = true;
                            break;
                    }
                    if (invalid) break;
                }
                if (invalid) yield return "sendtochaterror Invalid command!";
                else
                {
                    yield return new WaitUntil(() => flashIdx == 6);
                    for (int i = 0; i < 6; i++)
                    {
                        yield return new WaitUntil(() => flashIdx == i);
                        while (submissionInput[i] != tpInput[i])
                        {
                            CubeSelectable.OnInteract();
                            yield return new WaitForSeconds(0.05f);
                        }
                    }
                    yield return new WaitUntil(() => flashIdx == 6);
                    CubeSelectable.OnInteract();
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        if (!inSubmission) CubeSelectable.OnInteract();
        yield return new WaitUntil(() => flashIdx == 6);
        for (int i = 0; i < 6; i++)
        {
            yield return new WaitUntil(() => flashIdx == i);
            while (submissionInput[i] != expectedInput[i])
            {
                CubeSelectable.OnInteract();
                yield return new WaitForSeconds(0.05f);
            }
        }
        yield return new WaitUntil(() => flashIdx == 6);
        CubeSelectable.OnInteract();
    }
}
