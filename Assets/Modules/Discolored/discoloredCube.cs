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
    [SerializeField] List<KMSelectable> VertFaces; // 0 - Front; 1 - Back;
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
    bool startedSubmission;
    int submissionPos, submittedColors = -1;

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
        SetCube(false);
    }

    void VertPress()
    {
        VertFaces[vFace].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, VertFaces[vFace].transform);
        if (hFace == 0) strips[stripIdx] = strips[stripIdx][pos] + strips[stripIdx];
        else strips[stripIdx] = strips[stripIdx] + strips[stripIdx][pos];

        if ((vFace == 0 & stripIdx == 5) || (vFace == 1 & stripIdx == 0))
        {
            startedSubmission = true;
            submissionPos = pos;
            submittedColors = 1;
        }
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

        if (startedSubmission && submissionPos == pos)
        {
            submittedColors++;
            if (submittedColors == 7) CheckSolution();
        }
        else SetCube(false);
    }
    
    void CheckSolution()
    {
        bool correct = true;
        for (int i = 0; i < 6; i++) if (strips[i][submissionPos] != validColors[i]) correct = false;
        if (correct)
        {
            Log($"Correctly submitted position {submissionPos + 1}. Module solved!");
            GetComponent<KMBombModule>().HandlePass();
            ModuleSolved = true;
            CubeRenderer.material.color = ColorList[1];
            IndexText.text = "";
            ColorblindText.text = "!";
            ColorblindText.color = Color.white;
        }
        else
        {
            Log($"Incorrectly submitted position {submissionPos + 1}. Strike!");
            GetComponent<KMBombModule>().HandleStrike();
            SetCube(false);
        }
    }

    void SetCube(bool start)
    {
        int color = ColorShortNames.IndexOf(x => x == strips[stripIdx][pos].ToString());
        if (start) Log($"Starting position is {ColorFullNames[color]} on strip {stripIdx + 1}.");
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
        for (int i = 0; i < 6; i++) Log($"{i + 1}. - {strips[i]}");
        stripIdx = Rnd.Range(0, 6);
        pos = Rnd.Range(0, 6);
        SetCube(true);

        for (int i = 0; i < 6; i++) validColors += strips[i][stripVals[5 - i] % 6];
        Log($"The valid colors for each strip, in order, are {validColors.Select(x => ColorFullNames[ColorShortNames.IndexOf(c => c == x.ToString())]).Join(", ")}.");

        hFace = Rnd.Range(0, 2);
        vFace = Rnd.Range(0, 2);
        HorizFaces[hFace ^ 1].gameObject.SetActive(false);
        VertFaces[vFace ^ 1].gameObject.SetActive(false);
        Log($"The selectable faces are the {(hFace == 0 ? "left" : "right")} and {(vFace == 0 ? "front" : "back")} faces.");
    }

    void Log(string arg)
    {
        Debug.Log($"[Discolored Cube #{ModuleId}] {arg}");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0}> to do something.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
    }
}
