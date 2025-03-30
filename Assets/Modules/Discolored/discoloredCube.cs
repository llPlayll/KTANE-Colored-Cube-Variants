using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class discoloredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] List<KMSelectable> HorizFaces; // 0 - Left; 1 - Right
    [SerializeField] List<KMSelectable> VertFaces; // 0 - Back; 1 - Front;
    [SerializeField] TextMesh ColorblindText;
    [SerializeField] TextMesh IndexText;

    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan" };
    string[] TableStrips = new string[12] { "YBMGCR", "BCYMGR", "RMCGYB", "YBGMCR", "CYGMRB", "GCBYRM", "BGYCMR", "YMBRGC", "YGBRCM", "GCMRYB", "CMBRGY", "GMRCYB" };

    string[] strips = new string[6];
    int[] stripVals = new int[6];
    string validColors = "";

    int hFace, vFace;
    int stripIdx;
    int pos;
    int vertStreak;
    bool inSubmission = false;
    string submittedColors = "";

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        foreach (KMSelectable horiz in HorizFaces)
        {
            horiz.OnInteract += delegate () { HorizPress(); return false; };
        }
        foreach (KMSelectable vert in VertFaces)
        {
            vert.OnInteract += delegate () { VertPress(); return false; };
        }
        ColorblindText.gameObject.SetActive(Colorblind.ColorblindModeActive);
    }

    void HorizPress()
    {
        HorizFaces[hFace].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, HorizFaces[hFace].transform);

        vertStreak = 0;
        if (hFace == 0)
        {
            pos--;
            if (pos == -1) pos += strips[stripIdx].Length;
        }
        else
        {
            pos++;
            pos %= strips[stripIdx].Length;
        }
    }

    void VertPress()
    {
        VertFaces[vFace].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }

        vertStreak++;
        if (vertStreak == 6 && !inSubmission)
        {
            inSubmission = true;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, VertFaces[vFace].transform);

            for (int i = 0; i < 6; i++) strips[i] = TableStrips[stripVals[i] - 1];
            stripIdx = vFace == 0 ? 5 : 0;
            pos = 0;
            Log($"Entering submission mode. The strips have been reset. The new position is {ShortToFullName(strips[stripIdx][pos])} on strip {stripIdx + 1}");
            SetCube(strips[stripIdx][pos]);
        }
        else
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, VertFaces[vFace].transform);
            char transitionColor = strips[stripIdx][pos];
            if (!inSubmission) strips[stripIdx] += transitionColor;
            else submittedColors += transitionColor;

            int prevStripIdx = stripIdx;
            if (vFace == 0)
            {
                stripIdx--;
                if (stripIdx == -1) stripIdx += 6;
            }
            else
            {
                stripIdx++;
                stripIdx %= 6;
            }

            if (!inSubmission)
            {
                Log($"Navigated from strip {prevStripIdx + 1} to strip {stripIdx + 1} at position {pos + 1}: ");
                Log($"Appended {ShortToFullName(transitionColor)} to strip {prevStripIdx + 1}. Strip {prevStripIdx + 1} is now {strips[prevStripIdx]}. The cube is now {ShortToFullName(transitionColor)}.");
                SetCube(transitionColor);
            }
            else
            {
                SetCube(transitionColor);
                if (submittedColors.Length == 6)
                {
                    if (vFace == 0) submittedColors = submittedColors.Reverse().Join("");
                    if (submittedColors == validColors)
                    {
                        Log($"Correctly submitted all of the valid colors. Module solved!");
                        GetComponent<KMBombModule>().HandlePass();
                        ModuleSolved = true;
                        CubeRenderer.material.color = ColorList[1];
                        IndexText.text = "";
                        ColorblindText.text = "!";
                        ColorblindText.color = Color.white;
                        if (TwitchPlaysActive)
                        {
                            HorizFaces[hFace].transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                            VertFaces[vFace].transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                        }
                    }
                    else
                    {
                        Log($"Incorrectly submitted the colors {submittedColors.Select(x => ShortToFullName(x)).Join(", ")}. Strike! Exiting submission mode and regenerating the position...");
                        GetComponent<KMBombModule>().HandleStrike();

                        inSubmission = false;
                        vertStreak = 0;
                        submittedColors = "";

                        stripIdx = Rnd.Range(0, 6);
                        pos = Rnd.Range(0, 6);
                        SetCube(strips[stripIdx][pos]);
                        Log($"Starting position is {ShortToFullName(strips[stripIdx][pos])} on strip {stripIdx + 1}.");
                    }
                }
            }
        }
    }

    void SetCube(char c)
    {
        int color = ColorShortNames.IndexOf(x => x == c.ToString());
        CubeRenderer.material.color = ColorList[color];
        ColorblindText.text = ColorShortNames[color];
        IndexText.color = color == 6 ? Color.black : Color.white;
        ColorblindText.color = color == 6 ? Color.black : Color.white;
        IndexText.text = (stripIdx + 1).ToString();
    }

    void Start()
    {
        for (int i = 0; i < 6; i++)
        {
            stripVals[i] = Rnd.Range(1, 13);
            strips[i] = TableStrips[stripVals[i] - 1];
        }
        Log($"The values of the module's six strips are {stripVals.Join(", ")}. The strips themselves are:");
        for (int i = 0; i < 6; i++)
        {
            Log($"{i + 1}. - {strips[i]}");
            validColors += strips[i][stripVals[5 - i] % 6]; // This is what you call multitasking!
        }
        Log($"The valid colors for each strip, in order, are {validColors.Select(x => ShortToFullName(x)).Join(", ")}.");

        stripIdx = Rnd.Range(0, 6);
        pos = Rnd.Range(0, 6);
        SetCube(strips[stripIdx][pos]);
        Log($"Starting position is {ShortToFullName(strips[stripIdx][pos])} on strip {stripIdx + 1}.");

        hFace = Rnd.Range(0, 2);
        vFace = Rnd.Range(0, 2);
        HorizFaces[hFace ^ 1].gameObject.SetActive(false);
        VertFaces[vFace ^ 1].gameObject.SetActive(false);
        Log($"The selectable faces are the {(hFace == 0 ? "left" : "right")} and {(vFace == 0 ? "back" : "front")} faces.");
    }

    string ShortToFullName(char c)
    {
        return ColorFullNames[ColorShortNames.IndexOf(x => x == c.ToString())];
    }

    void Log(string arg)
    {
        Debug.Log($"[Discolored Cube #{ModuleId}] {arg}");
    }

    void Update()
    {
        if (ModuleSolved) return;
        HorizFaces[hFace].transform.GetChild(0).GetComponent<MeshRenderer>().enabled = TwitchPlaysActive;
        VertFaces[vFace].transform.GetChild(0).GetComponent<MeshRenderer>().enabled = TwitchPlaysActive;
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} l/b/r/f> to press the corresponding faces (you may also use <!{0} u/d> to press the back/front faces respectively). Chain presses without spaces.";
    private bool TwitchPlaysActive = false;
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (commandArgs.Length != 1) yield return "sendtochaterror Invalid command!";
        foreach (char f in commandArgs[0])
        {
            if (!"LBRFUD".Contains(f))
            {
                yield return "sendtochaterror Invalid command!";
                break;
            }
            else if (f == 'R' && hFace == 0)
            {
                yield return "sendtochaterror You are unable to interact with the right face!";
                break;
            }
            else if (f == 'L' && hFace == 1)
            {
                yield return "sendtochaterror You are unable to interact with the left face!";
                break;
            }
            else if ("DF".Contains(f) && vFace == 0)
            {
                yield return "sendtochaterror You are unable to interact with the front face!";
                break;
            }
            else if ("UB".Contains(f) && vFace == 1)
            {
                yield return "sendtochaterror You are unable to interact with the back face!";
                break;
            }
        }
        yield return null;
        foreach (char f in commandArgs[0])
        {
            switch (f)
            {
                case 'L':
                    HorizFaces[0].OnInteract();
                    break;
                case 'R':
                    HorizFaces[1].OnInteract();
                    break;
                case 'U':
                case 'B':
                    VertFaces[0].OnInteract();
                    break;
                case 'D':
                case 'F':
                    VertFaces[1].OnInteract();
                    break;
                default:
                    break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        if (inSubmission && submittedColors.Length > 0) // Just to be safe, let's reset submission if we're already in it
        {
            submittedColors = "";
            stripIdx = vFace == 0 ? 5 : 0;
            pos = 0;
            SetCube(strips[stripIdx][pos]);
        }
        while (!inSubmission)
        {
            VertFaces[vFace].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        while (!ModuleSolved)
        {
            while (strips[stripIdx][pos] != validColors[stripIdx])
            {
                HorizFaces[hFace].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            VertFaces[vFace].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
