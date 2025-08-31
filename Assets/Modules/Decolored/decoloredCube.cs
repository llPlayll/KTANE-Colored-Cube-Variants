using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class decoloredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] GameObject FaceAnchor;
    [SerializeField] KMSelectable Face;
    [SerializeField] TextMesh ColorblindText;
    [SerializeField] TextMesh IndexText;

    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };
    string[] FaceNames = { "back", "right", "front", "left" };
    string grid = "RRMWBCWGKYYBRCGWYMCKWWGRCKMYGBCWKGYWRMBWCMYKWGKBMRBYWBRMGCYKMCBYGRKWCCWGRMMYBKCMGWCMKGCYRBKKMYCBBRWG"; // 😋

    int startPos, pos;
    bool shouldHold;

    bool activated;
    int targetColor;
    int startDir, dir;
    bool holding;
    float holdTime;
    bool TPAutosolved;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        GetComponent<KMBombModule>().OnActivate += delegate () { OnActivate(); };
        Face.OnInteract += delegate () { holding = true; return false; };
        Face.OnInteractEnded += delegate () { FaceRelease(); };
        ColorblindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
    }

    void FaceRelease()
    {
        Face.AddInteractionPunch();
        if (ModuleSolved || !activated)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Face.transform);
        holding = false;
        
        bool struck = false;
        if (holdTime >= 1)
        {
            if (shouldHold)
            {
                dir++;
                dir %= 4;
                StartCoroutine(RotateFace());
                Log($"Held the face while on a {LogPosColor()} cell ({LogPos()}), which matches the cube's color. The selectable face rotated to the {FaceNames[dir]} face.");
                shouldHold = false;
            }
            else
            {
                struck = true;
                Log($"Held the face while on a {LogPosColor()} cell ({LogPos()}), which doesn't match the cube's color. Strike! Resetting back to starting position/direction.");
            }
        }
        else
        {
            if (shouldHold)
            {
                
                struck = true;
                Log($"Didn't hold the face while on a {LogPosColor()} cell ({LogPos()}), which matches the cube's color. Strike! Resetting back to starting position/direction.");
            }
            else
            {
                switch (dir)
                {
                    case 0: // Back/Up
                        pos -= 10;
                        if (pos < 0) pos += 100;
                        break;
                    case 1: // Right
                        pos++;
                        if (pos % 10 == 0) pos -= 10;
                        break;
                    case 2: // Front/Down
                        pos += 10;
                        if (pos > 99) pos -= 100;
                        break;
                    case 3: // Left
                        pos--;
                        if ((pos + 10) % 10 == 9) pos += 10;
                        break;
                    default:
                        break;
                }
                if (pos == startPos)
                {
                    Log("Navigated back to the starting position. Module solved!");
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                    CubeRenderer.material.color = ColorList[1];
                    IndexText.text = "";
                    ColorblindText.text = "!";
                    ColorblindText.color = Color.white;
                    if (TwitchPlaysActive) Face.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                }
                shouldHold = grid[pos].ToString() == ColorShortNames[targetColor];
            }
        }

        holdTime = 0;
        if (struck)
        {
            GetComponent<KMBombModule>().HandleStrike();
            shouldHold = false;
            pos = startPos;
            dir = startDir;
            FaceAnchor.transform.localRotation = new Quaternion(0, 0, 0, 0);
            FaceAnchor.transform.Rotate(new Vector3(0, dir * 90, 0));
        }
    }

    void SetCubeColor(bool log)
    {
        if (log) Log($"The cube is colored {ColorFullNames[targetColor]}.");
        CubeRenderer.material.color = ColorList[targetColor];
        ColorblindText.text = ColorShortNames[targetColor];
        IndexText.color = targetColor == 6 ? Color.black : Color.white;
        ColorblindText.color = targetColor == 6 ? Color.black : Color.white;
    }

    void Start()
    {
        IndexText.text = Rnd.Range(0, 10).ToString();
        targetColor = Rnd.Range(0, 8);
        SetCubeColor(false);

        startDir = Rnd.Range(0, 4);
        dir = startDir;
        FaceAnchor.transform.Rotate(new Vector3(0, dir * 90, 0));
    }

    void OnActivate()
    {
        activated = true;

        startPos = (Bomb.GetSolvableModuleNames().Count * ((int)Bomb.GetTime() / 60 + 1)) % 100;
        pos = startPos;
        while (ColorShortNames[targetColor] == grid[startPos].ToString())
        {
            targetColor = Rnd.Range(0, 8);
        }
        SetCubeColor(true);

        Log($"The selectable face is the {FaceNames[dir]} face.");
        Log($"Starting position is {LogPos()} in the grid.");
    }

    void Update()
    {
        if (ModuleSolved) return;
        Face.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = TwitchPlaysActive;
        if (holdTime <= 1)
        {
            if (holding) holdTime += Time.deltaTime;
            if (holdTime >= 1)
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, Face.transform);
            }
        }
    }

    string LogPos()
    {
        return "ABCDEFGHIJ"[pos % 10] + ((int)pos / 10 + 1).ToString();
    }

    string LogPosColor()
    {
        return ColorFullNames[ColorShortNames.IndexOf(x => x == grid[pos].ToString())];
    }

    void Log(string arg)
    {
        Debug.Log($"[Decolored Cube #{ModuleId}] {arg}");
    }

    IEnumerator RotateFace()
    {
        for (int i = 0; i < 5; i++)
        {
            FaceAnchor.transform.Rotate(new Vector3(0, 18, 0));
            yield return null;
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} f/face (#)> to quickly tap the selectable face, optionally, # times. Use <!{0} h/hold> to hold the selectable face for 1 second and then release it. Use <!{0} cb> to toggle colorblind mode.";
    private bool TwitchPlaysActive = false;
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

        int times = 1;
        if (commandArgs.Length < 1 || commandArgs.Length > 2) yield return "sendtochaterror Invalid command!";
        else if (!new List<string> { "F", "FACE", "H", "HOLD", "CB" }.Contains(commandArgs[0])) yield return "sendtochaterror Invalid command!";
        else if ((commandArgs[0] == "H" || commandArgs[0] == "HOLD" || commandArgs[0] == "CB") & commandArgs.Length == 2) yield return "sendtochaterror Invalid command!";
        else if (commandArgs.Length == 2)
        {
            if (!int.TryParse(commandArgs[1], out times)) yield return "sendtochaterror Invalid number of times!";
            else if (times < 1) yield return "sendtochaterror Invalid number of times!";
        }
        yield return null;
        switch (commandArgs[0])
        {
            case "F":
            case "FACE":
                for (int i = 0; i < times; i++)
                {
                    Face.OnInteractEnded();
                    yield return new WaitForSeconds(0.1f);
                }
                break;
            case "H":
            case "HOLD":
                Face.OnInteract();
                while (holdTime < 1) yield return null;
                Face.OnInteractEnded();
                yield return new WaitForSeconds(0.5f);
                break;
            case "CB":
                yield return null;
                ColorblindText.gameObject.SetActive(!ColorblindText.gameObject.activeInHierarchy);
                yield return new WaitForSeconds(0.5f);
                break;
            default:
                break;
        }

        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        TPAutosolved = true;
        while (!ModuleSolved)
        {
            if (shouldHold)
            {
                Face.OnInteract();
                while (holdTime < 1) yield return null;
                Face.OnInteractEnded();
            }
            else
            {
                Face.OnInteractEnded();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
}
