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

    private string answer;
    private int missingLabel;
    private string inputtedCode = "";
    private bool buttonLock;

    private void Start()
    {
        GenerateSolution();
        for (int btn = 0; btn < Buttons.Length; btn++)
        {
            Buttons[btn].OnInteract = ButtonPressed(btn);
            ButtonTexts[btn].color = TextColors[0];
        }
    }

    private void GenerateSolution()
    {
        missingLabel = Rnd.Range(0, 10);
        ButtonTexts[missingLabel].text = "";
        ButtonTexts[missingLabel].gameObject.SetActive(false);
        answer = GetOrder();
        Debug.LogFormat("[Extended Button Order #{0}] The correct order to press the buttons is {1}.", _moduleID, answer);
    }

    private KMSelectable.OnInteractHandler ButtonPressed(int btn)
    {
        return delegate
        {
            if (moduleSolved == true || buttonLock == true)
                return false;
            Audio.PlaySoundAtTransform("beep", Buttons[btn].transform);
            Buttons[btn].AddInteractionPunch();
            ButtonTexts[btn].color = TextColors[1];
            if (inputtedCode.Contains(btn.ToString()) && inputtedCode.Length > 0)
                return false;
            if (inputtedCode.Length < 10)
                inputtedCode += btn.ToString();
            if (inputtedCode.Length >= 10)
            {
                if (inputtedCode == answer)
                {
                    moduleSolved = true;
                    StartCoroutine(Solve());
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Buttons[btn].transform);
                    Debug.LogFormat("[Extended Button Order #{0}] You pressed the buttons in the correct order. Module solved!", _moduleID);
                    Module.HandlePass();
                }
                else
                {
                    buttonLock = true;
                    Debug.LogFormat("[Extended Button Order #{0}] You entered {1}. I was expecting {2}. Strike.", _moduleID, inputtedCode, answer);
                    inputtedCode = "";
                    StartCoroutine(Strike());
                }
            }
            return false;
        };
    }

    private IEnumerator Solve()
    {
        ButtonTexts[missingLabel].text = missingLabel.ToString();
        ButtonTexts[missingLabel].gameObject.SetActive(true);
        int init = 12;
        for (int i = 0; i < init; i++)
        {
            for (int j = 0; j < ButtonTexts.Length; j++)
                ButtonTexts[j].color = i % 2 == 0 ? TextColors[0] : TextColors[1];
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator Strike()
    {
        ButtonTexts[missingLabel].text = missingLabel.ToString();
        int init = 12;
        for (int i = 0; i < init; i++)
        {
            for (int j = 0; j < ButtonTexts.Length; j++)
                ButtonTexts[j].color = i % 2 == 0 ? TextColors[2] : TextColors[0];
            yield return new WaitForSeconds(0.1f);
        }
        Module.HandleStrike();
        buttonLock = false;
        GenerateSolution();
    }

    private string GetOrder()
    {
        if (Bomb.GetBatteryCount() >= 3 && Bomb.IsIndicatorOn(Indicator.BOB))
            return "0123456789";
        else if (Bomb.GetPortPlates().Any(p => p.Length == 0) && missingLabel != 8 && missingLabel != 2 && missingLabel != 9 && missingLabel != 1)
            return "0912345876";
        else if (Bomb.IsIndicatorPresent(Indicator.CAR) && Bomb.GetPortCount(Port.Serial) > 0 && missingLabel != 8 && missingLabel != 0)
            return "0526398741";
        else if (Bomb.GetBatteryCount(Battery.D) > 0 && Bomb.GetPortPlateCount() > 1)
            return "7894560312";
        else
            return "5869473012";
    }

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
