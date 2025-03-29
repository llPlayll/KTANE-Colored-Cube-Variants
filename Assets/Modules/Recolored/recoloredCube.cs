using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class recoloredCube : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] GameObject CubeletsParent;
    List<KMSelectable> CubeletsSelectables = new List<KMSelectable>();
    List<MeshRenderer> CubeletsRenderers = new List<MeshRenderer>();
    [SerializeField] TextMesh MainColorblindText;
    [SerializeField] TextMesh IndexText;

    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    Color[] HLColorList = { new Color(0.75f, 0, 0), new Color(0, 0.75f, 0), new Color(0, 0, 0.75f), new Color(0.75f, 0.75f, 0), new Color(0.75f, 0, 0.75f), new Color(0, 0.75f, 0.75f), new Color(0.75f, 0.75f, 0.75f), new Color(0.25f, 0.25f, 0.25f) };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };
    string[] ColorBinaries = { "100", "010", "001", "110", "101", "011", "111", "000" };

    int targetColor;
    string[] cubeletBinaries = new string[27];
    string[] initCubeletBinaries = new string[27];
    List<int> toggledCubelets = new List<int> { };
    List<string> toggledCubeletsChannels = new List<string> { };

    int ToggledCubeletsCount;
    int curHL;
    bool moduleStarted;
    bool resetOnNextPress;

    bool ColorblindActive;
    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    bool ZenModeActive;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        ColorblindActive = Colorblind.ColorblindModeActive;
        foreach (Transform cubelet in CubeletsParent.transform)
        {
            CubeletsSelectables.Add(cubelet.gameObject.GetComponent<KMSelectable>());
            CubeletsRenderers.Add(cubelet.gameObject.GetComponent<MeshRenderer>());
        } 
        foreach (KMSelectable cubelet in CubeletsSelectables) {
            int idx = CubeletsSelectables.IndexOf(cubelet);
            cubelet.OnHighlight += delegate () { OnCubeletHL(idx); };
            cubelet.OnHighlightEnded += delegate () { OnCubeletHLEnded(idx); ; };
            cubelet.OnInteract += delegate () { OnCubeletPress(idx); return false; };
        }
        MainColorblindText.gameObject.SetActive(ColorblindActive);
    }

    void OnCubeletHL(int c)
    {
        if (curHL == c)
        {
            return;
        }
        curHL = c;
        CubeletsSelectables[c].gameObject.transform.localScale *= 1.1f;
        if (ModuleSolved)
        {
            return;
        }
        string cColorBinary = cubeletBinaries[c];
        CubeletsRenderers[c].material.color = HLColorList[ColorBinaries.IndexOf(x => x == cColorBinary)];
        
    }

    void OnCubeletHLEnded(int c)
    {
        CubeletsSelectables[c].gameObject.transform.localScale = new Vector3(0.333f, 0.333f, 0.333f);
        if (ModuleSolved)
        {
            return;
        }
        string cColorBinary = cubeletBinaries[c];
        CubeletsRenderers[c].material.color = ColorList[ColorBinaries.IndexOf(x => x == cColorBinary)];
        curHL = -1;
    }

    void OnCubeletPress(int c)
    {
        CubeletsSelectables[c].AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        if (!moduleStarted)
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CubeletsSelectables[c].transform);
            StartModule();
        }
        else
        {
            int lastDigit = (int)Bomb.GetTime() % 10;
            if (lastDigit == 0)
            {
                if (!resetOnNextPress)
                {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CubeletsSelectables[c].transform);
                    resetOnNextPress = true;
                }
                else
                {
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, CubeletsSelectables[c].transform);
                    cubeletBinaries = initCubeletBinaries.Select(x => x).ToArray();
                    SetCubeletColors();
                }
            }
            else
            {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, CubeletsSelectables[c].transform);
                resetOnNextPress = false;
                ToggleCubelet(c, (lastDigit - 1) / 3);
                CheckSolution();
            }
        }
    }

    void Start()
    {
        ToggledCubeletsCount = Rnd.Range(6, 9);
        IndexText.text = ToggledCubeletsCount.ToString();
        targetColor = Rnd.Range(0, 8);
        Log($"The target color is {ColorFullNames[targetColor]}.");
        MainColorblindText.text = ColorShortNames[targetColor];
        IndexText.color = targetColor == 6 ? Color.black : Color.white;
        MainColorblindText.color = targetColor == 6 ? Color.black : Color.white;
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    string newBinary = "";
                    if (targetColor == 0 || targetColor == 3 || targetColor == 4 || targetColor == 6) newBinary += "1"; else newBinary += "0";
                    if (targetColor == 1 || targetColor == 3 || targetColor == 5 || targetColor == 6) newBinary += "1"; else newBinary += "0";
                    if (targetColor == 2 || targetColor == 4 || targetColor == 5 || targetColor == 6) newBinary += "1"; else newBinary += "0";
                    cubeletBinaries[i * 9 + j * 3 + k] = newBinary;
                }
            }
        }
        SetCubeletColors();
    }

    void StartModule()
    {
        MainColorblindText.gameObject.SetActive(false);
        IndexText.gameObject.SetActive(false);
        for (int i = 0; i < ToggledCubeletsCount; i++)
        {
            int cubelet = Rnd.Range(0, 27);
            while (toggledCubelets.Contains(cubelet) || cubelet == 13 || cubelet == 22) cubelet = Rnd.Range(0, 27); // The cubelets in the center of the cube (#13) and in the center of the bottom face (#22) cannot be toggled by the defuser
            string toggledChannels = ColorBinaries[Rnd.Range(0, 7)];
            for (int v = 0; v < 3; v++)
            {
                if (toggledChannels[v] == '1') ToggleCubelet(cubelet, v);
            }
            toggledCubelets.Add(cubelet);
            toggledCubeletsChannels.Add(toggledChannels);
        }
        Log($"The {ToggledCubeletsCount} following cubelets were toggled (layer-column-row): {toggledCubelets.Select(x => LogToggledCubelet(x)).Join(", ")}.");
        initCubeletBinaries = cubeletBinaries.Select(x => x).ToArray();
        moduleStarted = true;
        SetCubeletColors();
    }

    void CheckSolution()
    {
        bool flag = true;
        for (int i = 0; i < 27; i++)
        {
            if (i == 13 || i == 22) continue;
            else if (cubeletBinaries[i] != ColorBinaries[targetColor])
            {
                flag = false;
                break;
            }
        }
        if (flag)
        {
            Log($"All of the cubelets are now the target color ({ColorFullNames[targetColor]}), module solved!");
            GetComponent<KMBombModule>().HandlePass();
            ModuleSolved = true;
            for (int c = 0; c < 27; c++)
            {
                CubeletsRenderers[c].material.color = ColorList[1];
                foreach (Transform ct in CubeletsSelectables[c].transform.Find("Colorblind Texts"))
                {
                    ct.GetComponent<TextMesh>().text = "";
                }
            }
            MainColorblindText.gameObject.SetActive(ColorblindActive);
            MainColorblindText.text = "!";
            MainColorblindText.color = Color.white;
        }
    }

    void ToggleCubelet(int c, int toggle)
    {
        ToggleMainColor(c, toggle); // Itself
        if (c < 18) ToggleMainColor(c + 9, toggle); // Layer below
        if (c > 8) ToggleMainColor(c - 9, toggle); // Layer above
        if ((c % 9) < 6) ToggleMainColor(c + 3, toggle); // Below (on the face)
        if ((c % 9) > 2) ToggleMainColor(c - 3, toggle); // Above (on the face)
        if (c % 3 < 2) ToggleMainColor(c + 1, toggle); // To the right (on the face)
        if (c % 3 > 0) ToggleMainColor(c - 1, toggle); // To the left (on the face)
        SetCubeletColors();
    }

    void SetCubeletColors()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                for (int k = 0; k < 3; k++)
                {
                    int c = i * 9 + j * 3 + k;
                    string cColorBinary = cubeletBinaries[i * 9 + j * 3 + k];
                    CubeletsRenderers[c].material.color = ColorList[ColorBinaries.IndexOf(x => x == cColorBinary)];
                    if (ColorblindActive && moduleStarted)
                    {
                        string cColorblind = ColorShortNames[ColorBinaries.IndexOf(x => x == cColorBinary)];
                        CubeletsSelectables[c].transform.Find("Colorblind Texts").gameObject.SetActive(true);
                        foreach (Transform ct in CubeletsSelectables[c].transform.Find("Colorblind Texts"))
                        {
                            ct.GetComponent<TextMesh>().text = cColorblind;
                            ct.GetComponent<TextMesh>().color = cColorblind == "W" ? Color.black : Color.white;
                        }
                    }
                }
            }
        }
    }

    void ToggleMainColor(int c, int toggle)
    {
        cubeletBinaries[c] = ToggleColor(cubeletBinaries[c], toggle);
    }

    string ToggleColor(string color, int toggle)
    {
        string toggledColor = "";
        if (toggle == 0) toggledColor = (color[0] == '0' ? '1' : '0').ToString() + color[1].ToString() + color[2].ToString();
        else if (toggle == 1) toggledColor = color[0].ToString() + (color[1] == '0' ? '1' : '0').ToString() + color[2].ToString();
        else toggledColor = color[0].ToString() + color[1].ToString() + (color[2] == '0' ? '1' : '0').ToString();
        return toggledColor;
    }

    string LogToggledCubelet(int c)
    {
        return $"{LogCubeletPos(c)} ({LogToggledChannels(toggledCubeletsChannels[toggledCubelets.IndexOf(c)])})";
    }

    string LogCubeletPos(int c)
    {
        return new List<string> { "Top-", "Middle-", "Bottom-" }[c / 9].ToString() + "ABC"[c % 3].ToString() + ((c % 9) / 3 + 1).ToString();
    }

    string LogToggledChannels(string ch)
    {
        List<string> toggled = new List<string> { };
        if (ch[0] == '1') toggled.Add("R");
        if (ch[1] == '1') toggled.Add("G");
        if (ch[2] == '1') toggled.Add("B");
        return toggled.Join(", ");
    }

    void Log(string arg)
    {
        Debug.Log($"[Recolored Cube #{ModuleId}] {arg}");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} start> to start the module. Use <!{0} RTA1 GMB3 BBC2> to toggle the Red channel at A1 on the Top layer, the Green channel at B3 on the Middle layer, and the Blue channel at C2 on the Bottom layer (i.e. specify presses using the channel-layer-coordinate format). Use <!{0} reset> to reset the module. Use <!{0} cb> to toggle colorblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (commandArgs.Length == 0) yield return "sendtochaterror Invalid command!";
        else if (commandArgs[0] == "START")
        {
            if (moduleStarted) yield return "sendtochaterror Module's already started!";
            else
            {
                yield return null;
                OnCubeletPress(0);
                yield return new WaitForSeconds(0.5f);
            }
        }
        else if (commandArgs[0] == "CB")
        {
            yield return null;
            ColorblindActive = !ColorblindActive;
            SetCubeletColors();
            yield return new WaitForSeconds(0.5f);
        }
        else if (commandArgs[0] == "RESET")
        {
            while ((int)Bomb.GetTime() % 10 != 0)
            {
                yield return null;
            }
            OnCubeletPress(0);
            yield return new WaitForSeconds(0.1f);
            OnCubeletPress(0);
            yield return new WaitForSeconds(0.5f);
        }
        else
        {
            foreach (string arg in commandArgs)
            {
                if (arg.Length != 4) yield return "sendtochaterror Invalid press!";
                else
                {
                    if (!"RGB".Contains(arg[0]) || !"TMB".Contains(arg[1]) || !"ABC".Contains(arg[2]) || !"123".Contains(arg[3])) yield return "sendtochaterror Invalid press!";
                    else if (arg[1] == 'M' && arg[2] == 'B' && arg[3] == '2') yield return "sendtochaterror You cannot toggle the center of the cube!";
                    else if (arg[1] == 'B' && arg[2] == 'B' && arg[3] == '2') yield return "sendtochaterror You cannot toggle the center of the cube's bottom face!";
                }
            }
            foreach (string arg in commandArgs)
            {
                int pressCubelet = "TMB".IndexOf(arg[1]) * 9 + "123".IndexOf(arg[3]) * 3 + "ABC".IndexOf(arg[2]);
                List<int> acceptableLD = arg[0] == 'R' ? new List<int> { 1, 2, 3 } : (arg[0] == 'G' ? new List<int> { 4, 5, 6 } : new List<int> { 7, 8, 9 });
                while (!acceptableLD.Contains((int)Bomb.GetTime() % 10))
                {
                    yield return null;
                }
                OnCubeletPress(pressCubelet);
                yield return new WaitForSeconds(0.1f);
            }
        }
        yield return null;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!moduleStarted)
        {
            yield return null;
            OnCubeletPress(0);
        }
        else
        {
            while ((int)Bomb.GetTime() % 10 != 0)
            {
                yield return null;
            }
            OnCubeletPress(0);
            yield return new WaitForSeconds(0.1f);
            OnCubeletPress(0);
            yield return new WaitForSeconds(0.1f);
        }
        bool[] autosolverToggled = new bool[3];
        List<string> toggledChannelsCopy = toggledCubeletsChannels.Select(x => x).ToList();
        while (!ModuleSolved)
        {
            yield return null;
            int C = ((int)Bomb.GetTime() % 10 - 1) / 3;
            List<int> acceptableLD = C == 0 ? new List<int> { 1, 2, 3 } : (C == 1 ? new List<int> { 4, 5, 6 } : new List<int> { 7, 8, 9 });
            for (int i = 0; i < ToggledCubeletsCount; i++)
            {
                if (toggledChannelsCopy[i][C] == '1')
                {
                    if (acceptableLD.Contains((int)Bomb.GetTime() % 10))
                    {
                        OnCubeletPress(toggledCubelets[i]);
                        toggledChannelsCopy[i] = ToggleColor(toggledChannelsCopy[i], C);
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
        }
    }
}
