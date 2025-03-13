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
    [SerializeField] private KMColorblindMode Colorblind;

    [SerializeField] KMSelectable CubeSelectable;
    [SerializeField] MeshRenderer CubeRenderer;
    [SerializeField] TextMesh ColorblindText;
    [SerializeField] TextMesh IndexText;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };
    enum CTypes { Number, String, Boolean, Undefined };
    enum PressConditions { LastDigitEqualTo, TensEqualTo, SecondsEqualTo, SecondsDivisibleBy, LastDigitEven, LastDigitOdd, SecondsInString, SecondsMatch, AnyTime };

    bool cDefined = false;
    CTypes cType = CTypes.Undefined;
    int cNumber = 0;
    string cString = "";
    bool cBoolean = false;

    int curStage = -1;
    int[] stageColors = new int[10];
    PressConditions pressCondition;
    List<int> pressArgs = new List<int> { };

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
        ProcessPress(CheckPressCondition());
    }

    bool CheckPressCondition()
    {
        int LastDigit = (int)Bomb.GetTime() % 10;
        int Seconds = (int)Bomb.GetTime() % 60;
        int TensDigit = Seconds / 10;
        switch (pressCondition)
        {
            case PressConditions.LastDigitEqualTo:
                if (pressArgs.Contains(LastDigit)) return true;
                break;
            case PressConditions.TensEqualTo:
                if (pressArgs.Contains(TensDigit)) return true;
                break;
            case PressConditions.SecondsEqualTo:
                if (pressArgs.Contains(Seconds)) return true;
                break;
            case PressConditions.SecondsDivisibleBy:
                if (Seconds % pressArgs[0] == 0) return true;
                break;
            case PressConditions.SecondsMatch:
                if (LastDigit == TensDigit) return true;
                break;
            case PressConditions.LastDigitEven:
                if (LastDigit % 2 == 0) return true;
                break;
            case PressConditions.LastDigitOdd:
                if (LastDigit % 2 == 1) return true;
                break;
            case PressConditions.AnyTime:
                return true;
            default:
                return true;
        }
        return false;
    }
    
    void ProcessPress(bool correct)
    {
        if (pressCondition == PressConditions.AnyTime)
        {
            Log($"Correctly pressed the cube... Good job.");
            NextStage();
        }
        else
        {
            int LastDigit = (int)Bomb.GetTime() % 10;
            int Seconds = (int)Bomb.GetTime() % 60;
            int TensDigit = Seconds / 10;

            string relevantDigitLabel = "";
            int relevantDigitValue = 0;
            bool plural = false;

            switch (pressCondition)
            {
                case PressConditions.LastDigitEqualTo:
                case PressConditions.LastDigitEven:
                case PressConditions.LastDigitOdd:
                    relevantDigitLabel = "last";
                    relevantDigitValue = LastDigit;
                    break;
                case PressConditions.TensEqualTo:
                    relevantDigitLabel = "tens";
                    relevantDigitValue = TensDigit;
                    break;
                case PressConditions.SecondsDivisibleBy:
                case PressConditions.SecondsEqualTo:
                case PressConditions.SecondsMatch:
                    relevantDigitLabel = "seconds";
                    relevantDigitValue = Seconds;
                    plural = true;
                    break;
                default:
                    break;
            }
            if (correct)
            {
                Log($"Correctly pressed the cube at {relevantDigitLabel} digit{(plural ? "s" : "")} equal to {relevantDigitValue}.");
                NextStage();
            }
            else
            {
                Log($"Incorrectly pressed the cube at {relevantDigitLabel} digit{(plural ? "s" : "")} equal to {relevantDigitValue}. Strike!");
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

    void Start()
    {
        NextStage();
    }

    void SetCubeColor()
    {
        int color = stageColors[curStage];
        Log($"The cube is colored {ColorFullNames[color]}.");
        CubeRenderer.material.color = ColorList[color];
        ColorblindText.text = ColorShortNames[color];
        IndexText.color = color == 6 ? Color.black : Color.white;
        ColorblindText.color = color == 6 ? Color.black : Color.white;
    }

    void NextStage()
    {
        curStage++;
        if (curStage == 10)
        {
            Log("Processed all stages, module solved!");
            GetComponent<KMBombModule>().HandlePass();
            ModuleSolved = true;
            CubeRenderer.material.color = ColorList[1];
            IndexText.text = "";
            ColorblindText.text = "!";
            ColorblindText.color = Color.white;
        }
        else
        {
            IndexText.text = (curStage + 1).ToString();
            Log($"Stage {curStage + 1}:");
            switch (curStage)
            {
                case 0:
                    ProcessStage1();
                    break;
                case 1:
                    ProcessStage2();
                    break;
                case 2:
                    ProcessStage3();
                    break;
                case 3:
                    ProcessStage4();
                    break;
                case 4:
                    ProcessStage5();
                    break;
                default:
                    break;
            }
        }
    }

    void ProcessStage1()
    {
        // 1/3 chance of C getting defined; 2/3 chance of C being left undefined
        int stage1Color = 0;
        if (Rnd.Range(0, 3) == 0) stage1Color = new List<int>() { 0, 3, 5, 6, 7 }.PickRandom();
        else stage1Color = new List<int>() { 1, 2, 4 }.PickRandom();
        stageColors[curStage] = stage1Color;
        SetCubeColor();

        switch (stage1Color)
        {
            case 0:
            case 5:
                cType = CTypes.Number;
                cNumber = Bomb.GetSerialNumberNumbers().Last();
                pressCondition = PressConditions.LastDigitEqualTo;
                pressArgs = new List<int> { cNumber };
                Log($"The cube is {ColorFullNames[stage1Color]} - C is now a Number: C = {cNumber}.");
                Log($"You should press the cube when the last digit of the timer is {pressArgs[0]}.");
                break;
            case 3:
                cType = CTypes.String;
                cString = Bomb.GetSerialNumberLetters().Join("");
                pressCondition = PressConditions.SecondsDivisibleBy;
                pressArgs = new List<int> { Alphabet.IndexOf(cString.Last()) + 2 };
                Log($"The cube is Yellow - C is now a String: C = {cString}.");
                Log($"You should press the cube when the seconds digits of the timer form a number that is divisible by {pressArgs[0]}.");
                break;
            case 6:
            case 7:
                cType = CTypes.Boolean;
                cBoolean = Bomb.GetSerialNumberNumbers().First() % 2 == 0;
                if (Bomb.GetBatteryHolderCount() % 2 == 0)
                {
                    pressCondition = cBoolean ? PressConditions.LastDigitEven : PressConditions.LastDigitOdd;
                }
                else
                {
                    pressCondition = cBoolean ? PressConditions.LastDigitOdd : PressConditions.LastDigitEven;
                }
                Log($"The cube is {ColorFullNames[stage1Color]} - C is now a Boolean: C = {BoolLog(cBoolean)}.");
                Log($"You should press the cube when the last digit of the timer is {(pressCondition == PressConditions.LastDigitEven ? "even" : "odd")}.");
                break;
            default:
                pressCondition = PressConditions.LastDigitEqualTo;
                pressArgs = new List<int> { 2, 3, 5, 7 };
                Log($"The cube is none of the mentioned colors - C is left Undefined.");
                Log($"You should press the cube when the last digit of the timer is prime.");
                break;
        }
    }

    void ProcessStage2()
    {
        stageColors[curStage] = Rnd.Range(0, 8);
        SetCubeColor();
        switch (cType)
        {
            case CTypes.Number:
                cNumber *= 3;
                cNumber += 1; // C will be in range 1-28 after this, no modulo needed.
                pressCondition = PressConditions.SecondsEqualTo;
                pressArgs = new List<int> { cNumber % 60 };
                Log($"C is a Number - multiplying by 3 and adding 1: C = {cNumber}.");
                Log($"You should press the cube when the seconds digits of the timer are {IntoTwoDigitNumber(pressArgs[0])}.");
                break;
            case CTypes.String:
                cString += Alphabet[(Bomb.GetBatteryCount() + Bomb.GetBatteryHolderCount()) % 26];
                pressCondition = PressConditions.SecondsEqualTo;
                pressArgs = new List<int> { };
                for (int s = 0; s < 60; s++)
                {
                    if (cString.Contains(Alphabet[s % 26])) pressArgs.Add(s);
                }
                Log($"C is a String - appending {cString.Last()}: C = {cString}.");
                Log($"You should press the cube when the seconds digits of the timer are {pressArgs.Select(x => IntoTwoDigitNumber(x)).Join("/")}.");
                break;
            case CTypes.Boolean:
                if (stageColors[curStage] == 1 || stageColors[curStage] == 2 || stageColors[curStage] == 4 || stageColors[curStage] == 7)
                {
                    cBoolean = !cBoolean;
                    Log($"C is a Boolean and the cube is {ColorFullNames[stageColors[curStage]]} - applying the NOT operator: C = {BoolLog(cBoolean)}.");
                }
                else Log($"C is a Boolean but the cube is not Black/Green/Blue/Magenta - not modifying C this stage: C = {BoolLog(cBoolean)}.");
                pressCondition = PressConditions.LastDigitEqualTo;
                if (cBoolean) pressArgs = new List<int> { 4, 6, 8, 9 };
                else pressArgs = new List<int> { 0, 1, 2, 3, 5, 7 }; // 0 and 1 are included here as they are NOT composite. (NOT composite != prime)
                Log($"You should press the cube when the last digit of the timer is {(cBoolean ? "composite" : "not composite (prime or a 0/1)")}.");
                break;
            case CTypes.Undefined:
                if (Bomb.GetPortPlates().Any(x => x.Count() == 0))
                {
                    cType = CTypes.Boolean;
                    cBoolean = true;
                    Log($"C is Undefined and there is an empty port plate - C is now a Boolean: C = {BoolLog(cBoolean)}.");
                }
                else if (Bomb.GetPortCount() >= 5)
                {
                    cType = CTypes.Number;
                    cNumber = Bomb.GetPortCount() * Bomb.GetPortPlateCount();
                    Log($"C is Undefined, there isn't an empty port plate, but there are 5 or more ports - C is now a Number: C = {cNumber}.");
                }
                else Log($"C is Undefined and neither of the conditions were met - C is left Undefined.");
                pressCondition = PressConditions.AnyTime;
                Log($"You may press the cube at any time.");
                break;
            default:
                break;
        }
    }

    void ProcessStage3()
    {
        // 1/4 chance for RGB; 3/4 chance for MYCWK
        int stage3Color = 0;
        if (Rnd.Range(0, 4) == 0) stage3Color = Rnd.Range(0, 3);
        else stage3Color = Rnd.Range(3, 8);
        stageColors[curStage] = stage3Color;
        SetCubeColor();
        
        if (stage3Color < 3)
        {
            Log($"The cube is Red, Green or Blue ({ColorFullNames[stageColors[curStage]]}), modifying C:");
            switch (cType)
            {
                case CTypes.Number:
                    cNumber = ReverseNum(cNumber);
                    cNumber *= DigitalRoot(cNumber); // Again, C will be in range 1-729 after this, no modulo needed.
                    Log($"C is a Number - reversing it and then multyplying it by its digital root: C = {cNumber}.");
                    break;
                case CTypes.String:
                    cString = cString.Reverse().Join("");
                    bool prepend = cString.Length % 2 == 1;
                    if (prepend) cString = "ODD" + cString;
                    cString = cString.Substring(0, cString.Length / 2);
                    Log($"C is a String - reversing it{(prepend ? ", prepending \"ODD\" to it" : "")}, then taking its first half: C = {cString}.");
                    break;
                case CTypes.Boolean:
                case CTypes.Undefined:
                    bool definedC = false;
                    if (cType == CTypes.Undefined)
                    {
                        definedC = true;
                        cType = CTypes.Boolean;
                        cBoolean = false;
                        Log($"C is Undefined - C is now a Boolean: C = {BoolLog(cBoolean)}.");
                    }
                    bool XNOR = Bomb.GetSolvableModuleNames().Count % 2 == 0;
                    cBoolean = !(cBoolean ^ XNOR);
                    Log($"{(definedC ? "C has been defined as a Boolean now" : "C is a Boolean")} - XNORing it with {BoolLog(XNOR)}: C = {BoolLog(cBoolean)}.");
                    break;
                default:
                    break;
            }
        }
        List<int> productElements = new List<int> { Bomb.GetBatteryCount(), Bomb.GetBatteryHolderCount(), Bomb.GetIndicators().Count(), Bomb.GetPortCount(), Bomb.GetPortPlateCount(), Bomb.GetSerialNumberNumbers().Sum() };
        int product = 1;
        foreach (int e in productElements) product *= Math.Max(1, e);
        if (cType == CTypes.Number && cNumber != 0) product *= cNumber;
        Log($"Taking the product of all of the edgework values{((cType == CTypes.Number && cNumber != 0) ? " and C (because it is a number)" : "")} yields {product}.");
        pressCondition = PressConditions.SecondsEqualTo;
        pressArgs = new List<int> { product % 60 };
        Log($"You should press the cube when the seconds digits of the timer are {IntoTwoDigitNumber(pressArgs[0])}.");
    }

    void ProcessStage4()
    {
        // 1/10 chance for White; 9/10 chance for other colors
        int stage4Color = 0;
        if (Rnd.Range(0, 10) == 0) stage4Color = 6;
        else stage4Color = new List<int> { 0, 1, 2, 3, 4, 5, 7 }.PickRandom();
        stageColors[curStage] = stage4Color;
        SetCubeColor();
        if (stage4Color == 6 && cType != CTypes.Undefined)
        {
            Log($"The cube is White and C is defined, modifying C:");
            switch (cType)
            {
                case CTypes.Number:
                    if (Bomb.GetSerialNumberNumbers().Count() == 0) cNumber = 0;
                    else cNumber = int.Parse(Bomb.GetSerialNumberNumbers().Join(""));
                    Log($"C is a Number: C = {cNumber}.");
                    break;
                case CTypes.String:
                    List<string> modules = Bomb.GetModuleNames().Select(x => x.ToUpper()).Where(x => x.All(c => Alphabet.Contains(c) || c == ' ')).ToList();
                    modules.Sort();
                    cString = modules[0].Replace(" ", "");
                    Log($"C is a String: C = {cString}.");
                    break;
                case CTypes.Boolean:
                    cBoolean = !cBoolean;
                    Log($"C is a Boolean - applying the NOT operator: C = {cBoolean}.");
                    break;
                default:
                    break;
            }
            pressCondition = PressConditions.AnyTime;
            Log($"You may press the cube at any time.");
        }
        else
        {
            Log($"The cube is not White or C is Undefined - not modifying C this stage.");
            switch (cType)
            {
                case CTypes.Number:
                    pressCondition = PressConditions.TensEqualTo;
                    pressArgs = new List<int> { Int32.Parse(cNumber.ToString().First().ToString()) % 6 };
                    Log($"You should press the cube when the tens digit of the timer is {pressArgs[0]}.");
                    break;
                case CTypes.Boolean:
                    pressCondition = PressConditions.TensEqualTo;
                    if (cBoolean) pressArgs = new List<int> { 4 };
                    else pressArgs = new List<int> { 0, 1, 2, 3, 5 }; // Same thingy as before
                    Log($"You should press the cube when the tens digit of the timer is {(cBoolean ? "composite" : "not composite (prime or a 0/1)")}.");
                    break;
                default:
                    pressCondition = PressConditions.SecondsMatch;
                    Log($"You should press the cube when both of the seconds digits of the timer match.");
                    break;
            }
        }
    }

    void ProcessStage5()
    {
        stageColors[curStage] = Rnd.Range(0, 8);
        SetCubeColor();
        if (cType == CTypes.Boolean)
        {
            Log($"C is a Boolean - not modifying C this stage.");
        }
        else if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0)
        {
            switch (cType)
            {
                case CTypes.Number:
                    cNumber -= 10000;
                    cNumber = Mod(cNumber, 10000); // Okay modulo is actually forced here :P
                    Log($"The last digit of the S# is even and C is a Number - subtracting 10000: C = {cNumber}.");
                    break;
                case CTypes.String:
                    int shiftValue = Bomb.GetSerialNumberNumbers().Last() - Bomb.GetSerialNumberNumbers().First(); // Positive - right, negative - left.
                    shiftValue %= cString.Length; // In this case I NEED C#'s weird modulo where it keeps the sign
                    if (shiftValue > 0) cString = cString.Substring(shiftValue + 1) + cString.Substring(0, cString.Length - shiftValue);
                    else if (shiftValue < 0) cString = cString.Substring(-shiftValue) + cString.Substring(0, -shiftValue);
                    Log($"The last digit of the S# is even and C is a String - shifting it to the right by {Bomb.GetSerialNumberNumbers().Last()} then left by {Bomb.GetSerialNumberNumbers().First()}: C = {cString}.");
                    break;
                case CTypes.Undefined:
                    if (Bomb.GetSerialNumberNumbers().Any(x => new List<int> { 2, 3, 5, 7 }.Contains(x)))
                    {
                        cType = CTypes.String;
                        cString = "CUBE";
                        Log($"The last digit of the S# is even, the S# contains a prime number and C is Undefined - C is now a String: C = {cString}.");
                    }
                    else Log($"The last digit of the S# is even, C is Undefined, but the serial number does not contain a prime number - C is left Undefined.");
                    break;
                default:
                    break;
            }
        }
        int parityMatchNumber = 0;
        switch (cType)
        {
            case CTypes.Number:
                parityMatchNumber = cNumber;
                break;
            case CTypes.String:
                parityMatchNumber = (Alphabet.IndexOf(cString.First()) + 1) + (Alphabet.IndexOf(cString.Last()) + 1);
                break;
            case CTypes.Boolean:
                parityMatchNumber = cBoolean ? Bomb.GetSerialNumberNumbers().First() : Bomb.GetSerialNumberNumbers().Last();
                break;
            case CTypes.Undefined:
                parityMatchNumber = 3525854; // Last digits of the alphabetical positions of the letters in the word "COLORED"
                break;
            default:
                break;
        }
        pressCondition = parityMatchNumber % 2 == 0 ? PressConditions.LastDigitEven : PressConditions.LastDigitOdd;
        Log($"You should press the cube when the last digit of the timer is {(pressCondition == PressConditions.LastDigitEven ? "even" : "odd")}.");
    }

    string IntoTwoDigitNumber(int n)
    {
        if (n > 9) return n.ToString();
        else return "0" + n.ToString();
    }

    int DigitalRoot(int n)
    {
        if (n == 0) return 0;
        else return (n - 1) % 9 + 1;
    }

    int Mod(int n, int m)
    {
        if (n >= 0) return n % m;
        else
        {
            while (n < 0) n += m;
            return n;
        }
    }

    int ReverseNum(int n)
    {
        return Int32.Parse(n.ToString().Reverse().Join(""));
    }

    string BoolLog(bool b)
    {
        return b ? "TRUE" : "FALSE";
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
