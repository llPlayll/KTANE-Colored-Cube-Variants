using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] TextMesh TPSolverText;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    Color[] ColorList = { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta, Color.cyan, Color.white, Color.black };
    string[] ColorShortNames = { "R", "G", "B", "Y", "M", "C", "W", "K" };
    string[] ColorFullNames = { "Red", "Green", "Blue", "Yellow", "Magenta", "Cyan", "White", "Black" };
    string[] NumberOperations = { "Add [2 * M] to C", "Multiply C by M", "Divide C by [2 * M]", "Multiply C by 2, then divide C by M", "Subtract M from C, then divide C by 3", "Subtract [C % M] from C, then divide it by M", "Reverse C", "Do not modify C" };
    string[] StringWords = { "REBOOT", "TOUCHE", "WATERS", "GOLDEN", "PARSED", "BREEZY", "BRIGHT", "VOIDED" };
    string[,] StringSubstringQueries = {
        { "1", "2-6", "5-6", "4", "1-6" },
        { "3-4", "6", "4-5", "1-6", "4" },
        { "1-6", "3-4", "5", "1-2", "2" },
        { "1-3", "5", "6", "1-6", "2" },
        { "2-6", "1-5", "1-6", "4-6", "4" },
        { "2-4", "4-5", "3", "1-6", "1-5" },
        { "1-6", "2-3", "1-3", "3", "4-6" },
        { "2", "2-5", "1-6", "3", "5-6" },
        { "1-4", "4", "1-6", "1", "4-5" },
        { "5", "1-6", "2", "2-4", "3-5" },
    };
    string[] BooleanConditions = { "The number is even", "The number is composite.", "The number is odd.", "The number is divisible by 5.", "The number is prime." };
    string[] BooleanOperators = { "OR", "NAND", "XOR", "XOR", "NOR", "XNOR", "AND", "NOT" };
    enum CTypes { Number, String, Boolean, Undefined };
    enum PressConditions { LastDigitEqualTo, TensEqualTo, SecondsEqualTo, SecondsSumEqualTo, SecondsDifferenceEqualTo, SecondsDivisibleBy, LastDigitEven, LastDigitOdd, BothDigitsEven, BothDigitsOdd, SecondsMatch, AnyTime };

    int[,] NumberMTable = new int[8, 5];
    bool cDefined = false;
    CTypes cType = CTypes.Undefined;
    int cNumber = 0;
    string cString = "";
    bool cBoolean = false;

    int curStage = -1;
    int[] stageColors = new int[10];
    PressConditions pressCondition;
    List<int> pressArgs = new List<int> { };

    bool ZenModeActive;

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
        ProcessPress(CheckPressCondition((int)Bomb.GetTime()));
    }

    bool CheckPressCondition(int bombTime)
    {
        int LastDigit = bombTime % 10;
        int Seconds = bombTime % 60;
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
            case PressConditions.SecondsSumEqualTo:
                if ((TensDigit + LastDigit) == pressArgs[0]) return true;
                break;
            case PressConditions.SecondsDifferenceEqualTo:
                if (Math.Abs(TensDigit - LastDigit) == pressArgs[0]) return true;
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
            case PressConditions.BothDigitsEven:
                if (LastDigit % 2 == 0 && TensDigit % 2 == 0) return true;
                break;
            case PressConditions.BothDigitsOdd:
                if (LastDigit % 2 == 1 && TensDigit % 2 == 1) return true;
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

            string relevantDigitName = "";
            int relevantDigitValue = 0;
            bool plural = false;

            switch (pressCondition)
            {
                case PressConditions.LastDigitEqualTo:
                case PressConditions.LastDigitEven:
                case PressConditions.LastDigitOdd:
                    relevantDigitName = "last";
                    relevantDigitValue = LastDigit;
                    break;
                case PressConditions.TensEqualTo:
                    relevantDigitName = "tens";
                    relevantDigitValue = TensDigit;
                    break;
                case PressConditions.SecondsDivisibleBy:
                case PressConditions.SecondsEqualTo:
                case PressConditions.SecondsSumEqualTo:
                case PressConditions.SecondsDifferenceEqualTo:
                case PressConditions.SecondsMatch:
                case PressConditions.BothDigitsEven:
                case PressConditions.BothDigitsOdd:
                    relevantDigitName = "seconds";
                    relevantDigitValue = Seconds;
                    plural = true;
                    break;
                default:
                    break;
            }
            if (correct)
            {
                Log($"Correctly pressed the cube at {relevantDigitName} digit{(plural ? "s" : "")} equal to {relevantDigitValue}.");
                NextStage();
            }
            else
            {
                Log($"Incorrectly pressed the cube at {relevantDigitName} digit{(plural ? "s" : "")} equal to {relevantDigitValue}. Strike!");
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
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

    void LogCondition()
    {
        switch (pressCondition)
        {
            case PressConditions.LastDigitEqualTo:
                Log($"You should press the cube when the last digit of the timer is {pressArgs.Join("/")}.");
                break;
            case PressConditions.TensEqualTo:
                Log($"You should press the cube when the tens digit of the timer is {pressArgs.Join("/")}.");
                break;
            case PressConditions.SecondsEqualTo:
                Log($"You should press the cube when the seconds digits of the timer are {pressArgs.Select(x => IntoTwoDigitNumber(x)).Join("/")}.");
                break;
            case PressConditions.SecondsSumEqualTo:
                Log($"You should press the cube when the sum of the two seconds digits of the timer is {pressArgs[0]}.");
                break;
            case PressConditions.SecondsDifferenceEqualTo:
                Log($"You should press the cube when the difference between the two seconds digits of the timer is {pressArgs[0]}.");
                break;
            case PressConditions.SecondsDivisibleBy:
                Log($"You should press the cube when the seconds digits of the timer form a number that is divisible by {pressArgs[0]}.");
                break;
            case PressConditions.LastDigitEven:
                Log($"You should press the cube when the last digit of the timer is even.");
                break;
            case PressConditions.LastDigitOdd:
                Log($"You should press the cube when the last digit of the timer is odd.");
                break;
            case PressConditions.BothDigitsEven:
                Log($"You should press the cube when both of the seconds digits of the timer are even.");
                break;
            case PressConditions.BothDigitsOdd:
                Log($"You should press the cube when both of the seconds digits of the timer are odd.");
                break;
            case PressConditions.SecondsMatch:
                Log($"You should press the cube when both of the seconds digits of the timer match.");
                break;
            case PressConditions.AnyTime:
                Log($"You may press the cube at any time.");
                break;
            default:
                break;
        }
    }

    void FillInMTable()
    {
        int serialNumberVowels = Bomb.GetSerialNumberLetters().Count(x => "AEIOU".Contains(x));
        int serialNumberConsonants = Bomb.GetSerialNumberLetters().Count() - serialNumberVowels;
        int serialNumberSum = Bomb.GetSerialNumberNumbers().Sum();
        //Debug.Log($"<Not Colored Cube #{ModuleId}> the M table is as follows:");
        for (int i = 0; i < 8; i++)
        {
            int[] row = new int[5];
            switch (i)
            {
                case 0: // Red
                    row = new int[5] { Bomb.GetBatteryHolderCount(), Bomb.GetPortCount("StereoRCA"), Bomb.GetSerialNumberNumbers().Last(), Bomb.GetPortCount("DVI"), serialNumberSum };
                    break;
                case 1: // Green
                    row = new int[5] { Bomb.GetSerialNumberNumbers().First(), Bomb.GetSerialNumberLetters().Count(), Bomb.GetPortCount("PS2"), Bomb.GetSerialNumberNumbers().Last(), serialNumberConsonants };
                    break;
                case 2: // Blue
                    row = new int[5] { Bomb.GetSerialNumberLetters().Count(), Bomb.GetPortCount("Serial"), Bomb.GetBatteryCount(Battery.D), Bomb.GetSerialNumberNumbers().Count(), Bomb.GetIndicators().Count() };
                    break;
                case 3: // Yellow
                    row = new int[5] { Bomb.GetBatteryCount(), serialNumberSum, Bomb.GetPortPlateCount(), Bomb.GetBatteryHolderCount(), Bomb.GetBatteryCount(Battery.AA) };
                    break;
                case 4: // Magenta
                    row = new int[5] { Bomb.GetPortCount("Parallel"), Bomb.GetBatteryCount(Battery.AA), Bomb.GetSerialNumberNumbers().Last(), Bomb.GetBatteryCount(), serialNumberVowels };
                    break;
                case 5: // Cyan
                    row = new int[5] { serialNumberConsonants, Bomb.GetSerialNumberNumbers().Count(), Bomb.GetBatteryHolderCount(), Bomb.GetSerialNumber()[2] - '0', Bomb.GetBatteryCount(Battery.D) };
                    break;
                case 6: // White
                    row = new int[5] { Bomb.GetOnIndicators().Count(), Bomb.GetPortCount(), Bomb.GetSerialNumberNumbers().First(), Bomb.GetPortCount("StereoRCA"), Bomb.GetIndicators().Count() };
                    break;
                case 7: // Black
                    row = new int[5] { Bomb.GetPortCount(), serialNumberVowels, Bomb.GetPortCount("RJ45"), Bomb.GetPortPlateCount(), Bomb.GetOffIndicators().Count() };
                    break;
                default:
                    break;
            }
            for (int j = 0; j < 5; j++) NumberMTable[i, j] = row[j];
            //Debug.Log(row.Join(", "));
        }
    }

    void Start()
    {
        FillInMTable();
        NextStage();
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
                    ProcessStage1(true);
                    break;
                case 1:
                    ProcessStage2(true);
                    break;
                case 2:
                    ProcessStage3(true);
                    break;
                case 3:
                    ProcessStage4(true);
                    break;
                case 4:
                    ProcessStage5(true);
                    break;
                default:
                    ProcessLateStages();
                    break;
            }
            LogCondition();
        }
    }

    void ProcessStage1(bool generateColor)
    {
        if (generateColor)
        {
            // 1/3 chance of C getting defined; 2/3 chance of C being left undefined
            int stage1Color = 0;
            if (Rnd.Range(0, 3) == 0) stage1Color = new List<int>() { 0, 3, 5, 6, 7 }.PickRandom();
            else stage1Color = new List<int>() { 1, 2, 4 }.PickRandom();
            stageColors[curStage] = stage1Color;
            SetCubeColor();
        }
        switch (stageColors[curStage])
        {
            case 0:
            case 5:
                cType = CTypes.Number;
                cNumber = Bomb.GetSerialNumberNumbers().Last();
                pressCondition = PressConditions.LastDigitEqualTo;
                pressArgs = new List<int> { cNumber };
                Log($"The cube is {ColorFullNames[stageColors[curStage]]} - C is now a Number: C = {cNumber}.");
                break;
            case 3:
                cType = CTypes.String;
                cString = Bomb.GetSerialNumberLetters().Join("");
                pressCondition = PressConditions.SecondsDivisibleBy;
                pressArgs = new List<int> { Alphabet.IndexOf(cString.Last()) + 2 };
                Log($"The cube is Yellow - C is now a String: C = {cString}.");
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
                Log($"The cube is {ColorFullNames[stageColors[curStage]]} - C is now a Boolean: C = {LogBoolean(cBoolean)}.");
                break;
            default:
                pressCondition = PressConditions.LastDigitEqualTo;
                pressArgs = new List<int> { 2, 3, 5, 7 };
                Log($"The cube is none of the mentioned colors - C is left Undefined.");
                break;
        }
    }

    void ProcessStage2(bool generateColor)
    {
        if (generateColor)
        {
            stageColors[curStage] = Rnd.Range(0, 8);
            SetCubeColor();
        }
        switch (cType)
        {
            case CTypes.Number:
                cNumber *= 3;
                cNumber += 1; // C will be in range 1-28 after this, no modulo needed.
                pressCondition = PressConditions.SecondsEqualTo;
                pressArgs = new List<int> { cNumber % 60 };
                Log($"C is a Number - multiplying by 3 and adding 1: C = {cNumber}.");
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
                break;
            case CTypes.Boolean:
                if (stageColors[curStage] == 1 || stageColors[curStage] == 2 || stageColors[curStage] == 4 || stageColors[curStage] == 7)
                {
                    cBoolean = !cBoolean;
                    Log($"C is a Boolean and the cube is {ColorFullNames[stageColors[curStage]]} - applying the NOT operator: C = {LogBoolean(cBoolean)}.");
                }
                else Log($"C is a Boolean but the cube is not Black/Green/Blue/Magenta - not modifying C this stage: C = {LogBoolean(cBoolean)}.");
                pressCondition = PressConditions.LastDigitEqualTo;
                if (cBoolean) pressArgs = new List<int> { 4, 6, 8, 9 };
                else pressArgs = new List<int> { 0, 1, 2, 3, 5, 7 }; // 0 and 1 are included here as they are NOT composite. (NOT composite != prime)
                break;
            case CTypes.Undefined:
                if (Bomb.GetPortPlates().Any(x => x.Count() == 0))
                {
                    cType = CTypes.Boolean;
                    cBoolean = true;
                    Log($"C is Undefined and there is an empty port plate - C is now a Boolean: C = {LogBoolean(cBoolean)}.");
                }
                else if (Bomb.GetPortCount() >= 5)
                {
                    cType = CTypes.Number;
                    cNumber = Bomb.GetPortCount() * Bomb.GetPortPlateCount();
                    Log($"C is Undefined, there isn't an empty port plate, but there are 5 or more ports - C is now a Number: C = {cNumber}.");
                }
                else Log($"C is Undefined and neither of the conditions were met - C is left Undefined.");
                pressCondition = PressConditions.AnyTime;
                break;
            default:
                break;
        }
    }

    void ProcessStage3(bool generateColor)
    {
        if (generateColor)
        {
            // 1/4 chance for RGB; 3/4 chance for MYCWK
            int stage3Color = 0;
            if (Rnd.Range(0, 4) == 0) stage3Color = Rnd.Range(0, 3);
            else stage3Color = Rnd.Range(3, 8);
            stageColors[curStage] = stage3Color;
            SetCubeColor();
        }
        if (stageColors[curStage] < 3)
        {
            Log($"The cube is Red, Green or Blue ({ColorFullNames[stageColors[curStage]]}) - modifying C:");
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
                        Log($"C is Undefined - C is now a Boolean: C = {LogBoolean(cBoolean)}.");
                    }
                    bool XNOR = Bomb.GetSolvableModuleNames().Count % 2 == 0;
                    cBoolean = !(cBoolean ^ XNOR);
                    Log($"{(definedC ? "C has been defined as a Boolean now" : "C is a Boolean")} - XNORing it with {LogBoolean(XNOR)}: C = {LogBoolean(cBoolean)}.");
                    break;
                default:
                    break;
            }
        }
        else
        {
            Log($"The cube not Red, nor Green, nor Blue ({ColorFullNames[stageColors[curStage]]}) - not modifying C this stage.");
        }
        List<int> productElements = new List<int> { Bomb.GetBatteryCount(), Bomb.GetBatteryHolderCount(), Bomb.GetIndicators().Count(), Bomb.GetPortCount(), Bomb.GetPortPlateCount(), Bomb.GetSerialNumberNumbers().Sum() };
        int product = 1;
        foreach (int e in productElements) product *= Math.Max(1, e);
        if (cType == CTypes.Number && cNumber != 0) product *= cNumber;
        Log($"Taking the product of all of the edgework values{((cType == CTypes.Number && cNumber != 0) ? " and C (because it is a non-zero number)" : "")} yields {product}.");
        pressCondition = PressConditions.SecondsEqualTo;
        pressArgs = new List<int> { product % 60 };
    }

    void ProcessStage4(bool generateColor)
    {
        if (generateColor)
        {
            // 1/10 chance for White; 9/10 chance for other colors
            int stage4Color = 0;
            if (Rnd.Range(0, 10) == 0) stage4Color = 6;
            else stage4Color = new List<int> { 0, 1, 2, 3, 4, 5, 7 }.PickRandom();
            stageColors[curStage] = stage4Color;
            SetCubeColor();
        }
        if (stageColors[curStage] == 6 && cType != CTypes.Undefined)
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
        }
        else
        {
            Log($"The cube is not White or C is Undefined - not modifying C this stage.");
            switch (cType)
            {
                case CTypes.Number:
                    pressCondition = PressConditions.TensEqualTo;
                    pressArgs = new List<int> { (cNumber.ToString().First() - '0') % 6 };
                    break;
                case CTypes.Boolean:
                    pressCondition = PressConditions.TensEqualTo;
                    if (cBoolean) pressArgs = new List<int> { 4 };
                    else pressArgs = new List<int> { 0, 1, 2, 3, 5 }; // Same thingy as before
                    break;
                default:
                    pressCondition = PressConditions.SecondsMatch;
                    break;
            }
        }
    }

    void ProcessStage5(bool generateColor)
    {
        stageColors[curStage] = Rnd.Range(0, 8);
        SetCubeColor();
        if (cType == CTypes.Boolean) Log($"C is a Boolean - not modifying C this stage.");
        else if (Bomb.GetSerialNumberNumbers().Last() % 2 == 0)
        {
            switch (cType)
            {
                case CTypes.Number:
                    cNumber = 10000 - cNumber;
                    Log($"The last digit of the S# is even and C is a Number - subtracting it from 10000: C = {cNumber}.");
                    break;
                case CTypes.String:
                    int shiftValue = (Bomb.GetSerialNumberNumbers().Last() + 1) - (Bomb.GetSerialNumberNumbers().First() + 1); // Positive - right, negative - left.
                    shiftValue %= cString.Length; // In this case I NEED C#'s weird modulo where it keeps the sign
                    if (shiftValue > 0) cString = cString.Substring(cString.Length - shiftValue) + cString.Substring(0, cString.Length - shiftValue);
                    else if (shiftValue < 0) cString = cString.Substring(-shiftValue) + cString.Substring(0, -shiftValue);
                    Log($"The last digit of the S# is even and C is a String - shifting it to the right by {Bomb.GetSerialNumberNumbers().Last() + 1} then left by {Bomb.GetSerialNumberNumbers().First() + 1}: C = {cString}.");
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
        else Log($"The last digit of the S# is odd - not modifying C this stage.");
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
    }

    void ProcessLateStages()
    {
        stageColors[curStage] = Rnd.Range(0, 8);
        SetCubeColor();
        switch (cType)
        {
            case CTypes.Number:
                Log("C is a Number:");

                int M = -1;
                if (stageColors[curStage - 1] != 6 && stageColors[curStage - 1] != 7) M = NumberMTable[stageColors[curStage], curStage - 5];
                int intermediateC = -1;
                switch (stageColors[curStage - 1])
                {
                    case 0: // Red
                        cNumber += 2 * M;
                        break;
                    case 1: // Green
                        cNumber *= M;
                        break;
                    case 2: // Blue
                        cNumber = Divide(cNumber, 2 * M);
                        break;
                    case 3: // Yellow
                        cNumber *= 2;
                        cNumber = Mod(cNumber, 10000);
                        intermediateC = cNumber;
                        cNumber = Divide(cNumber, M);
                        break;
                    case 4: // Magenta
                        cNumber -= M;
                        cNumber = Mod(cNumber, 10000);
                        intermediateC = cNumber;
                        cNumber /= 3; // The Div method was made for the division-by-zero cases, which is why I'm not using it here
                        break;
                    case 5: // Cyan
                        cNumber -= Mod(cNumber, M);
                        intermediateC = cNumber;
                        cNumber = Divide(cNumber, M);
                        break;
                    case 6: // White
                        cNumber = ReverseNum(cNumber);
                        break;
                    default: // Black
                        break;
                }
                cNumber = Mod(cNumber, 10000);
                Log($"The cube was {ColorFullNames[stageColors[curStage - 1]]} last stage - the operation is \"{NumberOperations[stageColors[curStage - 1]]}\"{(M > -1 ? $" with M = {M} ({ColorFullNames[stageColors[curStage]]}/Stage {curStage + 1})" : "")}: C = {(intermediateC > -1 ? $"{intermediateC} -> C = " : "")}{cNumber}.");
                break;
            case CTypes.String:
                Log("C is a String:");
                string l1 = Alphabet[Bomb.GetSerialNumberNumbers().Last() + curStage - (Bomb.GetSerialNumberNumbers().First() % 2 == 1 ? 5 : 0)].ToString();
                if ((curStage + 1) % 2 == 0) cString += l1;
                else cString = l1 + cString;
                Log($"The first obtained letter is {l1}, which will be {((curStage + 1) % 2 == 0 ? "appended" : "prepended")} to C: C = {cString}.");

                string l2 = Alphabet[25 - Alphabet.IndexOf(l1)].ToString();
                int insertPos = (curStage + 1) % cString.Length;
                cString = InsertIntoString(cString, l2, insertPos);
                Log($"The second obtained letter (after Atbash) is {l2}, which will be inserted into C at position {insertPos + 1}: C = {cString}.");

                int cLength = cString.Length % 10;
                string stageWord = StringWords[stageColors[curStage]];
                string subQuery = StringSubstringQueries[cLength, curStage - 5];
                string substring = "";
                if (subQuery.Length == 1) substring = stageWord[subQuery[0] - '0' - 1].ToString();
                else
                {
                    int start = subQuery[0] - '0' - 1;
                    int end = subQuery[2] - '0' - 1;
                    int length = end - start + 1;
                    substring = stageWord.Substring(start, length);
                }
                if ((curStage + 1) % 2 == 0) cString = substring + cString;
                else cString += substring;
                Log($"The cube is {ColorFullNames[stageColors[curStage]]}, which gives the word {stageWord}; the {(substring.Length == 1 ? "letter" : "substring")} obtained from the table ({subQuery} - {cLength}/Stage {curStage + 1}) is {substring}, which will be {((curStage + 1) % 2 == 0 ? "prepended" : "appended")} to C: C = {cString}.");
                break;
            case CTypes.Boolean:
                Log("C is a Boolean:");
                bool B = false;
                int BM = NumberMTable[stageColors[curStage], curStage - 5];
                string BCondition = BooleanConditions[curStage - 5];
                switch (curStage - 5)
                {
                    case 0: // Stage 6 - "The number is even."
                        B = BM % 2 == 0;
                        break;
                    case 1: // Stage 7 - "The number is composite."
                        B = IsComposite(BM);
                        break;
                    case 2: // Stage 8 - "The number is odd."
                        B = BM % 2 == 1;
                        break;
                    case 3: // Stage 9 - "The number is divisible by 5."
                        B = BM % 5 == 0;
                        break;
                    case 4: // Stage 10 - "The number is prime."
                        B = IsPrime(BM);
                        break;
                    default:
                        break;
                }
                Log($"The number obtained from the Number table is {BM} ({ColorFullNames[stageColors[curStage]]}/Stage {curStage + 1}), and the condition is \"{BCondition}\": B = {LogBoolean(B)}.");
                string BOperator = BooleanOperators[stageColors[curStage]];
                switch (BOperator)
                {
                    case "NOT":
                        cBoolean = !cBoolean;
                        break;
                    case "OR":
                        cBoolean |= B;
                        break;
                    case "AND":
                        cBoolean &= B;
                        break;
                    case "XOR":
                        cBoolean ^= B;
                        break;
                    case "NOR":
                        cBoolean |= B;
                        cBoolean = !cBoolean;
                        break;
                    case "NAND":
                        cBoolean &= B;
                        cBoolean = !cBoolean;
                        break;
                    case "XNOR":
                        cBoolean ^= B;
                        cBoolean = !cBoolean;
                        break;
                    default:
                        break;
                }
                Log($"The cube is {ColorFullNames[stageColors[curStage]]}, which gives the {BOperator} operator: C = {LogBoolean(cBoolean)}");
                break;
            default:
                break;
        }
        switch (cType)
        {
            case CTypes.Number:
                switch (curStage)
                {
                    case 5:
                        pressCondition = PressConditions.LastDigitEqualTo;
                        pressArgs = new List<int> { DigitalRoot(cNumber) };
                        break;
                    case 6:
                        pressCondition = PressConditions.SecondsSumEqualTo;
                        pressArgs = new List<int> { (cNumber % 9) + 3 };
                        break;
                    case 7:
                        pressCondition = PressConditions.SecondsDifferenceEqualTo;
                        pressArgs = new List<int> { cNumber % 5 };
                        break;
                    case 8:
                        bool parity = cNumber % 2 == 0;
                        pressCondition = parity ? PressConditions.BothDigitsEven : PressConditions.BothDigitsOdd;
                        break;
                    case 9:
                        pressCondition = PressConditions.SecondsEqualTo;
                        int tens = (cNumber.ToString().First() - '0') % 6;
                        int last = cNumber.ToString().Last() - '0';
                        pressArgs = new List<int> { 10 * tens + last };
                        break;
                    default:
                        break;
                }
                break;
            case CTypes.String:
                pressCondition = PressConditions.SecondsEqualTo;
                int firstTwoSum = Alphabet.IndexOf(cString[0]) + Alphabet.IndexOf(cString[1]);
                int lastTwoSum = Alphabet.IndexOf(cString[cString.Length - 1]) + Alphabet.IndexOf(cString[cString.Length - 2]);
                pressArgs = new List<int> { Math.Abs(firstTwoSum - lastTwoSum) };
                break;
            case CTypes.Boolean:
                switch (curStage - 5)
                {
                    case 0: // Stage 6 - "The number is even."
                        pressCondition = cBoolean ? PressConditions.LastDigitEven : PressConditions.LastDigitOdd;
                        break;
                    case 1: // Stage 7 - "The number is composite."
                        pressCondition = PressConditions.LastDigitEqualTo;
                        pressArgs = cBoolean ? new List<int> { 4, 6, 8, 9 } : new List<int> { 0, 1, 2, 3, 5, 7 };
                        break;
                    case 2: // Stage 8 - "The number is odd."
                        pressCondition = cBoolean ? PressConditions.LastDigitOdd : PressConditions.LastDigitEven;
                        break;
                    case 3: // Stage 9 - "The number is divisible by 5."
                        pressCondition = PressConditions.LastDigitEqualTo;
                        pressArgs = cBoolean ? new List<int> { 0, 5 } : new List<int> { 1, 2, 3, 4, 6, 7, 8, 9 };
                        break;
                    case 4: // Stage 10 - "The number is prime."
                        pressCondition = PressConditions.LastDigitEqualTo;
                        pressArgs = cBoolean ? new List<int> { 2, 3, 5, 7 } : new List<int> { 0, 1, 4, 6, 8, 9 };
                        break;
                    default:
                        break;
                }
                break;
            case CTypes.Undefined:
                Log($"C is Undefined - Processing Stage {curStage - 4}'s rules:");
                switch (curStage)
                {
                    case 5:
                        ProcessStage1(false);
                        break;
                    case 6:
                        ProcessStage2(false);
                        break;
                    case 7:
                        ProcessStage3(false);
                        break;
                    case 8:
                        ProcessStage4(false);
                        break;
                    case 9:
                        ProcessStage5(false);
                        break;
                    default:
                        break;
                }
                break;
            default:
                break;
        }
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
        if (m == 0) return 0;
        else if (n > 0) return n % m;
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

    int Divide(int n, int m)
    {
        return m == 0 ? 0 : n / m;
    }

    bool IsPrime(int n)
    {
        if (n == 1 || n == 0) return false;
        if (n == 2) return true;
        for (int i = 2; i <= Math.Ceiling(Math.Sqrt(n)); ++i)
            if (n % i == 0)
                return false;
        return true;
    }

    bool IsComposite(int n)
    {
        if (n == 1 || n == 0) return false;
        else return !IsPrime(n);
    }

    string InsertIntoString(string s1, string s2, int pos)
    {
        return s1.Substring(0, pos) + s2 + s1.Substring(pos);
    }

    string LogBoolean(bool b)
    {
        return b ? "TRUE" : "FALSE";
    }

    void Log(string arg)
    {
        Debug.Log($"[Not Colored Cube #{ModuleId}] {arg}");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} #1 #2 #3...> to press the cube when the last digit of the timer is #1, or #2, or #3, etc. Use <!{0} ##1 ##2 ##3...> to press the cube when the seconds of the timer are ##1, or ##2, or ##3, etc. Use <!{0} any> to press the cube at any time. Use <!{0} cb> to toggle colorblind mode.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (commandArgs.Length == 0) yield return "sendtochaterror Invalid command!";
        else if (commandArgs[0] == "ANY")
        {
            yield return null;
            CubeSelectable.OnInteract();
        }
        else if (commandArgs[0] == "CB")
        {
            yield return null;
            ColorblindText.gameObject.SetActive(!ColorblindText.gameObject.activeInHierarchy);
        }
        else
        {
            int argLen = commandArgs[0].Length;
            List<int> pressArgs = new List<int> { };
            if (argLen < 1 || argLen > 2) yield return "sendtochaterror Invalid time!";
            else
            {
                foreach (string arg in commandArgs)
                {
                    if (arg.Length != argLen)
                    {
                        yield return "sendtochaterror Invalid time!";
                        break;
                    }
                    else
                    {
                        int argNum;
                        if (int.TryParse(arg, out argNum))
                        {
                            if (argLen == 1 && !(0 <= argNum && argNum <= 9))
                            {
                                yield return "sendtochatmessage Invalid time!";
                                break;
                            }
                            else if (argLen == 2 && !(0 <= argNum && argNum <= 59))
                            {
                                yield return "sendtochatmessage Invalid time!";
                                break;
                            }
                            else pressArgs.Add(argNum);
                        }
                        else
                        {
                            yield return "sendtochatmessage Invalid time!";
                            break;
                        }
                    }
                }
                if (pressArgs.Count > 0)
                {
                    yield return null;
                    if (argLen == 1)
                    {
                        while (!pressArgs.Contains((int)Bomb.GetTime() % 10))
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        while (!pressArgs.Contains((int)Bomb.GetTime() % 60))
                        {
                            yield return null;
                        }
                    }
                    CubeSelectable.OnInteract();
                    yield return new WaitForSeconds(0.5f);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        TPSolverText.gameObject.SetActive(true);
        while (!ModuleSolved)
        {
            int waitingFor = -1;
            int curBombTime = (int)Bomb.GetTime();
            if (ZenModeActive)
            {
                for (int t = 0; t < 60; t++)
                {
                    if (CheckPressCondition(curBombTime + t))
                    {
                        waitingFor = (curBombTime + t) % 60;
                        break;
                    }
                }
            }
            else
            {
                for (int t = 0; t < 60; t++)
                {
                    if (curBombTime - t > 0)
                    {
                        if (CheckPressCondition(curBombTime - t))
                        {
                            waitingFor = (curBombTime - t) % 60;
                            break;
                        }
                    }
                }
            }
            if (waitingFor > -1)
            {
                TPSolverText.text = $"WAITING FOR {IntoTwoDigitNumber(waitingFor)}...";
                while ((int)Bomb.GetTime() % 60 != waitingFor)
                {
                    yield return null;
                }
                if (curStage == 9)
                {
                    TPSolverText.text = "AUTOSOLVED!";
                    StartCoroutine(AutoSolveTextAnim());
                }
                CubeSelectable.OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
            else
            {
                TPSolverText.text = $"NOT ENOUGH TIME,\nAUTOSOLVING NOW!";
                TPSolverText.fontSize = 120;
                StartCoroutine(AutoSolveTextAnim());
                curStage = 9;
                NextStage();
            }
        }
    }

    IEnumerator AutoSolveTextAnim()
    {
        yield return new WaitForSeconds(2f);
        while (TPSolverText.text.Length > 0)
        {
            TPSolverText.text = TPSolverText.text.Substring(0, TPSolverText.text.Length - 1);
            yield return new WaitForSeconds(0.05f);
        }
    }
}