using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class notColoredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;

    [SerializeField] KMSelectable CubeSelectable;
    [SerializeField] MeshRenderer CubeRenderer;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    Color[] colorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] colorNamesList = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] colorFullNamesList = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };

    int curStage = 1;
    int curColor = -1;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        CubeSelectable.OnInteract += delegate () { CubePress(); return false; };
    }

    void CubePress()
    {

    }

    void Start()
    {

    }

    void Log(string arg)
    {
        Debug.Log($"[Not Colored Cube #{ModuleId}] {arg}");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} to do something.";
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
