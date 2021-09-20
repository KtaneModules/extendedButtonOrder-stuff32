using KModkit;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;
using System.Linq;


public class buttonOrder : MonoBehaviour {

	static int _moduleIdCounter = 1;
	int _moduleID = 0;
	bool moduleSolved = false;

	public KMBombModule Module;
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMSelectable[] Buttons;
	public TextMesh[] ButtonTexts;


	string answer;
	private int missingLabel;
	private string inputtedCode = "";
	public Color[] TextColors;
	bool buttonLock = false;

	void Awake()
    {
		missingLabel = Rnd.Range(0, 10);
		ButtonTexts[missingLabel].gameObject.SetActive(false);
		answer = GetOrder();
		Debug.Log(answer);
		Log("The correct sequence of buttons is, ", answer);
	}


	// Use this for initialization
	void Start () {
		for (int btn = 0; btn < Buttons.Length; btn++)
		{
			Buttons[btn].OnInteract = ButtonPressed(btn);
			ButtonTexts[btn].color = TextColors[0];
		}
	}
	
	// Update is called once per frame
	void Update () {

    }

	private KMSelectable.OnInteractHandler ButtonPressed(int btn)
	{
		return delegate
		{
			if (moduleSolved == true || buttonLock == true)
            {
				return false;
            }
			Audio.PlaySoundAtTransform("beep", Buttons[btn].transform);
			Buttons[btn].AddInteractionPunch();
			ButtonTexts[btn].color = TextColors[1];
			if (inputtedCode.Contains(ButtonTexts[btn].text) && inputtedCode.Length > 0)
            {
				return false;
            }
			if (inputtedCode.Length < 10)
            {
				inputtedCode += ButtonTexts[btn].text;
            } 
			if (inputtedCode.Length >= 10)
            {
				if (inputtedCode == answer)
				{
					moduleSolved = true;
					StartCoroutine(Solve());
					Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, Buttons[btn].transform);
					Log("Correct button has been pressed. Module solved!");
					Module.HandlePass();

				}
				else
				{
					buttonLock = true;
					Log("Incorrect sequence has been entered. You have entered {0}. I was expecting {1}.", inputtedCode, answer);
					inputtedCode = "";
					StartCoroutine(Strike());
					
				}

			}
			return false;

		};
	}

	IEnumerator Solve()
    {
		ButtonTexts[missingLabel].gameObject.SetActive(true);
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[1];

			yield return new WaitForSeconds(0.3f); 
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[0];

		yield return new WaitForSeconds(0.3f);
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[1];

		yield return new WaitForSeconds(0.3f);
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[0];


	}

	IEnumerator Strike()
    {
		ButtonTexts[missingLabel].gameObject.SetActive(true);
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[2];

		yield return new WaitForSeconds(0.3f);
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[0];

		yield return new WaitForSeconds(0.3f);
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[2];

		yield return new WaitForSeconds(0.3f);
		for (int btn = 0; btn < Buttons.Length; btn++)
			ButtonTexts[btn].color = TextColors[0];
		Module.HandleStrike();
		buttonLock = false;
		Awake();
	}

	private string GetOrder()
    {
		if (Bomb.GetBatteryCount() >= 3 && Bomb.IsIndicatorOn(Indicator.BOB))
		{
			return "0123456789";
		}
		else if (Bomb.GetPortPlates().Any(p => p.Length == 0) && missingLabel != 8 && missingLabel != 2 && missingLabel != 9 && missingLabel != 1) {
			return "0912345876";
        }
		else if (Bomb.IsIndicatorPresent(Indicator.CAR) && Bomb.GetPortCount(Port.Serial) > 0 && missingLabel != 8 && missingLabel != 0)
		{
			return "0526398741";
		}
		else if (Bomb.GetBatteryCount(Battery.D) > 0 && Bomb.GetPortPlateCount() > 1)
        {
			return "7894560312";
        }
		else
        {
			return "5869473012";
        }
		 
    }

	private void Log(string message, params object[] args)
	{
		Debug.LogFormat("[Extended Button Order #{0}] {1}", _moduleID, string.Format(message, args));

	}
}
