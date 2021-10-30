using KModkit;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;
using System.Linq;


public class buttonOrder : MonoBehaviour
{

    private static int _moduleIdCounter = 1;
    private int _moduleID = 0;
    private bool moduleSolved;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public TextMesh[] ButtonTexts;
    public Color[] TextColors;
    public MeshRenderer[] StageInds;
    public Material[] materials;

    private string answer;
    private string revealingAnswer;
    private string inputtedCode = "";
    private string inputtedRevealingCode = "";
    private bool buttonLock;
    private int chimeButton;
    private int stage;
    private int firstStageCheck;
    private int secondStageLightUp;
    private string[] numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    private string[] numbers2 = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
    private string[] initialState = new string[10];
    private string[] secondState = new string[10];
    private int shift;


    private void Start()
    {
        numbers = numbers.Shuffle();
        numbers2 = numbers2.Shuffle();
        initialState = numbers;
        secondState = numbers2;
        stage = 1;
        firstStageCheck = 0;
        secondStageLightUp = 0;
        for (int btn = 0; btn < Buttons.Length; btn++)
        {
            Buttons[btn].OnInteract = ButtonPressed(btn);
            ButtonTexts[btn].text = initialState[btn];
            ButtonTexts[btn].color = TextColors[0];
        }
        revealingAnswer = GetRevealOrder();
        shift = DetermineShift();
        answer = GenerateAnswer();
        Debug.LogFormat("[Extended Button Order #{0}] The correct answer for the first stage is {1}.", _moduleID, revealingAnswer);
        Debug.LogFormat("[Extended Button Order #{0}] The correct answer for the second stage is {1}.", _moduleID, answer);

    }

    private void StageTwo()
    {
        stage = 2;
        for (int btn = 0; btn < Buttons.Length; btn++)
        {
            ButtonTexts[btn].text = secondState[btn];
        }
    }

    private KMSelectable.OnInteractHandler ButtonPressed(int btn)
    {
        return delegate
        {
            if (moduleSolved == true || buttonLock == true)
                return false;
            if (stage == 1)
            {
                Audio.PlaySoundAtTransform("beep", Buttons[btn].transform);
                Buttons[btn].AddInteractionPunch();
                inputtedRevealingCode += btn.ToString();
                if (inputtedRevealingCode[firstStageCheck] == revealingAnswer[firstStageCheck])
                {
                    firstStageCheck++;
                }
                else
                {
                    inputtedRevealingCode = "";
                    firstStageCheck = 0;
                    Module.HandleStrike();
                }
                if (inputtedRevealingCode == revealingAnswer)
                {
                    StageInds[0].GetComponent<Renderer>().material = materials[0];
                    StageTwo();
                }

            }
            else
            {
                Audio.PlaySoundAtTransform("beep", Buttons[btn].transform);
                Buttons[btn].AddInteractionPunch();
                ButtonTexts[secondStageLightUp].color = TextColors[1];
                if (inputtedCode.Length < 10)
                    inputtedCode += secondState[btn];
                secondStageLightUp++;
                if (inputtedCode.Length >= 10)
                {
                    if (inputtedCode == answer)
                    {
                        moduleSolved = true;
                        StageInds[1].GetComponent<Renderer>().material = materials[0];
                        StartCoroutine(Solve());
                        chimeButton = btn;
                        Debug.LogFormat("[Extended Button Order #{0}] You pressed the buttons in the correct order. Module solved!", _moduleID);

                    }
                    else
                    {
                        buttonLock = true;
                        Debug.LogFormat("[Extended Button Order #{0}] You entered {1}. I was expecting {2}. Strike.", _moduleID, inputtedCode, answer);
                        StartCoroutine(Strike());
                        inputtedCode = "";
                        inputtedRevealingCode = "";
                        StageInds[0].GetComponent<Renderer>().material = materials[1];
                        StageInds[1].GetComponent<Renderer>().material = materials[1];
                        Start();
                    }
                }

            }

            return false;
        };
    }

    private IEnumerator Solve()
    {
        int init = 4;
        for (int i = 0; i < init; i++)
        {
            for (int j = 0; j < ButtonTexts.Length; j++)
                ButtonTexts[j].color = i % 2 == 0 ? TextColors[0] : TextColors[1];
            yield return new WaitForSeconds(0.3f);
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Buttons[chimeButton].transform);
        Module.HandlePass();
    }

    private IEnumerator Strike()
    {
        int init = 4;
        for (int i = 0; i < init; i++)
        {
            for (int j = 0; j < ButtonTexts.Length; j++)
                ButtonTexts[j].color = i % 2 == 0 ? TextColors[2] : TextColors[0];
            yield return new WaitForSeconds(0.3f);
        }
        Module.HandleStrike();
        buttonLock = false;
    }



    private string GetRevealOrder()
    {
        int tempAnswer = Bomb.GetSerialNumberNumbers().Sum();
        tempAnswer *= tempAnswer;
        tempAnswer *= Bomb.GetSerialNumberNumbers().Last();
        return tempAnswer.ToString();
    }

    private string GenerateAnswer()
    {

        int[] answer = new int[10];
        for (int i = 0; i < Buttons.Length; i++)
        {
            answer[i] = (Int32.Parse(initialState[i]) + Int32.Parse(secondState[i])) % 10;

        }
        string stringAnswer = answer.Join("");
        return stringAnswer.Substring(stringAnswer.Length - shift, shift) + stringAnswer.Substring(0, stringAnswer.Length - shift);

    }

    private int DetermineShift()
    {
        int whereIsZero = Array.IndexOf(secondState, "0");
        switch (whereIsZero)
        {
            case 0:
                return 4;
            case 1:
                return 7;
            case 2:
                return 1;
            case 3:
                return 8;
            case 4:
                return 9;
            case 5:
                return 6;
            case 6:
                return 2;
            case 7:
                return 5;
            case 8:
                return 3;
            case 9:
                return 2;
        }
        return 0;


    }


    //GoodHood ignore everything from here this is complicated stuff you don't understand. (Added by Quinn Wuest)
    private static string[] _twitchCommands = { "press", "push", "tap", "submit", "answer" };

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} press 0 1 2 3 4 5 6 7 8 9 | Presses buttons 0-9.";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length == 0)
            yield break;
        var skip = _twitchCommands.Contains(pieces[0]) ? 1 : 0;
        if (pieces.Skip(skip).Any(p => { int val; return !int.TryParse(p.Trim(), out val) || val < 0 || val > 9; }))
            yield break;
        yield return null;
        foreach (var p in pieces.Skip(skip))
        {
            Buttons[int.Parse(p.Trim())].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        int[] arr = new int[10];
        for (int i = 0; i < arr.Length; i++)
            arr[i] = int.Parse(answer.Substring(i, 1));
        for (int btn = 0; btn < 10; btn++)
        {
            Buttons[arr[btn]].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
    }
}
