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
    private int _moduleId;
    private bool _moduleSolved;

    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMSelectable[] ButtonSels;
    public TextMesh[] ButtonTexts;
    public Color[] TextColors;
    public MeshRenderer[] StageInds;
    public Material[] Materials;

    private int[] _stageOneNumbers;
    private int[] _stageTwoNumbers;
    private int _snSum;
    private int _stageOneNum;
    private int[] _stageOneAnswer;
    private readonly int[] _stageTwoUnshifted = new int[10];
    private readonly int[] _stageTwoAnswer = new int[10];
    private static readonly int[] _zeroPositions = { 7, 3, 0, 8, 7, 5, 1, 4, 2, 1 };
    private int _zeroShift;
    private int _stage;
    private List<int> _input = new List<int>();
    private int _numsInputted;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < ButtonSels.Length; i++)
            ButtonSels[i].OnInteract += ButtonPress(i);
        _stageOneNumbers = Enumerable.Range(0, 10).ToArray().Shuffle();
        for (int i = 0; i < _stageOneNumbers.Length; i++)
            ButtonTexts[i].text = _stageOneNumbers[i].ToString();
        var serialNumber = BombInfo.GetSerialNumber();
        for (int i = 0; i < serialNumber.Length; i++)
            _snSum += serialNumber[i] >= '0' && serialNumber[i] <= '9' ? serialNumber[i] - '0' : 0;
        _stageOneNum = _snSum * _snSum;
        _stageOneNum *= serialNumber[5] - '0';
        var numToString = _stageOneNum.ToString();
        _stageOneAnswer = new int[numToString.Length];
        for (int i = 0; i < numToString.Length; i++)
            _stageOneAnswer[i] = numToString[i] - '0';
        Debug.LogFormat("[Extended Button Order #{0}] Stage 1 answer: {1}", _moduleId, numToString);
        _stageTwoNumbers = Enumerable.Range(0, 10).ToArray().Shuffle();
        for (int i = 0; i < _stageTwoUnshifted.Length; i++)
            _stageTwoUnshifted[i] = (_stageOneNumbers[i] + _stageTwoNumbers[i]) % 10;
        _zeroShift = _zeroPositions[Array.IndexOf(_stageTwoNumbers, 0)];
        for (int i = 0; i < _stageTwoUnshifted.Length; i++)
            _stageTwoAnswer[i] = _stageTwoUnshifted[(i + _zeroShift) % 10];
        Debug.LogFormat("[Extended Button Order #{0}] Stage 2 answer: {1}", _moduleId, _stageTwoAnswer.Join(""));
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate ()
        {
            ButtonSels[btn].AddInteractionPunch(0.5f);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ButtonSels[btn].transform);
            if (_moduleSolved)
                return false;
            if (_stage == 0)
            {
                _input.Add(btn);
                if (_input[_numsInputted] != _stageOneAnswer[_numsInputted])
                {
                    Debug.LogFormat("[Extended Button Order #{0}] Pressed position {1} instead of {2}. Strike.", _moduleId, btn, _stageOneAnswer[_numsInputted]);
                    Module.HandleStrike();
                    StartCoroutine(FlashRed());
                    _input = new List<int>();
                    _numsInputted = 0;
                }
                else
                {
                    _numsInputted++;
                    if (_input.Count == _stageOneAnswer.Length)
                    {
                        _numsInputted = 0;
                        Debug.LogFormat("[Extended Button Order #{0}] Completed Stage 1. Moving onto Stage 2.", _moduleId);
                        _input = new List<int>();
                        _stage++;
                        for (int i = 0; i < _stageTwoNumbers.Length; i++)
                            ButtonTexts[i].text = _stageTwoNumbers[i].ToString();
                    }
                }
            }
            else
            {
                _input.Add(_stageTwoNumbers[btn]);
                if (_input[_numsInputted] != _stageTwoAnswer[_numsInputted])
                {
                    Debug.LogFormat("Extended Button Order #{0}] Pressed label {1} instead of {2}. Strike. Resetting back to Stage 1.", _moduleId, _stageTwoNumbers[btn], _stageTwoAnswer[_numsInputted]);
                    Module.HandleStrike();
                    StartCoroutine(FlashRed());
                    _input = new List<int>();
                    _numsInputted = 0;
                    _stage = 0;
                    for (int i = 0; i < _stageOneNumbers.Length; i++)
                        ButtonTexts[i].text = _stageOneNumbers[i].ToString();
                }
                else
                {
                    _numsInputted++;
                    if (_input.Count == _stageTwoAnswer.Length)
                    {
                        _moduleSolved = true;
                        Module.HandlePass();
                        StartCoroutine(FlashNumbers());
                        Debug.LogFormat("[Extended Button Order #{0}] Completed Stage 1. Moving onto Stage 2.", _moduleId);
                    }
                }
            }
            return false;
        };
    }

    private IEnumerator FlashNumbers()
    {
        for (int i = 0; i < 47; i++)
        {
            foreach (var btnText in ButtonTexts)
                btnText.color = TextColors[i % 2];
            yield return new WaitForSeconds(0.1f);
        }
    }
    private IEnumerator FlashRed()
    {
        foreach (var btnText in ButtonTexts)
            btnText.color = TextColors[2];
        yield return new WaitForSeconds(1f);
        foreach (var btnText in ButtonTexts)
            btnText.color = TextColors[0];
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} position 0 1 2 [Presses buttons in positions 0, 1, 2. Zero-indexed.] | !{0} label 0 1 2 [Presses buttons with labels 0, 1, 2]";
#pragma warning restore 0414

    private KMSelectable[] ProcessTwitchCommand(string command)
    {
        var m = Regex.Match(command, @"^\s*(?:position|pos|p)\s+([0123456789 ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
            return m.Groups[1].Value.Where(ch => ch != ' ').Select(ch => ButtonSels[ch - '0']).ToArray();
        m = Regex.Match(command, @"^\s*(?:label|lab|lbl|l)\s+([0123456789 ]+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (m.Success)
            return m.Groups[1].Value.Where(ch => ch != ' ').Select(ch => ButtonSels[Array.IndexOf(_stage == 0 ? _stageOneNumbers : _stageTwoNumbers, ch - '0')]).ToArray();
        return null;
    }
}
